using System;
using System.Reflection;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using Scopophobia.Dependencies;
using Scopophobia.Patches;
using UnityEngine;
using BepInEx.Bootstrap;
using Scopophobia.Data;
using LethalLib.Modules;
using Dawn;
using Dusk;

namespace Scopophobia
{

    [BepInPlugin("Scopophobia", "Scopophobia", "1.3.6")]
    [BepInDependency(LethalConfigProxy.PLUGIN_GUID, BepInDependency.DependencyFlags.SoftDependency)]
    [BepInDependency(LethalLib.MyPluginInfo.PLUGIN_GUID, BepInDependency.DependencyFlags.SoftDependency)]
    public class ScopophobiaPlugin : BaseUnityPlugin
    {
        private readonly Harmony harmony = new Harmony("Scopophobia");

        public static EnemyType shyGuy;
        //public static NamespacedKey<DawnEnemyInfo> shyGuy2 = NamespacedKey<DawnEnemyInfo>.From("Scopophobia", "shyguy");
        //public static NamespacedKey<DawnItemInfo> shyGuy2shyGuyPainting2 = NamespacedKey<DawnItemInfo>.From("Scopophobia", "shyGuyPainting");
        internal static ScopophobiaPlugin Instance;
        public static AssetBundle Assets;
        public static SpawnableEnemyWithRarity maskedPrefab;
        public static SpawnableEnemyWithRarity shyEnemy;
        public static Item ShyGuyPainting1;
        public static SpawnableItemWithRarity shyPainting1Prefab;
        public static ManualLogSource logger;
        public static float ShyGuyVolume;
        //public static DuskMod mod { get; private set; } = null!;
        public static SpawnableEnemyWithRarity shyPrefab;
        public static Config MyConfig { get; internal set; }

        internal Assembly assembly => Assembly.GetExecutingAssembly();

        internal string GetFilePath(string path)
        {
            return assembly.Location.Replace(assembly.GetName().Name + ".dll", path);
        }

        private void LoadAssets()
        {
            try
            {
                Assets = AssetBundle.LoadFromFile(GetFilePath("scp096"));
            }
            catch (Exception arg)
            {
                logger.LogError($"Failed to load asset bundle! {arg}");
            }
        }
        private void Awake()
        {
            if (Instance == null) Instance = this;
            NetcodePatchAwake();
            LoadAssets();
            logger = base.Logger;
            MyConfig = new Config(base.Config);
            base.Config.TryGetEntry("General", "Enable the Shy Guy", out ConfigEntry<bool> shyGuyEnabled);
            base.Config.TryGetEntry("Painting Spawn Settings", "Enable Painting", out ConfigEntry<bool> enablePainting);
            if (!shyGuyEnabled.Value)
            {
                return;
            }
            
            ShyGuyVolume = Scopophobia.Config.VolumeConfig.Value;
            shyGuy = Assets.LoadAsset<EnemyType>("ShyGuyDef.asset");
            TerminalNode val = Assets.LoadAsset<TerminalNode>("ShyGuyTerminal.asset");
            TerminalKeyword val2 = Assets.LoadAsset<TerminalKeyword>("ShyGuyKeyword.asset");
            Item Paint1 = Assets.LoadAsset<Item>("ShyGuyPainting.asset");
            NetworkPrefabs.RegisterNetworkPrefab(shyGuy.enemyPrefab);
            NetworkPrefabs.RegisterNetworkPrefab(Paint1.spawnPrefab);
            if(enablePainting.Value) Items.RegisterScrap(Paint1, Scopophobia.Config.PaintingSpawnRate, Levels.LevelTypes.All);
            Enemies.RegisterEnemy(shyGuy, 15, Levels.LevelTypes.All, Enemies.SpawnType.Default, val, val2);
            //mod = DuskMod.RegisterMod(this, Assets);
            //mod.RegisterContentHandlers();
            logger.LogInfo("Scopophobia | SCP-096 has entered the facility. All remaining personnel proceed with caution.");
            harmony.PatchAll(typeof(GetShyGuyPrefabForLaterUse));
            harmony.PatchAll(typeof(AudioSpatializerDisabler));
            harmony.PatchAll(typeof(RoundManagerPatch));//credit Crit / Zehs
            harmony.PatchAll(typeof(StartOfRoundPatch));//credit Crit / Zehs
            harmony.PatchAll(typeof(BeltBagItemPatch));
            if (CoronerProxy.Enabled)
            {
                CoronerProxy.Initialize();
            }
        }
        private static void NetcodePatchAwake()
        {
            // See https://github.com/EvaisaDev/UnityNetcodePatcher?tab=readme-ov-file#preparing-mods-for-patching
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

        public void LogInfoExtended(object data)
        {
            if (Scopophobia.Config.ExtendedLogging)
            {
                logger.LogInfo(data);
            }
        }
        public void LogErrorExtended(object data)
        {
            if (Scopophobia.Config.ExtendedLogging)
            {
                logger.LogError(data);
            }
        }

        public void LogWarningExtended(object data)
        {
            if (Scopophobia.Config.ExtendedLogging)
            {
                logger.LogWarning(data);
            }
        }
    }
}