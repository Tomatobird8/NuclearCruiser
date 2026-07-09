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
[BepInDependency("JacobG5.JLL", BepInDependency.DependencyFlags.HardDependency)] // added since it was missing and is a required dependancy, but i'm not sure where it is used

public class NuclearCruiser : BaseUnityPlugin
{
    public static NuclearCruiser Instance { get; private set; } = null!;
    internal new static ManualLogSource Logger { get; private set; } = null!;
    internal static Harmony? Harmony { get; set; }

    internal static string IsNuclear = "isNuclear";

    internal static Texture2D? cruiserTexture;
    internal static Texture2D? destroyedCruiserTexture;

    internal static GameObject? nukeObject;

    internal static float nuclearCruiserChance = 1f;

    internal static float nukeScale = 0.5f;
    internal static ConfigEntry<float> nukeVolume = null!;
    internal static ConfigEntry<float> nukeLightIntensity = null!;
    internal static ConfigEntry<int> nuclearCruiserCompensation = null!;
    internal static ConfigEntry<bool> compensationNotification = null!;
    internal static float decalVisibilityRange = 48f;
    internal static float decalScale = 2f;
    internal static float protectionTime = 3f;
    internal static bool infiniteBoosts = true;
    internal static bool nuclearCruiserWarning = true;
    internal static bool nuclearCruiserRadiationWarning = true;
    internal static Fragility cruiserFragility;
    internal static float minimumCrashVelocity = 4f;
    internal static int crashDamage = 4;

    internal static bool forcePatchCruiserStart = false;
    internal static bool isFasterDropshipLoaded = false;

    public void Awake()
    {
        Logger = base.Logger;
        Instance = this;

        cruiserTexture = GetTexture(Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "cruiser.png"));
        destroyedCruiserTexture = GetTexture(Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "cruiser_blown.png"));

        AssetBundle bundle = AssetBundle.LoadFromFile(Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "cruisernuke"));
        nukeObject = bundle.LoadAsset<GameObject>("Assets/LethalCompany/Mods/TomatoScrap/Prefabs/Miscallaneous/NuclearBombCruiser.prefab");

        if (cruiserTexture == null || destroyedCruiserTexture == null || nukeObject == null)
        {
            Logger.LogError("Failed to load textures. Plugin loading failed.");
        }
        else
        {
            nuclearCruiserChance = Config.Bind<float>("General", "NuclearCruiserChance", 1f, new ConfigDescription("Chance of cruiser being a Nuclear Cruiser. 1 is always, 0 is never.", new AcceptableValueRange<float>(0f, 1f))).Value;

            nukeScale = Config.Bind<float>("General", "NukeScale", 0.75f, "How large should the explosion be? 0.5 can already cover most of a moon's surface.").Value;
            nukeVolume = Config.Bind<float>("General", "NukeVolume", 0.67f, new ConfigDescription("Sound volume of the nuclear explosion.", new AcceptableValueRange<float>(0f, 1f)));
            nukeLightIntensity = Config.Bind<float>("General", "NukeLightIntensity", 1f, new ConfigDescription("Intensity of the light from the nuclear explosion.", new AcceptableValueRange<float>(0f, 1f)));
            nuclearCruiserCompensation = Config.Bind("General", "PurchaseCompensation", 0, "If set to more than 0, this amount of credits will be added to the terminal when a nuclear cruiser is spawned.");
            compensationNotification = Config.Bind("General", "CompensationNotification", true, "Should the added compensation be announced in chat?");
            decalVisibilityRange = Config.Bind<float>("General", "DecalVisibilityRange", 48f, "How far nuke explosion decals are visible.").Value;
            decalScale = Config.Bind<float>("General", "DecalScale", 4f, "How large are the nuke explosion decals.").Value;
            protectionTime = Config.Bind<float>("General", "ProtectionTime", 3f, "Amount of time in seconds the cruiser stays more protected from damage when detached from dropship.").Value;
            infiniteBoosts = Config.Bind<bool>("General", "InfiniteBoosts", true, "Should nuclear cruiser have infinite boosts?").Value;
            nuclearCruiserWarning = Config.Bind<bool>("General", "NuclearCruiserWarning", true, "Should a warning pop up when a Nuclear Cruiser is spawned?").Value;
            nuclearCruiserRadiationWarning = Config.Bind<bool>("General", "NuclearCruiserRadiationWarning", true, "Should a radiation warning pop up when a Nuclear Cruiser is spawned?").Value;
            cruiserFragility = Config.Bind<Fragility>("General", "CruiserFragility", Fragility.Fragile, "Fragility of the cruiser. Fragile makes cruiser take heavy damage from smaller impacts. Extreme makes cruiser on any impact past minimum crash velocity threshold and may explode upon landing on some moons if set too low.").Value;
            minimumCrashVelocity = Config.Bind<float>("General", "MinimumCrashVelocity", 4f, "Damaging impact velocity threshold. Default threshold is reached at very low speeds.").Value;
            crashDamage = Config.Bind<int>("General", "CrashDamage", 4, "Amount of damage cruiser takes on impact. Only used when CruiserFragility is set to Fragile.").Value;

            forcePatchCruiserStart = Config.Bind("Compatibility", "ForcePatchCruiserStart", false, "This patch is automatically applied if FasterItemDropShip is installed. You can also manually force it here.").Value;

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
