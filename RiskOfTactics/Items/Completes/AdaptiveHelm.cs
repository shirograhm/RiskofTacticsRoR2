using R2API;
using RoR2;
using System.Collections.Generic;
using UnityEngine;

namespace RiskOfTactics.Items.Completes
{
    class AdaptiveHelm
    {
        public static ItemDef itemDef;
        public static BuffDef rangedResetCooldownBuff;

        // Gain armor and max HP shield. Gain additional effects based on your character class.
        // Melee: Gain bonus armor and shield. When you take damage, reduce all active cooldowns.
        // Ranged: Gain BASE damage. Periodically refresh all cooldowns.
        public static ConfigurableValue<bool> isEnabled = new(
            "Item: Adaptive Helm",
            "Enabled",
            true,
            "Whether or not the item is enabled.",
            new List<string>()
            {
                "ITEM_ROT_ADAPTIVEHELM_DESC"
            }
        );
        public static ConfigurableValue<float> commonStatBoost = new(
            "Item: Adaptive Helm",
            "Common Stat Boost",
            10f,
            "Base armor and % max HP shield for all users.",
            new List<string>()
            {
                "ITEM_ROT_ADAPTIVEHELM_DESC"
            }
        );
        public static ConfigurableValue<float> meleeResistBonus = new(
            "Item: Adaptive Helm - Melee",
            "Bonus Resist",
            40f,
            "Melee: Armor and % shield bonus for melee users for the first stack.",
            new List<string>()
            {
                "ITEM_ROT_ADAPTIVEHELM_DESC"
            }
        );
        public static ConfigurableValue<float> meleeResistBonusExtraStacks = new(
            "Item: Adaptive Helm - Melee",
            "Bonus Resist Extra Stacks",
            30f,
            "Melee: Armor and % shield bonus for melee users with extra stacks.",
            new List<string>()
            {
                "ITEM_ROT_ADAPTIVEHELM_DESC"
            }
        );
        public static ConfigurableValue<float> cooldownRefundOnTakeDamage = new(
            "Item: Adaptive Helm - Melee",
            "Cooldown Refund",
            0.5f,
            "Melee: Seconds cooldown refunded when taking damage.",
            new List<string>()
            {
                "ITEM_ROT_ADAPTIVEHELM_DESC"
            }
        );
        public static ConfigurableValue<float> rangedDamageBonus = new(
            "Item: Adaptive Helm - Ranged",
            "Bonus Damage",
            5f,
            "Flat damage bonus for ranged users for the first stack.",
            new List<string>()
            {
                "ITEM_ROT_ADAPTIVEHELM_DESC"
            }
        );
        public static ConfigurableValue<float> rangedDamageBonusExtraStacks = new(
            "Item: Adaptive Helm - Ranged",
            "Bonus Damage Extra Stacks",
            3f,
            "Flat damage bonus for ranged users with extra stacks.",
            new List<string>()
            {
                "ITEM_ROT_ADAPTIVEHELM_DESC"
            }
        );
        public static ConfigurableValue<float> cooldownRefreshInterval = new(
            "Item: Adaptive Helm - Ranged",
            "Cooldown Interval",
            20f,
            "All cooldowns are refunded on this interval for ranged item users.",
            new List<string>()
            {
                "ITEM_ROT_ADAPTIVEHELM_DESC"
            }
        );
        public static ConfigurableValue<float> cooldownRefreshIntervalReduction = new(
            "Item: Adaptive Helm - Ranged",
            "Cooldown Interval Reduction",
            20f,
            "Cooldown refund timer for this item is reduced by this percentage per stack for ranged users.",
            new List<string>()
            {
                "ITEM_ROT_ADAPTIVEHELM_DESC"
            }
        );
        public static readonly float percentCommonStatBoost = commonStatBoost.Value / 100f;
        public static readonly float percentMeleeResistBonus = meleeResistBonus.Value / 100f;
        public static readonly float percentMeleeResistBonusExtraStacks = meleeResistBonusExtraStacks.Value / 100f;
        public static readonly float percentCooldownRefreshIntervalReduction = cooldownRefreshIntervalReduction.Value / 100f;

        internal static void Init()
        {
            GenerateItem();

            ItemDisplayRuleDict displayRules = new ItemDisplayRuleDict(null);
            ItemAPI.Add(new CustomItem(itemDef, displayRules));

            rangedResetCooldownBuff = Utils.GenerateBuffDef("AdaptiveHelmRangedCooldown", AssetHandler.bundle.LoadAsset<Sprite>("AdaptiveHelm.png"), false, false, false, true);
            ContentAddition.AddBuffDef(rangedResetCooldownBuff);

            Hooks();
        }

        private static void GenerateItem()
        {
            itemDef = ScriptableObject.CreateInstance<ItemDef>();

            itemDef.name = "ROT_ADAPTIVEHELM";
            itemDef.AutoPopulateTokens();

            Utils.SetItemTier(itemDef, ItemTier.Tier2);

            GameObject prefab = AssetHandler.bundle.LoadAsset<GameObject>("AdaptiveHelm.prefab");
            ModelPanelParameters modelPanelParameters = prefab.AddComponent<ModelPanelParameters>();
            modelPanelParameters.focusPointTransform = prefab.transform;
            modelPanelParameters.cameraPositionTransform = prefab.transform;
            modelPanelParameters.maxDistance = 10f;
            modelPanelParameters.minDistance = 5f;

            itemDef.pickupIconSprite = AssetHandler.bundle.LoadAsset<Sprite>("AdaptiveHelm.png");
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
            On.RoR2.CharacterBody.FixedUpdate += (orig, self) =>
            {
                orig(self);

                if (self && self.inventory && Utils.IsRangedBodyPrefab(self.gameObject))
                {
                    int itemCount = self.inventory.GetItemCountEffective(itemDef);
                    if (itemCount > 0 && !self.HasBuff(rangedResetCooldownBuff))
                    {
                        self.AddTimedBuff(rangedResetCooldownBuff, Utils.GetReverseExponentialStacking(cooldownRefreshInterval.Value, percentCooldownRefreshIntervalReduction, itemCount));
                    }
                }
            };

            RecalculateStatsAPI.GetStatCoefficients += (sender, args) =>
            {
                if (sender && sender.inventory)
                {
                    int count = sender.inventory.GetItemCountEffective(itemDef);
                    if (count > 0)
                    {
                        args.armorAdd += Utils.GetLinearStacking(commonStatBoost.Value, 0f, count);
                        args.baseShieldAdd += sender.healthComponent.fullHealth * Utils.GetLinearStacking(percentCommonStatBoost, 0f, count);

                        if (Utils.IsMeleeBodyPrefab(sender.gameObject))
                        {
                            args.armorTotalMult *= 1 + Utils.GetLinearStacking(percentMeleeResistBonus, percentMeleeResistBonusExtraStacks, count);
                            args.shieldTotalMult *= 1 + Utils.GetLinearStacking(percentMeleeResistBonus, percentMeleeResistBonusExtraStacks, count);
                        }

                        if (Utils.IsRangedBodyPrefab(sender.gameObject))
                        {
                            args.baseDamageAdd += Utils.GetLinearStacking(rangedDamageBonus.Value, rangedDamageBonusExtraStacks.Value, count);
                        }
                    }
                }
            };

            On.RoR2.CharacterBody.OnBuffFinalStackLost += (orig, self, buffDef) =>
            {
                orig(self, buffDef);

                if (self && self.skillLocator && buffDef == rangedResetCooldownBuff)
                {
                    self.skillLocator.ResetSkills();

                    if (self.inventory)
                    {
                        int itemCount = self.inventory.GetItemCountEffective(itemDef);
                        if (itemCount > 0 && Utils.IsRangedBodyPrefab(self.gameObject))
                        {
                            self.AddTimedBuff(rangedResetCooldownBuff, Utils.GetReverseExponentialStacking(cooldownRefreshInterval.Value, percentCooldownRefreshIntervalReduction, itemCount));
                        }
                    }
                }
            };

            GenericGameEvents.OnTakeDamage += (damageReport) =>
            {
                CharacterBody vicBody = damageReport.victimBody;
                if (vicBody && vicBody.inventory && vicBody.skillLocator && Utils.IsMeleeBodyPrefab(vicBody.gameObject))
                {
                    int count = vicBody.inventory.GetItemCountEffective(itemDef);
                    if (count > 0)
                    {
                        vicBody.skillLocator.DeductCooldownFromAllSkillsServer(cooldownRefundOnTakeDamage.Value);
                        //if (vicBody.skillLocator.primary)
                        //    vicBody.skillLocator.primary.rechargeStopwatch += cooldownRefundOnTakeDamage.Value;
                        //if (vicBody.skillLocator.secondary)
                        //    vicBody.skillLocator.secondary.rechargeStopwatch += cooldownRefundOnTakeDamage.Value;
                        //if (vicBody.skillLocator.utility)
                        //    vicBody.skillLocator.utility.rechargeStopwatch += cooldownRefundOnTakeDamage.Value;
                        //if (vicBody.skillLocator.special)
                        //    vicBody.skillLocator.special.rechargeStopwatch += cooldownRefundOnTakeDamage.Value;
                    }
                }
            };
        }
    }
}
