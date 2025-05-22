using R2API;
using R2API.Networking;
using R2API.Networking.Interfaces;
using RoR2;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

namespace RiskOfTactics
{
    class ArchangelsStaff
    {
        public static ItemDef itemDef;
        public static BuffDef foresightBuff;

        // Gain flat damage and cooldown reduction. Upon activation of the teleporter, gain 1 damage every 10 seconds.
        public static ConfigurableValue<bool> isEnabled = new(
            "Item: Archangels Staff",
            "Enabled",
            true,
            "Whether or not the item is enabled.",
            new List<string>()
            {
                "ITEM_ARCHANGELSSTAFF_DESC"
            }
        );
        public static ConfigurableValue<float> flatDamageBonus = new(
            "Item: Archangels Staff",
            "Flat Damage",
            8f,
            "Flat damage bonus when holding this item.",
            new List<string>()
            {
                "ITEM_ARCHANGELSSTAFF_DESC"
            }
        );
        public static ConfigurableValue<float> cooldownReductionBonus = new(
            "Item: Archangels Staff",
            "Cooldown Reduction",
            8f,
            "Cooldown reduction gained when holding this item.",
            new List<string>()
            {
                "ITEM_ARCHANGELSSTAFF_DESC"
            }
        );
        public static ConfigurableValue<float> flatDamagePerTick = new(
            "Item: Archangels Staff",
            "Flat Damage Per Tick",
            1f,
            "Flat damage gained per item proc.",
            new List<string>()
            {
                "ITEM_ARCHANGELSSTAFF_DESC"
            }
        );
        public static ConfigurableValue<float> tickDuration = new(
            "Item: Archangels Staff",
            "Tick Duration",
            10f,
            "Number of seconds between item procs.",
            new List<string>()
            {
                "ITEM_ARCHANGELSSTAFF_DESC"
            }
        );
        private static readonly float percentCooldownReductionBonus = cooldownReductionBonus.Value / 100f;

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
            GenerateItem();

            ItemDisplayRuleDict displayRules = new ItemDisplayRuleDict(null);
            ItemAPI.Add(new CustomItem(itemDef, displayRules));

            NetworkingAPI.RegisterMessageType<Statistics.Sync>();

            foresightBuff = Utils.GenerateBuffDef("Foresight", AssetHandler.bundle.LoadAsset<Sprite>("Foresight.png"), true, false, false, false);
            ContentAddition.AddBuffDef(foresightBuff);

            Hooks();
        }

        private static void GenerateItem()
        {
            itemDef = ScriptableObject.CreateInstance<ItemDef>();

            itemDef.name = "ARCHANGELSSTAFF";
            itemDef.AutoPopulateTokens();

            Utils.SetItemTier(itemDef, ItemTier.Tier3);

            itemDef.pickupIconSprite = AssetHandler.bundle.LoadAsset<Sprite>("ArchangelsStaff.png");
            itemDef.pickupModelPrefab = AssetHandler.bundle.LoadAsset<GameObject>("ArchangelsStaff.prefab");
            itemDef.canRemove = true;
            itemDef.hidden = false;

            itemDef.tags = new ItemTag[]
            {
                ItemTag.Damage
            };
        }

        public static void Hooks()
        {
            CharacterMaster.onStartGlobal += (obj) =>
            {
                obj.inventory?.gameObject.AddComponent<Statistics>();
            };

            RecalculateStatsAPI.GetStatCoefficients += (sender, args) =>
            {
                if (sender && sender.inventory)
                {
                    int count = sender.inventory.GetItemCount(itemDef);
                    if (count > 0)
                    {
                        int buffCount = sender.GetBuffCount(foresightBuff);

                        args.cooldownMultAdd -= percentCooldownReductionBonus;
                        args.baseDamageAdd += flatDamageBonus.Value;
                        args.baseDamageAdd += buffCount * flatDamagePerTick.Value;
                    }
                }
            };

            Stage.onStageStartGlobal += (stage) =>
            {
                foreach (NetworkUser user in NetworkUser.readOnlyInstancesList)
                {
                    CharacterMaster master = user.masterController.master ?? user.master;
                    if (master && master.inventory && master.inventory.GetItemCount(itemDef) > 0)
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
                        int itemCount = self.inventory.GetItemCount(itemDef);

                        if (itemCount > 0 && hzc.isActiveAndEnabled)
                        {
                            Statistics component = self.inventory.GetComponent<Statistics>();
                            // Check time elapsed 
                            if (component && Environment.TickCount - component.LastTick > tickDuration * 1000)
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
