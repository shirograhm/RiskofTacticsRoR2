using BepInEx;
using R2API;
using R2API.Utils;
using RiskOfTactics.Items.Artifacts;
using RoR2;
using RoR2.ExpansionManagement;

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

    [BepInPlugin(PluginGUID, PluginName, PluginVersion)]
    public class RiskOfTactics : BaseUnityPlugin
    {
        public const string PluginGUID = PluginAuthor + "." + PluginName;
        public const string PluginAuthor = "shirograhm";
        public const string PluginName = "RiskOfTactics";
        public const string PluginVersion = "0.1.0";

        public static PluginInfo PInfo { get; private set; }

        public static System.Random RandGen = new();
        public static Xoroshiro128Plus xoroshiro = new Xoroshiro128Plus((ulong)RandGen.Next());

        public static ExpansionDef voidDLC;

        public void Awake()
        {
            PInfo = Info;
            //voidDLC = Addressables.LoadAssetAsync<ExpansionDef>("RoR2/DLC1/Common/DLC1.asset").WaitForCompletion();

            Log.Init(Logger);
            AssetHandler.Init();
            GenericGameEvents.Init();
            ConfigOptions.Init();

            ItemCatalog.availability.CallWhenAvailable(Integrations.Init);
            //ItemCatalog.availability.CallWhenAvailable(InjectVoidItemTramsforms);

            // Buffs
            //Burn.Init();
            //Wound.Init();
            Sunder.Init();

            // Completes
            if (AdaptiveHelm.isEnabled.Value)
                AdaptiveHelm.Init();
            if (ArchangelsStaff.isEnabled.Value)
                ArchangelsStaff.Init();
            if (Bloodthirster.isEnabled.Value)
                Bloodthirster.Init();
            if (BrambleVest.isEnabled.Value)
                BrambleVest.Init();
            if (Crownguard.isEnabled.Value)
                Crownguard.Init();
            if (DragonsClaw.isEnabled.Value)
                DragonsClaw.Init();
            if (GuinsoosRageblade.isEnabled.Value)
                GuinsoosRageblade.Init();
            if (HandOfJustice.isEnabled.Value)
                HandOfJustice.Init();
            //if (JeweledGauntlet.isEnabled.Value)
            //    JeweledGauntlet.Init();
            //if (Quicksilver.isEnabled.Value)
            //    Quicksilver.Init();
            //if (SpearOfShojin.isEnabled.Value)
            //    SpearOfShojin.Init();
            if (StatikkShiv.isEnabled.Value)
                StatikkShiv.Init();
            //if (SteadfastHeart.isEnabled.Value)
            //    SteadfastHeart.Init();
            //if (SunfireCape.isEnabled.Value)
            //    SunfireCape.Init();
            //if (WarmogsArmor.isEnabled.Value)
            //    WarmogsArmor.Init();

            // Radiants

            // Artifacts
            if (GamblersBlade.isEnabled.Value)
                GamblersBlade.Init();

            Log.Message("Finished initializations.");
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
            //Testing.RunUpdate();
        }
    }
}
