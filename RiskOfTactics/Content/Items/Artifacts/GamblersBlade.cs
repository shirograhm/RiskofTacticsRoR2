using R2API;
using RiskOfTactics.Managers;
using RoR2;
using UnityEngine;
using UnityEngine.Networking;

namespace RiskOfTactics.Content.Items.Artifacts
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
            ["ITEM_ROT_GAMBLERSBLADE_DESC"]
        );
        public static ConfigurableValue<float> attackSpeedEffectCap = new(
            "Item: Gamblers Blade",
            "Attack Speed Effect Cap",
            75f,
            "Amount of attack speed granted at money cap.",
            ["ITEM_ROT_GAMBLERSBLADE_DESC"]
        );
        public static ConfigurableValue<float> attackSpeedEffectCapExtraStacks = new(
            "Item: Gamblers Blade",
            "Attack Speed Effect Cap Extra Stacks",
            75f,
            "Amount of attack speed granted at money cap for extra stacks.",
            ["ITEM_ROT_GAMBLERSBLADE_DESC"]
        );
        public static ConfigurableValue<int> moneyEffectCap = new(
            "Item: Gamblers Blade",
            "Money Cap",
            150,
            "Amount of money required to gain the attack speed cap. Scales with difficulty.",
            ["ITEM_ROT_GAMBLERSBLADE_DESC"]
        );
        public static ConfigurableValue<float> moneyDropChance = new(
            "Item: Gamblers Blade",
            "Drop Chance",
            5f,
            "Percent chance on-hit to gain money.",
            ["ITEM_ROT_GAMBLERSBLADE_DESC"]
        );
        public static ConfigurableValue<int> moneyGainOnDrop = new(
            "Item: Gamblers Blade",
            "Money Gain",
            30,
            "Money gained on drop.",
            ["ITEM_ROT_GAMBLERSBLADE_DESC"]
        );
        public static readonly float percentMoneyDropChance = moneyDropChance.Value / 100f;

        internal static void Init()
        {
            itemDef = ItemManager.GenerateItem("GamblersBlade", [ItemTag.Damage, ItemTag.Utility, ItemTag.CanBeTemporary], ItemManager.TacticTier.Artifact);

            ItemDisplayRuleDict displayRules = new ItemDisplayRuleDict(null);
            ItemAPI.Add(new CustomItem(itemDef, displayRules));

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
                        if (sender.master && sender.master.money > 0)
                        {
                            float moneyRequired = moneyEffectCap.Value * Utilities.GetDifficultyAsMultiplier();
                            // Cap money ratio at 100%
                            float currentMoneyRatio = Mathf.Min(1f, sender.master.money / moneyRequired);
                            float attackSpeedBonus = currentMoneyRatio * Utilities.GetLinearStacking(attackSpeedEffectCap.Value, attackSpeedEffectCapExtraStacks.Value, count) / 100f;

                            args.attackSpeedMultAdd += attackSpeedBonus;
                        }
                    }
                }
            };

            GameEventManager.OnHitEnemy += (damageInfo, attackerInfo, victimInfo) =>
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
                        componentInChildren.baseGoldReward = Mathf.RoundToInt(moneyGainOnDrop.Value * Utilities.GetDifficultyAsMultiplier());
                        Physics.IgnoreCollision(component, componentInChildren.GetComponent<Collider>());
                    }
                    GravitatePickup componentInChildren2 = goldPackObject.GetComponentInChildren<GravitatePickup>();
                    if ((bool)componentInChildren2)
                    {
                        Physics.IgnoreCollision(component, componentInChildren2.GetComponent<Collider>());
                    }
                    goldPackObject.transform.localScale = new Vector3(0.65f, 4.5f, 0.25f);

                    NetworkServer.Spawn(goldPackObject);
                }
            }
        }

    }
}
