using RoR2;
using RoR2.Navigation;
using RoR2.Networking;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Networking;

namespace RiskOfTactics
{
    class ShrineOfRadiance
    {
        public static GameObject prefab;

        public static ModelLocator modelLocator;
        public static Transform modelBaseTransform;
        public static Transform modelTransform;
        public static GameObject meshObject;
        public static string genericDisplayNameToken;

        public static InteractableSpawnCard spawnCard;
        public static DirectorCard directorCard;

        internal static void Init()
        {
            prefab = AssetHandler.bundle.LoadAsset<GameObject>("ShrineOfRadiance.prefab");

            Hooks();
        }

        public static void Load()
        {
            PrepareModel();
            AddPurchasable();

            SetSpawnCards();
        }

        public static void PrepareModel()
        {
            modelLocator = prefab.AddComponent<ModelLocator>();
            modelLocator.dontDetatchFromParent = true;
            modelLocator.modelBaseTransform = modelBaseTransform;
            modelLocator.modelTransform = modelTransform;
            modelLocator.normalizeToFloor = false;

            Highlight highlight = prefab.AddComponent<Highlight>();
            highlight.targetRenderer = meshObject.GetComponent<Renderer>();
            highlight.strength = 1f;
            highlight.highlightColor = Highlight.HighlightColor.interactive;

            prefab.AddComponent<GenericDisplayNameProvider>().displayToken = genericDisplayNameToken;

            EntityLocator entityLocator = meshObject.AddComponent<EntityLocator>();
            entityLocator.entity = prefab;
        }

        public static void SetSpawnCards()
        {
            spawnCard = ScriptableObject.CreateInstance<InteractableSpawnCard>();
            spawnCard.name = "iscShrineOfRadiance";
            spawnCard.prefab = prefab;
            spawnCard.sendOverNetwork = true;
            spawnCard.hullSize = HullClassification.Human;
            spawnCard.nodeGraphType = MapNodeGroup.GraphType.Ground;
            spawnCard.requiredFlags = NodeFlags.None;
            spawnCard.forbiddenFlags = NodeFlags.None;
            spawnCard.directorCreditCost = 0;
            spawnCard.occupyPosition = true;
            spawnCard.orientToFloor = true;
            spawnCard.slightlyRandomizeOrientation = true;
            spawnCard.skipSpawnWhenSacrificeArtifactEnabled = false;

            directorCard = new DirectorCard
            {
                spawnCard = spawnCard,
                selectionWeight = 1,
                spawnDistance = 0f,
                preventOverhead = false,
                minimumStageCompletions = 1,
                requiredUnlockableDef = null,
                forbiddenUnlockableDef = null
            };
        }

        public static void AddPurchasable()
        {
            GameObject shrineChanceSymbol = LegacyResourcesAPI.Load<GameObject>("Prefabs/NetworkedObjects/Shrines/ShrineGoldshoresAccess").transform.Find("Symbol").gameObject;
            GameObject symbol = prefab.transform.Find("Symbol").gameObject;
            symbol.GetComponent<MeshFilter>().mesh = Object.Instantiate(shrineChanceSymbol.GetComponent<MeshFilter>().mesh);
            Material symbolMaterial = Object.Instantiate(shrineChanceSymbol.GetComponent<MeshRenderer>().material);
            symbol.GetComponent<MeshRenderer>().material = symbolMaterial;
            symbolMaterial.SetTexture("_MainTex", AssetHandler.bundle.LoadAsset<Texture>("ShrineOfRadianceSymbol.png"));
            symbolMaterial.SetTextureScale("_MainTex", new Vector2(1f, 1f));
            symbolMaterial.SetTexture("_RemapTex", AssetHandler.bundle.LoadAsset<Texture>("ShrineOfRadianceSymbolRemap.png"));
            symbol.AddComponent<Billboard>();

            PurchaseInteraction purchaseInteraction = prefab.AddComponent<PurchaseInteraction>();
            purchaseInteraction.displayNameToken = "RISKOFTACTICS_SHRINE_OF_RADIANCE_NAME";
            purchaseInteraction.contextToken = "RISKOFTACTICS_SHRINE_OF_RADIANCE_CONTEXT";
            purchaseInteraction.costType = CostTypeIndex.Money;
            purchaseInteraction.available = true;
            purchaseInteraction.cost = 100;
            purchaseInteraction.automaticallyScaleCostWithDifficulty = true;
            purchaseInteraction.ignoreSpherecastForInteractability = false;
            purchaseInteraction.setUnavailableOnTeleporterActivated = true;
            purchaseInteraction.isShrine = true;

            PurchaseAvailabilityIndicator purchaseAvailabilityIndicator = prefab.AddComponent<PurchaseAvailabilityIndicator>();
            purchaseAvailabilityIndicator.indicatorObject = symbol.gameObject;

        }

        public static void Hooks()
        {
            SceneDirector.onGenerateInteractableCardSelection += (sceneDirector, dccs) =>
            {
                DirectorCardCategorySelection.Category[] categories = dccs.categories;
                if (categories != null)
                {
                    for (int i = 0; i < categories.Length; i++)
                    {
                        dccs.AddCard(i, directorCard);
                    }
                }
            };
        }

        public class ShrineOfRadianceBehavior : NetworkBehaviour
        {
            public int maxPurchaseCount;
            public float costMultiplierPerPurchase;
            public Transform symbolTransform;
            public PurchaseInteraction purchaseInteraction;
            public int purchaseCount;
            public float refreshTimer;
            public const float refreshDuration = 0.5f;
            public bool waitingForRefresh;
            public Xoroshiro128Plus rng;
            public List<ItemIndex> availableItems;

            public void Start()
            {
                RoR2.EntityLogic.DelayedEvent delayedEvent = GetComponent<RoR2.EntityLogic.DelayedEvent>();
                delayedEvent.action = new UnityEvent();
                delayedEvent.action.AddListener(() =>
                {
                    AddShrineStack(purchaseInteraction.lastActivator);
                });
                delayedEvent.timeStepType = RoR2.EntityLogic.DelayedEvent.TimeStepType.FixedTime;

                purchaseInteraction = GetComponent<PurchaseInteraction>();
                purchaseInteraction.onPurchase.AddListener((interactor) =>
                {
                    purchaseInteraction.SetAvailable(false);
                    delayedEvent.CallDelayed(1.5f);
                });

                availableItems = new List<ItemIndex>();
                if (NetworkServer.active)
                {
                    rng = new Xoroshiro128Plus(Run.instance.stageRng.nextUlong);
                    foreach (PickupIndex pickupIndex in Run.instance.availableTier3DropList)
                    {
                        availableItems.Add(PickupCatalog.GetPickupDef(pickupIndex).itemIndex);
                    }
                }
            }

            public void FixedUpdate()
            {
                if (waitingForRefresh)
                {
                    refreshTimer -= Time.fixedDeltaTime;
                    if (refreshTimer <= 0f && purchaseCount < maxPurchaseCount)
                    {
                        purchaseInteraction.SetAvailable(true);
                        purchaseInteraction.Networkcost = (int)(100f * (1f - Mathf.Pow(1f - (float)purchaseInteraction.cost / 100f, costMultiplierPerPurchase)));
                        waitingForRefresh = false;
                    }
                }
            }

            [Server]
            public void AddShrineStack(Interactor interactor)
            {
                waitingForRefresh = true;
                CharacterBody component = interactor.GetComponent<CharacterBody>();



                //int addReds = 0;
                //int[] itemStacks = component.inventory.itemStacks;
                //for (int i = 0; i < itemStacks.Length; i++)
                //{
                //    ItemIndex itemIndex = (ItemIndex)i;
                //    if (itemStacks[i] > 0)
                //    {
                //        switch (ItemCatalog.GetItemDef(itemIndex).tier)
                //        {
                //            case ItemTier.Tier1:
                //            case ItemTier.Tier2:
                //            case ItemTier.Lunar:
                //            case ItemTier.Boss:
                //                addReds += itemStacks[i];
                //                component.inventory.itemAcquisitionOrder.Remove(itemIndex);
                //                component.inventory.ResetItem(itemIndex);
                //                break;
                //        }
                //    }
                //}
                //for (var i = 0; i < addReds; i++)
                //{
                //    var rolledRed = rng.NextElementUniform<ItemIndex>(availableItems);
                //    component.inventory.GiveItem(rolledRed);
                //}
                //component.inventory.SetDirtyBit(8U);

                //Chat.SendBroadcastChat(new Chat.SubjectFormatChatMessage
                //{
                //    subjectAsCharacterBody = component,
                //    baseToken = "RISKOFTACTICS_SHRINEOFRADIANCE_USE_MESSAGE"
                //});
                //EffectManager.SpawnEffect(LegacyResourcesAPI.Load<GameObject>("Prefabs/Effects/ShrineUseEffect"), new EffectData
                //{
                //    origin = transform.position,
                //    rotation = Quaternion.identity,
                //    scale = 1f,
                //    color = new Color32(255, 97, 84, 255)
                //}, true);
                //purchaseCount++;
                //refreshTimer = refreshDuration;
                //if (purchaseCount >= maxPurchaseCount)
                //{
                //    symbolTransform.gameObject.SetActive(false);
                //}
            }

            public override int GetNetworkChannel()
            {
                return QosChannelIndex.defaultReliable.intVal;
            }
        }
    }
}