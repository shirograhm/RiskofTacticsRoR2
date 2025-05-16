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
    class DragonsClaw
    {
        public static ItemDef itemDef;

        // Gain base shield and percent health. Every 10 seconds, heal 2.5% max HP.
        public static ConfigurableValue<bool> isEnabled = new(
            "Item: Dragons Claw",
            "Enabled",
            true,
            "Whether or not the item is enabled.",
            new List<string>()
            {
                "ITEM_DRAGONSCLAW_DESC"
            }
        );
        public static ConfigurableValue<float> shieldBonus = new(
            "Item: Dragons Claw",
            "Percent Shield",
            75f,
            "Percent shield bonus when holding this item.",
            new List<string>()
            {
                "ITEM_DRAGONSCLAW_DESC"
            }
        );
        public static ConfigurableValue<float> maxHealthBonus = new(
            "Item: Dragons Claw",
            "Percent Health",
            9f,
            "Percent max health bonus when holding this item.",
            new List<string>()
            {
                "ITEM_DRAGONSCLAW_DESC"
            }
        );
        public static ConfigurableValue<float> healingPerTick = new(
            "Item: Dragons Claw",
            "Healing Per Tick",
            2.5f,
            "Percent max health healing per item proc.",
            new List<string>()
            {
                "ITEM_DRAGONSCLAW_DESC"
            }
        );
        public static ConfigurableValue<float> tickDuration = new(
            "Item: Dragons Claw",
            "Tick Duration",
            10f,
            "Number of seconds between item procs.",
            new List<string>()
            {
                "ITEM_DRAGONSCLAW_DESC"
            }
        );
        public static readonly float percentShieldBonus = shieldBonus.Value / 100f;
        public static readonly float percentMaxHealthBonus = maxHealthBonus.Value / 100f;
        public static readonly float percentHealingPerTick = healingPerTick.Value / 100f;

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

            Hooks();
        }

        private static void GenerateItem()
        {
            itemDef = ScriptableObject.CreateInstance<ItemDef>();

            itemDef.name = "DRAGONSCLAW";
            itemDef.AutoPopulateTokens();

            Utils.SetItemTier(itemDef, ItemTier.Tier3);

            itemDef.pickupIconSprite = AssetHandler.bundle.LoadAsset<Sprite>("DragonsClaw.png");
            itemDef.pickupModelPrefab = AssetHandler.bundle.LoadAsset<GameObject>("DragonsClaw.prefab");
            itemDef.canRemove = true;
            itemDef.hidden = false;

            itemDef.tags = new ItemTag[]
            {
                ItemTag.Healing,
                ItemTag.Utility
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
                        args.baseShieldAdd += sender.healthComponent.fullHealth * percentShieldBonus;
                        args.healthMultAdd += percentMaxHealthBonus;
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

                if (self && self.inventory)
                {
                    int itemCount = self.inventory.GetItemCount(itemDef);
                    if (itemCount > 0)
                    {
                        Statistics component = self.inventory.GetComponent<Statistics>();
                        // Check time elapsed 
                        if (component && Environment.TickCount - component.LastTick > tickDuration * 1000)
                        {
                            self.healthComponent.Heal(self.healthComponent.fullHealth * percentHealingPerTick, new ProcChainMask());
                            component.LastTick = Environment.TickCount;
                        }
                    }
                }
            };
        }
    }
}
