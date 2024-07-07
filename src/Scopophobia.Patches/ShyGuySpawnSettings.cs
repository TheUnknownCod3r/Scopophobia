using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using UnityEngine;

namespace Scopophobia.Patches
{
[HarmonyPatch(typeof(RoundManager))]
internal class ShyGuySpawnSettings
{
	public static string[] nonoOutside = new string[5] { "Level1Experimentation", "Level2Assurance", "Level3Vow", "Level4March", "Level7Offense" };

	[HarmonyPatch("BeginEnemySpawning")]
	[HarmonyPrefix]
	public static void UpdateSpawnRates(ref SelectableLevel ___currentLevel) //set as public
	{
		if (!Config.appears || ScopophobiaPlugin.shyPrefab == null)
		{
			return;
		}
		try
		{
			SpawnableEnemyWithRarity shyEnemy = ScopophobiaPlugin.shyPrefab;
			List<SpawnableEnemyWithRarity> enemies = ___currentLevel.Enemies;
			for (int i = 0; i < ___currentLevel.Enemies.Count; i++)
			{
				SpawnableEnemyWithRarity val2 = ___currentLevel.Enemies[i];
				if (val2.enemyType.enemyName == "Shy guy")
				{
					enemies.Remove(val2);
				}
			}
			___currentLevel.Enemies = enemies;
			shyEnemy.enemyType.PowerLevel = Config.ShyGuyPowerLevel; //change from int to float
			shyEnemy.rarity = Config.spawnRarity;
			shyEnemy.enemyType.probabilityCurve = new AnimationCurve(new Keyframe(0f, Config.startEnemySpawnCurve), new Keyframe(0.5f, Config.midEnemySpawnCurve), new Keyframe(1f, Config.endEnemySpawnCurve));
			shyEnemy.enemyType.MaxCount = Config.maxSpawnCount;
			shyEnemy.enemyType.isOutsideEnemy = Config.canSpawnOutside;
			if (Config.canSpawnOutside & (!Config.spawnOutsideHardPlanets || !nonoOutside.Contains(___currentLevel.sceneName)))
			{
				___currentLevel.OutsideEnemies.Add(shyEnemy);
				SelectableLevel obj = ___currentLevel;
				obj.maxOutsideEnemyPowerCount += shyEnemy.enemyType.MaxCount * (int)shyEnemy.enemyType.PowerLevel; //typecast as int to fix PowerLevel, ty MaskedOverhaulFork
					ScopophobiaPlugin.logger.LogInfo("Theres a Shy Guy outside... Proceed with caution"); //Test to see if Shy guy spawns outside, see below
			}
			if (!___currentLevel.OutsideEnemies.Contains(shyEnemy)) { ___currentLevel.Enemies.Add(shyEnemy); ScopophobiaPlugin.logger.LogInfo("Enemy not found Outside, Spawning Inside"); }//Check for outside enemy existing in level
		} catch { }
	}
} }
