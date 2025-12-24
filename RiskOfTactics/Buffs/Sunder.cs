using R2API;
using RoR2;
using System.Collections.Generic;
using UnityEngine;

namespace RiskOfTactics.Buffs
{
    class Sunder
    {
        public static BuffDef buffDef;

        public static DamageAPI.ModdedDamageType ApplySunder;

        // Temporarily reduces a flat amount of armor. Can be stacked.
        public static ConfigurableValue<int> armorReduction = new(
            "Buff: Sunder",
            "Sunder Reduction",
            10,
            "Armor reduction applied to enemies for each stack of this debuff.",
            new List<string>()
            {
                "BUFF_SUNDER_DESC"
            }
        );

        internal static void Init()
        {
            buffDef = Utils.GenerateBuffDef("Sunder",
                AssetHandler.bundle.LoadAsset<Sprite>("Sunder.png"),
                true, false, true, false
            );
            ContentAddition.AddBuffDef(buffDef);

            ApplySunder = DamageAPI.ReserveDamageType();

            Hooks();
        }

        public static void Hooks()
        {
            RecalculateStatsAPI.GetStatCoefficients += (sender, args) =>
            {
                if (sender && sender.GetBuffCount(buffDef) > 0)
                {
                    args.armorAdd -= armorReduction.Value * sender.GetBuffCount(buffDef);
                }
            };

            GenericGameEvents.BeforeTakeDamage += (damageInfo, attackerInfo, victimInfo) =>
            {
                CharacterBody vicBody = victimInfo.body;

                if (vicBody && damageInfo.HasModdedDamageType(ApplySunder))
                    vicBody.AddBuff(buffDef);
            };
        }
    }
}
