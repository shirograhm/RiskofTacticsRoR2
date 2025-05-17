using R2API;
using RoR2;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace RiskOfTactics
{
    class SteadfastHeart
    {
        public static ItemDef itemDef;

        //
        public static ConfigurableValue<bool> isEnabled = new(
            "Item: Steadfast Heart",
            "Enabled",
            true,
            "Whether or not the item is enabled.",
            new List<string>()
            {
                "ITEM_STEADFASTHEART_DESC"
            }
        );
        public static ConfigurableValue<float> armorBonus = new(
            "Item: Steadfast Heart",
            "Armor",
            20f,
            "Armor gained when holding this item.",
            new List<string>()
            {
                "ITEM_STEADFASTHEART_DESC"
            }
        );
        public static ConfigurableValue<float> critChanceBonus = new(
            "Item: Steadfast Heart",
            "Crit Chance",
            20f,
            "Crit chance gained when holding this item.",
            new List<string>()
            {
                "ITEM_STEADFASTHEART_DESC"
            }
        );
        public static ConfigurableValue<float> healthBonus = new(
            "Item: Steadfast Heart",
            "Health",
            250f,
            "Max health gained when holding this item.",
            new List<string>()
            {
                "ITEM_STEADFASTHEART_DESC"
            }
        );
        public static ConfigurableValue<float> durabilityBonus = new(
            "Item: Steadfast Heart",
            "Durability Bonus",
            10f,
            "Passive durability gained when holding this item.",
            new List<string>()
            {
                "ITEM_STEADFASTHEART_DESC"
            }
        );
        public static ConfigurableValue<float> durabilityBonusAboveHalf = new(
            "Item: Steadfast Heart",
            "Durability Bonus Healthy",
            18f,
            "Passive durability gained when holding this item above 50% max health.",
            new List<string>()
            {
                "ITEM_STEADFASTHEART_DESC"
            }
        );
        private static readonly float percentDurabilityBonus = durabilityBonus.Value / 100f;
        private static readonly float percentDurabilityBonusAboveHalf = durabilityBonusAboveHalf.Value / 100f;

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

            itemDef.name = "STEADFASTHEART";
            itemDef.AutoPopulateTokens();

            Utils.SetItemTier(itemDef, ItemTier.Tier3);

            itemDef.pickupIconSprite = AssetHandler.bundle.LoadAsset<Sprite>("SteadfastHeart.png");
            itemDef.pickupModelPrefab = AssetHandler.bundle.LoadAsset<GameObject>("SteadfastHeart.prefab");
            itemDef.canRemove = true;
            itemDef.hidden = false;

            itemDef.tags = new ItemTag[]
            {
                ItemTag.Damage,
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
                        args.armorAdd += armorBonus.Value;
                        args.critAdd += critChanceBonus.Value;
                        args.baseHealthAdd += healthBonus.Value;
                    }
                }
            };

            GenericGameEvents.BeforeTakeDamage += (damageInfo, attackerInfo, victimInfo) =>
            {
                CharacterBody attackerBody = attackerInfo.body;
                CharacterBody victimBody = victimInfo.body;
                if (attackerBody && victimBody && victimBody.inventory)
                {
                    int count = victimBody.inventory.GetItemCount(itemDef);
                    if (count > 0 && victimBody.master)
                    {
                        float durabilityPercent = victimBody.healthComponent.combinedHealthFraction >= 0.50f ? percentDurabilityBonusAboveHalf : percentDurabilityBonus;
                        damageInfo.damage *= 1 - durabilityPercent;
                    }
                }
            };
        }
    }
}
