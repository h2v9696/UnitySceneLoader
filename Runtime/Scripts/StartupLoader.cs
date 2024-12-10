using H2V.SceneLoader.ScriptableObjects;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;
using H2V.ExtensionsCore.AssetReferences;
using H2V.SceneLoader.ScriptableObjects.Events;

#if UNITY_EDITOR
    using UnityEditor;
    using H2V.ExtensionsCore.Editor.Helpers;
#endif

namespace H2V.SceneLoader
{
    /// <summary>
    /// Add this to one scene that is startup scene for your game
    /// You should use StartupScene in this package
    /// The build scenes will only contain this scene
    /// </summary>
    public class StartupLoader : MonoBehaviour
    {
        [SerializeField] private SceneSO _managerSceneSO;
        [SerializeField] private SceneSO _firstSceneSO;

        [Header("Raise")]
        [SerializeField] private ScriptableObjectAssetReference<SceneEventChannelSO> _linearLoadSceneEventChannelSO;

        private void Start()
        {
            LoadSceneAsync().Forget();
        }

        private async UniTask LoadSceneAsync()
        {
            await _managerSceneSO.SceneReference.TryLoadScene(LoadSceneMode.Additive);
            var loadSceneEvent = await _linearLoadSceneEventChannelSO.TryLoadAsset();
            loadSceneEvent.RaiseEvent(_firstSceneSO);
            await SceneManager.UnloadSceneAsync(0);
        }
    }
}