using RiskOfTactics.Managers;
using RoR2;
using System;

namespace RiskOfTactics.Content.Items.Completes
{
    class SpearOfShojin
    {
        public static ItemDef itemDef;
        public static ItemDef radiantDef;

        // Chance to refund a percentage of your active cooldowns on-hit.
        public static ConfigurableValue<bool> isEnabled = new(
            "Item: Spear Of Shojin",
            "Enabled",
            true,
            "Whether or not the item is enabled.",
            ["ITEM_ROT_SPEAROFSHOJIN_DESC"]
        );
        public static ConfigurableValue<float> chanceToProc = new(
            "Item: Spear Of Shojin",
            "On-Hit Chance",
            60f,
            "Percentage chance to trigger the cooldown refund.",
            ["ITEM_ROT_SPEAROFSHOJIN_DESC"],
            false
        );
        public static ConfigurableValue<float> cooldownOnHit = new(
            "Item: Spear Of Shojin",
            "On-Hit Cooldown",
            8f,
            "Percentage of remaining cooldown refunded on-hit.",
            ["ITEM_ROT_SPEAROFSHOJIN_DESC"],
            true
        );
        public static ConfigurableValue<float> cooldownOnHitExtraStacks = new(
            "Item: Spear Of Shojin",
            "On-Hit Cooldown Extra Stacks",
            5f,
            "Percentage of remaining cooldown refunded on-hit with extra stacks.",
            ["ITEM_ROT_SPEAROFSHOJIN_DESC"],
            true
        );
        public static readonly float percentCooldownOnHit = cooldownOnHit.Value / 100f;
        public static readonly float percentCooldownOnHitExtraStacks = cooldownOnHitExtraStacks.Value / 100f;

        internal static void Init()
        {
            itemDef = ItemManager.GenerateItem("SpearOfShojin", [ItemTag.Damage, ItemTag.Utility, ItemTag.CanBeTemporary], ItemManager.TacticTier.Normal);
            radiantDef = ItemManager.GenerateItem("Radiant_SpearOfShojin", [ItemTag.Damage, ItemTag.Utility, ItemTag.CanBeTemporary], ItemManager.TacticTier.Radiant);

            //Utilities.RegisterRadiantUpgrade(itemDef, radiantDef);

            Hooks(itemDef, ItemManager.TacticTier.Normal);
            Hooks(radiantDef, ItemManager.TacticTier.Radiant);
        }

        public static void Hooks(ItemDef def, ItemManager.TacticTier tier)
        {
            float radiantMultiplier = tier.Equals(ItemManager.TacticTier.Radiant) ? ConfigManager.Scaling.radiantItemStatMultiplier : 1f;

            GameEventManager.OnHitEnemy += (damageInfo, attackerInfo, victimInfo) =>
            {
                CharacterBody vicBody = victimInfo.body;
                CharacterBody atkBody = attackerInfo.body;

                if (atkBody && atkBody.inventory && Utilities.IsValidTargetBody(vicBody))
                {
                    int count = atkBody.inventory.GetItemCountEffective(def);
                    if (count > 0 && atkBody.master && Util.CheckRoll(chanceToProc.Value, atkBody.master))
                    {
                        if (atkBody.skillLocator)
                        {
                            foreach (SkillSlot slot in Enum.GetValues(typeof(SkillSlot)))
                            {
                                GenericSkill skill = atkBody.skillLocator.GetSkill(slot);
                                if (skill && skill.stock < skill.maxStock)
                                {
                                    float cooldownLeft = skill.finalRechargeInterval - skill.rechargeStopwatch;
                                    skill.rechargeStopwatch += cooldownLeft * Utilities.GetHyperbolicStacking(percentCooldownOnHit * radiantMultiplier, percentCooldownOnHitExtraStacks * radiantMultiplier, count);
                                }
                            }
                        }
                    }
                }
            };
        }
    }
}
