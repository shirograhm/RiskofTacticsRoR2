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
    class Quicksilver
    {
        public static ItemDef itemDef;
        public static BuffDef flowBuff;
        public static BuffDef cleanseBuff;

        // Gain scaling damage, crit chance and shielding. When the teleporter is activated, gain immunity to crowd control for 30 seconds. During this time, gain 0.5% attack speed every second.
        public static ConfigurableValue<bool> isEnabled = new(
            "Item: Quicksilver",
            "Enabled",
            true,
            "Whether or not the item is enabled.",
            new List<string>()
            {
                "ITEM_QUICKSILVER_DESC"
            }
        );
        public static ConfigurableValue<float> damageBonus = new(
            "Item: Quicksilver",
            "Percent Damage",
            10f,
            "Percent damage bonus when holding this item.",
            new List<string>()
            {
                "ITEM_QUICKSILVER_DESC"
            }
        );
        public static ConfigurableValue<float> critChanceBonus = new(
            "Item: Quicksilver",
            "Crit Chance",
            15f,
            "Crit chance gained when holding this item.",
            new List<string>()
            {
                "ITEM_QUICKSILVER_DESC"
            }
        );
        public static ConfigurableValue<float> shieldBonus = new(
            "Item: Quicksilver",
            "Percent Shield",
            15f,
            "Percent max health shield gained when holding this item.",
            new List<string>()
            {
                "ITEM_QUICKSILVER_DESC"
            }
        );
        public static ConfigurableValue<float> ccImmunityDuration = new(
            "Item: Quicksilver",
            "CC Immunity Duration",
            30f,
            "Number of seconds immune to crowd control once the teleporter event starts.",
            new List<string>()
            {
                "ITEM_QUICKSILVER_DESC"
            }
        );
        public static ConfigurableValue<float> attackSpeedPerBuff = new(
            "Item: Quicksilver",
            "Attack Speed",
            1f,
            "Attack speed gained per second while immune to CC.",
            new List<string>()
            {
                "ITEM_QUICKSILVER_DESC"
            }
        );
        private static readonly float percentDamageBonus = damageBonus.Value / 100f;
        private static readonly float percentShieldBonus = shieldBonus.Value / 100f;
        private static readonly float percentAttackSpeedPerBuff = attackSpeedPerBuff.Value / 100f;

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

            flowBuff = Utils.GenerateBuffDef("Flow", AssetHandler.bundle.LoadAsset<Sprite>("Flow.png"), true, false, false, false);
            ContentAddition.AddBuffDef(flowBuff);
            cleanseBuff = Utils.GenerateBuffDef("Cleanse", AssetHandler.bundle.LoadAsset<Sprite>("flowBuff.png"), false, false, false, true);
            ContentAddition.AddBuffDef(cleanseBuff);

            Hooks();
        }

        private static void GenerateItem()
        {
            itemDef = ScriptableObject.CreateInstance<ItemDef>();

            itemDef.name = "QUICKSILVER";
            itemDef.AutoPopulateTokens();

            Utils.SetItemTier(itemDef, ItemTier.Tier3);

            itemDef.pickupIconSprite = AssetHandler.bundle.LoadAsset<Sprite>("Quicksilver.png");
            itemDef.pickupModelPrefab = AssetHandler.bundle.LoadAsset<GameObject>("Quicksilver.prefab");
            itemDef.canRemove = true;
            itemDef.hidden = false;

            itemDef.tags = new ItemTag[]
            {
                ItemTag.Damage,
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
                    int buffCount = sender.GetBuffCount(flowBuff);
                    if (buffCount > 0)
                        args.attackSpeedMultAdd += buffCount * percentAttackSpeedPerBuff;

                    int count = sender.inventory.GetItemCount(itemDef);
                    if (count > 0)
                    {
                        args.damageMultAdd += percentDamageBonus;
                        args.critAdd += critChanceBonus.Value;
                        args.baseShieldAdd += sender.healthComponent.fullHealth * percentShieldBonus;
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
                            if (self.GetBuffCount(flowBuff) == 0 && self.GetBuffCount(cleanseBuff) == 0)
                                self.AddTimedBuff(cleanseBuff, ccImmunityDuration);

                            if (self.GetBuffCount(cleanseBuff) > 0)
                            {
                                Statistics component = self.inventory.GetComponent<Statistics>();
                                // Check time elapsed 
                                if (component && Environment.TickCount - component.LastTick > 1000)
                                {
                                    self.AddBuff(flowBuff);
                                    component.LastTick = Environment.TickCount;
                                }
                            }
                        }
                    }
                }
            };
        }
    }
}
