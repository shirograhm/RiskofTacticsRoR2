using R2API;
using R2API.Networking;
using R2API.Networking.Interfaces;
using RiskOfTactics.Buffs;
using RoR2;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

namespace RiskOfTactics.Items.Completes
{
    class SunfireCape
    {
        public static ItemDef itemDef;

        // Gain max HP. Periodically apply Wound and burn nearby enemies.
        public static ConfigurableValue<bool> isEnabled = new(
            "Item: Sunfire Cape",
            "Enabled",
            true,
            "Whether or not the item is enabled.",
            new List<string>()
            {
                "ITEM_SUNFIRECAPE_DESC"
            }
        );
        public static ConfigurableValue<float> healthBonus = new(
            "Item: Sunfire Cape",
            "Percent Health",
            7f,
            "Percent health gained when holding this item.",
            new List<string>()
            {
                "ITEM_SUNFIRECAPE_DESC"
            }
        );
        public static ConfigurableValue<float> healthBonusExtraStacks = new(
            "Item: Sunfire Cape",
            "Percent Health Extra Stacks",
            5f,
            "Percent health gained when holding extra stacks of this item.",
            new List<string>()
            {
                "ITEM_SUNFIRECAPE_DESC"
            }
        );
        public static ConfigurableValue<float> debuffTickDuration = new(
            "Item: Sunfire Cape",
            "Debuff Tick",
            10f,
            "Seconds between Burn and Wound reapplication.",
            new List<string>()
            {
                "ITEM_SUNFIRECAPE_DESC"
            }
        );
        public static ConfigurableValue<float> maxHealthBurn = new(
            "Item: Sunfire Cape",
            "Burn Percent",
            15f,
            "Total burn damage as a percentage of max HP.",
            new List<string>()
            {
                "ITEM_SUNFIRECAPE_DESC"
            }
        );
        public static ConfigurableValue<float> debuffRadius = new(
            "Item: Sunfire Cape",
            "Debuff Radius",
            12f,
            "Radius of the debuff application zone (meters).",
            new List<string>()
            {
                "ITEM_SUNFIRECAPE_DESC"
            }
        );
        private static readonly float percentHealthBonus = healthBonus.Value / 100f;
        private static readonly float percentHealthBonusExtraStacks = healthBonusExtraStacks.Value / 100f;
        private static readonly float percentMaxHealthBurn = maxHealthBurn.Value / 100f;

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

            itemDef.name = "SUNFIRECAPE";
            itemDef.AutoPopulateTokens();

            Utils.SetItemTier(itemDef, ItemTier.Tier2);

            GameObject prefab = AssetHandler.bundle.LoadAsset<GameObject>("SunfireCape.prefab");
            ModelPanelParameters modelPanelParameters = prefab.AddComponent<ModelPanelParameters>();
            modelPanelParameters.focusPointTransform = prefab.transform;
            modelPanelParameters.cameraPositionTransform = prefab.transform;
            modelPanelParameters.maxDistance = 10f;
            modelPanelParameters.minDistance = 5f;

            itemDef.pickupIconSprite = AssetHandler.bundle.LoadAsset<Sprite>("SunfireCape.png");
            itemDef.pickupModelPrefab = prefab;
            itemDef.canRemove = true;
            itemDef.hidden = false;

            itemDef.tags = new ItemTag[]
            {
                ItemTag.Damage,
                ItemTag.Utility,

                ItemTag.CanBeTemporary
            };
        }

        public static void Hooks()
        {
            RecalculateStatsAPI.GetStatCoefficients += (sender, args) =>
            {
                if (sender && sender.inventory)
                {
                    int count = sender.inventory.GetItemCountEffective(itemDef);
                    if (count > 0)
                    {
                        args.healthTotalMult *= 1 + Utils.GetLinearStacking(percentHealthBonus, percentHealthBonusExtraStacks, count);
                    }
                }
            };

            On.RoR2.CharacterBody.FixedUpdate += (orig, self) =>
            {
                if (self && self.inventory && self.inventory.GetItemCountEffective(itemDef) > 0)
                {
                    int ignitionTankCount = self.inventory.GetItemCountEffective(DLC1Content.Items.StrengthenBurn);

                    Statistics component = self.inventory.GetComponent<Statistics>();
                    // Check time elapsed 
                    if (component && Environment.TickCount - component.LastTick > debuffTickDuration.Value * 1000)
                    {
                        // Get all enemies nearby
                        HurtBox[] hurtboxes = new SphereSearch
                        {
                            mask = LayerIndex.entityPrecise.mask,
                            origin = self.corePosition,
                            queryTriggerInteraction = QueryTriggerInteraction.Collide,
                            radius = debuffRadius.Value
                        }.RefreshCandidates().FilterCandidatesByDistinctHurtBoxEntities().GetHurtBoxes();

                        foreach (HurtBox h in hurtboxes)
                        {
                            HealthComponent hc = h.healthComponent;
                            if (hc && hc.body && !Utils.OnSameTeam(hc.body, self))
                            {
                                InflictDotInfo dotInfo = new InflictDotInfo()
                                {
                                    attackerObject = self.gameObject,
                                    maxStacksFromAttacker = 1,
                                    totalDamage = hc.fullCombinedHealth * percentMaxHealthBurn,
                                    victimObject = hc.body.gameObject
                                };
                                if (ignitionTankCount > 0)
                                {
                                    dotInfo.dotIndex = DotController.DotIndex.StrongerBurn;
                                    dotInfo.damageMultiplier = 3f;
                                }
                                else
                                {
                                    dotInfo.dotIndex = DotController.DotIndex.Burn;
                                    dotInfo.damageMultiplier = 1f;
                                }
                                hc.body.AddTimedBuff(Wound.buffDef, Wound.woundDuration);
                            }
                        }
                        component.LastTick = Environment.TickCount;
                    }
                }
                orig(self);
            };
        }
    }
}
