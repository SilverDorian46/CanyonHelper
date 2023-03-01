using Microsoft.Xna.Framework;
using Monocle;
using System.Collections.Generic;
using System.Linq;

namespace Celeste.Mod.CanyonHelper
{
    public class TempleFallTrigger : Trigger
    {
        private bool triggered;
        private EntityID id;

        public TempleFallTrigger(EntityData data, Vector2 offset, EntityID entId) : base(data, offset)
        {
            triggered = false;
            id = entId;
        }

        public override void OnEnter(Player player)
        {
            if (!triggered)
            {
                triggered = true;
                //Glitch
                Audio.Play("event:/char/oshiro/boss_transform_begin", player.Position);

                Entity[] gates = Scene.Tracker.GetEntities<TempleGate>().ToArray();
                Entity[] spinners = Scene.Tracker.GetEntities<CrystalStaticSpinner>().ToArray();
                Entity[] refills = (Scene as Level).Entities.OfType<Refill>().ToArray();
                Entity[] dashBlocks = Scene.Tracker.GetEntities<DashBlock>().ToArray();

                foreach (Entity gate in gates)
                {
                    (gate as TempleGate).Close();
                }

                foreach (Entity spinner in spinners)
                {
                    CrystalStaticSpinner castSpinner = spinner as CrystalStaticSpinner;
                    if (!castSpinner.AttachToSolid)
                        castSpinner.Destroy();
                }

                foreach (Entity refill in refills)
                {
                    Level level = Scene as Level;
                    level.ParticlesFG.Emit(Refill.P_Shatter, 5, refill.Position, Vector2.One * 4f, -1.57079637f);
                    level.ParticlesFG.Emit(Refill.P_Shatter, 5, refill.Position, Vector2.One * 4f, 1.57079637f);
                    refill.RemoveSelf();
                }

                foreach (Entity dashBlock in dashBlocks)
                {
                    (dashBlock as DashBlock).Break(dashBlock.TopCenter - Vector2.UnitY, Vector2.UnitY, true, true);
                }

                (Scene as Level).CanRetry = false;
                (Scene as Level).SaveQuitDisabled = true;
            }
        }
    }
}