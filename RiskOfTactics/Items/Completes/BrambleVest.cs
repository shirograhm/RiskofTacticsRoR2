using R2API;
using R2API.Networking;
using R2API.Networking.Interfaces;
using RoR2;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

namespace RiskOfTactics
{
    class BrambleVest
    {
        public static ItemDef itemDef;

        // +65 armor - Gain 7% max HP. Reduce all incoming damage by 10, and reflect 80% of the damage you take.
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
        public static ConfigurableValue<float> armorBonus = new(
            "Item: Bramble Vest",
            "Armor",
            30f,
            "Armor bonus when holding this item.",
            new List<string>()
            {
                "ITEM_BRAMBLEVEST_DESC"
            }
        );
        public static ConfigurableValue<float> healthBonus = new(
            "Item: Bramble Vest",
            "Percent Health",
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
            10,
            "Flat damage reduction bonus when holding this item.",
            new List<string>()
            {
                "ITEM_BRAMBLEVEST_DESC"
            }
        );
        public static ConfigurableValue<float> reflectDamage = new(
            "Item: Bramble Vest",
            "Reflect Damage",
            80f,
            "Percent damage reflected back to the attacker when holding this item.",
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

            Hooks();
        }

        private static void GenerateItem()
        {
            itemDef = ScriptableObject.CreateInstance<ItemDef>();

            itemDef.name = "BRAMBLEVEST";
            itemDef.AutoPopulateTokens();

            Utils.SetItemTier(itemDef, ItemTier.Tier3);

            itemDef.pickupIconSprite = AssetHandler.bundle.LoadAsset<Sprite>("BrambleVest.png");
            itemDef.pickupModelPrefab = AssetHandler.bundle.LoadAsset<GameObject>("BrambleVest.prefab");
            itemDef.canRemove = true;
            itemDef.hidden = false;

            itemDef.tags = new ItemTag[]
            {
                ItemTag.Damage,
                ItemTag.Utility
            };
        }

        public static void Hooks()
        {
            RecalculateStatsAPI.GetStatCoefficients += (sender, args) =>
            {
                if (sender && sender.inventory)
                {
                    int count = sender.inventory.GetItemCount(itemDef);
                    if (count > 0)
                    {
                        args.armorAdd += armorBonus.Value;
                        args.healthMultAdd += percentHealthBonus;
                    }
                }
            };

            GenericGameEvents.BeforeTakeDamage += (damageInfo, attackerInfo, victimInfo) =>
            {
                CharacterBody victimBody = victimInfo.body;
                if (victimBody && victimBody.inventory)
                {
                    int count = victimBody.inventory.GetItemCount(itemDef);
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

                if (vicBody && atkBody && vicBody.inventory)
                {
                    if (vicBody.inventory.GetItemCount(itemDef) > 0 && vicBody.teamComponent.teamIndex != atkBody.teamComponent.teamIndex)
                    {
                        DamageInfo brambleProc = new DamageInfo
                        {
                            damage = percentReflectDamage * damageReport.damageInfo.damage,
                            damageColorIndex = DamageColorIndex.Poison,
                            damageType = DamageType.Generic,
                            attacker = vicBody.gameObject,
                            crit = vicBody.RollCrit(),
                            inflictor = vicBody.gameObject,
                            procCoefficient = 0.0f,
                            procChainMask = new ProcChainMask()
                        };
                        vicBody.healthComponent.TakeDamage(brambleProc);
                        // Store damage numbers for user flavor
                        Statistics component = vicBody.inventory.GetComponent<Statistics>();
                        if (component) component.DamageReflected += brambleProc.damage;
                    }
                }
            };
        }
    }
}
