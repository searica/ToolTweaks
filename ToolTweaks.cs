// Ignore Spelling: ToolTweaks Jotunn

using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using ToolTweaks.Configs;
using ToolTweaks.Extensions;
using System.Reflection;
using UnityEngine;
using System;
using Jotunn.Managers;
using Jotunn.Utils;

namespace ToolTweaks
{
    [BepInPlugin(PluginGUID, PluginName, PluginVersion)]
    [BepInDependency(Jotunn.Main.ModGuid, Jotunn.Main.Version)]
    [NetworkCompatibility(CompatibilityLevel.VersionCheckOnly, VersionStrictness.Patch)]
    [SynchronizationMode(AdminOnlyStrictness.IfOnServer)]
    internal sealed class ToolTweaks : BaseUnityPlugin
    {
        internal const string Author = "Searica";
        public const string PluginName = "ToolTweaks";
        public const string PluginGUID = $"{Author}.Valheim.{PluginName}";
        public const string PluginVersion = "1.4.0";

        // Use this class to add your own localization to the game
        // https://valheim-modding.github.io/Jotunn/tutorials/localization.html
        //public static CustomLocalization Localization = LocalizationManager.Instance.GetLocalization();

        private static readonly string MainSection = ConfigManager.SetStringPriority("Global", 3);

        private static ConfigEntry<float> useDelay;
        private static ConfigEntry<float> staminaMult;
        private static ConfigEntry<float> durabiltyMult;
        internal static float UseageDelay => useDelay.Value;
        internal static float StaminaMultiplier => staminaMult.Value;
        internal static float DurabilityMultiplier => durabiltyMult.Value;

        private static bool ShouldUpdatePlugin = false;

        public void Awake()
        {
            Log.Init(Logger);

            ConfigManager.Init(PluginGUID, Config, false);
            SetUpConfigEntries();

            Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly(), harmonyInstanceId: PluginGUID);
            Game.isModded = true;

            ConfigManager.SetupWatcher();

            ConfigManager.OnConfigFileReloaded += UpdatePlugin;
            SynchronizationManager.OnConfigurationWindowClosed += UpdatePlugin;
            SynchronizationManager.OnConfigurationSynchronized += (obj, attr) => { UpdatePlugin(); };
        }

        public void OnDestroy()
        {
            ConfigManager.Save();
        }

        internal static void SetUpConfigEntries()
        {
            Log.Verbosity = ConfigManager.BindConfig(
                MainSection,
                "Verbosity",
                LogLevel.Low,
                "Low will log basic information about the mod. Medium will log information that " +
                "is useful for troubleshooting. High will log a lot of information, do not set " +
                "it to this without good reason as it will slow Down your game.",
                synced: false
            );

            useDelay = ConfigManager.BindConfig(
                MainSection,
                "Usage Delay",
                0.25f,
                "Set the time delay between tool uses for both placement and removal. Vanilla default is 0.4s for placement and 0.25s for removal.",
                new AcceptableValueRange<float>(0.05f, 2f)
            );

            staminaMult = ConfigManager.BindConfig(
                MainSection,
                "Stamina Cost Multiplier",
                0.5f,
                "Change the stamina cost for using tools. Setting to 0.5 means stamina costs are reduced to 50%. Setting to 2 means stamina costs are increased to 200%.",
                new AcceptableValueRange<float>(0.0f, 2f)
            );

            durabiltyMult = ConfigManager.BindConfig(
                MainSection,
                "Durability Drain Multiplier",
                0.5f,
                "Change the amount of durability drained each time a tool is used. Setting to 0.5 means durability is drained 50% as much. Setting to 2 means durability drain is increased to 200%.",
                new AcceptableValueRange<float>(0.0f, 2f)
            );

            useDelay.SettingChanged += SetUpdatePlugin;
            staminaMult.SettingChanged += SetUpdatePlugin;
            durabiltyMult.SettingChanged += SetUpdatePlugin;
            ConfigManager.Save();
        }

        private static void SetUpdatePlugin(object obj, EventArgs e)
        {
            ShouldUpdatePlugin = !ShouldUpdatePlugin | ShouldUpdatePlugin;
        }

        private static void UpdatePlugin()
        {
            if (ShouldUpdatePlugin)
            {
                var player = Player.m_localPlayer;
                if (player)
                {
                    player.m_placeDelay = UseageDelay;
                    player.m_removeDelay = UseageDelay;
                }

                ShouldUpdatePlugin = false;
                ConfigManager.Save();
            }
        }
    }

    [HarmonyPatch(typeof(Player))]
    internal static class PlayerPatch
    {
        private static bool UsingBuildTool = false;

        [HarmonyPostfix]
        [HarmonyPatch(nameof(Player.Awake))]
        private static void AwakePostfix(Player __instance)
        {
            if (!__instance) { return; }

            __instance.m_placeDelay = ToolTweaks.UseageDelay;
            __instance.m_removeDelay = ToolTweaks.UseageDelay;
        }


        [HarmonyPrefix]
        [HarmonyPriority(Priority.Last)]
        [HarmonyPatch(nameof(Player.UpdatePlacement))]
        private static void UpdatePlacementPrefix(Player __instance, out float __state)
        {
            if (__instance && __instance.InPlaceMode() && !__instance.IsDead())
            {
                var rightItem = __instance.GetRightItem();
                if (rightItem != null)
                {
                    UsingBuildTool = true;
                    __state = rightItem.m_shared.m_useDurabilityDrain;
                    rightItem.m_shared.m_useDurabilityDrain *= ToolTweaks.DurabilityMultiplier;
                    return;
                }
            }
            __state = -1;
        }


        [HarmonyPostfix]
        [HarmonyPriority(Priority.First)]
        [HarmonyPatch(nameof(Player.UpdatePlacement))]
        private static void UpdatePlacementPostfix(Player __instance, float __state)
        {
            if (UsingBuildTool)
            {
                if (__instance && __state != -1)
                {
                    __instance.GetRightItem().m_shared.m_useDurabilityDrain = __state;
                }
                UsingBuildTool = false;
            }
        }


        [HarmonyPrefix]
        [HarmonyPatch(nameof(Player.UseStamina))]
        private static void UseStaminaPrefix(Player __instance, ref float v)
        {
            if (!UsingBuildTool || !__instance || !IsWieldingTool(__instance)) { return; }
            v *= ToolTweaks.StaminaMultiplier;
        }


        private static bool IsWieldingTool(Player player)
        {
            if (player)
            {
                return player.GetRightItem().m_shared.m_itemType == ItemDrop.ItemData.ItemType.Tool;
            }
            return false;
        }
    }

    /// <summary>
    ///     Log level to control output to BepInEx log
    /// </summary>
    internal enum LogLevel
    {
        Low = 0,
        Medium = 1,
        High = 2,
    }

    /// <summary>
    ///     Helper class for properly logging from static contexts.
    /// </summary>
    internal static class Log
    {
        #region Verbosity

        internal static ConfigEntry<LogLevel> Verbosity { get; set; }
        internal static LogLevel VerbosityLevel => Verbosity.Value;
        internal static bool IsVerbosityLow => Verbosity.Value >= LogLevel.Low;
        internal static bool IsVerbosityMedium => Verbosity.Value >= LogLevel.Medium;
        internal static bool IsVerbosityHigh => Verbosity.Value >= LogLevel.High;

        #endregion Verbosity

        private static ManualLogSource logSource;

        internal static void Init(ManualLogSource logSource)
        {
            Log.logSource = logSource;
        }

        internal static void LogDebug(object data) => logSource.LogDebug(data);

        internal static void LogError(object data) => logSource.LogError(data);

        internal static void LogFatal(object data) => logSource.LogFatal(data);

        internal static void LogMessage(object data) => logSource.LogMessage(data);

        internal static void LogWarning(object data) => logSource.LogWarning(data);

        internal static void LogInfo(object data, LogLevel level = LogLevel.Low)
        {
            if (Verbosity is null || VerbosityLevel >= level)
            {
                logSource.LogInfo(data);
            }
        }

        internal static void LogGameObject(GameObject prefab, bool includeChildren = false)
        {
            LogInfo("***** " + prefab.name + " *****");
            foreach (Component compo in prefab.GetComponents<Component>())
            {
                LogComponent(compo);
            }

            if (!includeChildren) { return; }

            LogInfo("***** " + prefab.name + " (children) *****");
            foreach (Transform child in prefab.transform)
            {
                LogInfo($" - {child.gameObject.name}");
                foreach (Component compo in child.gameObject.GetComponents<Component>())
                {
                    LogComponent(compo);
                }
            }
        }

        internal static void LogComponent(Component compo)
        {
            LogInfo($"--- {compo.GetType().Name}: {compo.name} ---");

            PropertyInfo[] properties = compo.GetType().GetProperties(ReflectionUtils.AllBindings);
            foreach (var property in properties)
            {
                try
                {
                    LogInfo($" - {property.Name} = {property.GetValue(compo)}");
                }
                catch
                {
                    LogWarning($"Failed to get value for {property.Name}");
                }
            }

            FieldInfo[] fields = compo.GetType().GetFields(ReflectionUtils.AllBindings);
            foreach (var field in fields)
            {
                try
                {
                    LogInfo($" - {field.Name} = {field.GetValue(compo)}");
                }
                catch
                {
                    LogWarning($"Failed to get value for {field.Name}");
                }
            }
        }
    }
}