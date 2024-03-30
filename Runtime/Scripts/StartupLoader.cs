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
            _ = LoadSceneAsync();
        }

        private async UniTask LoadSceneAsync()
        {
            await _managerSceneSO.SceneReference.TryLoadScene(LoadSceneMode.Additive);
            var loadSceneEvent = await _linearLoadSceneEventChannelSO.TryLoadAsset();
            loadSceneEvent.RaiseEvent(_firstSceneSO);
            await SceneManager.UnloadSceneAsync(0);
        }

#if UNITY_EDITOR
        private const string SCENE_SCRIPTABLE_OBJECTS_GROUP = "SceneManager_EventScriptableObjects";
        private const string LOAD_SCENE_EVENT_GUID = "777b5e4edf1e9fc4cb9bfe7f0cbe6153";

        private void Reset()
        {
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(this.gameObject.scene.path,
                true) };
            
            AddressableExtensions.SetObjectToAddressableGroup(LOAD_SCENE_EVENT_GUID,
                SCENE_SCRIPTABLE_OBJECTS_GROUP, true);
            _linearLoadSceneEventChannelSO = new ScriptableObjectAssetReference<SceneEventChannelSO>(
                LOAD_SCENE_EVENT_GUID);
        }
#endif
    }
}