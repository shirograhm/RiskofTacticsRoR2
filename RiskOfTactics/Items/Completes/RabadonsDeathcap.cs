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
    class RabadonsDeathcap
    {
        public static ItemDef itemDef;

        // Gain flat damage and damage amp.
        public static ConfigurableValue<bool> isEnabled = new(
            "Item: Deathblade",
            "Enabled",
            true,
            "Whether or not the item is enabled.",
            new List<string>()
            {
                "ITEM_RABADONSDEATHCAP_DESC"
            }
        );
        public static ConfigurableValue<float> damageBonus = new(
            "Item: Rabadons Deathcap",
            "Flat Damage",
            50f,
            "Flat damage bonus when holding this item.",
            new List<string>()
            {
                "ITEM_RABADONSDEATHCAP_DESC"
            }
        );
        public static ConfigurableValue<float> damageAmp = new(
            "Item: Rabadons Deathcap",
            "Damage Amp",
            20f,
            "Percent damage amp when holding this item.",
            new List<string>()
            {
                "ITEM_RABADONSDEATHCAP_DESC"
            }
        );
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

            itemDef.name = "RABADONSDEATHCAP";
            itemDef.AutoPopulateTokens();

            Utils.SetItemTier(itemDef, ItemTier.Tier3);

            itemDef.pickupIconSprite = AssetHandler.bundle.LoadAsset<Sprite>("RabadonsDeathcap.png");
            itemDef.pickupModelPrefab = AssetHandler.bundle.LoadAsset<GameObject>("RabadonsDeathcap.prefab");
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
                        args.baseDamageAdd += damageBonus.Value;
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
                    if (count > 0 && attackerBody.master)
                    {
                        damageInfo.damage *= 1 + percentDamageAmp;
                    }
                }
            };
        }
    }
}
