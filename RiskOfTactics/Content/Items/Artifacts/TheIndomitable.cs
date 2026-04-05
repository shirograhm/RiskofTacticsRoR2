using R2API;
using RiskOfTactics.Managers;
using RoR2;
using UnityEngine.Networking;

namespace RiskOfTactics.Content.Items.Artifacts
{
    internal class TheIndomitable
    {
        public static ItemDef itemDef;

        // Gain max health. You cannot be Stunned. On-hit, pull enemies towards you.
        public static ConfigurableValue<bool> isEnabled = new(
            "Item: Zhonyas Paradox",
            "Enabled",
            true,
            "Whether or not the item is enabled.",
            ["ITEM_ROT_ZHONYASPARADOX_DESC"]
        );
        public static ConfigurableValue<float> maxHealth = new(
            "Item: The Indomitable",
            "Max Health",
            20f,
            "Percent max health increase.",
            ["ITEM_ROT_THEINDOMITABLE_DESC"]
        );
        public static ConfigurableValue<float> maxHealthExtraStacks = new(
            "Item: The Indomitable",
            "Max Health Extra Stacks",
            20f,
            "Percent max health increase with extra stacks.",
            ["ITEM_ROT_THEINDOMITABLE_DESC"]
        );
        public static ConfigurableValue<float> movespeedLost = new(
            "Item: The Indomitable",
            "Movespeed Lost",
            20f,
            "Percent movement speed lost.",
            ["ITEM_ROT_THEINDOMITABLE_DESC"]
        );
        public static ConfigurableValue<float> movespeedLostExtraStacks = new(
            "Item: The Indomitable",
            "Movespeed Lost Extra Stacks",
            20f,
            "Percent movement speed lost with extra stacks.",
            ["ITEM_ROT_THEINDOMITABLE_DESC"]
        );
        public static ConfigurableValue<float> forceOnHit = new(
            "Item: The Indomitable",
            "Force On Hit",
            1f,
            "Force applied to enemies on hit.",
            ["ITEM_ROT_THEINDOMITABLE_DESC"]
        );
        public static readonly float percentMaxHealth = maxHealth.Value / 100f;
        public static readonly float percentMaxHealthExtraStacks = maxHealthExtraStacks.Value / 100f;
        public static readonly float percentMovespeedLost = movespeedLost.Value / 100f;
        public static readonly float percentMovespeedLostExtraStacks = movespeedLostExtraStacks.Value / 100f;

        internal static void Init()
        {
            itemDef = ItemManager.GenerateItem("TheIndomitable", [ItemTag.Utility, ItemTag.CanBeTemporary], ItemManager.TacticTier.Artifact);

            Hooks();
        }

        public static void Hooks()
        {
            RecalculateStatsAPI.GetStatCoefficients += (self, args) =>
            {
                if (self && self.inventory)
                {
                    int count = self.inventory.GetItemCountEffective(itemDef);
                    if (count > 0)
                    {
                        args.healthTotalMult *= 1 + Utilities.GetLinearStacking(percentMaxHealth, percentMaxHealthExtraStacks, count);
                        args.moveSpeedTotalMult *= 1 - Utilities.GetLinearStacking(percentMovespeedLost, percentMovespeedLostExtraStacks, count);
                    }
                }
            };

            On.RoR2.SetStateOnHurt.SetStun += (orig, self, damageInfo) =>
            {
                if (NetworkServer.active)
                {
                    if (self.GetComponentInParent<CharacterBody>() is CharacterBody body && body.inventory)
                    {
                        int count = body.inventory.GetItemCountEffective(itemDef);
                        // Prevent stuns by not calling the original method
                        if (count > 0) return;
                    }
                }

                // Call the original method for normal behavior
                orig(self, damageInfo);
            };
        }
    }
}
