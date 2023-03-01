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
    class ToggleBlockNode : Entity
    {
        MTexture nodeTexture;
        MTexture nodeCrystalTexture;
        public Color color = Color.Red;
        SineWave sine;

        float positionOffset = 0;

        public ToggleBlockNode() : base()
        {
            nodeTexture = GFX.Game["objects/canyon/toggleblock/node"];
            nodeCrystalTexture = GFX.Game["objects/canyon/toggleblock/nodeCrystal"];
            Depth = 8999;
            Add(sine = new SineWave(0.6f, 0f));
            //sine.Randomize();
        }

        public override void Update()
        {
            base.Update();
            positionOffset = sine.Value - 2;
        }

        public ToggleBlockNode Init(Vector2 position)
        {
            Position = position;
            return this;
        }

        public override void Render()
        {
            nodeCrystalTexture.DrawCentered(Position + (Vector2.UnitY * positionOffset), Color.White);
            nodeTexture.DrawCentered(Position + (Vector2.UnitY * positionOffset), color);
        }
    }
}
