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
    class StatikkShiv
    {
        public static ItemDef itemDef;
        public static BuffDef shockBuff;

        // Gain attack speed, flat damage, and cooldown reduction. Every 10 seconds, your next attack deals an additional 18 damage and reduces nearby enemy armor by 10.
        public static ConfigurableValue<bool> isEnabled = new(
            "Item: Statikk Shiv",
            "Enabled",
            true,
            "Whether or not the item is enabled.",
            new List<string>()
            {
                "ITEM_STATIKKSHIV_DESC"
            }
        );
        public static ConfigurableValue<float> attackSpeedBonus = new(
            "Item: Statikk Shiv",
            "Attack Speed",
            25f,
            "Percent attack speed gained when holding this item.",
            new List<string>()
            {
                "ITEM_STATIKKSHIV_DESC"
            }
        );
        public static ConfigurableValue<float> damageBonus = new(
            "Item: Statikk Shiv",
            "Flat Damage",
            8f,
            "Flat damage gained when holding this item.",
            new List<string>()
            {
                "ITEM_STATIKKSHIV_DESC"
            }
        );
        public static ConfigurableValue<float> cooldownReductionBonus = new(
            "Item: Statikk Shiv",
            "Cooldown Reduction",
            15f,
            "Percent cooldown reduction gained when holding this item.",
            new List<string>()
            {
                "ITEM_STATIKKSHIV_DESC"
            }
        );
        public static ConfigurableValue<float> effectCooldown = new(
            "Item: Statikk Shiv",
            "Effect Cooldown",
            5f,
            "Cooldown of this item's effect.",
            new List<string>()
            {
                "ITEM_STATIKKSHIV_DESC"
            }
        );
        public static ConfigurableValue<float> onHitDamage = new(
            "Item: Statikk Shiv",
            "Bonus On-Hit",
            18f,
            "Bonus on-hit damage dealt by this item's effect.",
            new List<string>()
            {
                "ITEM_STATIKKSHIV_DESC"
            }
        );
        public static ConfigurableValue<int> armorReduction = new(
            "Item: Statikk Shiv",
            "Armor Reduction",
            10,
            "Armor reduction applied to enemies hit by this item's effect. MUST BE A MULTIPLE OF 2 (because I'm lazy).",
            new List<string>()
            {
                "ITEM_STATIKKSHIV_DESC"
            }
        );
        private static readonly float percentAttackSpeedBonus = attackSpeedBonus.Value / 100f;
        private static readonly float percentCooldownReductionBonus = cooldownReductionBonus.Value / 100f;

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
            GenerateBuff();

            ItemDisplayRuleDict displayRules = new ItemDisplayRuleDict(null);
            ItemAPI.Add(new CustomItem(itemDef, displayRules));

            ContentAddition.AddBuffDef(shockBuff);

            Hooks();
        }

        private static void GenerateItem()
        {
            itemDef = ScriptableObject.CreateInstance<ItemDef>();

            itemDef.name = "STATIKKSHIV";
            itemDef.AutoPopulateTokens();

            Utils.SetItemTier(itemDef, ItemTier.Tier3);

            itemDef.pickupIconSprite = AssetHandler.bundle.LoadAsset<Sprite>("StatikkShiv.png");
            itemDef.pickupModelPrefab = AssetHandler.bundle.LoadAsset<GameObject>("StatikkShiv.prefab");
            itemDef.canRemove = true;
            itemDef.hidden = false;

            itemDef.tags = new ItemTag[]
            {
                ItemTag.Damage
            };
        }

        private static void GenerateBuff()
        {
            shockBuff = ScriptableObject.CreateInstance<BuffDef>();

            shockBuff.name = "Shock";
            shockBuff.iconSprite = AssetHandler.bundle.LoadAsset<Sprite>("Shock.png");
            shockBuff.canStack = false;
            shockBuff.isHidden = false;
            shockBuff.isDebuff = false;
            shockBuff.isCooldown = false;
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

                        args.attackSpeedMultAdd += percentAttackSpeedBonus;
                        args.baseDamageAdd += damageBonus.Value;
                        args.cooldownMultAdd -= percentCooldownReductionBonus;
                    }
                }
            };

            Stage.onStageStartGlobal += (stage) =>
            {
                foreach (NetworkUser user in NetworkUser.readOnlyInstancesList)
                {
                    CharacterMaster master = user.masterController.master ?? user.master;
                    if (master && master.inventory && master.inventory.GetItemCount(itemDef) > 0)
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

                if (self && self.inventory)
                {
                    int itemCount = self.inventory.GetItemCount(itemDef);
                    if (itemCount > 0)
                    {
                        Statistics component = self.inventory.GetComponent<Statistics>();

                        if (self.GetBuffCount(shockBuff) == 0 && component && Environment.TickCount - component.LastTick > effectCooldown.Value * 1000)
                            self.AddBuff(shockBuff);
                    }
                }
            };

            GenericGameEvents.OnTakeDamage += (damageReport) =>
                {
                    CharacterBody vicBody = damageReport.victimBody;
                    CharacterBody atkBody = damageReport.attackerBody;

                    if (vicBody && atkBody && atkBody.inventory)
                    {
                        Statistics component = atkBody.inventory.GetComponent<Statistics>();
                        int atkCount = atkBody.inventory.GetItemCount(itemDef);
                        if (component && atkCount > 0 && vicBody.teamComponent.teamIndex != atkBody.teamComponent.teamIndex)
                        {
                            // Get area of enemies impacted
                            HurtBox[] hurtboxes = new SphereSearch
                            {
                                mask = LayerIndex.entityPrecise.mask,
                                origin = vicBody.corePosition,
                                queryTriggerInteraction = QueryTriggerInteraction.Collide,
                                radius = 20f
                            }.RefreshCandidates().FilterCandidatesByDistinctHurtBoxEntities().GetHurtBoxes();

                            foreach (HurtBox hb in hurtboxes)
                            {
                                CharacterBody parent = hb.healthComponent.body;

                                if (parent && parent.teamComponent && parent.teamComponent.teamIndex != atkBody.teamComponent.teamIndex)
                                {
                                    for (int i = 0; i < armorReduction / 2; i++)
                                    {
                                        parent.AddBuff(DLC1Content.Buffs.PermanentDebuff);
                                    }
                                }
                            }
                            // Reset effect cooldown timer and remove the shock buff
                            atkBody.RemoveBuff(shockBuff);
                            component.LastTick = Environment.TickCount;
                        }
                    }
                };
        }
    }
}
