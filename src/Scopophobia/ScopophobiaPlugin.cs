using System;
using System.Reflection;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using LethalLib;
using LethalLib.Modules;
using Scopophobia.Patches;
using UnityEngine;

namespace Scopophobia
{
	[BepInPlugin("Scopophobia", "Scopophobia", "1.1.2")]
	public class ScopophobiaPlugin : BaseUnityPlugin
	{
		private readonly Harmony harmony = new Harmony("Scopophobia");

		public static EnemyType shyGuy;

		public static AssetBundle Assets;

		public static bool spawnsOutside;

		public static SpawnableEnemyWithRarity maskedPrefab;

		public static ManualLogSource logger;

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
			harmony.PatchAll(typeof(Config));
			LoadAssets();
			logger = base.Logger;
			MyConfig = new Config(base.Config);
			base.Config.TryGetEntry("General", "Enable the Shy Guy", out ConfigEntry<bool> shyGuyEnabled);
			if (!shyGuyEnabled.Value)
			{
				return;
			}
			base.Config.TryGetEntry("Values", "Spawn Rarity", out ConfigEntry<int> spawnWeight);
			int useWeight = spawnWeight?.Value ?? 15;
			shyGuy = Assets.LoadAsset<EnemyType>("ShyGuyDef.asset");
			TerminalNode val = Assets.LoadAsset<TerminalNode>("ShyGuyTerminal.asset");
			TerminalKeyword val2 = Assets.LoadAsset<TerminalKeyword>("ShyGuyKeyword.asset");
			NetworkPrefabs.RegisterNetworkPrefab(shyGuy.enemyPrefab);
			Enemies.RegisterEnemy(shyGuy, useWeight, Levels.LevelTypes.All, Enemies.SpawnType.Default, val, val2);
			Type[] types = Assembly.GetExecutingAssembly().GetTypes();
			Type[] array = types;
			foreach (Type type in array)
			{
				MethodInfo[] methods = type.GetMethods(BindingFlags.Instance | BindingFlags.Static | BindingFlags.NonPublic);
				MethodInfo[] array2 = methods;
				foreach (MethodInfo method in array2)
				{
					object[] attributes = method.GetCustomAttributes(typeof(RuntimeInitializeOnLoadMethodAttribute), inherit: false);
					if (attributes.Length != 0)
					{
						method.Invoke(null, null);
					}
				}
			}
			logger.LogInfo("Scopophobia | SCP-096 has entered the facility. All remaining personnel proceed with caution.");
			logger.LogInfo("Patched for v50 by TheUnknownCod3r, originally by JasperCreations");
			harmony.PatchAll(typeof(Plugin));
			harmony.PatchAll(typeof(GetShyGuyPrefabForLaterUse));
			harmony.PatchAll(typeof(ShyGuySpawnSettings));
		}
	}
}