using R2API;
using RoR2;
using System.Collections.Generic;
using UnityEngine;

namespace RiskOfTactics
{
    internal class NegatronCloak
    {
        public static ItemDef itemDef;

        // Gain base shield.
        public static ConfigurableValue<bool> isEnabled = new(
            "Item: Negatron Cloak",
            "Enabled",
            true,
            "Whether or not the item is enabled.",
            new List<string>()
            {
                "ITEM_NEGATRONCLOAK_DESC"
            }
        );
        public static ConfigurableValue<float> shieldBonus = new(
            "Item: Negatron Cloak",
            "Percent Shield",
            10f,
            "Percent shield gained when holding this item.",
            new List<string>()
            {
                "ITEM_NEGATRONCLOAK_DESC"
            }
        );
        public static readonly float percentShieldBonus = shieldBonus.Value / 100f;

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

            itemDef.name = "NEGATRONCLOAK";
            itemDef.AutoPopulateTokens();

            Utils.SetItemTier(itemDef, ItemTier.Tier1);

            itemDef.pickupIconSprite = AssetHandler.bundle.LoadAsset<Sprite>("NegatronCloak.png");
            itemDef.pickupModelPrefab = AssetHandler.bundle.LoadAsset<GameObject>("NegatronCloak.prefab");
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
                        args.baseShieldAdd += sender.healthComponent.fullHealth * percentShieldBonus * itemCount;
                    }
                }
            };
        }
    }
}
