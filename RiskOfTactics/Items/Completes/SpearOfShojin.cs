using R2API;
using R2API.Networking;
using R2API.Networking.Interfaces;
using RoR2;
using RoR2.Skills;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

namespace RiskOfTactics
{
    class SpearOfShojin
    {
        public static ItemDef itemDef;

        // Gain attack speed, flat damage, and cooldown reduction. Every 10 seconds, your next attack deals an additional 18 damage and reduces nearby enemy armor by 10.
        public static ConfigurableValue<bool> isEnabled = new(
            "Item: Spear Of Shojin",
            "Enabled",
            true,
            "Whether or not the item is enabled.",
            new List<string>()
            {
                "ITEM_SPEAROFSHOJIN_DESC"
            }
        );
        public static ConfigurableValue<float> damageBonus = new(
            "Item: Spear Of Shojin",
            "Percent Damage",
            15f,
            "Percent attack speed gained when holding this item.",
            new List<string>()
            {
                "ITEM_SPEAROFSHOJIN_DESC"
            }
        );
        public static ConfigurableValue<float> flatDamageBonus = new(
            "Item: Spear Of Shojin",
            "Flat Damage",
            5f,
            "Flat damage gained when holding this item.",
            new List<string>()
            {
                "ITEM_SPEAROFSHOJIN_DESC"
            }
        );
        public static ConfigurableValue<float> cooldownReductionBonus = new(
            "Item: Spear Of Shojin",
            "Cooldown Reduction",
            10f,
            "Percent cooldown reduction gained when holding this item.",
            new List<string>()
            {
                "ITEM_SPEAROFSHOJIN_DESC"
            }
        );
        public static ConfigurableValue<float> cooldownOnHit = new(
            "Item: Spear Of Shojin",
            "On-Hit Cooldown",
            5f,
            "Percentage of remaining cooldown refunded on-hit.",
            new List<string>()
            {
                "ITEM_SPEAROFSHOJIN_DESC"
            }
        );
        private static readonly float percentDamageBonus = damageBonus.Value / 100f;
        private static readonly float percentCooldownReductionBonus = cooldownReductionBonus.Value / 100f;
        private static readonly float percentCooldownOnHit = cooldownOnHit.Value / 100f;

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

            itemDef.name = "SPEAROFSHOJIN";
            itemDef.AutoPopulateTokens();

            Utils.SetItemTier(itemDef, ItemTier.Tier3);

            itemDef.pickupIconSprite = AssetHandler.bundle.LoadAsset<Sprite>("SpearOfShojin.png");
            itemDef.pickupModelPrefab = AssetHandler.bundle.LoadAsset<GameObject>("SpearOfShojin.prefab");
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
                    int count = sender.inventory.GetItemCountEffective(itemDef);
                    if (count > 0)
                    {
                        args.baseDamageAdd += flatDamageBonus.Value;
                        args.damageMultAdd += percentDamageBonus;
                        args.cooldownMultAdd -= percentCooldownReductionBonus;
                    }
                }
            };

            GenericGameEvents.OnTakeDamage += (damageReport) =>
            {
                CharacterBody vicBody = damageReport.victimBody;
                CharacterBody atkBody = damageReport.attackerBody;

                if (atkBody && atkBody.inventory && atkBody.inventory.GetItemCountEffective(itemDef) > 0)
                {
                    foreach (GenericSkill skill in atkBody.skillLocator.allSkills)
                    {
                        skill.rechargeStopwatch += skill.rechargeStopwatch * percentCooldownOnHit;
                    }
                }
            };
        }
    }
}
