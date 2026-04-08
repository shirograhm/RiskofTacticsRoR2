using RiskOfTactics.Managers;
using RoR2;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Networking;

namespace RiskOfTactics.Content.Equipment
{
    internal class LuckyItemChest
    {
        public static EquipmentDef equipmentDef;

        // Stunning enemies strikes them again after a short delay.
        public static ConfigurableValue<bool> isEnabled = new(
            "Equipment: Lucky Item Chest",
            "Enabled",
            true,
            "Whether or not the item is enabled.",
            ["EQUIPMENT_ROT_LUCKYITEMCHEST_DESC"]
        );
        public static ConfigurableValue<float> radiantChance = new(
            "Equipment: Lucky Item Chest",
            "Radiant Chance",
            8f,
            "Percent chance to roll a radiant on use.",
            ["EQUIPMENT_ROT_LUCKYITEMCHEST_DESC"]
        );
        public static ConfigurableValue<float> artifactChance = new(
            "Equipment: Lucky Item Chest",
            "Artifact Chance",
            8f,
            "Percent chance to roll an artifact on use.",
            ["EQUIPMENT_ROT_LUCKYITEMCHEST_DESC"]
        );
        public static ConfigurableValue<float> normalChance = new(
            "Equipment: Lucky Item Chest",
            "Normal Chance",
            84f,
            "Percent chance to roll a normal on use.",
            ["EQUIPMENT_ROT_LUCKYITEMCHEST_DESC"]
        );
        public static ConfigurableValue<int> numItems = new(
            "Equipment: Lucky Item Chest",
            "Num Items",
            3,
            "Number of items available to choose from.",
            ["EQUIPMENT_ROT_LUCKYITEMCHEST_DESC"]
        );
        public static ConfigurableValue<float> cooldown = new(
            "Equipment: Lucky Item Chest",
            "Cooldown",
            10f,
            "Equipment cooldown. This value is only used if 2 or more Lucky Item Chests are used back-to-back.",
            ["EQUIPMENT_ROT_LUCKYITEMCHEST_DESC"]
        );
        public static float percentRadiantChance = radiantChance.Value / 100f;
        public static float percentArtifactChance = artifactChance.Value / 100f;
        public static float percentNormalChance = normalChance.Value / 100f;

        internal static void Init()
        {
            equipmentDef = ItemManager.GenerateEquipment("LuckyItemChest", cooldown.Value);

            Hooks();
        }

        public static void Hooks()
        {
            On.RoR2.EquipmentSlot.PerformEquipmentAction += (orig, self, equipDef) =>
            {
                if (NetworkServer.active && equipDef == equipmentDef)
                {
                    return OnUse(self);
                }

                return orig(self, equipDef);
            };
        }

        private static bool OnUse(EquipmentSlot slot)
        {
            var pickups = new List<UniquePickup>();
            for (int i = 0; i < numItems; i++)
            {
                ItemManager.TacticTier tTier = RollTier();
                // Get a random item of the rolled tier
                ItemDef chosenDef = ItemManager.GetRandomTacticItemOfTier(tTier);
                PickupIndex rolledPickup = PickupCatalog.FindPickupIndex(chosenDef.itemIndex);
                pickups.Add(new UniquePickup { pickupIndex = rolledPickup });
            }
            var pickupInfo = new GenericPickupController.CreatePickupInfo
            {
                pickerOptions = PickupPickerController.GenerateOptionsFromList(pickups),
                prefabOverride = Addressables.LoadAssetAsync<GameObject>("RoR2/DLC1/OptionPickup/OptionPickup.prefab").WaitForCompletion(),
                position = slot.transform.position,
                rotation = Quaternion.identity,
                pickupIndex = PickupCatalog.FindPickupIndex(ItemTier.Tier1)
            };

            if (slot.characterBody && slot.characterBody.inventory)
            {
                PickupDropletController.CreatePickupDroplet(
                    pickupInfo,
                    pickupInfo.position,
                    Vector3.up * 20f + slot.transform.forward * 2f
                );

                CharacterMasterNotificationQueue.SendTransformNotification(
                    slot.characterBody.master, equipmentDef.equipmentIndex, EquipmentIndex.None, CharacterMasterNotificationQueue.TransformationType.Default
                );
                slot.characterBody.inventory.SetEquipmentIndex(EquipmentIndex.None, isRemovingEquipment: true);

                return true;
            }
            return false;
        }

        private static ItemManager.TacticTier RollTier()
        {
            var rng = new Xoroshiro128Plus(Run.instance.treasureRng.nextUlong);

            var selector = new WeightedSelection<ItemManager.TacticTier>();
            selector.AddChoice(ItemManager.TacticTier.Radiant, percentRadiantChance);
            selector.AddChoice(ItemManager.TacticTier.Artifact, percentArtifactChance);
            selector.AddChoice(ItemManager.TacticTier.Normal, percentNormalChance);
            return selector.Evaluate(rng.nextNormalizedFloat);
        }
    }
}

