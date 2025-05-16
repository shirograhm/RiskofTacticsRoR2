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
    class AdaptiveHelm
    {
        public static ItemDef itemDef;

        // Gain . Every 10 seconds, gain 1 flat damage.
        public static ConfigurableValue<bool> isEnabled = new(
            "Item: Adaptive Helm",
            "Enabled",
            true,
            "Whether or not the item is enabled.",
            new List<string>()
            {
                "ITEM_ADAPTIVEHELM_DESC"
            }
        );
        public static ConfigurableValue<float> flatDamageBonus = new(
            "Item: Adaptive Helm",
            "Flat Damage",
            10f,
            "Flat damage bonus when holding this item.",
            new List<string>()
            {
                "ITEM_ADAPTIVEHELM_DESC"
            }
        );
        public static ConfigurableValue<float> cooldownReductionBonus = new(
            "Item: Adaptive Helm",
            "Cooldown Reduction",
            15f,
            "Cooldown reduction gained when holding this item.",
            new List<string>()
            {
                "ITEM_ADAPTIVEHELM_DESC"
            }
        );
        public static ConfigurableValue<float> shieldBonus = new(
            "Item: Adaptive Helm",
            "Percent Shield",
            20f,
            "Percent shield gained when holding this item.",
            new List<string>()
            {
                "ITEM_ADAPTIVEHELM_DESC"
            }
        );
        public static ConfigurableValue<float> meleeResistBonus = new(
            "Item: Adaptive Helm - Melee",
            "Bonus Resists",
            40f,
            "Armor and % shield bonus for melee item users.",
            new List<string>()
            {
                "ITEM_ADAPTIVEHELM_DESC"
            }
        );
        public static ConfigurableValue<float> cooldownRefundOnTakeDamage = new(
            "Item: Adaptive Helm - Melee",
            "Cooldown Refund",
            1f,
            "Percent cooldown refunded when taking damage as melee.",
            new List<string>()
            {
                "ITEM_ADAPTIVEHELM_DESC"
            }
        );
        public static ConfigurableValue<float> rangedDamageBonus = new(
            "Item: Adaptive Helm - Ranged",
            "Bonus Damage",
            15f,
            "Flat damage bonus for ranged item users.",
            new List<string>()
            {
                "ITEM_ADAPTIVEHELM_DESC"
            }
        );
        public static ConfigurableValue<float> cooldownRefreshInterval = new(
            "Item: Adaptive Helm - Ranged",
            "Cooldown Refresh Interval",
            10f,
            "All cooldowns are refunded on this interval for ranged item users.",
            new List<string>()
            {
                "ITEM_ADAPTIVEHELM_DESC"
            }
        );
        public static readonly float percentShieldBonus = shieldBonus.Value / 100f;
        public static readonly float percentMeleeShieldBonus = meleeResistBonus.Value / 100f;
        public static readonly float percentCooldownReductionBonus = cooldownReductionBonus.Value / 100f;
        public static readonly float percentCooldownRefundedBonus = cooldownRefundOnTakeDamage.Value / 100f;

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

            itemDef.name = "ADAPTIVEHELM";
            itemDef.AutoPopulateTokens();

            Utils.SetItemTier(itemDef, ItemTier.Tier3);

            itemDef.pickupIconSprite = AssetHandler.bundle.LoadAsset<Sprite>("AdaptiveHelm.png");
            itemDef.pickupModelPrefab = AssetHandler.bundle.LoadAsset<GameObject>("AdaptiveHelm.prefab");
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
                    int count = sender.inventory.GetItemCount(itemDef);
                    if (count > 0)
                    {
                        args.baseDamageAdd += flatDamageBonus.Value;
                        args.cooldownMultAdd -= percentCooldownReductionBonus;
                        args.baseShieldAdd += sender.healthComponent.fullHealth * percentShieldBonus;

                        if (melee)
                        {
                            args.armorAdd += meleeResistBonus;
                            args.baseShieldAdd += sender.healthComponent.fullHealth * percentMeleeShieldBonus;
                        }

                        if (ranged) 
                        {
                            args.baseDamageAdd += rangedDamageBonus.Value;
                        }
                     }
                }
            };

            GenericGameEvents.OnTakeDamage += (damageReport) =>
            {
                CharacterBody vicBody = damageReport.victimBody;
                if (melee && vicBody && vicBody.inventory)
                {
                    int count = vicBody.inventory.GetItemCount(itemDef);
                    if (count > 0)
                    {
                        foreach (GenericSkill skill in vicBody.skillLocator.allSkills)
                        {
                            skill.rechargeStopwatch -= skill.baseRechargeStopwatch * percentCooldownReductionBonus;
                        }
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

                if (ranged && self && self.inventory)
                {
                    int itemCount = self.inventory.GetItemCount(itemDef);
                    if (itemCount > 0)
                    {
                        Statistics component = self.inventory.GetComponent<Statistics>();
                        // Check time elapsed 
                        if (component && Environment.TickCount - component.LastTick > cooldownRefreshInterval * 1000)
                        {
                            foreach (GenericSkill skill in self.skillLocator.allSkills)
                            {
                                skill.rechargeStopwatch = 0;
                            }
                            component.LastTick = Environment.TickCount;
                        }
                    }
                }
            };
        }
    }
}
