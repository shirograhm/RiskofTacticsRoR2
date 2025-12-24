using RiskOfTactics.Helpers;
using RoR2;
using RoR2.Orbs;
using UnityEngine;

namespace RiskOfTactics.Content.Items.Artifacts
{
    class HorizonFocus
    {
        public static ItemDef itemDef;

        public static GameObject lightningObject;

        // Your next attack periodically chains to nearby enemies, dealing bonus damage and applying Sunder.
        public static ConfigurableValue<bool> isEnabled = new(
            "Item: Horizon Focus",
            "Enabled",
            true,
            "Whether or not the item is enabled.",
            ["ITEM_ROT_HORIZONFOCUS_DESC"]
        );
        public static ConfigurableValue<float> stunChance = new(
            "Item: Horizon Focus",
            "Stun Chance On-Hit",
            8f,
            "Percent chance to stun enemies on-hit.",
            ["ITEM_ROT_HORIZONFOCUS_DESC"]
        );
        public static ConfigurableValue<float> lightningDamage = new(
            "Item: Horizon Focus",
            "Lightning Damage",
            18f,
            "Percent enemy max HP damage dealt by the lightning strike caused by this item.",
            ["ITEM_ROT_HORIZONFOCUS_DESC"]
        );
        public static ConfigurableValue<float> lightningDamageExtraStacks = new(
            "Item: Horizon Focus",
            "Lightning Damage Extra Stacks",
            12f,
            "Percent enemy max HP damage dealt by the lightning strike caused by extra stacks this item.",
            ["ITEM_ROT_HORIZONFOCUS_DESC"]
        );
        public static ConfigurableValue<float> lightningProcCoefficient = new(
            "Item: Horizon Focus",
            "Proc Coefficient",
            1f,
            "Proc coefficient for the lightning effect.",
            ["ITEM_ROT_HORIZONFOCUS_DESC"]
        );
        public static float percentStunChance = stunChance.Value / 100f;
        public static float percentLightningDamage = lightningDamage.Value / 100f;
        public static float percentLightningDamageExtraStacks = lightningDamageExtraStacks.Value / 100f;

        internal static void Init()
        {
            itemDef = ItemHelper.GenerateItem("HorizonFocus", [ItemTag.Damage, ItemTag.Utility, ItemTag.CanBeTemporary], ItemHelper.TacticTier.Artifact);

            //lightningObject = ;

            Hooks();
        }

        public static void Hooks()
        {
            GenericGameEvents.BeforeTakeDamage += (damageInfo, attackerInfo, victimInfo) =>
            {
                CharacterBody vicBody = victimInfo.body;
                CharacterBody atkBody = attackerInfo.body;

                if (vicBody && atkBody && atkBody.inventory && Utilities.IsValidTargetBody(vicBody))
                {
                    int count = atkBody.inventory.GetItemCountEffective(itemDef);
                    if (count > 0 && !Utilities.OnSameTeam(vicBody, atkBody))
                    {
                        if (Util.CheckRoll0To1(percentStunChance, atkBody.master))
                            damageInfo.damageType |= DamageType.Stun1s;
                    }
                }
            };

            GenericGameEvents.OnHitEnemy += (damageInfo, attackerInfo, victimInfo) =>
            {
                CharacterBody vicBody = victimInfo.body;
                CharacterBody atkBody = attackerInfo.body;

                if (vicBody && atkBody && atkBody.inventory && Utilities.IsValidTargetBody(vicBody))
                {
                    int count = atkBody.inventory.GetItemCountEffective(itemDef);
                    if (count > 0 && !Utilities.OnSameTeam(vicBody, atkBody))
                    {
                        float damageMultiplier = Utilities.GetLinearStacking(percentLightningDamage, percentLightningDamageExtraStacks, count);

                        SpawnLightningStrike(atkBody, vicBody, damageMultiplier);

                        //DamageInfo info = new DamageInfo()
                        //{
                        //    damage = vicBody.healthComponent.fullHealth * damageMultiplier,
                        //    damageColorIndex = DamageColorIndex.Electrocution,
                        //    attacker = atkBody.gameObject,
                        //    inflictor = atkBody.gameObject,
                        //    crit = atkBody.RollCrit(),
                        //    procCoefficient = 0f,
                        //    procChainMask = new ProcChainMask(),
                        //    position = vicBody.footPosition
                        //};

                        //vicBody.healthComponent.TakeDamage(info);
                    }
                }
            };
        }

        private static void SpawnLightningStrike(CharacterBody attackerBody, CharacterBody victimBody, float mult)
        {
            OrbManager.instance.AddOrb(new LightningStrikeOrb
            {
                attacker = attackerBody.gameObject,
                damageColorIndex = DamageColorIndex.Electrocution,
                damageValue = victimBody.healthComponent.fullHealth * mult,
                isCrit = Util.CheckRoll(attackerBody.crit, attackerBody.master),
                procChainMask = new ProcChainMask(),
                procCoefficient = lightningProcCoefficient,
                target = victimBody.mainHurtBox
            });
        }
    }
}

