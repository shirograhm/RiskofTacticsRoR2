using R2API;
using RiskOfTactics.Content.Equipment;
using RiskOfTactics.Managers;
using RoR2;
using UnityEngine;

namespace RiskOfTactics.Content.Artifacts
{
    internal class ArtifactOfTheGoldenSpat
    {
        public static ArtifactDef artifactDef;

        public static GameObject PickupModelPrefab { get; } = null;

        // Start with a random normal-tier TFT item. Every time you level up, replace your current equipment with a Lucky Item Chest.
        public static ConfigurableValue<bool> isEnabled = new(
            "Artifact of the Golden Spatula",
            "Enabled",
            true,
            "Whether or not the artifact is enabled.",
            ["ARTIFACT_ROT_GOLDEN_SPATULA_MODIFIER_DESC"]
        );

        internal static void Init()
        {
            artifactDef = ScriptableObject.CreateInstance<ArtifactDef>();
            artifactDef.cachedName = "ROT_GOLDEN_SPATULA_MODIFIER";
            artifactDef.nameToken = "ROT_GOLDEN_SPATULA_MODIFIER_NAME";
            artifactDef.descriptionToken = "ROT_GOLDEN_SPATULA_MODIFIER_DESC";
            artifactDef.smallIconSelectedSprite = AssetManager.bundle.LoadAsset<Sprite>("GoldenSpatModifierEnabled.png");
            artifactDef.smallIconDeselectedSprite = AssetManager.bundle.LoadAsset<Sprite>("GoldenSpatModifierDisabled.png");
            artifactDef.pickupModelPrefab = AssetManager.bundle.LoadAsset<GameObject>("GoldenSpatModifierPickup.prefab");

            ContentAddition.AddArtifactDef(artifactDef);

            Hooks();
        }

        public static void Hooks()
        {
            On.RoR2.Run.BeginStage += (orig, self) =>
            {
                if (RunArtifactManager.instance.IsArtifactEnabled(artifactDef) && Run.instance.stageClearCount == 0)
                {
                    foreach (var player in PlayerCharacterMasterController.instances)
                    {
                        if (player && player.master && player.master.inventory)
                        {
                            var inventory = player.master.inventory;
                            inventory.GiveItemPermanent(ItemManager.GetRandomTacticItemOfTier(ItemManager.TacticTier.Normal));
                        }
                    }
                }
                orig(self);
            };

            On.RoR2.CharacterBody.OnLevelUp += (orig, self) =>
            {
                if (RunArtifactManager.instance.IsArtifactEnabled(artifactDef))
                {
                    if (self && self.inventory)
                    {
                        self.inventory.SetEquipmentIndexForSlot(LuckyItemChest.equipmentDef.equipmentIndex, 0, 0);
                    }
                }

                orig(self);
            };
        }
    }
}
