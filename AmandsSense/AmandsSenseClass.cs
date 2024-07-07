using UnityEngine;
using System;
using System.Collections.Generic;
using HarmonyLib;
using EFT;
using EFT.InventoryLogic;
using EFT.UI;
using Comfort.Common;
using System.IO;
using System.Threading.Tasks;
using UnityEngine.Networking;
using EFT.Interactive;
using System.Linq;
using SPT.Common.Utils;
using Sirenix.Utilities;
using UnityEngine.UI;
using TMPro;

namespace AmandsSense
{
    public class AmandsSenseClass : MonoBehaviour
    {
        public static Player Player;
        public static InventoryControllerClass inventoryControllerClass;

        public static RaycastHit hit;
        public static LayerMask LowLayerMask;
        public static LayerMask HighLayerMask;
        public static LayerMask FoliageLayerMask;

        public static float CooldownTime = 0f;
        public static float AlwaysOnTime = 0f;
        public static float Radius = 0f;

        public static PrismEffects prismEffects;

        public static ItemsJsonClass itemsJsonClass;

        public static float lastDoubleClickTime = 0.0f;

        public static AudioSource SenseAudioSource;

        public static Dictionary<string, Sprite> LoadedSprites = new Dictionary<string, Sprite>();
        public static Dictionary<string, AudioClip> LoadedAudioClips = new Dictionary<string, AudioClip>();

        public static Vector3[] SenseOverlapLocations = new Vector3[9] { Vector3.zero, Vector3.forward, Vector3.back, Vector3.left, Vector3.right, Vector3.forward + Vector3.left, Vector3.forward + Vector3.right, Vector3.back + Vector3.left, Vector3.back + Vector3.right };
        public static int CurrentOverlapLocation = 9;

        public static LayerMask BoxInteractiveLayerMask;
        public static LayerMask BoxDeadbodyLayerMask;
        public static int[] CurrentOverlapCount = new int[9];
        public static Collider[] CurrentOverlapLoctionColliders = new Collider[100];

        public static Dictionary<int, AmandsSenseWorld> SenseWorlds = new Dictionary<int, AmandsSenseWorld>();

        public static List<SenseDeadPlayerStruct> DeadPlayers = new List<SenseDeadPlayerStruct>();

        public static List<AmandsSenseExfil> SenseExfils = new List<AmandsSenseExfil>();
        public static AmandsSenseExfil ClosestAmandsSenseExfil = null;

        public static List<Item> SenseItems = new List<Item>();

        public static Transform parent;

        public static string scene;
        public void OnGUI()
        {
            /*GUILayout.BeginArea(new Rect(20, 10, 1280, 720));
            GUILayout.Label("SenseWorlds " + SenseWorlds.Count().ToString());
            GUILayout.EndArea();*/
        }
        private void Awake()
        {
            LowLayerMask = LayerMask.GetMask("Terrain", "LowPolyCollider", "HitCollider");
            HighLayerMask = LayerMask.GetMask("Terrain", "HighPolyCollider", "HitCollider");
            FoliageLayerMask = LayerMask.GetMask("Terrain", "HighPolyCollider", "HitCollider", "Foliage");

            BoxInteractiveLayerMask = LayerMask.GetMask("Interactive");
            BoxDeadbodyLayerMask = LayerMask.GetMask("Deadbody");
        }
        public void Start()
        {
            itemsJsonClass = ReadFromJsonFile<ItemsJsonClass>((AppDomain.CurrentDomain.BaseDirectory + "/BepInEx/plugins/Sense/Items.json"));
            ReloadFiles(false);
        }
        public void Update()
        {
            if (gameObject != null && Player != null && AmandsSensePlugin.EnableSense.Value != EEnableSense.Off)
            {
                if (CurrentOverlapLocation <= 8)
                {
                    int CurrentOverlapCountTest = Physics.OverlapBoxNonAlloc(Player.Position + (Vector3)(SenseOverlapLocations[CurrentOverlapLocation] * ((AmandsSensePlugin.Radius.Value * 2f) / 3f)), (Vector3.one * ((AmandsSensePlugin.Radius.Value * 2f) / 3f)), CurrentOverlapLoctionColliders, Quaternion.Euler(0f, 0f, 0f), BoxInteractiveLayerMask, QueryTriggerInteraction.Collide);
                    for (int i = 0; i < CurrentOverlapCountTest; i++)
                    {
                        if (!SenseWorlds.ContainsKey(CurrentOverlapLoctionColliders[i].GetInstanceID()))
                        {
                            GameObject SenseWorldGameObject = new GameObject("SenseWorld");
                            AmandsSenseWorld amandsSenseWorld = SenseWorldGameObject.AddComponent<AmandsSenseWorld>();
                            amandsSenseWorld.OwnerCollider = CurrentOverlapLoctionColliders[i];
                            amandsSenseWorld.OwnerGameObject = amandsSenseWorld.OwnerCollider.gameObject;
                            amandsSenseWorld.Id = amandsSenseWorld.OwnerCollider.GetInstanceID();
                            amandsSenseWorld.Delay = Vector3.Distance(Player.Position, amandsSenseWorld.OwnerCollider.transform.position) / AmandsSensePlugin.Speed.Value;
                            SenseWorlds.Add(amandsSenseWorld.Id, amandsSenseWorld);
                        }
                        else
                        {
                            SenseWorlds[CurrentOverlapLoctionColliders[i].GetInstanceID()].RestartSense();
                        }
                    }
                    CurrentOverlapLocation++;
                }
                else if (AmandsSensePlugin.SenseAlwaysOn.Value)
                {
                    AlwaysOnTime += Time.deltaTime;
                    if (AlwaysOnTime > AmandsSensePlugin.AlwaysOnFrequency.Value)
                    {
                        AlwaysOnTime = 0f;
                        CurrentOverlapLocation = 0;
                        SenseDeadBodies();
                    }
                }
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
                        if (timeSinceLastClick <= 0.5f && CooldownTime >= AmandsSensePlugin.Cooldown.Value)
                        {
                            CooldownTime = 0f;
                            CurrentOverlapLocation = 0;
                            SenseDeadBodies();
                            ShowSenseExfils();
                            if (prismEffects != null)
                            {
                                Radius = 0;
                                prismEffects.useDof = AmandsSensePlugin.useDof.Value;
                            }
                            if (LoadedAudioClips.ContainsKey("Sense.wav"))
                            {
                                SenseAudioSource.PlayOneShot(LoadedAudioClips["Sense.wav"], AmandsSensePlugin.ActivateSenseVolume.Value);
                            }
                        }
                    }
                    else
                    {
                        if (CooldownTime >= AmandsSensePlugin.Cooldown.Value)
                        {
                            CooldownTime = 0f;
                            CurrentOverlapLocation = 0;
                            SenseDeadBodies();
                            ShowSenseExfils();
                            if (prismEffects != null)
                            {
                                Radius = 0;
                                prismEffects.useDof = AmandsSensePlugin.useDof.Value;
                            }
                            if (LoadedAudioClips.ContainsKey("Sense.wav"))
                            {
                                SenseAudioSource.PlayOneShot(LoadedAudioClips["Sense.wav"], AmandsSensePlugin.ActivateSenseVolume.Value);
                            }
                        }
                    }
                }
                if (Radius < Mathf.Max(AmandsSensePlugin.Radius.Value, AmandsSensePlugin.DeadPlayerRadius.Value))
                {
                    Radius += AmandsSensePlugin.Speed.Value * Time.deltaTime;
                    if (prismEffects != null)
                    {
                        prismEffects.dofFocusPoint = Radius - prismEffects.dofFocusDistance;
                        if (prismEffects.dofRadius < 0.5f)
                        {
                            prismEffects.dofRadius += 2f * Time.deltaTime;
                        }
                    }
                }
                else if (prismEffects != null && prismEffects.dofRadius > 0.001f)
                {
                    prismEffects.dofRadius -= 0.5f * Time.deltaTime;
                    if (prismEffects.dofRadius < 0.001f)
                    {
                        prismEffects.useDof = false;
                    }
                }
            }
        }
        public void SenseDeadBodies()
        {
            foreach (SenseDeadPlayerStruct deadPlayer in DeadPlayers)
            {
                if ((Vector3.Distance(Player.Position, deadPlayer.victim.Position)) < AmandsSensePlugin.DeadPlayerRadius.Value)
                {
                    if (!SenseWorlds.ContainsKey(deadPlayer.victim.GetInstanceID()))
                    {
                        GameObject SenseWorldGameObject = new GameObject("SenseWorld");
                        AmandsSenseWorld amandsSenseWorld = SenseWorldGameObject.AddComponent<AmandsSenseWorld>();
                        amandsSenseWorld.OwnerGameObject = deadPlayer.victim.gameObject;
                        amandsSenseWorld.Id = deadPlayer.victim.GetInstanceID();
                        amandsSenseWorld.Delay = Vector3.Distance(Player.Position, deadPlayer.victim.Position) / AmandsSensePlugin.Speed.Value;
                        amandsSenseWorld.Lazy = false;
                        amandsSenseWorld.eSenseWorldType = ESenseWorldType.Deadbody;
                        amandsSenseWorld.SenseDeadPlayer = deadPlayer.victim as LocalPlayer;
                        SenseWorlds.Add(amandsSenseWorld.Id, amandsSenseWorld);
                    }
                    else
                    {
                        SenseWorlds[deadPlayer.victim.GetInstanceID()].RestartSense();
                    }
                }
            }
        }
        public void ShowSenseExfils()
        {
            if (!AmandsSensePlugin.EnableExfilSense.Value) return;

            if (scene == "Factory_Day" || scene == "Factory_Night" || scene == "Laboratory_Scripts") return;

            float ClosestDistance = 10000000000f;
            if (ClosestAmandsSenseExfil != null && ClosestAmandsSenseExfil.light != null) ClosestAmandsSenseExfil.light.shadows = LightShadows.None;
            foreach (AmandsSenseExfil senseExfil in SenseExfils)
            {
                if (Player != null && Vector3.Distance(senseExfil.transform.position,Player.gameObject.transform.position) < ClosestDistance)
                {
                    ClosestAmandsSenseExfil = senseExfil;
                    ClosestDistance = Vector3.Distance(senseExfil.transform.position, Player.gameObject.transform.position);
                }

                if (senseExfil.Intensity > 0.5f)
                {
                    senseExfil.LifeSpan = 0f;
                    senseExfil.UpdateSense();
                }
                else
                {
                    senseExfil.ShowSense();
                }
            }
            if (AmandsSensePlugin.ExfilLightShadows.Value && ClosestAmandsSenseExfil != null && ClosestAmandsSenseExfil.light != null) ClosestAmandsSenseExfil.light.shadows = LightShadows.Hard;
        }
        public static void Clear()
        {
            foreach (KeyValuePair<int,AmandsSenseWorld> keyValuePair in SenseWorlds)
            {
                if (keyValuePair.Value != null) keyValuePair.Value.RemoveSense();
            }
            SenseWorlds.Clear();

            ClosestAmandsSenseExfil = null;
            SenseExfils = SenseExfils.Where(x => x != null).ToList();

            DeadPlayers.Clear();
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
        public static void ReloadFiles(bool onlySounds)
        {
            if (onlySounds) goto OnlySounds;

            string[] Files = Directory.GetFiles(AppDomain.CurrentDomain.BaseDirectory + "/BepInEx/plugins/Sense/images/", "*.png");
            foreach (string File in Files)
            {
                LoadSprite(File);
            }

            OnlySounds:
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
    public class AmandsSenseWorld : MonoBehaviour
    {
        public bool Lazy = true;
        public ESenseWorldType eSenseWorldType = ESenseWorldType.Item;
        public GameObject OwnerGameObject;
        public Collider OwnerCollider;

        public LocalPlayer SenseDeadPlayer;

        public int Id;

        public float Delay;
        public float LifeSpan;

        public bool Waiting = false;
        public bool WaitingRemoveSense = false;
        public bool UpdateIntensity = false;
        public bool Starting = true;
        public float Intensity = 0f;

        public GameObject amandsSenseConstructorGameObject;
        public AmandsSenseConstructor amandsSenseConstructor;

        public void Start()
        {
            enabled = false;
            WaitAndStart();
        }
        private async void WaitAndStart()
        {
            Waiting = true;
            await Task.Delay((int)(Delay * 1000));
            if (WaitingRemoveSense)
            {
                RemoveSense();
                return;
            }

            if (OwnerGameObject == null || (OwnerGameObject != null & !OwnerGameObject.activeSelf))
            {
                RemoveSense();
                return;
            }
            if (Starting)
            {
                if (OwnerGameObject != null)
                {
                    transform.position = OwnerGameObject.transform.position;
                }
                if (HeightCheck())
                {
                    RemoveSense();
                    return;
                }

                enabled = true;
                UpdateIntensity = true;

                amandsSenseConstructorGameObject = new GameObject("Constructor");
                amandsSenseConstructorGameObject.transform.SetParent(gameObject.transform, false);
                amandsSenseConstructorGameObject.transform.localScale = Vector3.one * AmandsSensePlugin.Size.Value;

                if (Lazy)
                {
                    ObservedLootItem observedLootItem = OwnerGameObject.GetComponent<ObservedLootItem>();
                    if (observedLootItem != null)
                    {
                        eSenseWorldType = ESenseWorldType.Item;
                        amandsSenseConstructor = amandsSenseConstructorGameObject.AddComponent<AmandsSenseItem>();
                        amandsSenseConstructor.amandsSenseWorld = this;
                        amandsSenseConstructor.Construct();
                        amandsSenseConstructor.SetSense(observedLootItem);
                    }
                    else
                    {
                        LootableContainer lootableContainer = OwnerGameObject.GetComponent<LootableContainer>();
                        if (lootableContainer != null)
                        {
                            if (lootableContainer.Template == "578f87b7245977356274f2cd")
                            {
                                eSenseWorldType = ESenseWorldType.Drawer;
                                amandsSenseConstructorGameObject.transform.localPosition = new Vector3(-0.08f, 0.05f, 0);
                                amandsSenseConstructorGameObject.transform.localRotation = Quaternion.Euler(90, 0, 0);
                            }
                            else
                            {
                                eSenseWorldType = ESenseWorldType.Container;
                            }

                            amandsSenseConstructor = amandsSenseConstructorGameObject.AddComponent<AmandsSenseContainer>();
                            amandsSenseConstructor.amandsSenseWorld = this;
                            amandsSenseConstructor.Construct();
                            amandsSenseConstructor.SetSense(lootableContainer);
                        }
                        else
                        {
                            RemoveSense();
                            return;
                        }
                    }
                }
                else
                {
                    switch (eSenseWorldType)
                    {
                        case ESenseWorldType.Item:
                            break;
                        case ESenseWorldType.Container:
                            break;
                        case ESenseWorldType.Drawer:
                            break;
                        case ESenseWorldType.Deadbody:
                            amandsSenseConstructor = amandsSenseConstructorGameObject.AddComponent<AmandsSenseDeadPlayer>();
                            amandsSenseConstructor.amandsSenseWorld = this;
                            amandsSenseConstructor.Construct();
                            amandsSenseConstructor.SetSense(SenseDeadPlayer);
                            break;
                    }
                }

                // SenseWorld Starting Posittion
                switch (eSenseWorldType)
                {
                    case ESenseWorldType.Item:
                    case ESenseWorldType.Container:
                        gameObject.transform.position = new Vector3(OwnerCollider.bounds.center.x, OwnerCollider.ClosestPoint(OwnerCollider.bounds.center + (Vector3.up * 10f)).y + AmandsSensePlugin.VerticalOffset.Value, OwnerCollider.bounds.center.z);
                        break;
                    case ESenseWorldType.Drawer:
                        if (OwnerCollider != null)
                        {
                            BoxCollider boxCollider = OwnerCollider as BoxCollider;
                            if (boxCollider != null)
                            {
                                Vector3 position = OwnerCollider.transform.TransformPoint(boxCollider.center);
                                gameObject.transform.position = position;
                                gameObject.transform.rotation = OwnerCollider.transform.rotation;
                            }
                        }
                        break;
                    case ESenseWorldType.Deadbody:
                        if (amandsSenseConstructor != null)
                        {
                            amandsSenseConstructor.UpdateSenseLocation();
                        }
                        break;
                }
            }
            else
            {
                LifeSpan = 0f;

                if (HeightCheck())
                {
                    RemoveSense();
                    return;
                }


                if (amandsSenseConstructor != null) amandsSenseConstructor.UpdateSense();

                // SenseWorld Position
                switch (eSenseWorldType)
                {
                    case ESenseWorldType.Item:
                        gameObject.transform.position = new Vector3(OwnerCollider.bounds.center.x, OwnerCollider.ClosestPoint(OwnerCollider.bounds.center + (Vector3.up * 10f)).y + AmandsSensePlugin.VerticalOffset.Value, OwnerCollider.bounds.center.z);
                        break;
                    case ESenseWorldType.Container:
                        break;
                    case ESenseWorldType.Deadbody:
                        if (amandsSenseConstructor != null) amandsSenseConstructor.UpdateSenseLocation();
                        break;
                    case ESenseWorldType.Drawer:
                        break;
                }
            }

            Waiting = false;
        }
        public void RestartSense()
        {
            if (Waiting || UpdateIntensity) return;

            LifeSpan = 0f;
            Delay = Vector3.Distance(AmandsSenseClass.Player.Position, gameObject.transform.position) / AmandsSensePlugin.Speed.Value;
            WaitAndStart();
        }
        public bool HeightCheck()
        {
            switch (eSenseWorldType)
            {
                case ESenseWorldType.Item:
                case ESenseWorldType.Container:
                case ESenseWorldType.Drawer:
                case ESenseWorldType.Deadbody:
                    return AmandsSenseClass.Player != null && (transform.position.y < AmandsSenseClass.Player.Position.y + AmandsSensePlugin.MinHeight.Value || transform.position.y > AmandsSenseClass.Player.Position.y + AmandsSensePlugin.MaxHeight.Value);
            }
            return false;
        }
        public void RemoveSense()
        {
            if (amandsSenseConstructor != null) amandsSenseConstructor.RemoveSense();
            AmandsSenseClass.SenseWorlds.Remove(Id);
            if (gameObject != null) Destroy(gameObject);
        }
        public void CancelSense()
        {
            UpdateIntensity = true;
            Starting = false;
        }
        public void Update()
        {
            if (UpdateIntensity)
            {
                if (Starting)
                {
                    Intensity += AmandsSensePlugin.IntensitySpeed.Value * Time.deltaTime;
                    if (Intensity >= 1f)
                    {
                        UpdateIntensity = false;
                        Starting = false;
                    }
                }
                else
                {
                    Intensity -= AmandsSensePlugin.IntensitySpeed.Value * Time.deltaTime;
                    if (Intensity <= 0f)
                    {
                        if (Waiting)
                        {
                            WaitingRemoveSense = true;
                        }
                        else
                        {
                            RemoveSense();
                        }
                        return;
                    }
                }

                if (amandsSenseConstructor != null) amandsSenseConstructor.UpdateIntensity(Intensity);

            }
            else if (!Starting && !Waiting)
            {
                LifeSpan += Time.deltaTime;
                if (LifeSpan > AmandsSensePlugin.Duration.Value)
                {
                    UpdateIntensity = true;
                }
            }
            if (Camera.main != null)
            {
                switch (eSenseWorldType)
                {
                    case ESenseWorldType.Item:
                    case ESenseWorldType.Container:
                    case ESenseWorldType.Deadbody:
                        transform.rotation = Camera.main.transform.rotation;
                        transform.localScale = Vector3.one * Mathf.Min(AmandsSensePlugin.SizeClamp.Value, Vector3.Distance(Camera.main.transform.position, transform.position));
                        break;
                    case ESenseWorldType.Drawer:
                        break;
                }
            }
        }
    }
    public class AmandsSenseConstructor : MonoBehaviour
    {
        public AmandsSenseWorld amandsSenseWorld;

        public Color color = AmandsSensePlugin.ObservedLootItemColor.Value;
        public Color textColor = AmandsSensePlugin.TextColor.Value;

        public SpriteRenderer spriteRenderer;
        public Sprite sprite;

        public Light light;

        public GameObject textGameObject;

        public TextMeshPro typeText;
        public TextMeshPro nameText;
        public TextMeshPro descriptionText;

        virtual public void Construct()
        {
            // SenseConstructor Sprite GameObject
            GameObject spriteGameObject = new GameObject("Sprite");
            spriteGameObject.transform.SetParent(gameObject.transform, false);
            RectTransform spriteRectTransform = spriteGameObject.AddComponent<RectTransform>();
            spriteRectTransform.localScale = Vector3.one * AmandsSensePlugin.IconSize.Value;

            // SenseConstructor Sprite
            spriteRenderer = spriteGameObject.AddComponent<SpriteRenderer>();
            spriteRenderer.sprite = sprite;
            spriteRenderer.color = new Color(color.r, color.g, color.b, 0f);

            // SenseConstructor Sprite Light
            light = spriteGameObject.AddComponent<Light>();
            light.color = new Color(color.r, color.g, color.b, 1f);
            light.shadows = AmandsSensePlugin.LightShadows.Value ? LightShadows.Hard : LightShadows.None;
            light.intensity = 0f;
            light.range = AmandsSensePlugin.LightRange.Value;

            if (AmandsSensePlugin.EnableSense.Value != EEnableSense.OnText) return;

            // SenseConstructor Text
            textGameObject = new GameObject("Text");
            textGameObject.transform.SetParent(gameObject.transform, false);
            RectTransform textRectTransform = textGameObject.AddComponent<RectTransform>();
            textRectTransform.localPosition = new Vector3(AmandsSensePlugin.TextOffset.Value, 0, 0);
            textRectTransform.pivot = new Vector2(0, 0.5f);

            // SenseConstructor VerticalLayoutGroup
            VerticalLayoutGroup verticalLayoutGroup = textGameObject.AddComponent<VerticalLayoutGroup>();
            verticalLayoutGroup.spacing = -0.02f;
            verticalLayoutGroup.childForceExpandHeight = false;
            verticalLayoutGroup.childForceExpandWidth = false;
            verticalLayoutGroup.childControlHeight = true;
            verticalLayoutGroup.childControlWidth = true;
            ContentSizeFitter contentSizeFitter = textGameObject.AddComponent<ContentSizeFitter>();
            contentSizeFitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            contentSizeFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            // SenseConstructor Type
            GameObject typeTextGameObject = new GameObject("Type");
            typeTextGameObject.transform.SetParent(textGameObject.transform, false);
            typeText = typeTextGameObject.AddComponent<TextMeshPro>();
            typeText.autoSizeTextContainer = true;
            typeText.fontSize = 0.5f;
            typeText.text = "Type";
            typeText.color = new Color(color.r, color.g, color.b, 0f);

            // SenseConstructor Name
            GameObject nameTextGameObject = new GameObject("Name");
            nameTextGameObject.transform.SetParent(textGameObject.transform, false);
            nameText = nameTextGameObject.AddComponent<TextMeshPro>();
            nameText.autoSizeTextContainer = true;
            nameText.fontSize = 1f;
            nameText.text = "Name";
            nameText.color = new Color(textColor.r, textColor.g, textColor.b, 0f);

            // SenseConstructor Description
            GameObject descriptionTextGameObject = new GameObject("Description");
            descriptionTextGameObject.transform.SetParent(textGameObject.transform, false);
            descriptionText = descriptionTextGameObject.AddComponent<TextMeshPro>();
            descriptionText.autoSizeTextContainer = true;
            descriptionText.fontSize = 0.75f;
            descriptionText.text = "";
            descriptionText.color = new Color(textColor.r, textColor.g, textColor.b, 0f);
        }
        virtual public void SetSense(ObservedLootItem observedLootItem)
        {

        }
        virtual public void SetSense(LootableContainer lootableContainer)
        {

        }
        virtual public void SetSense(LocalPlayer DeadPlayer)
        {

        }
        virtual public void SetSense(ExfiltrationPoint ExfiltrationPoint)
        {

        }
        virtual public void UpdateSense()
        {

        }
        virtual public void UpdateSenseLocation()
        {

        }
        virtual public void UpdateIntensity(float Intensity)
        {

        }
        virtual public void RemoveSense()
        {

        }
    }
    public class AmandsSenseItem : AmandsSenseConstructor
    {
        public ObservedLootItem observedLootItem;
        public string ItemId;
        public string type;

        public ESenseItemType eSenseItemType = ESenseItemType.All;

        public override void SetSense(ObservedLootItem ObservedLootItem)
        {
            eSenseItemType = ESenseItemType.All;
            color = AmandsSensePlugin.ObservedLootItemColor.Value;

            observedLootItem = ObservedLootItem;
            if (observedLootItem != null && observedLootItem.gameObject.activeSelf && observedLootItem.Item != null)
            {
                AmandsSenseClass.SenseItems.Add(observedLootItem.Item);

                ItemId = observedLootItem.ItemId;

                // Weapon SenseItem Color, Sprite and Type
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
                            type = AmandsSenseHelper.Localized("5b5f78e986f77447ed5636b1", EStringCase.None);
                            break;
                        case "assaultRifle":
                            eSenseItemType = ESenseItemType.AssaultRifles;
                            color = AmandsSensePlugin.AssaultRiflesColor.Value;
                            if (AmandsSenseClass.LoadedSprites.ContainsKey("icon_weapons_assaultrifles.png"))
                            {
                                sprite = AmandsSenseClass.LoadedSprites["icon_weapons_assaultrifles.png"];
                            }
                            type = AmandsSenseHelper.Localized("5b5f78fc86f77409407a7f90", EStringCase.None);
                            break;
                        case "sniperRifle":
                            eSenseItemType = ESenseItemType.BoltActionRifles;
                            color = AmandsSensePlugin.BoltActionRiflesColor.Value;
                            if (AmandsSenseClass.LoadedSprites.ContainsKey("icon_weapons_botaction.png"))
                            {
                                sprite = AmandsSenseClass.LoadedSprites["icon_weapons_botaction.png"];
                            }
                            type = AmandsSenseHelper.Localized("5b5f798886f77447ed5636b5", EStringCase.None);
                            break;
                        case "grenadeLauncher":
                            eSenseItemType = ESenseItemType.GrenadeLaunchers;
                            color = AmandsSensePlugin.GrenadeLaunchersColor.Value;
                            if (AmandsSenseClass.LoadedSprites.ContainsKey("icon_weapons_gl.png"))
                            {
                                sprite = AmandsSenseClass.LoadedSprites["icon_weapons_gl.png"];
                            }
                            type = AmandsSenseHelper.Localized("5b5f79d186f774093f2ed3c2", EStringCase.None);
                            break;
                        case "machinegun":
                            eSenseItemType = ESenseItemType.MachineGuns;
                            color = AmandsSensePlugin.MachineGunsColor.Value;
                            if (AmandsSenseClass.LoadedSprites.ContainsKey("icon_weapons_mg.png"))
                            {
                                sprite = AmandsSenseClass.LoadedSprites["icon_weapons_mg.png"];
                            }
                            type = AmandsSenseHelper.Localized("5b5f79a486f77409407a7f94", EStringCase.None);
                            break;
                        case "marksmanRifle":
                            eSenseItemType = ESenseItemType.MarksmanRifles;
                            color = AmandsSensePlugin.MarksmanRiflesColor.Value;
                            if (AmandsSenseClass.LoadedSprites.ContainsKey("icon_weapons_dmr.png"))
                            {
                                sprite = AmandsSenseClass.LoadedSprites["icon_weapons_dmr.png"];
                            }
                            type = AmandsSenseHelper.Localized("5b5f791486f774093f2ed3be", EStringCase.None);
                            break;
                        case "pistol":
                            eSenseItemType = ESenseItemType.Pistols;
                            color = AmandsSensePlugin.PistolsColor.Value;
                            if (AmandsSenseClass.LoadedSprites.ContainsKey("icon_weapons_pistols.png"))
                            {
                                sprite = AmandsSenseClass.LoadedSprites["icon_weapons_pistols.png"];
                            }
                            type = AmandsSenseHelper.Localized("5b5f792486f77447ed5636b3", EStringCase.None);
                            break;
                        case "smg":
                            eSenseItemType = ESenseItemType.SMGs;
                            color = AmandsSensePlugin.SMGsColor.Value;
                            if (AmandsSenseClass.LoadedSprites.ContainsKey("icon_weapons_smg.png"))
                            {
                                sprite = AmandsSenseClass.LoadedSprites["icon_weapons_smg.png"];
                            }
                            type = AmandsSenseHelper.Localized("5b5f796a86f774093f2ed3c0", EStringCase.None);
                            break;
                        case "shotgun":
                            eSenseItemType = ESenseItemType.Shotguns;
                            color = AmandsSensePlugin.ShotgunsColor.Value;
                            if (AmandsSenseClass.LoadedSprites.ContainsKey("icon_weapons_shotguns.png"))
                            {
                                sprite = AmandsSenseClass.LoadedSprites["icon_weapons_shotguns.png"];
                            }
                            type = AmandsSenseHelper.Localized("5b5f794b86f77409407a7f92", EStringCase.None);
                            break;
                        case "specialWeapon":
                            eSenseItemType = ESenseItemType.SpecialWeapons;
                            color = AmandsSensePlugin.SpecialWeaponsColor.Value;
                            if (AmandsSenseClass.LoadedSprites.ContainsKey("icon_weapons_special.png"))
                            {
                                sprite = AmandsSenseClass.LoadedSprites["icon_weapons_special.png"];
                            }
                            type = AmandsSenseHelper.Localized("5b5f79eb86f77447ed5636b7", EStringCase.None);
                            break;
                        default:
                            eSenseItemType = AmandsSenseClass.SenseItemType(observedLootItem.Item.GetType());
                            break;
                    }
                }
                else
                {
                    eSenseItemType = AmandsSenseClass.SenseItemType(observedLootItem.Item.GetType());
                }

                // SenseItem Color, Sprite and Type
                switch (eSenseItemType)
                {
                    case ESenseItemType.All:
                        color = AmandsSensePlugin.ObservedLootItemColor.Value;
                        if (AmandsSenseClass.LoadedSprites.ContainsKey("ObservedLootItem.png"))
                        {
                            sprite = AmandsSenseClass.LoadedSprites["ObservedLootItem.png"];
                        }
                        type = "ObservedLootItem";
                        break;
                    case ESenseItemType.Others:
                        color = AmandsSensePlugin.OthersColor.Value;
                        if (AmandsSenseClass.LoadedSprites.ContainsKey("icon_barter_others.png"))
                        {
                            sprite = AmandsSenseClass.LoadedSprites["icon_barter_others.png"];
                        }
                        type = AmandsSenseHelper.Localized("5b47574386f77428ca22b2f4", EStringCase.None);
                        break;
                    case ESenseItemType.BuildingMaterials:
                        color = AmandsSensePlugin.BuildingMaterialsColor.Value;
                        if (AmandsSenseClass.LoadedSprites.ContainsKey("icon_barter_building.png"))
                        {
                            sprite = AmandsSenseClass.LoadedSprites["icon_barter_building.png"];
                        }
                        type = AmandsSenseHelper.Localized("5b47574386f77428ca22b2ee", EStringCase.None);
                        break;
                    case ESenseItemType.Electronics:
                        color = AmandsSensePlugin.ElectronicsColor.Value;
                        if (AmandsSenseClass.LoadedSprites.ContainsKey("icon_barter_electronics.png"))
                        {
                            sprite = AmandsSenseClass.LoadedSprites["icon_barter_electronics.png"];
                        }
                        type = AmandsSenseHelper.Localized("5b47574386f77428ca22b2ef", EStringCase.None);
                        break;
                    case ESenseItemType.EnergyElements:
                        color = AmandsSensePlugin.EnergyElementsColor.Value;
                        if (AmandsSenseClass.LoadedSprites.ContainsKey("icon_barter_energy.png"))
                        {
                            sprite = AmandsSenseClass.LoadedSprites["icon_barter_energy.png"];
                        }
                        type = AmandsSenseHelper.Localized("5b47574386f77428ca22b2ed", EStringCase.None);
                        break;
                    case ESenseItemType.FlammableMaterials:
                        color = AmandsSensePlugin.FlammableMaterialsColor.Value;
                        if (AmandsSenseClass.LoadedSprites.ContainsKey("icon_barter_flammable.png"))
                        {
                            sprite = AmandsSenseClass.LoadedSprites["icon_barter_flammable.png"];
                        }
                        type = AmandsSenseHelper.Localized("5b47574386f77428ca22b2f2", EStringCase.None);
                        break;
                    case ESenseItemType.HouseholdMaterials:
                        color = AmandsSensePlugin.HouseholdMaterialsColor.Value;
                        if (AmandsSenseClass.LoadedSprites.ContainsKey("icon_barter_household.png"))
                        {
                            sprite = AmandsSenseClass.LoadedSprites["icon_barter_household.png"];
                        }
                        type = AmandsSenseHelper.Localized("5b47574386f77428ca22b2f0", EStringCase.None);
                        break;
                    case ESenseItemType.MedicalSupplies:
                        color = AmandsSensePlugin.MedicalSuppliesColor.Value;
                        if (AmandsSenseClass.LoadedSprites.ContainsKey("icon_barter_medical.png"))
                        {
                            sprite = AmandsSenseClass.LoadedSprites["icon_barter_medical.png"];
                        }
                        type = AmandsSenseHelper.Localized("5b47574386f77428ca22b2f3", EStringCase.None);
                        break;
                    case ESenseItemType.Tools:
                        color = AmandsSensePlugin.ToolsColor.Value;
                        if (AmandsSenseClass.LoadedSprites.ContainsKey("icon_barter_tools.png"))
                        {
                            sprite = AmandsSenseClass.LoadedSprites["icon_barter_tools.png"];
                        }
                        type = AmandsSenseHelper.Localized("5b47574386f77428ca22b2f6", EStringCase.None);
                        break;
                    case ESenseItemType.Valuables:
                        color = AmandsSensePlugin.ValuablesColor.Value;
                        if (AmandsSenseClass.LoadedSprites.ContainsKey("icon_barter_valuables.png"))
                        {
                            sprite = AmandsSenseClass.LoadedSprites["icon_barter_valuables.png"];
                        }
                        type = AmandsSenseHelper.Localized("5b47574386f77428ca22b2f1", EStringCase.None);
                        break;
                    case ESenseItemType.Backpacks:
                        color = AmandsSensePlugin.BackpacksColor.Value;
                        if (AmandsSenseClass.LoadedSprites.ContainsKey("icon_gear_backpacks.png"))
                        {
                            sprite = AmandsSenseClass.LoadedSprites["icon_gear_backpacks.png"];
                        }
                        type = AmandsSenseHelper.Localized("5b5f6f6c86f774093f2ecf0b", EStringCase.None);
                        break;
                    case ESenseItemType.BodyArmor:
                        color = AmandsSensePlugin.BodyArmorColor.Value;
                        if (AmandsSenseClass.LoadedSprites.ContainsKey("icon_gear_armor.png"))
                        {
                            sprite = AmandsSenseClass.LoadedSprites["icon_gear_armor.png"];
                        }
                        type = AmandsSenseHelper.Localized("5b5f701386f774093f2ecf0f", EStringCase.None);
                        break;
                    case ESenseItemType.Eyewear:
                        color = AmandsSensePlugin.EyewearColor.Value;
                        if (AmandsSenseClass.LoadedSprites.ContainsKey("icon_gear_visors.png"))
                        {
                            sprite = AmandsSenseClass.LoadedSprites["icon_gear_visors.png"];
                        }
                        type = AmandsSenseHelper.Localized("5b47574386f77428ca22b331", EStringCase.None);
                        break;
                    case ESenseItemType.Facecovers:
                        color = AmandsSensePlugin.FacecoversColor.Value;
                        if (AmandsSenseClass.LoadedSprites.ContainsKey("icon_gear_facecovers.png"))
                        {
                            sprite = AmandsSenseClass.LoadedSprites["icon_gear_facecovers.png"];
                        }
                        type = AmandsSenseHelper.Localized("5b47574386f77428ca22b32f", EStringCase.None);
                        break;
                    case ESenseItemType.GearComponents:
                        color = AmandsSensePlugin.GearComponentsColor.Value;
                        if (AmandsSenseClass.LoadedSprites.ContainsKey("icon_gear_components.png"))
                        {
                            sprite = AmandsSenseClass.LoadedSprites["icon_gear_components.png"];
                        }
                        type = AmandsSenseHelper.Localized("5b5f704686f77447ec5d76d7", EStringCase.None);
                        break;
                    case ESenseItemType.Headgear:
                        color = AmandsSensePlugin.HeadgearColor.Value;
                        if (AmandsSenseClass.LoadedSprites.ContainsKey("icon_gear_headwear.png"))
                        {
                            sprite = AmandsSenseClass.LoadedSprites["icon_gear_headwear.png"];
                        }
                        type = AmandsSenseHelper.Localized("5b47574386f77428ca22b330", EStringCase.None);
                        break;
                    case ESenseItemType.Headsets:
                        color = AmandsSensePlugin.HeadsetsColor.Value;
                        if (AmandsSenseClass.LoadedSprites.ContainsKey("icon_gear_headsets.png"))
                        {
                            sprite = AmandsSenseClass.LoadedSprites["icon_gear_headsets.png"];
                        }
                        type = AmandsSenseHelper.Localized("5b5f6f3c86f774094242ef87", EStringCase.None);
                        break;
                    case ESenseItemType.SecureContainers:
                        color = AmandsSensePlugin.SecureContainersColor.Value;
                        if (AmandsSenseClass.LoadedSprites.ContainsKey("icon_gear_secured.png"))
                        {
                            sprite = AmandsSenseClass.LoadedSprites["icon_gear_secured.png"];
                        }
                        type = AmandsSenseHelper.Localized("5b5f6fd286f774093f2ecf0d", EStringCase.None);
                        break;
                    case ESenseItemType.StorageContainers:
                        color = AmandsSensePlugin.StorageContainersColor.Value;
                        if (AmandsSenseClass.LoadedSprites.ContainsKey("icon_gear_cases.png"))
                        {
                            sprite = AmandsSenseClass.LoadedSprites["icon_gear_cases.png"];
                        }
                        type = AmandsSenseHelper.Localized("5b5f6fa186f77409407a7eb7", EStringCase.None);
                        break;
                    case ESenseItemType.TacticalRigs:
                        color = AmandsSensePlugin.TacticalRigsColor.Value;
                        if (AmandsSenseClass.LoadedSprites.ContainsKey("icon_gear_rigs.png"))
                        {
                            sprite = AmandsSenseClass.LoadedSprites["icon_gear_rigs.png"];
                        }
                        type = AmandsSenseHelper.Localized("5b5f6f8786f77447ed563642", EStringCase.None);
                        break;
                    case ESenseItemType.FunctionalMods:
                        color = AmandsSensePlugin.FunctionalModsColor.Value;
                        if (AmandsSenseClass.LoadedSprites.ContainsKey("icon_mods_functional.png"))
                        {
                            sprite = AmandsSenseClass.LoadedSprites["icon_mods_functional.png"];
                        }
                        type = AmandsSenseHelper.Localized("5b5f71b386f774093f2ecf11", EStringCase.None);
                        break;
                    case ESenseItemType.GearMods:
                        color = AmandsSensePlugin.GearModsColor.Value;
                        if (AmandsSenseClass.LoadedSprites.ContainsKey("icon_mods_gear.png"))
                        {
                            sprite = AmandsSenseClass.LoadedSprites["icon_mods_gear.png"];
                        }
                        type = AmandsSenseHelper.Localized("5b5f750686f774093e6cb503", EStringCase.None);
                        break;
                    case ESenseItemType.VitalParts:
                        color = AmandsSensePlugin.VitalPartsColor.Value;
                        if (AmandsSenseClass.LoadedSprites.ContainsKey("icon_mods_vital.png"))
                        {
                            sprite = AmandsSenseClass.LoadedSprites["icon_mods_vital.png"];
                        }
                        type = AmandsSenseHelper.Localized("5b5f75b986f77447ec5d7710", EStringCase.None);
                        break;
                    case ESenseItemType.MeleeWeapons:
                        color = AmandsSensePlugin.MeleeWeaponsColor.Value;
                        if (AmandsSenseClass.LoadedSprites.ContainsKey("icon_weapons_melee.png"))
                        {
                            sprite = AmandsSenseClass.LoadedSprites["icon_weapons_melee.png"];
                        }
                        type = AmandsSenseHelper.Localized("5b5f7a0886f77409407a7f96", EStringCase.None);
                        break;
                    case ESenseItemType.Throwables:
                        color = AmandsSensePlugin.ThrowablesColor.Value;
                        if (AmandsSenseClass.LoadedSprites.ContainsKey("icon_weapons_throw.png"))
                        {
                            sprite = AmandsSenseClass.LoadedSprites["icon_weapons_throw.png"];
                        }
                        type = AmandsSenseHelper.Localized("5b5f7a2386f774093f2ed3c4", EStringCase.None);
                        break;
                    case ESenseItemType.AmmoPacks:
                        color = AmandsSensePlugin.AmmoPacksColor.Value;
                        if (AmandsSenseClass.LoadedSprites.ContainsKey("icon_ammo_boxes.png"))
                        {
                            sprite = AmandsSenseClass.LoadedSprites["icon_ammo_boxes.png"];
                        }
                        type = AmandsSenseHelper.Localized("5b47574386f77428ca22b33c", EStringCase.None);
                        break;
                    case ESenseItemType.Rounds:
                        color = AmandsSensePlugin.RoundsColor.Value;
                        if (AmandsSenseClass.LoadedSprites.ContainsKey("icon_ammo_rounds.png"))
                        {
                            sprite = AmandsSenseClass.LoadedSprites["icon_ammo_rounds.png"];
                        }
                        type = AmandsSenseHelper.Localized("5b47574386f77428ca22b33b", EStringCase.None);
                        break;
                    case ESenseItemType.Drinks:
                        color = AmandsSensePlugin.DrinksColor.Value;
                        if (AmandsSenseClass.LoadedSprites.ContainsKey("icon_provisions_drinks.png"))
                        {
                            sprite = AmandsSenseClass.LoadedSprites["icon_provisions_drinks.png"];
                        }
                        type = AmandsSenseHelper.Localized("5b47574386f77428ca22b335", EStringCase.None);
                        break;
                    case ESenseItemType.Food:
                        color = AmandsSensePlugin.FoodColor.Value;
                        if (AmandsSenseClass.LoadedSprites.ContainsKey("icon_provisions_food.png"))
                        {
                            sprite = AmandsSenseClass.LoadedSprites["icon_provisions_food.png"];
                        }
                        type = AmandsSenseHelper.Localized("5b47574386f77428ca22b336", EStringCase.None);
                        break;
                    case ESenseItemType.Injectors:
                        color = AmandsSensePlugin.InjectorsColor.Value;
                        if (AmandsSenseClass.LoadedSprites.ContainsKey("icon_medical_injectors.png"))
                        {
                            sprite = AmandsSenseClass.LoadedSprites["icon_medical_injectors.png"];
                        }
                        type = AmandsSenseHelper.Localized("5b47574386f77428ca22b33a", EStringCase.None);
                        break;
                    case ESenseItemType.InjuryTreatment:
                        color = AmandsSensePlugin.InjuryTreatmentColor.Value;
                        if (AmandsSenseClass.LoadedSprites.ContainsKey("icon_medical_injury.png"))
                        {
                            sprite = AmandsSenseClass.LoadedSprites["icon_medical_injury.png"];
                        }
                        type = AmandsSenseHelper.Localized("5b47574386f77428ca22b339", EStringCase.None);
                        break;
                    case ESenseItemType.Medkits:
                        color = AmandsSensePlugin.MedkitsColor.Value;
                        if (AmandsSenseClass.LoadedSprites.ContainsKey("icon_medical_medkits.png"))
                        {
                            sprite = AmandsSenseClass.LoadedSprites["icon_medical_medkits.png"];
                        }
                        type = AmandsSenseHelper.Localized("5b47574386f77428ca22b338", EStringCase.None);
                        break;
                    case ESenseItemType.Pills:
                        color = AmandsSensePlugin.PillsColor.Value;
                        if (AmandsSenseClass.LoadedSprites.ContainsKey("icon_medical_pills.png"))
                        {
                            sprite = AmandsSenseClass.LoadedSprites["icon_medical_pills.png"];
                        }
                        type = AmandsSenseHelper.Localized("5b47574386f77428ca22b337", EStringCase.None);
                        break;
                    case ESenseItemType.ElectronicKeys:
                        color = AmandsSensePlugin.ElectronicKeysColor.Value;
                        if (AmandsSenseClass.LoadedSprites.ContainsKey("icon_keys_electronic.png"))
                        {
                            sprite = AmandsSenseClass.LoadedSprites["icon_keys_electronic.png"];
                        }
                        type = AmandsSenseHelper.Localized("5c518ed586f774119a772aee", EStringCase.None);
                        break;
                    case ESenseItemType.MechanicalKeys:
                        color = AmandsSensePlugin.MechanicalKeysColor.Value;
                        if (AmandsSenseClass.LoadedSprites.ContainsKey("icon_keys_mechanic.png"))
                        {
                            sprite = AmandsSenseClass.LoadedSprites["icon_keys_mechanic.png"];
                        }
                        type = AmandsSenseHelper.Localized("5c518ec986f7743b68682ce2", EStringCase.None);
                        break;
                    case ESenseItemType.InfoItems:
                        color = AmandsSensePlugin.InfoItemsColor.Value;
                        if (AmandsSenseClass.LoadedSprites.ContainsKey("icon_info.png"))
                        {
                            sprite = AmandsSenseClass.LoadedSprites["icon_info.png"];
                        }
                        type = AmandsSenseHelper.Localized("5b47574386f77428ca22b341", EStringCase.None);
                        break;
                    case ESenseItemType.SpecialEquipment:
                        color = AmandsSensePlugin.SpecialEquipmentColor.Value;
                        if (AmandsSenseClass.LoadedSprites.ContainsKey("icon_spec.png"))
                        {
                            sprite = AmandsSenseClass.LoadedSprites["icon_spec.png"];
                        }
                        type = AmandsSenseHelper.Localized("5b47574386f77428ca22b345", EStringCase.None);
                        break;
                    case ESenseItemType.Maps:
                        color = AmandsSensePlugin.MapsColor.Value;
                        if (AmandsSenseClass.LoadedSprites.ContainsKey("icon_maps.png"))
                        {
                            sprite = AmandsSenseClass.LoadedSprites["icon_maps.png"];
                        }
                        type = AmandsSenseHelper.Localized("5b47574386f77428ca22b343", EStringCase.None);
                        break;
                    case ESenseItemType.Money:
                        color = AmandsSensePlugin.MoneyColor.Value;
                        if (AmandsSenseClass.LoadedSprites.ContainsKey("icon_money.png"))
                        {
                            sprite = AmandsSenseClass.LoadedSprites["icon_money.png"];
                        }
                        type = AmandsSenseHelper.Localized("5b5f78b786f77447ed5636af", EStringCase.None);
                        break;
                }

                // Quest SenseItem Color
                if (observedLootItem.Item.QuestItem) color = AmandsSensePlugin.QuestItemsColor.Value;

                // JSON SenseItem Color
                if (AmandsSenseClass.itemsJsonClass != null)
                {
                    if (AmandsSenseClass.itemsJsonClass.KappaItems != null)
                    {
                        if (AmandsSenseClass.itemsJsonClass.KappaItems.Contains(observedLootItem.Item.TemplateId))
                        {
                            color = AmandsSensePlugin.KappaItemsColor.Value;
                        }
                    }
                    if (AmandsSensePlugin.EnableFlea.Value && !observedLootItem.Item.CanSellOnRagfair && !AmandsSenseClass.itemsJsonClass.NonFleaExclude.Contains(observedLootItem.Item.TemplateId))
                    {
                        color = AmandsSensePlugin.NonFleaItemsColor.Value;
                    }
                    if (AmandsSenseClass.Player != null && AmandsSenseClass.Player.Profile != null && AmandsSenseClass.Player.Profile.WishList != null && AmandsSenseClass.Player.Profile.WishList.Contains(observedLootItem.Item.TemplateId))
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

                if (AmandsSensePlugin.UseBackgroundColor.Value) color = AmandsSenseHelper.ToColor(observedLootItem.Item.BackgroundColor);

                // SenseItem Sprite
                if (spriteRenderer != null)
                {
                    spriteRenderer.sprite = sprite;
                    spriteRenderer.color = new Color(color.r, color.g, color.b, 0f);
                }

                // SenseItem Light
                if (light != null)
                {
                    light.color = new Color(color.r, color.g, color.b, 1f);
                    light.intensity = 0f;
                    light.range = AmandsSensePlugin.LightRange.Value;
                }

                // SenseItem Type
                if (typeText != null)
                {
                    typeText.fontSize = 0.5f;
                    typeText.text = type;
                    typeText.color = new Color(color.r, color.g, color.b, 0f);
                }

                if (AmandsSenseClass.inventoryControllerClass != null && !AmandsSenseClass.inventoryControllerClass.Examined(observedLootItem.Item))
                {
                    // SenseItem Unexamined Name
                    if (nameText != null)
                    {
                        nameText.fontSize = 1f;
                        nameText.text = "<b>???</b>";
                        nameText.color = new Color(textColor.r, textColor.g, textColor.b, 0f);
                    }
                    // SenseItem Unexamined Description
                    if (descriptionText != null)
                    {
                        descriptionText.text = "";
                        descriptionText.fontSize = 0.75f;
                        descriptionText.color = new Color(textColor.r, textColor.g, textColor.b, 0f);
                    }
                }
                else
                {
                    // SenseItem Name
                    if (nameText != null)
                    {
                        nameText.fontSize = 1f;
                        string Name = "<b>" + AmandsSenseHelper.Localized(observedLootItem.Item.Name, 0) + "</b>";
                        if (Name.Count() > 16) Name = "<b>" + AmandsSenseHelper.Localized(observedLootItem.Item.ShortName, 0) + "</b>";
                        if (observedLootItem.Item.StackObjectsCount > 1) Name = Name + " (" + observedLootItem.Item.StackObjectsCount + ")";
                        nameText.text = Name + "<color=#" + ColorUtility.ToHtmlStringRGB(color) + ">" + "<size=50%><voffset=0.5em> " + observedLootItem.Item.Weight + "kg";
                        nameText.color = new Color(textColor.r, textColor.g, textColor.b, 0f);
                    }

                    // SenseItem Description
                    if (descriptionText != null)
                    {
                        FoodDrinkComponent foodDrinkComponent;
                        if (observedLootItem.Item.TryGetItemComponent(out foodDrinkComponent) && ((int)foodDrinkComponent.MaxResource) > 1)
                        {
                            descriptionText.text = ((int)foodDrinkComponent.HpPercent) + "/" + ((int)foodDrinkComponent.MaxResource);
                        }
                        KeyComponent keyComponent;
                        if (observedLootItem.Item.TryGetItemComponent(out keyComponent))
                        {
                            int MaximumNumberOfUsage = Traverse.Create(Traverse.Create(keyComponent).Field("Template").GetValue<object>()).Field("MaximumNumberOfUsage").GetValue<int>();
                            descriptionText.text = (MaximumNumberOfUsage - keyComponent.NumberOfUsages) + "/" + MaximumNumberOfUsage;
                        }
                        MedKitComponent medKitComponent;
                        if (observedLootItem.Item.TryGetItemComponent(out medKitComponent) && medKitComponent.MaxHpResource > 1)
                        {
                            descriptionText.text = ((int)medKitComponent.HpResource) + "/" + medKitComponent.MaxHpResource;
                        }
                        RepairableComponent repairableComponent;
                        if (observedLootItem.Item.TryGetItemComponent(out repairableComponent))
                        {
                            descriptionText.text = ((int)repairableComponent.Durability) + "/" + ((int)repairableComponent.MaxDurability);
                        }
                        MagazineClass magazineClass = observedLootItem.Item as MagazineClass;
                        if (magazineClass != null)
                        {
                            descriptionText.text = magazineClass.Count + "/" + magazineClass.MaxCount;
                        }
                        descriptionText.fontSize = 0.75f;
                        descriptionText.color = new Color(textColor.r, textColor.g, textColor.b, 0f);
                    }
                }

                // SenseItem Sound
                if (AmandsSensePlugin.SenseRareSound.Value && AmandsSenseClass.LoadedAudioClips.ContainsKey("SenseRare.wav"))
                {
                    if (!AmandsSensePlugin.SenseAlwaysOn.Value)
                    {
                        Singleton<BetterAudio>.Instance.PlayAtPoint(transform.position, AmandsSenseClass.LoadedAudioClips["SenseRare.wav"], AmandsSensePlugin.AudioDistance.Value, BetterAudio.AudioSourceGroupType.Environment, AmandsSensePlugin.AudioRolloff.Value, AmandsSensePlugin.AudioVolume.Value, EOcclusionTest.Fast);
                    }
                }
                else
                {
                    if (!AmandsSensePlugin.SenseAlwaysOn.Value)
                    {
                        AudioClip itemClip = Singleton<GUISounds>.Instance.GetItemClip(observedLootItem.Item.ItemSound, EInventorySoundType.pickup);
                        if (itemClip != null)
                        {
                            Singleton<BetterAudio>.Instance.PlayAtPoint(transform.position, itemClip, AmandsSensePlugin.AudioDistance.Value, BetterAudio.AudioSourceGroupType.Environment, AmandsSensePlugin.AudioRolloff.Value, AmandsSensePlugin.AudioVolume.Value, EOcclusionTest.Fast);
                        }
                    }
                }
            }
            else if (amandsSenseWorld != null)
            {
                amandsSenseWorld.CancelSense();
            }
        }

        public override void UpdateIntensity(float Intensity)
        {
            if (spriteRenderer != null)
            {
                spriteRenderer.color = new Color(color.r, color.g, color.b, color.a * Intensity);
            }
            if (light != null)
            {
                light.intensity = AmandsSensePlugin.LightIntensity.Value * Intensity;
            }
            if (typeText != null)
            {
                typeText.color = new Color(color.r, color.g, color.b, Intensity);
            }
            if (nameText != null)
            {
                nameText.color = new Color(textColor.r, textColor.g, textColor.b, Intensity);
            }
            if (descriptionText != null)
            {
                descriptionText.color = new Color(textColor.r, textColor.g, textColor.b, Intensity);
            }
        }
        public override void RemoveSense()
        {
            if (observedLootItem != null && observedLootItem.gameObject.activeSelf && observedLootItem.Item != null)
            {
                AmandsSenseClass.SenseItems.Remove(observedLootItem.Item);
            }
            //Destroy(gameObject);
        }
    }
    public class AmandsSenseContainer : AmandsSenseConstructor
    {
        public LootableContainer lootableContainer;
        public bool emptyLootableContainer = false;
        public int itemCount = 0;
        public string ContainerId;
        public bool Drawer;

        public override void SetSense(LootableContainer LootableContainer)
        {
            lootableContainer = LootableContainer;
            if (lootableContainer != null && lootableContainer.gameObject.activeSelf)
            {
                Drawer = amandsSenseWorld.eSenseWorldType == ESenseWorldType.Drawer;
                // SenseContainer Defaults
                emptyLootableContainer = false;
                itemCount = 0;

                ContainerId = lootableContainer.Id;

                // SenseContainer Items
                ESenseItemColor eSenseItemColor = ESenseItemColor.Default;
                if (lootableContainer.ItemOwner != null && AmandsSenseClass.itemsJsonClass != null && AmandsSenseClass.itemsJsonClass.RareItems != null && AmandsSenseClass.itemsJsonClass.KappaItems != null && AmandsSenseClass.itemsJsonClass.NonFleaExclude != null && AmandsSenseClass.Player.Profile != null && AmandsSenseClass.Player.Profile.WishList != null)
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
                                        else if (AmandsSenseClass.Player.Profile.WishList.Contains(item.TemplateId) && eSenseItemColor != ESenseItemColor.Rare)
                                        {
                                            eSenseItemColor = ESenseItemColor.WishList;
                                        }
                                        else if (item.Template != null && !item.Template.CanSellOnRagfair && !AmandsSenseClass.itemsJsonClass.NonFleaExclude.Contains(item.TemplateId) && eSenseItemColor != ESenseItemColor.Rare && eSenseItemColor != ESenseItemColor.WishList)
                                        {
                                            if (!AmandsSensePlugin.FleaIncludeAmmo.Value && TemplateIdToObjectMappingsClass.TypeTable["5485a8684bdc2da71d8b4567"].IsAssignableFrom(item.GetType()))
                                            {
                                                continue;
                                            }
                                            else if (AmandsSensePlugin.EnableFlea.Value)
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
                if (itemCount == 0)
                {
                    amandsSenseWorld.CancelSense();
                    return;
                }

                // SenseContainer Color and Sprite
                if (AmandsSenseClass.LoadedSprites.ContainsKey("LootableContainer.png"))
                {
                    sprite = AmandsSenseClass.LoadedSprites["LootableContainer.png"];
                }
                switch (eSenseItemColor)
                {
                    case ESenseItemColor.Default:
                        color = AmandsSensePlugin.ObservedLootItemColor.Value;
                        break;
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

                // SenseContainer Sprite
                if (spriteRenderer != null)
                {
                    spriteRenderer.sprite = sprite;
                    spriteRenderer.color = new Color(color.r, color.g, color.b, 0f);
                }

                // SenseContainer Light
                if (light != null)
                {
                    light.color = new Color(color.r, color.g, color.b, 1f);
                    light.intensity = 0f;
                    light.range = AmandsSensePlugin.LightRange.Value;
                }

                // SenseContainer Type
                if (typeText != null)
                {
                    typeText.fontSize = 0.5f;
                    typeText.text = AmandsSenseHelper.Localized("container", EStringCase.None);
                    typeText.color = new Color(color.r, color.g, color.b, 0f);
                }

                // SenseContainer Name
                if (nameText != null)
                {
                    nameText.fontSize = 1f;
                    //nameText.text = "Name";
                    nameText.text = "<b>" + lootableContainer.ItemOwner.ContainerName + "</b>";
                    nameText.color = new Color(textColor.r, textColor.g, textColor.b, 0f);
                }

                // SenseContainer Description
                if (descriptionText != null)
                {
                    descriptionText.fontSize = 0.75f;
                    if (AmandsSensePlugin.ContainerLootcount.Value)
                    {
                        descriptionText.text = AmandsSenseHelper.Localized("loot", EStringCase.None) + " " + itemCount;
                    }
                    else
                    {
                        descriptionText.text = "";
                    }
                    descriptionText.color = new Color(textColor.r, textColor.g, textColor.b, 0f);
                }

                // SenseContainer Sound
                if (AmandsSensePlugin.SenseRareSound.Value && AmandsSenseClass.LoadedAudioClips.ContainsKey("SenseRare.wav"))
                {
                    if (!AmandsSensePlugin.SenseAlwaysOn.Value)
                    {
                        Singleton<BetterAudio>.Instance.PlayAtPoint(transform.position, AmandsSenseClass.LoadedAudioClips["SenseRare.wav"], AmandsSensePlugin.AudioDistance.Value, BetterAudio.AudioSourceGroupType.Environment, AmandsSensePlugin.AudioRolloff.Value, AmandsSensePlugin.ContainerAudioVolume.Value, EOcclusionTest.Fast);
                    }
                }
                else
                {
                    if (!AmandsSensePlugin.SenseAlwaysOn.Value && !Drawer && lootableContainer.OpenSound.Length > 0)
                    {
                        AudioClip OpenSound = lootableContainer.OpenSound[0];
                        if (OpenSound != null)
                        {
                            Singleton<BetterAudio>.Instance.PlayAtPoint(transform.position, OpenSound, AmandsSensePlugin.AudioDistance.Value, BetterAudio.AudioSourceGroupType.Environment, AmandsSensePlugin.AudioRolloff.Value, AmandsSensePlugin.ContainerAudioVolume.Value, EOcclusionTest.Fast);
                        }
                    }
                }
            }
            else if (amandsSenseWorld != null)
            {
                amandsSenseWorld.CancelSense();
            }
        }
        public override void UpdateSense()
        {
            if (lootableContainer != null && lootableContainer.gameObject.activeSelf)
            {
                // SenseContainer Defaults
                emptyLootableContainer = false;
                itemCount = 0;

                ContainerId = lootableContainer.Id;

                // SenseContainer Items
                ESenseItemColor eSenseItemColor = ESenseItemColor.Default;
                if (lootableContainer.ItemOwner != null && AmandsSenseClass.itemsJsonClass != null && AmandsSenseClass.itemsJsonClass.RareItems != null && AmandsSenseClass.itemsJsonClass.KappaItems != null && AmandsSenseClass.itemsJsonClass.NonFleaExclude != null && AmandsSenseClass.Player.Profile != null && AmandsSenseClass.Player.Profile.WishList != null)
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
                                        else if (AmandsSenseClass.Player.Profile.WishList.Contains(item.TemplateId) && eSenseItemColor != ESenseItemColor.Rare)
                                        {
                                            eSenseItemColor = ESenseItemColor.WishList;
                                        }
                                        else if (item.Template != null && !item.Template.CanSellOnRagfair && !AmandsSenseClass.itemsJsonClass.NonFleaExclude.Contains(item.TemplateId) && eSenseItemColor != ESenseItemColor.Rare && eSenseItemColor != ESenseItemColor.WishList)
                                        {
                                            if (!AmandsSensePlugin.FleaIncludeAmmo.Value && TemplateIdToObjectMappingsClass.TypeTable["5485a8684bdc2da71d8b4567"].IsAssignableFrom(item.GetType()))
                                            {
                                                continue;
                                            }
                                            else if (AmandsSensePlugin.EnableFlea.Value)
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
                if (itemCount == 0)
                {
                    amandsSenseWorld.CancelSense();
                    return;
                }

                // SenseContainer Color and Sprite
                if (AmandsSenseClass.LoadedSprites.ContainsKey("LootableContainer.png"))
                {
                    sprite = AmandsSenseClass.LoadedSprites["LootableContainer.png"];
                }
                switch (eSenseItemColor)
                {
                    case ESenseItemColor.Default:
                        color = AmandsSensePlugin.ObservedLootItemColor.Value;
                        break;
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

                // SenseContainer Sprite
                if (spriteRenderer != null)
                {
                    spriteRenderer.sprite = sprite;
                    spriteRenderer.color = new Color(color.r, color.g, color.b, spriteRenderer.color.a);
                }

                // SenseContainer Light
                if (light != null)
                {
                    light.color = new Color(color.r, color.g, color.b, 1f);
                    light.range = AmandsSensePlugin.LightRange.Value;
                }

                // SenseContainer Type
                if (typeText != null)
                {
                    typeText.fontSize = 0.5f;
                    //typeText.text = "Type";
                    typeText.color = new Color(color.r, color.g, color.b, typeText.color.a);
                }

                // SenseContainer Name
                if (nameText != null)
                {
                    nameText.fontSize = 1f;
                    //nameText.text = "Name";
                    nameText.color = new Color(textColor.r, textColor.g, textColor.b, nameText.color.a);
                }

                // SenseContainer Description
                if (descriptionText != null)
                {
                    descriptionText.fontSize = 0.75f;
                    if (AmandsSensePlugin.ContainerLootcount.Value)
                    {
                        descriptionText.text = AmandsSenseHelper.Localized("loot", EStringCase.None) + " " + itemCount;
                    }
                    else
                    {
                        descriptionText.text = "";
                    }
                    descriptionText.color = new Color(textColor.r, textColor.g, textColor.b, descriptionText.color.a);
                }
            }
            else if (amandsSenseWorld != null)
            {
                amandsSenseWorld.CancelSense();
            }
        }
        public override void UpdateIntensity(float Intensity)
        {
            if (spriteRenderer != null)
            {
                spriteRenderer.color = new Color(color.r, color.g, color.b, color.a * Intensity);
            }
            if (light != null)
            {
                light.intensity = AmandsSensePlugin.LightIntensity.Value * Intensity * (Drawer ? 0.25f : 1f);
            }
            if (typeText != null)
            {
                typeText.color = new Color(color.r, color.g, color.b, Intensity);
            }
            if (nameText != null)
            {
                nameText.color = new Color(textColor.r, textColor.g, textColor.b, Intensity);
            }
            if (descriptionText != null)
            {
                descriptionText.color = new Color(textColor.r, textColor.g, textColor.b, Intensity);
            }
        }
        public override void RemoveSense()
        {
            //Destroy(gameObject);
        }
    }
    public class AmandsSenseDeadPlayer : AmandsSenseConstructor
    {
        public LocalPlayer DeadPlayer;
        public Corpse corpse;

        public bool emptyDeadPlayer = true;
        public string Name;
        public string RoleName;

        public override void SetSense(LocalPlayer LocalPlayer)
        {
            DeadPlayer = LocalPlayer;
            if (DeadPlayer != null && DeadPlayer.gameObject.activeSelf)
            {
                corpse = DeadPlayer.gameObject.transform.GetComponent<Corpse>();
                // SenseDeadPlayer Defaults
                emptyDeadPlayer = false;
                ESenseItemColor eSenseItemColor = ESenseItemColor.Default;

                if (AmandsSenseClass.itemsJsonClass != null && AmandsSenseClass.itemsJsonClass.RareItems != null && AmandsSenseClass.itemsJsonClass.KappaItems != null && AmandsSenseClass.itemsJsonClass.NonFleaExclude != null && AmandsSenseClass.Player != null && AmandsSenseClass.Player.Profile != null && AmandsSenseClass.Player.Profile.WishList != null)
                {
                    if (DeadPlayer.Profile != null)
                    {
                        switch (DeadPlayer.Side)
                        {
                            case EPlayerSide.Usec:
                                RoleName = "USEC";
                                Name = DeadPlayer.Profile.Nickname;
                                break;
                            case EPlayerSide.Bear:
                                RoleName = "BEAR";
                                Name = DeadPlayer.Profile.Nickname;
                                break;
                            case EPlayerSide.Savage:
                                RoleName = AmandsSenseHelper.Localized(AmandsSenseHelper.GetScavRoleKey(Traverse.Create(Traverse.Create(DeadPlayer.Profile.Info).Field("Settings").GetValue<object>()).Field("Role").GetValue<WildSpawnType>()), EStringCase.Upper);
                                Name = AmandsSenseHelper.Transliterate(DeadPlayer.Profile.Nickname);
                                break;
                        }
                        object Inventory = Traverse.Create(DeadPlayer.Profile).Field("Inventory").GetValue();
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
                                    if (emptyDeadPlayer)
                                    {
                                        emptyDeadPlayer = false;
                                    }
                                    if (AmandsSenseClass.itemsJsonClass.RareItems.Contains(item.TemplateId))
                                    {
                                        eSenseItemColor = ESenseItemColor.Rare;
                                    }
                                    else if (AmandsSenseClass.Player.Profile.WishList.Contains(item.TemplateId) && eSenseItemColor != ESenseItemColor.Rare)
                                    {
                                        eSenseItemColor = ESenseItemColor.WishList;
                                    }
                                    else if (item.Template != null && !item.Template.CanSellOnRagfair && !AmandsSenseClass.itemsJsonClass.NonFleaExclude.Contains(item.TemplateId) && eSenseItemColor != ESenseItemColor.Rare && eSenseItemColor != ESenseItemColor.WishList)
                                    {
                                        if (!AmandsSensePlugin.FleaIncludeAmmo.Value && TemplateIdToObjectMappingsClass.TypeTable["5485a8684bdc2da71d8b4567"].IsAssignableFrom(item.GetType()))
                                        {
                                            continue;
                                        }
                                        else if (AmandsSensePlugin.EnableFlea.Value)
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

                switch (DeadPlayer.Side)
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
                        }
                        break;
                }

                switch (eSenseItemColor)
                {
                    case ESenseItemColor.Default:
                        color = AmandsSensePlugin.ObservedLootItemColor.Value;
                        break;
                    case ESenseItemColor.Kappa:
                        color = AmandsSensePlugin.KappaItemsColor.Value;
                        break;
                    case ESenseItemColor.NonFlea:
                        color = AmandsSensePlugin.NonFleaItemsColor.Value;
                        if (AmandsSenseClass.LoadedSprites.ContainsKey("icon_barter.png"))
                        {
                            //sprite = AmandsSenseClass.LoadedSprites["icon_barter.png"];
                        }
                        break;
                    case ESenseItemColor.WishList:
                        color = AmandsSensePlugin.WishListItemsColor.Value;
                        if (AmandsSenseClass.LoadedSprites.ContainsKey("icon_fav_checked.png"))
                        {
                            //sprite = AmandsSenseClass.LoadedSprites["icon_fav_checked.png"];
                        }
                        break;
                    case ESenseItemColor.Rare:
                        color = AmandsSensePlugin.RareItemsColor.Value;
                        break;
                }

                // SenseDeadPlayer Sprite
                if (spriteRenderer != null)
                {
                    spriteRenderer.sprite = sprite;
                    spriteRenderer.color = new Color(color.r, color.g, color.b, 0f);
                }

                // SenseDeadPlayer Light
                if (light != null)
                {
                    light.color = new Color(color.r, color.g, color.b, 1f);
                    light.intensity = 0f;
                    light.range = AmandsSensePlugin.LightRange.Value;
                }

                // SenseDeadPlayer Type
                if (typeText != null)
                {
                    typeText.fontSize = 0.5f;
                    typeText.text = RoleName;
                    typeText.color = new Color(color.r, color.g, color.b, 0f);
                }

                // SenseDeadPlayer Name
                if (nameText != null)
                {
                    nameText.fontSize = 1f;
                    nameText.text = "<b>" + Name + "</b>";
                    nameText.color = new Color(textColor.r, textColor.g, textColor.b, 0f);
                }

                // SenseDeadPlayer Description
                if (descriptionText != null)
                {
                    descriptionText.fontSize = 0.75f;
                    descriptionText.text = "";
                    descriptionText.color = new Color(textColor.r, textColor.g, textColor.b, 0f);
                }
            }
            else if (amandsSenseWorld != null)
            {
                amandsSenseWorld.CancelSense();
            }
        }
        public override void UpdateSense()
        {
            if (DeadPlayer != null && DeadPlayer.gameObject.activeSelf)// && bodyPartCollider != null && bodyPartCollider.gameObject.activeSelf && bodyPartCollider.Collider != null && AmandsSenseClass.localPlayer != null && bodyPartCollider.Collider.transform.position.y > AmandsSenseClass.localPlayer.Position.y + AmandsSensePlugin.MinHeight.Value && bodyPartCollider.Collider.transform.position.y < AmandsSenseClass.localPlayer.Position.y + AmandsSensePlugin.MaxHeight.Value)
            {
                // SenseDeadPlayer Defaults
                emptyDeadPlayer = false;
                ESenseItemColor eSenseItemColor = ESenseItemColor.Default;

                if (AmandsSenseClass.itemsJsonClass != null && AmandsSenseClass.itemsJsonClass.RareItems != null && AmandsSenseClass.itemsJsonClass.KappaItems != null && AmandsSenseClass.itemsJsonClass.NonFleaExclude != null && AmandsSenseClass.Player != null && AmandsSenseClass.Player.Profile != null && AmandsSenseClass.Player.Profile.WishList != null)
                {
                    if (DeadPlayer != null && DeadPlayer.Profile != null)
                    {
                        object Inventory = Traverse.Create(DeadPlayer.Profile).Field("Inventory").GetValue();
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
                                    if (emptyDeadPlayer)
                                    {
                                        emptyDeadPlayer = false;
                                    }
                                    if (AmandsSenseClass.itemsJsonClass.RareItems.Contains(item.TemplateId))
                                    {
                                        eSenseItemColor = ESenseItemColor.Rare;
                                    }
                                    else if (AmandsSenseClass.Player.Profile.WishList.Contains(item.TemplateId) && eSenseItemColor != ESenseItemColor.Rare)
                                    {
                                        eSenseItemColor = ESenseItemColor.WishList;
                                    }
                                    else if (item.Template != null && !item.Template.CanSellOnRagfair && !AmandsSenseClass.itemsJsonClass.NonFleaExclude.Contains(item.TemplateId) && eSenseItemColor != ESenseItemColor.Rare && eSenseItemColor != ESenseItemColor.WishList)
                                    {
                                        if (!AmandsSensePlugin.FleaIncludeAmmo.Value && TemplateIdToObjectMappingsClass.TypeTable["5485a8684bdc2da71d8b4567"].IsAssignableFrom(item.GetType()))
                                        {
                                            continue;
                                        }
                                        else if (AmandsSensePlugin.EnableFlea.Value)
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
                switch (DeadPlayer.Side)
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
                        }
                        break;
                }

                switch (eSenseItemColor)
                {
                    case ESenseItemColor.Default:
                        color = AmandsSensePlugin.ObservedLootItemColor.Value;
                        break;
                    case ESenseItemColor.Kappa:
                        color = AmandsSensePlugin.KappaItemsColor.Value;
                        break;
                    case ESenseItemColor.NonFlea:
                        color = AmandsSensePlugin.NonFleaItemsColor.Value;
                        if (AmandsSenseClass.LoadedSprites.ContainsKey("icon_barter.png"))
                        {
                            //sprite = AmandsSenseClass.LoadedSprites["icon_barter.png"];
                        }
                        break;
                    case ESenseItemColor.WishList:
                        color = AmandsSensePlugin.WishListItemsColor.Value;
                        if (AmandsSenseClass.LoadedSprites.ContainsKey("icon_fav_checked.png"))
                        {
                            //sprite = AmandsSenseClass.LoadedSprites["icon_fav_checked.png"];
                        }
                        break;
                    case ESenseItemColor.Rare:
                        color = AmandsSensePlugin.RareItemsColor.Value;
                        break;
                }

                // SenseDeadPlayer Sprite
                if (spriteRenderer != null)
                {
                    spriteRenderer.sprite = sprite;
                    spriteRenderer.color = new Color(color.r, color.g, color.b, spriteRenderer.color.a);
                }

                // SenseDeadPlayer Light
                if (light != null)
                {
                    light.color = new Color(color.r, color.g, color.b, 1f);
                    light.range = AmandsSensePlugin.LightRange.Value;
                }

                // SenseDeadPlayer Type
                if (typeText != null)
                {
                    typeText.fontSize = 0.5f;
                    //typeText.text = corpse.Side.ToString();
                    typeText.color = new Color(color.r, color.g, color.b, typeText.color.a);
                }

                // SenseDeadPlayer Name
                if (nameText != null)
                {
                    nameText.fontSize = 1f;
                    //nameText.text = Name;
                    nameText.color = new Color(textColor.r, textColor.g, textColor.b, nameText.color.a);
                }

                // SenseDeadPlayer Description
                if (descriptionText != null)
                {
                    descriptionText.fontSize = 0.75f;
                    descriptionText.text = "";
                    descriptionText.color = new Color(textColor.r, textColor.g, textColor.b, descriptionText.color.a);
                }
            }
            else if (amandsSenseWorld != null)
            {
                amandsSenseWorld.CancelSense();
            }
        }
        public override void UpdateIntensity(float Intensity)
        {
            if (spriteRenderer != null)
            {
                spriteRenderer.color = new Color(color.r, color.g, color.b, color.a * Intensity);
            }
            if (light != null)
            {
                light.intensity = AmandsSensePlugin.LightIntensity.Value * Intensity;
            }
            if (typeText != null)
            {
                typeText.color = new Color(color.r, color.g, color.b, Intensity);
            }
            if (nameText != null)
            {
                nameText.color = new Color(textColor.r, textColor.g, textColor.b, Intensity);
            }
            if (descriptionText != null)
            {
                descriptionText.color = new Color(textColor.r, textColor.g, textColor.b, Intensity);
            }
        }
        public override void UpdateSenseLocation()
        {
            if (corpse != null)
            {
                gameObject.transform.parent.position = corpse.TrackableTransform.position + (Vector3.up * 3f * AmandsSensePlugin.VerticalOffset.Value);
            }
        }
        public override void RemoveSense()
        {
            //Destroy(gameObject);
        }
    }
    public class AmandsSenseExfil : MonoBehaviour
    {
        public ExfiltrationPoint exfiltrationPoint;

        public Color color = Color.green;
        public Color textColor = AmandsSensePlugin.TextColor.Value;

        public SpriteRenderer spriteRenderer;
        public Sprite sprite;

        public Light light;

        public GameObject textGameObject;

        public TextMeshPro typeText;
        public TextMeshPro nameText;
        public TextMeshPro descriptionText;
        public TextMeshPro distanceText;

        public float Delay;
        public float LifeSpan;

        public bool UpdateIntensity = false;
        public bool Starting = true;
        public float Intensity = 0f;

        public void SetSense(ExfiltrationPoint ExfiltrationPoint)
        {
            exfiltrationPoint = ExfiltrationPoint;
            gameObject.transform.position = exfiltrationPoint.transform.position + (Vector3.up * AmandsSensePlugin.ExfilVerticalOffset.Value);
            gameObject.transform.localScale = new Vector3(-50,50,50);
        }

        public void Construct()
        {
            // AmandsSenseExfil Sprite GameObject
            GameObject spriteGameObject = new GameObject("Sprite");
            spriteGameObject.transform.SetParent(gameObject.transform, false);
            RectTransform spriteRectTransform = spriteGameObject.AddComponent<RectTransform>();
            spriteRectTransform.localScale /= 50f;

            // AmandsSenseExfil Sprite
            spriteRenderer = spriteGameObject.AddComponent<SpriteRenderer>();
            spriteRenderer.sprite = sprite;
            spriteRenderer.color = new Color(color.r, color.g, color.b, 0f);

            // AmandsSenseExfil Sprite Light
            light = spriteGameObject.AddComponent<Light>();
            light.color = new Color(color.r, color.g, color.b, 1f);
            light.shadows = LightShadows.None;
            light.intensity = 0f;
            light.range = AmandsSensePlugin.ExfilLightRange.Value;

            // AmandsSenseExfil Text
            textGameObject = new GameObject("Text");
            textGameObject.transform.SetParent(gameObject.transform, false);
            RectTransform textRectTransform = textGameObject.AddComponent<RectTransform>();
            textRectTransform.localPosition = new Vector3(0.1f, 0, 0);
            textRectTransform.pivot = new Vector2(0, 0.5f);

            // AmandsSenseExfil VerticalLayoutGroup
            VerticalLayoutGroup verticalLayoutGroup = textGameObject.AddComponent<VerticalLayoutGroup>();
            verticalLayoutGroup.spacing = -0.02f;
            verticalLayoutGroup.childForceExpandHeight = false;
            verticalLayoutGroup.childForceExpandWidth = false;
            verticalLayoutGroup.childControlHeight = true;
            verticalLayoutGroup.childControlWidth = true;
            ContentSizeFitter contentSizeFitter = textGameObject.AddComponent<ContentSizeFitter>();
            contentSizeFitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            contentSizeFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            GameObject typeTextGameObject = new GameObject("Type");
            typeTextGameObject.transform.SetParent(textGameObject.transform, false);
            typeText = typeTextGameObject.AddComponent<TextMeshPro>();
            typeText.autoSizeTextContainer = true;
            typeText.fontSize = 0.5f;
            typeText.text = "Type";
            typeText.color = new Color(color.r, color.g, color.b, 0f);

            GameObject nameTextGameObject = new GameObject("Name");
            nameTextGameObject.transform.SetParent(textGameObject.transform, false);
            nameText = nameTextGameObject.AddComponent<TextMeshPro>();
            nameText.autoSizeTextContainer = true;
            nameText.fontSize = 1f;
            nameText.text = "Name";
            nameText.color = new Color(textColor.r, textColor.g, textColor.b, 0f);

            GameObject descriptionTextGameObject = new GameObject("Description");
            descriptionTextGameObject.transform.SetParent(textGameObject.transform, false);
            descriptionText = descriptionTextGameObject.AddComponent<TextMeshPro>();
            descriptionText.autoSizeTextContainer = true;
            descriptionText.fontSize = 0.75f;
            descriptionText.text = "";
            descriptionText.color = new Color(textColor.r, textColor.g, textColor.b, 0f);

            GameObject distanceTextGameObject = new GameObject("Distance");
            distanceTextGameObject.transform.SetParent(gameObject.transform, false);
            distanceTextGameObject.transform.localPosition = new Vector3(0, -0.13f, 0);
            distanceText = distanceTextGameObject.AddComponent<TextMeshPro>();
            distanceText.alignment = TextAlignmentOptions.Center;
            distanceText.autoSizeTextContainer = true;
            distanceText.fontSize = 0.75f;
            distanceText.text = "Distance";
            distanceText.color = new Color(color.r, color.g, color.b, 0f);

            enabled = false;
            gameObject.SetActive(false);
        }
        public void ShowSense()
        {
            color = Color.green;
            textColor = AmandsSensePlugin.TextColor.Value;

            if (exfiltrationPoint != null && exfiltrationPoint.gameObject.activeSelf && AmandsSenseClass.Player != null && exfiltrationPoint.InfiltrationMatch(AmandsSenseClass.Player))
            {
                sprite = AmandsSenseClass.LoadedSprites["Exfil.png"];
                bool Unmet = exfiltrationPoint.UnmetRequirements(AmandsSenseClass.Player).ToArray().Any();
                color = Unmet ? AmandsSensePlugin.ExfilUnmetColor.Value : AmandsSensePlugin.ExfilColor.Value;
                // AmandsSenseExfil Sprite
                if (spriteRenderer != null)
                {
                    spriteRenderer.sprite = sprite;
                    spriteRenderer.color = new Color(color.r, color.g, color.b, 0f);
                }

                // AmandsSenseExfil Light
                if (light != null)
                {
                    light.color = new Color(color.r, color.g, color.b, 1f);
                    light.intensity = 0f;
                    light.range = AmandsSensePlugin.ExfilLightRange.Value;
                }

                // AmandsSenseExfil Type
                if (typeText != null)
                {
                    typeText.fontSize = 0.5f;
                    typeText.text = AmandsSenseHelper.Localized("exfil", EStringCase.None);
                    typeText.color = new Color(color.r, color.g, color.b, 0f);
                }

                // AmandsSenseExfil Name
                if (nameText != null)
                {
                    nameText.fontSize = 1f;
                    nameText.text = "<b>" + AmandsSenseHelper.Localized(exfiltrationPoint.Settings.Name,0) + "</b><color=#" + ColorUtility.ToHtmlStringRGB(color) + ">" + "<size=50%><voffset=0.5em> " + exfiltrationPoint.Settings.ExfiltrationTime + "s";
                    nameText.color = new Color(textColor.r, textColor.g, textColor.b, 0f);
                }

                // AmandsSenseExfil Description
                if (descriptionText != null)
                {
                    descriptionText.fontSize = 0.75f;
                    string tips = "";
                    if (Unmet)
                    {
                        foreach (string tip in exfiltrationPoint.GetTips(AmandsSenseClass.Player.ProfileId))
                        {
                            tips = tips + tip + "\n";
                        }
                    }
                    descriptionText.overrideColorTags = true;
                    descriptionText.text = tips;
                    descriptionText.color = new Color(textColor.r, textColor.g, textColor.b, 0f);
                }

                // AmandsSenseExfil Distancce
                if (distanceText != null)
                {
                    distanceText.fontSize = 0.5f;
                    if (Camera.main != null) distanceText.text = (int)Vector3.Distance(transform.position, Camera.main.transform.position) + "m";
                    distanceText.color = new Color(color.r, color.g, color.b, 0f);
                }

                gameObject.SetActive(true);
                enabled = true;

                LifeSpan = 0f;
                Starting = true;
                Intensity = 0f;
                UpdateIntensity = true;
            }
            if (exfiltrationPoint == null)
            {
                AmandsSenseClass.SenseExfils.Remove(this);
                Destroy(gameObject);
            }
        }
        public void UpdateSense()
        {
            if (exfiltrationPoint != null && exfiltrationPoint.gameObject.activeSelf && AmandsSenseClass.Player != null && exfiltrationPoint.InfiltrationMatch(AmandsSenseClass.Player))
            {
                sprite = AmandsSenseClass.LoadedSprites["Exfil.png"];
                bool Unmet = exfiltrationPoint.UnmetRequirements(AmandsSenseClass.Player).ToArray().Any();
                color = Unmet ? AmandsSensePlugin.ExfilUnmetColor.Value : AmandsSensePlugin.ExfilColor.Value;
                // AmandsSenseExfil Sprite
                if (spriteRenderer != null)
                {
                    spriteRenderer.sprite = sprite;
                    spriteRenderer.color = new Color(color.r, color.g, color.b, color.a);
                }

                // AmandsSenseExfil Light
                if (light != null)
                {
                    light.color = new Color(color.r, color.g, color.b, 1f);
                    light.range = AmandsSensePlugin.ExfilLightRange.Value;
                }

                // AmandsSenseExfil Type
                if (typeText != null)
                {
                    typeText.fontSize = 0.5f;
                    typeText.text = AmandsSenseHelper.Localized("exfil", EStringCase.None);
                    typeText.color = new Color(color.r, color.g, color.b, color.a);
                }

                // AmandsSenseExfil Name
                if (nameText != null)
                {
                    nameText.fontSize = 1f;
                    nameText.text = "<b>" + AmandsSenseHelper.Localized(exfiltrationPoint.Settings.Name, 0) + "</b><color=#" + ColorUtility.ToHtmlStringRGB(color) + ">" + "<size=50%><voffset=0.5em> " + exfiltrationPoint.Settings.ExfiltrationTime + "s";
                    nameText.color = new Color(textColor.r, textColor.g, textColor.b, textColor.a);
                }

                // AmandsSenseExfil Description
                if (descriptionText != null)
                {
                    descriptionText.fontSize = 0.75f;
                    string tips = "";
                    if (Unmet)
                    {
                        foreach (string tip in exfiltrationPoint.GetTips(AmandsSenseClass.Player.ProfileId))
                        {
                            tips = tips + tip + "\n";
                        }
                    }
                    descriptionText.overrideColorTags = true;
                    descriptionText.text = tips;
                    descriptionText.color = new Color(textColor.r, textColor.g, textColor.b, textColor.a);
                }

                // AmandsSenseExfil Distancce
                if (distanceText != null)
                {
                    distanceText.fontSize = 0.5f;
                    if (Camera.main != null) distanceText.text = (int)Vector3.Distance(transform.position, Camera.main.transform.position) + "m";
                    distanceText.color = new Color(color.r, color.g, color.b, color.a);
                }
            }
            if (exfiltrationPoint == null)
            {
                AmandsSenseClass.SenseExfils.Remove(this);
                Destroy(gameObject);
            }
        }
        public void Update()
        {
            if (UpdateIntensity)
            {
                if (Starting)
                {
                    Intensity += AmandsSensePlugin.IntensitySpeed.Value * Time.deltaTime;
                    if (Intensity >= 1f)
                    {
                        UpdateIntensity = false;
                        Starting = false;
                    }
                }
                else
                {
                    Intensity -= AmandsSensePlugin.IntensitySpeed.Value * Time.deltaTime;
                    if (Intensity <= 0f)
                    {
                        Starting = true;
                        UpdateIntensity = false;
                        enabled = false;
                        gameObject.SetActive(false);
                        return;
                    }
                }

                if (spriteRenderer != null)
                {
                    spriteRenderer.color = new Color(color.r, color.g, color.b, color.a * Intensity);
                }
                if (light != null)
                {
                    light.intensity = Intensity * AmandsSensePlugin.ExfilLightIntensity.Value;
                }
                if (typeText != null)
                {
                    typeText.color = new Color(color.r, color.g, color.b, Intensity);
                }
                if (nameText != null)
                {
                    nameText.color = new Color(textColor.r, textColor.g, textColor.b, Intensity);
                }
                if (descriptionText != null)
                {
                    descriptionText.color = new Color(textColor.r, textColor.g, textColor.b, Intensity);
                }
                if (distanceText != null)
                {
                    distanceText.color = new Color(color.r, color.g, color.b, Intensity);
                }
            }
            else if (!Starting)
            {
                LifeSpan += Time.deltaTime;
                if (LifeSpan > AmandsSensePlugin.ExfilDuration.Value)
                {
                    UpdateIntensity = true;
                }
            }
            if (Camera.main != null)
            {
                transform.LookAt(new Vector3(Camera.main.transform.position.x, transform.position.y, Camera.main.transform.position.z));
                if (distanceText != null)
                {
                    distanceText.text = (int)Vector3.Distance(transform.position, Camera.main.transform.position) + "m";
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
    public enum ESenseWorldType
    {
        Item,
        Container,
        Deadbody,
        Drawer
    }
    public enum EEnableSense
    {
        Off,
        On,
        OnText
    }
    public struct SenseDeadPlayerStruct
    {
        public Player victim;
        public Player aggressor;

        public SenseDeadPlayerStruct(Player Victim, Player Aggressor)
        {
            victim = Victim;
            aggressor = Aggressor;
        }
    }
}
