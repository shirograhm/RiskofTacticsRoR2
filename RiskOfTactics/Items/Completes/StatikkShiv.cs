using R2API;
using RoR2;
using System.Collections.Generic;
using UnityEngine;

namespace RiskOfTactics
{
    class StatikkShiv
    {
        public static ItemDef itemDef;
        public static BuffDef shockBuff;
        public static BuffDef shockCooldown;
        public static BuffDef shredDebuff;

        // Gain attack speed, flat damage, and cooldown reduction. Every 10 seconds, your next attack deals an additional 18 damage and reduces armor by 10.
        public static ConfigurableValue<bool> isEnabled = new(
            "Item: Statikk Shiv",
            "Enabled",
            true,
            "Whether or not the item is enabled.",
            new List<string>()
            {
                "ITEM_STATIKKSHIV_DESC"
            }
        );
        public static ConfigurableValue<float> attackSpeedBonus = new(
            "Item: Statikk Shiv",
            "Attack Speed",
            20f,
            "Percent attack speed gained when holding this item.",
            new List<string>()
            {
                "ITEM_STATIKKSHIV_DESC"
            }
        );
        public static ConfigurableValue<float> damageBonus = new(
            "Item: Statikk Shiv",
            "Flat Damage",
            4f,
            "Flat damage gained when holding this item.",
            new List<string>()
            {
                "ITEM_STATIKKSHIV_DESC"
            }
        );
        public static ConfigurableValue<float> cooldownReductionBonus = new(
            "Item: Statikk Shiv",
            "Cooldown Reduction",
            8f,
            "Percent cooldown reduction gained when holding this item.",
            new List<string>()
            {
                "ITEM_STATIKKSHIV_DESC"
            }
        );
        public static ConfigurableValue<float> effectCooldown = new(
            "Item: Statikk Shiv",
            "Effect Cooldown",
            20f,
            "Cooldown of this item's effect.",
            new List<string>()
            {
                "ITEM_STATIKKSHIV_DESC"
            }
        );
        public static ConfigurableValue<float> effectOnHitDamage = new(
            "Item: Statikk Shiv",
            "Bonus On-Hit",
            20f,
            "Bonus on-hit damage dealt by this item's effect.",
            new List<string>()
            {
                "ITEM_STATIKKSHIV_DESC"
            }
        );
        private static readonly float percentAttackSpeedBonus = attackSpeedBonus.Value / 100f;
        private static readonly float percentCooldownReductionBonus = cooldownReductionBonus.Value / 100f;

        internal static void Init()
        {
            GenerateItem();

            ItemDisplayRuleDict displayRules = new ItemDisplayRuleDict(null);
            ItemAPI.Add(new CustomItem(itemDef, displayRules));

            shockBuff = Utils.GenerateBuffDef("Shock", AssetHandler.bundle.LoadAsset<Sprite>("Shock.png"), false, false, false, false);
            ContentAddition.AddBuffDef(shockBuff);
            shockCooldown = Utils.GenerateBuffDef("Shock Cooldown", AssetHandler.bundle.LoadAsset<Sprite>("ShockCD.png"), false, false, false, true);
            ContentAddition.AddBuffDef(shockCooldown);

            Hooks();
        }

        private static void GenerateItem()
        {
            itemDef = ScriptableObject.CreateInstance<ItemDef>();

            itemDef.name = "STATIKKSHIV";
            itemDef.AutoPopulateTokens();

            Utils.SetItemTier(itemDef, ItemTier.Tier3);

            itemDef.pickupIconSprite = AssetHandler.bundle.LoadAsset<Sprite>("StatikkShiv.png");
            itemDef.pickupModelPrefab = AssetHandler.bundle.LoadAsset<GameObject>("StatikkShiv.prefab");
            itemDef.canRemove = true;
            itemDef.hidden = false;

            itemDef.tags = new ItemTag[]
            {
                ItemTag.Damage,
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
                        args.attackSpeedMultAdd += percentAttackSpeedBonus;
                        args.baseDamageAdd += damageBonus.Value;
                        args.cooldownMultAdd -= percentCooldownReductionBonus;
                    }
                }
            };

            Stage.onStageStartGlobal += (stage) =>
            {
                foreach (NetworkUser user in NetworkUser.readOnlyInstancesList)
                {
                    CharacterMaster master = user.masterController.master ?? user.master;
                    if (master && master.GetBody() && master.GetBody().inventory && master.GetBody().inventory.GetItemCount(itemDef) > 0)
                    {
                        master.GetBody().AddBuff(shockBuff);
                    }
                }
            };

            On.RoR2.Inventory.GiveItem_ItemIndex_int += (orig, self, index, count) =>
            {
                orig(self, index, count);

                if (index == itemDef.itemIndex)
                {
                    CharacterMaster master = self.GetComponent<CharacterMaster>();
                    if (master && master.GetBody() && self.GetItemCount(itemDef) > 0)
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
                    if (self && self.inventory && self.inventory.GetItemCount(itemDef) > 0 && self.GetBuffCount(shockBuff) == 0)
                    {
                        self.AddBuff(shockBuff);
                    }
                }
            };

            GenericGameEvents.OnTakeDamage += (damageReport) =>
            {
                CharacterBody vicBody = damageReport.victimBody;
                CharacterBody atkBody = damageReport.attackerBody;

                if (vicBody && atkBody && atkBody.inventory)
                {
                    bool hasShockBuff = atkBody.GetBuffCount(shockBuff) > 0;
                    if (hasShockBuff && vicBody.teamComponent.teamIndex != atkBody.teamComponent.teamIndex)
                    {
                        vicBody.AddBuff(Sunder.buffDef);

                        DamageInfo shockProc = new DamageInfo
                        {
                            damage = effectOnHitDamage.Value,
                            damageColorIndex = DamageColorIndex.WeakPoint,
                            damageType = DamageType.Generic,
                            attacker = atkBody.gameObject,
                            inflictor = atkBody.gameObject,
                            crit = atkBody.RollCrit(),
                            procCoefficient = 1.0f,
                            procChainMask = new ProcChainMask(),
                            position = vicBody.corePosition
                        };
                        vicBody.healthComponent.TakeDamage(shockProc);

                        // Remove the shock buff and add cooldown buff
                        atkBody.RemoveBuff(shockBuff);
                        atkBody.AddTimedBuff(shockCooldown, effectCooldown);
                    }
                }
            };
        }
    }
}
