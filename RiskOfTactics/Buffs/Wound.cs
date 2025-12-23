using R2API;
using RoR2;
using System.Collections.Generic;
using UnityEngine;

namespace RiskOfTactics.Buffs
{
    class Wound
    {
        public static BuffDef buffDef;

        public static ConfigurableValue<float> healingReduction = new(
            "Buff: Wound",
            "Wound Reduction",
            33f,
            "Healing reduction applied to enemies hit by this effect.",
            new List<string>()
            {
                "BUFF_WOUND_DESC"
            }
        );
        public static ConfigurableValue<float> woundDuration = new(
            "Buff: Wound",
            "Wound Duration",
            10f,
            "Duration of Wound debuff applied to enemies.",
            new List<string>()
            {
                "BUFF_WOUND_DESC"
            }
        );
        public static readonly float percentHealingReduction = healingReduction.Value / 100f;

        internal static void Init()
        {
            buffDef = Utils.GenerateBuffDef("Wound",
                AssetHandler.bundle.LoadAsset<Sprite>("Wound.png"),
                false, false, true, true
            );
            ContentAddition.AddBuffDef(buffDef);

            Hooks();
        }

        public static void Hooks()
        {
            On.RoR2.HealthComponent.Heal += (orig, self, amount, procChainMask, nonRegen) =>
            {
                if (self)
                {
                    CharacterBody body = self.GetComponentInParent<CharacterBody>();
                    if (body && body.GetBuffCount(buffDef) > 0)
                    {
                        // Cut both regen and heals
                        amount *= 1 - percentHealingReduction;
                    }
                }
                return orig(self, amount, procChainMask, nonRegen);
            };
        }
    }
}
