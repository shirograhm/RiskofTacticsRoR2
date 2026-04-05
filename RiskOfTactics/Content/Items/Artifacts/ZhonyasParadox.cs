using R2API;
using RiskOfTactics.Managers;
using RoR2;
using UnityEngine;

namespace RiskOfTactics.Content.Items.Artifacts
{
    internal class ZhonyasParadox
    {
        public static ItemDef itemDef;
        public static BuffDef zhonyasBuff;

        // Taking damage below a threshold causes you to become Gilded for a duration. During this duration you are also invulnerable.
        public static ConfigurableValue<bool> isEnabled = new(
            "Item: Zhonyas Paradox",
            "Enabled",
            true,
            "Whether or not the item is enabled.",
            ["ITEM_ROT_ZHONYASPARADOX_DESC"]
        );
        public static ConfigurableValue<float> healthThreshold = new(
            "Item: Zhonyas Paradox",
            "Health Threshold",
            40f,
            "Percent health threshold at which the effect activates.",
            ["ITEM_ROT_ZHONYASPARADOX_DESC"]
        );
        public static ConfigurableValue<float> effectDuration = new(
            "Item: Zhonyas Paradox",
            "Effect Duration",
            3f,
            "Duration of the Gilded effect.",
            ["ITEM_ROT_ZHONYASPARADOX_DESC"]
        );
        public static ConfigurableValue<float> effectDurationExtraStacks = new(
            "Item: Zhonyas Paradox",
            "Effect Duration Extra Stacks",
            3f,
            "Additional duration of the Gilded effect with extra stacks.",
            ["ITEM_ROT_ZHONYASPARADOX_DESC"]
        );
        public static ConfigurableValue<bool> includeInvulnerability = new(
            "Item: Zhonyas Paradox",
            "Include Invulnerability",
            true,
            "Whether or not to include invulnerability during the Gilded effect.",
            ["ITEM_ROT_ZHONYASPARADOX_DESC"]
        );
        public static readonly float percentHealthThreshold = healthThreshold.Value / 100f;

        internal static void Init()
        {
            itemDef = ItemManager.GenerateItem("ZhonyasParadox", [ItemTag.Utility, ItemTag.CanBeTemporary], ItemManager.TacticTier.Artifact);

            zhonyasBuff = Utilities.GenerateBuffDef("ZhonyasParadoxBuff", AssetManager.bundle.LoadAsset<Sprite>("ZhonyasParadox"), false, true, false, true);
            ContentAddition.AddBuffDef(zhonyasBuff);

            Hooks();
        }

        public static void Hooks()
        {
            GameEventManager.OnTakeDamage += (damageReport) =>
            {
                CharacterBody vicBody = damageReport.victimBody;
                if (vicBody && vicBody.inventory)
                {
                    int count = vicBody.inventory.GetItemCountEffective(itemDef);
                    if (count > 0 && vicBody.healthComponent)
                    {
                        if (vicBody.healthComponent.healthFraction <= percentHealthThreshold)
                        {
                            float duration = Utilities.GetLinearStacking(effectDuration.Value, effectDurationExtraStacks.Value, count);
                            vicBody.AddTimedBuff(DLC2Content.Buffs.EliteAurelionite, duration);
                            vicBody.AddTimedBuff(zhonyasBuff, duration);
                        }
                    }
                }
            };

            GameEventManager.BeforeTakeDamage += (damageInfo, attackerInfo, victimInfo) =>
            {
                CharacterBody vicBody = victimInfo.body;
                if (vicBody && vicBody.inventory)
                {
                    int count = vicBody.inventory.GetItemCountEffective(itemDef);
                    if (count > 0 && vicBody.healthComponent)
                    {
                        float resultingHealth = Mathf.Max(vicBody.healthComponent.combinedHealth - damageInfo.damage, 0f);
                        if (resultingHealth < 0 && vicBody.healthComponent.healthFraction > percentHealthThreshold)
                        {
                            // Damage the player down to the threshold instead of killing them
                            damageInfo.damage = resultingHealth - vicBody.healthComponent.fullCombinedHealth * percentHealthThreshold;
                        }

                        if (includeInvulnerability.Value && vicBody.HasBuff(zhonyasBuff))
                        {
                            // Make the player invulnerable while they have the buff
                            damageInfo.damage = 0f;
                        }
                    }
                }
            };
        }
    }
}
