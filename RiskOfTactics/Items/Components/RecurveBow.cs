using R2API;
using RoR2;
using System.Collections.Generic;
using UnityEngine;

namespace RiskOfTactics
{
    internal class RecurveBow
    {
        public static ItemDef itemDef;

        // Gain attack speed.
        public static ConfigurableValue<bool> isEnabled = new(
            "Item: Recurve Bow",
            "Enabled",
            true,
            "Whether or not the item is enabled.",
            new List<string>()
            {
                "ITEM_RECURVEBOW_DESC"
            }
        );
        public static ConfigurableValue<float> attackSpeedBonus = new(
            "Item: Recurve Bow",
            "Attack Speed",
            10f,
            "Percent attack speed gained when holding this item.",
            new List<string>()
            {
                "ITEM_RECURVEBOW_DESC"
            }
        );
        private static readonly float percentAttackSpeedBonus = attackSpeedBonus / 100f;

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

            itemDef.name = "RECURVEBOW";
            itemDef.AutoPopulateTokens();

            Utils.SetItemTier(itemDef, ItemTier.Tier2);

            itemDef.pickupIconSprite = AssetHandler.bundle.LoadAsset<Sprite>("RecurveBow.png");
            itemDef.pickupModelPrefab = AssetHandler.bundle.LoadAsset<GameObject>("RecurveBow.prefab");
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
                        args.attackSpeedMultAdd += percentAttackSpeedBonus;
                    }
                }
            };
        }
    }
}
