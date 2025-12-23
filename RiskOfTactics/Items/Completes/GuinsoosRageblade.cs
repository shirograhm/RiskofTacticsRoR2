using R2API;
using R2API.Networking;
using R2API.Networking.Interfaces;
using RoR2;
using System.Collections.Generic;
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
        public static ConfigurableValue<float> attackSpeedOnHit = new(
            "Item: Guinsoos Rageblade",
            "Attack Speed On-Hit",
            3f,
            "Percent attack speed gained on-hit.",
            new List<string>()
            {
                "ITEM_GUINSOOSRAGEBLADE_DESC"
            }
        );
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

            GameObject prefab = AssetHandler.bundle.LoadAsset<GameObject>("GuinsoosRageblade.prefab");
            ModelPanelParameters modelPanelParameters = prefab.AddComponent<ModelPanelParameters>();
            modelPanelParameters.focusPointTransform = prefab.transform;
            modelPanelParameters.cameraPositionTransform = prefab.transform;
            modelPanelParameters.maxDistance = 10f;
            modelPanelParameters.minDistance = 5f;

            itemDef.pickupIconSprite = AssetHandler.bundle.LoadAsset<Sprite>("GuinsoosRageblade.png");
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

            On.RoR2.CharacterBody.FixedUpdate += (orig, self) =>
            {
                orig(self);

                if (self && self.HasBuff(wrathBuff) && self.inventory)
                {
                    if (self.inventory.GetItemCountEffective(itemDef) == 0)
                    {
                        self.SetBuffCount(wrathBuff.buffIndex, 0);
                    }
                }
            };

            RecalculateStatsAPI.GetStatCoefficients += (sender, args) =>
            {
                if (sender && sender.inventory)
                {
                    int count = sender.inventory.GetItemCountEffective(itemDef);
                    int buffCount = sender.GetBuffCount(wrathBuff);
                    if (buffCount > 0)
                    {
                        args.attackSpeedMultAdd += buffCount * Utils.GetHyperbolicStacking(percentAttackSpeedOnHit, count);
                    }
                }
            };

            GenericGameEvents.OnTakeDamage += (damageReport) =>
            {
                CharacterBody vicBody = damageReport.victimBody;
                CharacterBody atkBody = damageReport.attackerBody;

                if (vicBody && atkBody && atkBody.inventory)
                {
                    if (atkBody.inventory.GetItemCountEffective(itemDef) > 0 && vicBody.healthComponent && !Utils.OnSameTeam(vicBody, atkBody))
                    {
                        Statistics component = atkBody.inventory.GetComponent<Statistics>();
                        if (component && vicBody.gameObject.Equals(component.LastTarget))
                        {
                            atkBody.AddBuff(wrathBuff);
                        }
                        else
                        {
                            atkBody.SetBuffCount(wrathBuff.buffIndex, 1);
                            component.LastTarget = vicBody.gameObject;
                        }
                    }
                }
            };
        }
    }
}
