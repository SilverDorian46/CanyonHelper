using System;
using Microsoft.Xna.Framework;
using Monocle;
using System.Linq;

namespace Celeste.Mod.CanyonHelper
{
    class PushBlock : Solid
    {
        private readonly MTexture texture;
        private readonly MTexture[] stickyTextures;
        private float speedX = 0;
        private float speedY = 0;
        private Level level;
        private Player playerEnt;

        private readonly bool stickyTop = false;
        private readonly bool stickyBottom = false;
        private readonly bool stickyLeft = false;
        private readonly bool stickyRight = false;

        private bool lastTouchingGround = true;
        private bool touchingGround;
        private bool touchingLeftWall;
        private bool touchingRightWall;
        private bool touchingCeiling;

        private readonly bool isTemple = false;
        private readonly char breakDebrisTileset = '5';

        private float gravity = 0;
        private bool applyGravity = false;

        private const float maxDelay = 0.1f;
        private float delayBetweenImpactEffect = maxDelay;

        private DashSwitch[] dashSwitches;

        private readonly MTexture[] stickyTextureArray = new MTexture[4];

        private readonly bool legacy;
        private readonly string blockTexture;
        private readonly string gooTexture;

        public PushBlock(EntityData data, Vector2 offset) : base(data.Position + offset, 32, 32, false)
        {
            // Added support for custom textures
            string customBlockTexture = data.Attr("customBlockTexture");
            string customGooTexture = data.Attr("customGooTexture");
            blockTexture = string.IsNullOrEmpty(customBlockTexture) ? "objects/canyon/pushblock/idle" : customBlockTexture;
            gooTexture = string.IsNullOrEmpty(customGooTexture) ? "objects/canyon/pushblock/stickyGoo" : customGooTexture;

            stickyTextures = GFX.Game.GetAtlasSubtextures(gooTexture).ToArray();
            OnDashCollide = new DashCollision(OnDashed);
            Collider.Center = Vector2.Zero;

            stickyTop = data.Bool("stickyTop", false);
            stickyBottom = data.Bool("stickyBottom", false);
            stickyLeft = data.Bool("stickyLeft", false);
            stickyRight = data.Bool("stickyRight", false);

            isTemple = data.Bool("isTemple", false);

            // Added a legacy option which is on for every placement before this update, and off by default for any future placements.
            legacy = data.Bool("legacy", !data.Has("legacy"));

            for (int i = 0; i < stickyTextureArray.Length; i++)
            {
                stickyTextureArray[i] = Calc.Random.Choose(stickyTextures);
            }

            string texturePath = blockTexture + (isTemple ? "Temple" : "");
            texture = GFX.Game[texturePath];
            if (isTemple)
            {
                breakDebrisTileset = 'f';
            }

            // Override debris data if enabled.
            if (data.Bool("overrideDebris"))
            {
                breakDebrisTileset = data.Char("customDebrisFromTileset");
            }
        }

        public override void Added(Scene scene)
        {
            base.Added(scene);
            level = SceneAs<Level>();
            dashSwitches = scene.Entities.OfType<DashSwitch>().ToArray();
        }

        private DashCollisionResults OnDashed(Player player, Vector2 direction)
        {
            DashCollisionResults result;
            if (player.Left + 1 > Right || player.Right - 1 < Left) //If not on top/bottom
            {
                if (OnDashed(player.Center))
                {
                    result = DashCollisionResults.Rebound;
                    level.Session.IncrementCounter("pushBlocksHit");
                }
                else
                {
                    result = DashCollisionResults.NormalCollision;
                }
            }
            else
            {
                if (OnDashedTop(player.Center))
                {
                    result = DashCollisionResults.Rebound;
                    level.Session.IncrementCounter("pushBlocksHit");
                }
                else
                {
                    result = DashCollisionResults.NormalCollision;
                }
            }
            playerEnt = player;
            return result;
        }

        public override void Update()
        {
            base.Update();
            ApplyGravityCheck();
            if (delayBetweenImpactEffect > 0)
            {
                delayBetweenImpactEffect -= Engine.DeltaTime;
            }
            else
            {
                delayBetweenImpactEffect = 0;
            }
            if (applyGravity)
            {
                if (gravity < 160)
                {
                    gravity += 480 * Engine.DeltaTime;
                }
                else
                {
                    gravity = 160;
                }
            }
            else
            {
                gravity = 0;
            }
            if (Scene.OnInterval(0.03f))
            {
                if (speedY > 0f)
                {
                    ScrapeParticles(Vector2.UnitY);
                }
                else if (speedY < 0f)
                {
                    ScrapeParticles(-Vector2.UnitY);
                }
                if (speedX > 0f)
                {
                    ScrapeParticles(Vector2.UnitX);
                }
                else if (speedX < 0f)
                {
                    ScrapeParticles(-Vector2.UnitX);
                }
            }

            if (legacy)
                MoveHExactCollideSolids((int)Math.Floor(speedX * Engine.DeltaTime), false, null);
            else
                MoveHCollideSolids(speedX * Engine.DeltaTime, false);
            if (speedX != 0)
            {
                speedX = Calc.Approach(speedX, 0, 960 * Engine.DeltaTime);
                if (Left < level.Bounds.Left || Right > level.Bounds.Right)
                    speedX = 0;
            }
            if (CollideCheck<SolidTiles>(Position + Vector2.UnitX * Math.Sign(speedX) * 256f) || CheckCollisionX(speedX))
            {
                Calc.Approach(speedX, 24f, 500f * Engine.DeltaTime * 0.25f);
            }

            if (legacy)
                MoveVExactCollideSolids((int)Math.Floor((speedY + (applyGravity ? gravity : 0f)) * Engine.DeltaTime), false, null);
            else
                MoveVCollideSolids((speedY + gravity) * Engine.DeltaTime, false);
            if (speedY != 0)
            {
                if (Top < level.Bounds.Top)
                    speedY = 0;
                if (Top > level.Bounds.Bottom)
                    RemoveSelf();
                speedY = Calc.Approach(speedY, 0, 960 * Engine.DeltaTime);
            }
            if (CollideCheck<SolidTiles>(Position + Vector2.UnitY * Math.Sign(speedY) * 256f) || CheckCollisionY(speedY))
            {
                Calc.Approach(speedY, 24f, 500f * Engine.DeltaTime * 0.25f);
            }
            //Is on edge? Slide off!
            if (speedX == 0 && !applyGravity)
            {
                int amt = 0;
                if (!Scene.CollideCheck<Solid>(BottomRight))
                    amt++;
                if (!Scene.CollideCheck<Solid>(BottomLeft))
                    amt++;
                if (amt == 1 && (Left < level.Bounds.Left || Right > level.Bounds.Right))
                {
                    if (Scene.CollideCheck<Solid>(BottomLeft) && !Scene.CollideCheck<Solid>(BottomLeft + new Vector2(4, 1)))
                    {
                        speedX = 150f;
                    }
                    else if (Scene.CollideCheck<Solid>(BottomRight) && !Scene.CollideCheck<Solid>(BottomRight + new Vector2(-4, 1)))
                    {
                        speedX = -150f;
                    }
                }
            }
        }

        private void LandParticles()
        {
            int num = 0;
            while (num <= Width)
            {
                if (Scene.CollideCheck<Solid>(BottomLeft + new Vector2(num, 1f)))
                {
                    SceneAs<Level>().ParticlesFG.Emit(FallingBlock.P_FallDustA, 1, new Vector2(Left + num, Bottom), Vector2.One * 4f, (float)-Math.PI / 2);
                    float direction;
                    if (num < Width / 2f)
                    {
                        direction = (float)Math.PI;
                    }
                    else
                    {
                        direction = 0f;
                    }
                    SceneAs<Level>().ParticlesFG.Emit(FallingBlock.P_LandDust, 1, new Vector2(Left + num, Bottom), Vector2.One * 4f, direction);
                }
                num += 4;
            }
        }

        private void ScrapeParticles(Vector2 dir)
        {
            if (dir.X != 0f)
            {
                int x = 0;
                while ((legacy && x < Width) || x <= Width)
                {
                    Vector2 bottomPos = new Vector2(Left + x, Bottom + 1);
                    if (Scene.CollideCheck<Solid>(bottomPos))
                    {
                        SceneAs<Level>().ParticlesFG.Emit(ZipMover.P_Scrape, bottomPos);
                    }
                    Vector2 topPos = new Vector2(Left + x, Top - 1);
                    if (Scene.CollideCheck<Solid>(topPos))
                    {
                        SceneAs<Level>().ParticlesFG.Emit(ZipMover.P_Scrape, topPos);
                    }
                    x += 8;
                }
            }
            else
            {
                int y = 0;
                while ((legacy && y < Height) || y <= Height)
                {
                    Vector2 leftPos = new Vector2(Left - 1, Top + y);
                    if (Scene.CollideCheck<Solid>(leftPos))
                    {
                        SceneAs<Level>().ParticlesFG.Emit(ZipMover.P_Scrape, leftPos);
                    }
                    Vector2 rightPos = new Vector2(Right + 1, Top + y);
                    if (Scene.CollideCheck<Solid>(rightPos))
                    {
                        SceneAs<Level>().ParticlesFG.Emit(ZipMover.P_Scrape, rightPos);
                    }
                    y += 8;
                }
            }
        }

        private void ApplyGravityCheck()
        {
            touchingGround = CheckCollisionY(1, true);
            touchingCeiling = CheckCollisionY(-1, true);
            touchingLeftWall = CheckCollisionX(-1, true);
            touchingRightWall = CheckCollisionX(1, true);

            if (touchingGround && !lastTouchingGround)
            {
                Audio.Play("event:/game/general/fallblock_impact", BottomCenter);
                StartShaking(0.2f);
                Input.Rumble(RumbleStrength.Strong, RumbleLength.Medium);
                SceneAs<Level>().DirectionalShake(Vector2.UnitY, 0.3f);
                LandParticles();
            }

            lastTouchingGround = touchingGround;

            if (touchingGround)
            {
                applyGravity = false;
                return;
            }
            if (stickyTop && touchingCeiling)
            {
                applyGravity = false;
                return;
            }
            if (stickyLeft && touchingLeftWall)
            {
                applyGravity = false;
                return;
            }
            if (stickyRight && touchingRightWall)
            {
                applyGravity = false;
                return;
            }
            applyGravity = true;
        }

        private bool CheckCollisionX(float amount, bool ignoreLevelBounds = false)
        {
            if (!ignoreLevelBounds)
            {
                if (Left + amount * Engine.DeltaTime < level.Bounds.Left || Right + amount * Engine.DeltaTime > level.Bounds.Right)
                    return true;
            }
            int i = 2;
            while (i < Height)
            {
                if (Scene.CollideCheck<Solid>((amount < 0 ? TopLeft : TopRight) + new Vector2(amount * Engine.DeltaTime, i)))
                {
                    foreach (DashSwitch dSwitch in dashSwitches)
                    {
                        if (CollideCheck(dSwitch))
                        {
                            dSwitch.OnDashCollide(null, Vector2.UnitX * Math.Sign(speedX));
                            return false;
                        }
                    }
                    return true;
                }
                i += 4;
            }
            return false;
        }

        private bool CheckCollisionY(float amount, bool ignoreLevelBounds = false)
        {
            if (!ignoreLevelBounds)
            {
                if (amount < 0) //if moving up
                {
                    if (Top < level.Bounds.Top)
                        return true;
                }
            }
            int i = 0;
            while (i < Width)
            {
                if (Scene.CollideCheck<Solid>((amount < 0 ? TopLeft : BottomLeft) + new Vector2(i, amount * Engine.DeltaTime)) || Scene.CollideCheck<Platform>((amount < 0 ? TopLeft : BottomLeft) + new Vector2(i, amount * Engine.DeltaTime)))
                {
                    return true;
                }
                i += 4;
            }
            if (Scene.CollideCheck<Solid>((amount < 0 ? TopLeft : BottomLeft) + new Vector2(31.5f, amount * Engine.DeltaTime)) || Scene.CollideCheck<Platform>((amount < 0 ? TopLeft : BottomLeft) + new Vector2(31.5f, amount * Engine.DeltaTime)))
            {
                return true;
            }
            return false;
        }

        private bool OnDashed(Vector2 from)
        {
            int j = 0;
            Vector2 startPos = TopLeft;
            int addPos = -1;
            while (j < Width)
            {
                if (from.X < Position.X) //from left
                {
                    startPos = TopRight;
                    addPos = 1;
                }
                if (Scene.CollideCheck<Solid>(startPos + new Vector2(addPos, j)) || Scene.CollideCheck<Platform>(startPos + new Vector2(addPos, j)))
                {
                    return false;
                }
                j += 4;
            }

            if (delayBetweenImpactEffect <= 0)
                Audio.Play("event:/game/general/wall_break_stone", Position);

            if (!(Left + 1 < level.Bounds.Left || Right + 1 > level.Bounds.Right))
            {
                if (from.X < Position.X) //If from the left
                {
                    if (delayBetweenImpactEffect <= 0)
                    {
                        for (int i = 1; i < 7; i++)
                        {
                            Scene.Add(Engine.Pooler.Create<Debris>().Init(TopLeft + new Vector2(-1, i * 4), breakDebrisTileset).BlastFrom(TopRight - TopCenter + from));
                        }
                        delayBetweenImpactEffect = maxDelay;
                    }
                    speedX = 320;
                }
                else //If from the right
                {
                    if (delayBetweenImpactEffect <= 0)
                    {
                        for (int i = 1; i < 7; i++)
                        {
                            Scene.Add(Engine.Pooler.Create<Debris>().Init(TopRight + new Vector2(1, i * 4), breakDebrisTileset).BlastFrom(legacy ? (TopLeft - TopCenter - from) : (TopLeft - TopCenter + from)));
                        }
                        delayBetweenImpactEffect = maxDelay;
                    }
                    speedX = -320;
                }
            }
            return true;
        }

        private bool OnDashedTop(Vector2 from)
        {
            int j = 0;
            Vector2 startPos = TopLeft;
            int addPos = -1;
            while (j < Width)
            {
                if (from.Y < Position.Y) //from the top
                {
                    startPos = BottomLeft;
                    addPos = 1;
                }
                if (Scene.CollideCheck<Solid>(startPos + new Vector2(j, addPos)) || Scene.CollideCheck<Platform>(startPos + new Vector2(j, addPos)))
                {
                    return false;
                }
                j += 4;
            }

            if (delayBetweenImpactEffect <= 0)
                Audio.Play("event:/game/general/wall_break_stone", Position);
            if (from.Y < Position.Y) //If from the top
            {
                if (delayBetweenImpactEffect <= 0)
                {
                    for (int i = 1; i < 7; i++)
                    {
                        Scene.Add(Engine.Pooler.Create<Debris>().Init(TopLeft + new Vector2(i * 4, -2), breakDebrisTileset).BlastFrom(TopCenter - Vector2.UnitY));
                    }
                    delayBetweenImpactEffect = maxDelay;
                }
                speedY = 320;
            }
            else //If from the bottom
            {
                if (delayBetweenImpactEffect <= 0)
                {
                    for (int i = 1; i < 7; i++)
                    {
                        Scene.Add(Engine.Pooler.Create<Debris>().Init(BottomLeft + new Vector2(i * 4, 2), breakDebrisTileset).BlastFrom(BottomCenter + Vector2.UnitY));
                    }
                    delayBetweenImpactEffect = maxDelay;
                }
                speedY = -320;
            }
            return true;
        }

        public override void Render()
        {
            base.Render();
            texture.DrawCentered(Position);
            if (stickyTop)
                stickyTextureArray[0].DrawCentered(Position, Color.White, 1, 0);
            if (stickyBottom)
                stickyTextureArray[1].DrawCentered(Position, Color.White, 1, MathHelper.Pi);
            if (stickyLeft)
                stickyTextureArray[2].DrawCentered(Position, Color.White, 1, -MathHelper.PiOver2);
            if (stickyRight)
                stickyTextureArray[3].DrawCentered(Position, Color.White, 1, MathHelper.PiOver2);
        }
    }
}
