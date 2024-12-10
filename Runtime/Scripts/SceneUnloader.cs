using H2V.ExtensionsCore.Events.ScriptableObjects;
using H2V.SceneLoader.ScriptableObjects;
using H2V.SceneLoader.ScriptableObjects.Events;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace H2V.SceneLoader
{
    public class SceneUnloader : MonoBehaviour
    {
        [Header("Listen to")]
        [SerializeField] private SceneEventChannelSO _unloadSceneEvent;
        
        [Header("Raise")]
        [SerializeField] private VoidEventChannelSO _sceneUnloadedEventChannel;

        private void OnEnable()
        {
            _unloadSceneEvent.EventRaised += UnloadSceneRequested;
        }

        private void OnDisable()
        {
            _unloadSceneEvent.EventRaised -= UnloadSceneRequested;
        }

        private void UnloadSceneRequested(SceneSO sceneSO)
        {
            UnloadScene(sceneSO).Forget();
        }

        public async UniTask UnloadScene(SceneSO sceneSO, bool isUnloadUnused = true)
        {
            if (!sceneSO.SceneReference.OperationHandle.IsValid()) return;
            var handler = Addressables.UnloadSceneAsync(sceneSO.SceneReference.OperationHandle);
            await UniTask.WaitUntil(() => handler.IsDone);
            sceneSO.SceneReference.ReleaseAsset();
            if (isUnloadUnused)
                await Resources.UnloadUnusedAssets();
            _sceneUnloadedEventChannel.RaiseEvent();
        }
    }
}