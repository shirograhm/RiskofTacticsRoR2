using R2API;
using RoR2;
using System.Collections.Generic;
using UnityEngine;

namespace RiskOfTactics
{
    internal class TearOfTheGoddess
    {
        public static ItemDef itemDef;

        // Gain cooldown reduction.
        public static ConfigurableValue<bool> isEnabled = new(
            "Item: Tear Of The Goddess",
            "Enabled",
            true,
            "Whether or not the item is enabled.",
            new List<string>()
            {
                "ITEM_TEAROFTHEGODDESS_DESC"
            }
        );
        public static ConfigurableValue<float> cooldownReductionBonus = new(
            "Item: Tear Of The Goddess",
            "Cooldown Reduction",
            10f,
            "Cooldown reduction gained when holding this item.",
            new List<string>()
            {
                "ITEM_TEAROFTHEGODDESS_DESC"
            }
        );
        private static readonly float percentCooldownReductionBonus = cooldownReductionBonus.Value / 100f;

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

            itemDef.name = "TEAROFTHEGODDESS";
            itemDef.AutoPopulateTokens();

            Utils.SetItemTier(itemDef, ItemTier.Tier2);

            itemDef.pickupIconSprite = AssetHandler.bundle.LoadAsset<Sprite>("TearOfTheGoddess.png");
            itemDef.pickupModelPrefab = AssetHandler.bundle.LoadAsset<GameObject>("TearOfTheGoddess.prefab");
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
                        args.cooldownMultAdd -= percentCooldownReductionBonus;
                    }
                }
            };
        }
    }
}
