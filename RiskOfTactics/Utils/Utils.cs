using R2API.Networking;
using R2API.Networking.Interfaces;
using RoR2;
using System;
using System.Linq;
using UnityEngine;
using UnityEngine.Networking;

namespace RiskOfTactics
{
    public static class Utils
    {
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
                this.netID = ID;
            }

            public void Deserialize(NetworkReader reader)
            {
                netID = reader.ReadNetworkId();
            }

            public void OnReceived()
            {
                if (NetworkServer.active) return;

                GameObject obj = RoR2.Util.FindNetworkObject(netID);
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
                    Log.Warning(String.Format("Error setting deprecatedTier for {0}: {1}", itemDef.name, e));
                }
            }

            ItemTierCatalog.availability.CallWhenAvailable(() =>
            {
                if (itemDef) itemDef.tier = tier;
            });
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

        public static float GetDifficultyAsPercentage()
        {
            return (Stage.instance.entryDifficultyCoefficient - 1f) / 98f;
        }

        public static float GetDifficultyAsMultiplier()
        {
            return (Stage.instance.entryDifficultyCoefficient);
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
            return 1f - 1f / (1f + percent * extraPercent * (count - 1));
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
    }
}
