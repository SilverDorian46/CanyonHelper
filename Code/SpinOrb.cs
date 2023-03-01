using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using Monocle;
using FMOD.Studio;

namespace Celeste.Mod.CanyonHelper
{
    class SpinOrb : Entity
    {
        private DashListener dashListener;
        private EventInstance moveSfx;
        private bool playerInOrb = false;
        private VertexLight light;
        private Sprite sprite;
        private Entity outline;

        private const float normalRotateSpeed = MathHelper.Pi * 1.5f;
        private const float fastRotateSpeed = MathHelper.Pi * 3f;
        private const float slowRotateSpeed = MathHelper.Pi * 0.75f;
        private float currentRotateSpeed = normalRotateSpeed;
        private const float rotateRadius = 12f;
        private const float launchSpeed = 350f;
        private const float maxUseDelay = 1.5f;
        private const float rotationOffset = MathHelper.PiOver2;

        List<Debris> debris = new List<Debris>();
        private bool debrisShaken = false;
        private bool debrisReturned = false; //Size 10 & fix stars not spawning in correct area

        private Vector2 pivotPoint;
        private float currentRotateAngle = -MathHelper.Pi / 2;
        private float useDelay = 0;

        private Player playerEntity;

        private BirdTutorialGui tutorialGui;
        bool shouldShowTutorial = false;

        ParticleType SpinOrbGlowParticles = new ParticleType
        {
            LifeMin = 0.4f,
            LifeMax = 0.6f,
            Size = 1f,
            SizeRange = 0f,
            DirectionRange = 6.28318548f,
            SpeedMin = 12f,
            SpeedMax = 18f,
            FadeMode = ParticleType.FadeModes.Late,
            Color = Calc.HexToColor("c16de0"),
            Color2 = Calc.HexToColor("bc5ce0"),
            ColorMode = ParticleType.ColorModes.Blink
        };

        public SpinOrb(EntityData data, Vector2 offset) : base(data.Position + offset)
        {
            Depth = -8500;
            Collider = new Circle(8.5f, -0.5f, 0f);
            Add(new PlayerCollider(OnPlayer, null, null));
            Add(light = new VertexLight(Color.White, 1f, 16, 32));
            Add(sprite = CanyonModule.SpriteBank.Create("spinorb"));
            Add(dashListener = new DashListener());
            dashListener.OnDash = OnPlayerDashed;
        }

        public override void Added(Scene scene)
        {
            base.Added(scene);
            pivotPoint = Position;
            //outline
            Image image = new Image(GFX.Game["objects/canyon/spinorb/outline"]);
            image.CenterOrigin();
            image.Color = Color.White * 0.75f;
            outline = new Entity(Position);
            outline.Depth = 8999;
            outline.Visible = false;
            outline.Add(image);
            scene.Add(outline);
        }

        public override void Awake(Scene scene)
        {
            base.Awake(scene);
            if ((Scene as Level).Session.Level == "a-09")
            {
                tutorialGui = new BirdTutorialGui(this, new Vector2(0f, -24f), Dialog.Clean("ec_tutorial_speed", null), new object[]
                {
                    Dialog.Clean("tutorial_hold", null),
                    Input.Grab,
                    Dialog.Clean("tutorial_hold", null),
                    Input.Jump
                });
                tutorialGui.Open = false;
                shouldShowTutorial = false;
                Scene.Add(tutorialGui);
            }
        }

        private void OnPlayer(Player player)
        {
            if (!playerInOrb && useDelay <= 0)
            {
                shouldShowTutorial = true;
                sprite.Play("playerenter", false);
                Audio.Play("event:/char/badeline/booster_begin", Position);
                moveSfx = Audio.Play("event:/game/04_cliffside/arrowblock_move", Center);
                Level level = Scene as Level;
                level.Displacement.AddBurst(Center, 0.35f, 8f, 48f, 0.25f, null, null);
                level.Particles.Emit(Player.P_Split, 16, Center, Vector2.One * 6f);
                useDelay = maxUseDelay;
                playerInOrb = true;
                player.Speed = Vector2.Zero;
                player.RefillDash();
                player.RefillStamina();
                currentRotateAngle = -rotationOffset;//in radians
                playerEntity = player;
            }
        }

        public override void Update()
        {
            base.Update();
            Level level = Scene as Level;
            if (playerEntity != null)
            {
                if (playerEntity.Dead)
                {
                    Audio.Stop(moveSfx);
                }
                if (tutorialGui != null)
                {
                    if (shouldShowTutorial)
                    {
                        tutorialGui.Open = ((playerEntity.Position - this.Position).Length() < 64);
                    }
                    else
                    {
                        tutorialGui.Open = false;
                    }
                }
            }
            if (playerInOrb)
            {
                if (playerEntity != null)
                {
                    Vector2 targetPos = Position + Vector2.UnitY * 6.5f;
                    targetPos = new Vector2((float)Math.Round(targetPos.X), (float)Math.Round(targetPos.Y)); //round to prevent garbage collision
                    playerEntity.Position = targetPos;
                    playerEntity.Visible = false;
                    playerEntity.Speed = Vector2.Zero;
                }
                if (Input.Grab.Check && !Input.Jump.Check)
                {
                    currentRotateSpeed = slowRotateSpeed;
                }
                else if (Input.Jump.Check && !Input.Grab.Check)
                {
                    currentRotateSpeed = fastRotateSpeed;
                }
                else if (Input.Jump.Check && Input.Grab.Check)
                {
                    currentRotateSpeed = 0;
                }
                else
                {
                    currentRotateSpeed = normalRotateSpeed;
                }
                sprite.Rotation = currentRotateAngle + rotationOffset;
                currentRotateAngle += currentRotateSpeed * Engine.DeltaTime;
            }
            else
            {
                if (useDelay > 0)
                {
                    useDelay -= Engine.DeltaTime;
                    outline.Visible = true;
                    if (useDelay < 0.8f && !debrisShaken)
                    {
                        debrisShaken = true;
                        EventInstance sound = Audio.Play("event:/game/04_cliffside/arrowblock_reform_begin", debris[0].Position);
                        foreach (Debris debrie in debris)
                        {
                            debrie.StartShaking();
                        }
                    }
                    if (useDelay < 0.65f && !debrisReturned)
                    {
                        debrisReturned = true;
                        foreach (Debris debrie in debris)
                        {
                            debrie.ReturnHome(0.5f);
                        }
                    }
                    if (useDelay < 0.15f)
                    {
                        outline.Visible = false;
                        Visible = true;
                        sprite.Scale = Vector2.One * ((0.15f - useDelay) / 0.15f);
                    }
                }
                else if (useDelay < 0)
                {
                    useDelay = 0;
                    Collidable = true;
                    level.Displacement.AddBurst(Center, 0.35f, 8f, 48f, 0.25f, null, null);
                    level.Particles.Emit(Player.P_Split, 32, Center, Vector2.One * 2f);
                    Audio.Play("event:/game/04_cliffside/greenbooster_reappear", Position);
                    foreach (Debris debrie in debris)
                    {
                        debrie.RemoveSelf();
                    }
                    debris = new List<Debris>();
                }
                else
                {
                    outline.Visible = false;
                    if (Scene.OnInterval(0.1f))
                    {
                        level.ParticlesFG.Emit(SpinOrbGlowParticles, 1, Position, Vector2.One * 5f);
                        //level.ParticlesBG.Emit(BadelineBoost.P_Ambience, 1, Center, Vector2.One);
                    }
                }
            }
        }

        private void OnPlayerDashed(Vector2 direction)
        {
            if (playerInOrb)
            {
                shouldShowTutorial = false;
                sprite.Play("idle", true);
                playerInOrb = false;
                if (playerEntity != null)
                {
                    playerEntity.StateMachine.State = Player.StDummy;
                    playerEntity.Visible = true;
                    playerEntity.Speed = Calc.AngleToVector(currentRotateAngle, launchSpeed);
                    playerEntity.StateMachine.State = Player.StNormal;
                    playerEntity.RefillDash();
                    playerEntity.RefillStamina();
                }
                Audio.Stop(moveSfx);
                Audio.Play("event:/char/badeline/booster_throw", Position);
                Level level = Scene as Level;
                level.Displacement.AddBurst(Center, 0.35f, 8f, 48f, 0.25f, null, null);
                level.Particles.Emit(Player.P_Split, 96, Center, Vector2.One * 12f);
                for (int i = -4; i < 5; i++)
                {
                    Random random = new Random();
                    StarBurst starBurst = Engine.Pooler.Create<StarBurst>().Init(Center, Calc.AngleToVector(currentRotateAngle + i * MathHelper.Pi / 72, 1f));
                    Scene.Add(starBurst);
                }
                currentRotateAngle = -rotationOffset;
                sprite.Rotation = 0;
                Visible = false;
                Collidable = false;
                BreakParticles();
            }
        }

        private void BreakParticles()
        {
            debrisShaken = false;
            debrisReturned = false;
            Vector2 center = base.Center;
            Random random = new Random();
            SceneAs<Level>().Particles.Emit(MoveBlock.P_Break, 6, center, Vector2.One * 8f, random.Range(0f, MathHelper.TwoPi));
            
            for (int i = 0; i < 4; i++)
            {
                Vector2 offset = new Vector2(random.Range(-4f,4f), random.Range(-4f,4f));
                offset.Normalize();
                Debris d = Engine.Pooler.Create<Debris>().Init(Position + (offset / 2), Center, Position + offset);
                debris.Add(d);
                Scene.Add(d);
                d = null;
            }
            
        }

        [Pooled]
        private class StarBurst : Actor
        {
            private Image sprite;
            private Vector2 speed;
            private float spin;
            private float alpha;

            public StarBurst() : base(Vector2.Zero)
            {
                Add(sprite = new Image(Calc.Random.Choose(GFX.Game.GetAtlasSubtextures("objects/canyon/spinorb/starBurst"))));
                sprite.CenterOrigin();
                sprite.FlipX = Calc.Random.Chance(0.5f);
            }

            public StarBurst Init(Vector2 position, Vector2 direction)
            {
                Depth = -8499;
                Position = position;
                direction.Normalize();
                speed = direction * Calc.Random.Range(90f, 120f);
                sprite.Position = Vector2.Zero;
                sprite.Rotation = Calc.Random.NextAngle();
                sprite.Color = Color.White;
                alpha = 1;
                spin = Calc.Random.Range(3.49065852f, 10.4719753f) * Calc.Random.Choose(1, -1);
                return this;
            }

            protected override void OnSquish(CollisionData data)
            {
            }

            public override void Update()
            {
                base.Update();
                sprite.Rotation += spin * Engine.DeltaTime;
                Position += speed * Engine.DeltaTime;
                alpha -= Engine.DeltaTime * 2;
                sprite.Scale.X = 1f;
                sprite.Scale.Y = 1f;
                sprite.Color = Color.White * alpha;
                if (alpha <= 0)
                {
                    RemoveSelf();
                }
            }
        }

        [Pooled]
        private class Debris : Actor
        {
            private Image sprite;
            private Vector2 home;
            private Vector2 speed;

            private bool shaking;
            private bool returning;

            private float returnEase;
            private float returnDuration;
            private SimpleCurve returnCurve;

            private bool firstHit;

            private float alpha;

            private Collision onCollideH;
            private Collision onCollideV;
            private float spin;

            public Debris() : base(Vector2.Zero)
            {
                Tag = Tags.TransitionUpdate;
                Collider = new Hitbox(4f, 4f, -2f, -2f);
                Add(sprite = new Image(Calc.Random.Choose(GFX.Game.GetAtlasSubtextures("objects/canyon/spinorb/debris"))));
                sprite.CenterOrigin();
                sprite.FlipX = Calc.Random.Chance(0.5f);
                onCollideH = delegate (CollisionData c)
                {
                    speed.X = -speed.X * 0.5f;
                };
                onCollideV = delegate (CollisionData c)
                {
                    if (firstHit || speed.Y > 50f)
                    {
                        Audio.Play("event:/game/general/debris_stone", Position, "debris_velocity", Calc.ClampedMap(speed.Y, 0f, 600f, 0f, 1f));
                    }
                    if (speed.Y > 0f && speed.Y < 40f)
                    {
                        speed.Y = 0f;
                    }
                    else
                    {
                        speed.Y = -speed.Y * 0.25f;
                    }
                    firstHit = false;
                };
            }

            protected override void OnSquish(CollisionData data)
            {
            }

            public Debris Init(Vector2 position, Vector2 center, Vector2 returnTo)
            {
                Collidable = true;
                Position = position;
                speed = (position - center).SafeNormalize(60f + Calc.Random.NextFloat(60f));
                home = returnTo;
                sprite.Position = Vector2.Zero;
                sprite.Rotation = Calc.Random.NextAngle();
                returning = false;
                shaking = false;
                sprite.Scale.X = 1f;
                sprite.Scale.Y = 1f;
                sprite.Color = Color.White;
                alpha = 1f;
                firstHit = false;
                spin = Calc.Random.Range(3.49065852f, 10.4719753f) * Calc.Random.Choose(1, -1);
                return this;
            }

            public override void Update()
            {
                base.Update();
                if (!returning)
                {
                    if (Collidable)
                    {
                        speed.X = Calc.Approach(speed.X, 0f, Engine.DeltaTime * 100f);
                        if (!OnGround(1))
                        {
                            speed.Y = speed.Y + 400f * Engine.DeltaTime;
                        }
                        MoveH(speed.X * Engine.DeltaTime, onCollideH, null);
                        MoveV(speed.Y * Engine.DeltaTime, onCollideV, null);
                    }
                    if (shaking && Scene.OnInterval(0.05f))
                    {
                        sprite.X = (-1 + Calc.Random.Next(3));
                        sprite.Y = (-1 + Calc.Random.Next(3));
                    }
                }
                else
                {
                    Position = returnCurve.GetPoint(Ease.CubeOut(returnEase));
                    returnEase = Calc.Approach(returnEase, 1f, Engine.DeltaTime / returnDuration);
                    sprite.Scale = Vector2.One * (1f + returnEase * 0.5f);
                }
                if ((Scene as Level).Transitioning)
                {
                    alpha = Calc.Approach(alpha, 0f, Engine.DeltaTime * 4f);
                    sprite.Color = Color.White * alpha;
                }
                sprite.Rotation += spin * Calc.ClampedMap(Math.Abs(speed.Y), 50f, 150f, 0f, 1f) * Engine.DeltaTime;
            }

            public void StopMoving()
            {
                Collidable = false;
            }

            public void StartShaking()
            {
                shaking = true;
            }

            public void ReturnHome(float duration)
            {
                if (Scene != null)
                {
                    Camera camera = (base.Scene as Level).Camera;
                    if (X < camera.X)
                    {
                        X = camera.X - 8f;
                    }
                    if (Y < camera.Y)
                    {
                        Y = camera.Y - 8f;
                    }
                    if (X > camera.X + 320f)
                    {
                        X = camera.X + 320f + 8f;
                    }
                    if (Y > camera.Y + 180f)
                    {
                        Y = camera.Y + 180f + 8f;
                    }
                }
                returning = true;
                returnEase = 0f;
                returnDuration = duration;
                Vector2 vector = (home - Position).SafeNormalize();
                Vector2 control = (Position + home) / 2f + new Vector2(vector.Y, -vector.X) * (Calc.Random.NextFloat(16f) + 16f) * Calc.Random.Facing();
                returnCurve = new SimpleCurve(Position, home, control);
            }
        }
    }
}