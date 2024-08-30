using HarmonyLib;

namespace Scopophobia.Patches
{
    [HarmonyPatch]
    internal class GetShyGuyPrefabForLaterUse
    {
        [HarmonyPatch(typeof(Terminal), "Start")]
        [HarmonyPostfix]
        private static void SavesPrefabForLaterUse(ref SelectableLevel[] ___moonsCatalogueList)
        {
            SelectableLevel[] array = ___moonsCatalogueList;
            SelectableLevel[] array2 = array;
            foreach (SelectableLevel val2 in array2)
            {
                foreach (SpawnableEnemyWithRarity enemy in val2.Enemies)
                {
                    if (enemy.enemyType.enemyName.ToLower() == "shy guy")
                    {
                        ScopophobiaPlugin.shyPrefab = enemy;
                    }
                }
            }
        }
    }
}
