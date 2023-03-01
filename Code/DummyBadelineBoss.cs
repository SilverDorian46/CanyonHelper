using System;
using System.IO;
using System.Collections.Generic;
using System.Collections;
using System.Text.RegularExpressions;
using Microsoft.Xna.Framework;
using Monocle;

namespace Celeste.Mod.CanyonHelper
{
    class DummyBadelineBoss : Entity
    {
        Sprite sprite;

        public DummyBadelineBoss(Vector2 position) : base(position)
        {
            Depth = -8500;
        }

        public override void Added(Scene scene)
        {
            base.Added(scene);

            Add(sprite = GFX.SpriteBank.Create("badeline_boss"));
            sprite.Play("idle");
            sprite.OnFrameChange = delegate (string anim)
            {
                if (anim == "idle" && sprite.CurrentAnimationFrame == 18)
                {
                    Audio.Play("event:/char/badeline/boss_idle_air", Position);
                }
            };
        }
    }
}
