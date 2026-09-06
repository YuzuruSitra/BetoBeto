using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace BetoBeto.Presentation
{
    /// <summary>A stage owns the request; Single transitions also unload the additive environment.</summary>
    [DisallowMultipleComponent]
    public sealed class KitchenEnvironmentLoader : MonoBehaviour
    {
        public const string SceneName = "KitchenEnvironment";
        public const string ScenePath = "Assets/BetoBeto/Scenes/KitchenEnvironment.unity";
        static AsyncOperation pending;
        public bool Ready { get; private set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetLoadingState() { pending = null; }

        void Start() => Request();

        public static void Request()
        {
            var scene = SceneManager.GetSceneByName(SceneName);
            if (scene.isLoaded) { Connect(); return; }
            if (pending != null) return;
            pending = SceneManager.LoadSceneAsync(SceneName, LoadSceneMode.Additive);
            if (pending != null) pending.completed += _ => { pending = null; Connect(); };
        }

        static void Connect()
        {
            var environment = SceneManager.GetSceneByName(SceneName);
            if (!environment.isLoaded) return;
            // A menu transition may finish while the additive operation is queued.
            var stage = SceneManager.GetActiveScene();
            var game = stage.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<Core.GameController>()).FirstOrDefault();
            if (game == null || game.GetComponent<KitchenEnvironmentLoader>() == null)
            {
                SceneManager.UnloadSceneAsync(environment);
                return;
            }
            var root = environment.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<KitchenEnvironment>()).FirstOrDefault();
            if (root != null)
            {
                root.Connect(game);
                game.GetComponent<KitchenEnvironmentLoader>().Ready = true;
                foreach (var preview in stage.GetRootGameObjects().Where(r => r.name == "Recipe tart preview"))
                    SceneManager.MoveGameObjectToScene(preview, environment);
            }
        }
    }
}
