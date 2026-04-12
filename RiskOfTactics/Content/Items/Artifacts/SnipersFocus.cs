using RiskOfTactics.Managers;
using RoR2;
using UnityEngine;

namespace RiskOfTactics.Content.Items.Artifacts
{
    public class SnipersFocus
    {
        public static ItemDef itemDef;
        public static GameObject missilePrefab;

        // Deal more damage to targets that are far away.
        public static ConfigurableValue<bool> isEnabled = new(
            "Item: Snipers Focus",
            "Enabled",
            true,
            "Whether or not the item is enabled.",
            ["ITEM_ROT_SNIPERSFOCUS_DESC"]
        );
        public static ConfigurableValue<float> damageIncreasePerMeter = new(
            "Item: Snipers Focus",
            "Damage Increase",
            1f,
            "Percent of additional damage dealt per meter of distance.",
            ["ITEM_ROT_SNIPERSFOCUS_DESC"]
        );
        public static ConfigurableValue<float> damageIncreasePerMeterExtraStacks = new(
            "Item: Snipers Focus",
            "Damage Increase Extra Stacks",
            1f,
            "Percent of additional damage dealt per meter of distance for additional item stacks.",
            ["ITEM_ROT_SNIPERSFOCUS_DESC"]
        );
        public static ConfigurableValue<float> missileDamageMult = new(
            "Item: Snipers Focus",
            "Missile Damage Multiplier",
            50f,
            "Missile damage dealt (multiplied by hit's damage).",
            ["ITEM_ROT_SNIPERSFOCUS_DESC"]
        );
        public static ConfigurableValue<float> maxDistance = new(
            "Item: Snipers Focus",
            "Max Distance",
            100f,
            "Maximum distance at which the damage increase is applied.",
            ["ITEM_ROT_SNIPERSFOCUS_DESC"]
        );
        public static readonly float percentDamageIncreasePerMeter = damageIncreasePerMeter.Value / 100f;
        public static readonly float percentDamageIncreasePerMeterExtraStacks = damageIncreasePerMeterExtraStacks.Value / 100f;
        public static readonly float percentMissileDamageMult = missileDamageMult.Value / 100f;

        internal static void Init()
        {
            itemDef = ItemManager.GenerateItem("SnipersFocus", [ItemTag.Damage, ItemTag.CanBeTemporary], ItemManager.TacticTier.Artifact);

            missilePrefab = LegacyResourcesAPI.LoadAsync<GameObject>("Prefabs/Projectiles/MissileProjectile").WaitForCompletion();

            Hooks();
        }

        public static void Hooks()
        {
            GameEventManager.BeforeTakeDamage += (damageInfo, attackerInfo, victimInfo) =>
            {
                CharacterBody atkBody = attackerInfo.body;
                GameObject victimObject = victimInfo.gameObject;

                if (atkBody && victimObject && atkBody.inventory)
                {
                    int count = atkBody.inventory.GetItemCountEffective(itemDef);
                    if (count > 0)
                    {
                        float distance = Vector3.Distance(attackerInfo.aimOrigin, victimObject.transform.position);
                        float totalPercentage = Utilities.GetLinearStacking(percentDamageIncreasePerMeter, percentDamageIncreasePerMeterExtraStacks, count);
                        float damageIncrease = Mathf.Min(distance * totalPercentage, maxDistance.Value * totalPercentage);

                        damageInfo.damage *= 1f + damageIncrease;

                        if (distance >= maxDistance.Value && !damageInfo.procChainMask.HasProc(ProcType.Missile))
                        {
                            int moreMissileCount = atkBody.inventory.GetItemCountEffective(DLC1Content.Items.MoreMissile);
                            // Fire a missile at the target
                            MissileUtils.FireMissile(
                                atkBody.corePosition,
                                atkBody,
                                default,
                                victimObject,
                                damageInfo.damage * percentMissileDamageMult * (1 + Utilities.GetLinearStacking(0f, 0.5f, moreMissileCount)),
                                atkBody.RollCrit(),
                                missilePrefab,
                                DamageColorIndex.WeakPoint,
                                addMissileProc: true
                            );
                        }
                    }
                }
            };
        }
    }
}