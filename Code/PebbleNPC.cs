using System;
using System.Collections;
using Microsoft.Xna.Framework;
using Monocle;

namespace Celeste.Mod.CanyonHelper
{
    public class PebbleNPC : NPC
    {
        private string dialog;

        private Vector2 scale = new Vector2(1, 1);
        private Vector2 indicatorOffset = Vector2.Zero;

        private Coroutine talkRoutine;

        public PebbleNPC(Vector2 pos) : base(pos)
        {
            dialog = "EC_A_PEBBLE";

            Add(Sprite = CanyonModule.SpriteBank.Create("pebble"));
            Sprite.Play("idle");
        }

        public override void Added(Scene scene)
        {
            base.Added(scene);

            if (!(scene as Level).Session.GetFlag("DoNotTalkPebbleCanyon"))
            {
                Add(Talker = new TalkComponent(new Rectangle(-24, -8, 48, 8), new Vector2(0f, -13f), new Action<Player>(OnTalk), null));
            }
        }

        void OnTalk(Player player)
        {
            Level.StartCutscene(OnTalkEnd);
            if (talkRoutine == null)
                Add(talkRoutine = new Coroutine(Talk(player)));
        }

        private void OnTalkEnd(Level level)
        {
            Player player = Scene.Tracker.GetEntity<Player>();
            if (player != null)
            {
                player.StateMachine.Locked = false;
                player.StateMachine.State = 0;
            }
            (Scene as Level).Session.SetFlag("DoNotTalkPebbleCanyon");
            talkRoutine.Cancel();
            talkRoutine.RemoveSelf();
            Talker.RemoveSelf();
        }

        private IEnumerator Talk(Player player)
        {
            if (player.Position.X > Position.X) //player is to the right
            {
                yield return player.DummyWalkToExact((int)Math.Round(Position.X + 16));
                player.Facing = Facings.Left;
            }
            else
            {
                yield return player.DummyWalkToExact((int)Math.Round(Position.X - 16));
                player.Facing = Facings.Right;
            }
            yield return Textbox.Say(dialog, null);
            Level.EndCutscene();
            OnTalkEnd(Level);
        }
    }
}