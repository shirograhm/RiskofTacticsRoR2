using R2API;
using R2API.Networking;
using R2API.Networking.Interfaces;
using RiskOfTactics.Managers;
using RoR2;
using System;
using UnityEngine;
using UnityEngine.Networking;

namespace RiskOfTactics.Content.Items.Completes
{
    class Quicksilver
    {
        public static ItemDef itemDef;
        public static BuffDef flowBuff;
        public static BuffDef cleanseBuff;

        public static GameObject ccShieldPrefab;

        public static ItemDef radiantDef;

        // When the teleporter is activated, gain immunity to crowd control for a duration. During this time, gain attack speed every second.
        public static ConfigurableValue<bool> isEnabled = new(
            "Item: Quicksilver",
            "Enabled",
            true,
            "Whether or not the item is enabled.",
            ["ITEM_ROT_QUICKSILVER_DESC"]
        );
        public static ConfigurableValue<float> ccImmunityDuration = new(
            "Item: Quicksilver",
            "CC Immunity Duration",
            30f,
            "Number of seconds immune to crowd control once the teleporter event starts.",
            ["ITEM_ROT_QUICKSILVER_DESC"],
            true
        );
        public static ConfigurableValue<float> ccImmunityDurationExtraStacks = new(
            "Item: Quicksilver",
            "CC Immunity Duration Extra Stacks",
            30f,
            "Number of seconds immune to crowd control once the teleporter event starts.",
            ["ITEM_ROT_QUICKSILVER_DESC"],
            false
        );
        public static ConfigurableValue<float> attackSpeedPerBuff = new(
            "Item: Quicksilver",
            "Attack Speed",
            1f,
            "Attack speed gained per second while immune to CC.",
            ["ITEM_ROT_QUICKSILVER_DESC"],
            true
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
            itemDef = ItemManager.GenerateItem("Quicksilver", [ItemTag.Damage, ItemTag.Utility, ItemTag.CanBeTemporary], ItemManager.TacticTier.Normal);
            radiantDef = ItemManager.GenerateItem("Radiant_Quicksilver", [ItemTag.Damage, ItemTag.Utility, ItemTag.CanBeTemporary], ItemManager.TacticTier.Radiant);

            NetworkingAPI.RegisterMessageType<Statistics.Sync>();

            flowBuff = Utilities.GenerateBuffDef("Flow", AssetManager.bundle.LoadAsset<Sprite>("Flow.png"), true, false, false, false);
            ContentAddition.AddBuffDef(flowBuff);
            cleanseBuff = Utilities.GenerateBuffDef("Cleanse", AssetManager.bundle.LoadAsset<Sprite>("Cleanse.png"), false, false, false, true);
            ContentAddition.AddBuffDef(cleanseBuff);

            ccShieldPrefab = LegacyResourcesAPI.LoadAsync<GameObject>("Prefabs/TemporaryVisualEffects/BearVoidEffect").WaitForCompletion();

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
                    int buffCount = sender.GetBuffCount(flowBuff);
                    if (buffCount > 0)
                        args.attackSpeedMultAdd += buffCount * percentAttackSpeedPerBuff * radiantMultiplier;
                }
            };

            Stage.onStageStartGlobal += (stage) =>
            {
                foreach (NetworkUser user in NetworkUser.readOnlyInstancesList)
                {
                    CharacterMaster master = user.masterController.master ?? user.master;
                    if (master && master.inventory && master.inventory.GetItemCountEffective(def) > 0)
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
                        int itemCount = self.inventory.GetItemCountEffective(def);

                        if (itemCount > 0 && hzc.isActiveAndEnabled)
                        {
                            if (self.GetBuffCount(flowBuff) == 0 && self.GetBuffCount(cleanseBuff) == 0)
                                self.AddTimedBuff(cleanseBuff, Utilities.GetLinearStacking(ccImmunityDuration.Value * radiantMultiplier, ccImmunityDurationExtraStacks.Value, itemCount));

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

            GameEventManager.BeforeTakeDamage += (damageInfo, attackerInfo, victimInfo) =>
            {
                // CC immunity
                if (victimInfo.body && victimInfo.body.HasBuff(cleanseBuff))
                {
                    damageInfo.force = Vector3.zero;
                }
            };

            On.RoR2.CharacterBody.UpdateAllTemporaryVisualEffects += (orig, self) =>
            {
                orig(self);

                TemporaryVisualEffect ccShieldEffectInstance = new() { };
                UpdateSingleTemporaryVisualEffect(ref ccShieldEffectInstance, ccShieldPrefab, self, self.HasBuff(cleanseBuff));
            };
        }

        private static void UpdateSingleTemporaryVisualEffect(ref TemporaryVisualEffect tempEffect, GameObject tempEffectPrefab, CharacterBody userBody, bool active, string childLocatorOverride = "")
        {
            if (tempEffect != null != active)
            {
                if (active)
                {
                    if (tempEffectPrefab)
                    {
                        GameObject gameObject = UnityEngine.Object.Instantiate(tempEffectPrefab, userBody.corePosition, Quaternion.identity);
                        tempEffect = gameObject.GetComponent<TemporaryVisualEffect>();
                        tempEffect.parentTransform = userBody.coreTransform;
                        tempEffect.visualState = TemporaryVisualEffect.VisualState.Enter;
                        tempEffect.healthComponent = userBody.healthComponent;
                        tempEffect.radius = 4f;
                        LocalCameraEffect component = gameObject.GetComponent<LocalCameraEffect>();
                        if (component)
                        {
                            component.targetCharacter = userBody.gameObject;
                        }
                        if (!string.IsNullOrEmpty(childLocatorOverride))
                        {
                            ChildLocator childLocator = userBody.modelLocator?.modelTransform?.GetComponent<ChildLocator>();
                            if ((bool)childLocator)
                            {
                                Transform transform = childLocator.FindChild(childLocatorOverride);
                                if ((bool)transform)
                                {
                                    tempEffect.parentTransform = transform;
                                }
                            }
                        }
                    }
                    else
                    {
                        Debug.LogError("Can't instantiate null temporary visual effect");
                    }
                }
                else
                {
                    tempEffect.visualState = TemporaryVisualEffect.VisualState.Exit;
                }
            }
        }
    }
}
