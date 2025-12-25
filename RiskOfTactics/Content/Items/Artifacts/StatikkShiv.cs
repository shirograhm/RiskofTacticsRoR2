using R2API;
using RiskOfTactics.Content.Buffs;
using RiskOfTactics.Helpers;
using RoR2;
using RoR2.Orbs;
using UnityEngine;

namespace RiskOfTactics.Content.Items.Artifacts
{
    class StatikkShiv
    {
        public static ItemDef itemDef;
        public static BuffDef shockBuff;
        public static BuffDef shockCooldown;

        // Your next attack periodically chains to nearby enemies, dealing bonus damage and applying Sunder.
        public static ConfigurableValue<bool> isEnabled = new(
            "Item: Statikk Shiv",
            "Enabled",
            true,
            "Whether or not the item is enabled.",
            ["ITEM_ROT_STATIKKSHIV_DESC"]
        );
        public static ConfigurableValue<float> effectCooldown = new(
            "Item: Statikk Shiv",
            "Effect Cooldown",
            6f,
            "Cooldown of this item's effect.",
            ["ITEM_ROT_STATIKKSHIV_DESC"]
        );
        public static ConfigurableValue<float> effectOnHitDamage = new(
            "Item: Statikk Shiv",
            "Bonus On-Hit",
            240f,
            "Bonus on-hit damage (as a percentage of TOTAL damage) dealt by this item's effect.",
            ["ITEM_ROT_STATIKKSHIV_DESC"]
        );
        public static ConfigurableValue<float> effectOnHitDamageExtraStacks = new(
            "Item: Statikk Shiv",
            "Bonus On-Hit Per Stack",
            240f,
            "Bonus on-hit damage for extra stacks of this item.",
            ["ITEM_ROT_STATIKKSHIV_DESC"]
        );
        public static ConfigurableValue<int> shivRange = new(
            "Item: Statikk Shiv",
            "Zap Range",
            30,
            "Zap range of the item's chain effect in meters.",
            ["ITEM_ROT_STATIKKSHIV_DESC"]
        );
        public static ConfigurableValue<int> numberEnemiesChained = new(
            "Item: Statikk Shiv",
            "Number of Enemies Chained",
            3,
            "Number of additional enemies chained off the initial enemy with this effect.",
            ["ITEM_ROT_STATIKKSHIV_DESC"]
        );
        public static ConfigurableValue<float> shivProcCoeff = new(
            "Item: Statikk Shiv",
            "Proc Coefficient",
            1f,
            "Proc coefficient for the chain effect of this item.",
            ["ITEM_ROT_STATIKKSHIV_DESC"]
        );
        public static float percentEffectOnHitDamage = effectOnHitDamage.Value / 100f;
        public static float percentEffectOnHitDamageExtraStacks = effectOnHitDamageExtraStacks.Value / 100f;

        internal static void Init()
        {
            itemDef = ItemHelper.GenerateItem("StatikkShiv", [ItemTag.Damage, ItemTag.Utility, ItemTag.CanBeTemporary], ItemHelper.TacticTier.Artifact);

            shockBuff = Utilities.GenerateBuffDef("Shock", AssetHandler.bundle.LoadAsset<Sprite>("Shock.png"), false, false, false, false);
            ContentAddition.AddBuffDef(shockBuff);
            shockCooldown = Utilities.GenerateBuffDef("Shock Cooldown", AssetHandler.bundle.LoadAsset<Sprite>("ShockCD.png"), false, false, false, true);
            ContentAddition.AddBuffDef(shockCooldown);

            Hooks();
        }

        public static void Hooks()
        {
            Stage.onStageStartGlobal += (stage) =>
            {
                foreach (PlayerCharacterMasterController controller in PlayerCharacterMasterController.instances)
                {
                    if (controller)
                    {
                        CharacterMaster master = controller.master;
                        if (master && master.GetBody() && master.GetBody().inventory && master.GetBody().inventory.GetItemCountEffective(itemDef) > 0)
                        {
                            master.GetBody().AddBuff(shockBuff);
                        }
                    }
                }
            };

            On.RoR2.Inventory.GiveItemPermanent_ItemIndex_int += (orig, self, index, count) =>
            {
                orig(self, index, count);

                if (index == itemDef.itemIndex)
                {
                    CharacterMaster master = self.GetComponent<CharacterMaster>();
                    if (master && master.GetBody() && self.GetItemCountEffective(itemDef) > 0)
                    {
                        master.GetBody().AddBuff(shockBuff);
                    }
                }
            };

            On.RoR2.Inventory.GiveItemTemp += (orig, self, index, count) =>
            {
                orig(self, index, count);

                if (index == itemDef.itemIndex)
                {
                    CharacterMaster master = self.GetComponent<CharacterMaster>();
                    if (master && master.GetBody() && self.GetItemCountEffective(itemDef) > 0)
                    {
                        master.GetBody().AddBuff(shockBuff);
                    }
                }
            };

            On.RoR2.CharacterBody.OnBuffFinalStackLost += (orig, self, buffDef) =>
            {
                orig(self, buffDef);

                if (buffDef == shockCooldown)
                {
                    if (self && self.inventory && self.inventory.GetItemCountEffective(itemDef) > 0 && self.GetBuffCount(shockBuff) == 0)
                    {
                        self.AddBuff(shockBuff);
                    }
                }
            };

            GenericGameEvents.BeforeTakeDamage += (damageInfo, attackerInfo, victimInfo) =>
            {
                CharacterBody vicBody = victimInfo.body;
                CharacterBody atkBody = attackerInfo.body;

                if (vicBody && atkBody && atkBody.inventory)
                {
                    int count = atkBody.inventory.GetItemCountEffective(itemDef);
                    bool hasShockBuff = atkBody.GetBuffCount(shockBuff) > 0;
                    if (hasShockBuff && !Utilities.OnSameTeam(vicBody, atkBody))
                    {
                        float damageMultiplier = 1 + Utilities.GetLinearStacking(percentEffectOnHitDamage, percentEffectOnHitDamageExtraStacks, count);
                        damageInfo.damage *= damageMultiplier;
                        damageInfo.damageColorIndex = DamageColorIndex.WeakPoint;

                        AddStatikkChainDamage(atkBody, vicBody, damageInfo, damageMultiplier);

                        vicBody.AddBuff(Sunder.buffDef);

                        // Remove the shock buff and add cooldown buff
                        atkBody.RemoveBuff(shockBuff);
                        atkBody.AddTimedBuff(shockCooldown, effectCooldown.Value);
                    }
                }
            };
        }

        private static void AddStatikkChainDamage(CharacterBody attacker, CharacterBody victim, DamageInfo damageInfo, float mult)
        {
            if (attacker.teamComponent != null)
            {
                LightningOrb orb = new()
                {
                    origin = damageInfo.position,
                    damageValue = damageInfo.damage * mult,
                    isCrit = damageInfo.crit,
                    bouncesRemaining = numberEnemiesChained.Value,
                    teamIndex = attacker.teamComponent.teamIndex,
                    attacker = attacker.gameObject,
                    bouncedObjects = [victim.GetComponent<HealthComponent>()],
                    procChainMask = damageInfo.procChainMask,
                    procCoefficient = shivProcCoeff,
                    lightningType = LightningOrb.LightningType.MageLightning,
                    damageColorIndex = DamageColorIndex.WeakPoint,
                    range = shivRange,
                    canBounceOnSameTarget = false
                };
                orb.AddModdedDamageType(Sunder.ApplySunder);

                HurtBox hb = orb.PickNextTarget(damageInfo.position);
                if (hb)
                {
                    orb.target = hb;
                    OrbManager.instance.AddOrb(orb);
                }
            }
        }
    }
}
