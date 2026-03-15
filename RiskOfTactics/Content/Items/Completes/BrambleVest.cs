using R2API;
using R2API.Networking;
using R2API.Networking.Interfaces;
using RiskOfTactics.Managers;
using RoR2;
using RoR2.Orbs;
using UnityEngine;
using UnityEngine.Networking;

namespace RiskOfTactics.Content.Items.Completes
{
    class BrambleVest
    {
        public static ItemDef itemDef;
        public static ItemDef radiantDef;

        // Become tankier and reflect a portion of the damage you take.
        public static ConfigurableValue<bool> isEnabled = new(
            "Item: Bramble Vest",
            "Enabled",
            true,
            "Whether or not the item is enabled.",
            ["ITEM_ROT_BRAMBLEVEST_DESC"]
        );
        public static ConfigurableValue<float> healthBonus = new(
            "Item: Bramble Vest",
            "Health",
            5f,
            "Percent health bonus when holding this item.",
            ["ITEM_ROT_BRAMBLEVEST_DESC"],
            true
        );
        public static ConfigurableValue<int> flatDamageReduction = new(
            "Item: Bramble Vest",
            "Damage Reduction",
            5,
            "Flat damage reduction bonus when holding this item.",
            ["ITEM_ROT_BRAMBLEVEST_DESC"],
            true
        );
        public static ConfigurableValue<float> reflectDamage = new(
            "Item: Bramble Vest",
            "Reflect Percent",
            100f,
            "Percent damage reflected back to the attacker when holding this item.",
            ["ITEM_ROT_BRAMBLEVEST_DESC"],
            true
        );
        public static ConfigurableValue<float> reflectProcCoefficient = new(
            "Item: Bramble Vest",
            "Reflect Proc Coefficient",
            1f,
            "Proc coefficient for the reflected damage hit when holding this item.",
            ["ITEM_ROT_BRAMBLEVEST_DESC"],
            false
        );
        public static readonly float percentHealthBonus = healthBonus.Value / 100f;
        public static readonly float percentReflectDamage = reflectDamage.Value / 100f;

        public class Statistics : MonoBehaviour
        {
            private float _damageReflected;
            public float DamageReflected
            {
                get { return _damageReflected; }
                set
                {
                    _damageReflected = value;
                    if (NetworkServer.active)
                    {
                        new Sync(gameObject.GetComponent<NetworkIdentity>().netId, value).Send(NetworkDestination.Clients);
                    }
                }
            }

            public class Sync : INetMessage
            {
                NetworkInstanceId objId;
                float damageReflected;

                public Sync()
                {
                }

                public Sync(NetworkInstanceId objId, float damage)
                {
                    this.objId = objId;
                    damageReflected = damage;
                }

                public void Deserialize(NetworkReader reader)
                {
                    objId = reader.ReadNetworkId();
                    damageReflected = reader.ReadSingle();
                }

                public void OnReceived()
                {
                    if (NetworkServer.active) return;

                    GameObject obj = Util.FindNetworkObject(objId);
                    if (obj != null)
                    {
                        Statistics component = obj.GetComponent<Statistics>();
                        if (component != null)
                        {
                            component.DamageReflected = damageReflected;
                        }
                    }
                }

                public void Serialize(NetworkWriter writer)
                {
                    writer.Write(objId);
                    writer.Write(damageReflected);

                    writer.FinishMessage();
                }
            }
        }

        internal static void Init()
        {
            // Normal Variant
            itemDef = ItemManager.GenerateItem("BrambleVest", [ItemTag.Damage, ItemTag.Utility, ItemTag.CanBeTemporary], ItemManager.TacticTier.Normal);
            radiantDef = ItemManager.GenerateItem("Radiant_BrambleVest", [ItemTag.Damage, ItemTag.Utility, ItemTag.CanBeTemporary], ItemManager.TacticTier.Radiant);

            NetworkingAPI.RegisterMessageType<Statistics.Sync>();

            //Utilities.RegisterRadiantUpgrade(itemDef, radiantDef);

            Hooks(itemDef, ItemManager.TacticTier.Normal);
            Hooks(radiantDef, ItemManager.TacticTier.Radiant);
        }

        public static void Hooks(ItemDef def, ItemManager.TacticTier tier)
        {
            float radiantMultiplier = tier.Equals(ItemManager.TacticTier.Radiant) ? ConfigManager.Scaling.radiantItemStatMultiplier : 1f;

            CharacterMaster.onStartGlobal += (obj) =>
            {
                obj.inventory?.gameObject.AddComponent<Statistics>();
            };

            RecalculateStatsAPI.GetStatCoefficients += (sender, args) =>
            {
                if (sender && sender.inventory)
                {
                    int count = sender.inventory.GetItemCountEffective(def);
                    if (count > 0)
                    {
                        args.healthTotalMult *= 1 + percentHealthBonus * radiantMultiplier;
                    }
                }
            };

            GameEventManager.BeforeTakeDamage += (damageInfo, attackerInfo, victimInfo) =>
            {
                CharacterBody victimBody = victimInfo.body;
                if (victimBody && victimBody.inventory)
                {
                    int count = victimBody.inventory.GetItemCountEffective(def);
                    if (count > 0)
                    {
                        // Cannot reduce damage below 1
                        damageInfo.damage = damageInfo.damage > flatDamageReduction.Value * radiantMultiplier + 1 ? damageInfo.damage - flatDamageReduction.Value * radiantMultiplier : 1;
                    }
                }
            };

            GameEventManager.OnTakeDamage += (damageReport) =>
            {
                CharacterBody vicBody = damageReport.victimBody;
                CharacterBody atkBody = damageReport.attackerBody;

                if (vicBody && vicBody.inventory && atkBody && atkBody.healthComponent)
                {
                    int count = vicBody.inventory.GetItemCountEffective(def);
                    if (count > 0 && !Utilities.OnSameTeam(vicBody, atkBody))
                    {
                        OrbManager.instance.AddOrb(new BrambleOrb(damageReport, radiantMultiplier));
                    }
                }
            };
        }

        public class BrambleOrb : Orb
        {
            private readonly float speed = 60f;

            private readonly float multi;
            private readonly DamageReport damageReport;

            public BrambleOrb(DamageReport report, float radiantMult)
            {
                damageReport = report;
                multi = radiantMult;

                if (report.victimBody && report.attackerBody)
                {
                    origin = report.victimBody ? report.victimBody.corePosition : Vector3.zero;
                    if (report.attackerBody) target = report.attackerBody.mainHurtBox;
                }
            }

            public override void Begin()
            {
                base.duration = base.distanceToTarget / speed;
                EffectData effectData = new()
                {
                    origin = origin,
                    genericFloat = base.duration
                };
                effectData.SetHurtBoxReference(target);
                EffectManager.SpawnEffect(OrbStorageUtility.Get("Prefabs/Effects/OrbEffects/ClayGooOrbEffect"), effectData, transmit: true);
            }

            public override void OnArrival()
            {
                if (damageReport.attackerBody && damageReport.victimBody && damageReport.victimBody.inventory)
                {
                    CharacterBody vicBody = damageReport.victimBody;
                    CharacterBody atkBody = damageReport.attackerBody;

                    int count = damageReport.victimBody.inventory.GetItemCountEffective(BrambleVest.itemDef);
                    DamageInfo brambleProc = new DamageInfo
                    {
                        damage = damageReport.damageInfo.damage * Utilities.GetLinearStacking(BrambleVest.percentReflectDamage * multi, count),
                        damageColorIndex = DamageColorIndex.Poison,
                        damageType = DamageType.Generic,
                        attacker = vicBody.gameObject,
                        crit = vicBody.RollCrit(),
                        inflictor = vicBody.gameObject,
                        procCoefficient = reflectProcCoefficient,
                        procChainMask = new ProcChainMask()
                    };
                    atkBody.healthComponent.TakeDamage(brambleProc);

                    // Damage calculation takes minions into account
                    CharacterBody trackerBody = Utilities.GetMinionOwnershipParentBody(damageReport.victimBody);
                    // Store damage numbers for user flavor
                    Statistics component = vicBody.inventory.GetComponent<Statistics>();
                    if (component) component.DamageReflected += brambleProc.damage;
                }
            }
        }
    }
}
