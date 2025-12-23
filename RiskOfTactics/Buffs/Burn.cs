using R2API;
using R2API.Networking;
using R2API.Networking.Interfaces;
using RoR2;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

namespace RiskOfTactics
{
    class Burn
    {
        public static BuffDef buffDef;

        public static ConfigurableValue<float> burnPerSecond = new(
            "Debuff: Burn",
            "Burn Per Second",
            1f,
            "Percent max HP lost per second for all enemies hit by this effect.",
            new List<string>()
            {
                "BUFF_WOUND_DESC"
            }
        );
        public static ConfigurableValue<float> burnProcCoeff = new(
            "Debuff: Burn",
            "Burn Proc Coefficient",
            0.5f,
            "Proc coefficient for this effect.",
            new List<string>()
            {
                "BUFF_WOUND_DESC"
            }
        );
        public static readonly float percentBurnPerSecond = burnPerSecond.Value / 100f;

        internal static void Init()
        {
            buffDef = Utils.GenerateBuffDef("Burn",
                AssetHandler.bundle.LoadAsset<Sprite>("Burn.png"),
                false, false, true, false
            );
            ContentAddition.AddBuffDef(buffDef);

            Hooks();
        }

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

        public static void Hooks()
        {
            On.RoR2.CharacterBody.FixedUpdate += (orig, self) =>
            {
                if (self && self.healthComponent && self.inventory)
                {
                    Statistics component = self.inventory.GetComponent<Statistics>();
                    if (component && Environment.TickCount - component.LastTick > 1000f && self.GetBuffCount(buffDef) > 0)
                    {
                        DamageInfo burnTick = new DamageInfo
                        {
                            damage = self.healthComponent.fullCombinedHealth * percentBurnPerSecond,
                            damageColorIndex = DamageColorIndex.Luminous,
                            damageType = DamageType.Generic,
                            attacker = null,
                            inflictor = null,
                            crit = false,
                            procCoefficient = burnProcCoeff.Value,
                            procChainMask = new ProcChainMask(),
                            position = self.corePosition
                        };
                        self.healthComponent.TakeDamage(burnTick);

                        component.LastTick = Environment.TickCount;
                    }
                }
                orig(self);
            };

            On.RoR2.HealthComponent.Heal += (orig, self, amount, procChainMask, nonRegen) =>
            {
                CharacterBody body = self.GetComponentInParent<CharacterBody>();
                if (body && body.GetBuffCount(buffDef) > 0)
                {
                    // Cut both regen and heals
                    amount *= percentBurnPerSecond;
                }
                return orig(self, amount, procChainMask, nonRegen);
            };
        }
    }
}
