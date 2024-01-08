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
        [SerializeField] private SceneManagerBusSO _sceneManagerBus;
        [SerializeField] private SceneEventChannelSO _additiveLoadSceneEvent;
        [SerializeField] private SceneEventChannelSO _linearLoadSceneEvent;
        [SerializeField] private VoidEventChannelSO _sceneLoadedEventChannel;

        private List<SceneSO> _loadedScenes = new();
        
        private void OnEnable()
        {
            _linearLoadSceneEvent.EventRaised += LinearLoadSceneRequested;
            _additiveLoadSceneEvent.EventRaised += AdditiveLoadSceneRequested;
        }

        private void OnDisable()
        {
            _linearLoadSceneEvent.EventRaised -= LinearLoadSceneRequested;
            _additiveLoadSceneEvent.EventRaised -= AdditiveLoadSceneRequested;
        }

        private void LinearLoadSceneRequested(SceneSO sceneSO)
        {
            _ = UnloadThenLoadAllScenes(sceneSO);
        }

        private void AdditiveLoadSceneRequested(SceneSO sceneSO)
        {
            _ = LoadAllScenes(sceneSO);
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

        private async UniTask UnloadPreviousScenes(SceneSO nextScene)
        {
            foreach (var scene in _loadedScenes)
            {
                foreach (var dependentScene in scene.DependentScenes)
                {
                    if (nextScene.DependentScenes.Contains(dependentScene)) continue;
                    await UnloadScene(dependentScene);
                }

                await UnloadScene(scene);
            }

            await Resources.UnloadUnusedAssets();
        }

        private async UniTask UnloadScene(SceneSO sceneSO)
        {
            if (!sceneSO.SceneReference.OperationHandle.IsValid()) return;
            var handler = Addressables.UnloadSceneAsync(sceneSO.SceneReference.OperationHandle);
            await UniTask.WaitUntil(() => handler.IsDone);
        }
    }
}