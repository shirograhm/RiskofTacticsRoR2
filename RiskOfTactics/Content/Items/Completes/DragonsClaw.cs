using R2API;
using RiskOfTactics.Managers;
using RoR2;
using UnityEngine;

namespace RiskOfTactics.Content.Items.Completes
{
    class DragonsClaw
    {
        public static ItemDef itemDef;
        public static BuffDef dragonsClawCooldownBuff;

        public static ItemDef radiantDef;
        public static BuffDef dragonsClawRadiantCooldownBuff;

        // Gain health. Periodically heal for a portion of your max HP.
        public static ConfigurableValue<bool> isEnabled = new(
            "Item: Dragons Claw",
            "Enabled",
            true,
            "Whether or not the item is enabled.",
            ["ITEM_ROT_DRAGONSCLAW_DESC"]
        );
        public static ConfigurableValue<float> maxHealthBonus = new(
            "Item: Dragons Claw",
            "Percent Health",
            9f,
            "Percent max health bonus when holding this item.",
            ["ITEM_ROT_DRAGONSCLAW_DESC"],
            true
        );
        public static ConfigurableValue<float> maxHealthBonusExtraStacks = new(
            "Item: Dragons Claw",
            "Percent Health Per Stack",
            9f,
            "Percent max health bonus when holding extra stacks of this item.",
            ["ITEM_ROT_DRAGONSCLAW_DESC"],
            true
        );
        public static ConfigurableValue<float> healingPerTick = new(
            "Item: Dragons Claw",
            "Healing Per Tick",
            4.5f,
            "Percent max health healing per item proc.",
            ["ITEM_ROT_DRAGONSCLAW_DESC"],
            true
        );
        public static ConfigurableValue<float> tickDuration = new(
            "Item: Dragons Claw",
            "Tick Duration",
            9f,
            "Number of seconds between item procs.",
            ["ITEM_ROT_DRAGONSCLAW_DESC"],
            false
        );
        public static readonly float percentHealingPerTick = healingPerTick.Value / 100f;
        public static readonly float percentMaxHealthBonus = maxHealthBonus.Value / 100f;
        public static readonly float percentMaxHealthBonusExtraStacks = maxHealthBonusExtraStacks.Value / 100f;

        internal static void Init()
        {
            itemDef = ItemManager.GenerateItem("DragonsClaw", [ItemTag.Healing, ItemTag.Utility, ItemTag.CanBeTemporary], ItemManager.TacticTier.Normal);
            dragonsClawCooldownBuff = Utilities.GenerateBuffDef("DragonsClawCooldown", AssetManager.bundle.LoadAsset<Sprite>("DragonsClawCooldown"), false, false, false, true);
            ContentAddition.AddBuffDef(dragonsClawCooldownBuff);

            radiantDef = ItemManager.GenerateItem("Radiant_DragonsClaw", [ItemTag.Healing, ItemTag.Utility, ItemTag.CanBeTemporary], ItemManager.TacticTier.Radiant);
            dragonsClawRadiantCooldownBuff = Utilities.GenerateBuffDef("DragonsClawRadiantCooldown", AssetManager.bundle.LoadAsset<Sprite>("DragonsClawRadiantCooldown"), false, false, false, true);
            ContentAddition.AddBuffDef(dragonsClawRadiantCooldownBuff);

            if (ConfigManager.Scaling.useRadiantAutoConversion) Utilities.RegisterRadiantUpgrade(itemDef, radiantDef);

            Hooks(itemDef, dragonsClawCooldownBuff, ItemManager.TacticTier.Normal);
            Hooks(radiantDef, dragonsClawRadiantCooldownBuff, ItemManager.TacticTier.Radiant);
        }

        public static void Hooks(ItemDef def, BuffDef cooldownBuff, ItemManager.TacticTier tier)
        {
            float radiantMultiplier = tier.Equals(ItemManager.TacticTier.Radiant) ? ConfigManager.Scaling.radiantItemStatMultiplier : 1f;

            RecalculateStatsAPI.GetStatCoefficients += (sender, args) =>
            {
                if (sender && sender.inventory)
                {
                    int count = sender.inventory.GetItemCountEffective(def);
                    if (count > 0)
                    {
                        args.healthTotalMult *= 1 + Utilities.GetLinearStacking(percentMaxHealthBonus * radiantMultiplier, percentMaxHealthBonusExtraStacks * radiantMultiplier, count);
                    }
                }
            };

            On.RoR2.CharacterBody.OnBuffFinalStackLost += (orig, self, buffDef) =>
            {
                orig(self, buffDef);

                if (buffDef == cooldownBuff)
                {
                    self.healthComponent.Heal(self.maxHealth * percentHealingPerTick, default, true);
                    Utilities.SpawnHealEffect(self);

                    if (self.inventory && self.inventory.GetItemCountEffective(def) > 0)
                        self.AddTimedBuff(cooldownBuff, tickDuration.Value);
                }
            };

            On.RoR2.Inventory.GiveItemPermanent_ItemIndex_int += (orig, self, index, count) =>
            {
                CharacterMaster master = self.GetComponent<CharacterMaster>();
                if (master && index == def.itemIndex)
                {
                    if (master.GetBody()) master.GetBody().AddTimedBuff(cooldownBuff, tickDuration.Value);
                }

                orig(self, index, count);
            };

            On.RoR2.Inventory.GiveItemTemp += (orig, self, index, count) =>
            {
                CharacterMaster master = self.GetComponent<CharacterMaster>();
                if (master && index == def.itemIndex)
                {
                    if (master.GetBody()) master.GetBody().AddTimedBuff(cooldownBuff, tickDuration.Value);
                }

                orig(self, index, count);
            };
        }
    }
}
