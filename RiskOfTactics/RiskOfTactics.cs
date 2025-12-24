using BepInEx;
using R2API;
using R2API.Utils;
using RiskOfTactics.Buffs;
using RiskOfTactics.Exts;
using RiskOfTactics.Items.Artifacts;
using RiskOfTactics.Items.Completes;
using RoR2;
using RoR2.ExpansionManagement;
using UnityEngine.AddressableAssets;

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

        public static ExpansionDef voidDLC = Addressables.LoadAssetAsync<ExpansionDef>("RoR2/DLC1/Common/DLC1.asset").WaitForCompletion();

        public void Awake()
        {
            PInfo = Info;

            // Setup
            Log.Init(Logger);
            AssetHandler.Init();
            GenericGameEvents.Init();
            ConfigOptions.Init();

            // Mod Integrations
            ItemCatalog.availability.CallWhenAvailable(Integrations.Init);

            // Buffs
            Wound.Init();
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
            if (Quicksilver.isEnabled.Value)
                Quicksilver.Init();
            if (SpearOfShojin.isEnabled.Value)
                SpearOfShojin.Init();
            if (StatikkShiv.isEnabled.Value)
                StatikkShiv.Init();
            if (SunfireCape.isEnabled.Value)
                SunfireCape.Init();

            // Radiants

            // Artifacts
            if (GamblersBlade.isEnabled.Value)
                GamblersBlade.Init();

            Log.Message("Finished initializations.");
        }
    }
}
