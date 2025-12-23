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
    class Quicksilver
    {
        public static ItemDef itemDef;
        public static BuffDef flowBuff;
        public static BuffDef cleanseBuff;

        // When the teleporter is activated, gain immunity to crowd control for a duration. During this time, gain attack speed every second.
        public static ConfigurableValue<bool> isEnabled = new(
            "Item: Quicksilver",
            "Enabled",
            true,
            "Whether or not the item is enabled.",
            new List<string>()
            {
                "ITEM_QUICKSILVER_DESC"
            }
        );
        public static ConfigurableValue<float> ccImmunityDuration = new(
            "Item: Quicksilver",
            "CC Immunity Duration",
            30f,
            "Number of seconds immune to crowd control once the teleporter event starts.",
            new List<string>()
            {
                "ITEM_QUICKSILVER_DESC"
            }
        );
        public static ConfigurableValue<float> ccImmunityDurationExtraStacks = new(
            "Item: Quicksilver",
            "CC Immunity Duration Extra Stacks",
            30f,
            "Number of seconds immune to crowd control once the teleporter event starts.",
            new List<string>()
            {
                "ITEM_QUICKSILVER_DESC"
            }
        );
        public static ConfigurableValue<float> attackSpeedPerBuff = new(
            "Item: Quicksilver",
            "Attack Speed",
            1f,
            "Attack speed gained per second while immune to CC.",
            new List<string>()
            {
                "ITEM_QUICKSILVER_DESC"
            }
        );
        private static readonly float percentAttackSpeedPerBuff = attackSpeedPerBuff.Value / 100f;

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

            flowBuff = Utils.GenerateBuffDef("Flow", AssetHandler.bundle.LoadAsset<Sprite>("Flow.png"), true, false, false, false);
            ContentAddition.AddBuffDef(flowBuff);
            cleanseBuff = Utils.GenerateBuffDef("Cleanse", AssetHandler.bundle.LoadAsset<Sprite>("flowBuff.png"), false, false, false, true);
            ContentAddition.AddBuffDef(cleanseBuff);

            Hooks();
        }

        private static void GenerateItem()
        {
            itemDef = ScriptableObject.CreateInstance<ItemDef>();

            itemDef.name = "QUICKSILVER";
            itemDef.AutoPopulateTokens();

            Utils.SetItemTier(itemDef, ItemTier.Tier2);

            GameObject prefab = AssetHandler.bundle.LoadAsset<GameObject>("Quicksilver.prefab");
            ModelPanelParameters modelPanelParameters = prefab.AddComponent<ModelPanelParameters>();
            modelPanelParameters.focusPointTransform = prefab.transform;
            modelPanelParameters.cameraPositionTransform = prefab.transform;
            modelPanelParameters.maxDistance = 10f;
            modelPanelParameters.minDistance = 5f;

            itemDef.pickupIconSprite = AssetHandler.bundle.LoadAsset<Sprite>("Quicksilver.png");
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
                    int buffCount = sender.GetBuffCount(flowBuff);
                    if (buffCount > 0)
                        args.attackSpeedMultAdd += buffCount * percentAttackSpeedPerBuff;
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
                            if (self.GetBuffCount(flowBuff) == 0 && self.GetBuffCount(cleanseBuff) == 0)
                                self.AddTimedBuff(cleanseBuff, Utils.GetLinearStacking(ccImmunityDuration.Value, ccImmunityDurationExtraStacks.Value, itemCount));

                            if (self.GetBuffCount(cleanseBuff) > 0)
                            {
                                Statistics component = self.inventory.GetComponent<Statistics>();
                                // Check time elapsed 
                                if (component && Environment.TickCount - component.LastTick > 1000)
                                {
                                    self.AddBuff(flowBuff);
                                    component.LastTick = Environment.TickCount;
                                }
                            }
                        }
                    }
                }
            };

            GenericGameEvents.BeforeTakeDamage += (damageInfo, attackerInfo, victimInfo) =>
            {
                // CC immunity
                if (victimInfo.body && victimInfo.body.GetBuffCount(cleanseBuff) > 0)
                {
                    damageInfo.force = Vector3.zero;
                }
            };
        }
    }
}
