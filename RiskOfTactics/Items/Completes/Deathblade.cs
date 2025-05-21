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
    class Deathblade
    {
        public static ItemDef itemDef;

        // Gain percent damage and damage amp.
        public static ConfigurableValue<bool> isEnabled = new(
            "Item: Deathblade",
            "Enabled",
            true,
            "Whether or not the item is enabled.",
            new List<string>()
            {
                "ITEM_DEATHBLADE_DESC"
            }
        );
        public static ConfigurableValue<float> damageBonus = new(
            "Item: Deathblade",
            "Percent Damage",
            25f,
            "Percent damage bonus when holding this item.",
            new List<string>()
            {
                "ITEM_DEATHBLADE_DESC"
            }
        );
        public static ConfigurableValue<float> damageAmp = new(
            "Item: Deathblade",
            "Damage Amp",
            10f,
            "Percent damage amp when holding this item.",
            new List<string>()
            {
                "ITEM_DEATHBLADE_DESC"
            }
        );
        public static readonly float percentDamageBonus = damageBonus.Value / 100f;
        public static readonly float percentDamageAmp = damageAmp.Value / 100f;

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

            itemDef.name = "DEATHBLADE";
            itemDef.AutoPopulateTokens();

            Utils.SetItemTier(itemDef, ItemTier.Tier3);

            itemDef.pickupIconSprite = AssetHandler.bundle.LoadAsset<Sprite>("Deathblade.png");
            itemDef.pickupModelPrefab = AssetHandler.bundle.LoadAsset<GameObject>("Deathblade.prefab");
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
                        args.damageMultAdd += percentDamageBonus;
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
                    }
                }
            };
        }
    }
}
