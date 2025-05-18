using BepInEx.Configuration;
using RoR2;
using RoR2.ContentManagement;
using System;
using System.Collections.Generic;
using System.Text;

namespace RiskOfTactics
{
    internal class ItemRemover
    {
        public static ConfigurableValue<bool> isEnabled = new(
                "Other Items",
                "Enabled",
                false,
                "Set to true to enable all base items in the game.",
                new List<string>()
                {
                    "FEATURE_ITEMREMOVAL_DESC"
                }
            );

        private static readonly ItemDef[] baseItemDefs =
        {
            RoR2Content.Items.AlienHead,
            RoR2Content.Items.ArmorPlate,
            RoR2Content.Items.ArmorReductionOnHit,
            RoR2Content.Items.ArtifactKey,
            RoR2Content.Items.AttackSpeedOnCrit,
            RoR2Content.Items.AutoCastEquipment,
            RoR2Content.Items.Bandolier,
            RoR2Content.Items.BarrierOnKill,
            RoR2Content.Items.BarrierOnOverHeal,
            RoR2Content.Items.Bear,
            RoR2Content.Items.BeetleGland,
            RoR2Content.Items.Behemoth,
            RoR2Content.Items.BleedOnHit,
            RoR2Content.Items.BleedOnHitAndExplode,
            RoR2Content.Items.BonusGoldPackOnKill,
            RoR2Content.Items.BoostEquipmentRecharge,
            RoR2Content.Items.BossDamageBonus,
            RoR2Content.Items.BounceNearby,
            RoR2Content.Items.CaptainDefenseMatrix,
            RoR2Content.Items.ChainLightning,
            RoR2Content.Items.Clover,
            RoR2Content.Items.CrippleWardOnLevel,
            RoR2Content.Items.CritGlasses,
            RoR2Content.Items.Crowbar,
            RoR2Content.Items.Dagger,
            RoR2Content.Items.DeathMark,
            RoR2Content.Items.EnergizedOnEquipmentUse,
            RoR2Content.Items.EquipmentMagazine,
            RoR2Content.Items.ExecuteLowHealthElite,
            RoR2Content.Items.ExplodeOnDeath,
            RoR2Content.Items.ExtraLife,
            RoR2Content.Items.ExtraLifeConsumed,
            RoR2Content.Items.FallBoots,
            RoR2Content.Items.Feather,
            RoR2Content.Items.FireballsOnHit,
            RoR2Content.Items.FireRing,
            RoR2Content.Items.Firework,
            RoR2Content.Items.FlatHealth,
            RoR2Content.Items.FocusConvergence,
            RoR2Content.Items.GhostOnKill,
            RoR2Content.Items.GoldOnHit,
            RoR2Content.Items.HeadHunter,
            RoR2Content.Items.HealOnCrit,
            RoR2Content.Items.HealWhileSafe,
            RoR2Content.Items.Hoof,
            RoR2Content.Items.IceRing,
            RoR2Content.Items.Icicle,
            RoR2Content.Items.IgniteOnKill,
            RoR2Content.Items.IncreaseHealing,
            RoR2Content.Items.Infusion,
            RoR2Content.Items.JumpBoost,
            RoR2Content.Items.KillEliteFrenzy,
            RoR2Content.Items.Knurl,
            RoR2Content.Items.LaserTurbine,
            RoR2Content.Items.LightningStrikeOnHit,
            RoR2Content.Items.LunarBadLuck,
            RoR2Content.Items.LunarDagger,
            RoR2Content.Items.LunarPrimaryReplacement,
            RoR2Content.Items.LunarSecondaryReplacement,
            RoR2Content.Items.LunarSpecialReplacement,
            RoR2Content.Items.LunarTrinket,
            RoR2Content.Items.LunarUtilityReplacement,
            RoR2Content.Items.Medkit,
            RoR2Content.Items.Missile,
            RoR2Content.Items.MonstersOnShrineUse,
            RoR2Content.Items.Mushroom,
            RoR2Content.Items.NearbyDamageBonus,
            RoR2Content.Items.NovaOnHeal,
            RoR2Content.Items.NovaOnLowHealth,
            RoR2Content.Items.ParentEgg,
            RoR2Content.Items.Pearl,
            RoR2Content.Items.PersonalShield,
            RoR2Content.Items.Phasing,
            RoR2Content.Items.Plant,
            RoR2Content.Items.RandomDamageZone,
            RoR2Content.Items.RepeatHeal,
            RoR2Content.Items.RoboBallBuddy,
            RoR2Content.Items.ScrapGreen,
            RoR2Content.Items.ScrapRed,
            RoR2Content.Items.ScrapWhite,
            RoR2Content.Items.ScrapYellow,
            RoR2Content.Items.SecondarySkillMagazine,
            RoR2Content.Items.Seed,
            RoR2Content.Items.ShieldOnly,
            RoR2Content.Items.ShinyPearl,
            RoR2Content.Items.ShockNearby,
            RoR2Content.Items.SiphonOnLowHealth,
            RoR2Content.Items.SlowOnHit,
            RoR2Content.Items.SprintArmor,
            RoR2Content.Items.SprintBonus,
            RoR2Content.Items.SprintOutOfCombat,
            RoR2Content.Items.SprintWisp,
            RoR2Content.Items.Squid,
            RoR2Content.Items.StickyBomb,
            RoR2Content.Items.StunChanceOnHit,
            RoR2Content.Items.Syringe,
            RoR2Content.Items.Talisman,
            RoR2Content.Items.Thorns,
            RoR2Content.Items.TitanGoldDuringTP,
            RoR2Content.Items.TonicAffliction,
            RoR2Content.Items.Tooth,
            RoR2Content.Items.TPHealingNova,
            RoR2Content.Items.TreasureCache,
            RoR2Content.Items.UtilitySkillMagazine,
            RoR2Content.Items.WarCryOnMultiKill,
            RoR2Content.Items.WardOnLevel,

            DLC1Content.Items.AttackSpeedAndMoveSpeed,
            DLC1Content.Items.BearVoid,
            DLC1Content.Items.BleedOnHitVoid,
            DLC1Content.Items.ChainLightningVoid,
            DLC1Content.Items.CloverVoid,
            DLC1Content.Items.CritDamage,
            DLC1Content.Items.CritGlassesVoid,
            DLC1Content.Items.DroneWeapons,
            DLC1Content.Items.ElementalRingVoid,
            DLC1Content.Items.EquipmentMagazineVoid,
            DLC1Content.Items.ExplodeOnDeathVoid,
            DLC1Content.Items.ExtraLifeVoid,
            DLC1Content.Items.ExtraLifeVoidConsumed,
            DLC1Content.Items.FragileDamageBonus,
            DLC1Content.Items.FragileDamageBonusConsumed,
            DLC1Content.Items.FreeChest,
            DLC1Content.Items.GoldOnHurt,
            DLC1Content.Items.HalfAttackSpeedHalfCooldowns,
            DLC1Content.Items.HalfSpeedDoubleHealth,
            DLC1Content.Items.HealingPotion,
            DLC1Content.Items.HealingPotionConsumed,
            DLC1Content.Items.ImmuneToDebuff,
            DLC1Content.Items.LunarSun,
            DLC1Content.Items.MinorConstructOnKill,
            DLC1Content.Items.MissileVoid,
            DLC1Content.Items.MoreMissile,
            DLC1Content.Items.MoveSpeedOnKill,
            DLC1Content.Items.MushroomVoid,
            DLC1Content.Items.OutOfCombatArmor,
            DLC1Content.Items.PermanentDebuffOnHit,
            DLC1Content.Items.PrimarySkillShuriken,
            DLC1Content.Items.RandomEquipmentTrigger,
            DLC1Content.Items.RandomlyLunar,
            DLC1Content.Items.RegeneratingScrap,
            DLC1Content.Items.RegeneratingScrapConsumed,
            DLC1Content.Items.SlowOnHitVoid,
            DLC1Content.Items.StrengthenBurn,
            DLC1Content.Items.TreasureCacheVoid,
            DLC1Content.Items.VoidMegaCrabItem,

            DLC2Content.Items.AttackSpeedPerNearbyAllyOrEnemy,
            DLC2Content.Items.BarrageOnBoss,
            DLC2Content.Items.BoostAllStats,
            DLC2Content.Items.DelayedDamage,
            DLC2Content.Items.ExtraShrineItem,
            DLC2Content.Items.ExtraStatsOnLevelUp,
            DLC2Content.Items.IncreaseDamageOnMultiKill,
            DLC2Content.Items.IncreasePrimaryDamage,
            DLC2Content.Items.ItemDropChanceOnKill,
            DLC2Content.Items.KnockBackHitEnemies,
            DLC2Content.Items.LowerPricedChests,
            DLC2Content.Items.LowerPricedChestsConsumed,
            DLC2Content.Items.MeteorAttackOnHighDamage,
            DLC2Content.Items.OnLevelUpFreeUnlock,
            DLC2Content.Items.SpeedBoostPickup,
            DLC2Content.Items.StunAndPierce,
            DLC2Content.Items.TeleportOnLowHealth,
            DLC2Content.Items.TeleportOnLowHealthConsumed,
            DLC2Content.Items.TriggerEnemyDebuffs
        };

        private static readonly EquipmentDef[] baseEquipmentDefs =
        {
            RoR2Content.Equipment.AffixBlue,
            RoR2Content.Equipment.AffixHaunted,
            RoR2Content.Equipment.AffixLunar,
            RoR2Content.Equipment.AffixPoison,
            RoR2Content.Equipment.AffixRed,
            RoR2Content.Equipment.AffixWhite,
            RoR2Content.Equipment.BFG,
            RoR2Content.Equipment.Blackhole,
            RoR2Content.Equipment.BurnNearby,
            RoR2Content.Equipment.Cleanse,
            RoR2Content.Equipment.CommandMissile,
            RoR2Content.Equipment.CrippleWard,
            RoR2Content.Equipment.CritOnUse,
            RoR2Content.Equipment.DeathProjectile,
            RoR2Content.Equipment.DroneBackup,
            RoR2Content.Equipment.FireBallDash,
            RoR2Content.Equipment.Fruit,
            RoR2Content.Equipment.GainArmor,
            RoR2Content.Equipment.Gateway,
            RoR2Content.Equipment.GoldGat,
            RoR2Content.Equipment.Jetpack,
            RoR2Content.Equipment.LifestealOnHit,
            RoR2Content.Equipment.Lightning,
            RoR2Content.Equipment.LunarPotion,
            RoR2Content.Equipment.Meteor,
            RoR2Content.Equipment.PassiveHealing,
            RoR2Content.Equipment.QuestVolatileBattery,
            RoR2Content.Equipment.Recycle,
            RoR2Content.Equipment.Saw,
            RoR2Content.Equipment.Scanner,
            RoR2Content.Equipment.TeamWarCry,
            RoR2Content.Equipment.Tonic,

            DLC1Content.Equipment.BossHunter,
            DLC1Content.Equipment.BossHunterConsumed,
            DLC1Content.Equipment.EliteVoidEquipment,
            DLC1Content.Equipment.GummyClone,
            DLC1Content.Equipment.LunarPortalOnUse,
            DLC1Content.Equipment.Molotov,
            DLC1Content.Equipment.MultiShopCard,
            DLC1Content.Equipment.VendingMachine,
            
            DLC2Content.Equipment.EliteAurelioniteEquipment,
            DLC2Content.Equipment.EliteBeadEquipment,
            DLC2Content.Equipment.HealAndRevive,
            DLC2Content.Equipment.HealAndReviveConsumed
        };

        internal static void Init()
        {
            foreach (ItemDef itemDef in baseItemDefs)
            {
                string name = itemDef.name;
                if (name == "") name = itemDef.nameToken;
                if (name == "") name = "<unknown>";

                itemDef._itemTierDef = null;
                Utils.SetItemTier(itemDef, ItemTier.NoTier);

                Log.Message("Disabling " + name + "...");
            }

            foreach (EquipmentDef equipmentDef in baseEquipmentDefs)
            {
                string name = equipmentDef.name;
                if (name == "") name = equipmentDef.nameToken;
                if (name == "") name = "<unknown>";

                equipmentDef.canDrop = false;
                equipmentDef.appearsInSinglePlayer = false;
                equipmentDef.appearsInMultiPlayer = false;
                equipmentDef.canBeRandomlyTriggered = false;
                equipmentDef.enigmaCompatible = false;
                equipmentDef.dropOnDeathChance = 0f;

                Log.Message("Disabling " + name + "...");
            }
        }
    }
}
