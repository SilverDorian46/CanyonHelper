using Microsoft.Xna.Framework;
using Monocle;

namespace Celeste.Mod.CanyonHelper
{
    class UnbreakableStone : Entity
    {
        MTexture textureIntact;
        MTexture textureBroken;

        MTexture texture;

        bool intact = false;

        public UnbreakableStone(EntityData data, Vector2 offset) : base(data.Position + offset)
        {
            textureIntact = GFX.Game["objects/canyon/pushblock/intact"];
            textureBroken = GFX.Game["objects/canyon/pushblock/broken"];
            Add(new PlayerCollider(OnPlayer, null, null));
        }
        
        void OnPlayer(Player player)
        {
            Visible = false;
            Collidable = false;
            Audio.Play("event:/game/general/wall_break_stone", Center);

            for (int x = 0; x <= 32; x += 8)
            {
                for (int y = 0; y < 32; y += 8)
                {
                    Scene.Add(Engine.Pooler.Create<Debris>().Init(TopLeft + new Vector2(x, y), '5').BlastFrom(player.Center));
                }
            }

            (Scene as Level).Session.SetFlag("unbreakableStoneBroke");

            Scene.Add(new PebbleNPC(BottomCenter));
        }

        public override void Added(Scene scene)
        {
            base.Added(scene);
            if ((scene as Level).Session.GetFlag("unbreakableStoneBroke"))
            {
                Scene.Add(new PebbleNPC(BottomCenter));
                RemoveSelf();
            }
                
            if ((Scene as Level).Session.GetCounter("pushBlocksHit") > 0)
            {
                texture = textureBroken;
                intact = false;
            }
            else
            {
                texture = textureIntact;
                intact = true;
            }

            if (intact)
            {
                Collider = new Hitbox(32, 32, -16, -32);
            }
        }

        public override void Render()
        {
            base.Render();
            texture.DrawCentered(Position + Vector2.UnitY * -16);
        }
    }
}
