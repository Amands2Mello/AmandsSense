using UnityEngine;
using UnityEngine.Rendering.PostProcessing;
using UnityStandardAssets.ImageEffects;
using UnityEngine.SceneManagement;
using System;
using EFT.Weather;
using System.Collections.Generic;
using BSG.CameraEffects;
using HarmonyLib;
using UnityEngine.Rendering;
using EFT;
using EFT.InventoryLogic;
using System.Reflection;
using EFT.UI;
using Comfort.Common;
using System.IO;
using System.Threading.Tasks;
using UnityEngine.Networking;
using EFT.Interactive;
using System.Linq;
using Aki.Common.Utils;
using Sirenix.Utilities;

namespace AmandsSense
{
    public class AmandsSenseClass : MonoBehaviour
    {
        public static LocalPlayer localPlayer;

        public static LayerMask SphereInteractiveLayerMask = LayerMask.GetMask("Interactive");
        public static LayerMask SphereDeadbodyLayerMask = LayerMask.GetMask("Deadbody");

        public static Dictionary<string, Sprite> LoadedSprites = new Dictionary<string, Sprite>();
        public static Dictionary<string, AudioClip> LoadedAudioClips = new Dictionary<string, AudioClip>();

        public static List<string> ItemsSenses = new List<string>();
        public static List<AmandsSenseAlwaysOn> ItemsAlwaysOn = new List<AmandsSenseAlwaysOn>();
        public static List<AmandsSenseContainerAlwaysOn> ContainersAlwaysOn = new List<AmandsSenseContainerAlwaysOn>();
        public static List<AmandsSenseDeadbodyAlwaysOn> DeadbodyAlwaysOn = new List<AmandsSenseDeadbodyAlwaysOn>();

        public static float CooldownTime = 0f;

        public delegate void ItemsSensesAdded(string Id, string TemplateId, bool CanSellOnRagfair, Vector3 position, ESenseItemType SenseItemType);
        public static ItemsSensesAdded onItemsSensesAdded;
        public delegate void ItemsSensesRemove(string Id);
        public static ItemsSensesRemove onItemsSensesRemove;
        public delegate void ContainerSensesAdded(string Id, Vector3 position);
        public static ContainerSensesAdded onContainerSensesAdded;
        public static float Radius = 0f;

        public static PrismEffects prismEffects;

        public static ItemsJsonClass itemsJsonClass;

        public static float lastDoubleClickTime = 0.0f;

        public static bool ValidSenseAlwaysOn = false;

        public void Start()
        {
            itemsJsonClass = ReadFromJsonFile<ItemsJsonClass>((AppDomain.CurrentDomain.BaseDirectory + "/BepInEx/plugins/Sense/Items.json"));
            ReloadFiles();
            onItemsSensesAdded += ItemsSensesAddedMethod;
            onItemsSensesRemove += ItemsSensesRemoveMethod;
            onContainerSensesAdded += ContainerSensesAddedMethod;
        }
        public void ItemsSensesAddedMethod(string Id, string TemplateId, bool CanSellOnRagfair, Vector3 position, ESenseItemType SenseItemType)
        {
        }
        public void ItemsSensesRemoveMethod(string Id)
        {
        }
        public void ContainerSensesAddedMethod(string Id, Vector3 position)
        {
        }
        public void Update()
        {
            if (localPlayer != null && AmandsSensePlugin.EnableSense.Value)
            {
                if (CooldownTime < AmandsSensePlugin.Cooldown.Value)
                {
                    CooldownTime += Time.deltaTime;
                }
                if (Input.GetKeyDown(AmandsSensePlugin.SenseKey.Value.MainKey))
                {
                    if (AmandsSensePlugin.DoubleClick.Value)
                    {
                        float timeSinceLastClick = Time.time - lastDoubleClickTime;
                        lastDoubleClickTime = Time.time;
                        if (timeSinceLastClick <= AmandsSensePlugin.DoubleClickDelay.Value && CooldownTime >= AmandsSensePlugin.Cooldown.Value)
                        {
                            Sense();
                        }
                    }
                    else
                    {
                        if (CooldownTime >= AmandsSensePlugin.Cooldown.Value)
                        {
                            Sense();
                        }
                    }
                }
                if (Radius < Mathf.Max(AmandsSensePlugin.Radius.Value, AmandsSensePlugin.DeadbodyRadius.Value))
                {
                    Radius += AmandsSensePlugin.Speed.Value * Time.deltaTime;
                    if (prismEffects != null)
                    {
                        prismEffects.dofFocusPoint = Radius - prismEffects.dofFocusDistance;
                        if (prismEffects.dofRadius < AmandsSensePlugin.dofRadius.Value)
                        {
                            prismEffects.dofRadius += AmandsSensePlugin.dofRadiusStartSpeed.Value * Time.deltaTime;
                        }
                    }
                }
                else if (prismEffects != null && prismEffects.dofRadius > 0.001f)
                {
                    prismEffects.dofRadius -= AmandsSensePlugin.dofRadiusEndSpeed.Value * Time.deltaTime;
                    if (prismEffects.dofRadius < 0.001f)
                    {
                        prismEffects.useDof = false;
                    }
                }
            }
        }
        public void Sense()
        {
            if (gameObject != null)
            {
                if (localPlayer != null)
                {
                    CooldownTime = 0;
                    Collider[] colliders = new Collider[AmandsSensePlugin.Limit.Value];
                    int colliderCount = Physics.OverlapSphereNonAlloc(localPlayer.Position, AmandsSensePlugin.Radius.Value, colliders, SphereInteractiveLayerMask, QueryTriggerInteraction.Collide);
                    for (int i = 0; i < colliderCount; i++)
                    {
                        Component component = colliders[i].transform.gameObject.GetComponent<ObservedLootItem>();
                        if (component != null)
                        {
                            ObservedLootItem observedLootItem = component as ObservedLootItem;
                            if (observedLootItem != null)
                            {
                                GameObject SenseItemGameObject = new GameObject("SenseItem");
                                AmandsSenseItem amandsSenseItem = SenseItemGameObject.AddComponent<AmandsSenseItem>();
                                amandsSenseItem.observedLootItem = observedLootItem;
                                amandsSenseItem.Id = observedLootItem.ItemId;
                                amandsSenseItem.Delay = Vector3.Distance(localPlayer.Position, observedLootItem.transform.position) / AmandsSensePlugin.Speed.Value;
                            }
                        }
                        else
                        {
                            component = colliders[i].transform.gameObject.GetComponent<LootableContainer>();
                            if (component != null)
                            {
                                LootableContainer lootableContainer = component as LootableContainer;
                                if (lootableContainer != null)
                                {
                                    GameObject SenseContainerGameObject = new GameObject("SenseContainer");
                                    AmandsSenseContainer amandsSenseContainer = SenseContainerGameObject.AddComponent<AmandsSenseContainer>();
                                    amandsSenseContainer.lootableContainer = lootableContainer;
                                    amandsSenseContainer.Id = lootableContainer.Id;
                                    amandsSenseContainer.Delay = Vector3.Distance(localPlayer.Position, lootableContainer.transform.position) / AmandsSensePlugin.Speed.Value;
                                }
                            }
                        }
                    }
                    List<string> players = new List<string>();
                    Collider[] colliders2 = new Collider[AmandsSensePlugin.Limit.Value];
                    int colliderCount2 = Physics.OverlapSphereNonAlloc(localPlayer.Position, AmandsSensePlugin.DeadbodyRadius.Value, colliders2, SphereDeadbodyLayerMask, QueryTriggerInteraction.Collide);
                    for (int i = 0; i < colliderCount2; i++)
                    {
                        BodyPartCollider bodyPartCollider = colliders2[i].transform.gameObject.GetComponent<BodyPartCollider>();
                        if (bodyPartCollider != null && bodyPartCollider.BodyPartType == EBodyPart.Chest)
                        {
                            Player player = Traverse.Create(bodyPartCollider).Field("Player").GetValue() as Player;
                            if (player == null)
                            {
                                player = Traverse.Create(bodyPartCollider).Property("Player").GetValue() as Player;
                            }
                            if (player != null && !players.Contains(player.ProfileId))
                            {
                                players.Add(player.ProfileId);
                                Corpse corpse = player.GetComponent<Corpse>();
                                if (corpse != null)
                                {
                                    GameObject SenseDeadbodyGameObject = new GameObject("SenseDeadbody");
                                    AmandsSenseDeadbody amandsSenseDeadbody = SenseDeadbodyGameObject.AddComponent<AmandsSenseDeadbody>();
                                    amandsSenseDeadbody.corpse = corpse;
                                    amandsSenseDeadbody.bodyPartCollider = bodyPartCollider;
                                    amandsSenseDeadbody.Id = corpse.ItemId;
                                    if (bodyPartCollider.Collider != null)
                                    {
                                        amandsSenseDeadbody.Delay = Vector3.Distance(localPlayer.Position, bodyPartCollider.Collider.transform.position) / AmandsSensePlugin.Speed.Value;
                                    }
                                    else
                                    {
                                        amandsSenseDeadbody.Delay = Vector3.Distance(localPlayer.Position, corpse.transform.position) / AmandsSensePlugin.Speed.Value;
                                    }
                                }
                            }
                        }
                    }
                }
                if (prismEffects != null)
                {
                    Radius = 0;
                    prismEffects.useDof = AmandsSensePlugin.useDof.Value;
                    prismEffects.debugDofPass = false;
                    prismEffects.dofForceEnableMedian = AmandsSensePlugin.dofForceEnableMedian.Value;
                    prismEffects.dofBokehFactor = AmandsSensePlugin.dofBokehFactor.Value;
                    prismEffects.dofFocusDistance = AmandsSensePlugin.dofFocusDistance.Value;
                    prismEffects.dofNearFocusDistance = 100f;
                    prismEffects.dofRadius = 0f;
                }
            }
        }
        public async void DynamicAlwaysOnSense()
        {
            await Task.Delay((int)(AmandsSensePlugin.AlwaysOnFrequency.Value * 1000));
            if (!AmandsSensePlugin.SenseAlwaysOn.Value)
            {
                if (ValidSenseAlwaysOn)
                {
                    ValidSenseAlwaysOn = false;
                    ClearAlwaysOn();
                }
                DynamicAlwaysOnSense();
                return;
            }
            if (localPlayer == null) return;
            Collider[] colliders = new Collider[AmandsSensePlugin.Limit.Value];
            int colliderCount = Physics.OverlapSphereNonAlloc(localPlayer.Position, AmandsSensePlugin.AlwaysOnRadius.Value, colliders, SphereInteractiveLayerMask, QueryTriggerInteraction.Collide);

            await Task.Delay((int)(100));
            List<Collider> collidersList = new List<Collider>(colliders);
            foreach (AmandsSenseAlwaysOn amandsSenseAlwaysOn in ItemsAlwaysOn)
            {
                if (amandsSenseAlwaysOn == null) continue;
                if (amandsSenseAlwaysOn.collider != null)
                {
                    int index = collidersList.IndexOf(amandsSenseAlwaysOn.collider);
                    if (index != -1)
                    {
                        collidersList[index] = null;
                    }
                    else
                    {
                        amandsSenseAlwaysOn.StartOpacity = false;
                        amandsSenseAlwaysOn.UpdateOpacity = true;
                    }
                }
                else
                {
                    amandsSenseAlwaysOn.StartOpacity = false;
                    amandsSenseAlwaysOn.UpdateOpacity = true;
                }
            }

            await Task.Delay((int)(100));
            foreach (AmandsSenseContainerAlwaysOn amandsSenseContainerAlwaysOn in ContainersAlwaysOn)
            {
                if (amandsSenseContainerAlwaysOn == null) continue;
                if (amandsSenseContainerAlwaysOn.collider != null)
                {
                    int index = collidersList.IndexOf(amandsSenseContainerAlwaysOn.collider);
                    if (index != -1)
                    {
                        collidersList[index] = null;
                    }
                    else
                    {
                        amandsSenseContainerAlwaysOn.StartOpacity = false;
                        amandsSenseContainerAlwaysOn.UpdateOpacity = true;
                    }
                }
                else
                {
                    amandsSenseContainerAlwaysOn.StartOpacity = false;
                    amandsSenseContainerAlwaysOn.UpdateOpacity = true;
                }
            }

            await Task.Delay((int)(100));
            for (int i = 0; i < colliderCount; i++)
            {
                if (collidersList[i] == null) continue;
                Component component = collidersList[i].transform.gameObject.GetComponent<ObservedLootItem>();
                if (component != null)
                {
                    ObservedLootItem observedLootItem = component as ObservedLootItem;
                    if (observedLootItem != null)
                    {
                        GameObject SenseAlwaysOnGameObject = new GameObject("SenseAlwaysOn");
                        AmandsSenseAlwaysOn amandsSenseAlwaysOn = SenseAlwaysOnGameObject.AddComponent<AmandsSenseAlwaysOn>();
                        amandsSenseAlwaysOn.collider = collidersList[i];
                        amandsSenseAlwaysOn.observedLootItem = observedLootItem;
                        amandsSenseAlwaysOn.Id = observedLootItem.ItemId;
                        //amandsSenseAlwaysOn.Delay = UnityEngine.Random.Range(0.0f, 1f);
                    }
                }
                else
                {
                    component = collidersList[i].transform.gameObject.GetComponent<LootableContainer>();
                    if (component != null)
                    {
                        LootableContainer lootableContainer = component as LootableContainer;
                        if (lootableContainer != null)
                        {
                            GameObject SenseContainerAlwaysOnGameObject = new GameObject("SenseContainerAlwaysOn");
                            AmandsSenseContainerAlwaysOn amandsSenseContainerAlwaysOn = SenseContainerAlwaysOnGameObject.AddComponent<AmandsSenseContainerAlwaysOn>();
                            amandsSenseContainerAlwaysOn.collider = collidersList[i];
                            amandsSenseContainerAlwaysOn.lootableContainer = lootableContainer;
                            amandsSenseContainerAlwaysOn.Id = lootableContainer.Id;
                            //amandsSenseContainerAlwaysOn.Delay = UnityEngine.Random.Range(0.0f, 1f);
                        }
                    }
                }
            }

            await Task.Delay((int)(100));
            List<string> players = new List<string>();
            Collider[] colliders2 = new Collider[AmandsSensePlugin.Limit.Value];
            int colliderCount2 = Physics.OverlapSphereNonAlloc(localPlayer.Position, AmandsSensePlugin.AlwaysOnDeadbodyRadius.Value, colliders2, SphereDeadbodyLayerMask, QueryTriggerInteraction.Collide);

            await Task.Delay((int)(100));
            List<Collider> colliders2List = new List<Collider>(colliders2);
            foreach (AmandsSenseDeadbodyAlwaysOn amandsSenseDeadbodyAlwaysOn in DeadbodyAlwaysOn)
            {
                if (amandsSenseDeadbodyAlwaysOn == null) continue;
                if (amandsSenseDeadbodyAlwaysOn.collider != null)
                {
                    int index = colliders2List.IndexOf(amandsSenseDeadbodyAlwaysOn.collider);
                    if (index != -1)
                    {
                        colliders2List[index] = null;
                    }
                    else
                    {
                        amandsSenseDeadbodyAlwaysOn.StartOpacity = false;
                        amandsSenseDeadbodyAlwaysOn.UpdateOpacity = true;
                    }
                }
                else
                {
                    amandsSenseDeadbodyAlwaysOn.StartOpacity = false;
                    amandsSenseDeadbodyAlwaysOn.UpdateOpacity = true;
                }
            }

            await Task.Delay((int)(100));
            for (int i = 0; i < colliderCount2; i++)
            {
                if (colliders2List[i] == null) continue;
                BodyPartCollider bodyPartCollider = colliders2List[i].transform.gameObject.GetComponent<BodyPartCollider>();
                if (bodyPartCollider != null && bodyPartCollider.BodyPartType == EBodyPart.Stomach)
                {
                    Player player = Traverse.Create(bodyPartCollider).Field("Player").GetValue() as Player;
                    if (player == null)
                    {
                        player = Traverse.Create(bodyPartCollider).Property("Player").GetValue() as Player;
                    }
                    if (player != null && !players.Contains(player.ProfileId))
                    {
                        players.Add(player.ProfileId);
                        Corpse corpse = player.GetComponent<Corpse>();
                        if (corpse != null)
                        {
                            GameObject SenseDeadbodyAlwaysOnGameObject = new GameObject("SenseDeadbodyAlwaysOn");
                            AmandsSenseDeadbodyAlwaysOn amandsSenseDeadbodyAlwaysOn = SenseDeadbodyAlwaysOnGameObject.AddComponent<AmandsSenseDeadbodyAlwaysOn>();
                            amandsSenseDeadbodyAlwaysOn.collider = colliders2List[i];
                            amandsSenseDeadbodyAlwaysOn.corpse = corpse;
                            amandsSenseDeadbodyAlwaysOn.bodyPartCollider = bodyPartCollider;
                            amandsSenseDeadbodyAlwaysOn.Id = corpse.ItemId;
                            //amandsSenseDeadbodyAlwaysOn.Delay = UnityEngine.Random.Range(0.0f, 1f);
                        }
                    }
                }
            }
            ValidSenseAlwaysOn = true;
            DynamicAlwaysOnSense();
        }
        public void ClearAlwaysOn()
        {
            foreach (AmandsSenseAlwaysOn amandsSenseAlwaysOn in ItemsAlwaysOn)
            {
                if (amandsSenseAlwaysOn == null) continue;
                amandsSenseAlwaysOn.StartOpacity = false;
                amandsSenseAlwaysOn.UpdateOpacity = true;
            }
            foreach (AmandsSenseContainerAlwaysOn amandsSenseContainerAlwaysOn in ContainersAlwaysOn)
            {
                if (amandsSenseContainerAlwaysOn == null) continue;
                amandsSenseContainerAlwaysOn.StartOpacity = false;
                amandsSenseContainerAlwaysOn.UpdateOpacity = true;
            }
            foreach (AmandsSenseDeadbodyAlwaysOn amandsSenseDeadbodyAlwaysOn in DeadbodyAlwaysOn)
            {
                if (amandsSenseDeadbodyAlwaysOn == null) continue;
                amandsSenseDeadbodyAlwaysOn.StartOpacity = false;
                amandsSenseDeadbodyAlwaysOn.UpdateOpacity = true;
            }
            ItemsAlwaysOn.Clear();
            ContainersAlwaysOn.Clear();
            DeadbodyAlwaysOn.Clear();
        }
        public static ESenseItemType SenseItemType(Type itemType)
        {
            if (TemplateIdToObjectMappingsClass.TypeTable["57864ada245977548638de91"].IsAssignableFrom(itemType))
            {
                return ESenseItemType.BuildingMaterials;
            }
            if (TemplateIdToObjectMappingsClass.TypeTable["57864a66245977548f04a81f"].IsAssignableFrom(itemType))
            {
                return ESenseItemType.Electronics;
            }
            if (TemplateIdToObjectMappingsClass.TypeTable["57864ee62459775490116fc1"].IsAssignableFrom(itemType))
            {
                return ESenseItemType.EnergyElements;
            }
            if (TemplateIdToObjectMappingsClass.TypeTable["57864e4c24597754843f8723"].IsAssignableFrom(itemType))
            {
                return ESenseItemType.FlammableMaterials;
            }
            if (TemplateIdToObjectMappingsClass.TypeTable["57864c322459775490116fbf"].IsAssignableFrom(itemType))
            {
                return ESenseItemType.HouseholdMaterials;
            }
            if (TemplateIdToObjectMappingsClass.TypeTable["57864c8c245977548867e7f1"].IsAssignableFrom(itemType))
            {
                return ESenseItemType.MedicalSupplies;
            }
            if (TemplateIdToObjectMappingsClass.TypeTable["57864bb7245977548b3b66c2"].IsAssignableFrom(itemType))
            {
                return ESenseItemType.Tools;
            }
            if (TemplateIdToObjectMappingsClass.TypeTable["57864a3d24597754843f8721"].IsAssignableFrom(itemType))
            {
                return ESenseItemType.Valuables;
            }
            if (TemplateIdToObjectMappingsClass.TypeTable["590c745b86f7743cc433c5f2"].IsAssignableFrom(itemType))
            {
                return ESenseItemType.Others;
            }
            if (TemplateIdToObjectMappingsClass.TypeTable["5448e53e4bdc2d60728b4567"].IsAssignableFrom(itemType))
            {
                return ESenseItemType.Backpacks;
            }
            if (TemplateIdToObjectMappingsClass.TypeTable["5448e54d4bdc2dcc718b4568"].IsAssignableFrom(itemType))
            {
                return ESenseItemType.BodyArmor;
            }
            if (TemplateIdToObjectMappingsClass.TypeTable["5448e5724bdc2ddf718b4568"].IsAssignableFrom(itemType))
            {
                return ESenseItemType.Eyewear;
            }
            if (TemplateIdToObjectMappingsClass.TypeTable["5a341c4686f77469e155819e"].IsAssignableFrom(itemType))
            {
                return ESenseItemType.Facecovers;
            }
            if (TemplateIdToObjectMappingsClass.TypeTable["5a341c4086f77401f2541505"].IsAssignableFrom(itemType))
            {
                return ESenseItemType.Headgear;
            }
            if (TemplateIdToObjectMappingsClass.TypeTable["57bef4c42459772e8d35a53b"].IsAssignableFrom(itemType))
            {
                return ESenseItemType.GearComponents;
            }
            if (TemplateIdToObjectMappingsClass.TypeTable["5b3f15d486f77432d0509248"].IsAssignableFrom(itemType))
            {
                return ESenseItemType.GearComponents;
            }
            if (TemplateIdToObjectMappingsClass.TypeTable["5645bcb74bdc2ded0b8b4578"].IsAssignableFrom(itemType))
            {
                return ESenseItemType.Headsets;
            }
            if (TemplateIdToObjectMappingsClass.TypeTable["5448bf274bdc2dfc2f8b456a"].IsAssignableFrom(itemType))
            {
                return ESenseItemType.SecureContainers;
            }
            if (TemplateIdToObjectMappingsClass.TypeTable["5795f317245977243854e041"].IsAssignableFrom(itemType))
            {
                return ESenseItemType.StorageContainers;
            }
            if (TemplateIdToObjectMappingsClass.TypeTable["5448e5284bdc2dcb718b4567"].IsAssignableFrom(itemType))
            {
                return ESenseItemType.TacticalRigs;
            }
            if (TemplateIdToObjectMappingsClass.TypeTable["550aa4154bdc2dd8348b456b"].IsAssignableFrom(itemType))
            {
                return ESenseItemType.FunctionalMods;
            }
            if (TemplateIdToObjectMappingsClass.TypeTable["55802f3e4bdc2de7118b4584"].IsAssignableFrom(itemType))
            {
                return ESenseItemType.GearMods;
            }
            if (TemplateIdToObjectMappingsClass.TypeTable["5a74651486f7744e73386dd1"].IsAssignableFrom(itemType))
            {
                return ESenseItemType.GearMods;
            }
            if (TemplateIdToObjectMappingsClass.TypeTable["55802f4a4bdc2ddb688b4569"].IsAssignableFrom(itemType))
            {
                return ESenseItemType.VitalParts;
            }
            if (TemplateIdToObjectMappingsClass.TypeTable["5447e1d04bdc2dff2f8b4567"].IsAssignableFrom(itemType))
            {
                return ESenseItemType.MeleeWeapons;
            }
            if (TemplateIdToObjectMappingsClass.TypeTable["543be6564bdc2df4348b4568"].IsAssignableFrom(itemType))
            {
                return ESenseItemType.Throwables;
            }
            if (TemplateIdToObjectMappingsClass.TypeTable["543be5cb4bdc2deb348b4568"].IsAssignableFrom(itemType))
            {
                return ESenseItemType.AmmoPacks;
            }
            if (TemplateIdToObjectMappingsClass.TypeTable["5485a8684bdc2da71d8b4567"].IsAssignableFrom(itemType))
            {
                return ESenseItemType.Rounds;
            }
            if (TemplateIdToObjectMappingsClass.TypeTable["5448e8d64bdc2dce718b4568"].IsAssignableFrom(itemType))
            {
                return ESenseItemType.Drinks;
            }
            if (TemplateIdToObjectMappingsClass.TypeTable["5448e8d04bdc2ddf718b4569"].IsAssignableFrom(itemType))
            {
                return ESenseItemType.Food;
            }
            if (TemplateIdToObjectMappingsClass.TypeTable["5448f3a64bdc2d60728b456a"].IsAssignableFrom(itemType))
            {
                return ESenseItemType.Injectors;
            }
            if (TemplateIdToObjectMappingsClass.TypeTable["5448f3ac4bdc2dce718b4569"].IsAssignableFrom(itemType))
            {
                return ESenseItemType.InjuryTreatment;
            }
            if (TemplateIdToObjectMappingsClass.TypeTable["5448f39d4bdc2d0a728b4568"].IsAssignableFrom(itemType))
            {
                return ESenseItemType.Medkits;
            }
            if (TemplateIdToObjectMappingsClass.TypeTable["5448f3a14bdc2d27728b4569"].IsAssignableFrom(itemType))
            {
                return ESenseItemType.Pills;
            }
            if (TemplateIdToObjectMappingsClass.TypeTable["5c164d2286f774194c5e69fa"].IsAssignableFrom(itemType))
            {
                return ESenseItemType.ElectronicKeys;
            }
            if (TemplateIdToObjectMappingsClass.TypeTable["5c99f98d86f7745c314214b3"].IsAssignableFrom(itemType))
            {
                return ESenseItemType.MechanicalKeys;
            }
            if (TemplateIdToObjectMappingsClass.TypeTable["5448ecbe4bdc2d60728b4568"].IsAssignableFrom(itemType))
            {
                return ESenseItemType.InfoItems;
            }
            if (TemplateIdToObjectMappingsClass.TypeTable["5447e0e74bdc2d3c308b4567"].IsAssignableFrom(itemType))
            {
                return ESenseItemType.SpecialEquipment;
            }
            if (TemplateIdToObjectMappingsClass.TypeTable["616eb7aea207f41933308f46"].IsAssignableFrom(itemType))
            {
                return ESenseItemType.SpecialEquipment;
            }
            if (TemplateIdToObjectMappingsClass.TypeTable["61605ddea09d851a0a0c1bbc"].IsAssignableFrom(itemType))
            {
                return ESenseItemType.SpecialEquipment;
            }
            if (TemplateIdToObjectMappingsClass.TypeTable["5f4fbaaca5573a5ac31db429"].IsAssignableFrom(itemType))
            {
                return ESenseItemType.SpecialEquipment;
            }
            if (TemplateIdToObjectMappingsClass.TypeTable["567849dd4bdc2d150f8b456e"].IsAssignableFrom(itemType))
            {
                return ESenseItemType.Maps;
            }
            if (TemplateIdToObjectMappingsClass.TypeTable["543be5dd4bdc2deb348b4569"].IsAssignableFrom(itemType))
            {
                return ESenseItemType.Money;
            }
            return ESenseItemType.All;
        }
        public static void WriteToJsonFile<T>(string filePath, T objectToWrite, bool append = false) where T : new()
        {
            TextWriter writer = null;
            try
            {
                var contentsToWriteToFile = Json.Serialize(objectToWrite);
                writer = new StreamWriter(filePath, append);
                writer.Write(contentsToWriteToFile);
            }
            finally
            {
                if (writer != null)
                    writer.Close();
            }
        }
        public static T ReadFromJsonFile<T>(string filePath) where T : new()
        {
            TextReader reader = null;
            try
            {
                reader = new StreamReader(filePath);
                var fileContents = reader.ReadToEnd();
                return Json.Deserialize<T>(fileContents);
            }
            finally
            {
                if (reader != null)
                    reader.Close();
            }
        }
        public static void ReloadFiles()
        {
            string[] Files = Directory.GetFiles(AppDomain.CurrentDomain.BaseDirectory + "/BepInEx/plugins/Sense/images/", "*.png");
            foreach (string File in Files)
            {
                LoadSprite(File);
            }
            string[] AudioFiles = Directory.GetFiles(AppDomain.CurrentDomain.BaseDirectory + "/BepInEx/plugins/Sense/sounds/");
            foreach (string File in AudioFiles)
            {
                LoadAudioClip(File);
            }
        }
        async static void LoadSprite(string path)
        {
            LoadedSprites[Path.GetFileName(path)] = await RequestSprite(path);
        }
        async static Task<Sprite> RequestSprite(string path)
        {
            UnityWebRequest www = UnityWebRequestTexture.GetTexture(path);
            var SendWeb = www.SendWebRequest();

            while (!SendWeb.isDone)
                await Task.Yield();

            if (www.isNetworkError || www.isHttpError)
            {
                return null;
            }
            else
            {
                Texture2D texture = ((DownloadHandlerTexture)www.downloadHandler).texture;
                Sprite sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f));

                return sprite;
            }
        }
        async static void LoadAudioClip(string path)
        {
            LoadedAudioClips[Path.GetFileName(path)] = await RequestAudioClip(path);
        }
        async static Task<AudioClip> RequestAudioClip(string path)
        {
            string extension = Path.GetExtension(path);
            AudioType audioType = AudioType.WAV;
            switch (extension)
            {
                case ".wav":
                    audioType = AudioType.WAV;
                    break;
                case ".ogg":
                    audioType = AudioType.OGGVORBIS;
                    break;
            }
            UnityWebRequest www = UnityWebRequestMultimedia.GetAudioClip(path, audioType);
            var SendWeb = www.SendWebRequest();

            while (!SendWeb.isDone)
                await Task.Yield();

            if (www.isNetworkError || www.isHttpError)
            {
                return null;
            }
            else
            {
                AudioClip audioclip = DownloadHandlerAudioClip.GetContent(www);
                return audioclip;
            }
        }
    }
    public class AmandsSenseItem : MonoBehaviour
    {
        public ObservedLootItem observedLootItem;
        public string Id;
        public SpriteRenderer spriteRenderer;
        public Sprite sprite;
        public string ItemSound;
        public Light light;
        public Color color = AmandsSensePlugin.ObservedLootItemColor.Value;
        public bool useNewSize = false;

        public float Delay = 0f;
        private bool UpdateOpacity = false;
        private bool StartOpacity = true;
        private float Opacity = 0f;

        public void Start()
        {
            if (AmandsSenseClass.ItemsSenses.Contains(Id))
            {
                Destroy(gameObject);
            }
            else
            {
                AmandsSenseClass.ItemsSenses.Add(Id);
                WaitAndStart();
            }
        }
        private async void WaitAndStart()
        {
            await Task.Delay((int)(Delay * 1000));
            if (observedLootItem != null && observedLootItem.gameObject.activeSelf && observedLootItem.Item != null && AmandsSenseClass.localPlayer != null && observedLootItem.transform.position.y > AmandsSenseClass.localPlayer.Position.y + AmandsSensePlugin.MinHeight.Value && observedLootItem.transform.position.y < AmandsSenseClass.localPlayer.Position.y + AmandsSensePlugin.MaxHeight.Value)
            {
                BoxCollider boxCollider = observedLootItem.gameObject.GetComponent<BoxCollider>();
                if (boxCollider != null)
                {
                    Vector3 position = boxCollider.transform.TransformPoint(boxCollider.center);
                    gameObject.transform.position = new Vector3(position.x, boxCollider.ClosestPoint(position + (Vector3.up * 100f)).y + AmandsSensePlugin.NormalSize.Value, position.z);
                }
                else
                {
                    gameObject.transform.position = observedLootItem.transform.position + (Vector3.up * AmandsSensePlugin.NormalSize.Value);
                }
                ESenseItemType eSenseItemType = ESenseItemType.ObservedLootItem;
                eSenseItemType = AmandsSenseClass.SenseItemType(observedLootItem.Item.GetType());
                if (typeof(Weapon).IsAssignableFrom(observedLootItem.Item.GetType()))
                {
                    Weapon weapon = observedLootItem.Item as Weapon;
                    if (weapon != null)
                    {
                        switch (weapon.WeapClass)
                        {
                            case "assaultCarbine":
                                eSenseItemType = ESenseItemType.AssaultCarbines;
                                color = AmandsSensePlugin.AssaultCarbinesColor.Value;
                                if (AmandsSenseClass.LoadedSprites.ContainsKey("icon_weapons_carbines.png"))
                                {
                                    sprite = AmandsSenseClass.LoadedSprites["icon_weapons_carbines.png"];
                                    useNewSize = true;
                                }
                                break;
                            case "assaultRifle":
                                eSenseItemType = ESenseItemType.AssaultRifles;
                                color = AmandsSensePlugin.AssaultRiflesColor.Value;
                                if (AmandsSenseClass.LoadedSprites.ContainsKey("icon_weapons_assaultrifles.png"))
                                {
                                    sprite = AmandsSenseClass.LoadedSprites["icon_weapons_assaultrifles.png"];
                                    useNewSize = true;
                                }
                                break;
                            case "sniperRifle":
                                eSenseItemType = ESenseItemType.BoltActionRifles;
                                color = AmandsSensePlugin.BoltActionRiflesColor.Value;
                                if (AmandsSenseClass.LoadedSprites.ContainsKey("icon_weapons_botaction.png"))
                                {
                                    sprite = AmandsSenseClass.LoadedSprites["icon_weapons_botaction.png"];
                                    useNewSize = true;
                                }
                                break;
                            case "grenadeLauncher":
                                eSenseItemType = ESenseItemType.GrenadeLaunchers;
                                color = AmandsSensePlugin.GrenadeLaunchersColor.Value;
                                if (AmandsSenseClass.LoadedSprites.ContainsKey("icon_weapons_gl.png"))
                                {
                                    sprite = AmandsSenseClass.LoadedSprites["icon_weapons_gl.png"];
                                    useNewSize = true;
                                }
                                break;
                            case "machinegun":
                                eSenseItemType = ESenseItemType.MachineGuns;
                                color = AmandsSensePlugin.MachineGunsColor.Value;
                                if (AmandsSenseClass.LoadedSprites.ContainsKey("icon_weapons_mg.png"))
                                {
                                    sprite = AmandsSenseClass.LoadedSprites["icon_weapons_mg.png"];
                                    useNewSize = true;
                                }
                                break;
                            case "marksmanRifle":
                                eSenseItemType = ESenseItemType.MarksmanRifles;
                                color = AmandsSensePlugin.MarksmanRiflesColor.Value;
                                if (AmandsSenseClass.LoadedSprites.ContainsKey("icon_weapons_dmr.png"))
                                {
                                    sprite = AmandsSenseClass.LoadedSprites["icon_weapons_dmr.png"];
                                    useNewSize = true;
                                }
                                break;
                            case "pistol":
                                eSenseItemType = ESenseItemType.Pistols;
                                color = AmandsSensePlugin.PistolsColor.Value;
                                if (AmandsSenseClass.LoadedSprites.ContainsKey("icon_weapons_pistols.png"))
                                {
                                    sprite = AmandsSenseClass.LoadedSprites["icon_weapons_pistols.png"];
                                    useNewSize = true;
                                }
                                break;
                            case "smg":
                                eSenseItemType = ESenseItemType.SMGs;
                                color = AmandsSensePlugin.SMGsColor.Value;
                                if (AmandsSenseClass.LoadedSprites.ContainsKey("icon_weapons_smg.png"))
                                {
                                    sprite = AmandsSenseClass.LoadedSprites["icon_weapons_smg.png"];
                                    useNewSize = true;
                                }
                                break;
                            case "shotgun":
                                eSenseItemType = ESenseItemType.Shotguns;
                                color = AmandsSensePlugin.ShotgunsColor.Value;
                                if (AmandsSenseClass.LoadedSprites.ContainsKey("icon_weapons_shotguns.png"))
                                {
                                    sprite = AmandsSenseClass.LoadedSprites["icon_weapons_shotguns.png"];
                                    useNewSize = true;
                                }
                                break;
                            case "specialWeapon":
                                eSenseItemType = ESenseItemType.SpecialWeapons;
                                color = AmandsSensePlugin.SpecialWeaponsColor.Value;
                                if (AmandsSenseClass.LoadedSprites.ContainsKey("icon_weapons_special.png"))
                                {
                                    sprite = AmandsSenseClass.LoadedSprites["icon_weapons_special.png"];
                                    useNewSize = true;
                                }
                                break;
                        }
                    }
                }
                if (eSenseItemType == ESenseItemType.All)
                {
                    eSenseItemType = ESenseItemType.ObservedLootItem;
                }
                AmandsSenseClass.onItemsSensesAdded(Id, observedLootItem.Item.TemplateId, observedLootItem.Item.CanSellOnRagfair, gameObject.transform.position, eSenseItemType);
                switch (eSenseItemType)
                {
                    case ESenseItemType.ObservedLootItem:
                        color = AmandsSensePlugin.ObservedLootItemColor.Value;
                        if (AmandsSenseClass.LoadedSprites.ContainsKey("ObservedLootItem.png"))
                        {
                            sprite = AmandsSenseClass.LoadedSprites["ObservedLootItem.png"];
                        }
                        break;
                    case ESenseItemType.Others:
                        color = AmandsSensePlugin.OthersColor.Value;
                        if (AmandsSenseClass.LoadedSprites.ContainsKey("icon_barter_others.png"))
                        {
                            sprite = AmandsSenseClass.LoadedSprites["icon_barter_others.png"];
                            useNewSize = true;
                        }
                        break;
                    case ESenseItemType.BuildingMaterials:
                        color = AmandsSensePlugin.BuildingMaterialsColor.Value;
                        if (AmandsSenseClass.LoadedSprites.ContainsKey("icon_barter_building.png"))
                        {
                            sprite = AmandsSenseClass.LoadedSprites["icon_barter_building.png"];
                            useNewSize = true;
                        }
                        break;
                    case ESenseItemType.Electronics:
                        color = AmandsSensePlugin.ElectronicsColor.Value;
                        if (AmandsSenseClass.LoadedSprites.ContainsKey("icon_barter_electronics.png"))
                        {
                            sprite = AmandsSenseClass.LoadedSprites["icon_barter_electronics.png"];
                            useNewSize = true;
                        }
                        break;
                    case ESenseItemType.EnergyElements:
                        color = AmandsSensePlugin.EnergyElementsColor.Value;
                        if (AmandsSenseClass.LoadedSprites.ContainsKey("icon_barter_energy.png"))
                        {
                            sprite = AmandsSenseClass.LoadedSprites["icon_barter_energy.png"];
                            useNewSize = true;
                        }
                        break;
                    case ESenseItemType.FlammableMaterials:
                        color = AmandsSensePlugin.FlammableMaterialsColor.Value;
                        if (AmandsSenseClass.LoadedSprites.ContainsKey("icon_barter_flammable.png"))
                        {
                            sprite = AmandsSenseClass.LoadedSprites["icon_barter_flammable.png"];
                            useNewSize = true;
                        }
                        break;
                    case ESenseItemType.HouseholdMaterials:
                        color = AmandsSensePlugin.HouseholdMaterialsColor.Value;
                        if (AmandsSenseClass.LoadedSprites.ContainsKey("icon_barter_household.png"))
                        {
                            sprite = AmandsSenseClass.LoadedSprites["icon_barter_household.png"];
                            useNewSize = true;
                        }
                        break;
                    case ESenseItemType.MedicalSupplies:
                        color = AmandsSensePlugin.MedicalSuppliesColor.Value;
                        if (AmandsSenseClass.LoadedSprites.ContainsKey("icon_barter_medical.png"))
                        {
                            sprite = AmandsSenseClass.LoadedSprites["icon_barter_medical.png"];
                            useNewSize = true;
                        }
                        break;
                    case ESenseItemType.Tools:
                        color = AmandsSensePlugin.ToolsColor.Value;
                        if (AmandsSenseClass.LoadedSprites.ContainsKey("icon_barter_tools.png"))
                        {
                            sprite = AmandsSenseClass.LoadedSprites["icon_barter_tools.png"];
                            useNewSize = true;
                        }
                        break;
                    case ESenseItemType.Valuables:
                        color = AmandsSensePlugin.ValuablesColor.Value;
                        if (AmandsSenseClass.LoadedSprites.ContainsKey("icon_barter_valuables.png"))
                        {
                            sprite = AmandsSenseClass.LoadedSprites["icon_barter_valuables.png"];
                            useNewSize = true;
                        }
                        break;
                    case ESenseItemType.Backpacks:
                        color = AmandsSensePlugin.BackpacksColor.Value;
                        if (AmandsSenseClass.LoadedSprites.ContainsKey("icon_gear_backpacks.png"))
                        {
                            sprite = AmandsSenseClass.LoadedSprites["icon_gear_backpacks.png"];
                            useNewSize = true;
                        }
                        break;
                    case ESenseItemType.BodyArmor:
                        color = AmandsSensePlugin.BodyArmorColor.Value;
                        if (AmandsSenseClass.LoadedSprites.ContainsKey("icon_gear_armor.png"))
                        {
                            sprite = AmandsSenseClass.LoadedSprites["icon_gear_armor.png"];
                            useNewSize = true;
                        }
                        break;
                    case ESenseItemType.Eyewear:
                        color = AmandsSensePlugin.EyewearColor.Value;
                        if (AmandsSenseClass.LoadedSprites.ContainsKey("icon_gear_visors.png"))
                        {
                            sprite = AmandsSenseClass.LoadedSprites["icon_gear_visors.png"];
                            useNewSize = true;
                        }
                        break;
                    case ESenseItemType.Facecovers:
                        color = AmandsSensePlugin.FacecoversColor.Value;
                        if (AmandsSenseClass.LoadedSprites.ContainsKey("icon_gear_facecovers.png"))
                        {
                            sprite = AmandsSenseClass.LoadedSprites["icon_gear_facecovers.png"];
                            useNewSize = true;
                        }
                        break;
                    case ESenseItemType.GearComponents:
                        color = AmandsSensePlugin.GearComponentsColor.Value;
                        if (AmandsSenseClass.LoadedSprites.ContainsKey("icon_gear_components.png"))
                        {
                            sprite = AmandsSenseClass.LoadedSprites["icon_gear_components.png"];
                            useNewSize = true;
                        }
                        break;
                    case ESenseItemType.Headgear:
                        color = AmandsSensePlugin.HeadgearColor.Value;
                        if (AmandsSenseClass.LoadedSprites.ContainsKey("icon_gear_headwear.png"))
                        {
                            sprite = AmandsSenseClass.LoadedSprites["icon_gear_headwear.png"];
                            useNewSize = true;
                        }
                        break;
                    case ESenseItemType.Headsets:
                        color = AmandsSensePlugin.HeadsetsColor.Value;
                        if (AmandsSenseClass.LoadedSprites.ContainsKey("icon_gear_headsets.png"))
                        {
                            sprite = AmandsSenseClass.LoadedSprites["icon_gear_headsets.png"];
                            useNewSize = true;
                        }
                        break;
                    case ESenseItemType.SecureContainers:
                        color = AmandsSensePlugin.SecureContainersColor.Value;
                        if (AmandsSenseClass.LoadedSprites.ContainsKey("icon_gear_secured.png"))
                        {
                            sprite = AmandsSenseClass.LoadedSprites["icon_gear_secured.png"];
                            useNewSize = true;
                        }
                        break;
                    case ESenseItemType.StorageContainers:
                        color = AmandsSensePlugin.StorageContainersColor.Value;
                        if (AmandsSenseClass.LoadedSprites.ContainsKey("icon_gear_cases.png"))
                        {
                            sprite = AmandsSenseClass.LoadedSprites["icon_gear_cases.png"];
                            useNewSize = true;
                        }
                        break;
                    case ESenseItemType.TacticalRigs:
                        color = AmandsSensePlugin.TacticalRigsColor.Value;
                        if (AmandsSenseClass.LoadedSprites.ContainsKey("icon_gear_rigs.png"))
                        {
                            sprite = AmandsSenseClass.LoadedSprites["icon_gear_rigs.png"];
                            useNewSize = true;
                        }
                        break;
                    case ESenseItemType.FunctionalMods:
                        color = AmandsSensePlugin.FunctionalModsColor.Value;
                        if (AmandsSenseClass.LoadedSprites.ContainsKey("icon_mods_functional.png"))
                        {
                            sprite = AmandsSenseClass.LoadedSprites["icon_mods_functional.png"];
                            useNewSize = true;
                        }
                        break;
                    case ESenseItemType.GearMods:
                        color = AmandsSensePlugin.GearModsColor.Value;
                        if (AmandsSenseClass.LoadedSprites.ContainsKey("icon_mods_gear.png"))
                        {
                            sprite = AmandsSenseClass.LoadedSprites["icon_mods_gear.png"];
                            useNewSize = true;
                        }
                        break;
                    case ESenseItemType.VitalParts:
                        color = AmandsSensePlugin.VitalPartsColor.Value;
                        if (AmandsSenseClass.LoadedSprites.ContainsKey("icon_mods_vital.png"))
                        {
                            sprite = AmandsSenseClass.LoadedSprites["icon_mods_vital.png"];
                            useNewSize = true;
                        }
                        break;
                    case ESenseItemType.MeleeWeapons:
                        color = AmandsSensePlugin.MeleeWeaponsColor.Value;
                        if (AmandsSenseClass.LoadedSprites.ContainsKey("icon_weapons_melee.png"))
                        {
                            sprite = AmandsSenseClass.LoadedSprites["icon_weapons_melee.png"];
                            useNewSize = true;
                        }
                        break;
                    case ESenseItemType.Throwables:
                        color = AmandsSensePlugin.ThrowablesColor.Value;
                        if (AmandsSenseClass.LoadedSprites.ContainsKey("icon_weapons_throw.png"))
                        {
                            sprite = AmandsSenseClass.LoadedSprites["icon_weapons_throw.png"];
                            useNewSize = true;
                        }
                        break;
                    case ESenseItemType.AmmoPacks:
                        color = AmandsSensePlugin.AmmoPacksColor.Value;
                        if (AmandsSenseClass.LoadedSprites.ContainsKey("icon_ammo_boxes.png"))
                        {
                            sprite = AmandsSenseClass.LoadedSprites["icon_ammo_boxes.png"];
                            useNewSize = true;
                        }
                        break;
                    case ESenseItemType.Rounds:
                        color = AmandsSensePlugin.RoundsColor.Value;
                        if (AmandsSenseClass.LoadedSprites.ContainsKey("icon_ammo_rounds.png"))
                        {
                            sprite = AmandsSenseClass.LoadedSprites["icon_ammo_rounds.png"];
                            useNewSize = true;
                        }
                        break;
                    case ESenseItemType.Drinks:
                        color = AmandsSensePlugin.DrinksColor.Value;
                        if (AmandsSenseClass.LoadedSprites.ContainsKey("icon_provisions_drinks.png"))
                        {
                            sprite = AmandsSenseClass.LoadedSprites["icon_provisions_drinks.png"];
                            useNewSize = true;
                        }
                        break;
                    case ESenseItemType.Food:
                        color = AmandsSensePlugin.FoodColor.Value;
                        if (AmandsSenseClass.LoadedSprites.ContainsKey("icon_provisions_food.png"))
                        {
                            sprite = AmandsSenseClass.LoadedSprites["icon_provisions_food.png"];
                            useNewSize = true;
                        }
                        break;
                    case ESenseItemType.Injectors:
                        color = AmandsSensePlugin.InjectorsColor.Value;
                        if (AmandsSenseClass.LoadedSprites.ContainsKey("icon_medical_injectors.png"))
                        {
                            sprite = AmandsSenseClass.LoadedSprites["icon_medical_injectors.png"];
                            useNewSize = true;
                        }
                        break;
                    case ESenseItemType.InjuryTreatment:
                        color = AmandsSensePlugin.InjuryTreatmentColor.Value;
                        if (AmandsSenseClass.LoadedSprites.ContainsKey("icon_medical_injury.png"))
                        {
                            sprite = AmandsSenseClass.LoadedSprites["icon_medical_injury.png"];
                            useNewSize = true;
                        }
                        break;
                    case ESenseItemType.Medkits:
                        color = AmandsSensePlugin.MedkitsColor.Value;
                        if (AmandsSenseClass.LoadedSprites.ContainsKey("icon_medical_medkits.png"))
                        {
                            sprite = AmandsSenseClass.LoadedSprites["icon_medical_medkits.png"];
                            useNewSize = true;
                        }
                        break;
                    case ESenseItemType.Pills:
                        color = AmandsSensePlugin.PillsColor.Value;
                        if (AmandsSenseClass.LoadedSprites.ContainsKey("icon_medical_pills.png"))
                        {
                            sprite = AmandsSenseClass.LoadedSprites["icon_medical_pills.png"];
                            useNewSize = true;
                        }
                        break;
                    case ESenseItemType.ElectronicKeys:
                        color = AmandsSensePlugin.ElectronicKeysColor.Value;
                        if (AmandsSenseClass.LoadedSprites.ContainsKey("icon_keys_electronic.png"))
                        {
                            sprite = AmandsSenseClass.LoadedSprites["icon_keys_electronic.png"];
                            useNewSize = true;
                        }
                        break;
                    case ESenseItemType.MechanicalKeys:
                        color = AmandsSensePlugin.MechanicalKeysColor.Value;
                        if (AmandsSenseClass.LoadedSprites.ContainsKey("icon_keys_mechanic.png"))
                        {
                            sprite = AmandsSenseClass.LoadedSprites["icon_keys_mechanic.png"];
                            useNewSize = true;
                        }
                        break;
                    case ESenseItemType.InfoItems:
                        if (observedLootItem.Item.QuestItem)
                        {
                            color = AmandsSensePlugin.QuestItemsColor.Value;
                        }
                        else
                        {
                            color = AmandsSensePlugin.InfoItemsColor.Value;
                        }
                        if (AmandsSenseClass.LoadedSprites.ContainsKey("icon_info.png"))
                        {
                            sprite = AmandsSenseClass.LoadedSprites["icon_info.png"];
                            useNewSize = true;
                        }
                        break;
                    case ESenseItemType.SpecialEquipment:
                        color = AmandsSensePlugin.SpecialEquipmentColor.Value;
                        if (AmandsSenseClass.LoadedSprites.ContainsKey("icon_spec.png"))
                        {
                            sprite = AmandsSenseClass.LoadedSprites["icon_spec.png"];
                            useNewSize = true;
                        }
                        break;
                    case ESenseItemType.Maps:
                        color = AmandsSensePlugin.MapsColor.Value;
                        if (AmandsSenseClass.LoadedSprites.ContainsKey("icon_maps.png"))
                        {
                            sprite = AmandsSenseClass.LoadedSprites["icon_maps.png"];
                            useNewSize = true;
                        }
                        break;
                    case ESenseItemType.Money:
                        color = AmandsSensePlugin.MoneyColor.Value;
                        if (AmandsSenseClass.LoadedSprites.ContainsKey("icon_money.png"))
                        {
                            sprite = AmandsSenseClass.LoadedSprites["icon_money.png"];
                            useNewSize = true;
                        }
                        break;
                }
                if (AmandsSenseClass.itemsJsonClass != null)
                {
                    if (AmandsSenseClass.itemsJsonClass.KappaItems != null)
                    {
                        if (AmandsSenseClass.itemsJsonClass.KappaItems.Contains(observedLootItem.Item.TemplateId))
                        {
                            color = AmandsSensePlugin.KappaItemsColor.Value;
                        }
                    }
                    if (!observedLootItem.Item.CanSellOnRagfair && !AmandsSenseClass.itemsJsonClass.NonFleaExclude.Contains(observedLootItem.Item.TemplateId))
                    {
                        color = AmandsSensePlugin.NonFleaItemsColor.Value;
                    }
                    if (AmandsSenseClass.localPlayer != null && AmandsSenseClass.localPlayer.Profile != null && AmandsSenseClass.localPlayer.Profile.WishList != null && AmandsSenseClass.localPlayer.Profile.WishList.Contains(observedLootItem.Item.TemplateId))
                    {
                        color = AmandsSensePlugin.WishListItemsColor.Value;
                    }
                    if (AmandsSenseClass.itemsJsonClass.RareItems != null)
                    {
                        if (AmandsSenseClass.itemsJsonClass.RareItems.Contains(observedLootItem.Item.TemplateId))
                        {
                            color = AmandsSensePlugin.RareItemsColor.Value;
                        }
                    }
                }
                ItemSound = observedLootItem.Item.ItemSound;
                spriteRenderer = gameObject.AddComponent<SpriteRenderer>();
                if (spriteRenderer != null)
                {
                    light = gameObject.AddComponent<Light>();
                    if (light != null)
                    {
                        light.color = new Color(color.r, color.g, color.b, 1f);
                        light.shadows = LightShadows.None;
                        light.intensity = 0f;
                        light.range = AmandsSensePlugin.LightRange.Value;
                    }
                    spriteRenderer.sprite = sprite;
                    spriteRenderer.color = new Color(color.r, color.g, color.b, 0f);
                    transform.LookAt(Camera.main.transform.position, Vector3.up);
                    transform.localScale = (useNewSize ? AmandsSensePlugin.NewSize.Value : AmandsSensePlugin.Size.Value) * Mathf.Min(AmandsSensePlugin.SizeClamp.Value, Vector3.Distance(Camera.main.transform.position, transform.position));
                    UpdateOpacity = true;
                    AudioClip itemClip = Singleton<GUISounds>.Instance.GetItemClip(ItemSound, EInventorySoundType.pickup);
                    if (itemClip != null)
                    {
                        Singleton<BetterAudio>.Instance.PlayAtPoint(transform.position, itemClip, AmandsSensePlugin.AudioDistance.Value, BetterAudio.AudioSourceGroupType.Character, AmandsSensePlugin.AudioRolloff.Value, AmandsSensePlugin.AudioVolume.Value, EOcclusionTest.Regular);
                    }
                    await Task.Delay((int)(AmandsSensePlugin.Duration.Value * 1000));
                    UpdateOpacity = true;
                    StartOpacity = false;
                }
                else
                {
                    AmandsSenseClass.onItemsSensesRemove(Id);
                    AmandsSenseClass.ItemsSenses.Remove(Id);
                    Destroy(gameObject);
                }
            }
            else
            {
                AmandsSenseClass.onItemsSensesRemove(Id);
                AmandsSenseClass.ItemsSenses.Remove(Id);
                Destroy(gameObject);
            }
        }
        public void Update()
        {
            if (Camera.main != null)
            {
                transform.LookAt(Camera.main.transform.position, Vector3.up);
                transform.localScale = (useNewSize ? AmandsSensePlugin.NewSize.Value : AmandsSensePlugin.Size.Value) * Mathf.Min(AmandsSensePlugin.SizeClamp.Value, Vector3.Distance(Camera.main.transform.position, transform.position));
            }
            if (UpdateOpacity)
            {
                if (StartOpacity)
                {
                    Opacity += AmandsSensePlugin.OpacitySpeed.Value * Time.deltaTime;
                    if (Opacity >= 1f)
                    {
                        UpdateOpacity = false;
                        StartOpacity = false;
                    }
                }
                else
                {
                    Opacity -= AmandsSensePlugin.OpacitySpeed.Value * Time.deltaTime;
                    if (Opacity <= 0f)
                    {
                        UpdateOpacity = false;
                        AmandsSenseClass.ItemsSenses.Remove(Id);
                        Destroy(gameObject);
                    }
                }
                if (spriteRenderer != null && light != null)
                {
                    spriteRenderer.color = new Color(color.r, color.g, color.b, color.a * Opacity);
                    light.intensity = AmandsSensePlugin.LightIntensity.Value * Opacity;
                }
            }
        }
    }
    public class AmandsSenseContainer : MonoBehaviour
    {
        public LootableContainer lootableContainer;
        public bool emptyLootableContainer = false;
        public int itemCount = 0;
        public string Id;
        public SpriteRenderer spriteRenderer;
        public Sprite sprite;
        public Light light;
        public Color color = AmandsSensePlugin.LootableContainerColor.Value;
        public bool useNewSize = false;

        public float Delay = 0f;
        private bool UpdateOpacity = false;
        private bool StartOpacity = true;
        private float Opacity = 0f;

        public void Start()
        {
            if (AmandsSenseClass.ItemsSenses.Contains(Id))
            {
                Destroy(gameObject);
            }
            else
            {
                AmandsSenseClass.ItemsSenses.Add(Id);
                WaitAndStart();
            }
        }
        private async void WaitAndStart()
        {
            await Task.Delay((int)(Delay * 1000));
            if (lootableContainer != null && lootableContainer.gameObject.activeSelf && AmandsSenseClass.localPlayer != null && lootableContainer.transform.position.y > AmandsSenseClass.localPlayer.Position.y + AmandsSensePlugin.MinHeight.Value && lootableContainer.transform.position.y < AmandsSenseClass.localPlayer.Position.y + AmandsSensePlugin.MaxHeight.Value)
            {
                BoxCollider boxCollider = lootableContainer.gameObject.GetComponent<BoxCollider>();
                if (boxCollider != null)
                {
                    Vector3 position = boxCollider.transform.TransformPoint(boxCollider.center);
                    gameObject.transform.position = new Vector3(position.x, boxCollider.ClosestPoint(position + (Vector3.up * 100f)).y + AmandsSensePlugin.NormalSize.Value, position.z);
                }
                else
                {
                    gameObject.transform.position = lootableContainer.transform.position + (Vector3.up * AmandsSensePlugin.NormalSize.Value);
                }
                ESenseItemColor eSenseItemColor = ESenseItemColor.Default;
                if (lootableContainer.ItemOwner != null && AmandsSenseClass.itemsJsonClass != null && AmandsSenseClass.itemsJsonClass.RareItems != null && AmandsSenseClass.itemsJsonClass.KappaItems != null && AmandsSenseClass.itemsJsonClass.NonFleaExclude != null && AmandsSenseClass.localPlayer.Profile != null && AmandsSenseClass.localPlayer.Profile.WishList != null)
                {
                    LootItemClass lootItemClass = lootableContainer.ItemOwner.RootItem as LootItemClass;
                    if (lootItemClass != null)
                    {
                        object[] Grids = Traverse.Create(lootItemClass).Field("Grids").GetValue<object[]>();
                        if (Grids != null)
                        {
                            foreach (object grid in Grids)
                            {
                                IEnumerable<Item> Items = Traverse.Create(grid).Property("Items").GetValue<IEnumerable<Item>>();
                                if (Items != null)
                                {
                                    foreach (Item item in Items)
                                    {
                                        itemCount += 1;
                                        if (AmandsSenseClass.itemsJsonClass.RareItems.Contains(item.TemplateId))
                                        {
                                            eSenseItemColor = ESenseItemColor.Rare;
                                        }
                                        else if (AmandsSenseClass.localPlayer.Profile.WishList.Contains(item.TemplateId) && eSenseItemColor != ESenseItemColor.Rare)
                                        {
                                            eSenseItemColor = ESenseItemColor.WishList;
                                        }
                                        else if (item.Template != null && !item.Template.CanSellOnRagfair && !AmandsSenseClass.itemsJsonClass.NonFleaExclude.Contains(item.TemplateId) && eSenseItemColor != ESenseItemColor.Rare && eSenseItemColor != ESenseItemColor.WishList)
                                        {
                                            if (!AmandsSensePlugin.NonFleaAmmo.Value && TemplateIdToObjectMappingsClass.TypeTable["5485a8684bdc2da71d8b4567"].IsAssignableFrom(item.GetType()))
                                            {
                                                continue;
                                            }
                                            else
                                            {
                                                eSenseItemColor = ESenseItemColor.NonFlea;
                                            }
                                        }
                                        else if (AmandsSenseClass.itemsJsonClass.KappaItems.Contains(item.TemplateId) && eSenseItemColor == ESenseItemColor.Default)
                                        {
                                            eSenseItemColor = ESenseItemColor.Kappa;
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
                if (itemCount != 0)
                {
                    AmandsSenseClass.onContainerSensesAdded(Id, gameObject.transform.position);
                    if (AmandsSenseClass.LoadedSprites.ContainsKey("LootableContainer.png"))
                    {
                        sprite = AmandsSenseClass.LoadedSprites["LootableContainer.png"];
                    }
                    switch (eSenseItemColor)
                    {
                        case ESenseItemColor.Kappa:
                            color = AmandsSensePlugin.KappaItemsColor.Value;
                            break;
                        case ESenseItemColor.NonFlea:
                            color = AmandsSensePlugin.NonFleaItemsColor.Value;
                            if (AmandsSenseClass.LoadedSprites.ContainsKey("icon_barter.png"))
                            {
                                sprite = AmandsSenseClass.LoadedSprites["icon_barter.png"];
                                useNewSize = true;
                            }
                            break;
                        case ESenseItemColor.WishList:
                            color = AmandsSensePlugin.WishListItemsColor.Value;
                            if (AmandsSenseClass.LoadedSprites.ContainsKey("icon_fav_checked.png"))
                            {
                                sprite = AmandsSenseClass.LoadedSprites["icon_fav_checked.png"];
                                useNewSize = true;
                            }
                            break;
                        case ESenseItemColor.Rare:
                            color = AmandsSensePlugin.RareItemsColor.Value;
                            break;
                    }
                    spriteRenderer = gameObject.AddComponent<SpriteRenderer>();
                    if (spriteRenderer != null)
                    {
                        light = gameObject.AddComponent<Light>();
                        if (light != null)
                        {
                            light.color = new Color(color.r, color.g, color.b, 1f);
                            light.shadows = LightShadows.None;
                            light.intensity = 0f;
                            light.range = AmandsSensePlugin.LightRange.Value;
                        }
                        spriteRenderer.sprite = sprite;
                        spriteRenderer.color = new Color(color.r, color.g, color.b, 0f);
                        transform.LookAt(Camera.main.transform.position, Vector3.up);
                        transform.localScale = (useNewSize ? AmandsSensePlugin.NewSize.Value : AmandsSensePlugin.Size.Value) * Mathf.Min(AmandsSensePlugin.SizeClamp.Value, Vector3.Distance(Camera.main.transform.position, transform.position));
                        UpdateOpacity = true;
                        if (lootableContainer.OpenSound.Length > 0)
                        {
                            AudioClip OpenSound = lootableContainer.OpenSound[0];
                            if (OpenSound != null)
                            {
                                Singleton<BetterAudio>.Instance.PlayAtPoint(transform.position, OpenSound, AmandsSensePlugin.AudioDistance.Value, BetterAudio.AudioSourceGroupType.Character, AmandsSensePlugin.AudioRolloff.Value, AmandsSensePlugin.AudioVolume.Value, EOcclusionTest.Regular);
                            }
                        }
                        await Task.Delay((int)(AmandsSensePlugin.Duration.Value * 1000));
                        UpdateOpacity = true;
                        StartOpacity = false;
                    }
                    else
                    {
                        AmandsSenseClass.ItemsSenses.Remove(Id);
                        Destroy(gameObject);
                    }
                }
                else
                {
                    AmandsSenseClass.ItemsSenses.Remove(Id);
                    Destroy(gameObject);
                }
            }
            else
            {
                AmandsSenseClass.ItemsSenses.Remove(Id);
                Destroy(gameObject);
            }
        }
        public void Update()
        {
            if (Camera.main != null)
            {
                transform.LookAt(Camera.main.transform.position, Vector3.up);
                transform.localScale = (useNewSize ? AmandsSensePlugin.NewSize.Value : AmandsSensePlugin.Size.Value) * Mathf.Min(AmandsSensePlugin.SizeClamp.Value, Vector3.Distance(Camera.main.transform.position, transform.position));
            }
            if (UpdateOpacity)
            {
                if (StartOpacity)
                {
                    Opacity += AmandsSensePlugin.OpacitySpeed.Value * Time.deltaTime;
                    if (Opacity >= 1f)
                    {
                        UpdateOpacity = false;
                        StartOpacity = false;
                    }
                }
                else
                {
                    Opacity -= AmandsSensePlugin.OpacitySpeed.Value * Time.deltaTime;
                    if (Opacity <= 0f)
                    {
                        UpdateOpacity = false;
                        AmandsSenseClass.ItemsSenses.Remove(Id);
                        Destroy(gameObject);
                    }
                }
                if (spriteRenderer != null && light != null)
                {
                    spriteRenderer.color = new Color(color.r, color.g, color.b, color.a * Opacity);
                    light.intensity = AmandsSensePlugin.LightIntensity.Value * Opacity;
                }
            }
        }
    }
    public class AmandsSenseDeadbody : MonoBehaviour
    {
        public Corpse corpse;
        public BodyPartCollider bodyPartCollider;
        public bool emptyCorpse = true;
        public string Id;
        public SpriteRenderer spriteRenderer;
        public Sprite sprite;
        public Light light;
        public Color color = AmandsSensePlugin.LootableContainerColor.Value;
        public bool useNewSize = false;

        public float Delay = 0f;
        private bool UpdateOpacity = false;
        private bool StartOpacity = true;
        private float Opacity = 0f;

        public void Start()
        {
            if (AmandsSenseClass.ItemsSenses.Contains(Id))
            {
                Destroy(gameObject);
            }
            else
            {
                AmandsSenseClass.ItemsSenses.Add(Id);
                WaitAndStart();
            }
        }
        private async void WaitAndStart()
        {
            await Task.Delay((int)(Delay * 1000));
            if (corpse != null && corpse.gameObject.activeSelf && bodyPartCollider != null && bodyPartCollider.gameObject.activeSelf && bodyPartCollider.Collider != null && AmandsSenseClass.localPlayer != null && bodyPartCollider.Collider.transform.position.y > AmandsSenseClass.localPlayer.Position.y + AmandsSensePlugin.MinHeight.Value && bodyPartCollider.Collider.transform.position.y < AmandsSenseClass.localPlayer.Position.y + AmandsSensePlugin.MaxHeight.Value)
            {
                gameObject.transform.position = bodyPartCollider.Collider.transform.position + (Vector3.up * AmandsSensePlugin.NormalSize.Value) + (Vector3.up * 0.5f);
                ESenseItemColor eSenseItemColor = ESenseItemColor.Default;
                if (AmandsSenseClass.itemsJsonClass != null && AmandsSenseClass.itemsJsonClass.RareItems != null && AmandsSenseClass.itemsJsonClass.KappaItems != null && AmandsSenseClass.itemsJsonClass.NonFleaExclude != null && AmandsSenseClass.localPlayer != null && AmandsSenseClass.localPlayer.Profile != null && AmandsSenseClass.localPlayer.Profile.WishList != null)
                {
                    LocalPlayer localPlayer = corpse.gameObject.GetComponent<LocalPlayer>();
                    if (localPlayer != null && localPlayer.Profile != null)
                    {
                        object Inventory = Traverse.Create(localPlayer.Profile).Field("Inventory").GetValue();
                        if (Inventory != null)
                        {
                            IEnumerable<Item> AllRealPlayerItems = Traverse.Create(Inventory).Property("AllRealPlayerItems").GetValue<IEnumerable<Item>>();
                            if (AllRealPlayerItems != null)
                            {
                                foreach (Item item in AllRealPlayerItems)
                                {
                                    if (item.Parent != null)
                                    {
                                        if (item.Parent.Container != null && item.Parent.Container.ParentItem != null && TemplateIdToObjectMappingsClass.TypeTable["5448bf274bdc2dfc2f8b456a"].IsAssignableFrom(item.Parent.Container.ParentItem.GetType()))
                                        {
                                            continue;
                                        }
                                        Slot slot = item.Parent.Container as Slot;
                                        if (slot != null)
                                        {
                                            if (slot.Name == "Dogtag")
                                            {
                                                continue;
                                            }
                                            if (slot.Name == "SecuredContainer")
                                            {
                                                continue;
                                            }
                                            if (slot.Name == "Scabbard")
                                            {
                                                continue;
                                            }
                                            if (slot.Name == "ArmBand")
                                            {
                                                continue;
                                            }
                                        }
                                    }
                                    if (emptyCorpse)
                                    {
                                        emptyCorpse = false;
                                    }
                                    if (AmandsSenseClass.itemsJsonClass.RareItems.Contains(item.TemplateId))
                                    {
                                        eSenseItemColor = ESenseItemColor.Rare;
                                    }
                                    else if (AmandsSenseClass.localPlayer.Profile.WishList.Contains(item.TemplateId) && eSenseItemColor != ESenseItemColor.Rare)
                                    {
                                        eSenseItemColor = ESenseItemColor.WishList;
                                    }
                                    else if (item.Template != null && !item.Template.CanSellOnRagfair && !AmandsSenseClass.itemsJsonClass.NonFleaExclude.Contains(item.TemplateId) && eSenseItemColor != ESenseItemColor.Rare && eSenseItemColor != ESenseItemColor.WishList)
                                    {
                                        if (!AmandsSensePlugin.NonFleaAmmo.Value && TemplateIdToObjectMappingsClass.TypeTable["5485a8684bdc2da71d8b4567"].IsAssignableFrom(item.GetType()))
                                        {
                                            continue;
                                        }
                                        else
                                        {
                                            eSenseItemColor = ESenseItemColor.NonFlea;
                                        }
                                    }
                                    else if (AmandsSenseClass.itemsJsonClass.KappaItems.Contains(item.TemplateId) && eSenseItemColor == ESenseItemColor.Default)
                                    {
                                        eSenseItemColor = ESenseItemColor.Kappa;
                                    }
                                }
                            }
                        }
                    }
                }
                if (!emptyCorpse)
                {
                    switch (corpse.Side)
                    {
                        case EPlayerSide.Usec:
                            if (AmandsSenseClass.LoadedSprites.ContainsKey("Usec.png"))
                            {
                                sprite = AmandsSenseClass.LoadedSprites["Usec.png"];
                            }
                            break;
                        case EPlayerSide.Bear:
                            if (AmandsSenseClass.LoadedSprites.ContainsKey("Bear.png"))
                            {
                                sprite = AmandsSenseClass.LoadedSprites["Bear.png"];
                            }
                            break;
                        case EPlayerSide.Savage:
                            if (AmandsSenseClass.LoadedSprites.ContainsKey("icon_kills_big.png"))
                            {
                                sprite = AmandsSenseClass.LoadedSprites["icon_kills_big.png"];
                                useNewSize = true;
                            }
                            break;
                    }
                    AmandsSenseClass.onContainerSensesAdded(Id, gameObject.transform.position);
                    switch (eSenseItemColor)
                    {
                        case ESenseItemColor.Kappa:
                            color = AmandsSensePlugin.KappaItemsColor.Value;
                            break;
                        case ESenseItemColor.NonFlea:
                            color = AmandsSensePlugin.NonFleaItemsColor.Value;
                            if (AmandsSenseClass.LoadedSprites.ContainsKey("icon_barter.png"))
                            {
                                sprite = AmandsSenseClass.LoadedSprites["icon_barter.png"];
                                useNewSize = true;
                            }
                            break;
                        case ESenseItemColor.WishList:
                            color = AmandsSensePlugin.WishListItemsColor.Value;
                            if (AmandsSenseClass.LoadedSprites.ContainsKey("icon_fav_checked.png"))
                            {
                                sprite = AmandsSenseClass.LoadedSprites["icon_fav_checked.png"];
                                useNewSize = true;
                            }
                            break;
                        case ESenseItemColor.Rare:
                            color = AmandsSensePlugin.RareItemsColor.Value;
                            break;
                    }
                    spriteRenderer = gameObject.AddComponent<SpriteRenderer>();
                    if (spriteRenderer != null)
                    {
                        light = gameObject.AddComponent<Light>();
                        if (light != null)
                        {
                            light.color = new Color(color.r, color.g, color.b, 1f);
                            light.shadows = LightShadows.None;
                            light.intensity = 0f;
                            light.range = AmandsSensePlugin.LightRange.Value;
                        }
                        spriteRenderer.sprite = sprite;
                        spriteRenderer.color = new Color(color.r, color.g, color.b, 0f);
                        transform.LookAt(Camera.main.transform.position, Vector3.up);
                        transform.localScale = (useNewSize ? AmandsSensePlugin.NewSize.Value : AmandsSensePlugin.Size.Value) * Mathf.Min(AmandsSensePlugin.SizeClamp.Value, Vector3.Distance(Camera.main.transform.position, transform.position));
                        UpdateOpacity = true;
                        await Task.Delay((int)(AmandsSensePlugin.Duration.Value * 1000));
                        UpdateOpacity = true;
                        StartOpacity = false;
                    }
                    else
                    {
                        AmandsSenseClass.ItemsSenses.Remove(Id);
                        Destroy(gameObject);
                    }
                }
                else
                {
                    AmandsSenseClass.ItemsSenses.Remove(Id);
                    Destroy(gameObject);
                }
            }
            else
            {
                AmandsSenseClass.ItemsSenses.Remove(Id);
                Destroy(gameObject);
            }
        }
        public void Update()
        {
            if (Camera.main != null)
            {
                transform.LookAt(Camera.main.transform.position, Vector3.up);
                transform.localScale = (useNewSize ? AmandsSensePlugin.NewSize.Value : AmandsSensePlugin.Size.Value) * Mathf.Min(AmandsSensePlugin.SizeClamp.Value, Vector3.Distance(Camera.main.transform.position, transform.position));
            }
            if (UpdateOpacity)
            {
                if (StartOpacity)
                {
                    Opacity += AmandsSensePlugin.OpacitySpeed.Value * Time.deltaTime;
                    if (Opacity >= 1f)
                    {
                        UpdateOpacity = false;
                        StartOpacity = false;
                    }
                }
                else
                {
                    Opacity -= AmandsSensePlugin.OpacitySpeed.Value * Time.deltaTime;
                    if (Opacity <= 0f)
                    {
                        UpdateOpacity = false;
                        AmandsSenseClass.ItemsSenses.Remove(Id);
                        Destroy(gameObject);
                    }
                }
                if (spriteRenderer != null && light != null)
                {
                    spriteRenderer.color = new Color(color.r, color.g, color.b, color.a * Opacity);
                    light.intensity = AmandsSensePlugin.LightIntensity.Value * Opacity;
                }
            }
        }
    }
    public class ItemsJsonClass
    {
        public List<string> RareItems { get; set; }
        public List<string> KappaItems { get; set; }
        public List<string> NonFleaExclude { get; set; }
    }
    public class AmandsSenseAlwaysOn : MonoBehaviour
    {
        public Collider collider;
        public ObservedLootItem observedLootItem;
        public string Id;
        public SpriteRenderer spriteRenderer;
        public Sprite sprite;
        public Light light;
        public Color color = AmandsSensePlugin.ObservedLootItemColor.Value;

        public float Delay = 0f;
        public bool UpdateOpacity = false;
        public bool StartOpacity = true;
        private float Opacity = 0f;

        public void Start()
        {
            if (AmandsSenseClass.ItemsAlwaysOn.Contains(this))
            {
                Destroy(gameObject);
            }
            else
            {
                AmandsSenseClass.ItemsAlwaysOn.Add(this);
                WaitAndStart();
            }
        }
        private async void WaitAndStart()
        {
            await Task.Delay((int)(UnityEngine.Random.Range(0.0f, 1f) * 1000));
            if (gameObject != null && observedLootItem != null && observedLootItem.gameObject.activeSelf && observedLootItem.Item != null && AmandsSenseClass.localPlayer != null)
            {
                if (AmandsSenseClass.LoadedSprites.ContainsKey("ObservedLootItem.png"))
                {
                    sprite = AmandsSenseClass.LoadedSprites["ObservedLootItem.png"];
                }
                BoxCollider boxCollider = observedLootItem.gameObject.GetComponent<BoxCollider>();
                if (boxCollider != null)
                {
                    Vector3 position = boxCollider.transform.TransformPoint(boxCollider.center);
                    gameObject.transform.position = new Vector3(position.x, boxCollider.ClosestPoint(position + (Vector3.up * 100f)).y + AmandsSensePlugin.NormalSize.Value, position.z);
                }
                else
                {
                    gameObject.transform.position = observedLootItem.transform.position + (Vector3.up * AmandsSensePlugin.NormalSize.Value);
                }
                ESenseItemType eSenseItemType = ESenseItemType.ObservedLootItem;
                eSenseItemType = AmandsSenseClass.SenseItemType(observedLootItem.Item.GetType());
                if (typeof(Weapon).IsAssignableFrom(observedLootItem.Item.GetType()))
                {
                    Weapon weapon = observedLootItem.Item as Weapon;
                    if (weapon != null)
                    {
                        switch (weapon.WeapClass)
                        {
                            case "assaultCarbine":
                                eSenseItemType = ESenseItemType.AssaultCarbines;
                                color = AmandsSensePlugin.AssaultCarbinesColor.Value;
                                if (AmandsSenseClass.LoadedSprites.ContainsKey("icon_weapons_carbines.png"))
                                {
                                    sprite = AmandsSenseClass.LoadedSprites["icon_weapons_carbines.png"];
                                }
                                break;
                            case "assaultRifle":
                                eSenseItemType = ESenseItemType.AssaultRifles;
                                color = AmandsSensePlugin.AssaultRiflesColor.Value;
                                if (AmandsSenseClass.LoadedSprites.ContainsKey("icon_weapons_assaultrifles.png"))
                                {
                                    sprite = AmandsSenseClass.LoadedSprites["icon_weapons_assaultrifles.png"];
                                }
                                break;
                            case "sniperRifle":
                                eSenseItemType = ESenseItemType.BoltActionRifles;
                                color = AmandsSensePlugin.BoltActionRiflesColor.Value;
                                if (AmandsSenseClass.LoadedSprites.ContainsKey("icon_weapons_botaction.png"))
                                {
                                    sprite = AmandsSenseClass.LoadedSprites["icon_weapons_botaction.png"];
                                }
                                break;
                            case "grenadeLauncher":
                                eSenseItemType = ESenseItemType.GrenadeLaunchers;
                                color = AmandsSensePlugin.GrenadeLaunchersColor.Value;
                                if (AmandsSenseClass.LoadedSprites.ContainsKey("icon_weapons_gl.png"))
                                {
                                    sprite = AmandsSenseClass.LoadedSprites["icon_weapons_gl.png"];
                                }
                                break;
                            case "machinegun":
                                eSenseItemType = ESenseItemType.MachineGuns;
                                color = AmandsSensePlugin.MachineGunsColor.Value;
                                if (AmandsSenseClass.LoadedSprites.ContainsKey("icon_weapons_mg.png"))
                                {
                                    sprite = AmandsSenseClass.LoadedSprites["icon_weapons_mg.png"];
                                }
                                break;
                            case "marksmanRifle":
                                eSenseItemType = ESenseItemType.MarksmanRifles;
                                color = AmandsSensePlugin.MarksmanRiflesColor.Value;
                                if (AmandsSenseClass.LoadedSprites.ContainsKey("icon_weapons_dmr.png"))
                                {
                                    sprite = AmandsSenseClass.LoadedSprites["icon_weapons_dmr.png"];
                                }
                                break;
                            case "pistol":
                                eSenseItemType = ESenseItemType.Pistols;
                                color = AmandsSensePlugin.PistolsColor.Value;
                                if (AmandsSenseClass.LoadedSprites.ContainsKey("icon_weapons_pistols.png"))
                                {
                                    sprite = AmandsSenseClass.LoadedSprites["icon_weapons_pistols.png"];
                                }
                                break;
                            case "smg":
                                eSenseItemType = ESenseItemType.SMGs;
                                color = AmandsSensePlugin.SMGsColor.Value;
                                if (AmandsSenseClass.LoadedSprites.ContainsKey("icon_weapons_smg.png"))
                                {
                                    sprite = AmandsSenseClass.LoadedSprites["icon_weapons_smg.png"];
                                }
                                break;
                            case "shotgun":
                                eSenseItemType = ESenseItemType.Shotguns;
                                color = AmandsSensePlugin.ShotgunsColor.Value;
                                if (AmandsSenseClass.LoadedSprites.ContainsKey("icon_weapons_shotguns.png"))
                                {
                                    sprite = AmandsSenseClass.LoadedSprites["icon_weapons_shotguns.png"];
                                }
                                break;
                            case "specialWeapon":
                                eSenseItemType = ESenseItemType.SpecialWeapons;
                                color = AmandsSensePlugin.SpecialWeaponsColor.Value;
                                if (AmandsSenseClass.LoadedSprites.ContainsKey("icon_weapons_special.png"))
                                {
                                    sprite = AmandsSenseClass.LoadedSprites["icon_weapons_special.png"];
                                }
                                break;
                        }
                    }
                }
                if (eSenseItemType == ESenseItemType.All)
                {
                    eSenseItemType = ESenseItemType.ObservedLootItem;
                }
                switch (eSenseItemType)
                {
                    case ESenseItemType.ObservedLootItem:
                        color = AmandsSensePlugin.ObservedLootItemColor.Value;
                        if (AmandsSenseClass.LoadedSprites.ContainsKey("ObservedLootItem.png"))
                        {
                            sprite = AmandsSenseClass.LoadedSprites["ObservedLootItem.png"];
                        }
                        break;
                    case ESenseItemType.Others:
                        color = AmandsSensePlugin.OthersColor.Value;
                        if (AmandsSenseClass.LoadedSprites.ContainsKey("icon_barter_others.png"))
                        {
                            sprite = AmandsSenseClass.LoadedSprites["icon_barter_others.png"];
                        }
                        break;
                    case ESenseItemType.BuildingMaterials:
                        color = AmandsSensePlugin.BuildingMaterialsColor.Value;
                        if (AmandsSenseClass.LoadedSprites.ContainsKey("icon_barter_building.png"))
                        {
                            sprite = AmandsSenseClass.LoadedSprites["icon_barter_building.png"];
                        }
                        break;
                    case ESenseItemType.Electronics:
                        color = AmandsSensePlugin.ElectronicsColor.Value;
                        if (AmandsSenseClass.LoadedSprites.ContainsKey("icon_barter_electronics.png"))
                        {
                            sprite = AmandsSenseClass.LoadedSprites["icon_barter_electronics.png"];
                        }
                        break;
                    case ESenseItemType.EnergyElements:
                        color = AmandsSensePlugin.EnergyElementsColor.Value;
                        if (AmandsSenseClass.LoadedSprites.ContainsKey("icon_barter_energy.png"))
                        {
                            sprite = AmandsSenseClass.LoadedSprites["icon_barter_energy.png"];
                        }
                        break;
                    case ESenseItemType.FlammableMaterials:
                        color = AmandsSensePlugin.FlammableMaterialsColor.Value;
                        if (AmandsSenseClass.LoadedSprites.ContainsKey("icon_barter_flammable.png"))
                        {
                            sprite = AmandsSenseClass.LoadedSprites["icon_barter_flammable.png"];
                        }
                        break;
                    case ESenseItemType.HouseholdMaterials:
                        color = AmandsSensePlugin.HouseholdMaterialsColor.Value;
                        if (AmandsSenseClass.LoadedSprites.ContainsKey("icon_barter_household.png"))
                        {
                            sprite = AmandsSenseClass.LoadedSprites["icon_barter_household.png"];
                        }
                        break;
                    case ESenseItemType.MedicalSupplies:
                        color = AmandsSensePlugin.MedicalSuppliesColor.Value;
                        if (AmandsSenseClass.LoadedSprites.ContainsKey("icon_barter_medical.png"))
                        {
                            sprite = AmandsSenseClass.LoadedSprites["icon_barter_medical.png"];
                        }
                        break;
                    case ESenseItemType.Tools:
                        color = AmandsSensePlugin.ToolsColor.Value;
                        if (AmandsSenseClass.LoadedSprites.ContainsKey("icon_barter_tools.png"))
                        {
                            sprite = AmandsSenseClass.LoadedSprites["icon_barter_tools.png"];
                        }
                        break;
                    case ESenseItemType.Valuables:
                        color = AmandsSensePlugin.ValuablesColor.Value;
                        if (AmandsSenseClass.LoadedSprites.ContainsKey("icon_barter_valuables.png"))
                        {
                            sprite = AmandsSenseClass.LoadedSprites["icon_barter_valuables.png"];
                        }
                        break;
                    case ESenseItemType.Backpacks:
                        color = AmandsSensePlugin.BackpacksColor.Value;
                        if (AmandsSenseClass.LoadedSprites.ContainsKey("icon_gear_backpacks.png"))
                        {
                            sprite = AmandsSenseClass.LoadedSprites["icon_gear_backpacks.png"];
                        }
                        break;
                    case ESenseItemType.BodyArmor:
                        color = AmandsSensePlugin.BodyArmorColor.Value;
                        if (AmandsSenseClass.LoadedSprites.ContainsKey("icon_gear_armor.png"))
                        {
                            sprite = AmandsSenseClass.LoadedSprites["icon_gear_armor.png"];
                        }
                        break;
                    case ESenseItemType.Eyewear:
                        color = AmandsSensePlugin.EyewearColor.Value;
                        if (AmandsSenseClass.LoadedSprites.ContainsKey("icon_gear_visors.png"))
                        {
                            sprite = AmandsSenseClass.LoadedSprites["icon_gear_visors.png"];
                        }
                        break;
                    case ESenseItemType.Facecovers:
                        color = AmandsSensePlugin.FacecoversColor.Value;
                        if (AmandsSenseClass.LoadedSprites.ContainsKey("icon_gear_facecovers.png"))
                        {
                            sprite = AmandsSenseClass.LoadedSprites["icon_gear_facecovers.png"];
                        }
                        break;
                    case ESenseItemType.GearComponents:
                        color = AmandsSensePlugin.GearComponentsColor.Value;
                        if (AmandsSenseClass.LoadedSprites.ContainsKey("icon_gear_components.png"))
                        {
                            sprite = AmandsSenseClass.LoadedSprites["icon_gear_components.png"];
                        }
                        break;
                    case ESenseItemType.Headgear:
                        color = AmandsSensePlugin.HeadgearColor.Value;
                        if (AmandsSenseClass.LoadedSprites.ContainsKey("icon_gear_headwear.png"))
                        {
                            sprite = AmandsSenseClass.LoadedSprites["icon_gear_headwear.png"];
                        }
                        break;
                    case ESenseItemType.Headsets:
                        color = AmandsSensePlugin.HeadsetsColor.Value;
                        if (AmandsSenseClass.LoadedSprites.ContainsKey("icon_gear_headsets.png"))
                        {
                            sprite = AmandsSenseClass.LoadedSprites["icon_gear_headsets.png"];
                        }
                        break;
                    case ESenseItemType.SecureContainers:
                        color = AmandsSensePlugin.SecureContainersColor.Value;
                        if (AmandsSenseClass.LoadedSprites.ContainsKey("icon_gear_secured.png"))
                        {
                            sprite = AmandsSenseClass.LoadedSprites["icon_gear_secured.png"];
                        }
                        break;
                    case ESenseItemType.StorageContainers:
                        color = AmandsSensePlugin.StorageContainersColor.Value;
                        if (AmandsSenseClass.LoadedSprites.ContainsKey("icon_gear_cases.png"))
                        {
                            sprite = AmandsSenseClass.LoadedSprites["icon_gear_cases.png"];
                        }
                        break;
                    case ESenseItemType.TacticalRigs:
                        color = AmandsSensePlugin.TacticalRigsColor.Value;
                        if (AmandsSenseClass.LoadedSprites.ContainsKey("icon_gear_rigs.png"))
                        {
                            sprite = AmandsSenseClass.LoadedSprites["icon_gear_rigs.png"];
                        }
                        break;
                    case ESenseItemType.FunctionalMods:
                        color = AmandsSensePlugin.FunctionalModsColor.Value;
                        if (AmandsSenseClass.LoadedSprites.ContainsKey("icon_mods_functional.png"))
                        {
                            sprite = AmandsSenseClass.LoadedSprites["icon_mods_functional.png"];
                        }
                        break;
                    case ESenseItemType.GearMods:
                        color = AmandsSensePlugin.GearModsColor.Value;
                        if (AmandsSenseClass.LoadedSprites.ContainsKey("icon_mods_gear.png"))
                        {
                            sprite = AmandsSenseClass.LoadedSprites["icon_mods_gear.png"];
                        }
                        break;
                    case ESenseItemType.VitalParts:
                        color = AmandsSensePlugin.VitalPartsColor.Value;
                        if (AmandsSenseClass.LoadedSprites.ContainsKey("icon_mods_vital.png"))
                        {
                            sprite = AmandsSenseClass.LoadedSprites["icon_mods_vital.png"];
                        }
                        break;
                    case ESenseItemType.MeleeWeapons:
                        color = AmandsSensePlugin.MeleeWeaponsColor.Value;
                        if (AmandsSenseClass.LoadedSprites.ContainsKey("icon_weapons_melee.png"))
                        {
                            sprite = AmandsSenseClass.LoadedSprites["icon_weapons_melee.png"];
                        }
                        break;
                    case ESenseItemType.Throwables:
                        color = AmandsSensePlugin.ThrowablesColor.Value;
                        if (AmandsSenseClass.LoadedSprites.ContainsKey("icon_weapons_throw.png"))
                        {
                            sprite = AmandsSenseClass.LoadedSprites["icon_weapons_throw.png"];
                        }
                        break;
                    case ESenseItemType.AmmoPacks:
                        color = AmandsSensePlugin.AmmoPacksColor.Value;
                        if (AmandsSenseClass.LoadedSprites.ContainsKey("icon_ammo_boxes.png"))
                        {
                            sprite = AmandsSenseClass.LoadedSprites["icon_ammo_boxes.png"];
                        }
                        break;
                    case ESenseItemType.Rounds:
                        color = AmandsSensePlugin.RoundsColor.Value;
                        if (AmandsSenseClass.LoadedSprites.ContainsKey("icon_ammo_rounds.png"))
                        {
                            sprite = AmandsSenseClass.LoadedSprites["icon_ammo_rounds.png"];
                        }
                        break;
                    case ESenseItemType.Drinks:
                        color = AmandsSensePlugin.DrinksColor.Value;
                        if (AmandsSenseClass.LoadedSprites.ContainsKey("icon_provisions_drinks.png"))
                        {
                            sprite = AmandsSenseClass.LoadedSprites["icon_provisions_drinks.png"];
                        }
                        break;
                    case ESenseItemType.Food:
                        color = AmandsSensePlugin.FoodColor.Value;
                        if (AmandsSenseClass.LoadedSprites.ContainsKey("icon_provisions_food.png"))
                        {
                            sprite = AmandsSenseClass.LoadedSprites["icon_provisions_food.png"];
                        }
                        break;
                    case ESenseItemType.Injectors:
                        color = AmandsSensePlugin.InjectorsColor.Value;
                        if (AmandsSenseClass.LoadedSprites.ContainsKey("icon_medical_injectors.png"))
                        {
                            sprite = AmandsSenseClass.LoadedSprites["icon_medical_injectors.png"];
                        }
                        break;
                    case ESenseItemType.InjuryTreatment:
                        color = AmandsSensePlugin.InjuryTreatmentColor.Value;
                        if (AmandsSenseClass.LoadedSprites.ContainsKey("icon_medical_injury.png"))
                        {
                            sprite = AmandsSenseClass.LoadedSprites["icon_medical_injury.png"];
                        }
                        break;
                    case ESenseItemType.Medkits:
                        color = AmandsSensePlugin.MedkitsColor.Value;
                        if (AmandsSenseClass.LoadedSprites.ContainsKey("icon_medical_medkits.png"))
                        {
                            sprite = AmandsSenseClass.LoadedSprites["icon_medical_medkits.png"];
                        }
                        break;
                    case ESenseItemType.Pills:
                        color = AmandsSensePlugin.PillsColor.Value;
                        if (AmandsSenseClass.LoadedSprites.ContainsKey("icon_medical_pills.png"))
                        {
                            sprite = AmandsSenseClass.LoadedSprites["icon_medical_pills.png"];
                        }
                        break;
                    case ESenseItemType.ElectronicKeys:
                        color = AmandsSensePlugin.ElectronicKeysColor.Value;
                        if (AmandsSenseClass.LoadedSprites.ContainsKey("icon_keys_electronic.png"))
                        {
                            sprite = AmandsSenseClass.LoadedSprites["icon_keys_electronic.png"];
                        }
                        break;
                    case ESenseItemType.MechanicalKeys:
                        color = AmandsSensePlugin.MechanicalKeysColor.Value;
                        if (AmandsSenseClass.LoadedSprites.ContainsKey("icon_keys_mechanic.png"))
                        {
                            sprite = AmandsSenseClass.LoadedSprites["icon_keys_mechanic.png"];
                        }
                        break;
                    case ESenseItemType.InfoItems:
                        if (observedLootItem.Item.QuestItem)
                        {
                            color = AmandsSensePlugin.QuestItemsColor.Value;
                        }
                        else
                        {
                            color = AmandsSensePlugin.InfoItemsColor.Value;
                        }
                        if (AmandsSenseClass.LoadedSprites.ContainsKey("icon_info.png"))
                        {
                            sprite = AmandsSenseClass.LoadedSprites["icon_info.png"];
                        }
                        break;
                    case ESenseItemType.SpecialEquipment:
                        color = AmandsSensePlugin.SpecialEquipmentColor.Value;
                        if (AmandsSenseClass.LoadedSprites.ContainsKey("icon_spec.png"))
                        {
                            sprite = AmandsSenseClass.LoadedSprites["icon_spec.png"];
                        }
                        break;
                    case ESenseItemType.Maps:
                        color = AmandsSensePlugin.MapsColor.Value;
                        if (AmandsSenseClass.LoadedSprites.ContainsKey("icon_maps.png"))
                        {
                            sprite = AmandsSenseClass.LoadedSprites["icon_maps.png"];
                        }
                        break;
                    case ESenseItemType.Money:
                        color = AmandsSensePlugin.MoneyColor.Value;
                        if (AmandsSenseClass.LoadedSprites.ContainsKey("icon_money.png"))
                        {
                            sprite = AmandsSenseClass.LoadedSprites["icon_money.png"];
                        }
                        break;
                }
                if (AmandsSenseClass.itemsJsonClass != null)
                {
                    if (AmandsSenseClass.itemsJsonClass.KappaItems != null)
                    {
                        if (AmandsSenseClass.itemsJsonClass.KappaItems.Contains(observedLootItem.Item.TemplateId))
                        {
                            color = AmandsSensePlugin.KappaItemsColor.Value;
                        }
                    }
                    if (!observedLootItem.Item.CanSellOnRagfair && !AmandsSenseClass.itemsJsonClass.NonFleaExclude.Contains(observedLootItem.Item.TemplateId))
                    {
                        color = AmandsSensePlugin.NonFleaItemsColor.Value;
                    }
                    if (AmandsSenseClass.localPlayer != null && AmandsSenseClass.localPlayer.Profile != null && AmandsSenseClass.localPlayer.Profile.WishList != null && AmandsSenseClass.localPlayer.Profile.WishList.Contains(observedLootItem.Item.TemplateId))
                    {
                        color = AmandsSensePlugin.WishListItemsColor.Value;
                    }
                    if (AmandsSenseClass.itemsJsonClass.RareItems != null)
                    {
                        if (AmandsSenseClass.itemsJsonClass.RareItems.Contains(observedLootItem.Item.TemplateId))
                        {
                            color = AmandsSensePlugin.RareItemsColor.Value;
                        }
                    }
                }
                spriteRenderer = gameObject.AddComponent<SpriteRenderer>();
                if (spriteRenderer != null)
                {
                    light = gameObject.AddComponent<Light>();
                    if (light != null)
                    {
                        light.color = new Color(color.r, color.g, color.b, 1f);
                        light.shadows = LightShadows.None;
                        light.intensity = 0.0f;
                        light.range = AmandsSensePlugin.LightRange.Value;
                    }
                    spriteRenderer.sprite = sprite;
                    spriteRenderer.color = new Color(color.r, color.g, color.b, 0f);
                    transform.LookAt(Camera.main.transform.position, Vector3.up);
                    transform.localScale = AmandsSensePlugin.AlwaysOnSize.Value;
                    UpdateOpacity = true;
                }
                else
                {
                    AmandsSenseClass.ItemsAlwaysOn.Remove(this);
                    Destroy(gameObject);
                }
            }
            else
            {
                AmandsSenseClass.ItemsAlwaysOn.Remove(this);
                Destroy(gameObject);
            }
        }
        public void Update()
        {
            if (Camera.main != null)
            {
                transform.LookAt(Camera.main.transform.position, Vector3.up);
            }
            if (UpdateOpacity)
            {
                if (StartOpacity)
                {
                    Opacity += AmandsSensePlugin.OpacitySpeed.Value * Time.deltaTime;
                    if (Opacity >= 1f)
                    {
                        UpdateOpacity = false;
                        StartOpacity = false;
                    }
                }
                else
                {
                    Opacity -= AmandsSensePlugin.OpacitySpeed.Value * Time.deltaTime;
                    if (Opacity <= 0f)
                    {
                        UpdateOpacity = false;
                        AmandsSenseClass.ItemsAlwaysOn.Remove(this);
                        Destroy(gameObject);
                    }
                }
                if (spriteRenderer != null && light != null)
                {
                    spriteRenderer.color = new Color(color.r, color.g, color.b, color.a * Opacity * 0.5f);
                    light.intensity = AmandsSensePlugin.LightIntensity.Value * Opacity * 0.5f;
                }
            }
        }
    }
    public class AmandsSenseContainerAlwaysOn : MonoBehaviour
    {
        public Collider collider;
        public LootableContainer lootableContainer;
        public bool emptyLootableContainer = false;
        public int itemCount = 0;
        public string Id;
        public SpriteRenderer spriteRenderer;
        public Sprite sprite;
        public Light light;
        public Color color = AmandsSensePlugin.LootableContainerColor.Value;

        public float Delay = 0f;
        public bool UpdateOpacity = false;
        public bool StartOpacity = true;
        private float Opacity = 0f;

        public void Start()
        {
            if (AmandsSenseClass.ContainersAlwaysOn.Contains(this))
            {
                Destroy(gameObject);
            }
            else
            {
                AmandsSenseClass.ContainersAlwaysOn.Add(this);
                WaitAndStart();
            }
        }
        private async void WaitAndStart()
        {
            await Task.Delay((int)(UnityEngine.Random.Range(0.0f, 1f) * 1000));
            if (gameObject != null && lootableContainer != null && lootableContainer.gameObject.activeSelf && AmandsSenseClass.localPlayer != null)
            {
                BoxCollider boxCollider = lootableContainer.gameObject.GetComponent<BoxCollider>();
                if (boxCollider != null)
                {
                    Vector3 position = boxCollider.transform.TransformPoint(boxCollider.center);
                    gameObject.transform.position = new Vector3(position.x, boxCollider.ClosestPoint(position + (Vector3.up * 100f)).y + AmandsSensePlugin.NormalSize.Value, position.z);
                }
                else
                {
                    gameObject.transform.position = lootableContainer.transform.position + (Vector3.up * AmandsSensePlugin.NormalSize.Value);
                }
                ESenseItemColor eSenseItemColor = ESenseItemColor.Default;
                if (lootableContainer.ItemOwner != null && AmandsSenseClass.itemsJsonClass != null && AmandsSenseClass.itemsJsonClass.RareItems != null && AmandsSenseClass.itemsJsonClass.KappaItems != null && AmandsSenseClass.itemsJsonClass.NonFleaExclude != null && AmandsSenseClass.localPlayer.Profile != null && AmandsSenseClass.localPlayer.Profile.WishList != null)
                {
                    LootItemClass lootItemClass = lootableContainer.ItemOwner.RootItem as LootItemClass;
                    if (lootItemClass != null)
                    {
                        object[] Grids = Traverse.Create(lootItemClass).Field("Grids").GetValue<object[]>();
                        if (Grids != null)
                        {
                            foreach (object grid in Grids)
                            {
                                IEnumerable<Item> Items = Traverse.Create(grid).Property("Items").GetValue<IEnumerable<Item>>();
                                if (Items != null)
                                {
                                    foreach (Item item in Items)
                                    {
                                        itemCount += 1;
                                        if (AmandsSenseClass.itemsJsonClass.RareItems.Contains(item.TemplateId))
                                        {
                                            eSenseItemColor = ESenseItemColor.Rare;
                                        }
                                        else if (AmandsSenseClass.localPlayer.Profile.WishList.Contains(item.TemplateId) && eSenseItemColor != ESenseItemColor.Rare)
                                        {
                                            eSenseItemColor = ESenseItemColor.WishList;
                                        }
                                        else if (item.Template != null && !item.Template.CanSellOnRagfair && !AmandsSenseClass.itemsJsonClass.NonFleaExclude.Contains(item.TemplateId) && eSenseItemColor != ESenseItemColor.Rare && eSenseItemColor != ESenseItemColor.WishList)
                                        {
                                            if (!AmandsSensePlugin.NonFleaAmmo.Value && TemplateIdToObjectMappingsClass.TypeTable["5485a8684bdc2da71d8b4567"].IsAssignableFrom(item.GetType()))
                                            {
                                                continue;
                                            }
                                            else
                                            {
                                                eSenseItemColor = ESenseItemColor.NonFlea;
                                            }
                                        }
                                        else if (AmandsSenseClass.itemsJsonClass.KappaItems.Contains(item.TemplateId) && eSenseItemColor == ESenseItemColor.Default)
                                        {
                                            eSenseItemColor = ESenseItemColor.Kappa;
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
                if (itemCount != 0)
                {
                    if (AmandsSenseClass.LoadedSprites.ContainsKey("LootableContainer.png"))
                    {
                        sprite = AmandsSenseClass.LoadedSprites["LootableContainer.png"];
                    }
                    switch (eSenseItemColor)
                    {
                        case ESenseItemColor.Kappa:
                            color = AmandsSensePlugin.KappaItemsColor.Value;
                            break;
                        case ESenseItemColor.NonFlea:
                            color = AmandsSensePlugin.NonFleaItemsColor.Value;
                            if (AmandsSenseClass.LoadedSprites.ContainsKey("icon_barter.png"))
                            {
                                sprite = AmandsSenseClass.LoadedSprites["icon_barter.png"];
                            }
                            break;
                        case ESenseItemColor.WishList:
                            color = AmandsSensePlugin.WishListItemsColor.Value;
                            if (AmandsSenseClass.LoadedSprites.ContainsKey("icon_fav_checked.png"))
                            {
                                sprite = AmandsSenseClass.LoadedSprites["icon_fav_checked.png"];
                            }
                            break;
                        case ESenseItemColor.Rare:
                            color = AmandsSensePlugin.RareItemsColor.Value;
                            break;
                    }
                    spriteRenderer = gameObject.AddComponent<SpriteRenderer>();
                    if (spriteRenderer != null)
                    {
                        light = gameObject.AddComponent<Light>();
                        if (light != null)
                        {
                            light.color = new Color(color.r, color.g, color.b, 1f);
                            light.shadows = LightShadows.None;
                            light.intensity = 0.0f;
                            light.range = AmandsSensePlugin.LightRange.Value;
                        }
                        spriteRenderer.sprite = sprite;
                        spriteRenderer.color = new Color(color.r, color.g, color.b, 0f);
                        transform.LookAt(Camera.main.transform.position, Vector3.up);
                        transform.localScale = AmandsSensePlugin.AlwaysOnSize.Value;
                        UpdateOpacity = true;
                    }
                    else
                    {
                        AmandsSenseClass.ContainersAlwaysOn.Remove(this);
                        Destroy(gameObject);
                    }
                }
                else
                {
                    AmandsSenseClass.ContainersAlwaysOn.Remove(this);
                    Destroy(gameObject);
                }
            }
            else
            {
                AmandsSenseClass.ContainersAlwaysOn.Remove(this);
                Destroy(gameObject);
            }
        }
        public void Update()
        {
            if (Camera.main != null)
            {
                transform.LookAt(Camera.main.transform.position, Vector3.up);
            }
            if (UpdateOpacity)
            {
                if (StartOpacity)
                {
                    Opacity += AmandsSensePlugin.OpacitySpeed.Value * Time.deltaTime;
                    if (Opacity >= 1f)
                    {
                        UpdateOpacity = false;
                        StartOpacity = false;
                    }
                }
                else
                {
                    Opacity -= AmandsSensePlugin.OpacitySpeed.Value * Time.deltaTime;
                    if (Opacity <= 0f)
                    {
                        UpdateOpacity = false;
                        AmandsSenseClass.ContainersAlwaysOn.Remove(this);
                        Destroy(gameObject);
                    }
                }
                if (spriteRenderer != null && light != null)
                {
                    spriteRenderer.color = new Color(color.r, color.g, color.b, color.a * Opacity * 0.5f);
                    light.intensity = AmandsSensePlugin.LightIntensity.Value * Opacity * 0.5f;
                }
            }
        }
    }
    public class AmandsSenseDeadbodyAlwaysOn : MonoBehaviour
    {
        public Collider collider;
        public Corpse corpse;
        public BodyPartCollider bodyPartCollider;
        public bool emptyCorpse = true;
        public string Id;
        public SpriteRenderer spriteRenderer;
        public Sprite sprite;
        public Light light;
        public Color color = AmandsSensePlugin.LootableContainerColor.Value;

        public float Delay = 0f;
        public bool UpdateOpacity = false;
        public bool StartOpacity = true;
        private float Opacity = 0f;

        public void Start()
        {
            if (AmandsSenseClass.DeadbodyAlwaysOn.Contains(this))
            {
                Destroy(gameObject);
            }
            else
            {
                AmandsSenseClass.DeadbodyAlwaysOn.Add(this);
                WaitAndStart();
            }
        }
        private async void WaitAndStart()
        {
            await Task.Delay((int)(UnityEngine.Random.Range(0.0f, 1f) * 1000));
            if (gameObject != null && corpse != null && corpse.gameObject.activeSelf && bodyPartCollider != null && bodyPartCollider.gameObject.activeSelf && bodyPartCollider.Collider != null && AmandsSenseClass.localPlayer != null)
            {
                gameObject.transform.position = bodyPartCollider.Collider.transform.position + (Vector3.up * AmandsSensePlugin.NormalSize.Value) + (Vector3.up * 0.5f);
                ESenseItemColor eSenseItemColor = ESenseItemColor.Default;
                if (AmandsSenseClass.itemsJsonClass != null && AmandsSenseClass.itemsJsonClass.RareItems != null && AmandsSenseClass.itemsJsonClass.KappaItems != null && AmandsSenseClass.itemsJsonClass.NonFleaExclude != null && AmandsSenseClass.localPlayer != null && AmandsSenseClass.localPlayer.Profile != null && AmandsSenseClass.localPlayer.Profile.WishList != null)
                {
                    LocalPlayer localPlayer = corpse.gameObject.GetComponent<LocalPlayer>();
                    if (localPlayer != null && localPlayer.Profile != null)
                    {
                        object Inventory = Traverse.Create(localPlayer.Profile).Field("Inventory").GetValue();
                        if (Inventory != null)
                        {
                            IEnumerable<Item> AllRealPlayerItems = Traverse.Create(Inventory).Property("AllRealPlayerItems").GetValue<IEnumerable<Item>>();
                            if (AllRealPlayerItems != null)
                            {
                                foreach (Item item in AllRealPlayerItems)
                                {
                                    if (item.Parent != null)
                                    {
                                        if (item.Parent.Container != null && item.Parent.Container.ParentItem != null && TemplateIdToObjectMappingsClass.TypeTable["5448bf274bdc2dfc2f8b456a"].IsAssignableFrom(item.Parent.Container.ParentItem.GetType()))
                                        {
                                            continue;
                                        }
                                        Slot slot = item.Parent.Container as Slot;
                                        if (slot != null)
                                        {
                                            if (slot.Name == "Dogtag")
                                            {
                                                continue;
                                            }
                                            if (slot.Name == "SecuredContainer")
                                            {
                                                continue;
                                            }
                                            if (slot.Name == "Scabbard")
                                            {
                                                continue;
                                            }
                                            if (slot.Name == "ArmBand")
                                            {
                                                continue;
                                            }
                                        }
                                    }
                                    if (emptyCorpse)
                                    {
                                        emptyCorpse = false;
                                    }
                                    if (AmandsSenseClass.itemsJsonClass.RareItems.Contains(item.TemplateId))
                                    {
                                        eSenseItemColor = ESenseItemColor.Rare;
                                    }
                                    else if (AmandsSenseClass.localPlayer.Profile.WishList.Contains(item.TemplateId) && eSenseItemColor != ESenseItemColor.Rare)
                                    {
                                        eSenseItemColor = ESenseItemColor.WishList;
                                    }
                                    else if (item.Template != null && !item.Template.CanSellOnRagfair && !AmandsSenseClass.itemsJsonClass.NonFleaExclude.Contains(item.TemplateId) && eSenseItemColor != ESenseItemColor.Rare && eSenseItemColor != ESenseItemColor.WishList)
                                    {
                                        if (!AmandsSensePlugin.NonFleaAmmo.Value && TemplateIdToObjectMappingsClass.TypeTable["5485a8684bdc2da71d8b4567"].IsAssignableFrom(item.GetType()))
                                        {
                                            continue;
                                        }
                                        else
                                        {
                                            eSenseItemColor = ESenseItemColor.NonFlea;
                                        }
                                    }
                                    else if (AmandsSenseClass.itemsJsonClass.KappaItems.Contains(item.TemplateId) && eSenseItemColor == ESenseItemColor.Default)
                                    {
                                        eSenseItemColor = ESenseItemColor.Kappa;
                                    }
                                }
                            }
                        }
                    }
                }
                if (!emptyCorpse)
                {
                    if (AmandsSenseClass.LoadedSprites.ContainsKey("icon_kills_big.png"))
                    {
                        sprite = AmandsSenseClass.LoadedSprites["icon_kills_big.png"];
                    }
                    switch (corpse.Side)
                    {
                        case EPlayerSide.Usec:
                            if (AmandsSenseClass.LoadedSprites.ContainsKey("Usec.png"))
                            {
                                sprite = AmandsSenseClass.LoadedSprites["Usec.png"];
                            }
                            break;
                        case EPlayerSide.Bear:
                            if (AmandsSenseClass.LoadedSprites.ContainsKey("Bear.png"))
                            {
                                sprite = AmandsSenseClass.LoadedSprites["Bear.png"];
                            }
                            break;
                        case EPlayerSide.Savage:
                            break;
                    }
                    switch (eSenseItemColor)
                    {
                        case ESenseItemColor.Kappa:
                            color = AmandsSensePlugin.KappaItemsColor.Value;
                            break;
                        case ESenseItemColor.NonFlea:
                            color = AmandsSensePlugin.NonFleaItemsColor.Value;
                            if (AmandsSenseClass.LoadedSprites.ContainsKey("icon_barter.png"))
                            {
                                sprite = AmandsSenseClass.LoadedSprites["icon_barter.png"];
                            }
                            break;
                        case ESenseItemColor.WishList:
                            color = AmandsSensePlugin.WishListItemsColor.Value;
                            if (AmandsSenseClass.LoadedSprites.ContainsKey("icon_fav_checked.png"))
                            {
                                sprite = AmandsSenseClass.LoadedSprites["icon_fav_checked.png"];
                            }
                            break;
                        case ESenseItemColor.Rare:
                            color = AmandsSensePlugin.RareItemsColor.Value;
                            break;
                    }
                    spriteRenderer = gameObject.AddComponent<SpriteRenderer>();
                    if (spriteRenderer != null)
                    {
                        light = gameObject.AddComponent<Light>();
                        if (light != null)
                        {
                            light.color = new Color(color.r, color.g, color.b, 1f);
                            light.shadows = LightShadows.None;
                            light.intensity = 0.0f;
                            light.range = AmandsSensePlugin.LightRange.Value;
                        }
                        spriteRenderer.sprite = sprite;
                        spriteRenderer.color = new Color(color.r, color.g, color.b, 0f);
                        transform.LookAt(Camera.main.transform.position, Vector3.up);
                        transform.localScale = AmandsSensePlugin.AlwaysOnSize.Value;
                        UpdateOpacity = true;
                    }
                    else
                    {
                        AmandsSenseClass.DeadbodyAlwaysOn.Remove(this);
                        Destroy(gameObject);
                    }
                }
                else
                {
                    AmandsSenseClass.DeadbodyAlwaysOn.Remove(this);
                    Destroy(gameObject);
                }
            }
            else
            {
                AmandsSenseClass.DeadbodyAlwaysOn.Remove(this);
                Destroy(gameObject);
            }
        }
        public void Update()
        {
            if (Camera.main != null)
            {
                transform.LookAt(Camera.main.transform.position, Vector3.up);
            }
            if (UpdateOpacity)
            {
                if (StartOpacity)
                {
                    Opacity += AmandsSensePlugin.OpacitySpeed.Value * Time.deltaTime;
                    if (Opacity >= 1f)
                    {
                        UpdateOpacity = false;
                        StartOpacity = false;
                    }
                }
                else
                {
                    Opacity -= AmandsSensePlugin.OpacitySpeed.Value * Time.deltaTime;
                    if (Opacity <= 0f)
                    {
                        UpdateOpacity = false;
                        AmandsSenseClass.DeadbodyAlwaysOn.Remove(this);
                        Destroy(gameObject);
                    }
                }
                if (spriteRenderer != null && light != null)
                {
                    spriteRenderer.color = new Color(color.r, color.g, color.b, color.a * Opacity * 0.5f);
                    light.intensity = AmandsSensePlugin.LightIntensity.Value * Opacity * 0.5f;
                }
            }
        }
    }
    public enum ESenseItemType
    {
        All,
        ObservedLootItem,
        Others,
        BuildingMaterials,
        Electronics,
        EnergyElements,
        FlammableMaterials,
        HouseholdMaterials,
        MedicalSupplies,
        Tools,
        Valuables,
        Backpacks,
        BodyArmor,
        Eyewear,
        Facecovers,
        GearComponents,
        Headgear,
        Headsets,
        SecureContainers,
        StorageContainers,
        TacticalRigs,
        FunctionalMods,
        GearMods,
        VitalParts,
        AssaultCarbines,
        AssaultRifles,
        BoltActionRifles,
        GrenadeLaunchers,
        MachineGuns,
        MarksmanRifles,
        MeleeWeapons,
        Pistols,
        SMGs,
        Shotguns,
        SpecialWeapons,
        Throwables,
        AmmoPacks,
        Rounds,
        Drinks,
        Food,
        Injectors,
        InjuryTreatment,
        Medkits,
        Pills,
        ElectronicKeys,
        MechanicalKeys,
        InfoItems,
        QuestItems,
        SpecialEquipment,
        Maps,
        Money
    }
    public enum ESenseItemColor
    {
        Default,
        Kappa,
        NonFlea,
        WishList,
        Rare
    }
}
