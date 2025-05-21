using R2API;
using RoR2;
using System.Collections.Generic;
using UnityEngine;

namespace RiskOfTactics
{
    internal class GiantsBelt
    {
        public static ItemDef itemDef;

        // Gain health.
        public static ConfigurableValue<bool> isEnabled = new(
            "Item: Giants Belt",
            "Enabled",
            true,
            "Whether or not the item is enabled.",
            new List<string>()
            {
                "ITEM_GIANTSBELT_DESC"
            }
        );
        public static ConfigurableValue<float> baseHealthBonus = new(
            "Item: Giants Belt",
            "Health",
            100f,
            "Health gained when holding this item.",
            new List<string>()
            {
                "ITEM_GIANTSBELT_DESC"
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

            itemDef.name = "GIANTSBELT";
            itemDef.AutoPopulateTokens();

            Utils.SetItemTier(itemDef, ItemTier.Tier1);

            itemDef.pickupIconSprite = AssetHandler.bundle.LoadAsset<Sprite>("GiantsBelt.png");
            itemDef.pickupModelPrefab = AssetHandler.bundle.LoadAsset<GameObject>("GiantsBelt.prefab");
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
                        args.baseHealthAdd += baseHealthBonus.Value;
                    }
                }
            };
        }
    }
}
