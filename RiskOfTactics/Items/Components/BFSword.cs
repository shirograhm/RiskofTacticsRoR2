using R2API;
using RoR2;
using System.Collections.Generic;
using UnityEngine;

namespace RiskOfTactics
{
    internal class BFSword
    {
        public static ItemDef itemDef;

        // Gain percent damage.
        public static ConfigurableValue<bool> isEnabled = new(
            "Item: B.F. Sword",
            "Enabled",
            true,
            "Whether or not the item is enabled.",
            new List<string>()
            {
                "ITEM_BFSWORD_DESC"
            }
        );
        public static ConfigurableValue<float> damageBonus = new(
            "Item: B.F. Sword",
            "Percent Damage",
            10f,
            "Percent damage gained when holding this item.",
            new List<string>()
            {
                "ITEM_BFSWORD_DESC"
            }
        );
        private static readonly float percentDamageBonus = damageBonus / 100f;

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

            itemDef.name = "BFSWORD";
            itemDef.AutoPopulateTokens();

            Utils.SetItemTier(itemDef, ItemTier.Tier1);

            itemDef.pickupIconSprite = AssetHandler.bundle.LoadAsset<Sprite>("BFSword.png");
            itemDef.pickupModelPrefab = AssetHandler.bundle.LoadAsset<GameObject>("BFSword.prefab");
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
                        args.damageMultAdd += percentDamageBonus * itemCount;
                    }
                }
            };
        }
    }
}
