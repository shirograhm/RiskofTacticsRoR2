using R2API;
using RoR2;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

namespace RiskOfTactics.Items.Artifacts
{
    class GamblersBlade
    {
        public static ItemDef itemDef;

        // Gain 0%-30% additional attack speed based on your current gold ($0-$150). On-hit, 5% chance to drop treasure.
        public static ConfigurableValue<bool> isEnabled = new(
            "Item: Gamblers Blade",
            "Enabled",
            true,
            "Whether or not the item is enabled.",
            new List<string>()
            {
                "ITEM_ROT_GAMBLERSBLADE_DESC"
            }
        );
        public static ConfigurableValue<float> attackSpeedEffectCap = new(
            "Item: Gamblers Blade",
            "Attack Speed Effect Cap",
            75f,
            "Amount of attack speed granted at money cap.",
            new List<string>()
            {
                "ITEM_ROT_GAMBLERSBLADE_DESC"
            }
        );
        public static ConfigurableValue<float> attackSpeedEffectCapExtraStacks = new(
            "Item: Gamblers Blade",
            "Attack Speed Effect Cap Extra Stacks",
            75f,
            "Amount of attack speed granted at money cap for extra stacks.",
            new List<string>()
            {
                "ITEM_ROT_GAMBLERSBLADE_DESC"
            }
        );
        public static ConfigurableValue<int> moneyEffectCap = new(
            "Item: Gamblers Blade",
            "Money Cap",
            150,
            "Amount of money required to gain the attack speed cap. Scales with difficulty.",
            new List<string>()
            {
                "ITEM_ROT_GAMBLERSBLADE_DESC"
            }
        );
        public static ConfigurableValue<float> moneyDropChance = new(
            "Item: Gamblers Blade",
            "Drop Chance",
            5f,
            "Percent chance on-hit to gain money.",
            new List<string>()
            {
                "ITEM_ROT_GAMBLERSBLADE_DESC"
            }
        );
        public static ConfigurableValue<int> moneyGainOnDrop = new(
            "Item: Gamblers Blade",
            "Money Gain",
            30,
            "Money gained on drop.",
            new List<string>()
            {
                "ITEM_ROT_GAMBLERSBLADE_DESC"
            }
        );
        public static readonly float percentMoneyDropChance = moneyDropChance.Value / 100f;

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

            itemDef.name = "ROT_GAMBLERSBLADE";
            itemDef.AutoPopulateTokens();

            Utils.SetItemTier(itemDef, ItemTier.Tier3);

            GameObject prefab = AssetHandler.bundle.LoadAsset<GameObject>("GamblersBlade.prefab");
            ModelPanelParameters modelPanelParameters = prefab.AddComponent<ModelPanelParameters>();
            modelPanelParameters.focusPointTransform = prefab.transform;
            modelPanelParameters.cameraPositionTransform = prefab.transform;
            modelPanelParameters.maxDistance = 10f;
            modelPanelParameters.minDistance = 5f;

            itemDef.pickupIconSprite = AssetHandler.bundle.LoadAsset<Sprite>("GamblersBlade.png");
            itemDef.pickupModelPrefab = prefab;
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
                    int count = sender.inventory.GetItemCountEffective(itemDef);
                    if (count > 0)
                    {
                        if (sender.master && sender.master.money > 0)
                        {
                            float moneyRequired = moneyEffectCap.Value * Utils.GetDifficultyAsMultiplier();
                            // Cap money ratio at 100%
                            float currentMoneyRatio = Mathf.Min(1f, sender.master.money / moneyRequired);
                            float attackSpeedBonus = currentMoneyRatio * Utils.GetLinearStacking(attackSpeedEffectCap.Value, attackSpeedEffectCapExtraStacks.Value, count) / 100f;

                            args.attackSpeedMultAdd += attackSpeedBonus;
                        }
                    }
                }
            };

            GenericGameEvents.OnHitEnemy += (damageInfo, attackerInfo, victimInfo) =>
            {
                CharacterBody atkBody = attackerInfo.body;
                CharacterBody vicBody = victimInfo.body;
                if (atkBody && atkBody.master && atkBody.inventory && atkBody.inventory.GetItemCountEffective(itemDef) > 0)
                {
                    if (Util.CheckRoll0To1(percentMoneyDropChance, atkBody.master.luck))
                    {
                        SpawnGoldPack(atkBody, vicBody);
                    }
                }
            };
        }

        private static void SpawnGoldPack(CharacterBody attacker, CharacterBody victim)
        {
            GameObject goldPackObject = Object.Instantiate(LegacyResourcesAPI.Load<GameObject>("Prefabs/NetworkedObjects/BonusMoneyPack"), victim.transform.position, Random.rotation);
            if (goldPackObject)
            {
                Collider component = goldPackObject.GetComponent<Collider>();
                if (component)
                {
                    TeamFilter teamComponent = goldPackObject.GetComponent<TeamFilter>();
                    if (teamComponent && attacker.teamComponent)
                    {
                        teamComponent.teamIndex = attacker.teamComponent.teamIndex;
                    }
                    MoneyPickup componentInChildren = goldPackObject.GetComponentInChildren<MoneyPickup>();
                    if ((bool)componentInChildren)
                    {
                        componentInChildren.baseGoldReward = moneyGainOnDrop;
                        Physics.IgnoreCollision(component, componentInChildren.GetComponent<Collider>());
                    }
                    GravitatePickup componentInChildren2 = goldPackObject.GetComponentInChildren<GravitatePickup>();
                    if ((bool)componentInChildren2)
                    {
                        Physics.IgnoreCollision(component, componentInChildren2.GetComponent<Collider>());
                    }
                    goldPackObject.transform.localScale = new Vector3(1f, 1.5f, 0.85f);

                    NetworkServer.Spawn(goldPackObject);
                }
            }
        }

    }
}
