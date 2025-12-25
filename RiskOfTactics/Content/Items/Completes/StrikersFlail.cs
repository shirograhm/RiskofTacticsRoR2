using R2API;
using RiskOfTactics.Helpers;
using RoR2;
using UnityEngine;

namespace RiskOfTactics.Content.Items.Completes
{
    class StrikersFlail
    {
        public static ItemDef itemDef;
        public static BuffDef damageAmpBuff;

        public static ItemDef radiantDef;

        // Gain crit chance. Your critical strikes grant stacking damage amp.
        public static ConfigurableValue<bool> isEnabled = new(
            "Item: Strikers Flail",
            "Enabled",
            true,
            "Whether or not the item is enabled.",
            ["ITEM_ROT_HANDOFJUSTICE_DESC"]
        );
        public static ConfigurableValue<float> critChanceBonus = new(
            "Item: Strikers Flail",
            "Crit Chance",
            5f,
            "Crit chance gained when holding this item.",
            ["ITEM_ROT_STRIKERSFLAIL_DESC"],
            false
        );
        public static ConfigurableValue<float> damageAmp = new(
            "Item: Strikers Flail",
            "Damage Amp",
            5f,
            "Percent damage amp gained from the damage amp buff for the first item stack.",
            ["ITEM_ROT_STRIKERSFLAIL_DESC"],
            true
        );
        public static ConfigurableValue<float> damageAmpExtraStacks = new(
            "Item: Strikers Flail",
            "Damage Amp Extra Stacks",
            5f,
            "Percent damage amp gained from the damage amp buff for additional item stacks.",
            ["ITEM_ROT_STRIKERSFLAIL_DESC"],
            true
        );
        public static ConfigurableValue<float> damageAmpDuration = new(
            "Item: Strikers Flail",
            "Damage Amp Duration",
            5f,
            "Duration of the damage amp buff.",
            ["ITEM_ROT_STRIKERSFLAIL_DESC"],
            true
        );
        public static ConfigurableValue<int> maxBuffStacks = new(
            "Item: Strikers Flail",
            "Max Stacks",
            5,
            "Max stacks of the damage amp buff.",
            ["ITEM_ROT_STRIKERSFLAIL_DESC"],
            false
        );
        private static readonly float percentdamageAmp = damageAmp.Value / 100f;
        private static readonly float percentDamageAmpExtraStacks = damageAmpExtraStacks.Value / 100f;

        internal static void Init()
        {
            itemDef = ItemHelper.GenerateItem("StrikersFlail", [ItemTag.Damage, ItemTag.CanBeTemporary], ItemHelper.TacticTier.Normal);
            radiantDef = ItemHelper.GenerateItem("Radiant_StrikersFlail", [ItemTag.Damage, ItemTag.CanBeTemporary], ItemHelper.TacticTier.Radiant);

            damageAmpBuff = Utilities.GenerateBuffDef("Damage Amp", AssetHandler.bundle.LoadAsset<Sprite>("DamageAmp.png"), canStack: true, isHidden: false, isDebuff: false, isCooldown: false);
            ContentAddition.AddBuffDef(damageAmpBuff);

            Utilities.RegisterVoidPair(itemDef, radiantDef);

            Hooks(itemDef, ItemHelper.TacticTier.Normal);
            Hooks(radiantDef, ItemHelper.TacticTier.Radiant);
        }

        public static void Hooks(ItemDef def, ItemHelper.TacticTier tier)
        {
            float radiantMultiplier = tier.Equals(ItemHelper.TacticTier.Radiant) ? ConfigManager.Scaling.radiantItemStatMultiplier : 1f;

            RecalculateStatsAPI.GetStatCoefficients += (sender, args) =>
            {
                if (sender && sender.inventory)
                {
                    int count = sender.inventory.GetItemCountEffective(def);
                    if (count > 0)
                    {
                        args.critAdd += critChanceBonus.Value;
                    }
                }
            };

            GenericGameEvents.BeforeTakeDamage += (damageInfo, attackerInfo, victimInfo) =>
            {
                CharacterBody vicBody = victimInfo.body;
                CharacterBody atkBody = attackerInfo.body;

                if (vicBody && atkBody && atkBody.inventory)
                {
                    int count = atkBody.inventory.GetItemCountEffective(def);
                    int buffCount = atkBody.GetBuffCount(damageAmpBuff);
                    if (count > 0 && buffCount > 0 && !Utilities.OnSameTeam(vicBody, atkBody))
                    {
                        damageInfo.damage *= 1 + Utilities.GetLinearStacking(percentdamageAmp * radiantMultiplier, percentDamageAmpExtraStacks * radiantMultiplier, buffCount);
                        damageInfo.damageColorIndex = DamageColorIndex.WeakPoint;
                        if (buffCount == maxBuffStacks.Value)
                        {
                            damageInfo.damageColorIndex = DamageColorIndex.Luminous;
                        }
                    }
                }
            };

            GenericGameEvents.OnHitEnemy += (damageInfo, attackerInfo, victimInfo) =>
            {
                CharacterBody vicBody = victimInfo.body;
                CharacterBody atkBody = attackerInfo.body;

                if (vicBody && atkBody && atkBody.inventory)
                {
                    int count = atkBody.inventory.GetItemCountEffective(def);
                    if (count > 0 && !Utilities.OnSameTeam(vicBody, atkBody))
                    {
                        if (damageInfo.crit && atkBody.GetBuffCount(damageAmpBuff) < maxBuffStacks.Value)
                        {
                            atkBody.AddTimedBuff(damageAmpBuff, damageAmpDuration.Value * radiantMultiplier);
                        }
                    }
                }
            };
        }
    }
}
