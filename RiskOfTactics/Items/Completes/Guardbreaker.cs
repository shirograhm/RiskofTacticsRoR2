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
    class Guardbreaker
    {
        public static ItemDef itemDef;

        // Gain base damage, attack speed, crit, health, and damage amp. Deal 30% more damage to enemies with active shields.
        public static ConfigurableValue<bool> isEnabled = new(
            "Item: Guardbreaker",
            "Enabled",
            true,
            "Whether or not the item is enabled.",
            new List<string>()
            {
                "ITEM_GUARDBREAKER_DESC"
            }
        );
        public static ConfigurableValue<float> flatDamageBonus = new(
            "Item: Guardbreaker",
            "Flat Damage",
            4f,
            "Flat damage bonus when holding this item.",
            new List<string>()
            {
                "ITEM_GUARDBREAKER_DESC"
            }
        );
        public static ConfigurableValue<float> attackSpeedBonus = new(
            "Item: Guardbreaker",
            "Attack Speed",
            12f,
            "Percent attack speed bonus when holding this item.",
            new List<string>()
            {
                "ITEM_GUARDBREAKER_DESC"
            }
        );
        public static ConfigurableValue<float> critBonus = new(
            "Item: Guardbreaker",
            "Crit Chance",
            12f,
            "Crit chance bonus when holding this item.",
            new List<string>()
            {
                "ITEM_GUARDBREAKER_DESC"
            }
        );
        public static ConfigurableValue<float> healthBonus = new(
            "Item: Guardbreaker",
            "Flat Health",
            100f,
            "Flat health bonus when holding this item.",
            new List<string>()
            {
                "ITEM_GUARDBREAKER_DESC"
            }
        );
        public static ConfigurableValue<float> damageAmp = new(
            "Item: Guardbreaker",
            "Damage Amp",
            5f,
            "Damage amp bonus when holding this item.",
            new List<string>()
            {
                "ITEM_GUARDBREAKER_DESC"
            }
        );
        public static ConfigurableValue<float> bonusShieldDamage = new(
            "Item: Guardbreaker",
            "Shield Damage",
            30f,
            "Damage bonus to shields when holding this item.",
            new List<string>()
            {
                "ITEM_GUARDBREAKER_DESC"
            }
        );
        public static readonly float percentAttackSpeedBonus = attackSpeedBonus.Value / 100f;
        public static readonly float percentDamageAmp = damageAmp.Value / 100f;
        public static readonly float percentBonusShieldDamage = bonusShieldDamage.Value / 100f;

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

            itemDef.name = "GUARDBREAKER";
            itemDef.AutoPopulateTokens();

            Utils.SetItemTier(itemDef, ItemTier.Tier3);

            itemDef.pickupIconSprite = AssetHandler.bundle.LoadAsset<Sprite>("Guardbreaker.png");
            itemDef.pickupModelPrefab = AssetHandler.bundle.LoadAsset<GameObject>("Guardbreaker.prefab");
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
                        args.baseDamageAdd += flatDamageBonus.Value;
                        args.attackSpeedMultAdd += percentAttackSpeedBonus;
                        args.critAdd += critBonus.Value;
                        args.baseHealthAdd += healthBonus.Value;
                    }
                }
            };

            GenericGameEvents.BeforeTakeDamage += (damageInfo, attackerInfo, victimInfo) =>
            {
                CharacterBody attackerBody = attackerInfo.body;
                CharacterBody victimBody = victimInfo.body;
                if (attackerBody && victimBody && attackerBody.inventory)
                {
                    int count = attackerBody.inventory.GetItemCount(itemDef);
                    if (count > 0)
                    {
                        damageInfo.damage *= 1 + percentDamageAmp;

                        if (victimBody.healthComponent && victimBody.healthComponent.shield > 0)
                            damageInfo.damage *= 1 + percentBonusShieldDamage;
                    }
                }
            };
        }
    }
}
