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
    class GuinsoosRageblade
    {
        public static ItemDef itemDef;
        public static BuffDef wrathBuff;

        public static ConfigurableValue<bool> isEnabled = new(
            "Item: Guinsoos Rageblade",
            "Enabled",
            true,
            "Whether or not the item is enabled.",
            new List<string>()
            {
                "ITEM_GUINSOOSRAGEBLADE_DESC"
            }
        );
        public static ConfigurableValue<float> flatDamageBonus = new(
            "Item: Guinsoos Rageblade",
            "Flat Damage",
            10f,
            "Flat damage bonus when holding this item.",
            new List<string>()
            {
                "ITEM_GUINSOOSRAGEBLADE_DESC"
            }
        );
        public static ConfigurableValue<float> attackSpeedBonus = new(
            "Item: Guinsoos Rageblade",
            "Attack Speed",
            15f,
            "Percent attack speed bonus when holding this item.",
            new List<string>()
            {
                "ITEM_GUINSOOSRAGEBLADE_DESC"
            }
        );
        public static ConfigurableValue<float> attackSpeedOnHit = new(
            "Item: Guinsoos Rageblade",
            "Attack Speed On-Hit",
            2f,
            "Percent attack speed gained on-hit.",
            new List<string>()
            {
                "ITEM_GUINSOOSRAGEBLADE_DESC"
            }
        );
        public static readonly float percentAttackSpeedBonus = attackSpeedBonus.Value / 100f;
        public static readonly float percentAttackSpeedOnHit = attackSpeedOnHit.Value / 100f;

        public class Statistics : MonoBehaviour
        {
            private GameObject _lastTarget;
            public GameObject LastTarget
            {
                get { return _lastTarget; }
                set
                {
                    _lastTarget = value;
                    if (NetworkServer.active)
                    {
                        new Sync(gameObject.GetComponent<NetworkIdentity>().netId, value).Send(NetworkDestination.Clients);
                    }
                }
            }

            public class Sync : INetMessage
            {
                NetworkInstanceId objId;
                GameObject lastTarget;

                public Sync()
                {
                }

                public Sync(NetworkInstanceId objId, GameObject tick)
                {
                    this.objId = objId;
                    lastTarget = tick;
                }

                public void Deserialize(NetworkReader reader)
                {
                    objId = reader.ReadNetworkId();
                    lastTarget = reader.ReadGameObject();
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
                            component.LastTarget = lastTarget;
                        }
                    }
                }

                public void Serialize(NetworkWriter writer)
                {
                    writer.Write(objId);
                    writer.Write(lastTarget);

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

            wrathBuff = Utils.GenerateBuffDef("Wrath", AssetHandler.bundle.LoadAsset<Sprite>("Wrath.png"), true, false, false, false);
            ContentAddition.AddBuffDef(wrathBuff);

            Hooks();
        }

        private static void GenerateItem()
        {
            itemDef = ScriptableObject.CreateInstance<ItemDef>();

            itemDef.name = "GUINSOOSRAGEBLADE";
            itemDef.AutoPopulateTokens();

            Utils.SetItemTier(itemDef, ItemTier.Tier3);

            itemDef.pickupIconSprite = AssetHandler.bundle.LoadAsset<Sprite>("GuinsoosRageblade.png");
            itemDef.pickupModelPrefab = AssetHandler.bundle.LoadAsset<GameObject>("GuinsoosRageblade.prefab");
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
            CharacterMaster.onStartGlobal += (obj) =>
            {
                obj.inventory?.gameObject.AddComponent<Statistics>();
            };

            RecalculateStatsAPI.GetStatCoefficients += (sender, args) =>
            {
                if (sender && sender.inventory)
                {
                    int count = sender.inventory.GetItemCount(itemDef);
                    if (count > 0)
                    {
                        args.baseDamageAdd += flatDamageBonus.Value;
                        args.attackSpeedMultAdd += percentAttackSpeedBonus;
                    }

                    int buffCount = sender.GetBuffCount(wrathBuff);
                    if (buffCount > 0)
                    {
                        args.attackSpeedMultAdd += percentAttackSpeedOnHit * buffCount;
                    }
                }
            };

            GenericGameEvents.OnTakeDamage += (damageReport) =>
            {
                CharacterBody vicBody = damageReport.victimBody;
                CharacterBody atkBody = damageReport.attackerBody;

                if (vicBody && atkBody && atkBody.inventory)
                {
                    if (atkBody.inventory.GetItemCount(itemDef) > 0 && vicBody.teamComponent.teamIndex != atkBody.teamComponent.teamIndex)
                    {
                        Statistics component = atkBody.inventory.GetComponent<Statistics>();
                        if (component.LastTarget = vicBody.gameObject)
                        {
                            atkBody.AddBuff(wrathBuff);
                        }
                        else
                        {
                            atkBody.SetBuffCount(wrathBuff.buffIndex, 0);
                            component.LastTarget = vicBody.gameObject;
                        }
                    }
                }
            };
        }
    }
}
