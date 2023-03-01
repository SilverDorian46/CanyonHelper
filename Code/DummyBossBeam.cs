using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Monocle;

namespace Celeste.Mod.CanyonHelper
{
    class DummyBossBeam : Entity
    {
        public static ParticleType P_Dissipate;

        public const float ChargeTime = 1.4f;
        public const float FollowTime = 0.9f;
        public const float ActiveTime = 0.12f;

        private const float AngleStartOffset = 100f;
        private const float RotationSpeed = 200f;
        private const float CollideCheckSep = 2f;

        private const float BeamLength = 2000f;
        private const float BeamStartDist = 12f;
        private const int BeamsDrawn = 15;

        private const float SideDarknessAlpha = 0.35f;

        private Sprite beamSprite;
        private Sprite beamStartSprite;

        private float chargeTimer;
        private float followTimer;
        public float activeTimer;

        private float angle;
        private float beamAlpha;
        private float sideFadeAlpha;
        private VertexPositionColor[] fade;

        Vector2 from;
        Vector2 to;

        public DummyBossBeam(Vector2 origin, Vector2 target) : base()
        {
            fade = new VertexPositionColor[24];
            Add(beamSprite = GFX.SpriteBank.Create("badeline_beam"));
            beamSprite.OnLastFrame = delegate (string anim)
            {
                if (anim == "shoot")
                {
                    Destroy();
                }
            };
            Add(beamStartSprite = GFX.SpriteBank.Create("badeline_beam_start"));
            beamSprite.Visible = false;
            Depth = -1000000;
            from = origin;
            to = target;
        }

        public override void Added(Scene scene)
        {
            base.Added(scene);

            Init(from, to);
        }

        public DummyBossBeam Init(Vector2 start, Vector2 end)
        {
            chargeTimer = 1.4f;
            followTimer = 0.9f;
            activeTimer = 0.12f;
            beamSprite.Play("charge", false, false);
            sideFadeAlpha = 0f;
            beamAlpha = 0f;
            int num;
            if (end.Y <= start.Y + 16f)
            {
                num = 1;
            }
            else
            {
                num = -1;
            }
            if (end.X >= start.X)
            {
                num *= -1;
            }
            angle = Calc.Angle(start, end);
            Vector2 vector = Calc.ClosestPointOnLine(start, start + Calc.AngleToVector(angle, 2000f), end);
            vector += (end - start).Perpendicular().SafeNormalize(100f) * num;
            angle = Calc.Angle(start, vector);
            return this;
        }

        public override void Update()
        {
            base.Update();
            beamAlpha = Calc.Approach(beamAlpha, 1f, 2f * Engine.DeltaTime);
            if (chargeTimer > 0f)
            {
                sideFadeAlpha = Calc.Approach(sideFadeAlpha, 1f, Engine.DeltaTime);
                followTimer -= Engine.DeltaTime;
                chargeTimer -= Engine.DeltaTime;
                if (followTimer > 0f)
                {
                    Vector2 vector = Calc.ClosestPointOnLine(from, from + Calc.AngleToVector(angle, 2000f), to);
                    Vector2 center = to;
                    vector = Calc.Approach(vector, center, 200f * Engine.DeltaTime);
                    angle = Calc.Angle(from, vector);
                }
                else
                {
                    if (beamSprite.CurrentAnimationID == "charge")
                    {
                        beamSprite.Play("lock", false, false);
                    }
                }
                if (chargeTimer <= 0f)
                {
                    SceneAs<Level>().DirectionalShake(Calc.AngleToVector(angle, 1f), 0.15f);
                    Input.Rumble(RumbleStrength.Medium, RumbleLength.Medium);
                    DissipateParticles();
                }
            }
            else
            {
                if (activeTimer > 0f)
                {
                    sideFadeAlpha = Calc.Approach(sideFadeAlpha, 0f, Engine.DeltaTime * 8f);
                    if (beamSprite.CurrentAnimationID != "shoot")
                    {
                        beamSprite.Play("shoot", false, false);
                        beamStartSprite.Play("shoot", true, false);
                    }
                    activeTimer -= Engine.DeltaTime;
                }
            }
        }

        private void DissipateParticles()
        {
            Level level = SceneAs<Level>();
            Vector2 vector = level.Camera.Position + new Vector2(160f, 90f);
            Vector2 vector2 = from + Calc.AngleToVector(angle, 12f);
            Vector2 vector3 = from + Calc.AngleToVector(angle, 2000f);
            Vector2 vector4 = (vector3 - vector2).Perpendicular().SafeNormalize();
            Vector2 value = (vector3 - vector2).SafeNormalize();
            Vector2 min = -vector4 * 1f;
            Vector2 max = vector4 * 1f;
            float direction = vector4.Angle();
            float direction2 = (-vector4).Angle();
            float num = Vector2.Distance(vector, vector2) - 12f;
            vector = Calc.ClosestPointOnLine(vector2, vector3, vector);
            for (int i = 0; i < 200; i += 12)
            {
                for (int j = -1; j <= 1; j += 2)
                {
                    level.ParticlesFG.Emit(FinalBossBeam.P_Dissipate, vector + value * i + vector4 * 2f * j + Calc.Random.Range(min, max), direction);
                    level.ParticlesFG.Emit(FinalBossBeam.P_Dissipate, vector + value * i - vector4 * 2f * j + Calc.Random.Range(min, max), direction2);
                    bool flag = i != 0 && (float)i < num;
                    if (flag)
                    {
                        level.ParticlesFG.Emit(FinalBossBeam.P_Dissipate, vector - value * i + vector4 * 2f * j + Calc.Random.Range(min, max), direction);
                        level.ParticlesFG.Emit(FinalBossBeam.P_Dissipate, vector - value * i - vector4 * 2f * j + Calc.Random.Range(min, max), direction2);
                    }
                }
            }
        }

        public override void Render()
        {
            Vector2 vector = from;
            Vector2 vector2 = Calc.AngleToVector(angle, beamSprite.Width);
            beamSprite.Rotation = angle;
            beamSprite.Color = Color.White * beamAlpha;
            beamStartSprite.Rotation = angle;
            beamStartSprite.Color = Color.White * beamAlpha;
            if (beamSprite.CurrentAnimationID == "shoot")
            {
                vector += Calc.AngleToVector(angle, 8f);
            }
            for (int i = 0; i < 15; i++)
            {
                beamSprite.RenderPosition = vector;
                beamSprite.Render();
                vector += vector2;
            }
            if (beamSprite.CurrentAnimationID == "shoot")
            {
                beamStartSprite.RenderPosition = from;
                beamStartSprite.Render();
            }
            GameplayRenderer.End();
            Vector2 vector3 = vector2.SafeNormalize();
            Vector2 vector4 = vector3.Perpendicular();
            Color color = Color.Black * sideFadeAlpha * 0.35f;
            Color transparent = Color.Transparent;
            vector3 *= 4000f;
            vector4 *= 120f;
            int num = 0;
            Quad(ref num, vector, -vector3 + vector4 * 2f, vector3 + vector4 * 2f, vector3 + vector4, -vector3 + vector4, color, color);
            Quad(ref num, vector, -vector3 + vector4, vector3 + vector4, vector3, -vector3, color, transparent);
            Quad(ref num, vector, -vector3, vector3, vector3 - vector4, -vector3 - vector4, transparent, color);
            Quad(ref num, vector, -vector3 - vector4, vector3 - vector4, vector3 - vector4 * 2f, -vector3 - vector4 * 2f, color, color);
            GFX.DrawVertices<VertexPositionColor>((base.Scene as Level).Camera.Matrix, fade, fade.Length, null, null);
            GameplayRenderer.Begin();
        }

        private void Quad(ref int v, Vector2 offset, Vector2 a, Vector2 b, Vector2 c, Vector2 d, Color ab, Color cd)
        {
            fade[v].Position.X = offset.X + a.X;
            fade[v].Position.Y = offset.Y + a.Y;
            VertexPositionColor[] array = fade;
            int num = v;
            v = num + 1;
            array[num].Color = ab;
            fade[v].Position.X = offset.X + b.X;
            fade[v].Position.Y = offset.Y + b.Y;
            VertexPositionColor[] array2 = fade;
            num = v;
            v = num + 1;
            array2[num].Color = ab;
            fade[v].Position.X = offset.X + c.X;
            fade[v].Position.Y = offset.Y + c.Y;
            VertexPositionColor[] array3 = fade;
            num = v;
            v = num + 1;
            array3[num].Color = cd;
            fade[v].Position.X = offset.X + a.X;
            fade[v].Position.Y = offset.Y + a.Y;
            VertexPositionColor[] array4 = fade;
            num = v;
            v = num + 1;
            array4[num].Color = ab;
            fade[v].Position.X = offset.X + c.X;
            fade[v].Position.Y = offset.Y + c.Y;
            VertexPositionColor[] array5 = fade;
            num = v;
            v = num + 1;
            array5[num].Color = cd;
            fade[v].Position.X = offset.X + d.X;
            fade[v].Position.Y = offset.Y + d.Y;
            VertexPositionColor[] array6 = fade;
            num = v;
            v = num + 1;
            array6[num].Color = cd;
        }

        public void Destroy()
        {
            base.RemoveSelf();
        }
    }
}
