using H2V.SceneLoader.ScriptableObjects;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;
using H2V.ExtensionsCore.AssetReferences;
using H2V.SceneLoader.ScriptableObjects.Events;

namespace H2V.SceneLoader
{
    /// <summary>
    /// Add this to one scene that is startup scene for your game
    /// You should use StartupScene in this package
    /// The build scenes will only contain this scene
    /// </summary>
    public class StartupLoader : MonoBehaviour
    {
        [SerializeField] private ScriptableObjectAssetReference<SceneSO> _managerSceneSOReference;
        [SerializeField] private ScriptableObjectAssetReference<SceneSO> _firstSceneSOReference;

        [Header("Raise")]
        [SerializeField] private ScriptableObjectAssetReference<SceneEventChannelSO> _linearLoadSceneEventChannelSO;

        private void Start()
        {
            LoadSceneAsync().Forget();
        }

        private async UniTask LoadSceneAsync()
        {
            var loadSceneEvent = await _linearLoadSceneEventChannelSO.TryLoadAsset();
            var managerSceneSO = await _managerSceneSOReference.TryLoadAsset();
            var firstScene = await _firstSceneSOReference.TryLoadAsset();
            await managerSceneSO.SceneReference.TryLoadScene(LoadSceneMode.Additive);
            loadSceneEvent.RaiseEvent(firstScene);
            await SceneManager.UnloadSceneAsync(0);
        }
    }
}