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
    class DragonsClaw
    {
        public static ItemDef itemDef;

        public static ItemDef radiantDef;

        // Gain health. Periodically heal for a portion of your max HP.
        public static ConfigurableValue<bool> isEnabled = new(
            "Item: Dragons Claw",
            "Enabled",
            true,
            "Whether or not the item is enabled.",
            ["ITEM_ROT_DRAGONSCLAW_DESC"]
        );
        public static ConfigurableValue<float> maxHealthBonus = new(
            "Item: Dragons Claw",
            "Percent Health",
            9f,
            "Percent max health bonus when holding this item.",
            ["ITEM_ROT_DRAGONSCLAW_DESC"],
            true
        );
        public static ConfigurableValue<float> maxHealthBonusExtraStacks = new(
            "Item: Dragons Claw",
            "Percent Health Per Stack",
            9f,
            "Percent max health bonus when holding extra stacks of this item.",
            ["ITEM_ROT_DRAGONSCLAW_DESC"],
            true
        );
        public static ConfigurableValue<float> healingPerTick = new(
            "Item: Dragons Claw",
            "Healing Per Tick",
            9f,
            "Percent max health healing per item proc.",
            ["ITEM_ROT_DRAGONSCLAW_DESC"],
            true
        );
        public static ConfigurableValue<float> tickDuration = new(
            "Item: Dragons Claw",
            "Tick Duration",
            9f,
            "Number of seconds between item procs.",
            ["ITEM_ROT_DRAGONSCLAW_DESC"],
            false
        );
        public static readonly float percentHealingPerTick = healingPerTick.Value / 100f;
        public static readonly float percentMaxHealthBonus = maxHealthBonus.Value / 100f;
        public static readonly float percentMaxHealthBonusExtraStacks = maxHealthBonusExtraStacks.Value / 100f;

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
            itemDef = ItemManager.GenerateItem("DragonsClaw", [ItemTag.Healing, ItemTag.Utility, ItemTag.CanBeTemporary], ItemManager.TacticTier.Normal);
            radiantDef = ItemManager.GenerateItem("Radiant_DragonsClaw", [ItemTag.Healing, ItemTag.Utility, ItemTag.CanBeTemporary], ItemManager.TacticTier.Radiant);

            NetworkingAPI.RegisterMessageType<Statistics.Sync>();

            Utilities.RegisterRadiantUpgrade(itemDef, radiantDef);

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
                    int count = sender.inventory.GetItemCountEffective(def);
                    if (count > 0)
                    {
                        args.healthTotalMult *= 1 + Utilities.GetLinearStacking(percentMaxHealthBonus * radiantMultiplier, percentMaxHealthBonusExtraStacks * radiantMultiplier, count);
                    }
                }
            };

            Stage.onStageStartGlobal += (stage) =>
            {
                foreach (NetworkUser user in NetworkUser.readOnlyInstancesList)
                {
                    CharacterMaster master = user.masterController?.master ?? user.master;
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

                if (self && self.inventory)
                {
                    int itemCount = self.inventory.GetItemCountEffective(def);
                    if (itemCount > 0)
                    {
                        Statistics component = self.inventory.GetComponent<Statistics>();
                        // Check time elapsed 
                        if (component && Environment.TickCount - component.LastTick > tickDuration * 1000)
                        {
                            self.healthComponent.Heal(self.healthComponent.fullHealth * percentHealingPerTick * radiantMultiplier, new ProcChainMask());
                            component.LastTick = Environment.TickCount;

                            Utilities.SpawnHealEffect(self);
                        }
                    }
                }
            };
        }
    }
}
