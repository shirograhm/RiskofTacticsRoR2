using R2API;
using RiskOfTactics.Helpers;
using RoR2;
using RoR2.Navigation;
using RoR2.Networking;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Networking;

namespace RiskOfTactics.Content.Items.Shrines
{
    public class ForgeAnvil
    {
        public static DirectorCard directorCard;
        public static InteractableSpawnCard spawnCard;

        public static GameObject prefab;

        public static Dictionary<string, Dictionary<string, List<DirectorCard>>> sceneCategoryCards = [];

        public static ConfigurableValue<bool> isEnabled = new(
            "Shrine: Forge Anvil",
            "Enabled",
            true,
            "Whether or not the shrine is enabled.",
            ["ITEM_ROT_FORGEANVIL_DESC"]
        );

        internal static void Init()
        {
            prefab = PrefabAPI.InstantiateClone(AssetHandler.bundle.LoadAsset<GameObject>("ForgeAnvil.prefab"), "RiskOfTactics_ForgeAnvil", true);
            prefab.AddComponent<NetworkTransform>();

            GameObject shrineChanceSymbol = LegacyResourcesAPI.Load<GameObject>("Prefabs/NetworkedObjects/Shrines/ShrineGoldshoresAccess").transform.Find("Symbol").gameObject;
            GameObject symbol = PrefabAPI.InstantiateClone(shrineChanceSymbol, "Symbol");

            PurchaseInteraction purchaseInteraction = prefab.AddComponent<PurchaseInteraction>();
            purchaseInteraction.displayNameToken = "ROT_FORGEANVIL_NAME";
            purchaseInteraction.contextToken = "ROT_FORGEANVIL_CONTEXT";
            purchaseInteraction.costType = CostTypeIndex.GreenItem;
            purchaseInteraction.available = true;
            purchaseInteraction.cost = 5;
            purchaseInteraction.automaticallyScaleCostWithDifficulty = false;
            purchaseInteraction.ignoreSpherecastForInteractability = false;
            purchaseInteraction.setUnavailableOnTeleporterActivated = false;
            purchaseInteraction.isShrine = true;

            PurchaseAvailabilityIndicator purchaseAvailabilityIndicator = prefab.AddComponent<PurchaseAvailabilityIndicator>();
            purchaseAvailabilityIndicator.indicatorObject = symbol.gameObject;

            ForgeAnvilBehaviour behaviour = prefab.AddComponent<ForgeAnvilBehaviour>();
            behaviour.maxPurchaseCount = 10;
            behaviour.costMultiplierPerPurchase = 1f;
            behaviour.symbolTransform = symbol.transform;

            spawnCard = ScriptableObject.CreateInstance<InteractableSpawnCard>();
            spawnCard.name = "iscShrineForgeAnvil";
            spawnCard.prefab = prefab;
            spawnCard.sendOverNetwork = true;
            spawnCard.hullSize = HullClassification.Golem;
            spawnCard.nodeGraphType = MapNodeGroup.GraphType.Ground;
            spawnCard.requiredFlags = NodeFlags.None;
            spawnCard.forbiddenFlags = NodeFlags.NoShrineSpawn;
            // Director credit cost is used for rarity
            spawnCard.directorCreditCost = 1;
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

            if (isEnabled && directorCard != null)
                AddDirectorCardTo("wispgraveyard", "Shrines", directorCard);
            else
                RemoveDirectorCardFrom("wispgraveyard", "Shrines", directorCard);

            // Add director cards
            SceneDirector.onGenerateInteractableCardSelection += (sceneDirector, dccs) =>
            {
                SceneInfo sceneInfo = SceneInfo.instance;
                if (sceneInfo)
                {
                    SceneDef sceneDef = sceneInfo.sceneDef;
                    if (sceneDef && sceneCategoryCards.ContainsKey(sceneDef.baseSceneName))
                    {
                        Dictionary<string, List<DirectorCard>> categoryCards = sceneCategoryCards[sceneDef.baseSceneName];
                        DirectorCardCategorySelection.Category[] categories = dccs.categories;
                        if (categories != null)
                        {
                            for (int i = 0; i < dccs.categories.Length; i++)
                            {
                                DirectorCardCategorySelection.Category category = dccs.categories[i];
                                if (categoryCards.ContainsKey(category.name))
                                {
                                    foreach (DirectorCard directorCard in categoryCards[category.name])
                                    {
                                        dccs.AddCard(i, directorCard);
                                    }
                                }
                            }
                        }
                    }
                }
            };
        }

        public static void AddDirectorCardTo(string sceneName, string categoryName, DirectorCard directorCard)
        {
            Log.Error("8");
            Dictionary<string, List<DirectorCard>> categoryCards;
            if (sceneCategoryCards.ContainsKey(sceneName)) categoryCards = sceneCategoryCards[sceneName];
            else
            {
                categoryCards = [];
                sceneCategoryCards.Add(sceneName, categoryCards);
            }

            List<DirectorCard> cards;
            if (categoryCards.ContainsKey(categoryName)) cards = categoryCards[categoryName];
            else
            {
                cards = [];
                categoryCards.Add(categoryName, cards);
            }

            cards.Add(directorCard);
        }

        public static void RemoveDirectorCardFrom(string sceneName, string categoryName, DirectorCard directorCard)
        {
            Log.Error("9");
            Dictionary<string, List<DirectorCard>> categoryCards;
            if (!sceneCategoryCards.ContainsKey(sceneName)) return;
            categoryCards = sceneCategoryCards[sceneName];

            List<DirectorCard> cards;
            if (!categoryCards.ContainsKey(categoryName)) return;
            cards = categoryCards[categoryName];

            if (cards.Contains(directorCard)) cards.Remove(directorCard);
        }

        public class ForgeAnvilBehaviour : NetworkBehaviour
        {
            public int maxPurchaseCount;
            public float costMultiplierPerPurchase;
            public Transform symbolTransform;
            public PurchaseInteraction purchaseInteraction;
            public int purchaseCount;
            public float refreshTimer;
            public const float refreshDuration = 0.5f;
            public bool waitingForRefresh;
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
                purchaseInteraction.onDetailedPurchaseServer.AddListener((interactor, payCostResults) =>
                {
                    purchaseInteraction.SetAvailable(false);
                    delayedEvent.CallDelayed(1.5f);
                });

                availableItems = ItemHelper.artifactList.ConvertAll(x => x.itemIndex);
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

                int addReds = 0;
                var itemStacks = component.inventory.effectiveItemStacks;
                for (int i = 0; i < ItemCatalog.itemCount; i++)
                {
                    ItemIndex itemIndex = (ItemIndex)i;
                    var count = itemStacks.GetStackValue(itemIndex);
                    if (count > 0)
                    {
                        switch (ItemCatalog.GetItemDef(itemIndex).tier)
                        {
                            case ItemTier.Tier1:
                            case ItemTier.Tier2:
                            case ItemTier.Lunar:
                            case ItemTier.Boss:
                                addReds += count;
                                component.inventory.itemAcquisitionOrder.Remove(itemIndex);
                                component.inventory.ResetItemPermanent(itemIndex);
                                break;
                        }
                    }
                }
                for (var i = 0; i < addReds; i++)
                {
                    var rolledArtifact = RiskOfTactics.rng.NextElementUniform(availableItems);
                    component.inventory.GiveItemPermanent(rolledArtifact);
                }
                component.inventory.SetDirtyBit(8U);

                Chat.SendBroadcastChat(new Chat.SubjectFormatChatMessage
                {
                    subjectAsCharacterBody = component,
                    baseToken = "ROT_FORGEANVIL_USE_MESSAGE"
                });
                EffectManager.SpawnEffect(LegacyResourcesAPI.Load<GameObject>("Prefabs/Effects/ShrineUseEffect"), new EffectData
                {
                    origin = transform.position,
                    rotation = Quaternion.identity,
                    scale = 1f,
                    color = new Color32(250, 73, 50, 255)
                }, true);
                purchaseCount++;
                refreshTimer = refreshDuration;
                if (purchaseCount >= maxPurchaseCount)
                {
                    symbolTransform.gameObject.SetActive(false);
                }
            }

            public override int GetNetworkChannel()
            {
                return QosChannelIndex.defaultReliable.intVal;
            }
        }
    }
}
