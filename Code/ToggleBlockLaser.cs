using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using Monocle;
using FMOD.Studio;
using System.Collections;
using Microsoft.Xna.Framework.Graphics;

namespace Celeste.Mod.CanyonHelper
{
    [Pooled]
    [Tracked(false)]
    class ToggleBlockLaser : Entity
    {
        VertexPositionColor[] fade;
        MTexture laserSprite;
        float angle;
        public Color color;

        public Vector2 start;
        public Vector2 end;

        float timer;
        float sineTimer = 0;

        public ToggleBlockLaser() : base()
        {
            timer = 0;
            fade = new VertexPositionColor[24];
            laserSprite = GFX.Game["objects/canyon/toggleblock/laser"];
            Depth = 8999;
            timer = Calc.Random.NextFloat();
        }

        public ToggleBlockLaser Init(Vector2 currentNode, Vector2 nextNode)
        {
            start = currentNode;
            end = nextNode;
            angle = Calc.Angle(currentNode, nextNode);
            return this;
        }

        public override void Update()
        {
            base.Update();
            timer += Engine.DeltaTime * 4f;
            sineTimer += Engine.DeltaTime * 12f;
        }

        public override void Render()
        {
            float scale = 0.5f * (0.5f + ((float)Math.Sin(timer) + 1f) * 0.25f);
            //Draw.TextureBannerV(laserSprite, start, Vector2.Zero, new Vector2(Vector2.Distance(start, end) / 128f, 1), angle, color * scale, SpriteEffects.None, 5, 2, 1);
            Draw.SineTextureH(laserSprite, start, Vector2.Zero, new Vector2(Vector2.Distance(start, end) / 128f, 1.5f), angle, color * scale, SpriteEffects.None, sineTimer, 1, 1, 0.04f);
        }
    }
}
