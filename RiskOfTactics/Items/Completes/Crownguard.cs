using R2API;
using RoR2;
using System.Collections.Generic;
using UnityEngine;

namespace RiskOfTactics.Items.Completes
{
    class Crownguard
    {
        public static ItemDef itemDef;
        public static BuffDef guardedBuff;
        public static BuffDef crownedBuff;

        // Upon activation of the teleporter, gain a temporary shield. When the shield expires, gain permanent BASE damage.
        public static ConfigurableValue<bool> isEnabled = new(
            "Item: Crownguard",
            "Enabled",
            true,
            "Whether or not the item is enabled.",
            new List<string>()
            {
                "ITEM_ROT_CROWNGUARD_DESC"
            }
        );
        public static ConfigurableValue<float> effectShield = new(
            "Item: Crownguard",
            "Effect Shield",
            25f,
            "Percent max HP shield bonus when teleporter is activated.",
            new List<string>()
            {
                "ITEM_ROT_CROWNGUARD_DESC"
            }
        );
        public static ConfigurableValue<float> effectShieldExtraStacks = new(
            "Item: Crownguard",
            "Effect Shield Extra Stacks",
            25f,
            "Percent max HP shield bonus with extra stacks when teleporter is activated.",
            new List<string>()
            {
                "ITEM_ROT_CROWNGUARD_DESC"
            }
        );
        public static ConfigurableValue<float> effectDuration = new(
            "Item: Crownguard",
            "Effect Duration",
            30f,
            "How long the shield effect lasts when teleporter is activated.",
            new List<string>()
            {
                "ITEM_ROT_CROWNGUARD_DESC"
            }
        );
        public static ConfigurableValue<float> effectDamage = new(
            "Item: Crownguard",
            "Effect Damage",
            4f,
            "Damage bonus given after the shield effect expires.",
            new List<string>()
            {
                "ITEM_ROT_CROWNGUARD_DESC"
            }
        );
        public static ConfigurableValue<float> effectDamageExtraStacks = new(
            "Item: Crownguard",
            "Effect Damage Extra Stacks",
            1.5f,
            "Damage bonus given after the shield effect expires.",
            new List<string>()
            {
                "ITEM_ROT_CROWNGUARD_DESC"
            }
        );
        private static readonly float percentEffectShield = effectShield.Value / 100f;
        private static readonly float percentEffectShieldExtraStacks = effectShieldExtraStacks.Value / 100f;

        internal static void Init()
        {
            GenerateItem();

            ItemDisplayRuleDict displayRules = new ItemDisplayRuleDict(null);
            ItemAPI.Add(new CustomItem(itemDef, displayRules));

            guardedBuff = Utils.GenerateBuffDef("Guarded", AssetHandler.bundle.LoadAsset<Sprite>("Guarded.png"), false, false, false, true);
            ContentAddition.AddBuffDef(guardedBuff);
            crownedBuff = Utils.GenerateBuffDef("Crowned", AssetHandler.bundle.LoadAsset<Sprite>("Crowned.png"), false, false, false, false);
            ContentAddition.AddBuffDef(crownedBuff);

            Hooks();
        }

        private static void GenerateItem()
        {
            itemDef = ScriptableObject.CreateInstance<ItemDef>();

            itemDef.name = "CROWNGUARD";
            itemDef.AutoPopulateTokens();

            Utils.SetItemTier(itemDef, ItemTier.Tier2);

            GameObject prefab = AssetHandler.bundle.LoadAsset<GameObject>("Crownguard.prefab");
            ModelPanelParameters modelPanelParameters = prefab.AddComponent<ModelPanelParameters>();
            modelPanelParameters.focusPointTransform = prefab.transform;
            modelPanelParameters.cameraPositionTransform = prefab.transform;
            modelPanelParameters.maxDistance = 10f;
            modelPanelParameters.minDistance = 5f;

            itemDef.pickupIconSprite = AssetHandler.bundle.LoadAsset<Sprite>("Crownguard.png");
            itemDef.pickupModelPrefab = prefab;
            itemDef.canRemove = true;
            itemDef.hidden = false;

            itemDef.tags = new ItemTag[]
            {
                ItemTag.Healing,
                ItemTag.Utility
            };
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
                        if (sender.GetBuffCount(guardedBuff) > 0)
                            args.baseShieldAdd += sender.healthComponent.fullHealth * Utils.GetLinearStacking(percentEffectShield, percentEffectShieldExtraStacks, count);

                        if (sender.GetBuffCount(crownedBuff) > 0)
                            args.baseDamageAdd += Utils.GetLinearStacking(effectDamage.Value, effectDamageExtraStacks.Value, count);
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
                        if (body && body.inventory && body.inventory.GetItemCountEffective(itemDef) > 0)
                        {
                            body.AddTimedBuff(guardedBuff, effectDuration.Value);
                        }
                    }
                }
            };
        }
    }
}
