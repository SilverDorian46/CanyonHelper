using System;
using System.IO;
using System.Collections.Generic;
using System.Collections;
using System.Text.RegularExpressions;
using Microsoft.Xna.Framework;
using Monocle;

namespace Celeste.Mod.CanyonHelper
{
    public class GrannyTemple : NPC
    {
        private EntityID id;
        private Hahaha Hahaha;
        GrannyLaughSfx LaughSfx;

        private string dialog;

        private Vector2 scale = new Vector2(1, 1);
        private Vector2 indicatorOffset = Vector2.Zero;

        private Coroutine talkRoutine;
        private Coroutine trappedRoutine;

        public event Action<int> OnStart;

        public GrannyTemple(EntityData data, Vector2 offset, EntityID id) : base(data.Position + offset)
        {
            this.id = id;
            dialog = "EC_A_GRANNY_04";

            Add(Sprite = GFX.SpriteBank.Create("granny"));
            Sprite.Scale.X = -1;
            Sprite.Play("idle");
            Add(LaughSfx = new GrannyLaughSfx(Sprite));
        }

        public override void Added(Scene scene)
        {
            base.Added(scene);

            scene.Add(Hahaha = new Hahaha(Position + new Vector2(8f, -4f), "", false, null));
            Hahaha.Enabled = false;

            if ((scene as Level).Session.GetFlag("DoNotTalk" + id))
                return;
        }

        public override void Update()
        {
            base.Update();

            if ((Scene as Level).Session.GetFlag("canyonTempleTriggered"))
            {
                Visible = false;
                Entity[] gates = Scene.Tracker.GetEntities<TempleGate>().ToArray();
                foreach (Entity gate in gates)
                {
                    if ((gate as TempleGate).Type == TempleGate.Types.CloseBehindPlayerAndTheo)
                    {
                        (gate as TempleGate).Close();
                    }
                }
                for (int i = 360; i <= 552; i += 16)
                {
                    Scene.Add(new CrystalStaticSpinner(new Vector2(i, 328) + (Scene as Level).LevelOffset, false, CrystalColor.Red));
                }
                Active = false;
            }

            if ((Scene as Level).Session.GetFlag("approachedTempleGranny") && !(Scene as Level).Session.GetFlag("DoNotTalkIntro" + id))
            {
                (Scene as Level).Session.SetFlag("DoNotTalkIntro" + id, true);
                (Scene as Level).Session.Flags.Remove("approachedTempleGranny");
                Player player = Scene.Tracker.GetEntity<Player>();
                if (player != null)
                {
                    (Scene as Level).Session.SetFlag("DoNotTalk" + id, true); // Sets flag to not load
                    player.StateMachine.State = Player.StDummy;
                    OnStart?.Invoke(Session.GetCounter(id + "DialogCounter"));
                    Level.StartCutscene(OnTalkEnd);
                    Add(talkRoutine = new Coroutine(Talk(player)));
                }
            }

            if ((Scene as Level).Session.GetFlag("enteredCanyonTemple") && !(Scene as Level).Session.GetFlag("DoNotTalkOutro" + id))
            {
                (Scene as Level).Session.SetFlag("DoNotTalkOutro" + id, true);
                Player player = Scene.Tracker.GetEntity<Player>();
                (Scene as Level).Session.Flags.Remove("enteredCanyonTemple");
                Level.StartCutscene(OnTrappedEnd);
                Add(trappedRoutine = new Coroutine(TrappedInTemple(player)));
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
            talkRoutine.Cancel();
            talkRoutine.RemoveSelf();
            Session.IncrementCounter(id + "DialogCounter");
        }

        private void OnTrappedEnd(Level level)
        {
            Player player = Scene.Tracker.GetEntity<Player>();
            if (player != null)
            {
                player.StateMachine.Locked = false;
                player.StateMachine.State = 0;
            }
            trappedRoutine.Cancel();
            trappedRoutine.RemoveSelf();
            Visible = false;
            Entity[] gates = Scene.Tracker.GetEntities<TempleGate>().ToArray();
            foreach (Entity gate in gates)
            {
                if ((gate as TempleGate).Type == TempleGate.Types.CloseBehindPlayerAndTheo)
                {
                    (gate as TempleGate).Close();
                }
            }
            for (int i = 360; i <= 552; i += 16)
            {
                Scene.Add(new CrystalStaticSpinner(new Vector2(i, 328) + (Scene as Level).LevelOffset, false, CrystalColor.Red));
            }
            Session session = (Scene as Level).Session;
            Vector2 newSpawnPoint = player.Position;
            session.SetFlag("canyonTempleTriggered", true);
            if (session.RespawnPoint == null || session.RespawnPoint.Value != newSpawnPoint)
            {
                session.HitCheckpoint = true;
                session.RespawnPoint = newSpawnPoint;
                session.UpdateLevelStartDashes();
            }
        }

        private IEnumerator TrappedInTemple(Player player)
        {
            player.StateMachine.State = 11;
            player.Facing = Facings.Left;
            yield return 0.6f;
            yield return Textbox.Say("EC_A_GRANNY_05", null);
            Sprite.Play("laugh", false, false);
            if (Hahaha != null)
                Hahaha.Enabled = true;
            yield return 5f;
            if (Hahaha != null)
                Hahaha.Enabled = false;
            Sprite.Play("idle", false, false);
            Sprite.Scale.X = 1;
            yield return 0.5f;
            yield return Textbox.Say("EC_A_GRANNY_06", null);
            Audio.Play("event:/new_content/game/10_farewell/lightning_strike", Position);
            Visible = false;
            RecoverBlast.Spawn(Position);
            Entity[] gates = Scene.Tracker.GetEntities<TempleGate>().ToArray();
            foreach (Entity gate in gates)
            {
                if ((gate as TempleGate).Type == TempleGate.Types.CloseBehindPlayerAndTheo)
                {
                    (gate as TempleGate).Close();
                }
            }
            Level.Flash(Color.White);
            for (int i = 360; i <= 552; i += 16)
            {
                Scene.Add(new CrystalStaticSpinner(new Vector2(i, 328) + (Scene as Level).LevelOffset, false, CrystalColor.Red));
            }
            Session session = (base.Scene as Level).Session;
            Vector2 newSpawnPoint = player.Position;
            session.SetFlag("canyonTempleTriggered", true);
            if (session.RespawnPoint == null || session.RespawnPoint.Value != newSpawnPoint)
            {
                session.HitCheckpoint = true;
                session.RespawnPoint = newSpawnPoint;
                session.UpdateLevelStartDashes();
            }
            Level.EndCutscene();
            OnTrappedEnd(Level);
        }

        private IEnumerator Talk(Player player)
        {
            yield return PlayerApproachLeftSide(player, true, 16);
            yield return Textbox.Say(dialog, null);
            Level.EndCutscene();
            OnTalkEnd(Level);
        }

        [Pooled]
        private class RecoverBlast : Entity
        {
            private Sprite sprite;

            public override void Added(Scene scene)
            {
                base.Added(scene);
                Depth = -199;
                if (sprite == null)
                {
                    Add(sprite = GFX.SpriteBank.Create("seekerShockWave"));
                    sprite.OnLastFrame = delegate (string a)
                    {
                        RemoveSelf();
                    };
                }
                sprite.Play("shockwave", true, false);
                sprite.Rate = 5;
            }

            public static void Spawn(Vector2 position)
            {
                GrannyTemple.RecoverBlast recoverBlast = Engine.Pooler.Create<GrannyTemple.RecoverBlast>();
                recoverBlast.Position = position;
                Engine.Scene.Add(recoverBlast);
            }
        }
    }
}