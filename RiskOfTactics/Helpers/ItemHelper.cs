using R2API;
using RoR2;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace RiskOfTactics.Helpers
{
    public static class ItemHelper
    {
        public static List<ItemDef> normalList = [];
        public static List<ItemDef> radiantList = [];
        public static List<ItemDef> artifactList = [];

        public enum TacticTier
        {
            Normal, Radiant, Artifact
        }

        public static ItemDef GenerateItem(string name, ItemTag[] tags, TacticTier tTier)
        {
            ItemDef itemDef = ScriptableObject.CreateInstance<ItemDef>();

            itemDef.name = "ROT_" + name.ToUpperInvariant();
            itemDef.AutoPopulateTokens();

            switch (tTier)
            {
                case TacticTier.Normal:
                    SetItemTier(itemDef, ItemTier.Tier2);
                    break;
                case TacticTier.Radiant:
                    SetItemTier(itemDef, ItemTier.Boss);
                    break;
                case TacticTier.Artifact:
                    SetItemTier(itemDef, ItemTier.Tier3);
                    break;
            }

            GameObject prefab = AssetHandler.bundle.LoadAsset<GameObject>(name + ".prefab");
            if (prefab == null)
            {
                Log.Warning("Missing prefab file for item " + itemDef.name + ". Substituting default...");
                prefab = Addressables.LoadAssetAsync<GameObject>("RoR2/Base/Mystery/PickupMystery.prefab").WaitForCompletion();
            }
            ModelPanelParameters modelPanelParameters = prefab.AddComponent<ModelPanelParameters>();
            modelPanelParameters.focusPointTransform = prefab.transform;
            modelPanelParameters.cameraPositionTransform = prefab.transform;
            modelPanelParameters.maxDistance = 10f;
            modelPanelParameters.minDistance = 5f;

            itemDef.pickupIconSprite = AssetHandler.bundle.LoadAsset<Sprite>(name + ".png");
            itemDef.pickupModelPrefab = prefab;
            itemDef.canRemove = true;
            itemDef.hidden = false;

            itemDef.tags = tags;

            // Add item to item dict
            ItemDisplayRuleDict displayRules = new ItemDisplayRuleDict(null);
            ItemAPI.Add(new CustomItem(itemDef, displayRules));

            // Add item to personal dict
            switch (tTier)
            {
                case TacticTier.Normal:
                    normalList.Add(itemDef);
                    break;
                case TacticTier.Radiant:
                    radiantList.Add(itemDef);
                    break;
                case TacticTier.Artifact:
                    artifactList.Add(itemDef);
                    break;
            }

            return itemDef;
        }

        public static void SetItemTier(ItemDef itemDef, ItemTier tier)
        {
            if (tier == ItemTier.NoTier)
            {
                try
                {
#pragma warning disable CS0618 // Type or member is obsolete
                    itemDef.deprecatedTier = tier;
#pragma warning restore CS0618 // Type or member is obsolete
                }
                catch (Exception e)
                {
                    Log.Warning(string.Format("Error setting deprecatedTier for {0}: {1}", itemDef.name, e));
                }
            }

            ItemTierCatalog.availability.CallWhenAvailable(() =>
            {
                if (itemDef) itemDef.tier = tier;
            });
        }
    }
}
