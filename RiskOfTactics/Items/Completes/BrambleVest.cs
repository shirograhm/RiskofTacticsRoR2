using R2API;
using R2API.Networking;
using R2API.Networking.Interfaces;
using RoR2;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

namespace RiskOfTactics.Items.Completes
{
    class BrambleVest
    {
        public static ItemDef itemDef;

        // Become tankier and reflect a portion of the damage you take.
        public static ConfigurableValue<bool> isEnabled = new(
            "Item: Bramble Vest",
            "Enabled",
            true,
            "Whether or not the item is enabled.",
            new List<string>()
            {
                "ITEM_BRAMBLEVEST_DESC"
            }
        );
        public static ConfigurableValue<float> healthBonus = new(
            "Item: Bramble Vest",
            "Health",
            7f,
            "Percent health bonus when holding this item.",
            new List<string>()
            {
                "ITEM_BRAMBLEVEST_DESC"
            }
        );
        public static ConfigurableValue<int> flatDamageReduction = new(
            "Item: Bramble Vest",
            "Damage Reduction",
            12,
            "Flat damage reduction bonus when holding this item.",
            new List<string>()
            {
                "ITEM_BRAMBLEVEST_DESC"
            }
        );
        public static ConfigurableValue<float> reflectDamage = new(
            "Item: Bramble Vest",
            "Reflect Percent",
            80f,
            "Percent damage reflected back to the attacker when holding this item.",
            new List<string>()
            {
                "ITEM_BRAMBLEVEST_DESC"
            }
        );
        public static ConfigurableValue<float> reflectProcCoefficient = new(
            "Item: Bramble Vest",
            "Reflect Proc Coefficient",
            0.5f,
            "Proc coefficient for the reflected damage hit when holding this item.",
            new List<string>()
            {
                "ITEM_BRAMBLEVEST_DESC"
            }
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
            GenerateItem();

            ItemDisplayRuleDict displayRules = new ItemDisplayRuleDict(null);
            ItemAPI.Add(new CustomItem(itemDef, displayRules));

            NetworkingAPI.RegisterMessageType<Statistics.Sync>();

            Hooks();
        }

        private static void GenerateItem()
        {
            itemDef = ScriptableObject.CreateInstance<ItemDef>();

            itemDef.name = "BRAMBLEVEST";
            itemDef.AutoPopulateTokens();

            Utils.SetItemTier(itemDef, ItemTier.Tier2);

            GameObject prefab = AssetHandler.bundle.LoadAsset<GameObject>("BrambleVest.prefab");
            ModelPanelParameters modelPanelParameters = prefab.AddComponent<ModelPanelParameters>();
            modelPanelParameters.focusPointTransform = prefab.transform;
            modelPanelParameters.cameraPositionTransform = prefab.transform;
            modelPanelParameters.maxDistance = 10f;
            modelPanelParameters.minDistance = 5f;

            itemDef.pickupIconSprite = AssetHandler.bundle.LoadAsset<Sprite>("BrambleVest.png");
            itemDef.pickupModelPrefab = prefab;
            itemDef.canRemove = true;
            itemDef.hidden = false;

            itemDef.tags = new ItemTag[]
            {
                ItemTag.Damage,
                ItemTag.Utility,

                ItemTag.CanBeTemporary
            };
        }

        public static void Hooks()
        {
            CharacterMaster.onStartGlobal += (obj) =>
            {
                obj.inventory?.gameObject.AddComponent<Statistics>();
            };

            RecalculateStatsAPI.GetStatCoefficients += (sender, args) =>
            {
                if (sender && sender.inventory)
                {
                    int count = sender.inventory.GetItemCountEffective(itemDef);
                    if (count > 0)
                    {
                        args.healthTotalMult *= 1 + percentHealthBonus;
                    }
                }
            };

            GenericGameEvents.BeforeTakeDamage += (damageInfo, attackerInfo, victimInfo) =>
            {
                CharacterBody victimBody = victimInfo.body;
                if (victimBody && victimBody.inventory)
                {
                    int count = victimBody.inventory.GetItemCountEffective(itemDef);
                    if (count > 0)
                    {
                        // Cannot reduce damage below 1
                        damageInfo.damage = damageInfo.damage > flatDamageReduction.Value + 1 ? damageInfo.damage - flatDamageReduction.Value : 1;
                    }
                }
            };

            GenericGameEvents.OnTakeDamage += (damageReport) =>
            {
                CharacterBody vicBody = damageReport.victimBody;
                CharacterBody atkBody = damageReport.attackerBody;

                if (vicBody && vicBody.inventory && atkBody && atkBody.healthComponent)
                {
                    int count = vicBody.inventory.GetItemCountEffective(itemDef);
                    if (count > 0 && !Utils.OnSameTeam(vicBody, atkBody))
                    {
                        DamageInfo brambleProc = new DamageInfo
                        {
                            damage = damageReport.damageInfo.damage * Utils.GetLinearStacking(percentReflectDamage, count),
                            damageColorIndex = DamageColorIndex.Poison,
                            damageType = DamageType.Generic,
                            attacker = vicBody.gameObject,
                            crit = vicBody.RollCrit(),
                            inflictor = vicBody.gameObject,
                            procCoefficient = reflectProcCoefficient,
                            procChainMask = new ProcChainMask()
                        };
                        atkBody.healthComponent.TakeDamage(brambleProc);
                        // Store damage numbers for user flavor
                        Statistics component = vicBody.inventory.GetComponent<Statistics>();
                        if (component) component.DamageReflected += brambleProc.damage;
                    }
                }
            };
        }
    }
}
