using BepInEx;
using R2API;
using R2API.Utils;
using RiskOfTactics.Content.Buffs;
using RiskOfTactics.Content.Items.Artifacts;
using RiskOfTactics.Content.Items.Completes;
using RiskOfTactics.Content.Items.Shrines;
using RiskOfTactics.Extensions;
using RiskOfTactics.Helpers;
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
        public static Xoroshiro128Plus rng = new((ulong)RandGen.Next());

        public static ExpansionDef voidDLC = Addressables.LoadAssetAsync<ExpansionDef>("RoR2/DLC1/Common/DLC1.asset").WaitForCompletion();

        public void Awake()
        {
            PInfo = Info;

            // Setup
            Log.Init(Logger);
            AssetHandler.Init();
            GenericGameEvents.Init();
            ConfigOptions.Init();
            Utilities.Init();

            // Mod Integrations
            ItemCatalog.availability.CallWhenAvailable(Integrations.Init);

            // Buffs
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
            if (StrikersFlail.isEnabled.Value)
                StrikersFlail.Init();
            if (SpearOfShojin.isEnabled.Value)
                SpearOfShojin.Init();
            if (SunfireCape.isEnabled.Value)
                SunfireCape.Init();

            // Artifacts
            if (GamblersBlade.isEnabled.Value)
                GamblersBlade.Init();
            if (HellfireHatchet.isEnabled.Value)
                HellfireHatchet.Init();
            if (HorizonFocus.isEnabled.Value)
                HorizonFocus.Init();
            if (StatikkShiv.isEnabled.Value)
                StatikkShiv.Init();

            // Shrines
            if (ForgeAnvil.isEnabled.Value)
                ForgeAnvil.Init();

            Log.Message("Finished initializations.");
        }
    }
}
