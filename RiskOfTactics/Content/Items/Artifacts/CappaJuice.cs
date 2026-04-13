using R2API;
using RiskOfTactics.Managers;
using RoR2;

namespace RiskOfTactics.Content.Items.Artifacts
{
    internal class CappaJuice
    {
        public static ItemDef itemDef;

        // Gain stacking base damage. Killing enemies during the teleporter event grants an additional stack of this item. 
        public static ConfigurableValue<bool> isEnabled = new(
            "Item: Cappa Juice",
            "Enabled",
            true,
            "Whether or not the item is enabled.",
            ["ITEM_ROT_CAPPAJUICE_DESC"]
        );
        public static ConfigurableValue<float> damageBonus = new(
            "Item: Cappa Juice",
            "Damage Bonus",
            0.5f,
            "Percent damage bonus for each stack of this item.",
            ["ITEM_ROT_CAPPAJUICE_DESC"]
        );
        public static float percentDamageBonus = damageBonus.Value / 100f;

        internal static void Init()
        {
            itemDef = ItemManager.GenerateItem("CappaJuice", [ItemTag.Damage, ItemTag.CanBeTemporary], ItemManager.TacticTier.Artifact);

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
                        args.damageTotalMult *= 1 + Utilities.GetLinearStacking(percentDamageBonus, percentDamageBonus, count);
                    }
                }
            };

            GlobalEventManager.onCharacterDeathGlobal += (damageReport) =>
            {
                CharacterBody vicBody = damageReport.victimBody;
                CharacterBody atkBody = damageReport.attackerBody;

                foreach (HoldoutZoneController hzc in InstanceTracker.GetInstancesList<HoldoutZoneController>())
                {
                    if (vicBody && atkBody && atkBody.inventory)
                    {
                        int count = atkBody.inventory.GetItemCountEffective(itemDef);

                        if (count > 0 && hzc.isActiveAndEnabled)
                        {
                            atkBody.inventory.GiveItemPermanent(itemDef);
                        }
                    }
                }
            };
        }
    }
}
