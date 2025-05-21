using RoR2;
using UnityEngine;
using UnityEngine.Networking;

namespace RiskOfTactics
{
    class Testing
    {
        public static void RunUpdate()
        {
            if (!NetworkServer.active) return;

            if (Input.GetKeyDown(KeyCode.F2))
            {
                /* TODO: Add items here for testing purposes. */
                DropItem(BFSword.itemDef);
                DropItem(ChainVest.itemDef);
                DropItem(GiantsBelt.itemDef);
                DropItem(NeedlesslyLargeRod.itemDef);
                DropItem(NegatronCloak.itemDef);
                DropItem(RecurveBow.itemDef);
                DropItem(SparringGloves.itemDef);
                DropItem(TearOfTheGoddess.itemDef);

                DropItem(AdaptiveHelm.itemDef);
                DropItem(ArchangelsStaff.itemDef);
                DropItem(Bloodthirster.itemDef);
                DropItem(Crownguard.itemDef);
                DropItem(Deathblade.itemDef);
                DropItem(DragonsClaw.itemDef);
                DropItem(HandOfJustice.itemDef);
                DropItem(JeweledGauntlet.itemDef);
                DropItem(RabadonsDeathcap.itemDef);
                DropItem(SpearOfShojin.itemDef);
                DropItem(StatikkShiv.itemDef);
                DropItem(SteadfastHeart.itemDef);
                DropItem(Quicksilver.itemDef);
                DropItem(WarmogsArmor.itemDef);
            }
        }

        public static void DropItem(ItemDef def)
        {
            DropItem(def, 1);
        }

        public static void DropItem(ItemDef def, int itemCount)
        {
            foreach (PlayerCharacterMasterController controller in PlayerCharacterMasterController.instances)
            {
                CharacterBody body = controller.master.GetBody();
                if (body)
                {
                    ScrapperController.CreateItemTakenOrb(body.corePosition, body.gameObject, def.itemIndex);
                    body.inventory.GiveItem(def, itemCount);
                }
            }
        }

        public static void DropItem(EquipmentDef def)
        {
            foreach (PlayerCharacterMasterController controller in PlayerCharacterMasterController.instances)
            {
                Transform transform = controller.master.GetBodyObject().transform;

                Log.Info($"Dropping {def.nameToken} at coordinates {transform.position}");
                PickupDropletController.CreatePickupDroplet(PickupCatalog.FindPickupIndex(def.equipmentIndex), transform.position, transform.forward * 20f);
            }
        }
    }
}
