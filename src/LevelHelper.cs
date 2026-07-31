using System.Collections.Generic;

namespace Scopophobia
{
    internal static class LevelHelper
    {
        public static string CurrentPlanetName
        {
            get
            {
                if (StartOfRound.Instance == null)
                {
                    ScopophobiaPlugin.logger.LogError("Failed to get current planet name. StartOfRound Instance is null.");
                    return string.Empty;
                }

                return StartOfRound.Instance.currentLevel.PlanetName;
            }
        }

        public static SelectableLevel? GetLevelByName(string planetName)
        {
            if (StartOfRound.Instance == null) return null;

            foreach (var level in StartOfRound.Instance.levels)
            {
                if (level.PlanetName == planetName)
                {
                    return level;
                }
            }

            return null;
        }

        public static bool IsCurrentLevel(string planetName)
        {
            if (StartOfRound.Instance == null)
            {
                ScopophobiaPlugin.logger.LogError($"Failed to check is current level. StartOfRound Instance is null. (PlanetName: {planetName})");
                return false;
            }

            return StartOfRound.Instance.currentLevel.PlanetName == planetName;
        }

        public static List<SpawnableEnemyWithRarity> GetEnemyList(SelectableLevel level, EnemyListType enemyListType)
        {
            if (level == null)
            {
                ScopophobiaPlugin.logger.LogError($"Failed to get enemy list. SelectableLevel is null. (EnemyListType: {Utils.GetEnumName(enemyListType)})");
                return [];
            }

            return enemyListType switch
            {
                EnemyListType.Inside => level.Enemies,
                EnemyListType.Outside => level.OutsideEnemies,
                EnemyListType.Daytime => level.DaytimeEnemies,
                _ => [],
            };
        }

        public static bool LevelHasEnemy(string planetName, string enemyName, EnemyListType enemyListType)
        {
            return LevelHasEnemy(planetName, enemyName, enemyListType, out _);
        }

        public static bool LevelHasEnemy(string planetName, string enemyName, EnemyListType enemyListType, out int spawnWeight)
        {
            spawnWeight = 0;

            SelectableLevel? level = GetLevelByName(planetName);

            if (level == null)
            {
                ScopophobiaPlugin.logger.LogError($"Failed to check if level has enemy. SelectableLevel is null. (PlanetName: {planetName}, EnemyName: {enemyName}, EnemyListType: {Utils.GetEnumName(enemyListType)})");
                return false;
            }

            EnemyType enemyType = EnemyHelper.GetEnemyType(enemyName);

            if (enemyType == null)
            {
                ScopophobiaPlugin.logger.LogError($"Failed to check if level has enemy. EnemyType is null. (PlanetName: {planetName}, EnemyName: {enemyName}, EnemyListType: {Utils.GetEnumName(enemyListType)})");
                return false;
            }

            List<SpawnableEnemyWithRarity> EnemyList = GetEnemyList(level, enemyListType);

            foreach (var spawnableEnemyWithRarity in EnemyList)
            {
                if (spawnableEnemyWithRarity.enemyType == enemyType)
                {
                    spawnWeight = spawnableEnemyWithRarity.rarity;
                    return true;
                }
            }

            return false;
        }

        public static void AddEnemyToLevel(string planetName, string enemyName, int spawnWeight, EnemyListType enemyListType)
        {
            if (LevelHasEnemy(planetName, enemyName, enemyListType))
            {
                ScopophobiaPlugin.Instance.LogWarningExtended($"Failed to add enemy to level. SelectableLevel already contains enemy. (PlanetName: {planetName}, EnemyName: {enemyName}, SpawnWeight: {spawnWeight}, EnemyListType: {Utils.GetEnumName(enemyListType)})");
                return;
            }

            SelectableLevel? level = GetLevelByName(planetName);

            if (level == null)
            {
                ScopophobiaPlugin.logger.LogError($"Failed to add enemy to level. SelectableLevel is null. (PlanetName: {planetName}, EnemyName: {enemyName}, SpawnWeight: {spawnWeight}, EnemyListType: {Utils.GetEnumName(enemyListType)})");
                return;
            }

            EnemyType enemyType = EnemyHelper.GetEnemyType(enemyName);

            if (enemyType == null)
            {
                ScopophobiaPlugin.logger.LogError($"Failed to add enemy to level. EnemyType is null. (PlanetName: {planetName}, EnemyName: {enemyName}, SpawnWeight: {spawnWeight}, EnemyListType: {Utils.GetEnumName(enemyListType)})");
                return;
            }

            List<SpawnableEnemyWithRarity> EnemyList = GetEnemyList(level, enemyListType);

            SpawnableEnemyWithRarity spawnableEnemyWithRarity = new SpawnableEnemyWithRarity(enemyType, spawnWeight);

            EnemyList.Add(spawnableEnemyWithRarity);

            ScopophobiaPlugin.Instance.LogInfoExtended($"Added enemy to level. (PlanetName: {planetName}, EnemyName: {enemyName}, SpawnWeight: {spawnWeight}, EnemyListType: {Utils.GetEnumName(enemyListType)})");
        }

        public static void RemoveEnemyFromLevel(string planetName, string enemyName, EnemyListType enemyListType)
        {
            if (!LevelHasEnemy(planetName, enemyName, enemyListType))
            {
                ScopophobiaPlugin.Instance.LogWarningExtended($"Failed to remove enemy from level. SelectableLevel does not contain enemy. (PlanetName: {planetName}, EnemyName: {enemyName}, EnemyListType: {Utils.GetEnumName(enemyListType)})");
                return;
            }

            SelectableLevel? level = GetLevelByName(planetName);

            if (level == null)
            {
                ScopophobiaPlugin.logger.LogError($"Failed to remove enemy from level. SelectableLevel is null. (PlanetName: {planetName}, EnemyName: {enemyName}, EnemyListType: {Utils.GetEnumName(enemyListType)})");
                return;
            }

            EnemyType enemyType = EnemyHelper.GetEnemyType(enemyName);

            if (enemyType == null)
            {
                ScopophobiaPlugin.logger.LogError($"Failed to remove enemy from level. EnemyType is null. (PlanetName: {planetName}, EnemyName: {enemyName}, EnemyListType: {Utils.GetEnumName(enemyListType)})");
                return;
            }

            List<SpawnableEnemyWithRarity> EnemyList = GetEnemyList(level, enemyListType);

            int index = -1;

            for (int i = 0; i < EnemyList.Count; i++)
            {
                if (EnemyList[i].enemyType == enemyType)
                {
                    index = i;
                    break;
                }
            }

            if (index <= -1)
            {
                ScopophobiaPlugin.Instance.LogWarningExtended($"Failed to remove enemy from level. Could not find EnemyType in SelectableLevel. (PlanetName: {planetName}, EnemyName: {enemyName}, EnemyListType: {Utils.GetEnumName(enemyListType)})");
                return;
            }

            EnemyList.RemoveAt(index);

            ScopophobiaPlugin.Instance.LogInfoExtended($"Removed enemy from level. (PlanetName: {planetName}, EnemyName: {enemyName}, EnemyListType: {Utils.GetEnumName(enemyListType)})");
        }
    }
}
