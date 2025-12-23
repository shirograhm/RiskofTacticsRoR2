using R2API;
using RoR2;
using System.Collections.Generic;
using UnityEngine;

namespace RiskOfTactics
{
    class Bloodthirster
    {
        public static ItemDef itemDef;
        public static BuffDef satedBuff;

        // Periodically gain a barrier when taking damage at low health.
        public static ConfigurableValue<bool> isEnabled = new(
            "Item: Bloodthirster",
            "Enabled",
            true,
            "Whether or not the item is enabled.",
            new List<string>()
            {
                "ITEM_BLOODTHIRSTER_DESC"
            }
        );
        public static ConfigurableValue<float> effectCooldown = new(
            "Item: Bloodthirster",
            "Effect Cooldown",
            20f,
            "Cooldown of this item's effect.",
            new List<string>()
            {
                "ITEM_BLOODTHIRSTER_DESC"
            }
        );
        public static ConfigurableValue<float> barrierTriggerHP = new(
            "Item: Bloodthirster",
            "HP Threshold",
            20f,
            "Threshold needed to fall below in order to trigger this item's effect.",
            new List<string>()
            {
                "ITEM_BLOODTHIRSTER_DESC"
            }
        );
        public static ConfigurableValue<float> barrierSize = new(
            "Item: Bloodthirster",
            "Percent Barrier",
            100f,
            "Percent max HP barrier given when this item is procced.",
            new List<string>()
            {
                "ITEM_BLOODTHIRSTER_DESC"
            }
        );
        public static ConfigurableValue<float> barrierSizeExtraStacks = new(
            "Item: Bloodthirster",
            "Percent Barrier Extra Stacks",
            50f,
            "Percent max HP barrier given when extra stacks of item are procced.",
            new List<string>()
            {
                "ITEM_BLOODTHIRSTER_DESC"
            }
        );
        private static readonly float percentBarrierTriggerHP = barrierTriggerHP.Value / 100f;
        private static readonly float percentBarrierSize = barrierSize.Value / 100f;
        private static readonly float percentBarrierSizeExtraStacks = barrierSizeExtraStacks.Value / 100f;

        internal static void Init()
        {
            GenerateItem();

            ItemDisplayRuleDict displayRules = new ItemDisplayRuleDict(null);
            ItemAPI.Add(new CustomItem(itemDef, displayRules));

            satedBuff = Utils.GenerateBuffDef("Sated", AssetHandler.bundle.LoadAsset<Sprite>("Sated.png"), false, false, false, true);
            ContentAddition.AddBuffDef(satedBuff);

            Hooks();
        }

        private static void GenerateItem()
        {
            itemDef = ScriptableObject.CreateInstance<ItemDef>();

            itemDef.name = "BLOODTHIRSTER";
            itemDef.AutoPopulateTokens();

            Utils.SetItemTier(itemDef, ItemTier.Tier3);

            GameObject prefab = AssetHandler.bundle.LoadAsset<GameObject>("Bloodthirster.prefab");
            ModelPanelParameters modelPanelParameters = prefab.AddComponent<ModelPanelParameters>();
            modelPanelParameters.focusPointTransform = prefab.transform;
            modelPanelParameters.cameraPositionTransform = prefab.transform;
            modelPanelParameters.maxDistance = 10f;
            modelPanelParameters.minDistance = 5f;

            itemDef.pickupIconSprite = AssetHandler.bundle.LoadAsset<Sprite>("Bloodthirster.png");
            itemDef.pickupModelPrefab = prefab;
            itemDef.canRemove = true;
            itemDef.hidden = false;

            itemDef.tags = new ItemTag[]
            {
                ItemTag.Healing,
                ItemTag.Utility,

                ItemTag.LowHealth,
                ItemTag.CanBeTemporary
            };
        }

        public static void Hooks()
        {
            GenericGameEvents.OnTakeDamage += (damageReport) =>
            {
                CharacterBody vicBody = damageReport.victimBody;
                if (vicBody && vicBody.inventory && vicBody.healthComponent)
                {
                    // Low health barrier effect
                    int vicCount = vicBody.inventory.GetItemCountEffective(itemDef);
                    if (vicCount > 0 && !vicBody.HasBuff(satedBuff) && vicBody.healthComponent.combinedHealthFraction < percentBarrierTriggerHP)
                    {
                        vicBody.healthComponent.AddBarrier(vicBody.healthComponent.fullCombinedHealth * Utils.GetLinearStacking(percentBarrierSize, percentBarrierSizeExtraStacks, vicCount));
                        vicBody.AddTimedBuff(satedBuff, effectCooldown);
                    }
                }
            };
        }
    }
}
