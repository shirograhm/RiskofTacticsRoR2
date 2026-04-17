using R2API;
using RiskOfTactics.Managers;
using RoR2;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace RiskOfTactics.Content.Items.Completes
{
    class Quicksilver
    {
        public static ItemDef itemDef;
        public static BuffDef hiddenCooldownBuff;

        public static ItemDef radiantDef;
        public static BuffDef radiantHiddenCooldownBuff;

        public static BuffDef flowBuff;
        public static BuffDef cleanseBuff;
        public static GameObject ccShieldPrefab;
        public static TemporaryVisualEffect ccShieldEffectInstance;

        // When the teleporter is activated, gain immunity to crowd control for a duration. During this time, gain attack speed every second.
        public static ConfigurableValue<bool> isEnabled = new(
            "Item: Quicksilver",
            "Enabled",
            true,
            "Whether or not the item is enabled.",
            ["ITEM_ROT_QUICKSILVER_DESC"]
        );
        public static ConfigurableValue<float> ccImmunityDuration = new(
            "Item: Quicksilver",
            "CC Immunity Duration",
            20f,
            "Number of seconds immune to crowd control once the teleporter event starts.",
            ["ITEM_ROT_QUICKSILVER_DESC"],
            true
        );
        public static ConfigurableValue<float> ccImmunityDurationExtraStacks = new(
            "Item: Quicksilver",
            "CC Immunity Duration Extra Stacks",
            20f,
            "Number of seconds immune to crowd control once the teleporter event starts.",
            ["ITEM_ROT_QUICKSILVER_DESC"],
            false
        );
        public static ConfigurableValue<float> attackSpeedPerBuff = new(
            "Item: Quicksilver",
            "Attack Speed",
            1f,
            "Attack speed gained per second while immune to CC.",
            ["ITEM_ROT_QUICKSILVER_DESC"],
            true
        );
        private static readonly float percentAttackSpeedPerBuff = attackSpeedPerBuff.Value / 100f;

        internal static void Init()
        {
            itemDef = ItemManager.GenerateItem("Quicksilver", [ItemTag.Damage, ItemTag.Utility, ItemTag.CanBeTemporary], ItemManager.TacticTier.Normal);
            hiddenCooldownBuff = Utilities.GenerateBuffDef("QuicksilverTicker", AssetManager.bundle.LoadAsset<Sprite>("Quicksilver.png"), false, true, false, true);
            ContentAddition.AddBuffDef(hiddenCooldownBuff);
            radiantDef = ItemManager.GenerateItem("Radiant_Quicksilver", [ItemTag.Damage, ItemTag.Utility, ItemTag.CanBeTemporary], ItemManager.TacticTier.Radiant);
            radiantHiddenCooldownBuff = Utilities.GenerateBuffDef("RadiantQuicksilverTicker", AssetManager.bundle.LoadAsset<Sprite>("RadiantQuicksilver.png"), false, true, false, true);
            ContentAddition.AddBuffDef(radiantHiddenCooldownBuff);

            flowBuff = Utilities.GenerateBuffDef("Flow", AssetManager.bundle.LoadAsset<Sprite>("Flow.png"), true, false, false, false);
            ContentAddition.AddBuffDef(flowBuff);
            cleanseBuff = Utilities.GenerateBuffDef("Cleanse", AssetManager.bundle.LoadAsset<Sprite>("Cleanse.png"), false, false, false, true);
            ContentAddition.AddBuffDef(cleanseBuff);

            ccShieldPrefab = Addressables.LoadAssetAsync<GameObject>("RoR2/DLC1/OutOfCombatArmor/OutOfCombatArmorEffect.prefab").WaitForCompletion();

            Utilities.RegisterRadiantUpgrade(itemDef, radiantDef);

            Hooks(itemDef, hiddenCooldownBuff, ItemManager.TacticTier.Normal);
            Hooks(radiantDef, radiantHiddenCooldownBuff, ItemManager.TacticTier.Radiant);
        }

        public static void Hooks(ItemDef def, BuffDef hiddenBuffDef, ItemManager.TacticTier tier)
        {
            float radiantMultiplier = tier.Equals(ItemManager.TacticTier.Radiant) ? ConfigManager.Scaling.radiantItemStatMultiplier : 1f;

            RecalculateStatsAPI.GetStatCoefficients += (sender, args) =>
            {
                if (sender)
                {
                    int buffCount = sender.GetBuffCount(flowBuff);
                    args.attackSpeedMultAdd += buffCount * percentAttackSpeedPerBuff * radiantMultiplier;
                }
            };

            On.RoR2.HoldoutZoneController.Start += (orig, self) =>
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
                            body.AddTimedBuff(cleanseBuff, Utilities.GetLinearStacking(ccImmunityDuration.Value, ccImmunityDurationExtraStacks.Value, body.inventory.GetItemCountEffective(def)));
                            Utilities.UpdateSingleTemporaryVisualEffect(ref ccShieldEffectInstance, ccShieldPrefab, body, true);
                            // Add hidden buff CD
                            body.AddTimedBuff(hiddenBuffDef, 1f);
                        }
                    }
                }
            };

            On.RoR2.CharacterBody.OnBuffFinalStackLost += (orig, self, buffDef) =>
            {
                orig(self, buffDef);
                if (buffDef == cleanseBuff)
                {
                    Utilities.UpdateSingleTemporaryVisualEffect(ref ccShieldEffectInstance, ccShieldPrefab, self, false);
                }
                if (buffDef == hiddenBuffDef && self.HasBuff(cleanseBuff))
                {
                    self.AddBuff(flowBuff);
                    self.AddTimedBuff(hiddenBuffDef, 1f);
                }
            };

            GameEventManager.BeforeTakeDamage += (damageInfo, attackerInfo, victimInfo) =>
            {
                // CC immunity
                if (victimInfo.body && victimInfo.body.HasBuff(cleanseBuff))
                {
                    damageInfo.force = Vector3.zero;
                }
            };
        }
    }
}
