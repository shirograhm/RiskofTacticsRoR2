using R2API;
using RoR2;
using System.Collections.Generic;
using UnityEngine;

namespace RiskOfTactics.Items.Completes
{
    class SpearOfShojin
    {
        public static ItemDef itemDef;

        // Refund a percentage of your active cooldowns on-hit.
        public static ConfigurableValue<bool> isEnabled = new(
            "Item: Spear Of Shojin",
            "Enabled",
            true,
            "Whether or not the item is enabled.",
            new List<string>()
            {
                "ITEM_ROT_SPEAROFSHOJIN_DESC"
            }
        );
        public static ConfigurableValue<float> cooldownOnHit = new(
            "Item: Spear Of Shojin",
            "On-Hit Cooldown",
            5f,
            "Percentage of remaining cooldown refunded on-hit.",
            new List<string>()
            {
                "ITEM_ROT_SPEAROFSHOJIN_DESC"
            }
        );
        public static ConfigurableValue<float> cooldownOnHitExtraStacks = new(
            "Item: Spear Of Shojin",
            "On-Hit Cooldown Extra Stacks",
            5f,
            "Percentage of remaining cooldown refunded on-hit.",
            new List<string>()
            {
                "ITEM_ROT_SPEAROFSHOJIN_DESC"
            }
        );
        private static readonly float percentCooldownOnHit = cooldownOnHit.Value / 100f;
        private static readonly float percentCooldownOnHitExtraStacks = cooldownOnHitExtraStacks.Value / 100f;

        internal static void Init()
        {
            GenerateItem();

            ItemDisplayRuleDict displayRules = new ItemDisplayRuleDict(null);
            ItemAPI.Add(new CustomItem(itemDef, displayRules));

            Hooks();
        }

        private static void GenerateItem()
        {
            itemDef = ScriptableObject.CreateInstance<ItemDef>();

            itemDef.name = "ROT_SPEAROFSHOJIN";
            itemDef.AutoPopulateTokens();

            Utils.SetItemTier(itemDef, ItemTier.Tier2);

            GameObject prefab = AssetHandler.bundle.LoadAsset<GameObject>("SpearOfShojin.prefab");
            ModelPanelParameters modelPanelParameters = prefab.AddComponent<ModelPanelParameters>();
            modelPanelParameters.focusPointTransform = prefab.transform;
            modelPanelParameters.cameraPositionTransform = prefab.transform;
            modelPanelParameters.maxDistance = 10f;
            modelPanelParameters.minDistance = 5f;

            itemDef.pickupIconSprite = AssetHandler.bundle.LoadAsset<Sprite>("SpearOfShojin.png");
            itemDef.pickupModelPrefab = prefab;
            itemDef.canRemove = true;
            itemDef.hidden = false;

            itemDef.tags = new ItemTag[]
            {
                ItemTag.Damage,
                ItemTag.Utility,

                ItemTag.CanBeTemporary
            };
        }

        public static void Hooks()
        {
            GenericGameEvents.OnHitEnemy += (damageInfo, attackerInfo, victimInfo) =>
            {
                CharacterBody vicBody = victimInfo.body;
                CharacterBody atkBody = attackerInfo.body;

                if (atkBody && atkBody.inventory && atkBody.skillLocator)
                {
                    int count = atkBody.inventory.GetItemCountEffective(itemDef);
                    if (count > 0)
                    {
                        foreach (GenericSkill skill in atkBody.skillLocator.allSkills)
                        {
                            float cooldownLeft = skill.finalRechargeInterval - skill.rechargeStopwatch;
                            skill.rechargeStopwatch += cooldownLeft * Utils.GetHyperbolicStacking(percentCooldownOnHit, percentCooldownOnHitExtraStacks, count);
                        }
                    }
                }
            };
        }
    }
}
