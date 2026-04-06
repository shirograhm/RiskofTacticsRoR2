using R2API;
using RiskOfTactics.Managers;
using RoR2;
using UnityEngine;

namespace RiskOfTactics.Content.Items.Completes
{
    internal class TitansResolve
    {
        public static ItemDef itemDef;
        public static BuffDef resolveBuff;

        public static ItemDef radiantDef;
        public static BuffDef radiantResolveBuff;

        // Taking or dealing damage grants a stacking damage multiplier, up to a cap.
        public static ConfigurableValue<bool> isEnabled = new(
            "Item: Titans Resolve",
            "Enabled",
            true,
            "Whether or not the item is enabled.",
            ["ITEM_ROT_TITANSRESOLVE_DESC"]
        );
        public static ConfigurableValue<float> damagePerStack = new(
            "Item: Titans Resolve",
            "Damage Per Stack",
            1f,
            "Total base damage bonus per stack of Resolve.",
            ["ITEM_ROT_TITANSRESOLVE_DESC"],
            true
        );
        public static ConfigurableValue<float> damagePerStackExtraStacks = new(
            "Item: Titans Resolve",
            "Damage Per Stack Extra Stacks",
            1f,
            "Total base damage bonus per stack of Resolve with extra stacks.",
            ["ITEM_ROT_TITANSRESOLVE_DESC"],
            true
        );
        public static ConfigurableValue<int> maxStacks = new(
            "Item: Titans Resolve",
            "Max Stacks",
            25,
            "Max stacks allowed of Resolve.",
            ["ITEM_ROT_TITANSRESOLVE_DESC"],
            false
        );
        public static ConfigurableValue<float> duration = new(
            "Item: Titans Resolve",
            "Stack Duration",
            10f,
            "Duration of each stack of Resolve.",
            ["ITEM_ROT_TITANSRESOLVE_DESC"],
            true
        );
        public static readonly float percentDamagePerStack = damagePerStack.Value / 100f;
        public static readonly float percentDamagePerStackExtraStacks = damagePerStackExtraStacks.Value / 100f;

        internal static void Init()
        {
            itemDef = ItemManager.GenerateItem("TitansResolve", [ItemTag.Damage, ItemTag.CanBeTemporary], ItemManager.TacticTier.Normal);
            resolveBuff = Utilities.GenerateBuffDef("TitansResolveBuff", AssetManager.bundle.LoadAsset<Sprite>("TitansResolve"), true, false, false, false);
            ContentAddition.AddBuffDef(resolveBuff);

            radiantDef = ItemManager.GenerateItem("Radiant_TitansResolve", [ItemTag.Damage, ItemTag.CanBeTemporary], ItemManager.TacticTier.Radiant);
            radiantResolveBuff = Utilities.GenerateBuffDef("RadiantTitansResolveBuff", AssetManager.bundle.LoadAsset<Sprite>("Radiant_TitansResolve"), true, false, false, false);
            ContentAddition.AddBuffDef(radiantResolveBuff);

            if (ConfigManager.Scaling.useRadiantAutoConversion) Utilities.RegisterRadiantUpgrade(itemDef, radiantDef);

            Hooks(itemDef, resolveBuff, ItemManager.TacticTier.Normal);
            Hooks(radiantDef, radiantResolveBuff, ItemManager.TacticTier.Radiant);
        }

        public static void Hooks(ItemDef def, BuffDef bDef, ItemManager.TacticTier tier)
        {
            float radiantMultiplier = tier.Equals(ItemManager.TacticTier.Radiant) ? ConfigManager.Scaling.radiantItemStatMultiplier : 1f;

            RecalculateStatsAPI.GetStatCoefficients += (self, args) =>
            {
                if (self && self.inventory)
                {
                    int count = self.inventory.GetItemCountEffective(def);
                    if (count > 0)
                    {
                        int stacks = self.GetBuffCount(bDef);
                        args.damageTotalMult *= 1 + Utilities.GetLinearStacking(percentDamagePerStack, percentDamagePerStackExtraStacks, count) * radiantMultiplier * stacks;
                    }
                }
            };

            GameEventManager.OnTakeDamage += (damageReport) =>
            {
                CharacterBody vicBody = damageReport.victimBody;
                CharacterBody atkBody = damageReport.attackerBody;
                foreach (var body in new CharacterBody[] { vicBody, atkBody })
                {
                    if (body && body.inventory && body.inventory.GetItemCountEffective(def) > 0 && body.GetBuffCount(bDef) < maxStacks.Value)
                    {
                        body.AddTimedBuff(bDef, duration.Value * radiantMultiplier);
                    }
                }
            };
        }
    }
}
