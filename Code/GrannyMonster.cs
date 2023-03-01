using System;
using System.IO;
using System.Collections.Generic;
using System.Collections;
using System.Text.RegularExpressions;
using Microsoft.Xna.Framework;
using Monocle;

namespace Celeste.Mod.CanyonHelper
{
    public class GrannyMonster : NPC
    {
        private EntityID id;

        private string dialog;
        private string dialogMonster;
        private string dialogBaddy;

        SoundSource laserSfx;

        private Vector2 scale = new Vector2(1, 1);
        private Vector2 indicatorOffset = Vector2.Zero;

        private Coroutine talkRoutine;
        private Coroutine monsterRoutine;

        DummyBadelineBoss badelineBoss;
        MonsterJumpscare monster;
        MonsterJumpscare[] monsters;

        Player playerEnt;

        public GrannyMonster(EntityData data, Vector2 offset, EntityID id) : base(data.Position + offset)
        {
            this.id = id;
            dialog = "EC_A_GRANNY_07";
            dialogMonster = "EC_A_GRANNY_08";
            dialogBaddy = "EC_A_BADDY_01";

            Add(Sprite = GFX.SpriteBank.Create("granny"));
            Sprite.Scale.X = -1;
            Sprite.Play("idle");
        }

        public override void Update()
        {
            base.Update();

            Player player = Scene.Tracker.GetEntity<Player>();

            if ((Scene as Level).Session.GetFlag("DontSpawnMonsterGranny"))
            {
                RemoveSelf();
            }

            if ((Scene as Level).Session.GetFlag("badelineJoinFight") && !(Scene as Level).Session.GetFlag("DoNotTalk" + id))
            {
                
                Level.StartCutscene(OnTalkEnd);
                if (talkRoutine ==  null)
                    Add(talkRoutine = new Coroutine(Talk(player)));
            }

            if ((Scene as Level).Session.GetFlag("grannyIntoMonster"))
            {
                (Scene as Level).Session.SetFlag("grannyIntoMonster", false);
                Level.StartCutscene(OnTransformEnd);
                Add(monsterRoutine = new Coroutine(GrannyIntoMonster(player)));
            }
        }

        private void OnTransformEnd(Level level)
        {
            Player player = Scene.Tracker.GetEntity<Player>();
            if (badeline != null)
                badeline.RemoveSelf();
            Session.Inventory.Dashes = 1;
            player.Dashes = 1;
            if (monster != null)
                monster.RemoveSelf();
            if (badelineBoss != null)
                badelineBoss.RemoveSelf();
            if (monsters != null)
            {
                for (int i = 0; i < monsters.Length; i++)
                {
                    if (monsters[i] != null)
                        monsters[i].RemoveSelf();
                }
            }
            Visible = false;
            if (player != null)
            {
                player.StateMachine.Locked = false;
                player.StateMachine.State = 0;
            }
            player.Position.X = 276 + Level.LevelOffset.X;
            monsterRoutine.Cancel();
            monsterRoutine.RemoveSelf();
            (Scene as Level).Session.SetFlag("DontSpawnMonsterGranny");
            level.Session.DarkRoomAlpha = 0f;
            level.Lighting.Alpha = level.Session.DarkRoomAlpha;
            Session session = (Scene as Level).Session;
            Vector2 newSpawnPoint = new Vector2(276, 152) + Level.LevelOffset;
            if (session.RespawnPoint == null || session.RespawnPoint.Value != newSpawnPoint)
            {
                session.HitCheckpoint = true;
                session.RespawnPoint = newSpawnPoint;
                session.UpdateLevelStartDashes();
            }
        }

        private IEnumerator GrannyIntoMonster(Player player)
        {
            yield return player.DummyWalkTo(276 + Level.LevelOffset.X);
            Audio.Play("event:/char/badeline/maddy_split");
            player.CreateSplitParticles();
            Input.Rumble(RumbleStrength.Light, RumbleLength.Medium);
            Level.Displacement.AddBurst(player.Position, 0.4f, 8f, 32f, 0.5f, null, null);
            player.Dashes = 1;
            Session.Inventory.Dashes = 1;
            Session session = (Scene as Level).Session;
            Vector2 newSpawnPoint = new Vector2(276, 152) + Level.LevelOffset;
            if (session.RespawnPoint == null || session.RespawnPoint.Value != newSpawnPoint)
            {
                session.HitCheckpoint = true;
                session.RespawnPoint = newSpawnPoint;
                session.UpdateLevelStartDashes();
            }
            Scene.Add(badeline = new BadelineDummy(player.Center));
            badeline.Appear(Scene as Level, true);
            badeline.FloatSpeed = 80f;
            badeline.Sprite.Scale.X = 1;
            yield return badeline.FloatTo(player.Position + new Vector2(0, -24));
            badeline.AutoAnimator.Enabled = false;
            yield return 0.5f;
            yield return Textbox.Say(dialogMonster, null);
            RecoverBlast.Spawn(Position);
            Visible = false;
            Audio.Play("event:/new_content/game/10_farewell/lightning_strike", Position);
            monster = MonsterJumpscare.Spawn(Position);
            yield return 2f;
            yield return Textbox.Say(dialogBaddy, new Func<IEnumerator>[]
            {
                new Func<IEnumerator>(BadelineTransform),
                new Func<IEnumerator>(BadelineLaser),
                new Func<IEnumerator>(BadelineShoot),
                new Func<IEnumerator>(BadelineDisappear),
                new Func<IEnumerator>(MonsterClones),
                new Func<IEnumerator>(ClonesDie),
                new Func<IEnumerator>(BadelineAppear),
                new Func<IEnumerator>(MonsterShrink),
                new Func<IEnumerator>(MonsterDisappear),
                new Func<IEnumerator>(BadelineTurn),
                new Func<IEnumerator>(BadelineDisappear)
            });
            Level.EndCutscene();
            OnTransformEnd(Level);
        }

        IEnumerator MonsterShrink()
        {
            Sprite monsterSprite = monster.Get<Sprite>();
            while (monsterSprite.Scale.X > 0.2f)
            {
                monsterSprite.Scale -= Vector2.One * 0.05f;
                yield return 0.05f;
            }
        }

        IEnumerator BadelineTurn()
        {
            badeline.Sprite.Scale.X = -1;
            yield break;
        }

        IEnumerator MonsterDisappear()
        {
            RecoverBlast.Spawn(monster.Position);
            Audio.Play("event:/new_content/game/10_farewell/lightning_strike", monster.Position);
            monster.Visible = false;
            yield return 1;
        }

        IEnumerator Split()
        {
            Player player = Scene.Tracker.GetEntity<Player>();
            Sprite boss = badelineBoss.Get<Sprite>();
            for (int i = 0; i < 10; i++)
            {
                boss.Scale = new Vector2(boss.Scale.X - 0.03f, boss.Scale.Y - 0.03f);
                yield return 0.1f;
            }
            player.Visible = true;
            player.Position = badelineBoss.Position;
            player.CreateSplitParticles();
            (Scene as Level).Session.Inventory.Dashes = 1;
            player.Dashes = 1;
            Scene.Add(badeline = new BadelineDummy(badelineBoss.Center));
            badeline.AutoAnimator.Enabled = false;
            badeline.Sprite.Scale.X = -1;
            badelineBoss.RemoveSelf();
            Vector2 posToGo = badeline.Position + new Vector2(16, -8);
            yield return badeline.FloatTo(posToGo);
            badeline.Sprite.Scale.X = -1;
        }

        IEnumerator MadelineBoss()
        {
            Player player = Scene.Tracker.GetEntity<Player>();
            player.Visible = false;
            Audio.Play("event:/char/badeline/disappear");
            player.CreateSplitParticles();
            yield return 0.25f;
            Sprite boss = badelineBoss.Get<Sprite>();
            for (int i = 0; i < 10; i++)
            {
                boss.Scale = new Vector2(boss.Scale.X + 0.03f, boss.Scale.Y + 0.03f);
                yield return 0.1f;
            }
        }

        IEnumerator ClonesDie()
        {
            Player player = Scene.Tracker.GetEntity<Player>();
            DeathEffect[] effects = new DeathEffect[4];
            for (int i = 3; i >= 0; i--)
            {
                Audio.Play("event:/game/05_mirror_temple/seeker_death", monsters[i].Position);
                Entity ent = new Entity(monsters[i].Position + new Vector2(-4, -30));
                effects[i] = new DeathEffect(Color.HotPink, new Vector2?(monsters[i].Center - monsters[i].Position));
                effects[i].OnEnd = delegate ()
                {
                    ent.RemoveSelf();
                };
                ent.Add(effects[i]);
                ent.Depth = -1000000;
                Scene.Add(ent);
                monsters[i].RemoveSelf();
                yield return 0.2f;
            }
            Coroutine coroutine = new Coroutine(BrightenFully(), true);
            Add(coroutine);
            yield return 1.5f;
        }

        IEnumerator BadelineAppear()
        {
            Player player = Scene.Tracker.GetEntity<Player>();
            Audio.Play("event:/char/badeline/appear");
            player.CreateSplitParticles();
            Input.Rumble(RumbleStrength.Light, RumbleLength.Medium);
            Level.Displacement.AddBurst(player.Center + new Vector2(0, -16), 0.4f, 8f, 32f, 0.5f, null, null);
            Scene.Add(badeline = new BadelineDummy(player.Center));
            badeline.Appear(Scene as Level, true);
            badeline.FloatSpeed = 80f;
            yield return badeline.FloatTo(player.Position + new Vector2(8, -16));
            badeline.Sprite.Scale.X = 1;
            badeline.AutoAnimator.Enabled = false;
            yield return 0.5f;
        }

        private IEnumerator BrightenFully()
        {
            yield return 0.3f;
            Level.Session.DarkRoomAlpha = 0f;
            float darkness = Level.Session.DarkRoomAlpha;
            while (Level.Lighting.Alpha != darkness)
            {
                Level.Lighting.Alpha = Calc.Approach(Level.Lighting.Alpha, darkness, Engine.DeltaTime * 0.5f);
                yield return null;
            }
            yield break;
        }

        private IEnumerator BadelineTransform()
        {
            Level.Flash(Color.White);
            Scene.Add(badelineBoss = new DummyBadelineBoss(badeline.Position));
            badeline.RemoveSelf();
            yield return null;
        }

        private IEnumerator MonsterClones()
        {
            Player player = Scene.Tracker.GetEntity<Player>();
            monsters = new MonsterJumpscare[4];
            float distance = (monster.Position - player.Position).Length();
            float angle = (float)Math.PI;
            for (int i = 0; i < 4; i++)
            {
                angle -= (float)(Math.PI / 4);
                monsters[i] = MonsterJumpscare.Spawn(player.Center + Calc.AngleToVector((float)Math.PI + angle, distance * 2), i > 1 ? -1 : 1);
                Audio.Play("event:/new_content/game/10_farewell/lightning_strike", monsters[i].Position);
                yield return 0.25f;
            }
        }

        private IEnumerator BadelineDisappear()
        {
            badeline.Vanish();
            badeline.Visible = false;
            yield return 1.5f;
        }

        private IEnumerator BadelineLaser()
        {
            badelineBoss.Add(laserSfx = new SoundSource());
            laserSfx.Position = badelineBoss.Position;
            laserSfx.Play("event:/char/badeline/boss_laser_charge", null, 0f);
            badelineBoss.Get<Sprite>().Play("attack2Begin", true, false);
            yield return null;
        }

        private IEnumerator BadelineShoot()
        {
            Player player = Scene.Tracker.GetEntity<Player>();
            DummyBossBeam beam;
            Scene.Add(beam = new DummyBossBeam(badelineBoss.TopCenter + new Vector2(0, -19), Center + new Vector2(0, -18)));
            yield return 0.9f;
            badelineBoss.Get<Sprite>().Play("attack2Lock", true, false);
            yield return 0.5f;
            Level.Flash(Color.White, false);
            Sprite mSprite = monster.Get<Sprite>();
            mSprite.Color = Color.Red;
            mSprite.Scale = new Vector2(mSprite.Scale.X, 1) * 1.1f;
            yield return 0.1f;
            monster.Get<Sprite>().Color = Color.White;
            mSprite.Scale = new Vector2(Math.Sign(mSprite.Scale.X), 1);
            Audio.Play("event:/char/badeline/boss_laser_fire", badelineBoss.Position);
            badelineBoss.Get<Sprite>().Play("attack2Recoil", false, false);
            yield return 0.5f;
            yield return 2;
            Scene.Add(badeline = new BadelineDummy(badelineBoss.Center));
            badeline.AutoAnimator.Enabled = false;
            badeline.Sprite.Scale.X = 1f;
            badelineBoss.RemoveSelf();
            yield return 1;
        }

        private IEnumerator Brighten()
        {
            yield return 0.3f;
            Level.Session.DarkRoomAlpha = 0.3f;
            float darkness = Level.Session.DarkRoomAlpha;
            while (Level.Lighting.Alpha != darkness)
            {
                Level.Lighting.Alpha = Calc.Approach(Level.Lighting.Alpha, darkness, Engine.DeltaTime * 0.5f);
                yield return null;
            }
            yield break;
        }

        private IEnumerator Talk(Player player)
        {
            playerEnt = player;
            yield return player.DummyWalkTo(36 + Level.LevelOffset.X);
            yield return Textbox.Say(dialog, new Func<IEnumerator>[]
                {
                    new Func<IEnumerator>(BadelineShowUp),
                    new Func<IEnumerator>(BadelineJoin)
                });
            Level.EndCutscene();
            OnTalkEnd(Level);
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
            (Scene as Level).Session.SetFlag("DoNotTalk" + id);
            player.Position.X = 36 + Level.LevelOffset.X;
            level.Flash(Color.White, false);
            level.Session.Inventory.Dashes = 2;
            level.Session.DarkRoomAlpha = 0.3f;
            level.Lighting.Alpha = level.Session.DarkRoomAlpha;
            if (badeline != null)
                badeline.RemoveSelf();
        }

        BadelineDummy badeline;

        IEnumerator BadelineShowUp()
        {
            Audio.Play("event:/char/badeline/appear");
            playerEnt.CreateSplitParticles();
            Input.Rumble(RumbleStrength.Light, RumbleLength.Medium);
            Level.Displacement.AddBurst(playerEnt.Center + new Vector2(0, -16), 0.4f, 8f, 32f, 0.5f, null, null);
            playerEnt.Dashes = 1;
            Scene.Add(badeline = new BadelineDummy(playerEnt.Center));
            badeline.Appear(Scene as Level, true);
            badeline.FloatSpeed = 80f;
            Coroutine coroutine = new Coroutine(Brighten(), true);
            Add(coroutine);
            yield return badeline.FloatTo(new Vector2(48, 96) + Level.LevelOffset);
            badeline.Sprite.Scale.X = -1;
            badeline.AutoAnimator.Enabled = false;
            yield return 0.5f;
        }

        IEnumerator BadelineJoin()
        {
            badeline.FloatSpeed = 240f;
            yield return badeline.FloatTo(playerEnt.Center);
            Audio.Play("event:/char/badeline/maddy_join");
            badeline.RemoveSelf();
            (Scene as Level).Flash(Color.White, false);
            (Scene as Level).Session.Inventory.Dashes = 2;
            playerEnt.CreateSplitParticles();
            Input.Rumble(RumbleStrength.Light, RumbleLength.Medium);
            Level.Displacement.AddBurst(playerEnt.Position, 0.4f, 8f, 32f, 0.5f, null, null);
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
                RecoverBlast recoverBlast = Engine.Pooler.Create<RecoverBlast>();
                recoverBlast.Position = position;
                Engine.Scene.Add(recoverBlast);
            }
        }

        [Pooled]
        private class DummyTheoCrystal : Entity
        {
            private Sprite sprite;
            private SineWave sine;
            private Vector2 initPos;

            public override void Added(Scene scene)
            {
                base.Added(scene);
                Depth = 100;
                Add(sprite = GFX.SpriteBank.Create("theo_crystal"));
                Add(sine = new SineWave(0.6f, 0f));
                sprite.Scale.X = -1f;
                initPos = Position;
            }

            public static DummyTheoCrystal Spawn(Vector2 position)
            {
                DummyTheoCrystal crystal = Engine.Pooler.Create<DummyTheoCrystal>();
                crystal.Position = position;
                Engine.Scene.Add(crystal);
                return crystal;
            }

            public override void Update()
            {
                base.Update();
                Vector2 positionOffset = new Vector2(0, sine.Value - 2);
                Position = initPos + positionOffset;
            }
        }

        [Pooled]
        private class MonsterJumpscare : Entity
        {
            private Sprite sprite;
            SineWave sine;
            Vector2 originalPos;
            int dirToFace = 1;

            public override void Added(Scene scene)
            {
                base.Added(scene);
                Depth = -199;
                if (sprite == null)
                {
                    Add(sprite = CanyonModule.SpriteBank.Create("monster"));
                }
                sprite.Play("idle", true, false);
                sprite.Rate = 5;
                sprite.Scale.X = dirToFace;
                Add(sine = new SineWave(0.6f, 0f));
                originalPos = Position;
            }

            public override void Update()
            {
                base.Update();
                Position = originalPos + Vector2.UnitY * sine.Value;
            }

            public static MonsterJumpscare Spawn(Vector2 position, int dir = 1)
            {
                MonsterJumpscare jumpscare = Engine.Pooler.Create<MonsterJumpscare>();
                jumpscare.Position = position;
                jumpscare.dirToFace = dir;
                Engine.Scene.Add(jumpscare);
                return jumpscare;
            }
        }
    }
}