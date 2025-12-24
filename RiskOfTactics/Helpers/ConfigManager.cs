using BepInEx;
using BepInEx.Configuration;
using System;
using System.Collections.Generic;

namespace RiskOfTactics.Helpers
{
    public static class ConfigManager
    {
        public static class Scaling
        {
            public static ConfigFile config = new(Paths.ConfigPath + "\\RiskOfTactics.cfg", true);
            public static string categoryName = "RiskOfTactics Config";
            public static string categoryGUID = RiskOfTactics.PluginGUID + "_config";

            public static ConfigOptions.ConfigurableValue<bool> useCustomValues = ConfigOptions.ConfigurableValue.CreateBool(
                categoryGUID,
                categoryName,
                config,
                "! Important !",
                "Use Custom Configs",
                false,
                "Set to true to enable the custom config values below."
            );
            public static ConfigOptions.ConfigurableValue<string> meleeCharactersList = ConfigOptions.ConfigurableValue.CreateString(
                categoryGUID,
                categoryName,
                config,
                "! Important !",
                "Melee Characters",
                "MercBody,LoaderBody,CrocoBody,FalseSonBody",
                "List of melee characters. Add modded charater bodies here to enable selective item effects (e.g. Adaptive Helm)."
            );
            public static ConfigOptions.ConfigurableValue<string> rangedCharactersList = ConfigOptions.ConfigurableValue.CreateString(
                categoryGUID,
                categoryName,
                config,
                "! Important !",
                "Ranged Characters",
                "CommandoBody,HuntressBody,Bandit2Body,ToolbotBody,EngiBody,EngiTurretBody,MageBody,TreebotBody,CaptainBody,RailGunnerBody,VoidSurvivorBody,SeekerBody,ChefBody,ScavBody",
                "List of ranged characters. Add modded charater bodies here to enable selective item effects (e.g. Adaptive Helm)."
            );
            public static ConfigOptions.ConfigurableValue<float> radiantItemStatMultiplier = ConfigOptions.ConfigurableValue.CreateFloat(
                categoryGUID,
                categoryName,
                config,
                "! Important !",
                "Radiant Stat Multiplier",
                2f,
                0f,
                1000000f,
                "Value all stats are multiplied by when upgraded to radiant."
            );
        }
    }

    public class ConfigurableValue<T> : ConfigOptions.ConfigurableValue<T>
    {
        public ConfigurableValue(string section, string key, float defaultValue, string description = "", List<string> stringsToAffect = null, bool isRadiantStat = false, Action<float> onChanged = null) : base(ConfigManager.Scaling.config, section, key, (T)Convert.ChangeType(defaultValue, typeof(T)), description, stringsToAffect, isRadiantStat, ConfigManager.Scaling.useCustomValues.bepinexConfigEntry)
        {
            CreateFloat(ConfigManager.Scaling.categoryGUID, ConfigManager.Scaling.categoryName, ConfigManager.Scaling.config, section, key, defaultValue, min: 0f, max: 1000000f, description: description, stringsToAffect: stringsToAffect, isRadiantStat: isRadiantStat, useCustomValueConfigEntry: ConfigManager.Scaling.useCustomValues.bepinexConfigEntry, onChanged: onChanged);
        }

        public ConfigurableValue(string section, string key, int defaultValue, string description = "", List<string> stringsToAffect = null, bool isRadiantStat = false, Action<int> onChanged = null) : base(ConfigManager.Scaling.config, section, key, (T)Convert.ChangeType(defaultValue, typeof(T)), description, stringsToAffect, isRadiantStat, ConfigManager.Scaling.useCustomValues.bepinexConfigEntry)
        {
            CreateInt(ConfigManager.Scaling.categoryGUID, ConfigManager.Scaling.categoryName, ConfigManager.Scaling.config, section, key, defaultValue, min: 0, max: 1000000, description: description, stringsToAffect: stringsToAffect, isRadiantStat: isRadiantStat, useCustomValueConfigEntry: ConfigManager.Scaling.useCustomValues.bepinexConfigEntry, onChanged: onChanged);
        }

        public ConfigurableValue(string section, string key, bool defaultValue, string description = "", List<string> stringsToAffect = null, Action<bool> onChanged = null) : base(ConfigManager.Scaling.config, section, key, (T)Convert.ChangeType(defaultValue, typeof(T)), description, stringsToAffect, false, ConfigManager.Scaling.useCustomValues.bepinexConfigEntry)
        {
            CreateBool(ConfigManager.Scaling.categoryGUID, ConfigManager.Scaling.categoryName, ConfigManager.Scaling.config, section, key, defaultValue, description: description, stringsToAffect: stringsToAffect, isRadiantStat: false, useCustomValueConfigEntry: ConfigManager.Scaling.useCustomValues.bepinexConfigEntry, onChanged: onChanged);
        }

        public ConfigurableValue(string section, string key, string defaultValue, string description = "", List<string> stringsToAffect = null, Action<string> onChanged = null) : base(ConfigManager.Scaling.config, section, key, (T)Convert.ChangeType(defaultValue, typeof(T)), description, stringsToAffect, false, ConfigManager.Scaling.useCustomValues.bepinexConfigEntry)
        {
            CreateString(ConfigManager.Scaling.categoryGUID, ConfigManager.Scaling.categoryName, ConfigManager.Scaling.config, section, key, defaultValue, description: description, stringsToAffect: stringsToAffect, isRadiantStat: false, useCustomValueConfigEntry: ConfigManager.Scaling.useCustomValues.bepinexConfigEntry, onChanged: onChanged);
        }
    }
}
