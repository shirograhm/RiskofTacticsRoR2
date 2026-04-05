using R2API;
using RiskOfTactics.Managers;
using RoR2;

namespace RiskOfTactics.Content.Items.Artifacts
{
    internal class Mittens
    {
        public static ItemDef itemDef;

        // Become smaller and faster. You are immune to burn, anti-heal, and cripple.
        public static ConfigurableValue<bool> isEnabled = new(
            "Item: Mittens",
            "Enabled",
            true,
            "Whether or not the item is enabled.",
            ["ITEM_ROT_MITTENS_DESC"]
        );
        public static ConfigurableValue<float> movementSpeed = new(
            "Item: Mittens",
            "Movement Speed",
            20f,
            "Percent movement speed gained when holding this item.",
            ["ITEM_ROT_MITTENS_DESC"]
        );
        public static ConfigurableValue<float> movementSpeedExtraStacks = new(
            "Item: Mittens",
            "Movement Speed Extra Stacks",
            20f,
            "Percent movement speed gained when holding extra stacks of this item.",
            ["ITEM_ROT_MITTENS_DESC"]
        );
        public static ConfigurableValue<float> attackSpeed = new(
            "Item: Mittens",
            "Attack Speed",
            20f,
            "Percent attack speed gained when holding this item.",
            ["ITEM_ROT_MITTENS_DESC"]
        );
        public static ConfigurableValue<float> attackSpeedExtraStacks = new(
            "Item: Mittens",
            "Attack Speed Extra Stacks",
            20f,
            "Percent attack speed gained when holding extra stacks of this item.",
            ["ITEM_ROT_MITTENS_DESC"]
        );
        public static readonly float percentMovementSpeed = movementSpeed.Value / 100f;
        public static readonly float percentMovementSpeedExtraStacks = movementSpeedExtraStacks.Value / 100f;
        public static readonly float percentAttackSpeed = attackSpeed.Value / 100f;
        public static readonly float percentAttackSpeedExtraStacks = attackSpeedExtraStacks.Value / 100f;

        internal static void Init()
        {
            itemDef = ItemManager.GenerateItem("Mittens", [ItemTag.Damage, ItemTag.Utility, ItemTag.CanBeTemporary], ItemManager.TacticTier.Artifact);

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
                        args.moveSpeedMultAdd += Utilities.GetLinearStacking(percentMovementSpeed, percentMovementSpeedExtraStacks, count);
                        args.attackSpeedMultAdd += Utilities.GetLinearStacking(percentAttackSpeed, percentAttackSpeedExtraStacks, count);
                    }
                }
            };

            On.RoR2.CharacterBody.AddBuff_BuffDef += (orig, self, buffDef) =>
            {
                if (self && self.inventory)
                {
                    int count = self.inventory.GetItemCountEffective(itemDef);
                    if (count > 0)
                    {
                        if (buffDef == RoR2Content.Buffs.OnFire
                            || buffDef == RoR2Content.Buffs.HealingDisabled
                            || buffDef == RoR2Content.Buffs.Cripple)
                            return;
                    }
                }

                orig(self, buffDef);
            };

            On.RoR2.CharacterBody.AddBuff_BuffIndex += (orig, self, buffIndex) =>
            {
                if (self && self.inventory)
                {
                    int count = self.inventory.GetItemCountEffective(itemDef);
                    if (count > 0)
                    {
                        var buffDef = BuffCatalog.GetBuffDef(buffIndex);
                        if (buffDef == RoR2Content.Buffs.OnFire
                            || buffDef == RoR2Content.Buffs.HealingDisabled
                            || buffDef == RoR2Content.Buffs.Cripple)
                            return;
                    }
                }
                orig(self, buffIndex);
            };

            /// TODO: Fix Mitten's scaling
            //    On.RoR2.Inventory.GiveItemTemp += (orig, self, itemIndex, count) =>
            //    {
            //        AdjustMittensScaling(itemIndex, self, Vector3.one * 0.5f);

            //        orig(self, itemIndex, count);
            //    };

            //    On.RoR2.Inventory.GiveItemPermanent_ItemDef_int += (orig, self, itemDef, count) =>
            //    {
            //        AdjustMittensScaling(itemDef.itemIndex, self, Vector3.one * 0.5f);

            //        orig(self, itemDef, count);
            //    };

            //    On.RoR2.Inventory.GiveItemPermanent_ItemIndex_int += (orig, self, itemIndex, count) =>
            //    {
            //        AdjustMittensScaling(itemIndex, self, Vector3.one * 0.5f);

            //        orig(self, itemIndex, count);
            //    };

            //    On.RoR2.Inventory.RemoveItemTemp += (orig, self, itemIndex, count) =>
            //    {
            //        AdjustMittensScaling(itemIndex, self, Vector3.one);

            //        orig(self, itemIndex, count);
            //    };

            //    On.RoR2.Inventory.RemoveItemPermanent_ItemDef_int += (orig, self, itemDef, count) =>
            //    {
            //        AdjustMittensScaling(itemDef.itemIndex, self, Vector3.one);

            //        orig(self, itemDef, count);
            //    };

            //    On.RoR2.Inventory.RemoveItemPermanent_ItemIndex_int += (orig, self, itemIndex, count) =>
            //    {
            //        AdjustMittensScaling(itemIndex, self, Vector3.one);

            //        orig(self, itemIndex, count);
            //    };
            //}

            //private static void AdjustMittensScaling(ItemIndex index, Inventory self, Vector3 scaling)
            //{
            //    if (index == itemDef.itemIndex)
            //    {
            //        if (self)
            //        {
            //            var body = self.GetComponentInParent<CharacterBody>();
            //            if (body && self.GetItemCountEffective(itemDef) > 0)
            //            {
            //                body.gameObject.transform.localScale = scaling;

            //                body.mainHurtBox.transform.localScale = scaling;
            //                System.Array.ForEach(body.hurtBoxGroup.hurtBoxes, hurtBox =>
            //                {
            //                    if (hurtBox)
            //                        hurtBox.transform.localScale = scaling;
            //                });
            //            }
            //        }
            //    }
        }
    }
}
