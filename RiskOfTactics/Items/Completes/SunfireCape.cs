using R2API;
using R2API.Networking;
using R2API.Networking.Interfaces;
using RoR2;
using RoR2.Skills;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

namespace RiskOfTactics
{
    class SunfireCape
    {
        public static ItemDef itemDef;

        // Gain armor and HP. Every 10 seconds, apply Burn and Wound nearby enemies.
        public static ConfigurableValue<bool> isEnabled = new(
            "Item: Sunfire Cape",
            "Enabled",
            true,
            "Whether or not the item is enabled.",
            new List<string>()
            {
                "ITEM_SUNFIRECAPE_DESC"
            }
        );
        public static ConfigurableValue<float> armorBonus = new(
            "Item: Sunfire Cape",
            "Armor",
            15f,
            "Percent armor gained when holding this item.",
            new List<string>()
            {
                "ITEM_SUNFIRECAPE_DESC"
            }
        );
        public static ConfigurableValue<float> flatHealthBonus = new(
            "Item: Sunfire Cape",
            "Flat Health",
            100f,
            "Flat health gained when holding this item.",
            new List<string>()
            {
                "ITEM_SUNFIRECAPE_DESC"
            }
        );
        public static ConfigurableValue<float> healthBonus = new(
            "Item: Sunfire Cape",
            "Percent Health", 
            7f, 
            "Percent health gained when holding this item.", 
            new List<string>()
            {
                "ITEM_SUNFIRECAPE_DESC"
            }
        );
        public static ConfigurableValue<float> debuffTickDuration = new(
            "Item: Sunfire Cape",
            "Debuff Tick",
            10f,
            "Seconds between Burn and Wound reapplication.",
            new List<string>()
            {
                "ITEM_SUNFIRECAPE_DESC"
            }
        );
        public static ConfigurableValue<float> debuffRadius = new(
            "Item: Sunfire Cape",
            "Debuff Radius",
            6f,
            "Radius of the debuff application zone (meters).",
            new List<string>()
            {
                "ITEM_SUNFIRECAPE_DESC"
            }
        );
        private static readonly float percentHealthBonus = healthBonus.Value / 100f;

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

            Hooks();
        }

        private static void GenerateItem()
        {
            itemDef = ScriptableObject.CreateInstance<ItemDef>();

            itemDef.name = "SUNFIRECAPE";
            itemDef.AutoPopulateTokens();

            Utils.SetItemTier(itemDef, ItemTier.Tier3);

            itemDef.pickupIconSprite = AssetHandler.bundle.LoadAsset<Sprite>("SunfireCape.png");
            itemDef.pickupModelPrefab = AssetHandler.bundle.LoadAsset<GameObject>("SunfireCape.prefab");
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
                        args.baseHealthAdd += flatHealthBonus.Value;
                        args.healthMultAdd += percentHealthBonus;
                    }
                }
            };

            On.RoR2.CharacterBody.FixedUpdate += (orig, self) =>
            {
                if (self && self.inventory && self.inventory.GetItemCount(itemDef) > 0)
                {
                    Statistics component = self.inventory.GetComponent<Statistics>();
                    // Check time elapsed 
                    if (component && Environment.TickCount - component.LastTick > debuffTickDuration.Value * 1000)
                    {
                        // Get all enemies nearby
                        HurtBox[] hurtboxes = new SphereSearch
                        {
                            mask = LayerIndex.entityPrecise.mask,
                            origin = self.corePosition,
                            queryTriggerInteraction = QueryTriggerInteraction.Collide,
                            radius = debuffRadius.Value
                        }.RefreshCandidates().FilterCandidatesByDistinctHurtBoxEntities().GetHurtBoxes();

                        foreach (HurtBox h in hurtboxes)
                        {
                            HealthComponent hc = h.healthComponent;
                            if (hc && hc.body && hc.body.teamComponent && hc.body.teamComponent.teamIndex != self.teamComponent.teamIndex)
                            {
                                hc.body.AddBuff(Burn.buffDef);
                                hc.body.AddBuff(Wound.buffDef);
                            }
                        }
                        component.LastTick = Environment.TickCount;
                    }
                }
                orig(self);
            };
        }
    }
}
