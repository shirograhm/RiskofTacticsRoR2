using R2API;
using RoR2;
using System.Collections.Generic;
using UnityEngine;

namespace RiskOfTactics
{
    internal class NeedlesslyLargeRod
    {
        public static ItemDef itemDef;

        // Gain base damage.
        public static ConfigurableValue<bool> isEnabled = new(
            "Item: Needlessly Large Rod",
            "Enabled",
            true,
            "Whether or not the item is enabled.",
            new List<string>()
            {
                "ITEM_NEEDLESSLYLARGEROD_DESC"
            }
        );
        public static ConfigurableValue<float> baseDamageBonus = new(
            "Item: Needlessly Large Rod",
            "Flat Damage",
            10f,
            "Flat damage gained when holding this item.",
            new List<string>()
            {
                "ITEM_NEEDLESSLYLARGEROD_DESC"
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

            itemDef.name = "NEEDLESSLYLARGEROD";
            itemDef.AutoPopulateTokens();

            Utils.SetItemTier(itemDef, ItemTier.Tier2);

            itemDef.pickupIconSprite = AssetHandler.bundle.LoadAsset<Sprite>("NeedlesslyLargeRod.png");
            itemDef.pickupModelPrefab = AssetHandler.bundle.LoadAsset<GameObject>("NeedlesslyLargeRod.prefab");
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
                        args.baseDamageAdd += baseDamageBonus.Value;
                    }
                }
            };
        }
    }
}
