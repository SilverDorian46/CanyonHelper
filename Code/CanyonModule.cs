using System;
using Microsoft.Xna.Framework;
using Monocle;

namespace Celeste.Mod.CanyonHelper
{
    public class CanyonModule : EverestModule
    {
        public override Type SettingsType
        {
            get
            {
                return null;
            }
        }

        public static SpriteBank SpriteBank;
        private int pushblockCounter = 0;

        public override void Load()
        {
            Everest.Events.Level.OnLoadEntity += this.OnLoadEntity;
            Everest.Events.Level.OnLoadLevel += Level_OnLoadLevel;
            Everest.Events.Player.OnDie += Player_Die;
            Everest.Events.Player.OnSpawn += Player_Spawn;
        }

        public override void LoadContent(bool firstLoad)
        {
            CanyonModule.SpriteBank = new SpriteBank(GFX.Game, "Graphics/CanyonSprites.xml");
        }

        public override void Unload()
        {
            Everest.Events.Level.OnLoadEntity -= this.OnLoadEntity;
            Everest.Events.Level.OnLoadLevel -= Level_OnLoadLevel;
            Everest.Events.Player.OnDie -= Player_Die;
            Everest.Events.Player.OnSpawn -= Player_Spawn;
        }

        private void Player_Die(Player player)
        {
            (player.Scene as Level).Session.SetCounter("pushBlocksHit", pushblockCounter);
        }

        private void Player_Spawn(Player player)
        {
            pushblockCounter = (player.Scene as Level).Session.GetCounter("pushBlocksHit");
        }

        private void Level_OnLoadLevel(Level level, Player.IntroTypes playerIntro, bool isFromLoader)
        {
            if (level.Session.Area.SID == "exudias/4/EnchantedCanyon")
            {
                pushblockCounter = level.Session.GetCounter("pushBlocksHit");
                if (level.Session.Level == "c-03")
                {
                    if (level.Session.GetFlag("canyonHeartActive"))
                    {
                        TempleGate gate = level.Tracker.GetEntity<TempleGate>();
                        gate.RemoveSelf();
                    }
                }
                if (level.Session.Level.StartsWith("c-") || level.Session.Level.StartsWith("b-"))
                {
                    AreaDataExt.Get("exudias/4/EnchantedCanyon").Wipe = delegate (Scene scene, bool wipeIn, Action onComplete)
                    {
                        new DropWipe(scene, wipeIn, onComplete);
                    };
                }
                else
                {
                    AreaDataExt.Get("exudias/4/EnchantedCanyon").Wipe = delegate (Scene scene, bool wipeIn, Action onComplete)
                    {
                        new WindWipe(scene, wipeIn, onComplete);
                    };
                }
            }
        }

        private bool OnLoadEntity(Level level, LevelData levelData, Vector2 offset, EntityData entityData)
        {
            if (!entityData.Name.StartsWith("canyon/"))
            {
                return false;
            }
            string name = entityData.Name.Substring(7);
            int id = entityData.ID;
            EntityID entityID = new EntityID(levelData.Name, id);
            switch (name)
            {
                case "spinorb":
                    level.Add(new SpinOrb(entityData, offset));
                    return true;
                case "pushblock":
                    level.Add(new PushBlock(entityData, offset));
                    return true;
                case "toggleblock":
                    level.Add(new ToggleSwapBlock(entityData, offset));
                    return true;
                case "grannytemple":
                    level.Add(new GrannyTemple(entityData, offset, entityID));
                    return true;
                case "monstertrigger":
                    level.Add(new MonsterTrigger(entityData, offset, entityID));
                    return true;
                case "templefalltrigger":
                    level.Add(new TempleFallTrigger(entityData, offset, entityID));
                    return true;
                case "grannymonster":
                    level.Add(new GrannyMonster(entityData, offset, entityID));
                    return true;
                case "grannystart":
                    level.Add(new GrannyStart(entityData, offset, entityID));
                    return true;
                case "unbreakablestone":
                    level.Add(new UnbreakableStone(entityData, offset));
                    return true;
                default:
                    return false;
            }
        }
    }
}