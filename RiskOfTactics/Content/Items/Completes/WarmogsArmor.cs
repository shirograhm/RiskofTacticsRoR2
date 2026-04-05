using R2API;
using RiskOfTactics.Managers;
using RoR2;

namespace RiskOfTactics.Content.Items.Completes
{
    class WarmogsArmor
    {
        public static ItemDef itemDef;
        public static ItemDef radiantDef;

        // Gain health. Periodically heal for a portion of your max HP.
        public static ConfigurableValue<bool> isEnabled = new(
            "Item: Warmogs Armor",
            "Enabled",
            true,
            "Whether or not the item is enabled.",
            ["ITEM_ROT_WARMOGSARMOR_DESC"]
        );
        public static ConfigurableValue<float> percentHealth = new(
            "Item: Warmogs Armor",
            "Percent Health",
            18f,
            "Percent max health bonus when holding this item.",
            ["ITEM_ROT_WARMOGSARMOR_DESC"],
            true
        );
        public static ConfigurableValue<float> flatHealth = new(
            "Item: Warmogs Armor",
            "Flat Health",
            25f,
            "Flat max health bonus when holding this item.",
            ["ITEM_ROT_WARMOGSARMOR_DESC"],
            true
        );
        public static ConfigurableValue<float> flatHealthExtraStacks = new(
            "Item: Warmogs Armor",
            "Flat Health Extra Stacks",
            50f,
            "Flat max health bonus per extra stack of this item.",
            ["ITEM_ROT_WARMOGSARMOR_DESC"],
            true
        );
        public static readonly float percentHealthBonus = percentHealth.Value / 100f;

        internal static void Init()
        {
            itemDef = ItemManager.GenerateItem("WarmogsArmor", [ItemTag.Healing, ItemTag.CanBeTemporary], ItemManager.TacticTier.Normal);
            radiantDef = ItemManager.GenerateItem("Radiant_WarmogsArmor", [ItemTag.Healing, ItemTag.CanBeTemporary], ItemManager.TacticTier.Radiant);

            if (ConfigManager.Scaling.useRadiantAutoConversion) Utilities.RegisterRadiantUpgrade(itemDef, radiantDef);

            Hooks(itemDef, ItemManager.TacticTier.Normal);
            Hooks(radiantDef, ItemManager.TacticTier.Radiant);
        }

        public static void Hooks(ItemDef def, ItemManager.TacticTier tier)
        {
            float radiantMultiplier = tier.Equals(ItemManager.TacticTier.Radiant) ? ConfigManager.Scaling.radiantItemStatMultiplier : 1f;

            RecalculateStatsAPI.GetStatCoefficients += (sender, args) =>
            {
                if (sender && sender.inventory)
                {
                    int count = sender.inventory.GetItemCountEffective(def);
                    if (count > 0)
                    {
                        args.baseHealthAdd += Utilities.GetLinearStacking(flatHealth.Value * radiantMultiplier, flatHealthExtraStacks.Value * radiantMultiplier, count);
                        args.healthTotalMult *= 1 + percentHealthBonus * radiantMultiplier;
                    }
                }
            };
        }
    }
}
