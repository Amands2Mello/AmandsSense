
using Aki.Common.Utils;
using Aki.Reflection.Patching;
using BepInEx;
using BepInEx.Configuration;
using System;
using System.IO;
using System.Reflection;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering.PostProcessing;
using EFT.CameraControl;
using EFT.InventoryLogic;
using HarmonyLib;
using EFT;
using System.Threading.Tasks;

namespace AmandsSense
{
    [BepInPlugin("com.Amanda.Sense", "Sense", "1.1.0")]
    public class AmandsSensePlugin : BaseUnityPlugin
    {
        public static GameObject Hook;
        public static AmandsSenseClass AmandsSenseClassComponent;
        public static ConfigEntry<KeyboardShortcut> SenseKey { get; set; }
        public static ConfigEntry<bool> DoubleClick { get; set; }
        public static ConfigEntry<float> DoubleClickDelay { get; set; }
        public static ConfigEntry<bool> EnableSense { get; set; }
        public static ConfigEntry<bool> SenseAlwaysOn { get; set; }
        public static ConfigEntry<float> Cooldown { get; set; }
        public static ConfigEntry<int> Radius { get; set; }
        public static ConfigEntry<int> DeadbodyRadius { get; set; }
        public static ConfigEntry<int> AlwaysOnRadius { get; set; }
        public static ConfigEntry<int> AlwaysOnDeadbodyRadius { get; set; }
        public static ConfigEntry<float> AlwaysOnFrequency { get; set; }
        public static ConfigEntry<float> MaxHeight { get; set; }
        public static ConfigEntry<float> MinHeight { get; set; }
        public static ConfigEntry<float> Speed { get; set; }
        public static ConfigEntry<int> Limit { get; set; }
        public static ConfigEntry<bool> NonFleaAmmo { get; set; }

        public static ConfigEntry<Color> RareItemsColor { get; set; }
        public static ConfigEntry<Color> WishListItemsColor { get; set; }
        public static ConfigEntry<Color> NonFleaItemsColor { get; set; }
        public static ConfigEntry<Color> KappaItemsColor { get; set; }
        public static ConfigEntry<Color> LootableContainerColor { get; set; }
        public static ConfigEntry<Color> ObservedLootItemColor { get; set; }
        public static ConfigEntry<Color> OthersColor { get; set; }
        public static ConfigEntry<Color> BuildingMaterialsColor { get; set; }
        public static ConfigEntry<Color> ElectronicsColor { get; set; }
        public static ConfigEntry<Color> EnergyElementsColor { get; set; }
        public static ConfigEntry<Color> FlammableMaterialsColor { get; set; }
        public static ConfigEntry<Color> HouseholdMaterialsColor { get; set; }
        public static ConfigEntry<Color> MedicalSuppliesColor { get; set; }
        public static ConfigEntry<Color> ToolsColor { get; set; }
        public static ConfigEntry<Color> ValuablesColor { get; set; }
        public static ConfigEntry<Color> BackpacksColor { get; set; }
        public static ConfigEntry<Color> BodyArmorColor { get; set; }
        public static ConfigEntry<Color> EyewearColor { get; set; }
        public static ConfigEntry<Color> FacecoversColor { get; set; }
        public static ConfigEntry<Color> GearComponentsColor { get; set; }
        public static ConfigEntry<Color> HeadgearColor { get; set; }
        public static ConfigEntry<Color> HeadsetsColor { get; set; }
        public static ConfigEntry<Color> SecureContainersColor { get; set; }
        public static ConfigEntry<Color> StorageContainersColor { get; set; }
        public static ConfigEntry<Color> TacticalRigsColor { get; set; }
        public static ConfigEntry<Color> FunctionalModsColor { get; set; }
        public static ConfigEntry<Color> GearModsColor { get; set; }
        public static ConfigEntry<Color> VitalPartsColor { get; set; }
        public static ConfigEntry<Color> AssaultCarbinesColor { get; set; }
        public static ConfigEntry<Color> AssaultRiflesColor { get; set; }
        public static ConfigEntry<Color> BoltActionRiflesColor { get; set; }
        public static ConfigEntry<Color> GrenadeLaunchersColor { get; set; }
        public static ConfigEntry<Color> MachineGunsColor { get; set; }
        public static ConfigEntry<Color> MarksmanRiflesColor { get; set; }
        public static ConfigEntry<Color> PistolsColor { get; set; }
        public static ConfigEntry<Color> SMGsColor { get; set; }
        public static ConfigEntry<Color> ShotgunsColor { get; set; }
        public static ConfigEntry<Color> SpecialWeaponsColor { get; set; }
        public static ConfigEntry<Color> MeleeWeaponsColor { get; set; }
        public static ConfigEntry<Color> ThrowablesColor { get; set; }
        public static ConfigEntry<Color> AmmoPacksColor { get; set; }
        public static ConfigEntry<Color> RoundsColor { get; set; }
        public static ConfigEntry<Color> DrinksColor { get; set; }
        public static ConfigEntry<Color> FoodColor { get; set; }
        public static ConfigEntry<Color> InjectorsColor { get; set; }
        public static ConfigEntry<Color> InjuryTreatmentColor { get; set; }
        public static ConfigEntry<Color> MedkitsColor { get; set; }
        public static ConfigEntry<Color> PillsColor { get; set; }
        public static ConfigEntry<Color> ElectronicKeysColor { get; set; }
        public static ConfigEntry<Color> MechanicalKeysColor { get; set; }
        public static ConfigEntry<Color> InfoItemsColor { get; set; }
        public static ConfigEntry<Color> QuestItemsColor { get; set; }
        public static ConfigEntry<Color> SpecialEquipmentColor { get; set; }
        public static ConfigEntry<Color> MapsColor { get; set; }
        public static ConfigEntry<Color> MoneyColor { get; set; }

        public static ConfigEntry<Vector2> Size { get; set; }
        public static ConfigEntry<Vector2> NewSize { get; set; }
        public static ConfigEntry<Vector2> AlwaysOnSize { get; set; }
        public static ConfigEntry<float> SizeClamp { get; set; }
        public static ConfigEntry<float> NormalSize { get; set; }
        public static ConfigEntry<float> Duration { get; set; }
        public static ConfigEntry<float> OpacitySpeed { get; set; }
        public static ConfigEntry<float> StartOpacitySpeed { get; set; }
        public static ConfigEntry<float> LightIntensity { get; set; }
        public static ConfigEntry<float> LightRange { get; set; }
        public static ConfigEntry<float> AudioDistance { get; set; }
        public static ConfigEntry<int> AudioRolloff { get; set; }
        public static ConfigEntry<float> AudioVolume { get; set; }

        public static ConfigEntry<bool> useDof { get; set; }
        public static ConfigEntry<bool> dofForceEnableMedian { get; set; }
        public static ConfigEntry<float> dofBokehFactor { get; set; }
        public static ConfigEntry<float> dofFocusDistance { get; set; }
        public static ConfigEntry<float> dofRadius { get; set; }
        public static ConfigEntry<float> dofRadiusEndSpeed { get; set; }
        public static ConfigEntry<float> dofRadiusStartSpeed { get; set; }
        private void Awake()
        {
            Debug.LogError("Sense Awake()");
            Hook = new GameObject();
            AmandsSenseClassComponent = Hook.AddComponent<AmandsSenseClass>();
            DontDestroyOnLoad(Hook);
        }
        private void Start()
        {
            EnableSense = Config.Bind("AmandsSense", "EnableSense", true, new ConfigDescription("", null, new ConfigurationManagerAttributes { Order = 690 }));
            SenseAlwaysOn = Config.Bind("AmandsSense", "AlwaysOn", false, new ConfigDescription("", null, new ConfigurationManagerAttributes { Order = 688 }));
            SenseKey = Config.Bind("AmandsSense", "SenseKey", new KeyboardShortcut(KeyCode.F), new ConfigDescription("", null, new ConfigurationManagerAttributes { Order = 370 }));
            DoubleClick = Config.Bind("AmandsSense", "DoubleClick", true, new ConfigDescription("", null, new ConfigurationManagerAttributes { Order = 360 }));
            DoubleClickDelay = Config.Bind("AmandsSense", "DoubleClickDelay", 0.5f, new ConfigDescription("", null, new ConfigurationManagerAttributes { Order = 350, IsAdvanced = true }));
            Cooldown = Config.Bind("AmandsSense", "Cooldown", 2f, new ConfigDescription("", null, new ConfigurationManagerAttributes { Order = 340 }));
            Speed = Config.Bind("AmandsSense", "Speed", 20f, new ConfigDescription("", null, new ConfigurationManagerAttributes { Order = 330 }));
            Duration = Config.Bind("AmandsSense", "Duration", 10f, new ConfigDescription("", null, new ConfigurationManagerAttributes { Order = 320 }));
            Radius = Config.Bind("AmandsSense", "Radius", 10, new ConfigDescription("", null, new ConfigurationManagerAttributes { Order = 310 }));
            DeadbodyRadius = Config.Bind("AmandsSense", "Deadbody Radius", 20, new ConfigDescription("", null, new ConfigurationManagerAttributes { Order = 308 }));
            AlwaysOnRadius = Config.Bind("AmandsSense", "AlwaysOnRadius", 20, new ConfigDescription("", null, new ConfigurationManagerAttributes { Order = 306 }));
            AlwaysOnDeadbodyRadius = Config.Bind("AmandsSense", "AlwaysOnDeadbody Radius", 20, new ConfigDescription("", null, new ConfigurationManagerAttributes { Order = 304 }));
            AlwaysOnFrequency = Config.Bind("AmandsSense", "AlwaysOn Frequency", 2f, new ConfigDescription("", null, new ConfigurationManagerAttributes { Order = 302, IsAdvanced = true }));
            MaxHeight = Config.Bind("AmandsSense", "MaxHeight", 3f, new ConfigDescription("", null, new ConfigurationManagerAttributes { Order = 300, IsAdvanced = true }));
            MinHeight = Config.Bind("AmandsSense", "MinHeight", -1f, new ConfigDescription("", null, new ConfigurationManagerAttributes { Order = 290, IsAdvanced = true }));
            Limit = Config.Bind("AmandsSense", "Limit", 200, new ConfigDescription("", null, new ConfigurationManagerAttributes { Order = 280, IsAdvanced = true }));
            NonFleaAmmo = Config.Bind("AmandsSense", "NonFleaAmmo", true, new ConfigDescription("", null, new ConfigurationManagerAttributes { Order = 278, IsAdvanced = true }));

            useDof = Config.Bind("AmandsSense", "useDof", true, new ConfigDescription("", null, new ConfigurationManagerAttributes { Order = 270 }));
            dofForceEnableMedian = Config.Bind("AmandsSense", "dofForceEnableMedian", false, new ConfigDescription("", null, new ConfigurationManagerAttributes { Order = 260, IsAdvanced = true }));
            dofBokehFactor = Config.Bind("AmandsSense", "dofBokehFactor", 157f, new ConfigDescription("", null, new ConfigurationManagerAttributes { Order = 250, IsAdvanced = true }));
            dofFocusDistance = Config.Bind("AmandsSense", "dofFocusDistance", 2f, new ConfigDescription("", null, new ConfigurationManagerAttributes { Order = 240, IsAdvanced = true }));
            dofRadius = Config.Bind("AmandsSense", "dofRadius", 0.5f, new ConfigDescription("", null, new ConfigurationManagerAttributes { Order = 230 }));
            dofRadiusEndSpeed = Config.Bind("AmandsSense", "dofRadiusEndSpeed", 0.5f, new ConfigDescription("", null, new ConfigurationManagerAttributes { Order = 220, IsAdvanced = true }));
            dofRadiusStartSpeed = Config.Bind("AmandsSense", "dofRadiusStartSpeed", 2f, new ConfigDescription("", null, new ConfigurationManagerAttributes { Order = 210, IsAdvanced = true }));

            Size = Config.Bind<Vector2>("AmandsSense", "Size", new Vector2(-0.07f, 0.07f), new ConfigDescription("", null, new ConfigurationManagerAttributes { Order = 200 }));
            NewSize = Config.Bind<Vector2>("AmandsSense", "NewSize", new Vector2(-0.15f, 0.15f), new ConfigDescription("", null, new ConfigurationManagerAttributes { Order = 190 }));
            AlwaysOnSize = Config.Bind<Vector2>("AmandsSense", "AlwaysOnSize", new Vector2(-0.3f, 0.3f), new ConfigDescription("", null, new ConfigurationManagerAttributes { Order = 188 }));
            SizeClamp = Config.Bind<float>("AmandsSense", "SizeClamp", 4.0f, new ConfigDescription("", null, new ConfigurationManagerAttributes { Order = 180, IsAdvanced = true }));
            NormalSize = Config.Bind<float>("AmandsSense", "NormalSize", 0.15f, new ConfigDescription("", null, new ConfigurationManagerAttributes { Order = 170 }));
            StartOpacitySpeed = Config.Bind<float>("AmandsSense", "StartOpacitySpeed", 2f, new ConfigDescription("", null, new ConfigurationManagerAttributes { Order = 160, IsAdvanced = true }));
            OpacitySpeed = Config.Bind<float>("AmandsSense", "OpacitySpeed", 2f, new ConfigDescription("", null, new ConfigurationManagerAttributes { Order = 150, IsAdvanced = true }));

            LightIntensity = Config.Bind<float>("AmandsSense", "LightIntensity", 0.3f, new ConfigDescription("", null, new ConfigurationManagerAttributes { Order = 140 }));
            LightRange = Config.Bind<float>("AmandsSense", "LightRange", 5f, new ConfigDescription("", null, new ConfigurationManagerAttributes { Order = 130 }));

            AudioDistance = Config.Bind<float>("AmandsSense", "AudioDistance", 99f, new ConfigDescription("", null, new ConfigurationManagerAttributes { Order = 120 }));
            AudioRolloff = Config.Bind<int>("AmandsSense", "AudioRolloff", 100, new ConfigDescription("", null, new ConfigurationManagerAttributes { Order = 110 }));
            AudioVolume = Config.Bind<float>("AmandsSense", "AudioVolume", 0.5f, new ConfigDescription("", null, new ConfigurationManagerAttributes { Order = 100 }));

            RareItemsColor = Config.Bind("Colors", "RareItemsColor", new Color(1.0f, 0.01f, 0.01f, 0.8f), new ConfigDescription("", null, new ConfigurationManagerAttributes { Order = 540 }));
            WishListItemsColor = Config.Bind("Colors", "WishListItemsColor", new Color(1.0f, 0.01f, 0.2f, 0.8f), new ConfigDescription("", null, new ConfigurationManagerAttributes { Order = 530 }));
            NonFleaItemsColor = Config.Bind("Colors", "NonFleaItemsColor", new Color(1.0f, 0.12f, 0.01f, 0.8f), new ConfigDescription("", null, new ConfigurationManagerAttributes { Order = 520 }));
            KappaItemsColor = Config.Bind("Colors", "KappaItemsColor", new Color(1.0f, 1.0f, 0.01f, 0.8f), new ConfigDescription("", null, new ConfigurationManagerAttributes { Order = 510 }));

            LootableContainerColor = Config.Bind("Colors", "LootableContainerColor", new Color(0.36f, 0.18f, 1.0f, 0.8f), new ConfigDescription("", null, new ConfigurationManagerAttributes { Order = 500 }));
            ObservedLootItemColor = Config.Bind("Colors", "ObservedLootItemColor", new Color(0.84f, 0.88f, 0.95f, 0.8f), new ConfigDescription("", null, new ConfigurationManagerAttributes { Order = 490 }));

            OthersColor = Config.Bind("Colors", "OthersColor", new Color(0.84f, 0.88f, 0.95f, 0.8f), new ConfigDescription("", null, new ConfigurationManagerAttributes { Order = 480 }));
            BuildingMaterialsColor = Config.Bind("Colors", "BuildingMaterialsColor", new Color(0.84f, 0.88f, 0.95f, 0.8f), new ConfigDescription("", null, new ConfigurationManagerAttributes { Order = 470 }));
            ElectronicsColor = Config.Bind("Colors", "ElectronicsColor", new Color(0.84f, 0.88f, 0.95f, 0.8f), new ConfigDescription("", null, new ConfigurationManagerAttributes { Order = 460 }));
            EnergyElementsColor = Config.Bind("Colors", "EnergyElementsColor", new Color(0.84f, 0.88f, 0.95f, 0.8f), new ConfigDescription("", null, new ConfigurationManagerAttributes { Order = 450 }));
            FlammableMaterialsColor = Config.Bind("Colors", "FlammableMaterialsColor", new Color(0.84f, 0.88f, 0.95f, 0.8f), new ConfigDescription("", null, new ConfigurationManagerAttributes { Order = 440 }));
            HouseholdMaterialsColor = Config.Bind("Colors", "HouseholdMaterialsColor", new Color(0.84f, 0.88f, 0.95f, 0.8f), new ConfigDescription("", null, new ConfigurationManagerAttributes { Order = 430 }));
            MedicalSuppliesColor = Config.Bind("Colors", "MedicalSuppliesColor", new Color(0.84f, 0.88f, 0.95f, 0.8f), new ConfigDescription("", null, new ConfigurationManagerAttributes { Order = 420 }));
            ToolsColor = Config.Bind("Colors", "ToolsColor", new Color(0.84f, 0.88f, 0.95f, 0.8f), new ConfigDescription("", null, new ConfigurationManagerAttributes { Order = 410 }));
            ValuablesColor = Config.Bind("Colors", "ValuablesColor", new Color(0.36f, 0.18f, 1.0f, 0.8f), new ConfigDescription("", null, new ConfigurationManagerAttributes { Order = 400 }));

            BackpacksColor = Config.Bind("Colors", "BackpacksColor", new Color(0.84f, 0.88f, 0.95f, 0.8f), new ConfigDescription("", null, new ConfigurationManagerAttributes { Order = 390 }));
            BodyArmorColor = Config.Bind("Colors", "BodyArmorColor", new Color(0.84f, 0.88f, 0.95f, 0.8f), new ConfigDescription("", null, new ConfigurationManagerAttributes { Order = 380 }));
            EyewearColor = Config.Bind("Colors", "EyewearColor", new Color(0.84f, 0.88f, 0.95f, 0.8f), new ConfigDescription("", null, new ConfigurationManagerAttributes { Order = 370 }));
            FacecoversColor = Config.Bind("Colors", "FacecoversColor", new Color(0.84f, 0.88f, 0.95f, 0.8f), new ConfigDescription("", null, new ConfigurationManagerAttributes { Order = 360 }));
            GearComponentsColor = Config.Bind("Colors", "GearComponentsColor", new Color(0.84f, 0.88f, 0.95f, 0.8f), new ConfigDescription("", null, new ConfigurationManagerAttributes { Order = 350 }));
            HeadgearColor = Config.Bind("Colors", "HeadgearColor", new Color(0.84f, 0.88f, 0.95f, 0.8f), new ConfigDescription("", null, new ConfigurationManagerAttributes { Order = 340 }));
            HeadsetsColor = Config.Bind("Colors", "HeadsetsColor", new Color(0.84f, 0.88f, 0.95f, 0.8f), new ConfigDescription("", null, new ConfigurationManagerAttributes { Order = 330 }));
            SecureContainersColor = Config.Bind("Colors", "SecureContainersColor", new Color(0.84f, 0.88f, 0.95f, 0.8f), new ConfigDescription("", null, new ConfigurationManagerAttributes { Order = 320 }));
            StorageContainersColor = Config.Bind("Colors", "StorageContainersColor", new Color(0.84f, 0.88f, 0.95f, 0.8f), new ConfigDescription("", null, new ConfigurationManagerAttributes { Order = 310 }));
            TacticalRigsColor = Config.Bind("Colors", "TacticalRigsColor", new Color(0.84f, 0.88f, 0.95f, 0.8f), new ConfigDescription("", null, new ConfigurationManagerAttributes { Order = 300 }));

            FunctionalModsColor = Config.Bind("Colors", "FunctionalModsColor", new Color(0.1f, 0.35f, 0.65f, 0.8f), new ConfigDescription("", null, new ConfigurationManagerAttributes { Order = 290 }));
            GearModsColor = Config.Bind("Colors", "GearModsColor", new Color(0.15f, 0.5f, 0.1f, 0.8f), new ConfigDescription("", null, new ConfigurationManagerAttributes { Order = 280 }));
            VitalPartsColor = Config.Bind("Colors", "VitalPartsColor", new Color(0.7f, 0.2f, 0.1f, 0.8f), new ConfigDescription("", null, new ConfigurationManagerAttributes { Order = 270 }));

            AssaultCarbinesColor = Config.Bind("Colors", "AssaultCarbinesColor", new Color(0.84f, 0.88f, 0.95f, 0.8f), new ConfigDescription("", null, new ConfigurationManagerAttributes { Order = 260 }));
            AssaultRiflesColor = Config.Bind("Colors", "AssaultRiflesColor", new Color(0.84f, 0.88f, 0.95f, 0.8f), new ConfigDescription("", null, new ConfigurationManagerAttributes { Order = 250 }));
            BoltActionRiflesColor = Config.Bind("Colors", "BoltActionRiflesColor", new Color(0.84f, 0.88f, 0.95f, 0.8f), new ConfigDescription("", null, new ConfigurationManagerAttributes { Order = 240 }));
            GrenadeLaunchersColor = Config.Bind("Colors", "GrenadeLaunchersColor", new Color(0.84f, 0.88f, 0.95f, 0.8f), new ConfigDescription("", null, new ConfigurationManagerAttributes { Order = 230 }));
            MachineGunsColor = Config.Bind("Colors", "MachineGunsColor", new Color(0.84f, 0.88f, 0.95f, 0.8f), new ConfigDescription("", null, new ConfigurationManagerAttributes { Order = 220 }));
            MarksmanRiflesColor = Config.Bind("Colors", "MarksmanRiflesColor", new Color(0.84f, 0.88f, 0.95f, 0.8f), new ConfigDescription("", null, new ConfigurationManagerAttributes { Order = 210 }));
            MeleeWeaponsColor = Config.Bind("Colors", "MeleeWeaponsColor", new Color(0.84f, 0.88f, 0.95f, 0.8f), new ConfigDescription("", null, new ConfigurationManagerAttributes { Order = 200 }));
            PistolsColor = Config.Bind("Colors", "PistolsColor", new Color(0.84f, 0.88f, 0.95f, 0.8f), new ConfigDescription("", null, new ConfigurationManagerAttributes { Order = 190 }));
            SMGsColor = Config.Bind("Colors", "SMGsColor", new Color(0.84f, 0.88f, 0.95f, 0.8f), new ConfigDescription("", null, new ConfigurationManagerAttributes { Order = 180 }));
            ShotgunsColor = Config.Bind("Colors", "ShotgunsColor", new Color(0.84f, 0.88f, 0.95f, 0.8f), new ConfigDescription("", null, new ConfigurationManagerAttributes { Order = 170 }));
            SpecialWeaponsColor = Config.Bind("Colors", "SpecialWeaponsColor", new Color(0.84f, 0.88f, 0.95f, 0.8f), new ConfigDescription("", null, new ConfigurationManagerAttributes { Order = 160 }));
            ThrowablesColor = Config.Bind("Colors", "ThrowablesColor", new Color(0.84f, 0.88f, 0.95f, 0.8f), new ConfigDescription("", null, new ConfigurationManagerAttributes { Order = 150 }));

            AmmoPacksColor = Config.Bind("Colors", "AmmoPacksColor", new Color(0.84f, 0.88f, 0.95f, 0.8f), new ConfigDescription("", null, new ConfigurationManagerAttributes { Order = 140 }));
            RoundsColor = Config.Bind("Colors", "RoundsColor", new Color(0.84f, 0.88f, 0.95f, 0.8f), new ConfigDescription("", null, new ConfigurationManagerAttributes { Order = 130 }));
            DrinksColor = Config.Bind("Colors", "DrinksColor", new Color(0.13f, 0.66f, 1.0f, 0.8f), new ConfigDescription("", null, new ConfigurationManagerAttributes { Order = 120 }));
            FoodColor = Config.Bind("Colors", "FoodColor", new Color(0.84f, 0.88f, 0.95f, 0.8f), new ConfigDescription("", null, new ConfigurationManagerAttributes { Order = 110 }));
            InjectorsColor = Config.Bind("Colors", "InjectorsColor", new Color(0.84f, 0.88f, 0.95f, 0.8f), new ConfigDescription("", null, new ConfigurationManagerAttributes { Order = 100 }));
            InjuryTreatmentColor = Config.Bind("Colors", "InjuryTreatmentColor", new Color(0.84f, 0.88f, 0.95f, 0.8f), new ConfigDescription("", null, new ConfigurationManagerAttributes { Order = 90 }));
            MedkitsColor = Config.Bind("Colors", "MedkitsColor", new Color(0.3f, 1.0f, 0.13f, 0.8f), new ConfigDescription("", null, new ConfigurationManagerAttributes { Order = 80 }));
            PillsColor = Config.Bind("Colors", "PillsColor", new Color(0.84f, 0.88f, 0.95f, 0.8f), new ConfigDescription("", null, new ConfigurationManagerAttributes { Order = 70 }));

            ElectronicKeysColor = Config.Bind("Colors", "ElectronicKeysColor", new Color(1.0f, 0.01f, 0.01f, 0.8f), new ConfigDescription("", null, new ConfigurationManagerAttributes { Order = 60 }));
            MechanicalKeysColor = Config.Bind("Colors", "MechanicalKeysColor", new Color(0.84f, 0.88f, 0.95f, 0.8f), new ConfigDescription("", null, new ConfigurationManagerAttributes { Order = 50 }));

            InfoItemsColor = Config.Bind("Colors", "InfoItemsColor", new Color(0.84f, 0.88f, 0.95f, 0.8f), new ConfigDescription("", null, new ConfigurationManagerAttributes { Order = 40 }));
            QuestItemsColor = Config.Bind("Colors", "QuestItemsColor", new Color(1.0f, 1.0f, 0.01f, 0.8f), new ConfigDescription("", null, new ConfigurationManagerAttributes { Order = 38 }));
            SpecialEquipmentColor = Config.Bind("Colors", "SpecialEquipmentColor", new Color(0.84f, 0.88f, 0.95f, 0.8f), new ConfigDescription("", null, new ConfigurationManagerAttributes { Order = 30 }));
            MapsColor = Config.Bind("Colors", "MapsColor", new Color(0.84f, 0.88f, 0.95f, 0.8f), new ConfigDescription("", null, new ConfigurationManagerAttributes { Order = 20 }));
            MoneyColor = Config.Bind("Colors", "MoneyColor", new Color(0.84f, 0.88f, 0.95f, 0.8f), new ConfigDescription("", null, new ConfigurationManagerAttributes { Order = 10 }));

            new AmandsLocalPlayerPatch().Enable();
            new AmandsSensePrismEffectsPatch().Enable();
        }
    }
    public class AmandsLocalPlayerPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(LocalPlayer).GetMethod("Create", BindingFlags.Static | BindingFlags.Public);
        }
        [PatchPostfix]
        private static void PatchPostFix(ref Task<LocalPlayer> __result)
        {
            LocalPlayer localPlayer = __result.Result;
            if (localPlayer != null && localPlayer.IsYourPlayer)
            {
                AmandsSenseClass.localPlayer = localPlayer;
                AmandsSenseClass.ItemsSenses.Clear();
                AmandsSenseClass.ItemsAlwaysOn.Clear();
                AmandsSenseClass.ContainersAlwaysOn.Clear();
                AmandsSenseClass.DeadbodyAlwaysOn.Clear();
                AmandsSensePlugin.AmandsSenseClassComponent.DynamicAlwaysOnSense();
            }
        }
    }
    public class AmandsSensePrismEffectsPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(PrismEffects).GetMethod("OnEnable", BindingFlags.Instance | BindingFlags.NonPublic);
        }
        [PatchPostfix]
        private static void PatchPostFix(ref PrismEffects __instance)
        {
            if (__instance.gameObject.name == "FPS Camera")
            {
                AmandsSenseClass.prismEffects = __instance;
            }
        }
    }
}
