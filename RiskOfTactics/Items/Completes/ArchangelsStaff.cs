using R2API;
using R2API.Networking;
using R2API.Networking.Interfaces;
using RoR2;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

namespace RiskOfTactics.Items.Completes
{
    class ArchangelsStaff
    {
        public static ItemDef itemDef;
        public static BuffDef foresightBuff;

        // During the teleporter event, periodically gain BASE damage.
        public static ConfigurableValue<bool> isEnabled = new(
            "Item: Archangels Staff",
            "Enabled",
            true,
            "Whether or not the item is enabled.",
            new List<string>()
            {
                "ITEM_ARCHANGELSSTAFF_DESC"
            }
        );
        public static ConfigurableValue<float> flatDamagePerTick = new(
            "Item: Archangels Staff",
            "Flat Damage Per Tick",
            0.5f,
            "Flat damage gained per item proc.",
            new List<string>()
            {
                "ITEM_ARCHANGELSSTAFF_DESC"
            }
        );
        public static ConfigurableValue<float> tickDuration = new(
            "Item: Archangels Staff",
            "Tick Duration",
            5f,
            "Number of seconds between item procs.",
            new List<string>()
            {
                "ITEM_ARCHANGELSSTAFF_DESC"
            }
        );

        public class Statistics : MonoBehaviour
        {
            private float _lastTick;
            public float LastTick
            {
                get { return _lastTick; }
                set
                {
                    _lastTick = value;
                    if (NetworkServer.active)
                    {
                        new Sync(gameObject.GetComponent<NetworkIdentity>().netId, value).Send(NetworkDestination.Clients);
                    }
                }
            }

            public class Sync : INetMessage
            {
                NetworkInstanceId objId;
                float lastTick;

                public Sync()
                {
                }

                public Sync(NetworkInstanceId objId, float tick)
                {
                    this.objId = objId;
                    lastTick = tick;
                }

                public void Deserialize(NetworkReader reader)
                {
                    objId = reader.ReadNetworkId();
                    lastTick = reader.ReadSingle();
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
                            component.LastTick = lastTick;
                        }
                    }
                }

                public void Serialize(NetworkWriter writer)
                {
                    writer.Write(objId);
                    writer.Write(lastTick);

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

            foresightBuff = Utils.GenerateBuffDef("Foresight", AssetHandler.bundle.LoadAsset<Sprite>("Foresight.png"), true, false, false, false);
            ContentAddition.AddBuffDef(foresightBuff);

            Hooks();
        }

        private static void GenerateItem()
        {
            itemDef = ScriptableObject.CreateInstance<ItemDef>();

            itemDef.name = "ARCHANGELSSTAFF";
            itemDef.AutoPopulateTokens();

            Utils.SetItemTier(itemDef, ItemTier.Tier2);

            GameObject prefab = AssetHandler.bundle.LoadAsset<GameObject>("ArchangelsStaff.prefab");
            ModelPanelParameters modelPanelParameters = prefab.AddComponent<ModelPanelParameters>();
            modelPanelParameters.focusPointTransform = prefab.transform;
            modelPanelParameters.cameraPositionTransform = prefab.transform;
            modelPanelParameters.maxDistance = 10f;
            modelPanelParameters.minDistance = 5f;

            itemDef.pickupIconSprite = AssetHandler.bundle.LoadAsset<Sprite>("ArchangelsStaff.png");
            itemDef.pickupModelPrefab = prefab;
            itemDef.canRemove = true;
            itemDef.hidden = false;

            itemDef.tags = new ItemTag[]
            {
                ItemTag.Damage,

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
                        int buffCount = sender.GetBuffCount(foresightBuff);

                        args.baseDamageAdd += buffCount * flatDamagePerTick.Value;
                    }
                }
            };

            Stage.onStageStartGlobal += (stage) =>
            {
                foreach (NetworkUser user in NetworkUser.readOnlyInstancesList)
                {
                    CharacterMaster master = user.masterController.master ?? user.master;
                    if (master && master.inventory && master.inventory.GetItemCountEffective(itemDef) > 0)
                    {
                        Statistics component = master.inventory.GetComponent<Statistics>();
                        if (component)
                        {
                            component.LastTick = Environment.TickCount;
                        }
                    }
                }
            };

            On.RoR2.CharacterBody.FixedUpdate += (orig, self) =>
            {
                orig(self);

                foreach (HoldoutZoneController hzc in InstanceTracker.GetInstancesList<HoldoutZoneController>())
                {
                    if (self && self.inventory)
                    {
                        int itemCount = self.inventory.GetItemCountEffective(itemDef);

                        if (itemCount > 0 && hzc.isActiveAndEnabled)
                        {
                            Statistics component = self.inventory.GetComponent<Statistics>();
                            // Check time elapsed 
                            if (component && Environment.TickCount - component.LastTick > tickDuration.Value * 1000)
                            {
                                self.AddBuff(foresightBuff);
                                component.LastTick = Environment.TickCount;
                            }
                        }
                    }
                }
            };
        }
    }
}
