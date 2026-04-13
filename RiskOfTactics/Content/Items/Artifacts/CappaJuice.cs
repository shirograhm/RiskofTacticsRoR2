using R2API;
using RiskOfTactics.Managers;
using RoR2;

namespace RiskOfTactics.Content.Items.Artifacts
{
    internal class CappaJuice
    {
        public static ItemDef itemDef;

        // Gain 1 (+1 per stack) base damage. Killing boss enemies grants an additional stack of this item. 
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
            1f,
            "Damage bonus for each stack of this item.",
            ["ITEM_ROT_CAPPAJUICE_DESC"]
        );

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
                        args.baseDamageAdd += Utilities.GetLinearStacking(damageBonus.Value, damageBonus.Value, count);
                    }
                }
            };

            GlobalEventManager.onCharacterDeathGlobal += (damageReport) =>
            {
                CharacterBody vicBody = damageReport.victimBody;
                CharacterBody atkBody = damageReport.attackerBody;

                if (vicBody && atkBody && atkBody.inventory)
                {
                    int count = atkBody.inventory.GetItemCountEffective(itemDef);
                    if (count > 0 && vicBody.isBoss)
                    {
                        atkBody.inventory.GiveItemPermanent(itemDef);
                    }
                }
            };
        }
    }
}
