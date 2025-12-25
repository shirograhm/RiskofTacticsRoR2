using R2API;
using RiskOfTactics.Helpers;
using RoR2;

namespace RiskOfTactics.Content.Items.Artifacts
{
    class HellfireHatchet
    {
        public static ItemDef itemDef;

        public static DamageColorIndex hatchetDamageColor = DamageColorAPI.RegisterDamageColor(Utilities.HELLFIRE_HATCHET_COLOR);

        // Deal bonus damage based on max HP. Gain attack speed scaling with missing HP.
        public static ConfigurableValue<bool> isEnabled = new(
            "Item: Hellfire Hatchet",
            "Enabled",
            true,
            "Whether or not the item is enabled.",
            ["ITEM_ROT_HELLFIREHATCHET_DESC"]
        );
        public static ConfigurableValue<float> maxHealthDamage = new(
            "Item: Hellfire Hatchet",
            "Max Health Damage",
            3f,
            "Percent of max HP dealt as damage on-hit.",
            ["ITEM_ROT_HELLFIREHATCHET_DESC"]
        );
        public static ConfigurableValue<float> maxHealthDamageExtraStacks = new(
            "Item: Hellfire Hatchet",
            "Max Health Damage Extra Stacks",
            3f,
            "Percent of max HP dealt as damage on-hit with extra stacks.",
            ["ITEM_ROT_HELLFIREHATCHET_DESC"]
        );
        public static ConfigurableValue<float> attackSpeedPerPercent = new(
            "Item: Hellfire Hatchet",
            "Attack Speed Per Percent",
            1.5f,
            "Percent attack speed gained for every 1% missing HP.",
            ["ITEM_ROT_HELLFIREHATCHET_DESC"]
        );
        public static ConfigurableValue<float> attackSpeedPerPercentExtraStacks = new(
            "Item: Hellfire Hatchet",
            "Attack Speed Per Percent Extra Stacks",
            1.5f,
            "Percent attack speed gained for every 1% missing HP with extra stacks.",
            ["ITEM_ROT_HELLFIREHATCHET_DESC"]
        );
        public static ConfigurableValue<float> onHitProcCoefficient = new(
            "Item: Hellfire Hatchet",
            "On-Hit Proc",
            0.5f,
            "Proc coefficient for the percent max HP on-hit damage.",
            ["ITEM_ROT_HELLFIREHATCHET_DESC"]
        );
        public static float percentMaxHealthDamage = maxHealthDamage.Value / 100f;
        public static float percentMaxHealthDamageExtraStacks = maxHealthDamageExtraStacks.Value / 100f;
        public static float percentAttackSpeedPerPercent = attackSpeedPerPercent.Value / 100f;
        public static float percentAttackSpeedPerPercentExtraStacks = attackSpeedPerPercentExtraStacks.Value / 100f;

        internal static void Init()
        {
            itemDef = ItemHelper.GenerateItem("HellfireHatchet", [ItemTag.Damage, ItemTag.Utility, ItemTag.CanBeTemporary], ItemHelper.TacticTier.Artifact);

            Hooks();
        }

        public static void Hooks()
        {
            Utilities.AddRecalculateOnFrameHook(itemDef);

            RecalculateStatsAPI.GetStatCoefficients += (sender, args) =>
            {
                if (sender && sender.inventory)
                {
                    int count = sender.inventory.GetItemCountEffective(itemDef);
                    if (count > 0)
                    {
                        // Make sure this calculation only runs when healthFraction is below 1, not above 1
                        if (sender.healthComponent.combinedHealthFraction < 1f)
                        {
                            args.attackSpeedMultAdd += Utilities.GetLinearStacking(percentAttackSpeedPerPercent, percentAttackSpeedPerPercentExtraStacks, count) * Utilities.GetMissingHealthPercent(sender.healthComponent, true);
                        }
                    }
                }
            };

            GenericGameEvents.OnHitEnemy += (damageInfo, attackerInfo, victimInfo) =>
            {
                CharacterBody vicBody = victimInfo.body;
                CharacterBody atkBody = attackerInfo.body;

                if (vicBody && atkBody && atkBody.inventory && Utilities.IsValidTargetBody(vicBody))
                {
                    int count = atkBody.inventory.GetItemCountEffective(itemDef);
                    if (count > 0 && !Utilities.OnSameTeam(vicBody, atkBody))
                    {
                        DamageInfo proc = new()
                        {
                            damage = CalculateDamageOnHit(atkBody, count),
                            attacker = attackerInfo.gameObject,
                            inflictor = attackerInfo.gameObject,
                            procCoefficient = onHitProcCoefficient.Value,
                            position = damageInfo.position,
                            crit = atkBody.RollCrit(),
                            damageColorIndex = hatchetDamageColor,
                            procChainMask = damageInfo.procChainMask,
                            damageType = DamageType.Silent
                        };
                        victimInfo.healthComponent.TakeDamage(proc);
                    }
                }
            };
        }

        public static float CalculateDamageOnHit(CharacterBody sender, int count)
        {
            if (sender.healthComponent)
                return sender.healthComponent.fullHealth * Utilities.GetLinearStacking(percentMaxHealthDamage, percentMaxHealthDamageExtraStacks, count);

            return 0f;
        }
    }
}

