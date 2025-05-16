using R2API;
using RoR2;
using System.Collections.Generic;
using UnityEngine;

namespace RiskOfTactics
{
    internal class ChainVest
    {
        public static ItemDef itemDef;

        // Gain armor.
        public static ConfigurableValue<bool> isEnabled = new(
            "Item: Chain Vest",
            "Enabled",
            true,
            "Whether or not the item is enabled.",
            new List<string>()
            {
                "ITEM_CHAINVEST_DESC"
            }
        );
        public static ConfigurableValue<float> armorBonus = new(
            "Item: Chain Vest",
            "Armor",
            20f,
            "Armor gained when holding this item.",
            new List<string>()
            {
                "ITEM_CHAINVEST_DESC"
            }
        );

        internal static void Init()
        {
            GenerateItem();

            ItemDisplayRuleDict displayRules = new ItemDisplayRuleDict(null);
            ItemAPI.Add(new CustomItem(itemDef, displayRules));

            Hooks();
        }

        private static void GenerateItem()
        {
            itemDef = ScriptableObject.CreateInstance<ItemDef>();

            itemDef.name = "CHAINVEST";
            itemDef.AutoPopulateTokens();

            Utils.SetItemTier(itemDef, ItemTier.Tier2);

            itemDef.pickupIconSprite = AssetHandler.bundle.LoadAsset<Sprite>("ChainVest.png");
            itemDef.pickupModelPrefab = AssetHandler.bundle.LoadAsset<GameObject>("ChainVest.prefab");
            itemDef.canRemove = true;
            itemDef.hidden = false;

            itemDef.tags = new ItemTag[]
            {
                ItemTag.Utility
            };
        }

        public static void Hooks()
        {
            RecalculateStatsAPI.GetStatCoefficients += (sender, args) =>
            {
                if (sender && sender.inventory)
                {
                    int itemCount = sender.inventory.GetItemCount(itemDef);
                    if (itemCount > 0)
                    {
                        args.armorAdd += armorBonus.Value;
                    }
                }
            };
        }
    }
}
