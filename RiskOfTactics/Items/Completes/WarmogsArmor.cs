using R2API;
using R2API.Networking;
using R2API.Networking.Interfaces;
using RoR2;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

namespace RiskOfTactics
{
    class WarmogsArmor
    {
        public static ItemDef itemDef;

        public static ConfigurableValue<bool> isEnabled = new(
            "Item: Warmogs Armor",
            "Enabled",
            true,
            "Whether or not the item is enabled.",
            new List<string>()
            {
                "ITEM_WARMOGSARMOR_DESC"
            }
        );
        public static ConfigurableValue<float> healthBonus = new(
            "Item: Warmogs Armor",
            "Flat Health",
            250f,
            "Max health bonus when holding this item.",
            new List<string>()
            {
                "ITEM_WARMOGSARMOR_DESC"
            }
        );
        public static ConfigurableValue<float> percentHealthBonus = new(
            "Item: Warmogs Armor",
            "Percent Health",
            12f,
            "Percent health bonus when holding this item.",
            new List<string>()
            {
                "ITEM_WARMOGSARMOR_DESC"
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

            itemDef.name = "WARMOGSARMOR";
            itemDef.AutoPopulateTokens();

            Utils.SetItemTier(itemDef, ItemTier.Tier3);

            itemDef.pickupIconSprite = AssetHandler.bundle.LoadAsset<Sprite>("WarmogsArmor.png");
            itemDef.pickupModelPrefab = AssetHandler.bundle.LoadAsset<GameObject>("WarmogsArmor.prefab");
            itemDef.canRemove = true;
            itemDef.hidden = false;

            itemDef.tags = new ItemTag[]
            {
                ItemTag.Healing,
                ItemTag.Utility
            };
        }

        public static void Hooks()
        {
            RecalculateStatsAPI.GetStatCoefficients += (sender, args) =>
            {
                if (sender && sender.inventory)
                {
                    int count = sender.inventory.GetItemCount(itemDef);
                    if (count > 0)
                    {
                        args.baseHealthAdd += healthBonus.Value;
                        args.healthMultAdd += percentHealthBonus.Value / 100f;
                    }
                }
            };
        }
    }
}
