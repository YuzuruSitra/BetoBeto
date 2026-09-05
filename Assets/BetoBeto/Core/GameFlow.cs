using UnityEngine;
using UnityEngine.SceneManagement;

namespace BetoBeto.Core
{
    /// <summary>Only a compact result snapshot crosses scene boundaries; no gameplay objects persist.</summary>
    public static class GameFlow
    {
        public sealed class ResultSnapshot
        {
            public bool won;
            public string stageName, dessert;
            public int score, harvested, escaped, bestChain, recipeCount, recipeTotal;
            public float elapsed;
        }
        public static int SelectedStage { get; private set; }
        public static ResultSnapshot LastResult { get; private set; }
        public static bool IsLoading { get; private set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void Reset() { SelectedStage = 0; LastResult = null; IsLoading = false; }
        public static void SceneReady() => IsLoading = false;
        public static void Title() => Load("Title");
        public static void StageSelect() => Load("StageSelect");
        public static void PlayStage(int index)
        {
            var catalog = StageCatalog.Load();
            if (catalog == null || catalog.stages == null || index < 0 || index >= catalog.stages.Length) { Debug.LogError("StageCatalogにステージがありません。"); return; }
            SelectedStage = index; LastResult = null;
            Load(catalog.stages[index].sceneName);
        }
        public static void Retry() => PlayStage(SelectedStage);
        public static void Complete(GameSession session, Stage.StageData stage)
        {
            LastResult = new ResultSnapshot
            {
                won = session.State == GameState.Won, stageName = stage.name, dessert = stage.dessert,
                score = session.Score, harvested = session.TotalHarvested, escaped = session.Escaped,
                bestChain = session.BestChain, recipeCount = session.RecipeCount, recipeTotal = session.Recipe.Total,
                elapsed = session.Elapsed
            };
            Load("Result");
        }
        static void Load(string scene)
        {
            if (IsLoading) return;
            IsLoading = true;
            SceneManager.LoadSceneAsync(scene, LoadSceneMode.Single);
        }
    }
}
