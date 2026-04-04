//using R2API;
//using RiskOfTactics.Managers;
//using RoR2;
//using UnityEngine;

//namespace RiskOfTactics.Content.Items.Uniques
//{
//    class CrownOfDemacia
//    {
//        public static ItemDef itemDef;
//        public static BuffDef crownedBuff;
//        public static BuffDef crownedTickerCooldown;

//        // During the teleporter event, don the Demacian crown, granting max health, attack speed, and base damage. If you die while wearing the crown, end the run.
//        public static ConfigurableValue<bool> isEnabled = new(
//            "Item: Crown of Demacia",
//            "Enabled",
//            true,
//            "Whether or not the item is enabled.",
//            ["ITEM_ROT_CROWNOFDEMACIA_DESC"]
//        );
//        public static ConfigurableValue<float> attackSpeed = new(
//            "Item: Crown of Demacia",
//            "Attack Speed",
//            30f,
//            "Percent attack speed gained while holding this item. DOES NOT STACK.",
//            ["ITEM_ROT_CROWNOFDEMACIA_DESC"],
//            true
//        );
//        public static ConfigurableValue<float> maxHealth = new(
//            "Item: Crown of Demacia",
//            "Max Health",
//            300f,
//            "Max health gained while holding this item. DOES NOT STACK.",
//            ["ITEM_ROT_CROWNOFDEMACIA_DESC"],
//            true
//        );
//        public static ConfigurableValue<float> interval = new(
//            "Item: Crown of Demacia",
//            "Interval",
//            1f,
//            "Interval in seconds for base damage gain during the teleporter event. DOES NOT STACK.",
//            ["ITEM_ROT_CROWNOFDEMACIA_DESC"],
//            true
//        );
//        public static ConfigurableValue<float> baseDamage = new(
//            "Item: Crown of Demacia",
//            "Base Damage",
//            1f,
//            "Percent base damage gained per interval during the teleporter event. DOES NOT STACK.",
//            ["ITEM_ROT_CROWNOFDEMACIA_DESC"],
//            true
//        );
//        public static ConfigurableValue<float> baseDamageStageBonus = new(
//            "Item: Crown of Demacia",
//            "Base Damage Stage Bonus",
//            1f,
//            "Percent base damage gained per interval during the teleporter event for each stage completed. DOES NOT STACK.",
//            ["ITEM_ROT_CROWNOFDEMACIA_DESC"],
//            true
//        );
//        public static float percentAttackSpeed = attackSpeed.Value / 100f;
//        public static float percentBaseDamage = baseDamage.Value / 100f;
//        public static float percentBaseDamageStageBonus = baseDamageStageBonus.Value / 100f;

//        internal static void Init()
//        {
//            itemDef = ItemManager.GenerateItem("CrownOfDemacia", [ItemTag.Damage, ItemTag.Utility, ItemTag.Healing, ItemTag.CanBeTemporary], ItemManager.TacticTier.Unique);

//            crownedBuff = Utilities.GenerateBuffDef("CrownedBuff", AssetManager.bundle.LoadAsset<Sprite>("CrownOfDemaciaBuff"), false, false, false, false);
//            ContentAddition.AddBuffDef(crownedBuff);

//            //crownedTickerCooldown = Utilities.GenerateBuffDef("CrownedTickerCooldown", AssetManager.bundle.LoadAsset<Sprite>("CrownOfDemaciaCooldown"), false, true, false, true);
//            //ContentAddition.AddBuffDef(crownedTickerCooldown);

//            Hooks(itemDef);
//        }

//        public static void Hooks(ItemDef def)
//        {
//            RecalculateStatsAPI.GetStatCoefficients += (sender, args) =>
//            {
//                if (sender && sender.inventory)
//                {
//                    int count = sender.inventory.GetItemCountEffective(def);
//                    if (count > 0)
//                    {
//                        args.baseHealthAdd += maxHealth.Value;
//                        args.attackSpeedMultAdd += percentAttackSpeed;
//                        args.damageMultAdd += percentBaseDamage;
//                    }
//                }
//            };

//            GlobalEventManager.onCharacterDeathGlobal += (damageReport) =>
//            {
//                if (damageReport.victimBody && damageReport.victimBody.inventory)
//                {
//                    int count = damageReport.victimBody.inventory.GetItemCountEffective(def);
//                    if (count > 0 && damageReport.victimBody.HasBuff()
//                    {
//                        Run.instance.BeginGameOver(RoR2Content.GameEndings.ObliterationEnding);
//                    }
//                }
//            };
//        }
//    }
//}
