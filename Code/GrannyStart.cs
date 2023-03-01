using System;
using System.IO;
using System.Collections.Generic;
using System.Collections;
using System.Text.RegularExpressions;
using Microsoft.Xna.Framework;
using Monocle;

namespace Celeste.Mod.CanyonHelper
{
    public class GrannyStart : NPC
    {
        private EntityID id;

        private string dialog1;
        private string dialog2;

        private Vector2 scale = new Vector2(1, 1);
        private Vector2 indicatorOffset = Vector2.Zero;

        private Coroutine talkRoutine;

        public GrannyStart(EntityData data, Vector2 offset, EntityID id) : base(data.Position + offset)
        {
            this.id = id;
            dialog1 = "EC_A_GRANNY_01";
            dialog2 = "EC_A_GRANNY_02";

            Add(Sprite = GFX.SpriteBank.Create("granny"));
            Sprite.Scale.X = -1;
            Sprite.Play("idle");
        }

        public override void Added(Scene scene)
        {
            base.Added(scene);

            if ((scene as Level).Session.GetFlag("DoNotTalk" + id))
                RemoveSelf();
        }

        public override void Update()
        {
            base.Update();

            Player player = Scene.Tracker.GetEntity<Player>();
            if (player != null)
            {
                if ((Scene as Level).Session.GetFlag("canyonLevelStart"))
                {
                    Level.StartCutscene(OnTalkEnd);
                    if (talkRoutine == null)
                        Add(talkRoutine = new Coroutine(Talk(player)));
                }
            }
        }

        private void OnTalkEnd(Level level)
        {
            Player player = Scene.Tracker.GetEntity<Player>();
            if (player != null)
            {
                player.StateMachine.Locked = false;
                player.StateMachine.State = 0;
            }
            (Scene as Level).Session.SetFlag("canyonLevelStart", false);
            player.Position.X = (float)Math.Round(Position.X - 16);
            player.Position.Y = (float)Math.Round(Position.Y);
            (Scene as Level).Session.SetFlag("DoNotTalk" + id);
            talkRoutine.Cancel();
            talkRoutine.RemoveSelf();
        }

        private IEnumerator Talk(Player player)
        {
            yield return player.DummyWalkToExact(42 + (int)Level.LevelOffset.X);
            Audio.Play("event:/char/madeline/jump", player.BottomCenter);
            Dust.Burst(player.BottomCenter, (float)-Math.PI / 2, 4, ParticleTypes.Dust);
            player.Speed.Y = -180f;
            yield return player.DummyWalkTo((int)Position.X - 52);
            yield return Textbox.Say(dialog1, null);
            Audio.Play("event:/char/madeline/jump", player.BottomCenter);
            Dust.Burst(player.BottomCenter, (float)-Math.PI /2, 4, ParticleTypes.Dust);
            player.Speed.Y = -200f;
            yield return player.DummyWalkToExact((int)Position.X - 16, false, 1.2f);
            yield return Textbox.Say(dialog2, null);
            Level.EndCutscene();
            OnTalkEnd(Level);
        }

        private IEnumerator JumpForSec(float time, Player player)
        {
            float counter = 0;
            while (counter < time)
            {
                counter++;
                player.Jump();
                yield return null;
            }
        }
    }
}