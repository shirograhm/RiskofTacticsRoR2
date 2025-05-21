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
    class GiantSlayer
    {
        public static ItemDef itemDef;

        // Gain percent damage, attack speed, flat damage, and damage amp. Gain additional damage amp against bosses and elites.
        public static ConfigurableValue<bool> isEnabled = new(
            "Item: Giant Slayer",
            "Enabled",
            true,
            "Whether or not the item is enabled.",
            new List<string>()
            {
                "ITEM_GIANTSLAYER_DESC"
            }
        );
        public static ConfigurableValue<float> damageBonus = new(
            "Item: Giant Slayer",
            "Percent Damage",
            12f,
            "Percent damage bonus when holding this item.",
            new List<string>()
            {
                "ITEM_GIANTSLAYER_DESC"
            }
        );
        public static ConfigurableValue<float> attackSpeedBonus = new(
            "Item: Giant Slayer",
            "Attack Speed",
            20f,
            "Percent attack speed bonus when holding this item.",
            new List<string>()
            {
                "ITEM_GIANTSLAYER_DESC"
            }
        );
        public static ConfigurableValue<float> flatDamageBonus = new(
            "Item: Giant Slayer",
            "Flat Damage",
            6f,
            "Flat damage bonus when holding this item.",
            new List<string>()
            {
                "ITEM_GIANTSLAYER_DESC"
            }
        );
        public static ConfigurableValue<float> damageAmp = new(
            "Item: Giant Slayer",
            "Damage Amp",
            5f,
            "Percent attack speed bonus when holding this item.",
            new List<string>()
            {
                "ITEM_GIANTSLAYER_DESC"
            }
        );
        public static ConfigurableValue<float> damageAmpBossesAndElites = new(
            "Item: Giant Slayer",
            "Damage Amp",
            10f,
            "Percent max health healing per item proc.",
            new List<string>()
            {
                "ITEM_GIANTSLAYER_DESC"
            }
        );
        public static readonly float percentDamageBonus = damageBonus.Value / 100f;
        public static readonly float percentAttackSpeedBonus = attackSpeedBonus.Value / 100f;
        public static readonly float percentDamageAmp = damageAmp.Value / 100f;
        public static readonly float percentDamageAmpBossesAndElites = damageAmpBossesAndElites.Value / 100f;

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

            itemDef.name = "GIANTSLAYER";
            itemDef.AutoPopulateTokens();

            Utils.SetItemTier(itemDef, ItemTier.Tier3);

            itemDef.pickupIconSprite = AssetHandler.bundle.LoadAsset<Sprite>("GiantSlayer.png");
            itemDef.pickupModelPrefab = AssetHandler.bundle.LoadAsset<GameObject>("GiantSlayer.prefab");
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
                    int count = sender.inventory.GetItemCount(itemDef);
                    if (count > 0)
                    {
                        args.baseDamageAdd += flatDamageBonus.Value;
                        args.damageMultAdd += percentDamageBonus;
                        args.attackSpeedMultAdd += percentAttackSpeedBonus;
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

                        if (victimBody.isBoss || victimBody.isElite)
                        {
                            damageInfo.damage *= 1 + percentDamageAmpBossesAndElites;
                        }
                    }
                }
            };
        }
    }
}
