using R2API.Networking;
using R2API.Networking.Interfaces;
using RoR2;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Networking;

namespace RiskOfTactics.Helpers
{
    public static class Utilities
    {
        public static Color STRIKERS_FLAIL_COLOR = new(242f, 208f, 111f);
        public static Color STRIKERS_FLAIL_STACKED_COLOR = new(252f, 186f, 3f);
        public static Color HELLFIRE_HATCHET_COLOR = new(245f, 163f, 69f);

        internal static void Init()
        {
            NetworkingAPI.RegisterMessageType<SyncForceRecalculate>();
        }

        private class SyncForceRecalculate : INetMessage
        {
            NetworkInstanceId netID;

            public SyncForceRecalculate() { }
            public SyncForceRecalculate(NetworkInstanceId ID)
            {
                netID = ID;
            }

            public void Deserialize(NetworkReader reader)
            {
                netID = reader.ReadNetworkId();
            }

            public void OnReceived()
            {
                if (NetworkServer.active) return;

                GameObject obj = Util.FindNetworkObject(netID);
                if (obj)
                {
                    CharacterBody body = obj.GetComponent<CharacterBody>();
                    if (body) body.RecalculateStats();
                }
            }

            public void Serialize(NetworkWriter writer)
            {
                writer.Write(netID);
                writer.FinishMessage();
            }
        }

        public static void ForceRecalculate(CharacterBody body)
        {
            body.RecalculateStats();
            if (NetworkServer.active) new SyncForceRecalculate(body.netId);
        }

        public static void AddRecalculateOnFrameHook(ItemDef def)
        {
            On.RoR2.CharacterBody.FixedUpdate += (orig, self) =>
            {
                orig(self);

                if (self && self.inventory)
                {
                    int count = self.inventory.GetItemCountEffective(def);
                    if (count > 0)
                    {
                        ForceRecalculate(self);
                    }
                }
            };
        }

        public static CharacterBody GetMinionOwnershipParentBody(CharacterBody body)
        {
            if (body && body.master && body.master.minionOwnership && body.master.minionOwnership.ownerMaster && body.master.minionOwnership.ownerMaster.GetBody())
            {
                return body.master.minionOwnership.ownerMaster.GetBody();
            }
            return body;
        }

        public static uint ScaleGoldWithDifficulty(int goldGranted)
        {
            return Convert.ToUInt32(goldGranted * (1 + 50 * GetDifficultyAsPercentage()));
        }

        public static float GetChanceAfterLuck(float percent, float luckIn)
        {
            int luck = Mathf.CeilToInt(luckIn);

            if (luck > 0)
                return 1f - Mathf.Pow(1f - percent, luck + 1);
            if (luck < 0)
                return Mathf.Pow(percent, Mathf.Abs(luck) + 1);

            return percent;
        }

        public static bool IsMeleeBodyPrefab(GameObject bodyPrefab)
        {
            if (!bodyPrefab) return false;

            string name = bodyPrefab.name;
            if (name.Contains("(Clone)"))
                name = name.Replace("(Clone)", "");

            string[] meleeBodies = ConfigManager.Scaling.meleeCharactersList.Value.Split(',');
            return meleeBodies.Contains(name);
        }

        public static bool IsRangedBodyPrefab(GameObject bodyPrefab)
        {
            if (!bodyPrefab) return false;

            string name = bodyPrefab.name;
            if (name.Contains("(Clone)"))
                name = name.Replace("(Clone)", "");

            string[] rangedBodies = ConfigManager.Scaling.rangedCharactersList.Value.Split(',');
            return rangedBodies.Contains(name);
        }

        public static float GetMissingHealth(HealthComponent healthComponent, bool includeShield)
        {
            if (includeShield)
                return healthComponent.fullCombinedHealth * (1 - healthComponent.combinedHealthFraction);
            else
                return healthComponent.fullHealth * (1 - healthComponent.healthFraction);
        }

        public static float GetDifficultyAsPercentage()
        {
            return (Stage.instance.entryDifficultyCoefficient - 1f) / 98f;
        }

        public static float GetDifficultyAsMultiplier()
        {
            return Stage.instance.entryDifficultyCoefficient;
        }

        public static float GetLinearStacking(float baseValue, int count)
        {
            return GetLinearStacking(baseValue, baseValue, count);
        }

        public static float GetLinearStacking(float baseValue, float extraValue, int count)
        {
            return baseValue + extraValue * (count - 1);
        }

        public static float GetExponentialStacking(float percent, int count)
        {
            return GetExponentialStacking(percent, percent, count);
        }

        public static float GetExponentialStacking(float percent, float stackPercent, int count)
        {
            return 1f - (1 - percent) * Mathf.Pow(1f - stackPercent, count - 1);
        }

        public static float GetReverseExponentialStacking(float baseValue, float reducePercent, int count)
        {
            return baseValue * Mathf.Pow(1 - reducePercent, count - 1);
        }

        public static float GetHyperbolicStacking(float percent, int count)
        {
            return GetHyperbolicStacking(percent, percent, count);
        }

        public static float GetHyperbolicStacking(float percent, float extraPercent, int count)
        {
            float denominator = (1f + percent) * (1 + extraPercent * (count - 1));
            return 1f - 1f / denominator;
        }

        internal static BuffDef GenerateBuffDef(string name, Sprite sprite, bool canStack, bool isHidden, bool isDebuff, bool isCooldown)
        {
            BuffDef returnable = ScriptableObject.CreateInstance<BuffDef>();

            returnable.name = name;
            returnable.iconSprite = sprite;
            returnable.canStack = canStack;
            returnable.isHidden = isHidden;
            returnable.isDebuff = isDebuff;
            returnable.isCooldown = isCooldown;

            return returnable;
        }

        /**
         * <summary>Returns true if the victim and attacker bodies are on the same team.</summary>
         * 
         * <param name="body1">Cannot be null.</param>
         * <param name="body2">Cannot be null.</param>
         */
        internal static bool OnSameTeam(CharacterBody body1, CharacterBody body2)
        {
            if (body1 == null) throw new ArgumentNullException("body1");
            if (body2 == null) throw new ArgumentNullException("body2");
            return body1.teamComponent && body2.teamComponent && body1.teamComponent.teamIndex == body2.teamComponent.teamIndex;
        }

        internal static bool IsValidTargetBody(CharacterBody body)
        {
            return body && body.healthComponent;
        }

        internal static void SpawnHealEffect(CharacterBody self)
        {
            EffectData effectData = new()
            {
                origin = self.transform.position,
                rootObject = self.gameObject
            };
            EffectManager.SpawnEffect(LegacyResourcesAPI.Load<GameObject>("Prefabs/Effects/MedkitHealEffect"), effectData, transmit: true);
        }

        internal static void RegisterVoidPair(ItemDef itemDef, ItemDef radiantDef)
        {
            On.RoR2.Items.ContagiousItemManager.Init += (orig) =>
            {
                List<ItemDef.Pair> newVoidPairs = [
                    new ItemDef.Pair() { itemDef1 = itemDef, itemDef2 = radiantDef }];

                ItemRelationshipType key = DLC1Content.ItemRelationshipTypes.ContagiousItem;
                Debug.Log(key);

                ItemDef.Pair[] voidPairs = ItemCatalog.itemRelationships[DLC1Content.ItemRelationshipTypes.ContagiousItem];
                ItemCatalog.itemRelationships[DLC1Content.ItemRelationshipTypes.ContagiousItem] = [.. voidPairs.Union(newVoidPairs)];

                Debug.Log("Injected radiant item transformation for " + itemDef.name + ".");
                orig();
            };
        }
    }
}
