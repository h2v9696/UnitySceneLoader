using System.Collections.Generic;
using H2V.ExtensionsCore.Events.ScriptableObjects;
using H2V.SceneLoader.ScriptableObjects;
using H2V.SceneLoader.ScriptableObjects.Events;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.ResourceManagement.ResourceProviders;

namespace H2V.SceneLoader
{
    public class SceneLoader : MonoBehaviour
    {
        [SerializeField] private SceneEventChannelSO _loadSceneEventChannel;
        [SerializeField] private VoidEventChannelSO _sceneLoadedEventChannel;

        private readonly List<SceneSO> _loadedScenes = new();

        private void OnEnable()
        {
            _loadSceneEventChannel.EventRaised += LoadSceneRequested;
        }

        private void OnDisable()
        {
            _loadSceneEventChannel.EventRaised -= LoadSceneRequested;
        }

        private void LoadSceneRequested(SceneSO sceneSO)
        {
            _ = LoadAllScenes(sceneSO);
        }

        private async UniTask LoadAllScenes(SceneSO mainScene)
        {
            var scene = await LoadScene(mainScene);
            OnSceneLoaded(mainScene, scene);
        }

        private async UniTask<Scene> LoadScene(SceneSO sceneSO)
        {
            foreach (var dependentSceneSO in sceneSO.DependentScenes)
            {
                await LoadScene(dependentSceneSO);
            }

            if (sceneSO.SceneReference.OperationHandle.IsValid())
                return ((SceneInstance) sceneSO.SceneReference.OperationHandle.Result).Scene;
            return await sceneSO.SceneReference.TryLoadScene(LoadSceneMode.Additive);
        }

        private void OnSceneLoaded(SceneSO sceneSO, Scene scene)
        {
            _loadedScenes.Add(sceneSO);
            SceneManager.SetActiveScene(scene);
            _sceneLoadedEventChannel.RaiseEvent();
        }
    }
}