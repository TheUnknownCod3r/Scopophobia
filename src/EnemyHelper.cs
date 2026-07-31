using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace Scopophobia
{
    public enum EnemyListType
    {
        Inside,
        Outside,
        Daytime
    }

    internal static class EnemyHelper
    {
        public static void SetSpawnWeight(string enemyName, int spawnWeight, EnemyListType enemyListType, string planetName)
        {
            if (StartOfRound.Instance == null)
            {
                ScopophobiaPlugin.logger.LogError($"Failed to set enemy spawn weight. StartOfRound Instance is null. (EnemyName: {enemyName}, SpawnWeight: {spawnWeight}, EnemyListType: {Utils.GetEnumName(enemyListType)}, PlanetName: {planetName})");
                return;
            }

            EnemyType? enemyType = GetEnemyType(enemyName);

            if (enemyType == null)
            {
                ScopophobiaPlugin.logger.LogError($"Failed to set enemy spawn weight. EnemyType is null. (EnemyName: {enemyName}, SpawnWeight: {spawnWeight}, EnemyListType: {Utils.GetEnumName(enemyListType)}, PlanetName: {planetName})");
                return;
            }

            SelectableLevel? level = LevelHelper.GetLevelByName(planetName);

            if (level == null)
            {
                ScopophobiaPlugin.logger.LogError($"Failed to set enemy spawn weight. SelectableLevel is null. (EnemyName: {enemyName}, SpawnWeight: {spawnWeight}, EnemyListType: {Utils.GetEnumName(enemyListType)}, PlanetName: {planetName})");
                return;
            }

            if (!LevelHelper.LevelHasEnemy(planetName, enemyName, enemyListType))
            {
                ScopophobiaPlugin.logger.LogError($"Failed to set enemy spawn weight. SelectableLevel does not contain enemy. (EnemyName: {enemyName}, SpawnWeight: {spawnWeight}, EnemyListType: {Utils.GetEnumName(enemyListType)}, PlanetName: {planetName})");
                return;
            }

            List<SpawnableEnemyWithRarity> EnemyList = LevelHelper.GetEnemyList(level, enemyListType);

            foreach (var spawnableEnemyWithRarity in EnemyList)
            {
                if (spawnableEnemyWithRarity.enemyType == enemyType)
                {
                    spawnableEnemyWithRarity.rarity = spawnWeight;
                    break;
                }
            }

            ScopophobiaPlugin.Instance.LogInfoExtended($"Set enemy spawn weight. (EnemyName: {enemyName}, SpawnWeight: {spawnWeight}, EnemyListType: {Utils.GetEnumName(enemyListType)}, PlanetName: {planetName})");
        }

        public static void SetMaxSpawnCount(string enemyName, int maxSpawnCount, string planetName)
        {
            if (StartOfRound.Instance == null)
            {
                ScopophobiaPlugin.logger.LogError($"Failed to set enemy max spawn count. StartOfRound Instance is null. (EnemyName: {enemyName}, MaxSpawnCount: {maxSpawnCount}, PlanetName: {planetName})");
                return;
            }

            if (!LevelHelper.IsCurrentLevel(planetName))
            {
                ScopophobiaPlugin.logger.LogError($"Failed to set enemy max spawn count. Planet name is not the current planet name. (EnemyName: {enemyName}, MaxSpawnCount: {maxSpawnCount}, PlanetName: {planetName})");
                return;
            }

            EnemyType? enemyType = GetEnemyType(enemyName);

            if (enemyType == null)
            {
                ScopophobiaPlugin.logger.LogError($"Failed to set enemy max spawn count. EnemyType is null. (EnemyName: {enemyName}, MaxSpawnCount: {maxSpawnCount}, PlanetName: {planetName})");
                return;
            }

            enemyType.MaxCount = maxSpawnCount;

            ScopophobiaPlugin.Instance.LogInfoExtended($"Set enemy max spawn count. (EnemyName: {enemyName}, MaxSpawnCount: {maxSpawnCount}, PlanetName: {planetName})");
        }

        public static void SetProbabilityCurve(string enemyName, float[] values)
        {
            if (values == null)
            {
                ScopophobiaPlugin.logger.LogError($"Failed to set enemy probability curve. Values are null. (EnemyName: {enemyName})");
                return;
            }

            if (values.Length == 0)
            {
                ScopophobiaPlugin.logger.LogError($"Failed to set enemy probability curve. Values are empty. (EnemyName: {enemyName})");
                return;
            }

            if (StartOfRound.Instance == null)
            {
                ScopophobiaPlugin.logger.LogError($"Failed to set enemy probability curve. StartOfRound Instance is null. (EnemyName: {enemyName}, Values: {string.Join(", ", values)})");
                return;
            }

            EnemyType? enemyType = GetEnemyType(enemyName);

            if (enemyType == null)
            {
                ScopophobiaPlugin.logger.LogError($"Failed to set enemy probability curve. EnemyType is null. (EnemyName: {enemyName}, Values: {string.Join(", ", values)})");
                return;
            }

            AnimationCurve animationCurve = Utils.CreateAnimationCurve(values);

            if (animationCurve == null)
            {
                ScopophobiaPlugin.logger.LogError($"Failed to set enemy probability curve. AnimationCurve is invalid. (EnemyName: {enemyName}, Values: {string.Join(", ", values)})");
                return;
            }

            enemyType.probabilityCurve = animationCurve;

            ScopophobiaPlugin.Instance.LogInfoExtended($"Set enemy probability curve. (EnemyName: {enemyName}, Values: {string.Join(", ", values)})");
        }

        public static EnemyType? GetEnemyType(string enemyName)
        {
            foreach (var enemyType in GetEnemyTypes())
            {
                if (enemyType.enemyName == enemyName)
                {
                    return enemyType;
                }
            }

            try
            {
                EnemyType enemyType = Resources.FindObjectsOfTypeAll<EnemyType>().Single((EnemyType x) => x.enemyName == enemyName);

                if (IsValidEnemyType(enemyType) && NetworkUtils.IsNetworkPrefab(enemyType.enemyPrefab))
                {
                    ScopophobiaPlugin.Instance.LogInfoExtended($"Found EnemyType \"{enemyType.enemyName}\" from Resources.");

                    return enemyType;
                }
            }
            catch { }

            return null;
        }

        public static List<EnemyType> GetEnemyTypes()
        {
            if (StartOfRound.Instance == null) return [];

            var enemyTypes = new HashSet<EnemyType>(new EnemyTypeComparer());

            foreach (var level in StartOfRound.Instance.levels)
            {
                var levelEnemyTypes = level.Enemies
                    .Concat(level.DaytimeEnemies)
                    .Concat(level.OutsideEnemies)
                    .Select(e => e.enemyType)
                    .Where(IsValidEnemyType);

                foreach (var levelEnemyType in levelEnemyTypes)
                {
                    enemyTypes.Add(levelEnemyType);
                }
            }

            return enemyTypes.ToList();
        }

        public static bool IsValidEnemyType(EnemyType enemyType)
        {
            if (enemyType == null) return false;
            if (string.IsNullOrWhiteSpace(enemyType.enemyName)) return false;
            if (enemyType.enemyPrefab == null) return false;

            return true;
        }
    }

    public class EnemyTypeComparer : IEqualityComparer<EnemyType>
    {
        public bool Equals(EnemyType x, EnemyType y)
        {
            if (x == null || y == null) return false;
            return x.enemyName == y.enemyName;
        }

        public int GetHashCode(EnemyType obj)
        {
            return obj.enemyName?.GetHashCode() ?? 0;
        }
    }
}
