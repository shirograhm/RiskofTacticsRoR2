using R2API;
using RiskOfTactics.Managers;
using RoR2;
using RoR2.Items;
using UnityEngine;
using UnityEngine.Networking;

namespace RiskOfTactics.Content.Items.Completes
{
    public class SunfireCapeItemBehavior : BaseItemBodyBehavior
    {
        [ItemDefAssociation(useOnServer = true, useOnClient = false)]
        public static ItemDef GetItemDef()
        {
            return SunfireCape.itemDef;
        }

        public void FixedUpdate()
        {
            SunfireCape.FixedUpdateHook(body, stack, SunfireCape.sunfireCooldownBuff);
        }
    }

    public class RadiantSunfireCapeItemBehavior : BaseItemBodyBehavior
    {
        [ItemDefAssociation(useOnServer = true, useOnClient = false)]
        public static ItemDef GetItemDef()
        {
            return SunfireCape.radiantDef;
        }

        public void FixedUpdate()
        {
            SunfireCape.FixedUpdateHook(body, stack, SunfireCape.radiantSunfireCooldownBuff);
        }
    }

    class SunfireCape
    {
        public static GameObject sunfireEffectIndicator;

        public static ItemDef itemDef;
        public static BuffDef sunfireCooldownBuff;

        public static ItemDef radiantDef;
        public static BuffDef radiantSunfireCooldownBuff;

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
            false
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

        internal static void Init()
        {
            itemDef = ItemManager.GenerateItem("SunfireCape", [ItemTag.Damage, ItemTag.Utility, ItemTag.CanBeTemporary], ItemManager.TacticTier.Normal);
            sunfireCooldownBuff = Utilities.GenerateBuffDef("SunfireCapeCooldown", AssetManager.bundle.LoadAsset<Sprite>("SunfireCape"), false, false, false, false);
            ContentAddition.AddBuffDef(sunfireCooldownBuff);

            radiantDef = ItemManager.GenerateItem("Radiant_SunfireCape", [ItemTag.Damage, ItemTag.Utility, ItemTag.CanBeTemporary], ItemManager.TacticTier.Radiant);
            radiantSunfireCooldownBuff = Utilities.GenerateBuffDef("RadiantSunfireCapeCooldown", AssetManager.bundle.LoadAsset<Sprite>("Radiant_SunfireCape"), false, false, false, false);
            ContentAddition.AddBuffDef(radiantSunfireCooldownBuff);

            sunfireEffectIndicator = LegacyResourcesAPI.LoadAsync<GameObject>("Prefabs/NetworkedObjects/ExplodeOnDeathVoidExplosion").WaitForCompletion();

            if (ConfigManager.Scaling.useRadiantAutoConversion) Utilities.RegisterRadiantUpgrade(itemDef, radiantDef);

            Hooks(itemDef, sunfireCooldownBuff, ItemManager.TacticTier.Normal);
            Hooks(radiantDef, radiantSunfireCooldownBuff, ItemManager.TacticTier.Radiant);
        }

        public static void Hooks(ItemDef def, BuffDef cooldownBuff, ItemManager.TacticTier tier)
        {
            float radiantMultiplier = tier.Equals(ItemManager.TacticTier.Radiant) ? ConfigManager.Scaling.radiantItemStatMultiplier : 1f;

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

            On.RoR2.CharacterBody.OnBuffFinalStackLost += (orig, self, buffDef) =>
            {
                orig(self, buffDef);

                if (buffDef == cooldownBuff)
                {
                    if (self && self.inventory)
                    {
                        int count = self.inventory.GetItemCountEffective(def);
                        ApplySunfireEffect(self, count, cooldownBuff);

                        if (count > 0)
                            self.AddTimedBuff(cooldownBuff, debuffTickDuration.Value);
                    }
                }
            };
        }

        internal static void ApplySunfireEffect(CharacterBody self, int count, BuffDef cooldownBuff)
        {
            if (count > 0)
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
                DisplaySunfireEffectIndicator(self);
            }
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

        internal static void FixedUpdateHook(CharacterBody body, int stack, BuffDef buff)
        {
            if (body && body.GetBuffCount(buff) == 0 && stack > 0)
            {
                body.AddTimedBuff(buff, debuffTickDuration.Value);
            }
        }
    }
}
