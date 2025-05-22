using BepInEx;
using R2API;
using R2API.Utils;
using RoR2;
using RoR2.ExpansionManagement;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Networking;

namespace RiskOfTactics
{
    // Dependencies
    [BepInDependency(ItemAPI.PluginGUID)]
    [BepInDependency(LanguageAPI.PluginGUID)]
    [BepInDependency(RecalculateStatsAPI.PluginGUID)]
    // Soft Dependencies
    [BepInDependency(LookingGlass.PluginInfo.PLUGIN_GUID, BepInDependency.DependencyFlags.SoftDependency)]
    // Compatibility
    [NetworkCompatibility(CompatibilityLevel.EveryoneMustHaveMod, VersionStrictness.EveryoneNeedSameModVersion)]

    /// Attack Damage -> Percent Damage
    /// Ability Power -> Flat Damage
    /// Attack Speed  -> Attack Speed
    /// Armor         -> Armor
    /// Health        -> Health
    /// Magic Resist  -> Shield
    /// Crit Chance   -> Crit Chance
    /// Mana          -> Cooldown Reduction

    [BepInPlugin(PluginGUID, PluginName, PluginVersion)]
    public class RiskOfTactics : BaseUnityPlugin
    {
        public const string PluginGUID = PluginAuthor + "." + PluginName;
        public const string PluginAuthor = "shirograhm";
        public const string PluginName = "RiskOfTactics";
        public const string PluginVersion = "0.1.0";

        public static PluginInfo PInfo { get; private set; }

        public static System.Random RandGen = new();

        public static ExpansionDef voidDLC;

        public void Awake()
        {
            PInfo = Info;
            //voidDLC = Addressables.LoadAssetAsync<ExpansionDef>("RoR2/DLC1/Common/DLC1.asset").WaitForCompletion();

            Log.Init(Logger);
            AssetHandler.Init();
            GenericGameEvents.Init();
            ConfigOptions.Init();

            ItemCatalog.availability.CallWhenAvailable(ItemRemover.Init);
            ItemCatalog.availability.CallWhenAvailable(Integrations.Init);
            //ItemCatalog.availability.CallWhenAvailable(InjectVoidItemTramsforms);

            // Components
            if (BFSword.isEnabled.Value)
                BFSword.Init();
            if (ChainVest.isEnabled.Value)
                ChainVest.Init();
            if (GiantsBelt.isEnabled.Value)
                GiantsBelt.Init();
            if (NeedlesslyLargeRod.isEnabled.Value)
                NeedlesslyLargeRod.Init();
            if (NegatronCloak.isEnabled.Value)
                NegatronCloak.Init();
            if (RecurveBow.isEnabled.Value)
                RecurveBow.Init();
            if (SparringGloves.isEnabled.Value)
                SparringGloves.Init();
            if (TearOfTheGoddess.isEnabled.Value)
                TearOfTheGoddess.Init();

            InjectTFTItemCompletion();

            // Completes
            if (AdaptiveHelm.isEnabled.Value)
                AdaptiveHelm.Init();
            if (ArchangelsStaff.isEnabled.Value)
                ArchangelsStaff.Init();
            if (Bloodthirster.isEnabled.Value)
                Bloodthirster.Init();
            if (Crownguard.isEnabled.Value)
                Crownguard.Init();
            if (Deathblade.isEnabled.Value)
                Deathblade.Init();
            if (DragonsClaw.isEnabled.Value)
                DragonsClaw.Init();
            if (Guardbreaker.isEnabled.Value)
                Guardbreaker.Init();
            if (GiantSlayer.isEnabled.Value)
                GiantSlayer.Init();
            if (HandOfJustice.isEnabled.Value)
                HandOfJustice.Init();
            if (JeweledGauntlet.isEnabled.Value)
                JeweledGauntlet.Init();
            if (Quicksilver.isEnabled.Value)
                Quicksilver.Init();
            if (RabadonsDeathcap.isEnabled.Value)
                RabadonsDeathcap.Init();
            if (SpearOfShojin.isEnabled.Value)
                SpearOfShojin.Init();
            if (StatikkShiv.isEnabled.Value)
                StatikkShiv.Init();
            if (SteadfastHeart.isEnabled.Value)
                SteadfastHeart.Init();
            if (WarmogsArmor.isEnabled.Value)
                WarmogsArmor.Init();

            // Radiants

            // Supports


            Log.Message("Finished initializations.");
        }

        private void InjectTFTItemCompletion()
        {
            On.RoR2.Inventory.GiveItem_ItemIndex_int += (orig, self, itemIndex, count) =>
            {
                ItemIndex[] componentList =
                {
                    BFSword.itemDef.itemIndex,
                    ChainVest.itemDef.itemIndex,
                    GiantsBelt.itemDef.itemIndex,
                    NeedlesslyLargeRod.itemDef.itemIndex,
                    NegatronCloak.itemDef.itemIndex,
                    RecurveBow.itemDef.itemIndex,
                    SparringGloves.itemDef.itemIndex,
                    TearOfTheGoddess.itemDef.itemIndex
                };

                if (componentList.Contains(itemIndex))
                {
                    CharacterMaster master = self.GetComponent<CharacterMaster>();
                    if (master && master.inventory)
                    {
                        Inventory inv = master.inventory;
                        foreach (ItemIndex dex in componentList)
                        {
                            if (inv.GetItemCount(dex) > 0)
                            {
                                ItemIndex completeItem = Utils.GetCompletedItemFromParts(itemIndex, dex);
                                if (completeItem == ItemIndex.None) continue;

                                // Remove components
                                inv.RemoveItem(itemIndex);
                                inv.RemoveItem(dex);
                                // Add new full item
                                inv.GiveItem(completeItem);
                                // Notify player
                                CharacterMasterNotificationQueue.PushItemNotification(
                                    master, completeItem);

                                break;
                            }
                        }
                    }
                }

                orig(self, itemIndex, count);
            };
        }

        //private void InjectVoidItemTramsforms()
        //{
        //    On.RoR2.Items.ContagiousItemManager.Init += (orig) =>
        //    {
        //        List<ItemDef.Pair> newVoidPairs = new List<ItemDef.Pair> { };

        //        ItemRelationshipType key = DLC1Content.ItemRelationshipTypes.ContagiousItem;
        //        Debug.Log(key);
        //        ItemDef.Pair[] voidPairs = ItemCatalog.itemRelationships[DLC1Content.ItemRelationshipTypes.ContagiousItem];
        //        ItemCatalog.itemRelationships[DLC1Content.ItemRelationshipTypes.ContagiousItem] = voidPairs.Union(newVoidPairs).ToArray();
        //        Debug.Log("Injected void item transformations.");
        //        orig();
        //    };
        //}

        //private ItemDef.Pair GenerateVoidItemPair(ItemDef item1, ItemDef item2)
        //{
        //    return new ItemDef.Pair()
        //    {
        //        itemDef1 = item1,
        //        itemDef2 = item2
        //    };
        //}

        private void Update()
        {
            Testing.RunUpdate();
        }
    }
}
