using R2API.Networking;
using R2API.Networking.Interfaces;
using RoR2;
using System;
using System.Collections.Generic;
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

        public static float GetDifficultyAsPercentage()
        {
            return (Stage.instance.entryDifficultyCoefficient - 1f) / 98f;
        }

        public static float GetExponentialStacking(float percent, int count)
        {
            return 1f - Mathf.Pow(1f - percent, count);
        }

        public static float GetHyperbolicStacking(float percent, int count)
        {
            return 1f - 1f / (1f + percent * count);
        }

        private const int SWORD_INDEX = 0;
        private const int VEST_INDEX = 1;
        private const int BELT_INDEX = 2;
        private const int ROD_INDEX = 3;
        private const int CLOAK_INDEX = 4;
        private const int BOW_INDEX = 5;
        private const int GLOVES_INDEX = 6;
        private const int TEAR_INDEX = 7;

        public static ItemIndex GetCompletedItemFromParts(ItemIndex[] indices, ItemIndex index1, ItemIndex index2)
        {
            List<ItemIndex> list = new List<ItemIndex>();
            list.Append(index1);
            Log.Debug("appending index1: " + index1);
            list.Append(index2);
            Log.Debug("appending index2: " + index2);
            list.Sort();

            Log.Debug("list sorted: " + list.ToArray().ToString());

            // Adaptive Helm
            if (list.Contains(TearOfTheGoddess.itemDef.itemIndex) && 
                list.Contains(NegatronCloak.itemDef.itemIndex))
                return AdaptiveHelm.itemDef.itemIndex;
            // Archangel's Staff
            if (list.Contains(TearOfTheGoddess.itemDef.itemIndex) &&
                list.Contains(NeedlesslyLargeRod.itemDef.itemIndex))
                return ArchangelsStaff.itemDef.itemIndex;
            // Bloodthirster
            if (list.Contains(indices[SWORD_INDEX]) &&
                list.Contains(indices[CLOAK_INDEX]))
                return Bloodthirster.itemDef.itemIndex;
            // Crownguard
            if (list.Contains(NeedlesslyLargeRod.itemDef.itemIndex) &&
                list.Contains(ChainVest.itemDef.itemIndex))
                return Crownguard.itemDef.itemIndex;
            // Deathblade
            if (list.Contains(BFSword.itemDef.itemIndex) &&
                list.IndexOf(BFSword.itemDef.itemIndex) != list.LastIndexOf(BFSword.itemDef.itemIndex))
                return Deathblade.itemDef.itemIndex;
            // Dragon's Claw
            if (list.Contains(NegatronCloak.itemDef.itemIndex) &&
                list.IndexOf(NegatronCloak.itemDef.itemIndex) != list.LastIndexOf(NegatronCloak.itemDef.itemIndex))
                return DragonsClaw.itemDef.itemIndex;
            // Giant Slayer
            if (list.Contains(BFSword.itemDef.itemIndex) &&
                list.Contains(RecurveBow.itemDef.itemIndex))
                return GiantSlayer.itemDef.itemIndex;
            // Guardbreaker
            if (list.Contains(GiantsBelt.itemDef.itemIndex) &&
                list.Contains(SparringGloves.itemDef.itemIndex))
                return Guardbreaker.itemDef.itemIndex;
            // Hand of Justice
            if (list.Contains(TearOfTheGoddess.itemDef.itemIndex) &&
                list.Contains(SparringGloves.itemDef.itemIndex))
                return HandOfJustice.itemDef.itemIndex;
            // Jeweled Gauntlet
            if (list.Contains(NeedlesslyLargeRod.itemDef.itemIndex) &&
                list.Contains(SparringGloves.itemDef.itemIndex))
                return JeweledGauntlet.itemDef.itemIndex;
            // Quicksilver
            if (list.Contains(SparringGloves.itemDef.itemIndex) &&
                list.Contains(NegatronCloak.itemDef.itemIndex))
                return Quicksilver.itemDef.itemIndex;
            // Rabadon's Deathcap
            if (list.Contains(NeedlesslyLargeRod.itemDef.itemIndex) &&
                list.IndexOf(NeedlesslyLargeRod.itemDef.itemIndex) != list.LastIndexOf(NeedlesslyLargeRod.itemDef.itemIndex))
                return RabadonsDeathcap.itemDef.itemIndex;
            // Spear of Shojin
            if (list.Contains(BFSword.itemDef.itemIndex) &&
                list.Contains(TearOfTheGoddess.itemDef.itemIndex))
                return SpearOfShojin.itemDef.itemIndex;
            // Statikk Shiv
            if (list.Contains(TearOfTheGoddess.itemDef.itemIndex) &&
                list.Contains(RecurveBow.itemDef.itemIndex))
                return StatikkShiv.itemDef.itemIndex;
            // Steadfast Heart
            if (list.Contains(ChainVest.itemDef.itemIndex) &&
                list.Contains(SparringGloves.itemDef.itemIndex))
                return SteadfastHeart.itemDef.itemIndex;
            // Warmog's Armor
            if (list.Contains(GiantsBelt.itemDef.itemIndex) &&
                list.IndexOf(GiantsBelt.itemDef.itemIndex) != list.LastIndexOf(GiantsBelt.itemDef.itemIndex))
                return WarmogsArmor.itemDef.itemIndex;


            // Return none item if no recipes (should never reach this... i hope)
            return ItemIndex.None;
        }

        public static ItemDef GetRandomItemOfTier(ItemTier tier)
        {
            ItemDef[] tierItems = ItemCatalog.allItemDefs.Where(itemDef => itemDef.tier == tier).ToArray();

            return tierItems[RiskOfTactics.RandGen.Next(0, tierItems.Length)];
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
    }
}
