using R2API;
using RoR2;
using System.Collections.Generic;
using UnityEngine;

namespace RiskOfTactics
{
    class HandOfJustice
    {
        public static ItemDef itemDef;

        // Gain crit chance. Grants damage and omnivamp that scales with your current health.
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
        public static ConfigurableValue<float> critChanceBonus = new(
            "Item: Hand Of Justice",
            "Crit Chance",
            8f,
            "Crit chance gained when holding this item.",
            new List<string>()
            {
                "ITEM_HANDOFJUSTICE_DESC"
            }
        );
        public static ConfigurableValue<float> scaledBonusDamageEffect = new(
            "Item: Hand Of Justice",
            "Bonus Damage Percent",
            10f,
            "Percent BASE damage scaling effect gained for this item.",
            new List<string>()
            {
                "ITEM_HANDOFJUSTICE_DESC"
            }
        );
        public static ConfigurableValue<float> scaledBonusDamageEffectExtraStacks = new(
            "Item: Hand Of Justice",
            "Bonus Damage Percent Per Stack",
            10f,
            "Percent BASE damage scaling effect gained for extra stacks of this item.",
            new List<string>()
            {
                "ITEM_HANDOFJUSTICE_DESC"
            }
        );
        public static ConfigurableValue<float> omnivampEffect = new(
            "Item: Hand Of Justice",
            "Omnivamp",
            10f,
            "Percent omnivamp scaling effect gained for this item.",
            new List<string>()
            {
                "ITEM_HANDOFJUSTICE_DESC"
            }
        );
        public static ConfigurableValue<float> omnivampEffectExtraStacks = new(
            "Item: Hand Of Justice",
            "Omnivamp Per Stack",
            10f,
            "Percent omnivamp scaling effect gained for extra stacks of this item.",
            new List<string>()
            {
                "ITEM_HANDOFJUSTICE_DESC"
            }
        );
        private static readonly float percentScaledBonusDamageEffect = scaledBonusDamageEffect.Value / 100f;
        private static readonly float percentScaledBonusDamageEffectExtraStacks = scaledBonusDamageEffectExtraStacks.Value / 100f;
        private static readonly float percentOmnivampEffect = omnivampEffect.Value / 100f;
        private static readonly float percentOmnivampEffectExtraStacks = omnivampEffectExtraStacks.Value / 100f;

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

            Utils.SetItemTier(itemDef, ItemTier.Tier2);

            GameObject prefab = AssetHandler.bundle.LoadAsset<GameObject>("HandOfJustice.prefab");
            ModelPanelParameters modelPanelParameters = prefab.AddComponent<ModelPanelParameters>();
            modelPanelParameters.focusPointTransform = prefab.transform;
            modelPanelParameters.cameraPositionTransform = prefab.transform;
            modelPanelParameters.maxDistance = 10f;
            modelPanelParameters.minDistance = 5f;

            itemDef.pickupIconSprite = AssetHandler.bundle.LoadAsset<Sprite>("HandOfJustice.png");
            itemDef.pickupModelPrefab = prefab;
            itemDef.canRemove = true;
            itemDef.hidden = false;

            itemDef.tags = new ItemTag[]
            {
                ItemTag.Damage,
                ItemTag.Healing,

                ItemTag.CanBeTemporary
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
                        args.critAdd += critChanceBonus.Value;

                        int multiplier = sender.healthComponent.combinedHealthFraction >= 0.50f ? 2 : 1;
                        args.damageTotalMult *= 1 + Utils.GetLinearStacking(percentScaledBonusDamageEffect, percentScaledBonusDamageEffectExtraStacks, count) * multiplier;
                    }
                }
            };

            GenericGameEvents.OnTakeDamage += (damageReport) =>
            {
                CharacterBody vicBody = damageReport.victimBody;
                CharacterBody atkBody = damageReport.attackerBody;

                if (vicBody && atkBody && atkBody.inventory)
                {
                    int count = atkBody.inventory.GetItemCountEffective(itemDef);
                    if (count > 0 && !Utils.OnSameTeam(vicBody, atkBody) && atkBody.healthComponent)
                    {
                        int multiplier = atkBody.healthComponent.combinedHealthFraction < 0.50f ? 2 : 1;
                        atkBody.healthComponent.Heal(damageReport.damageInfo.damage * Utils.GetHyperbolicStacking(percentOmnivampEffect, percentOmnivampEffectExtraStacks, count) * multiplier, new ProcChainMask());
                    }
                }
            };
        }
    }
}
