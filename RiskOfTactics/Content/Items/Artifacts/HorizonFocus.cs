using Mono.Cecil.Cil;
using MonoMod.Cil;
using RiskOfTactics.Managers;
using RoR2;
using RoR2.Projectile;
using System;
using UnityEngine;

namespace RiskOfTactics.Content.Items.Artifacts
{
    class HorizonFocus
    {
        public static ItemDef itemDef;

        // Stunning enemies strikes them again after a short delay.
        public static ConfigurableValue<bool> isEnabled = new(
            "Item: Horizon Focus",
            "Enabled",
            true,
            "Whether or not the item is enabled.",
            ["ITEM_ROT_HORIZONFOCUS_DESC"]
        );
        public static ConfigurableValue<float> stunChance = new(
            "Item: Horizon Focus",
            "Stun Chance",
            10f,
            "Percent chance to stun enemies when dealing damage.",
            ["ITEM_ROT_HORIZONFOCUS_DESC"]
        );
        public static ConfigurableValue<float> lightningDamage = new(
            "Item: Horizon Focus",
            "Lightning Damage",
            10f,
            "Percent enemy max HP damage dealt by the lightning orb caused by this item.",
            ["ITEM_ROT_HORIZONFOCUS_DESC"]
        );
        public static ConfigurableValue<float> lightningDamageExtraStacks = new(
            "Item: Horizon Focus",
            "Lightning Damage Extra Stacks",
            10f,
            "Percent enemy max HP damage dealt by the lightning orb caused by extra stacks this item.",
            ["ITEM_ROT_HORIZONFOCUS_DESC"]
        );
        public static float percentStunChance = stunChance.Value / 100f;
        public static float percentLightningDamage = lightningDamage.Value / 100f;
        public static float percentLightningDamageExtraStacks = lightningDamageExtraStacks.Value / 100f;

        internal static void Init()
        {
            itemDef = ItemManager.GenerateItem("HorizonFocus", [ItemTag.Damage, ItemTag.Utility, ItemTag.CanBeTemporary], ItemManager.TacticTier.Artifact);

            Hooks();
        }

        public static void Hooks()
        {
            GameEventManager.BeforeTakeDamage += (damageInfo, attackerInfo, victimInfo) =>
            {
                CharacterBody vicBody = victimInfo.body;
                CharacterBody atkBody = attackerInfo.body;

                if (vicBody && atkBody && atkBody.inventory && Utilities.IsValidTargetBody(vicBody))
                {
                    int count = atkBody.inventory.GetItemCountEffective(itemDef);
                    if (count > 0 && !Utilities.OnSameTeam(vicBody, atkBody))
                    {
                        if (Util.CheckRoll0To1(percentStunChance * damageInfo.procCoefficient, atkBody.master))
                        {
                            damageInfo.damageType |= DamageType.Stun1s;
                            damageInfo.damageColorIndex = DamageColorIndex.Electrocution;
                        }
                    }
                }
            };

            IL.RoR2.SetStateOnHurt.OnTakeDamageServer += (il) =>
            {
                ILCursor c = new(il);
                int matchCount = 0;

                while (c.TryGotoNext(MoveType.Before, x => x.MatchCall<SetStateOnHurt>(nameof(SetStateOnHurt.SetStun))))
                {
                    c.Emit(OpCodes.Ldarg_1);
                    c.EmitDelegate<Func<float, DamageReport, float>>((duration, damageReport) =>
                    {
                        if (damageReport.victimBody && damageReport.attackerBody && damageReport.attackerBody.inventory)
                        {
                            int count = damageReport.attackerBody.inventory.GetItemCountEffective(itemDef);
                            if (count > 0)
                            {
                                float damageMultiplier = Utilities.GetHyperbolicStacking(percentLightningDamage, percentLightningDamageExtraStacks, count);
                                SpawnLightningStrike(damageReport.damageInfo, damageReport.attackerBody, damageReport.victimBody, damageMultiplier);
                            }
                        }
                        return duration;
                    });
                    c.Index++;
                    matchCount++;
                }

                Log.Info($"SetStun hook matched {matchCount} times"); // should be 3
            };
        }

        private static void SpawnLightningStrike(DamageInfo info, CharacterBody attackerBody, CharacterBody victimBody, float mult)
        {
            if (victimBody.healthComponent)
            {
                ProjectileManager.instance.FireProjectileWithoutDamageType(
                    LegacyResourcesAPI.Load<GameObject>("Prefabs/Projectiles/LightningStake"),
                    info.position,
                    Quaternion.identity,
                    attackerBody.gameObject,
                    victimBody.healthComponent.fullHealth * mult,
                    0f,
                    info.crit,
                    DamageColorIndex.Electrocution
                );
            }
        }
    }
}

