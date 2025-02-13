using System.Collections.Generic;
using H2V.ExtensionsCore.Events.ScriptableObjects;
using H2V.SceneLoader.ScriptableObjects;
using H2V.SceneLoader.ScriptableObjects.Events;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.ResourceManagement.ResourceProviders;
using UnityEngine.AddressableAssets;
using System.Linq;

namespace H2V.SceneLoader
{
    public class SceneLoader : MonoBehaviour
    {
        [SerializeField] private SceneUnloader _sceneUnloader;

        [Header("Listen to")]
        [SerializeField] private SceneEventChannelSO _additiveLoadSceneEvent;
        [SerializeField] private SceneEventChannelSO _linearLoadSceneEvent;
        [SerializeField] private SceneEventChannelSO _loadAllScenesEvent;
        [SerializeField] private SceneEventChannelSO _unloadPreviousScenesEvent;

        [Header("Raise")]
        [SerializeField] private VoidEventChannelSO _sceneLoadedEventChannel;

        private List<SceneSO> _loadedScenes = new();
        
        private void OnEnable()
        {
            _linearLoadSceneEvent.EventRaised += LinearLoadSceneRequested;
            _loadAllScenesEvent.EventRaised += LoadAllScenesRequested;
            _additiveLoadSceneEvent.EventRaised += AdditiveLoadSceneRequested;
            _unloadPreviousScenesEvent.EventRaised += UnloadPreviousScenesRequested;
        }

        private void OnDisable()
        {
            _linearLoadSceneEvent.EventRaised -= LinearLoadSceneRequested;
            _loadAllScenesEvent.EventRaised -= LoadAllScenesRequested;
            _additiveLoadSceneEvent.EventRaised -= AdditiveLoadSceneRequested;
            _unloadPreviousScenesEvent.EventRaised -= UnloadPreviousScenesRequested;
        }

        private void LoadAllScenesRequested(SceneSO sceneSO)
        {
            LoadScene(sceneSO).ContinueWith(scene =>
            {
                OnSceneLoaded(sceneSO, scene);
            }).Forget();
        }

        private void UnloadPreviousScenesRequested(SceneSO sceneSO)
        {
            UnloadPreviousScenes(sceneSO).Forget();
        }

        private void LinearLoadSceneRequested(SceneSO sceneSO)
        {
            UnloadThenLoadAllScenes(sceneSO).Forget();
        }

        private void AdditiveLoadSceneRequested(SceneSO sceneSO)
        {
            LoadAllScenes(sceneSO).Forget();
        }

        private async UniTask UnloadThenLoadAllScenes(SceneSO mainScene)
        {
            await UnloadPreviousScenes(mainScene);
            await LoadAllScenes(mainScene);
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

            return await sceneSO.SceneReference.TryLoadScene(LoadSceneMode.Additive);
        }

        private void OnSceneLoaded(SceneSO sceneSO, Scene scene)
        {
            _loadedScenes.Add(sceneSO);
            SceneManager.SetActiveScene(scene);
            _sceneLoadedEventChannel.RaiseEvent();
        }

        private async UniTask UnloadPreviousScenes(SceneSO nextScene)
        {
            var markRemoveScenes = new List<SceneSO>();
            foreach (var scene in _loadedScenes)
            {
                if (scene == nextScene) continue;
                foreach (var dependentScene in scene.DependentScenes)
                {
                    if (nextScene.DependentScenes.Contains(dependentScene)) continue;
                    await _sceneUnloader.UnloadScene(dependentScene, false);
                    markRemoveScenes.Add(dependentScene);
                }

                await _sceneUnloader.UnloadScene(scene, false);
                markRemoveScenes.Add(scene);
            }

            _loadedScenes.RemoveAll(scene => markRemoveScenes.Contains(scene));
            markRemoveScenes.Clear();
        }
    }
}