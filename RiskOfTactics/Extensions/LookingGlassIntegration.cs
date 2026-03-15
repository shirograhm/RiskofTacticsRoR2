using LookingGlass.ItemStatsNameSpace;
using RiskOfTactics.Content.Items.Artifacts;
using RiskOfTactics.Content.Items.Completes;
using RiskOfTactics.Managers;
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
                // Normal Items & Radiant Variants
                if (BrambleVest.isEnabled.Value)
                {
                    RegisterStatsForItemWithRadiantVariant(BrambleVest.itemDef, BrambleVest.radiantDef, [
                        new("Damage Reflected: ", ItemStatsDef.ValueType.Damage, ItemStatsDef.MeasurementUnits.Number)
                        ], (master, itemCount) =>
                        {
                            var values = new List<float> { };
                            if (master && master.inventory && master.inventory.GetComponent<BrambleVest.Statistics>())
                                values.Add(master.inventory.GetComponent<BrambleVest.Statistics>().DamageReflected);
                            else
                                values.Add(0f);

                            return values;
                        });
                }
                if (SpearOfShojin.isEnabled.Value)
                {
                    RegisterStatsForItemWithRadiantVariant(SpearOfShojin.itemDef, SpearOfShojin.radiantDef, [
                        new("On-Hit Reduction: ", ItemStatsDef.ValueType.Utility, ItemStatsDef.MeasurementUnits.Percentage)
                        ], (master, itemCount) =>
                        {
                            return [Utilities.GetHyperbolicStacking(SpearOfShojin.percentCooldownOnHit * ConfigManager.Scaling.radiantItemStatMultiplier, SpearOfShojin.percentCooldownOnHitExtraStacks * ConfigManager.Scaling.radiantItemStatMultiplier, itemCount)];
                        });
                }


                // Artifacts
                if (GamblersBlade.isEnabled.Value)
                {
                    RegisterStatsForItem(GamblersBlade.itemDef, [
                        new("Max Money Threshold: ", ItemStatsDef.ValueType.Gold, ItemStatsDef.MeasurementUnits.Money)
                        ], (master, itemCount) =>
                        {
                            return [GamblersBlade.moneyEffectCap.Value * Utilities.GetDifficultyAsMultiplier()];
                        });
                }
                if (HorizonFocus.isEnabled.Value)
                {
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
            }

            private static void RegisterStatsForItemWithRadiantVariant(ItemDef defaultItem, ItemDef radiantItem, List<ItemStatLine> statLines, Func<CharacterMaster, int, List<float>> func)
            {
                foreach (var itemDef in new ItemDef[] { defaultItem, radiantItem })
                {
                    if (!itemDef) throw new ArgumentNullException(nameof(itemDef));

                    ItemStatsDef stats = new();
                    foreach (ItemStatLine line in statLines)
                    {
                        stats.descriptions.Add(line.Name);
                        stats.valueTypes.Add(line.ValueType);
                        stats.measurementUnits.Add(line.Units);
                    }
                    stats.calculateValues = func;
                    ItemDefinitions.allItemDefinitions.Add((int)itemDef.itemIndex, stats);
                }
            }

            private static void RegisterStatsForItem(ItemDef itemDef, List<ItemStatLine> statLines, Func<CharacterMaster, int, List<float>> func)
            {
                if (!itemDef) throw new ArgumentNullException(nameof(itemDef));

                ItemStatsDef stats = new();
                foreach (ItemStatLine line in statLines)
                {
                    stats.descriptions.Add(line.Name);
                    stats.valueTypes.Add(line.ValueType);
                    stats.measurementUnits.Add(line.Units);
                }
                stats.calculateValues = func;
                ItemDefinitions.allItemDefinitions.Add((int)itemDef.itemIndex, stats);
            }

            private readonly struct ItemStatLine(string n, ItemStatsDef.ValueType v, ItemStatsDef.MeasurementUnits u)
            {
                public string Name { get; } = n;
                public ItemStatsDef.ValueType ValueType { get; } = v;
                public ItemStatsDef.MeasurementUnits Units { get; } = u;
            }
        }
    }
}
