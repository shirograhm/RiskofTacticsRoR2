using R2API;
using RiskOfTactics.Managers;
using RoR2;
using RoR2.Items;
using UnityEngine;

namespace RiskOfTactics.Content.Items.Completes
{
    public class ArchangelsStaffItemBehavior : BaseItemBodyBehavior
    {
        [ItemDefAssociation(useOnServer = true, useOnClient = false)]
        public static ItemDef GetItemDef()
        {
            return ArchangelsStaff.itemDef;
        }

        public void FixedUpdate()
        {
            ArchangelsStaff.FixedUpdateHook(body, stack, ArchangelsStaff.staffIntervalCooldown);
        }
    }

    public class ArchangelsStaffRadiantItemBehavior : BaseItemBodyBehavior
    {
        [ItemDefAssociation(useOnServer = true, useOnClient = false)]
        public static ItemDef GetItemDef()
        {
            return ArchangelsStaff.radiantDef;
        }

        public void FixedUpdate()
        {
            ArchangelsStaff.FixedUpdateHook(body, stack, ArchangelsStaff.staffRadiantIntervalCooldown);
        }
    }

    class ArchangelsStaff
    {
        public static BuffDef foresightBuff;

        public static ItemDef itemDef;
        public static BuffDef staffIntervalCooldown;

        public static ItemDef radiantDef;
        public static BuffDef staffRadiantIntervalCooldown;

        // During the teleporter event, periodically gain BASE damage.
        public static ConfigurableValue<bool> isEnabled = new(
            "Item: Archangels Staff",
            "Enabled",
            true,
            "Whether or not the item is enabled.",
            ["ITEM_ROT_ARCHANGELSSTAFF_DESC"]
        );
        public static ConfigurableValue<float> flatDamagePerTick = new(
            "Item: Archangels Staff",
            "Flat Damage Per Tick",
            0.5f,
            "Flat damage gained per item proc.",
            ["ITEM_ROT_ARCHANGELSSTAFF_DESC"],
            true
        );
        public static ConfigurableValue<float> tickDuration = new(
            "Item: Archangels Staff",
            "Tick Duration",
            5f,
            "Number of seconds between item procs.",
            ["ITEM_ROT_ARCHANGELSSTAFF_DESC"],
            false
        );

        internal static void Init()
        {
            // Normal Variant
            itemDef = ItemManager.GenerateItem("ArchangelsStaff", [ItemTag.Damage, ItemTag.CanBeTemporary], ItemManager.TacticTier.Normal);
            radiantDef = ItemManager.GenerateItem("Radiant_ArchangelsStaff", [ItemTag.Damage, ItemTag.CanBeTemporary], ItemManager.TacticTier.Radiant);

            foresightBuff = Utilities.GenerateBuffDef("Foresight", AssetManager.bundle.LoadAsset<Sprite>("Foresight.png"), true, false, false, false);
            ContentAddition.AddBuffDef(foresightBuff);

            staffIntervalCooldown = Utilities.GenerateBuffDef("ArchangelIntervalCooldown", AssetManager.bundle.LoadAsset<Sprite>("ArchangelsStaff.png"), false, true, false, true);
            ContentAddition.AddBuffDef(staffIntervalCooldown);
            staffRadiantIntervalCooldown = Utilities.GenerateBuffDef("RadiantArchangelIntervalCooldown", AssetManager.bundle.LoadAsset<Sprite>("ArchangelsStaff.png"), false, true, false, true);
            ContentAddition.AddBuffDef(staffRadiantIntervalCooldown);

            if (ConfigManager.Scaling.useRadiantAutoConversion) Utilities.RegisterRadiantUpgrade(itemDef, radiantDef);

            Hooks(itemDef, staffIntervalCooldown, ItemManager.TacticTier.Normal);
            Hooks(radiantDef, staffRadiantIntervalCooldown, ItemManager.TacticTier.Radiant);
        }

        public static void Hooks(ItemDef def, BuffDef cooldownBuff, ItemManager.TacticTier tier)
        {
            float radiantMultiplier = tier.Equals(ItemManager.TacticTier.Radiant) ? ConfigManager.Scaling.radiantItemStatMultiplier : 1f;

            RecalculateStatsAPI.GetStatCoefficients += (sender, args) =>
            {
                if (sender)
                {
                    int buffCount = sender.GetBuffCount(foresightBuff);
                    args.baseDamageAdd += buffCount * flatDamagePerTick.Value * radiantMultiplier;
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
                            body.AddTimedBuff(cooldownBuff, tickDuration.Value);
                        }
                    }
                }
            };

            On.RoR2.CharacterBody.OnBuffFinalStackLost += (orig, self, buffDef) =>
            {
                orig(self, buffDef);
                if (buffDef == cooldownBuff)
                {
                    if (self && self.inventory)
                    {
                        int itemCount = self.inventory.GetItemCountEffective(def);
                        if (itemCount > 0 && InstanceTracker.GetInstancesList<HoldoutZoneController>().Count > 0)
                        {
                            self.AddBuff(foresightBuff);
                            self.AddTimedBuff(cooldownBuff, tickDuration.Value);
                        }
                    }
                }
            };
        }

        public static void FixedUpdateHook(CharacterBody body, int itemCount, BuffDef cooldownBuff)
        {
            foreach (HoldoutZoneController hzc in InstanceTracker.GetInstancesList<HoldoutZoneController>())
            {
                if (body && body.inventory)
                {
                    if (itemCount > 0 && hzc.isActiveAndEnabled)
                    {
                        if (!body.HasBuff(cooldownBuff))
                            body.AddTimedBuff(cooldownBuff, tickDuration.Value);
                    }
                }
            }
        }
    }
}
