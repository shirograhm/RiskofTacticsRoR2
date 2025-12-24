using RiskOfTactics.Helpers;
using System;

namespace RiskOfTactics.Extensions
{
    internal class Integrations
    {
        internal static bool lookingGlassEnabled = false;

        internal static void Init()
        {
            System.Collections.Generic.Dictionary<string, BepInEx.PluginInfo> pluginInfos = BepInEx.Bootstrap.Chainloader.PluginInfos;

            if (pluginInfos.ContainsKey(LookingGlass.PluginInfo.PLUGIN_GUID))
            {
                try
                {
                    ROTLogger.Debug("Running code injection for LookingGlass.");
                    LookingGlassIntegration.Init();
                    lookingGlassEnabled = true;
                }
                catch (Exception e)
                {
                    ROTLogger.Error(e);
                }
            }
        }
    }
}