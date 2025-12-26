using R2API;
using R2API.Networking;
using R2API.Networking.Interfaces;
using RiskOfTactics.Managers;
using RoR2;
using System;
using UnityEngine;
using UnityEngine.Networking;

namespace RiskOfTactics.Content.Items.Completes
{
    class ArchangelsStaff
    {
        public static ItemDef itemDef;
        public static BuffDef foresightBuff;

        public static ItemDef radiantDef;

        // During the teleporter event, periodically gain BASE damage.
        public static ConfigurableValue<bool> isEnabled = new(
            "Item: Archangels Staff",
            "Enabled",
            true,
            "Whether or not the item is enabled.",
            ["ITEM_ROT_ARCHANGELSSTAFF_DESC"]
        );
        public static ConfigurableValue<float> flatDamagePerTick = new(
            "Item: Archangels Staff",
            "Flat Damage Per Tick",
            0.5f,
            "Flat damage gained per item proc.",
            ["ITEM_ROT_ARCHANGELSSTAFF_DESC"],
            true
        );
        public static ConfigurableValue<float> tickDuration = new(
            "Item: Archangels Staff",
            "Tick Duration",
            5f,
            "Number of seconds between item procs.",
            ["ITEM_ROT_ARCHANGELSSTAFF_DESC"],
            false
        );

        public class Statistics : MonoBehaviour
        {
            private float _lastTick;
            public float LastTick
            {
                get { return _lastTick; }
                set
                {
                    _lastTick = value;
                    if (NetworkServer.active)
                    {
                        new Sync(gameObject.GetComponent<NetworkIdentity>().netId, value).Send(NetworkDestination.Clients);
                    }
                }
            }

            public class Sync : INetMessage
            {
                NetworkInstanceId objId;
                float lastTick;

                public Sync()
                {
                }

                public Sync(NetworkInstanceId objId, float tick)
                {
                    this.objId = objId;
                    lastTick = tick;
                }

                public void Deserialize(NetworkReader reader)
                {
                    objId = reader.ReadNetworkId();
                    lastTick = reader.ReadSingle();
                }

                public void OnReceived()
                {
                    if (NetworkServer.active) return;

                    GameObject obj = Util.FindNetworkObject(objId);
                    if (obj != null)
                    {
                        Statistics component = obj.GetComponent<Statistics>();
                        if (component != null)
                        {
                            component.LastTick = lastTick;
                        }
                    }
                }

                public void Serialize(NetworkWriter writer)
                {
                    writer.Write(objId);
                    writer.Write(lastTick);

                    writer.FinishMessage();
                }
            }
        }

        internal static void Init()
        {
            // Normal Variant
            itemDef = ItemManager.GenerateItem("ArchangelsStaff", [ItemTag.Damage, ItemTag.CanBeTemporary], ItemManager.TacticTier.Normal);
            radiantDef = ItemManager.GenerateItem("Radiant_ArchangelsStaff", [ItemTag.Damage, ItemTag.CanBeTemporary], ItemManager.TacticTier.Radiant);

            NetworkingAPI.RegisterMessageType<Statistics.Sync>();

            foresightBuff = Utilities.GenerateBuffDef("Foresight", AssetManager.bundle.LoadAsset<Sprite>("Foresight.png"), true, false, false, false);
            ContentAddition.AddBuffDef(foresightBuff);

            Utilities.RegisterRadiantUpgrade(itemDef, radiantDef);

            Hooks(itemDef, ItemManager.TacticTier.Normal);
            Hooks(radiantDef, ItemManager.TacticTier.Radiant);
        }

        public static void Hooks(ItemDef def, ItemManager.TacticTier tier)
        {
            float radiantMultiplier = tier.Equals(ItemManager.TacticTier.Radiant) ? ConfigManager.Scaling.radiantItemStatMultiplier : 1f;

            CharacterMaster.onStartGlobal += (obj) =>
            {
                obj.inventory?.gameObject.AddComponent<Statistics>();
            };

            RecalculateStatsAPI.GetStatCoefficients += (sender, args) =>
            {
                if (sender && sender.inventory)
                {
                    int count = sender.inventory.GetItemCountEffective(def);
                    if (count > 0)
                    {
                        int buffCount = sender.GetBuffCount(foresightBuff);

                        args.baseDamageAdd += buffCount * flatDamagePerTick.Value * radiantMultiplier;
                    }
                }
            };

            Stage.onStageStartGlobal += (stage) =>
            {
                foreach (NetworkUser user in NetworkUser.readOnlyInstancesList)
                {
                    CharacterMaster master = user.masterController.master ?? user.master;
                    if (master && master.inventory && master.inventory.GetItemCountEffective(def) > 0)
                    {
                        Statistics component = master.inventory.GetComponent<Statistics>();
                        if (component)
                        {
                            component.LastTick = Environment.TickCount;
                        }
                    }
                }
            };

            On.RoR2.CharacterBody.FixedUpdate += (orig, self) =>
            {
                orig(self);

                foreach (HoldoutZoneController hzc in InstanceTracker.GetInstancesList<HoldoutZoneController>())
                {
                    if (self && self.inventory)
                    {
                        int itemCount = self.inventory.GetItemCountEffective(def);

                        if (itemCount > 0 && hzc.isActiveAndEnabled)
                        {
                            Statistics component = self.inventory.GetComponent<Statistics>();
                            // Check time elapsed 
                            if (component && Environment.TickCount - component.LastTick > tickDuration.Value * 1000)
                            {
                                self.AddBuff(foresightBuff);
                                component.LastTick = Environment.TickCount;
                            }
                        }
                    }
                }
            };
        }
    }
}
