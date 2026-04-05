using R2API;
using RiskOfTactics.Managers;
using RoR2;
using UnityEngine;

namespace RiskOfTactics.Content.Items.Artifacts
{
    class LightshieldCrest
    {
        public static ItemDef itemDef;
        public static BuffDef crestShieldCooldownBuff;

        // Periodically shield the lowest health ally for a portion of your armor.
        public static ConfigurableValue<bool> isEnabled = new(
            "Item: Lightshield Crest",
            "Enabled",
            true,
            "Whether or not the item is enabled.",
            ["ITEM_ROT_LIGHTSHIELDCREST_DESC"]
        );
        public static ConfigurableValue<float> interval = new(
            "Item: Lightshield Crest",
            "Interval",
            10f,
            "Interval in seconds between each shield application.",
            ["ITEM_ROT_LIGHTSHIELDCREST_DESC"]
        );
        public static ConfigurableValue<float> armor = new(
            "Item: Lightshield Crest",
            "Armor",
            50f,
            "Amount of armor to apply as a shield to the lowest health ally.",
            ["ITEM_ROT_LIGHTSHIELDCREST_DESC"]
        );
        public static ConfigurableValue<float> armorPercent = new(
            "Item: Lightshield Crest",
            "Percent Armor",
            50f,
            "Percent of your armor to apply as a shield to the lowest health ally.",
            ["ITEM_ROT_LIGHTSHIELDCREST_DESC"]
        );
        public static ConfigurableValue<float> armorPercentExtraStacks = new(
            "Item: Lightshield Crest",
            "Percent Armor Extra Stacks",
            50f,
            "Percent of your armor to apply as a shield to the lowest health ally for each extra stack of this item.",
            ["ITEM_ROT_LIGHTSHIELDCREST_DESC"]
        );
        public static float percentArmor = armorPercent.Value / 100f;
        public static float percentArmorExtraStacks = armorPercentExtraStacks.Value / 100f;

        internal static void Init()
        {
            itemDef = ItemManager.GenerateItem("LightshieldCrest", [ItemTag.Utility, ItemTag.CanBeTemporary], ItemManager.TacticTier.Artifact);

            crestShieldCooldownBuff = Utilities.GenerateBuffDef("LightshieldCrestCooldown", AssetManager.bundle.LoadAsset<Sprite>("LightshieldCrestCooldown"), false, false, false, true);
            ContentAddition.AddBuffDef(crestShieldCooldownBuff);

            Hooks();
        }

        public static void Hooks()
        {
            RecalculateStatsAPI.GetStatCoefficients += (sender, args) =>
            {
                if (sender && sender.inventory)
                {
                    int count = sender.inventory.GetItemCountEffective(itemDef);
                    if (count > 0)
                    {
                        args.armorAdd += armor.Value;
                    }
                }
            };

            On.RoR2.CharacterBody.OnBuffFinalStackLost += (orig, self, buffDef) =>
            {
                orig(self, buffDef);

                if (buffDef == crestShieldCooldownBuff)
                {
                    CharacterBody lowest = Utilities.GetLowestHealthAlly(self.teamComponent.teamIndex);
                    if (lowest && lowest.healthComponent)
                    {
                        int count = self.inventory.GetItemCountEffective(itemDef);
                        float shieldAmount = self.armor * Utilities.GetLinearStacking(percentArmor, percentArmorExtraStacks, count);
                        lowest.healthComponent.AddBarrier(shieldAmount);

                        if (count > 0)
                        {
                            self.AddTimedBuff(crestShieldCooldownBuff, interval.Value);
                        }
                    }
                }
            };

            On.RoR2.Inventory.GiveItemPermanent_ItemIndex_int += (orig, self, index, count) =>
            {
                CharacterMaster master = self.GetComponent<CharacterMaster>();

                if (master && index == itemDef.itemIndex)
                {
                    if (master.GetBody()) master.GetBody().AddTimedBuff(crestShieldCooldownBuff, interval.Value);
                }

                orig(self, index, count);
            };

            On.RoR2.Inventory.GiveItemTemp += (orig, self, index, count) =>
            {
                CharacterMaster master = self.GetComponent<CharacterMaster>();

                if (master && index == itemDef.itemIndex)
                {
                    if (master.GetBody()) master.GetBody().AddTimedBuff(crestShieldCooldownBuff, interval.Value);
                }

                orig(self, index, count);
            };
        }
    }
}

