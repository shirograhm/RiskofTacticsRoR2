using R2API;
using RiskOfTactics.Managers;
using RoR2;
using RoR2.Items;
using UnityEngine;

namespace RiskOfTactics.Content.Items.Artifacts
{
    public class MittensItemBehavior : BaseItemBodyBehavior
    {
        [ItemDefAssociation(useOnServer = true, useOnClient = false)]
        public static ItemDef GetItemDef()
        {
            return Mittens.itemDef;
        }

        public void FixedUpdate()
        {
            if (body && body.gameObject)
            {
                if (stack > 0)
                {
                    //body.gameObject.transform.localScale = (Vector3.one * 0.5f);
                    //body.mainHurtBox.gameObject.transform.localScale = (Vector3.one * 0.5f);
                    body.masterObject.transform.localScale = (Vector3.one * 0.5f);
                }
                else
                {
                    //body.gameObject.transform.localScale = Vector3.one;
                    //body.mainHurtBox.gameObject.transform.localScale = Vector3.one;
                    body.masterObject.transform.localScale = Vector3.one;
                }
            }
        }
    }

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
            ["ITEM_ROT_MITTENS_DESC"],
            true
        );
        public static ConfigurableValue<float> movementSpeedExtraStacks = new(
            "Item: Mittens",
            "Movement Speed Extra Stacks",
            30f,
            "Percent movement speed gained when holding extra stacks of this item.",
            ["ITEM_ROT_MITTENS_DESC"],
            true
        );
        public static ConfigurableValue<float> attackSpeed = new(
            "Item: Mittens",
            "Attack Speed",
            20f,
            "Percent attack speed gained when holding this item.",
            ["ITEM_ROT_MITTENS_DESC"],
            true
        );
        public static ConfigurableValue<float> attackSpeedExtraStacks = new(
            "Item: Mittens",
            "Attack Speed Extra Stacks",
            30f,
            "Percent attack speed gained when holding extra stacks of this item.",
            ["ITEM_ROT_MITTENS_DESC"],
            true
        );
        public static readonly float percentMovementSpeed = movementSpeed.Value / 100f;
        public static readonly float percentMovementSpeedExtraStacks = movementSpeedExtraStacks.Value / 100f;
        public static readonly float percentAttackSpeed = attackSpeed.Value / 100f;
        public static readonly float percentAttackSpeedExtraStacks = attackSpeedExtraStacks.Value / 100f;

        internal static void Init()
        {
            itemDef = ItemManager.GenerateItem("Mittens", [ItemTag.Damage, ItemTag.Utility, ItemTag.CanBeTemporary], ItemManager.TacticTier.Artifact);

            Hooks(itemDef);
        }

        public static void Hooks(ItemDef def)
        {
            RecalculateStatsAPI.GetStatCoefficients += (sender, args) =>
            {
                if (sender && sender.inventory)
                {
                    int count = sender.inventory.GetItemCountEffective(def);
                    if (count > 0)
                    {
                        args.moveSpeedMultAdd += Utilities.GetLinearStacking(percentMovementSpeed, percentMovementSpeedExtraStacks, count);
                        args.attackSpeedMultAdd += Utilities.GetLinearStacking(percentAttackSpeed, percentAttackSpeedExtraStacks, count);
                    }
                }
            };

            On.RoR2.CharacterBody.AddBuff_BuffDef += (orig, self, buffDef) =>
            {
                if (buffDef == RoR2Content.Buffs.OnFire
                    || buffDef == RoR2Content.Buffs.HealingDisabled
                    || buffDef == RoR2Content.Buffs.Cripple)
                    return;

                orig(self, buffDef);
            };
        }
    }
}
