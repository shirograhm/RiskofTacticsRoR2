using R2API.Networking;
using R2API.Networking.Interfaces;
using RiskOfTactics.Managers;
using RoR2;
using RoR2.Orbs;
using UnityEngine;
using UnityEngine.Networking;

namespace RiskOfTactics.Content.Items.Completes
{
    public class HextechGunblade
    {
        public static ItemDef itemDef;
        public static BuffDef afterKillHealing;

        public static ItemDef radiantDef;

        // On-kill, heal your lowest ally for a portion of their max health.
        public static ConfigurableValue<bool> isEnabled = new(
            "Item: Hextech Gunblade",
            "Enabled",
            true,
            "Whether or not the item is enabled.",
            ["ITEM_ROT_HEXTECHGUNBLADE_DESC"]
        );
        public static ConfigurableValue<float> healingOnDamage = new(
            "Item: Hextech Gunblade",
            "Percent Healing",
            8f,
            "Percent of all special damage dealt returned as healing.",
            ["ITEM_ROT_HEXTECHGUNBLADE_DESC"],
            true
        );
        public static ConfigurableValue<float> healingOnDamageExtraStacks = new(
            "Item: Hextech Gunblade",
            "Percent Healing Extra Stacks",
            8f,
            "Percent of all special damage dealt returned as healing.",
            ["ITEM_ROT_HEXTECHGUNBLADE_DESC"],
            true
        );
        public static readonly float percentHealingOnDamage = healingOnDamage.Value / 100f;
        public static readonly float percentHealingOnDamageExtraStacks = healingOnDamageExtraStacks.Value / 100f;

        public class Statistics : MonoBehaviour
        {
            private float _damageHealed;
            public float DamageHealed
            {
                get { return _damageHealed; }
                set
                {
                    _damageHealed = value;
                    if (NetworkServer.active)
                    {
                        new Sync(gameObject.GetComponent<NetworkIdentity>().netId, value).Send(NetworkDestination.Clients);
                    }
                }
            }

            public class Sync : INetMessage
            {
                NetworkInstanceId objId;
                float damageHealed;

                public Sync()
                {
                }

                public Sync(NetworkInstanceId objId, float damage)
                {
                    this.objId = objId;
                    damageHealed = damage;
                }

                public void Deserialize(NetworkReader reader)
                {
                    objId = reader.ReadNetworkId();
                    damageHealed = reader.ReadSingle();
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
                            component.DamageHealed = damageHealed;
                        }
                    }
                }

                public void Serialize(NetworkWriter writer)
                {
                    writer.Write(objId);
                    writer.Write(damageHealed);

                    writer.FinishMessage();
                }
            }
        }

        internal static void Init()
        {
            // Normal Variant
            itemDef = ItemManager.GenerateItem("HextechGunblade", [ItemTag.Damage, ItemTag.Utility, ItemTag.CanBeTemporary], ItemManager.TacticTier.Normal);
            radiantDef = ItemManager.GenerateItem("Radiant_HextechGunblade", [ItemTag.Damage, ItemTag.Utility, ItemTag.CanBeTemporary], ItemManager.TacticTier.Radiant);

            NetworkingAPI.RegisterMessageType<Statistics.Sync>();

            if (ConfigManager.Scaling.useRadiantAutoConversion) Utilities.RegisterRadiantUpgrade(itemDef, radiantDef);

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

            GlobalEventManager.onCharacterDeathGlobal += (damageReport) =>
            {
                CharacterBody vicBody = damageReport.victimBody;
                CharacterBody atkBody = damageReport.attackerBody;

                if (vicBody && atkBody && atkBody.teamComponent && atkBody.inventory)
                {
                    int count = atkBody.inventory.GetItemCountEffective(def);
                    if (count > 0)
                    {
                        var lowest = Utilities.GetLowestHealthAlly(atkBody.teamComponent.teamIndex);
                        if (lowest && lowest.healthComponent && lowest.healthComponent.alive)
                        {
                            var healing = lowest.healthComponent.fullHealth * Utilities.GetLinearStacking(percentHealingOnDamage, percentHealingOnDamageExtraStacks, count) * radiantMultiplier;
                            OrbManager.instance.AddOrb(new HextechOrb(lowest, vicBody, atkBody, healing));
                        }
                    }
                }
            };
        }

        public class HextechOrb : Orb
        {
            private readonly float speed = 25f;

            private readonly CharacterBody toBeHealed;
            private readonly CharacterBody toBeTracked;
            private readonly float healing;

            public HextechOrb(CharacterBody targetBody, CharacterBody originBody, CharacterBody statisticsBody, float healAmount)
            {
                healing = healAmount;
                toBeHealed = targetBody;
                toBeTracked = statisticsBody;

                if (originBody && targetBody)
                {
                    origin = originBody ? originBody.corePosition : Vector3.zero;
                    if (targetBody) target = targetBody.mainHurtBox;
                }
            }

            public override void Begin()
            {
                base.duration = base.distanceToTarget / speed;
                EffectData effectData = new()
                {
                    origin = origin,
                    genericFloat = base.duration
                };
                effectData.SetHurtBoxReference(target);
                EffectManager.SpawnEffect(OrbStorageUtility.Get("Prefabs/Effects/OrbEffects/HealthOrbEffect"), effectData, transmit: true);
            }

            public override void OnArrival()
            {
                if (toBeHealed && toBeHealed.healthComponent)
                {
                    toBeHealed.healthComponent.Heal(healing, default, true);
                    if (toBeTracked && toBeTracked.inventory)
                    {
                        Statistics component = toBeTracked.inventory.GetComponent<Statistics>();
                        if (component) component.DamageHealed += healing;
                    }
                }
            }
        }
    }
}
