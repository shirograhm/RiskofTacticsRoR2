using R2API;
using RiskOfTactics.Managers;
using RoR2;
using UnityEngine;

namespace RiskOfTactics.Content.Items.Completes
{
    class Bloodthirster
    {
        public static ItemDef itemDef;
        public static BuffDef satedBuff;

        public static ItemDef radiantDef;

        // Periodically gain a barrier when taking damage at low health.
        public static ConfigurableValue<bool> isEnabled = new(
            "Item: Bloodthirster",
            "Enabled",
            true,
            "Whether or not the item is enabled.",
            ["ITEM_ROT_BLOODTHIRSTER_DESC"]
        );
        public static ConfigurableValue<float> effectCooldown = new(
            "Item: Bloodthirster",
            "Effect Cooldown",
            20f,
            "Cooldown of this item's effect.",
            ["ITEM_ROT_BLOODTHIRSTER_DESC"],
            false
        );
        public static ConfigurableValue<float> barrierTriggerHP = new(
            "Item: Bloodthirster",
            "HP Threshold",
            20f,
            "Threshold needed to fall below in order to trigger this item's effect.",
            ["ITEM_ROT_BLOODTHIRSTER_DESC"],
            false
        );
        public static ConfigurableValue<float> barrierSize = new(
            "Item: Bloodthirster",
            "Percent Barrier",
            25f,
            "Percent max HP barrier given when this item is procced.",
            ["ITEM_ROT_BLOODTHIRSTER_DESC"],
            true
        );
        public static ConfigurableValue<float> barrierSizeExtraStacks = new(
            "Item: Bloodthirster",
            "Percent Barrier Extra Stacks",
            50f,
            "Percent max HP barrier given when extra stacks of item are procced.",
            ["ITEM_ROT_BLOODTHIRSTER_DESC"],
            true
        );
        private static readonly float percentBarrierTriggerHP = barrierTriggerHP.Value / 100f;
        private static readonly float percentBarrierSize = barrierSize.Value / 100f;
        private static readonly float percentBarrierSizeExtraStacks = barrierSizeExtraStacks.Value / 100f;

        internal static void Init()
        {
            itemDef = ItemManager.GenerateItem("Bloodthirster", [ItemTag.Healing, ItemTag.Utility, ItemTag.LowHealth, ItemTag.CanBeTemporary], ItemManager.TacticTier.Normal);
            radiantDef = ItemManager.GenerateItem("Radiant_Bloodthirster", [ItemTag.Healing, ItemTag.Utility, ItemTag.LowHealth, ItemTag.CanBeTemporary], ItemManager.TacticTier.Radiant);

            satedBuff = Utilities.GenerateBuffDef("Sated", AssetManager.bundle.LoadAsset<Sprite>("Sated.png"), false, false, false, true);
            ContentAddition.AddBuffDef(satedBuff);

            //Utilities.RegisterRadiantUpgrade(itemDef, radiantDef);

            Hooks(itemDef, ItemManager.TacticTier.Normal);
            Hooks(radiantDef, ItemManager.TacticTier.Radiant);
        }

        public static void Hooks(ItemDef def, ItemManager.TacticTier tier)
        {
            float radiantMultiplier = tier.Equals(ItemManager.TacticTier.Radiant) ? ConfigManager.Scaling.radiantItemStatMultiplier : 1f;

            GameEventManager.OnTakeDamage += (damageReport) =>
            {
                CharacterBody vicBody = damageReport.victimBody;
                if (vicBody && vicBody.inventory && vicBody.healthComponent)
                {
                    // Low health barrier effect
                    int vicCount = vicBody.inventory.GetItemCountEffective(def);
                    if (vicCount > 0 && !vicBody.HasBuff(satedBuff) && vicBody.healthComponent.combinedHealthFraction < percentBarrierTriggerHP)
                    {
                        vicBody.healthComponent.AddBarrier(vicBody.healthComponent.fullCombinedHealth * Utilities.GetLinearStacking(percentBarrierSize * radiantMultiplier, percentBarrierSizeExtraStacks * radiantMultiplier, vicCount));
                        vicBody.AddTimedBuff(satedBuff, effectCooldown);
                    }
                }
            };
        }
    }
}
