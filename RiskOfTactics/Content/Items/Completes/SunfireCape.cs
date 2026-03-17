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
    class SunfireCape
    {
        public static ItemDef itemDef;
        public static GameObject sunfireEffectIndicator;

        public static ItemDef radiantDef;

        // Gain max HP. Periodically burn nearby enemies and disable their healing.
        public static ConfigurableValue<bool> isEnabled = new(
            "Item: Sunfire Cape",
            "Enabled",
            true,
            "Whether or not the item is enabled.",
            ["ITEM_ROT_SUNFIRECAPE_DESC"]
        );
        public static ConfigurableValue<float> healthBonus = new(
            "Item: Sunfire Cape",
            "Percent Health",
            7f,
            "Percent health gained when holding this item.",
            ["ITEM_ROT_SUNFIRECAPE_DESC"],
            true
        );
        public static ConfigurableValue<float> healthBonusExtraStacks = new(
            "Item: Sunfire Cape",
            "Percent Health Extra Stacks",
            7f,
            "Percent health gained when holding extra stacks of this item.",
            ["ITEM_ROT_SUNFIRECAPE_DESC"],
            true
        );
        public static ConfigurableValue<float> debuffTickDuration = new(
            "Item: Sunfire Cape",
            "Debuff Tick",
            7f,
            "Seconds between Burn and Wound reapplication.",
            ["ITEM_ROT_SUNFIRECAPE_DESC"],
            false
        );
        public static ConfigurableValue<float> maxHealthBurn = new(
            "Item: Sunfire Cape",
            "Burn Percent",
            15f,
            "Total burn damage as a percentage of max HP.",
            ["ITEM_ROT_SUNFIRECAPE_DESC"],
            true
        );
        public static ConfigurableValue<int> debuffRadius = new(
            "Item: Sunfire Cape",
            "Debuff Radius",
            15,
            "Range of the debuff application (meters).",
            ["ITEM_ROT_SUNFIRECAPE_DESC"],
            true
        );
        public static ConfigurableValue<float> healingDisableDuration = new(
            "Item: Sunfire Cape",
            "Healing Disable Duration",
            7f,
            "Healing disable duration once applied by this item.",
            ["ITEM_ROT_SUNFIRECAPE_DESC"],
            false
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
            itemDef = ItemManager.GenerateItem("SunfireCape", [ItemTag.Damage, ItemTag.Utility, ItemTag.CanBeTemporary], ItemManager.TacticTier.Normal);
            radiantDef = ItemManager.GenerateItem("Radiant_SunfireCape", [ItemTag.Damage, ItemTag.Utility, ItemTag.CanBeTemporary], ItemManager.TacticTier.Radiant);

            sunfireEffectIndicator = LegacyResourcesAPI.LoadAsync<GameObject>("Prefabs/NetworkedObjects/ExplodeOnDeathVoidExplosion").WaitForCompletion();

            NetworkingAPI.RegisterMessageType<Statistics.Sync>();

            //Utilities.RegisterRadiantUpgrade(itemDef, radiantDef);

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
                        args.healthMultAdd += Utilities.GetLinearStacking(percentHealthBonus, percentHealthBonusExtraStacks, count);
                    }
                }
            };

            On.RoR2.CharacterBody.FixedUpdate += (orig, self) =>
            {
                if (self && self.inventory && self.inventory.GetItemCountEffective(def) > 0)
                {
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
                            if (hc && hc.body && !Utilities.OnSameTeam(hc.body, self))
                            {
                                InflictDotInfo dotInfo = new InflictDotInfo()
                                {
                                    attackerObject = self.gameObject,
                                    maxStacksFromAttacker = 1,
                                    totalDamage = hc.fullCombinedHealth * percentMaxHealthBurn,
                                    victimObject = hc.body.gameObject
                                };
                                if (self.inventory.GetItemCountEffective(DLC1Content.Items.StrengthenBurn) > 0)
                                {
                                    dotInfo.dotIndex = DotController.DotIndex.StrongerBurn;
                                    dotInfo.damageMultiplier = 3f;
                                }
                                else
                                {
                                    dotInfo.dotIndex = DotController.DotIndex.Burn;
                                    dotInfo.damageMultiplier = 1f;
                                }
                                DotController.InflictDot(ref dotInfo);

                                hc.body.AddTimedBuff(RoR2Content.Buffs.HealingDisabled, healingDisableDuration.Value);
                            }
                        }
                        component.LastTick = Environment.TickCount;

                        DisplaySunfireEffectIndicator(self);
                    }
                }
                orig(self);
            };
        }

        private static void DisplaySunfireEffectIndicator(CharacterBody self)
        {
            if (self.teamComponent)
            {
                GameObject obj = UnityEngine.Object.Instantiate(sunfireEffectIndicator, self.corePosition, Quaternion.identity);
                DelayBlast component4 = obj.GetComponent<DelayBlast>();
                component4.position = self.corePosition;
                component4.baseDamage = 0;
                component4.baseForce = 0f;
                component4.radius = debuffRadius.Value;
                component4.attacker = self.gameObject;
                component4.inflictor = self.gameObject;
                component4.crit = Util.CheckRoll(self.crit, self.master);
                component4.maxTimer = 0.2f;
                component4.damageColorIndex = DamageColorIndex.Electrocution;
                component4.falloffModel = BlastAttack.FalloffModel.SweetSpot;
                obj.GetComponent<TeamFilter>().teamIndex = self.teamComponent.teamIndex;
                NetworkServer.Spawn(obj);
            }
        }
    }
}
