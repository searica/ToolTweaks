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
using System.Collections.Generic;
using System.Reflection.Emit;

namespace ToolTweaks
{
    [BepInPlugin(PluginGUID, PluginName, PluginVersion)]
    [BepInDependency(Jotunn.Main.ModGuid, Jotunn.Main.Version)]
    internal class ToolTweaks : BaseUnityPlugin
    {
        internal const string Author = "Searica";
        public const string PluginName = "ToolTweaks";
        public const string PluginGUID = $"{Author}.Valheim.{PluginName}";
        public const string PluginVersion = "0.0.1";

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
            ConfigManager.SaveOnConfigSet(false);

            Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly(), harmonyInstanceId: PluginGUID);
            Game.isModded = true;

            ConfigManager.SetupWatcher();
            //ConfigManager.CheckForConfigManager();

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
                "UsageDelayMultiplier",
                0.25f,
                "Set the time delay between tool uses for both placement and removal. Vanilla default is 0.4s for placement and 0.25s for removal.",
                new AcceptableValueRange<float>(0.05f, 2f)
            );

            staminaMult = ConfigManager.BindConfig(
                MainSection,
                "StaminaCostMultiplier",
                0.5f,
                "Change the stamina cost for using tools.",
                new AcceptableValueRange<float>(0.0f, 2f)
            );

            durabiltyMult = ConfigManager.BindConfig(
                MainSection,
                "DurabilityMultiplier",
                0.5f,
                "Change the amount of durability used up each time a tool is used.",
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
            }
        }
    }

    [HarmonyPatch(typeof(Player))]
    internal class PlayerPatch
    {
        [HarmonyPostfix]
        [HarmonyPatch(nameof(Player.Awake))]
        private static void AwakePostfix(Player __instance)
        {
            if (!__instance) { return; }

            __instance.m_placeDelay = ToolTweaks.UseageDelay;
            __instance.m_removeDelay = ToolTweaks.UseageDelay;
        }

        /// <summary>
        ///     Transpiler to patch in a delegate that modifies tool stamina usage
        /// </summary>
        /// <param name="instructions"></param>
        /// <returns></returns>
        [HarmonyTranspiler]
        [HarmonyPatch(nameof(Player.UpdatePlacement))]
        private static IEnumerable<CodeInstruction> PlacementUseStaminaTranspiler(IEnumerable<CodeInstruction> instructions)
        {
            // Targeting code
            // UseStamina(rightItem.m_shared.m_attack.m_attackStamina, isBaseUsage: true);
            // IL_01e7: ldarg.0
            // IL_01e8: ldloc.0
            // IL_01e9: ldfld class ItemDrop/ItemData/SharedData ItemDrop/ItemData::m_shared
            // IL_01ee: ldfld class Attack ItemDrop/ItemData/SharedData::m_attack
            // IL_01f3: ldfld float32 Attack::m_attackStamina
            // IL_01f8: ldc.i4.1
            // IL_01f9: callvirt instance void Character::UseStamina(float32, bool)

            FieldInfo sharedField = typeof(ItemDrop.ItemData).GetField(nameof(ItemDrop.ItemData.m_shared));
            FieldInfo attackField = typeof(ItemDrop.ItemData.SharedData).GetField(nameof(ItemDrop.ItemData.SharedData.m_attack));
            FieldInfo attackStaminaField = typeof(Attack).GetField(nameof(Attack.m_attackStamina));
            MethodInfo useStaminaMethod = typeof(Character).GetMethod(nameof(Character.UseStamina));

            var codeMatches = new CodeMatch[]
            {
                new CodeMatch(OpCodes.Ldloc_0),
                new CodeMatch(OpCodes.Ldfld, sharedField),
                new CodeMatch(OpCodes.Ldfld, attackField),
                new CodeMatch(OpCodes.Ldfld, attackStaminaField),
                new CodeMatch(OpCodes.Ldc_I4_1),
                new CodeMatch(OpCodes.Callvirt, useStaminaMethod)
            };


            var codeMatcher = new CodeMatcher(instructions);
            codeMatcher.MatchForward(useEnd: false, codeMatches);

            while (codeMatcher.IsValid)
            {
                // Replace first line of matched code with delegate and remove the rest
                codeMatcher.SetInstructionAndAdvance(Transpilers.EmitDelegate(UseStaminaDelegate));
                codeMatcher.RemoveInstructions(codeMatches.Length - 1);
                codeMatcher.MatchForward(useEnd: false, codeMatches);
            }
            return codeMatcher.InstructionEnumeration();
        }


        private static void UseStaminaDelegate(Player player)
        {
            Log.LogInfo("UseStamina delegate");
            var stamCost = player.m_rightItem.m_shared.m_attack.m_attackStamina * ToolTweaks.StaminaMultiplier;
            player.UseStamina(stamCost, isBaseUsage: true);
        }


        /// <summary>
        ///     Transpiler to patch in a delegate that modifies tool durability drain
        /// </summary>
        /// <param name="instructions"></param>
        /// <returns></returns>
        [HarmonyTranspiler]
        [HarmonyPatch(nameof(Player.UpdatePlacement))]
        private static IEnumerable<CodeInstruction> PlacementUseDurabilityTranspiler(IEnumerable<CodeInstruction> instructions)
        {
            // Targeting code
            // if (rightItem.m_shared.m_useDurability)
            // IL_01fe: ldloc.0
            // IL_01ff: ldfld class ItemDrop/ItemData/SharedData ItemDrop/ItemData::m_shared
            // IL_0204: ldfld bool ItemDrop/ItemData/SharedData::m_useDurability
            // IL_0209: brfalse.s IL_022f

            // rightItem.m_durability -= rightItem.m_shared.m_useDurabilityDrain;
            // IL_020b: ldloc.0
            // IL_020c: dup
            // IL_020d: ldfld float32 ItemDrop/ItemData::m_durability
            // IL_0212: ldloc.0
            // IL_0213: ldfld class ItemDrop/ItemData/SharedData ItemDrop/ItemData::m_shared
            // IL_0218: ldfld float32 ItemDrop/ItemData/SharedData::m_useDurabilityDrain
            // IL_021d: sub
            // IL_021e: stfld float32 ItemDrop/ItemData::m_durability

            FieldInfo durabilityField = typeof(ItemDrop.ItemData).GetField(nameof(ItemDrop.ItemData.m_durability));
            FieldInfo sharedField = typeof(ItemDrop.ItemData).GetField(nameof(ItemDrop.ItemData.m_shared));
            FieldInfo durabilityDrainField = typeof(ItemDrop.ItemData.SharedData)
                .GetField(nameof(ItemDrop.ItemData.SharedData.m_useDurabilityDrain));

            var codeMatches = new CodeMatch[]
            {
                new CodeMatch(OpCodes.Dup),
                new CodeMatch(OpCodes.Ldfld, durabilityField),
                new CodeMatch(OpCodes.Ldloc_0),
                new CodeMatch(OpCodes.Ldfld, sharedField),
                new CodeMatch(OpCodes.Ldfld, durabilityDrainField),
                new CodeMatch(OpCodes.Sub),
                new CodeMatch(OpCodes.Stfld, durabilityField)
            };

            var codeMatcher = new CodeMatcher(instructions);
            codeMatcher.MatchForward(useEnd: false, codeMatches);
            while (codeMatcher.IsValid)
            {
                // Replace first line of matched code with delegate and remove the rest
                codeMatcher.SetInstructionAndAdvance(Transpilers.EmitDelegate(UseDurabilityDelegate));
                codeMatcher.RemoveInstructions(codeMatches.Length - 1);
                codeMatcher.MatchForward(useEnd: false, codeMatches);
            }
            return codeMatcher.InstructionEnumeration();
        }


        private static void UseDurabilityDelegate(ItemDrop.ItemData rightItem)
        {
            rightItem.m_durability -= rightItem.m_shared.m_useDurabilityDrain * ToolTweaks.DurabilityMultiplier;
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

        internal static ManualLogSource _logSource;

        internal static void Init(ManualLogSource logSource)
        {
            _logSource = logSource;
        }

        internal static void LogDebug(object data) => _logSource.LogDebug(data);

        internal static void LogError(object data) => _logSource.LogError(data);

        internal static void LogFatal(object data) => _logSource.LogFatal(data);

        internal static void LogMessage(object data) => _logSource.LogMessage(data);

        internal static void LogWarning(object data) => _logSource.LogWarning(data);

        internal static void LogInfo(object data, LogLevel level = LogLevel.Low)
        {
            if (Verbosity is null || VerbosityLevel >= level)
            {
                _logSource.LogInfo(data);
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