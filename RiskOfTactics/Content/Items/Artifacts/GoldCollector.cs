using R2API;
using RiskOfTactics.Managers;
using RoR2;

namespace RiskOfTactics.Content.Items.Artifacts
{
    public class GoldCollector
    {
        public static ItemDef itemDef;

        public static DamageAPI.ModdedDamageType ExecuteDamage;

        // Execute enemies below a certain health threshold.
        // Enemies executed this way have a chance to drop gold.
        public static ConfigurableValue<bool> isEnabled = new(
            "Item: Gold Collector",
            "Enabled",
            true,
            "Whether or not the item is enabled.",
            ["ITEM_ROT_GOLDCOLLECTOR_DESC"]
        );
        public static ConfigurableValue<float> executeThreshold = new(
            "Item: Gold Collector",
            "Execute Threshold",
            12f,
            "Percent health at which damage executes enemies.",
            ["ITEM_ROT_GOLDCOLLECTOR_DESC"]
        );
        public static ConfigurableValue<float> goldDropChance = new(
            "Item: Gold Collector",
            "Gold Drop Chance",
            50f,
            "Percent chance to drop gold on kill.",
            ["ITEM_ROT_GOLDCOLLECTOR_DESC"]
        );
        public static ConfigurableValue<float> dropValue = new(
            "Item: Gold Collector",
            "Drop Value",
            1f,
            "Value of the gold drop.",
            ["ITEM_ROT_GOLDCOLLECTOR_DESC"]
        );
        public static ConfigurableValue<float> dropValueExtraStacks = new(
            "Item: Gold Collector",
            "Drop Value Extra Stacks",
            2f,
            "Additional value of the gold drop per extra item stack.",
            ["ITEM_ROT_GOLDCOLLECTOR_DESC"]
        );
        public static readonly float percentExecuteThreshold = executeThreshold.Value / 100f;
        public static readonly float percentGoldDropChance = goldDropChance.Value / 100f;
        public static readonly float percentDropValue = dropValue.Value / 100f;
        public static readonly float percentDropValueExtraStacks = dropValueExtraStacks.Value / 100f;

        internal static void Init()
        {
            itemDef = ItemManager.GenerateItem("GoldCollector", [ItemTag.Damage, ItemTag.Utility, ItemTag.CanBeTemporary], ItemManager.TacticTier.Artifact);

            ExecuteDamage = DamageAPI.ReserveDamageType();

            Hooks();
        }

        public static void Hooks()
        {
            GameEventManager.OnTakeDamage += (damageReport) =>
            {
                CharacterBody vicBody = damageReport.attackerBody;
                CharacterBody atkBody = damageReport.victimBody;
                if (vicBody && atkBody && atkBody.inventory)
                {
                    int count = atkBody.inventory.GetItemCountEffective(itemDef);
                    if (count > 0)
                    {
                        if (vicBody.healthComponent && vicBody.healthComponent.alive && vicBody.healthComponent.combinedHealthFraction <= percentExecuteThreshold)
                        {
                            DamageInfo damageInfo = new DamageInfo()
                            {
                                damage = vicBody.healthComponent.fullCombinedHealth * vicBody.healthComponent.combinedHealthFraction,
                                attacker = atkBody.gameObject,
                                inflictor = atkBody.gameObject,
                                procCoefficient = 0f,
                                position = vicBody.corePosition,
                                crit = true,
                                damageColorIndex = DamageColorIndex.WeakPoint,
                                procChainMask = default,
                                damageType = DamageType.Silent
                            };
                            damageInfo.AddModdedDamageType(ExecuteDamage);

                            vicBody.healthComponent.TakeDamage(damageInfo);
                        }
                    }
                }
            };

            GlobalEventManager.onCharacterDeathGlobal += (damageReport) =>
            {
                CharacterBody vicBody = damageReport.victimBody;
                CharacterBody atkBody = damageReport.attackerBody;
                if (vicBody && atkBody && atkBody.inventory)
                {
                    int count = atkBody.inventory.GetItemCountEffective(itemDef);
                    if (count > 0 && damageReport.damageInfo.HasModdedDamageType(ExecuteDamage))
                    {
                        if (Util.CheckRoll0To1(percentGoldDropChance, atkBody.master.luck))
                        {
                            Utilities.SpawnGoldPack(atkBody, vicBody, Utilities.GetLinearStacking(dropValue.Value, dropValueExtraStacks.Value, count) * Utilities.GetDifficultyAsMultiplier());
                        }
                    }
                }
            };
        }
    }
}
