using R2API;
using RiskOfTactics.Helpers;
using RoR2;
using UnityEngine;

namespace RiskOfTactics.Content.Items.Completes
{
    class Crownguard
    {
        public static ItemDef itemDef;
        public static BuffDef guardedBuff;
        public static BuffDef crownedBuff;

        public static ItemDef radiantDef;

        // Upon activation of the teleporter, gain a temporary shield. When the shield expires, gain permanent BASE damage.
        public static ConfigurableValue<bool> isEnabled = new(
            "Item: Crownguard",
            "Enabled",
            true,
            "Whether or not the item is enabled.",
            ["ITEM_ROT_CROWNGUARD_DESC"]
        );
        public static ConfigurableValue<float> effectShield = new(
            "Item: Crownguard",
            "Effect Shield",
            25f,
            "Percent max HP shield bonus when teleporter is activated.",
            ["ITEM_ROT_CROWNGUARD_DESC"],
            true
        );
        public static ConfigurableValue<float> effectShieldExtraStacks = new(
            "Item: Crownguard",
            "Effect Shield Extra Stacks",
            25f,
            "Percent max HP shield bonus with extra stacks when teleporter is activated.",
            ["ITEM_ROT_CROWNGUARD_DESC"],
            true
        );
        public static ConfigurableValue<float> effectDuration = new(
            "Item: Crownguard",
            "Effect Duration",
            30f,
            "How long the shield effect lasts when teleporter is activated.",
            ["ITEM_ROT_CROWNGUARD_DESC"],
            true
        );
        public static ConfigurableValue<float> effectDamage = new(
            "Item: Crownguard",
            "Effect Damage",
            4f,
            "Damage bonus given after the shield effect expires.",
            ["ITEM_ROT_CROWNGUARD_DESC"],
            true
        );
        public static ConfigurableValue<float> effectDamageExtraStacks = new(
            "Item: Crownguard",
            "Effect Damage Extra Stacks",
            1.5f,
            "Damage bonus given after the shield effect expires.",
            ["ITEM_ROT_CROWNGUARD_DESC"],
            true
        );
        private static readonly float percentEffectShield = effectShield.Value / 100f;
        private static readonly float percentEffectShieldExtraStacks = effectShieldExtraStacks.Value / 100f;

        internal static void Init()
        {
            itemDef = ItemHelper.GenerateItem("Crownguard", [ItemTag.Damage, ItemTag.Utility, ItemTag.CanBeTemporary], ItemHelper.TacticTier.Normal);
            radiantDef = ItemHelper.GenerateItem("Radiant_Crownguard", [ItemTag.Damage, ItemTag.Utility, ItemTag.CanBeTemporary], ItemHelper.TacticTier.Radiant);

            guardedBuff = Utilities.GenerateBuffDef("Guarded", AssetHandler.bundle.LoadAsset<Sprite>("Guarded.png"), false, false, false, true);
            ContentAddition.AddBuffDef(guardedBuff);
            crownedBuff = Utilities.GenerateBuffDef("Crowned", AssetHandler.bundle.LoadAsset<Sprite>("Crowned.png"), false, false, false, false);
            ContentAddition.AddBuffDef(crownedBuff);

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
                        if (sender.GetBuffCount(guardedBuff) > 0)
                            args.baseShieldAdd += sender.healthComponent.fullHealth * Utilities.GetLinearStacking(percentEffectShield * radiantMultiplier, percentEffectShieldExtraStacks * radiantMultiplier, count);

                        if (sender.GetBuffCount(crownedBuff) > 0)
                            args.baseDamageAdd += Utilities.GetLinearStacking(effectDamage.Value * radiantMultiplier, effectDamageExtraStacks.Value * radiantMultiplier, count);
                    }
                }
            };

            On.RoR2.CharacterBody.OnBuffFinalStackLost += (orig, self, buffDef) =>
            {
                orig(self, buffDef);

                if (self && buffDef == guardedBuff)
                    self.AddBuff(crownedBuff);
            };

            On.RoR2.HoldoutZoneController.Awake += (orig, self) =>
            {
                orig(self);

                foreach (NetworkUser user in NetworkUser.readOnlyInstancesList)
                {
                    CharacterMaster master = user.masterController.master ?? user.master;
                    if (master)
                    {
                        CharacterBody body = master.GetBody();
                        if (body && body.inventory && body.inventory.GetItemCountEffective(def) > 0)
                        {
                            body.AddTimedBuff(guardedBuff, effectDuration.Value * radiantMultiplier);
                        }
                    }
                }
            };
        }
    }
}
