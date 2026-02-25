using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using System.IO;
using System.Reflection;
using UnityEngine;

namespace NuclearCruiser
{
    [BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]

    public class NuclearCruiser : BaseUnityPlugin
    {
        public static NuclearCruiser Instance { get; private set; } = null!;
        internal new static ManualLogSource Logger { get; private set; } = null!;
        internal static Harmony? Harmony { get; set; }

        internal static Texture2D? cruiserTexture;
        internal static Texture2D? destroyedCruiserTexture;

        internal static GameObject? nukeObject;

        internal static float nukeScale = 0.5f;
        internal static float nuclearCruiserChance = 1f;
        internal static bool infiniteBoosts = true;
        internal static bool nuclearCruiserWarning = true;
        internal static bool nuclearCruiserRadiationWarning = true;

        private void Awake()
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
                nuclearCruiserChance = Config.Bind<float>("General", "NuclearCruiserChance", 1f, new BepInEx.Configuration.ConfigDescription("Chance of cruiser being a Nuclear Cruiser. 1 is always, 0 is never.", new BepInEx.Configuration.AcceptableValueRange<float>(0f, 1f))).Value;
                nukeScale = Config.Bind<float>("General", "NukeScale", 0.5f, "How large should the explosion be? 0.5 can already cover most of a moon surface.").Value;
                infiniteBoosts = Config.Bind<bool>("General", "InfiniteBoosts", true, "Should nuclear cruiser have infinite boosts?").Value;
                nuclearCruiserWarning = Config.Bind<bool>("General", "NuclearCruiserWarning", true, "Should a warning pop up when a Nuclear Cruiser is spawned?").Value;
                nuclearCruiserRadiationWarning = Config.Bind<bool>("General", "NuclearCruiserRadiationWarning", true, "Should a radiation warning pop up when a Nuclear Cruiser is spawned?").Value;

                nukeObject.transform.localScale *= nukeScale;
                cruiserTexture.name = "nukeCruiserTexture";
                destroyedCruiserTexture.name = "blownNukeCruiserTexture";
                Patch();
                PatchNetwork();
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

        internal static void PatchNetwork()
        {
            var types = Assembly.GetExecutingAssembly().GetTypes();
            foreach (var type in types)
            {
                var methods = type.GetMethods(BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);
                foreach (var method in methods)
                {
                    var attributes = method.GetCustomAttributes(typeof(RuntimeInitializeOnLoadMethodAttribute), false);
                    if (attributes.Length > 0)
                    {
                        method.Invoke(null, null);
                    }
                }
            }
        }

        internal static void Patch()
        {
            Harmony ??= new Harmony(MyPluginInfo.PLUGIN_GUID);

            Logger.LogDebug("Patching...");

            Harmony.PatchAll();

            Logger.LogDebug("Finished patching!");
        }

        internal static void Unpatch()
        {
            Logger.LogDebug("Unpatching...");

            Harmony?.UnpatchSelf();

            Logger.LogDebug("Finished unpatching!");
        }
    }
}
