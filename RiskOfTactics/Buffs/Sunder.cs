using R2API;
using RoR2;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace RiskOfTactics
{
    class Sunder
    {
        public static BuffDef buffDef;

        public static ConfigurableValue<int> armorReduction = new(
            "Buff: Sunder",
            "Armor Reduction",
            10,
            "Armor reduction applied to enemies hit by this effect.",
            new List<string>()
            {
                "BUFF_SHRED_DESC"
            }
        );

        internal static void Init()
        {
            buffDef = Utils.GenerateBuffDef("Sunder",
                AssetHandler.bundle.LoadAsset<Sprite>("Sunder.png"),
                false, false, true, false
            );
            ContentAddition.AddBuffDef(buffDef);

            Hooks();
        }

        public static void Hooks()
        {
            RecalculateStatsAPI.GetStatCoefficients += (sender, args) =>
            {
                if (sender && sender.GetBuffCount(buffDef) > 0)
                {
                    args.armorAdd -= armorReduction.Value;
                }
            };
        }
    }
}
