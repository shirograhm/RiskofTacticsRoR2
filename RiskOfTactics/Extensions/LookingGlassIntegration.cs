using LookingGlass.ItemStatsNameSpace;
using RiskOfTactics.Content.Items.Artifacts;
using RiskOfTactics.Content.Items.Completes;
using RoR2;
using System;
using System.Collections.Generic;

namespace RiskOfTactics.Extensions
{
    internal static class LookingGlassIntegration
    {
        internal static void Init()
        {
            RoR2Application.onLoad += LookingGlassStats.RegisterStats;
        }

        public static class LookingGlassStats
        {
            public static void RegisterStats()
            {
                // Bramble Vest
                if (BrambleVest.isEnabled.Value)
                {
                    ItemStatsDef stats = new();
                    stats.descriptions.Add("Damage Reflected: ");
                    stats.valueTypes.Add(ItemStatsDef.ValueType.Damage);
                    stats.measurementUnits.Add(ItemStatsDef.MeasurementUnits.Number);
                    stats.calculateValues = (master, itemCount) =>
                    {
                        var values = new List<float> { };
                        if (master && master.inventory && master.inventory.GetComponent<BrambleVest.Statistics>())
                            values.Add(master.inventory.GetComponent<BrambleVest.Statistics>().DamageReflected);
                        else
                            values.Add(0f);

                        return values;
                    };
                    ItemDefinitions.allItemDefinitions.Add((int)BrambleVest.itemDef.itemIndex, stats);
                }

                if (GamblersBlade.isEnabled.Value)
                {
                    ItemStatsDef stats = new();
                    stats.descriptions.Add("Max Money Threshold: ");
                    stats.valueTypes.Add(ItemStatsDef.ValueType.Gold);
                    stats.measurementUnits.Add(ItemStatsDef.MeasurementUnits.Money);
                    stats.calculateValues = (master, itemCount) =>
                    {
                        return [GamblersBlade.moneyEffectCap.Value * Utilities.GetDifficultyAsMultiplier()];
                    };
                    ItemDefinitions.allItemDefinitions.Add((int)GamblersBlade.itemDef.itemIndex, stats);
                }

                //if (HorizonFocus.isEnabled.Value)
                //{
                //    ItemStatsDef stats = new();
                //    stats.descriptions.Add("Stun Chance: ");
                //    stats.valueTypes.Add(ItemStatsDef.ValueType.Utility);
                //    stats.measurementUnits.Add(ItemStatsDef.MeasurementUnits.Percentage);
                //    stats.descriptions.Add("Explosion Damage: ");
                //    stats.valueTypes.Add(ItemStatsDef.ValueType.Health);
                //    stats.measurementUnits.Add(ItemStatsDef.MeasurementUnits.PercentHealth);
                //    stats.calculateValues = (master, itemCount) =>
                //    {
                //        var values = new List<float> { };
                //        if (master)
                //            values.Add(Utilities.GetChanceAfterLuck(HorizonFocus.percentStunChance, master.luck));
                //        else
                //            values.Add(HorizonFocus.percentStunChance);

                //        values.Add(Utilities.GetHyperbolicStacking(HorizonFocus.percentLightningDamage, HorizonFocus.percentLightningDamageExtraStacks, itemCount));
                //        return values;
                //    };
                //    ItemDefinitions.allItemDefinitions.Add((int)HorizonFocus.itemDef.itemIndex, stats);
                //}

                RegisterStatsForItem(HorizonFocus.itemDef, [
                    new("Stun Chance: ", ItemStatsDef.ValueType.Utility, ItemStatsDef.MeasurementUnits.Percentage),
                    new("Explosion Damage: ", ItemStatsDef.ValueType.Health, ItemStatsDef.MeasurementUnits.PercentHealth)
                    ], (master, itemCount) =>
                {
                    var values = new List<float> { };
                    if (master)
                        values.Add(Utilities.GetChanceAfterLuck(HorizonFocus.percentStunChance, master.luck));
                    else
                        values.Add(HorizonFocus.percentStunChance);

                    values.Add(Utilities.GetHyperbolicStacking(HorizonFocus.percentLightningDamage, HorizonFocus.percentLightningDamageExtraStacks, itemCount));
                    return values;
                });
            }

            public static void RegisterStatsForItem(ItemDef itemDef, List<ItemStatLine> statLines, Func<CharacterMaster, int, List<float>> func)
            {
                ItemStatsDef stats = new();
                foreach (ItemStatLine line in statLines)
                {
                    stats.descriptions.Add(line.Name);
                    stats.valueTypes.Add(line.ValueType);
                    stats.measurementUnits.Add(line.Units);
                }
                stats.calculateValues = func;
                ItemDefinitions.allItemDefinitions.Add((int)HorizonFocus.itemDef.itemIndex, stats);
            }

            public readonly struct ItemStatLine(string n, ItemStatsDef.ValueType v, ItemStatsDef.MeasurementUnits u)
            {
                public string Name { get; } = n;
                public ItemStatsDef.ValueType ValueType { get; } = v;
                public ItemStatsDef.MeasurementUnits Units { get; } = u;
            }
        }
    }
}
