using R2API;
using RoR2;
using System.Collections.Generic;
using UnityEngine;

namespace RiskOfTactics
{
    internal class SparringGloves
    {
        public static ItemDef itemDef;

        // Gain crit chance.
        public static ConfigurableValue<bool> isEnabled = new(
            "Item: Sparring Gloves",
            "Enabled",
            true,
            "Whether or not the item is enabled.",
            new List<string>()
            {
                "ITEM_SPARRINGGLOVES_DESC"
            }
        );
        public static ConfigurableValue<float> critChanceBonus = new(
            "Item: Sparring Gloves",
            "Crit Chance",
            20f,
            "Crit chance gained when holding this item.",
            new List<string>()
            {
                "ITEM_SPARRINGGLOVES_DESC"
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

            itemDef.name = "SPARRINGGLOVES";
            itemDef.AutoPopulateTokens();

            Utils.SetItemTier(itemDef, ItemTier.Tier2);

            itemDef.pickupIconSprite = AssetHandler.bundle.LoadAsset<Sprite>("SparringGloves.png");
            itemDef.pickupModelPrefab = AssetHandler.bundle.LoadAsset<GameObject>("SparringGloves.prefab");
            itemDef.canRemove = true;
            itemDef.hidden = false;

            itemDef.tags = new ItemTag[]
            {
                ItemTag.Damage
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
                        args.critAdd += critChanceBonus.Value;
                    }
                }
            };
        }
    }
}
