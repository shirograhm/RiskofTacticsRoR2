using R2API;
using RiskOfTactics.Managers;
using RoR2;
using UnityEngine;

namespace RiskOfTactics.Content.Items.Completes
{
    class AdaptiveHelm
    {
        public static ItemDef itemDef;
        public static BuffDef cooldownResetBuff;

        public static ItemDef radiantDef;
        public static BuffDef radiantCooldownResetBuff;

        // Gain armor and max HP shield. Gain additional effects based on your character class.
        // Melee: Gain bonus armor and shield. When you take damage, reduce all active cooldowns.
        // Ranged: Gain BASE damage. Periodically refresh all cooldowns.
        public static ConfigurableValue<bool> isEnabled = new(
            "Item: Adaptive Helm",
            "Enabled",
            true,
            "Whether or not the item is enabled.",
            ["ITEM_ROT_ADAPTIVEHELM_DESC"]
        );
        public static ConfigurableValue<float> commonStatBoost = new(
            "Item: Adaptive Helm",
            "Common Stat Boost",
            10f,
            "Base armor and % max HP shield for all users.",
            ["ITEM_ROT_ADAPTIVEHELM_DESC"],
            true
        );
        public static ConfigurableValue<float> meleeResistBonus = new(
            "Item: Adaptive Helm - Melee",
            "Bonus Resist",
            40f,
            "Melee: Armor and % shield bonus for melee users for the first stack.",
            ["ITEM_ROT_ADAPTIVEHELM_DESC"],
            true
        );
        public static ConfigurableValue<float> meleeResistBonusExtraStacks = new(
            "Item: Adaptive Helm - Melee",
            "Bonus Resist Extra Stacks",
            30f,
            "Melee: Armor and % shield bonus for melee users with extra stacks.",
            ["ITEM_ROT_ADAPTIVEHELM_DESC"],
            true
        );
        public static ConfigurableValue<float> cooldownRefundOnTakeDamage = new(
            "Item: Adaptive Helm - Melee",
            "Cooldown Refund",
            0.5f,
            "Melee: Seconds cooldown refunded when taking damage.",
            ["ITEM_ROT_ADAPTIVEHELM_DESC"],
            true
        );
        public static ConfigurableValue<float> rangedDamageBonus = new(
            "Item: Adaptive Helm - Ranged",
            "Bonus Damage",
            5f,
            "Flat damage bonus for ranged users for the first stack.",
            ["ITEM_ROT_ADAPTIVEHELM_DESC"],
            true
        );
        public static ConfigurableValue<float> rangedDamageBonusExtraStacks = new(
            "Item: Adaptive Helm - Ranged",
            "Bonus Damage Extra Stacks",
            3f,
            "Flat damage bonus for ranged users with extra stacks.",
            ["ITEM_ROT_ADAPTIVEHELM_DESC"],
            true
        );
        public static ConfigurableValue<float> cooldownRefreshInterval = new(
            "Item: Adaptive Helm - Ranged",
            "Cooldown Interval",
            20f,
            "All cooldowns are refunded on this interval for ranged item users.",
            ["ITEM_ROT_ADAPTIVEHELM_DESC"],
            false
        );
        public static ConfigurableValue<float> cooldownRefreshIntervalReduction = new(
            "Item: Adaptive Helm - Ranged",
            "Cooldown Interval Reduction",
            20f,
            "Cooldown refund timer for this item is reduced by this percentage per stack for ranged users.",
            ["ITEM_ROT_ADAPTIVEHELM_DESC"],
            false
        );
        public static readonly float percentCommonStatBoost = commonStatBoost.Value / 100f;
        public static readonly float percentMeleeResistBonus = meleeResistBonus.Value / 100f;
        public static readonly float percentMeleeResistBonusExtraStacks = meleeResistBonusExtraStacks.Value / 100f;
        public static readonly float percentCooldownRefreshIntervalReduction = cooldownRefreshIntervalReduction.Value / 100f;

        internal static void Init()
        {
            // Normal Variant
            itemDef = ItemManager.GenerateItem("AdaptiveHelm", [ItemTag.Damage, ItemTag.Utility, ItemTag.CanBeTemporary], ItemManager.TacticTier.Normal);
            radiantDef = ItemManager.GenerateItem("Radiant_AdaptiveHelm", [ItemTag.Damage, ItemTag.Utility, ItemTag.CanBeTemporary], ItemManager.TacticTier.Radiant);

            cooldownResetBuff = Utilities.GenerateBuffDef("CooldownReset", AssetManager.bundle.LoadAsset<Sprite>("AdaptiveHelm.png"), false, false, false, true);
            ContentAddition.AddBuffDef(cooldownResetBuff);
            radiantCooldownResetBuff = Utilities.GenerateBuffDef("Radiant_CooldownReset", AssetManager.bundle.LoadAsset<Sprite>("Radiant_AdaptiveHelm.png"), false, false, false, true);
            ContentAddition.AddBuffDef(radiantCooldownResetBuff);

            Utilities.RegisterRadiantUpgrade(itemDef, radiantDef);

            Hooks(itemDef, ItemManager.TacticTier.Normal, cooldownResetBuff);
            Hooks(radiantDef, ItemManager.TacticTier.Radiant, radiantCooldownResetBuff);
        }

        public static void Hooks(ItemDef def, ItemManager.TacticTier tier, BuffDef cooldownReset)
        {
            float radiantMultiplier = tier.Equals(ItemManager.TacticTier.Radiant) ? ConfigManager.Scaling.radiantItemStatMultiplier : 1f;

            On.RoR2.CharacterBody.FixedUpdate += (orig, self) =>
            {
                orig(self);

                if (self && self.inventory && Utilities.IsRangedBodyPrefab(self.gameObject))
                {
                    int itemCount = self.inventory.GetItemCountEffective(def);
                    if (itemCount > 0 && !self.HasBuff(cooldownReset))
                    {
                        self.AddTimedBuff(cooldownReset, Utilities.GetReverseExponentialStacking(cooldownRefreshInterval.Value, percentCooldownRefreshIntervalReduction, itemCount));
                    }
                }
            };

            RecalculateStatsAPI.GetStatCoefficients += (sender, args) =>
            {
                if (sender && sender.inventory)
                {
                    int count = sender.inventory.GetItemCountEffective(def);
                    if (count > 0)
                    {
                        args.armorAdd += Utilities.GetLinearStacking(commonStatBoost.Value * radiantMultiplier, 0f, count);
                        args.baseShieldAdd += sender.healthComponent.fullHealth * Utilities.GetLinearStacking(percentCommonStatBoost * radiantMultiplier, 0f, count);

                        if (Utilities.IsMeleeBodyPrefab(sender.gameObject))
                        {
                            args.armorTotalMult *= 1 + Utilities.GetLinearStacking(percentMeleeResistBonus * radiantMultiplier, percentMeleeResistBonusExtraStacks * radiantMultiplier, count);
                            args.shieldTotalMult *= 1 + Utilities.GetLinearStacking(percentMeleeResistBonus * radiantMultiplier, percentMeleeResistBonusExtraStacks * radiantMultiplier, count);
                        }

                        if (Utilities.IsRangedBodyPrefab(sender.gameObject))
                        {
                            args.baseDamageAdd += Utilities.GetLinearStacking(rangedDamageBonus.Value * radiantMultiplier, rangedDamageBonusExtraStacks.Value * radiantMultiplier, count);
                        }
                    }
                }
            };

            On.RoR2.CharacterBody.OnBuffFinalStackLost += (orig, self, buffDef) =>
            {
                orig(self, buffDef);

                if (self && self.skillLocator && buffDef == cooldownReset)
                {
                    self.skillLocator.ResetSkills();

                    //if (self.inventory)
                    //{
                    //    int itemCount = self.inventory.GetItemCountEffective(def);
                    //    if (itemCount > 0 && Utilities.IsRangedBodyPrefab(self.gameObject))
                    //    {
                    //        self.AddTimedBuff(cooldownResetBuff, Utilities.GetReverseExponentialStacking(cooldownRefreshInterval.Value, percentCooldownRefreshIntervalReduction, itemCount));
                    //    }
                    //}
                }
            };

            GameEventManager.OnTakeDamage += (damageReport) =>
            {
                CharacterBody vicBody = damageReport.victimBody;
                if (vicBody && vicBody.inventory && vicBody.skillLocator && Utilities.IsMeleeBodyPrefab(vicBody.gameObject))
                {
                    int count = vicBody.inventory.GetItemCountEffective(def);
                    if (count > 0)
                    {
                        vicBody.skillLocator.DeductCooldownFromAllSkillsServer(cooldownRefundOnTakeDamage.Value * radiantMultiplier);
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
