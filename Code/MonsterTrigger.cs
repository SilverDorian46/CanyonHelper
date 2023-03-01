using Microsoft.Xna.Framework;
using Monocle;
using System.Collections;

namespace Celeste.Mod.CanyonHelper
{
    public class MonsterTrigger : Trigger
    {
        private bool triggered;
        private EntityID id;
        private Vector2 posToSpawn;
        private string animationName;
        private bool faceRight;

        MonsterJumpscare monster;

        public MonsterTrigger(EntityData data, Vector2 offset, EntityID entId) : base(data, offset)
        {
            triggered = false;
            id = entId;

            animationName = data.Attr("animationName", "idle");
            posToSpawn.X = data.Int("spawnPosX");
            posToSpawn.Y = data.Int("spawnPosY");
            faceRight = data.Bool("faceRight");
        }

        public override void OnEnter(Player player)
        {
            if (!triggered && !(Scene as Level).Session.GetFlag("DoNotLoad" + id))
            {
                triggered = true;
                (Scene as Level).Session.SetFlag("DoNotLoad" + id, true); //Sets flag to not load
                monster = MonsterJumpscare.Spawn(posToSpawn + (Scene as Level).LevelOffset, animationName, faceRight);
                //Glitch
                Audio.Play("event:/char/oshiro/boss_transform_begin", player.Position);
                Tween tween = Tween.Create(Tween.TweenMode.Oneshot, null, 0.3f, true);
                tween.OnUpdate = delegate (Tween t)
                {
                    Glitch.Value = 0.3f * t.Eased;
                };
                player.Add(tween);
                tween = null;
            }
        }

        [Pooled]
        private class MonsterJumpscare : Entity
        {
            private Sprite sprite;
            private string animationName;
            private bool shouldFaceRight;

            public override void Added(Scene scene)
            {
                base.Added(scene);
                Depth = -199;
                if (sprite == null)
                    Add(sprite = CanyonModule.SpriteBank.Create("monster"));
                sprite.Play(animationName);
                sprite.FlipX = shouldFaceRight;
                Player player = Scene.Tracker.GetEntity<Player>();
                player.Add(new Coroutine(Despawn()));
            }

            private IEnumerator Despawn()
            {
                yield return 0.5f;
                if (Scene != null)
                {
                    Player player = Scene.Tracker.GetEntity<Player>();
                    Tween tweenOut = Tween.Create(Tween.TweenMode.Oneshot, null, 0.3f, true);
                    tweenOut.OnUpdate = delegate (Tween t)
                    {
                        Audio.Play("event:/char/granny/laugh_firstphrase", player.Position);
                        Glitch.Value = 0.3f * (1f - t.Eased);
                    };
                    if (player != null)
                    {
                        player.Add(tweenOut);
                        tweenOut = null;
                    }
                    RemoveSelf();
                }
                else
                {
                    Glitch.Value = 0;
                }
            }

            public static MonsterJumpscare Spawn(Vector2 position, string animName, bool faceRight = false)
            {
                MonsterJumpscare jumpscare = Engine.Pooler.Create<MonsterJumpscare>();
                jumpscare.Position = position;
                Engine.Scene.Add(jumpscare);
                jumpscare.shouldFaceRight = faceRight;
                jumpscare.animationName = animName;
                return jumpscare;
            }
        }
    }
}