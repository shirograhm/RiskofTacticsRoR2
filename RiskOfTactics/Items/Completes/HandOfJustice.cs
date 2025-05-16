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
    class HandOfJustice
    {
        public static ItemDef itemDef;

        //
        public static ConfigurableValue<bool> isEnabled = new(
            "Item: Hand Of Justice",
            "Enabled",
            true,
            "Whether or not the item is enabled.",
            new List<string>()
            {
                "ITEM_HANDOFJUSTICE_DESC"
            }
        );
        public static ConfigurableValue<float> cooldownReductionBonus = new(
            "Item: Hand Of Justice",
            "Cooldown Reduction",
            15f,
            "Cooldown reduction gained when holding this item.",
            new List<string>()
            {
                "ITEM_HANDOFJUSTICE_DESC"
            }
        );
        public static ConfigurableValue<float> critChanceBonus = new(
            "Item: Hand Of Justice",
            "Crit Chance",
            20f,
            "Crit chance gained when holding this item.",
            new List<string>()
            {
                "ITEM_HANDOFJUSTICE_DESC"
            }
        );
        public static ConfigurableValue<float> bonusDamageEffect = new(
            "Item: Hand Of Justice",
            "Bonus Damages",
            15f,
            "Flat and percent damage scaling effect gained for this item.",
            new List<string>()
            {
                "ITEM_HANDOFJUSTICE_DESC"
            }
        );
        public static ConfigurableValue<float> omnivampEffect = new(
            "Item: Hand Of Justice",
            "Omnivamp",
            12f,
            "Percent omnivamp scaling effect gained for this item.",
            new List<string>()
            {
                "ITEM_HANDOFJUSTICE_DESC"
            }
        );
        private static readonly float percentCooldownReductionBonus = cooldownReductionBonus.Value / 100f;
        private static readonly float percentBonusDamageEffect = bonusDamageEffect.Value / 100f;
        private static readonly float percentOmnivampEffect = omnivampEffect.Value / 100f;

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

            itemDef.name = "HANDOFJUSTICE";
            itemDef.AutoPopulateTokens();

            Utils.SetItemTier(itemDef, ItemTier.Tier3);

            itemDef.pickupIconSprite = AssetHandler.bundle.LoadAsset<Sprite>("HandOfJustice.png");
            itemDef.pickupModelPrefab = AssetHandler.bundle.LoadAsset<GameObject>("HandOfJustice.prefab");
            itemDef.canRemove = true;
            itemDef.hidden = false;

            itemDef.tags = new ItemTag[]
            {
                ItemTag.Damage,
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
                        args.cooldownMultAdd -= percentCooldownReductionBonus;
                        args.critAdd += critChanceBonus.Value;

                        int multiplier = sender.healthComponent.combinedHealthFraction >= 0.50f ? 2 : 1;
                        args.baseDamageAdd += bonusDamageEffect.Value * multiplier;
                        args.damageMultAdd += percentBonusDamageEffect * multiplier;
                    }
                }
            };

            GenericGameEvents.OnTakeDamage += (damageReport) =>
            {
                CharacterBody vicBody = damageReport.victimBody;
                CharacterBody atkBody = damageReport.attackerBody;

                if (vicBody && atkBody && atkBody.inventory)
                {
                    int count = atkBody.inventory.GetItemCount(itemDef);
                    if (count > 0 && vicBody.teamComponent.teamIndex != atkBody.teamComponent.teamIndex)
                    {
                        int multiplier = atkBody.healthComponent.combinedHealthFraction < 0.50f ? 2 : 1;
                        atkBody.healthComponent.Heal(damageReport.damageInfo.damage * percentOmnivampEffect * multiplier, new ProcChainMask());
                    }
                }
            };
        }
    }
}
