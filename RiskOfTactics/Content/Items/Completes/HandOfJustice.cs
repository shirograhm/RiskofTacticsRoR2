using R2API;
using RiskOfTactics.Helpers;
using RoR2;
using UnityEngine;

namespace RiskOfTactics.Content.Items.Completes
{
    class HandOfJustice
    {
        public static ItemDef itemDef;
        public static BuffDef aboveHalfBuff;
        public static BuffDef belowHalfBuff;

        public static ItemDef radiantDef;

        // Gain crit chance. Grants damage and omnivamp that scales with your current health.
        public static ConfigurableValue<bool> isEnabled = new(
            "Item: Hand Of Justice",
            "Enabled",
            true,
            "Whether or not the item is enabled.",
            ["ITEM_ROT_HANDOFJUSTICE_DESC"]
        );
        public static ConfigurableValue<float> critChanceBonus = new(
            "Item: Hand Of Justice",
            "Crit Chance",
            5f,
            "Crit chance gained when holding this item.",
            ["ITEM_ROT_HANDOFJUSTICE_DESC"],
            false
        );
        public static ConfigurableValue<float> scaledBonusDamageEffect = new(
            "Item: Hand Of Justice",
            "Bonus Damage Percent",
            10f,
            "Percent BASE damage scaling effect gained for this item.",
            ["ITEM_ROT_HANDOFJUSTICE_DESC"],
            true
        );
        public static ConfigurableValue<float> scaledBonusDamageEffectExtraStacks = new(
            "Item: Hand Of Justice",
            "Bonus Damage Percent Per Stack",
            10f,
            "Percent BASE damage scaling effect gained for extra stacks of this item.",
            ["ITEM_ROT_HANDOFJUSTICE_DESC"],
            true
        );
        public static ConfigurableValue<float> omnivampEffect = new(
            "Item: Hand Of Justice",
            "Omnivamp",
            10f,
            "Percent omnivamp scaling effect gained for this item.",
            ["ITEM_ROT_HANDOFJUSTICE_DESC"],
            true
        );
        public static ConfigurableValue<float> omnivampEffectExtraStacks = new(
            "Item: Hand Of Justice",
            "Omnivamp Per Stack",
            10f,
            "Percent omnivamp scaling effect gained for extra stacks of this item.",
            ["ITEM_ROT_HANDOFJUSTICE_DESC"],
            true
        );
        private static readonly float percentScaledBonusDamageEffect = scaledBonusDamageEffect.Value / 100f;
        private static readonly float percentScaledBonusDamageEffectExtraStacks = scaledBonusDamageEffectExtraStacks.Value / 100f;
        private static readonly float percentOmnivampEffect = omnivampEffect.Value / 100f;
        private static readonly float percentOmnivampEffectExtraStacks = omnivampEffectExtraStacks.Value / 100f;

        internal static void Init()
        {
            itemDef = ItemHelper.GenerateItem("HandOfJustice", [ItemTag.Damage, ItemTag.Healing, ItemTag.CanBeTemporary], ItemHelper.TacticTier.Normal);
            radiantDef = ItemHelper.GenerateItem("Radiant_HandOfJustice", [ItemTag.Damage, ItemTag.Healing, ItemTag.CanBeTemporary], ItemHelper.TacticTier.Radiant);

            aboveHalfBuff = Utilities.GenerateBuffDef("Above", AssetHandler.bundle.LoadAsset<Sprite>("HoJ Damage.png"), false, false, false, false);
            ContentAddition.AddBuffDef(aboveHalfBuff);
            belowHalfBuff = Utilities.GenerateBuffDef("Below", AssetHandler.bundle.LoadAsset<Sprite>("HoJ Omnivamp.png"), false, false, false, false);
            ContentAddition.AddBuffDef(belowHalfBuff);

            Utilities.RegisterVoidPair(itemDef, radiantDef);

            Hooks(itemDef, ItemHelper.TacticTier.Normal);
            Hooks(radiantDef, ItemHelper.TacticTier.Radiant);
        }

        public static void Hooks(ItemDef def, ItemHelper.TacticTier tier)
        {
            float radiantMultiplier = tier.Equals(ItemHelper.TacticTier.Radiant) ? ConfigManager.Scaling.radiantItemStatMultiplier : 1f;

            RecalculateStatsAPI.GetStatCoefficients += (sender, args) =>
            {
                if (sender && sender.inventory)
                {
                    int count = sender.inventory.GetItemCountEffective(def);
                    if (count > 0)
                    {
                        args.critAdd += critChanceBonus.Value;

                        int multiplier = sender.HasBuff(aboveHalfBuff) ? 2 : 1;
                        args.damageTotalMult *= 1 + Utilities.GetLinearStacking(percentScaledBonusDamageEffect * radiantMultiplier, percentScaledBonusDamageEffectExtraStacks * radiantMultiplier, count) * multiplier;
                    }
                }
            };

            GenericGameEvents.OnTakeDamage += (damageReport) =>
            {
                CharacterBody vicBody = damageReport.victimBody;
                CharacterBody atkBody = damageReport.attackerBody;

                if (vicBody && atkBody && atkBody.inventory)
                {
                    int count = atkBody.inventory.GetItemCountEffective(def);
                    if (count > 0 && !Utilities.OnSameTeam(vicBody, atkBody) && atkBody.healthComponent)
                    {
                        int multiplier = atkBody.HasBuff(belowHalfBuff) ? 2 : 1;

                        float healAmount = damageReport.damageInfo.damage * Utilities.GetHyperbolicStacking(percentOmnivampEffect * radiantMultiplier, percentOmnivampEffectExtraStacks * radiantMultiplier, count) * multiplier;
                        atkBody.healthComponent.Heal(healAmount, new ProcChainMask());

                        Utilities.SpawnHealEffect(atkBody);
                    }
                }
            };

            On.RoR2.CharacterBody.FixedUpdate += (orig, self) =>
            {
                if (self && self.inventory && self.healthComponent)
                {
                    int count = self.inventory.GetItemCountEffective(def);
                    if (count > 0)
                    {
                        if (self.healthComponent.combinedHealthFraction >= 0.50f)
                        {
                            if (!self.HasBuff(aboveHalfBuff)) self.AddBuff(aboveHalfBuff);
                            if (self.HasBuff(belowHalfBuff)) self.RemoveBuff(belowHalfBuff);
                        }
                        else
                        {
                            if (!self.HasBuff(belowHalfBuff)) self.AddBuff(belowHalfBuff);
                            if (self.HasBuff(aboveHalfBuff)) self.RemoveBuff(aboveHalfBuff);
                        }
                    }
                    else
                    {
                        self.RemoveBuff(aboveHalfBuff);
                        self.RemoveBuff(belowHalfBuff);
                    }
                }
                orig(self);
            };
        }
    }
}
