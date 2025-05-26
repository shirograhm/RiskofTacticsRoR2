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
    class Bloodthirster
    {
        public static ItemDef itemDef;
        public static BuffDef satedBuff;

        // Gain scaling damage, flat damage, shielding and omnivamp. Every 30 seconds, falling below 20% max HP grants a 50% max HP barrier.
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
        public static ConfigurableValue<float> flatDamageBonus = new(
            "Item: Bloodthirster",
            "Flat Damage",
            4f,
            "Flat damage bonus when holding this item.",
            new List<string>()
            {
                "ITEM_BLOODTHIRSTER_DESC"
            }
        );
        public static ConfigurableValue<float> damageBonus = new(
            "Item: Bloodthirster",
            "Percent Damage",
            12f,
            "Percent damage bonus when holding this item.",
            new List<string>()
            {
                "ITEM_BLOODTHIRSTER_DESC"
            }
        );
        public static ConfigurableValue<float> shieldBonus = new(
            "Item: Bloodthirster",
            "Percent Shield",
            12f,
            "Percent max HP shield gained when holding this item.",
            new List<string>()
            {
                "ITEM_BLOODTHIRSTER_DESC"
            }
        );
        public static ConfigurableValue<float> omnivampBonus = new(
            "Item: Bloodthirster",
            "Omnivamp",
            12f,
            "Percent omnivamp gained when holding this item.",
            new List<string>()
            {
                "ITEM_BLOODTHIRSTER_DESC"
            }
        );
        public static ConfigurableValue<float> effectCooldown = new(
            "Item: Bloodthirster",
            "Effect Cooldown",
            30f,
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
            50f,
            "Percent max HP barrier given when this item is procced during the teleporter event.",
            new List<string>()
            {
                "ITEM_BLOODTHIRSTER_DESC"
            }
        );
        private static readonly float percentDamageBonus = damageBonus.Value / 100f;
        private static readonly float percentShieldBonus = shieldBonus.Value / 100f;
        private static readonly float percentOmnivampBonus = omnivampBonus.Value / 100f;
        private static readonly float percentBarrierTriggerHP = barrierTriggerHP.Value / 100f;
        private static readonly float percentBarrierSize = barrierSize.Value / 100f;

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

            itemDef.pickupIconSprite = AssetHandler.bundle.LoadAsset<Sprite>("Bloodthirster.png");
            itemDef.pickupModelPrefab = AssetHandler.bundle.LoadAsset<GameObject>("Bloodthirster.prefab");
            itemDef.canRemove = true;
            itemDef.hidden = false;

            itemDef.tags = new ItemTag[]
            {
                ItemTag.Damage,
                ItemTag.Healing,
                ItemTag.Utility,

                ItemTag.LowHealth
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
                        args.baseDamageAdd += flatDamageBonus.Value;
                        args.baseShieldAdd += sender.healthComponent.fullHealth * percentShieldBonus;
                    }
                }
            };

            GenericGameEvents.OnTakeDamage += (damageReport) =>
            {
                CharacterBody vicBody = damageReport.victimBody;
                CharacterBody atkBody = damageReport.attackerBody;

                if (vicBody && atkBody && vicBody.inventory && atkBody.inventory)
                {

                    int vicCount = vicBody.inventory.GetItemCount(itemDef);
                    int atkCount = atkBody.inventory.GetItemCount(itemDef);
                    // Omnivamp effect
                    if (atkCount > 0 && vicBody.teamComponent.teamIndex != atkBody.teamComponent.teamIndex)
                    {
                        atkBody.healthComponent.Heal(damageReport.damageInfo.damage * percentOmnivampBonus, new ProcChainMask());
                    }
                    // Low health barrier effect
                    int buffCount = vicBody.GetBuffCount(satedBuff);
                    if (vicCount > 0 && buffCount == 0 && vicBody.healthComponent.combinedHealthFraction < percentBarrierTriggerHP)
                    {
                        vicBody.healthComponent.AddBarrier(vicBody.healthComponent.fullCombinedHealth * percentBarrierSize);
                        vicBody.AddTimedBuff(satedBuff, effectCooldown);
                    }
                }
            };
        }
    }
}
