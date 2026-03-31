using R2API;
using RiskOfTactics.Managers;
using RoR2;
using System.Collections.Generic;
using UnityEngine;

namespace RiskOfTactics.Content.Buffs
{
    class Chill
    {
        public static BuffDef buffDef;

        public static DamageAPI.ModdedDamageType ApplyChill;
        // Temporarily reduces attack speed. Can be stacked.
        public static ConfigurableValue<int> attackSpeedReduction = new(
            "Buff: Chill",
            "Chill Reduction",
            10,
            "Percent attack speed reduction applied to enemies for each stack of this debuff.",
            new List<string>()
            {
                "BUFF_CHILL_DESC"
            }
        );
        public static float percentAttackSpeedReduction = attackSpeedReduction.Value / 100f;

        internal static void Init()
        {
            buffDef = Utilities.GenerateBuffDef("Chill",
                AssetManager.bundle.LoadAsset<Sprite>("Chill.png"),
                true, false, true, false
            );
            ContentAddition.AddBuffDef(buffDef);

            ApplyChill = DamageAPI.ReserveDamageType();

            Hooks();
        }

        public static void Hooks()
        {
            RecalculateStatsAPI.GetStatCoefficients += (sender, args) =>
            {
                if (sender && sender.GetBuffCount(buffDef) > 0)
                {
                    args.attackSpeedReductionMultAdd += percentAttackSpeedReduction * sender.GetBuffCount(buffDef);
                }
            };

            GameEventManager.BeforeTakeDamage += (damageInfo, attackerInfo, victimInfo) =>
            {
                CharacterBody vicBody = victimInfo.body;

                if (vicBody && damageInfo.HasModdedDamageType(ApplyChill))
                    vicBody.AddBuff(buffDef);
            };
        }
    }
}
