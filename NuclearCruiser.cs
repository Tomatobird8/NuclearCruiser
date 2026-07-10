using BepInEx;
using BepInEx.Bootstrap;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using NuclearCruiser.Patches;
using System.IO;
using System.Reflection;
using UnityEngine;
using UnityEngine.Rendering.HighDefinition;

namespace NuclearCruiser;

[BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
[BepInDependency("JacobG5.JLL", BepInDependency.DependencyFlags.HardDependency)]

public class NuclearCruiser : BaseUnityPlugin
{
    public static NuclearCruiser Instance { get; private set; } = null!;
    internal new static ManualLogSource Logger { get; private set; } = null!;
    internal static Harmony? Harmony { get; set; }

    // Save data
    internal static string IsNuclear = "isNuclear";

    // Texture references
    internal static Texture2D? cruiserTexture;
    internal static Texture2D? destroyedCruiserTexture;

    // Nuclear blast object reference
    internal static GameObject? nukeObject;

    // Compatibility
    internal static bool isFasterDropshipLoaded = false;

    // --- CONFIG CATEGORIES ---
    internal static string cfgGeneral = "General";
    internal static string cfgCruiser = "Cruiser";
    internal static string cfgNuke = "Explosion";
    internal static string cfgCompat = "Compatibility";

    // --- CONFIG ---
    // General
    internal static ConfigEntry<float> nuclearCruiserChance = null!;
    internal static bool nuclearCruiserWarning = true;
    internal static bool nuclearCruiserRadiationWarning = true;

    // Cruiser
    internal static float protectionTime = 3f;
    internal static bool infiniteBoosts = true;
    internal static Fragility cruiserFragility;
    internal static float minimumCrashVelocity = 4f;
    internal static int crashDamage = 4;
    internal static ConfigEntry<int> nuclearCruiserCompensation = null!;
    internal static ConfigEntry<bool> compensationNotification = null!;

    // Explosion
    internal static float nukeScale = 0.5f;
    internal static ConfigEntry<float> nukeVolume = null!;
    internal static ConfigEntry<float> nukeLightIntensity = null!;
    internal static float decalVisibilityRange = 48f;
    internal static float decalScale = 2f;
    internal static ConfigEntry<bool> shakeCamera = null!;

    // Compatibility
    internal static bool forcePatchCruiserStart = false;

    public void Awake()
    {
        Logger = base.Logger;
        Instance = this;

        cruiserTexture = GetTexture(Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "cruiser.png"));
        destroyedCruiserTexture = GetTexture(Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "cruiser_blown.png"));

        AssetBundle bundle = AssetBundle.LoadFromFile(Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "cruisernuke"));
        if (bundle != null) nukeObject = bundle.LoadAsset<GameObject>("Assets/LethalCompany/Mods/TomatoScrap/Prefabs/Miscallaneous/NuclearBombCruiser.prefab");

        if (cruiserTexture == null || destroyedCruiserTexture == null || nukeObject == null)
        {
            Logger.LogError("Failed to load assets. Plugin loading aborted.");
            return;
        }
        nuclearCruiserChance = Config.Bind(cfgGeneral, "NuclearCruiserChance", 1f, new ConfigDescription("Chance of cruiser being a Nuclear Cruiser. 1 is always, 0 is never.", new AcceptableValueRange<float>(0f, 1f)));
        nuclearCruiserWarning = Config.Bind(cfgGeneral, "NuclearCruiserWarning", true, "Should a warning pop up when a Nuclear Cruiser is spawned?").Value;
        nuclearCruiserRadiationWarning = Config.Bind(cfgGeneral, "NuclearCruiserRadiationWarning", true, "Should a radiation warning pop up when a Nuclear Cruiser is spawned?").Value;

        nukeScale = Config.Bind(cfgNuke, "NukeScale", 0.75f, "How large should the explosion be? 0.5 can already cover most of a moon's surface.").Value;
        nukeVolume = Config.Bind(cfgNuke, "NukeVolume", 0.67f, new ConfigDescription("Sound volume of the nuclear explosion.", new AcceptableValueRange<float>(0f, 1f)));
        nukeLightIntensity = Config.Bind(cfgNuke, "NukeLightIntensity", 1f, new ConfigDescription("Intensity of the light from the nuclear explosion.", new AcceptableValueRange<float>(0f, 1f)));
        decalVisibilityRange = Config.Bind(cfgNuke, "DecalVisibilityRange", 48f, "How far nuke explosion decals are visible.").Value;
        decalScale = Config.Bind(cfgNuke, "DecalScale", 4f, "How large are the nuke explosion decals.").Value;
        shakeCamera = Config.Bind(cfgNuke, "ShakeCamera", true, "Should camera shake upon explosion?");

        infiniteBoosts = Config.Bind(cfgCruiser, "InfiniteBoosts", true, "Should nuclear cruiser have infinite boosts?").Value;
        cruiserFragility = Config.Bind<Fragility>(cfgCruiser, "CruiserFragility", Fragility.Fragile, "Fragility of the cruiser. Fragile makes cruiser take heavy damage from smaller impacts. Extreme makes cruiser on any impact past minimum crash velocity threshold and may explode upon landing on some moons if set too low.").Value;
        minimumCrashVelocity = Config.Bind(cfgCruiser, "MinimumCrashVelocity", 4f, "Damaging impact velocity threshold. Default threshold is reached at very low speeds.").Value;
        crashDamage = Config.Bind(cfgCruiser, "CrashDamage", 4, "Amount of damage cruiser takes on impact. Only used when CruiserFragility is set to Fragile.").Value;
        protectionTime = Config.Bind(cfgCruiser, "ProtectionTime", 3f, "Amount of time in seconds the cruiser stays more protected from damage when detached from dropship.").Value;
        nuclearCruiserCompensation = Config.Bind(cfgCruiser, "PurchaseCompensation", 0, "If set to more than 0, this amount of credits will be added to the terminal when a nuclear cruiser is spawned.");
        compensationNotification = Config.Bind(cfgCruiser, "CompensationNotification", true, "Should the added compensation be announced in chat?");

        forcePatchCruiserStart = Config.Bind(cfgCompat, "ForcePatchCruiserStart", false, "This patch is automatically applied if FasterItemDropShip is installed. You can also manually force it here.").Value;

        nukeObject.transform.localScale *= nukeScale;

        Transform nukeDecalTransform = nukeObject.transform.GetChild(0);
        nukeDecalTransform.rotation = Quaternion.Euler(90f, 0f, 0f);
        nukeDecalTransform.localScale *= decalScale;
        nukeDecalTransform.TryGetComponent(out DecalProjector decal);
        decal.drawDistance = decalVisibilityRange;

        nukeObject.transform.GetChild(5).TryGetComponent(out AudioSource nearNukeAudio);
        nukeObject.transform.GetChild(5).GetChild(0).TryGetComponent(out AudioSource farNukeAudio);
        nearNukeAudio.volume = nukeVolume.Value;
        farNukeAudio.volume = nukeVolume.Value;

        nukeObject.transform.GetChild(1).TryGetComponent(out HDAdditionalLightData light1);
        nukeObject.transform.GetChild(2).TryGetComponent(out HDAdditionalLightData light2);
        light1.color = new Color(nukeLightIntensity.Value, nukeLightIntensity.Value, nukeLightIntensity.Value, 1f);
        light2.color = new Color(nukeLightIntensity.Value, nukeLightIntensity.Value, nukeLightIntensity.Value, 1f);

        cruiserTexture.name = "nukeCruiserTexture";
        destroyedCruiserTexture.name = "blownNukeCruiserTexture";
        isFasterDropshipLoaded = Chainloader.PluginInfos.ContainsKey("FlipMods.FasterItemDropship");
        Patch();
        Logger.LogInfo($"{MyPluginInfo.PLUGIN_GUID} v{MyPluginInfo.PLUGIN_VERSION} has loaded!");
    }

    internal static Texture2D? GetTexture(string path)
    {
        if (File.Exists(path))
        {
            Texture2D t = new Texture2D(2, 2);
            ImageConversion.LoadImage(t, File.ReadAllBytes(path));
            return t;
        }
        return null;
    }

    internal static void Patch()
    {
        Harmony ??= new Harmony(MyPluginInfo.PLUGIN_GUID);

        Logger.LogDebug("Patching...");

        Harmony.PatchAll(typeof(GameNetworkManagerPatch));
        Harmony.PatchAll(typeof(VehicleControllerPatch));
        Harmony.PatchAll(typeof(StartOfRoundPatch));

        if (isFasterDropshipLoaded || forcePatchCruiserStart) Harmony.PatchAll(typeof(FasterDropshipCompatibilityPatch));

        Logger.LogDebug("Finished patching!");
    }

    internal static void Unpatch()
    {
        Logger.LogDebug("Unpatching...");

        Harmony?.UnpatchSelf();

        Logger.LogDebug("Finished unpatching!");
    }

    internal enum Fragility
    {
        Default,
        Fragile,
        Extreme
    }
}
