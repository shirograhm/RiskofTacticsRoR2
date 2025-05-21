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
    class JeweledGauntlet
    {
        public static ItemDef itemDef;

        // Gain flat damage and crit chance. Crits deal more damage.
        public static ConfigurableValue<bool> isEnabled = new(
            "Item: Jeweled Gauntlet",
            "Enabled",
            true,
            "Whether or not the item is enabled.",
            new List<string>()
            {
                "ITEM_JEWELEDGAUNTLET_DESC"
            }
        );
        public static ConfigurableValue<float> damageBonus = new(
            "Item: Jeweled Gauntlet",
            "Flat Damage",
            10f,
            "Flat damage bonus when holding this item.",
            new List<string>()
            {
                "ITEM_JEWELEDGAUNTLET_DESC"
            }
        );
        public static ConfigurableValue<float> critChance = new(
            "Item: Jeweled Gauntlet",
            "Crit Chance",
            20f,
            "Percent crit chance gained when holding this item.",
            new List<string>()
            {
                "ITEM_JEWELEDGAUNTLET_DESC"
            }
        );
        public static ConfigurableValue<float> critAmp = new(
            "Item: Jeweled Gauntlet",
            "Crit Amp",
            20f,
            "Bonus damage dealt by critical strikes when holding this item.",
            new List<string>()
            {
                "ITEM_JEWELEDGAUNTLET_DESC"
            }
        );
        public static readonly float percentCritAmp = critAmp.Value / 100f;

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

            itemDef.name = "JEWELEDGAUNTLET";
            itemDef.AutoPopulateTokens();

            Utils.SetItemTier(itemDef, ItemTier.Tier3);

            itemDef.pickupIconSprite = AssetHandler.bundle.LoadAsset<Sprite>("JeweledGauntlet.png");
            itemDef.pickupModelPrefab = AssetHandler.bundle.LoadAsset<GameObject>("JeweledGauntlet.prefab");
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
                        args.critAdd += critChance.Value;
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
                    if (count > 0 && attackerBody.master && damageInfo.crit)
                    {
                        damageInfo.damage *= 1 + percentCritAmp;
                    }
                }
            };
        }
    }
}
