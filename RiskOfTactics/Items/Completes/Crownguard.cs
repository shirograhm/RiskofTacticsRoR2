using R2API;
using R2API.Networking;
using R2API.Networking.Interfaces;
using RoR2;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

namespace RiskOfTactics
{
    class Crownguard

    {
        public static ItemDef itemDef;
        public static BuffDef guardedBuff;
        public static BuffDef crownedBuff;

        public static ConfigurableValue<bool> isEnabled = new(
            "Item: Crownguard",
            "Enabled",
            true,
            "Whether or not the item is enabled.",
            [
                "ITEM_CROWNGUARD_DESC"
            ]
        );
        public static ConfigurableValue<float> flatDamageBonus = new(
            "Item: Crownguard",
            "Flat Damage",
            8f,
            "Flat damage bonus when holding this item.",
            [
                "ITEM_CROWNGUARD_DESC"
            ]
        );
        public static ConfigurableValue<float> armorBonus = new(
            "Item: Crownguard",
            "Armor",
            15f,
            "Armor bonus when holding this item.",
            [
                "ITEM_CROWNGUARD_DESC"
            ]
        );
        public static ConfigurableValue<float> flatHealthBonus = new(
            "Item: Crownguard",
            "Flat Health",
            100f,
            "Flat health bonus when holding this item.",
            [
                "ITEM_CROWNGUARD_DESC"
            ]
        );
        public static ConfigurableValue<float> effectShield = new(
            "Item: Crownguard",
            "Effect Shield",
            50f,
            "Percent max HP shield bonus when teleporter is activated.",
            [
                "ITEM_CROWNGUARD_DESC"
            ]
        );
        public static ConfigurableValue<int> effectDuration = new(
            "Item: Crownguard",
            "Effect Duration",
            10,
            "How long the shield effect lasts when teleporter is activated.",
            [
                "ITEM_CROWNGUARD_DESC"
            ]
        );
        public static ConfigurableValue<float> effectDamage = new(
            "Item: Crownguard",
            "Effect Damage",
            8f,
            "Damage bonus given after the shield effect expires.",
            [
                "ITEM_CROWNGUARD_DESC"
            ]
        );
        private static readonly float percentEffectShield = effectShield.Value / 100f;

        internal static void Init()
        {
            GenerateItem();
            guardedBuff = Utils.GenerateBuffDef("Guarded", AssetHandler.bundle.LoadAsset<Sprite>("Guarded.png"), false, false, false, true);
            crownedBuff = Utils.GenerateBuffDef("Crowned", AssetHandler.bundle.LoadAsset<Sprite>("Crowned.png"), false, false, false, false);

            ItemDisplayRuleDict displayRules = new ItemDisplayRuleDict(null);
            ItemAPI.Add(new CustomItem(itemDef, displayRules));

            ContentAddition.AddBuffDef(guardedBuff);
            ContentAddition.AddBuffDef(crownedBuff);

            Hooks();
        }

        private static void GenerateItem()
        {
            itemDef = ScriptableObject.CreateInstance<ItemDef>();

            itemDef.name = "CROWNGUARD";
            itemDef.AutoPopulateTokens();

            Utils.SetItemTier(itemDef, ItemTier.Tier3);

            itemDef.pickupIconSprite = AssetHandler.bundle.LoadAsset<Sprite>("Crownguard.png");
            itemDef.pickupModelPrefab = AssetHandler.bundle.LoadAsset<GameObject>("Crownguard.prefab");
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
                    int count = sender.inventory.GetItemCount(itemDef);
                    if (count > 0)
                    {
                        args.baseDamageAdd += flatDamageBonus.Value;
                        args.armorAdd += armorBonus.Value;
                        args.baseHealthAdd += flatHealthBonus.Value;
                    }

                    if (sender.GetBuffCount(guardedBuff) > 0)
                        args.baseShieldAdd += sender.healthComponent.fullHealth * percentEffectShield;

                    if (sender.GetBuffCount(crownedBuff) > 0)
                        args.baseDamageAdd += effectDamage.Value;
                }
            };

            On.RoR2.CharacterBody.OnBuffFinalStackLost += (orig, self, buffDef) =>
            {
                orig(self, buffDef);

                if (buffDef == guardedBuff)
                {

                }
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
                        if (body && body.inventory && body.inventory.GetItemCount(itemDef) > 0)
                        {
                            body.AddTimedBuff(guardedBuff, effectDuration.Value);
                        }
                    }
                }
            };
        }
    }
}
