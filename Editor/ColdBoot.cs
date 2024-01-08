using H2V.SceneLoader.ScriptableObjects.Events;
using H2V.SceneLoader.ScriptableObjects;
using UnityEngine;
using UnityEngine.SceneManagement;
using H2V.ExtensionsCore.Editor.Helpers;
using System.Linq;
using Cysharp.Threading.Tasks;

namespace H2V.SceneLoader.Editor
{
    public class ColdBoot : MonoBehaviour
    {
        [SerializeField] private SceneSO _thisSceneSO;
        [SerializeField] private SceneSO _sceneManagerSO;
        [SerializeField] private SceneEventChannelSO _loadSceneEvent;

        private bool _isColdBoot;

        private void OnValidate()
        {
            if (!gameObject.activeInHierarchy) return;
            if (_thisSceneSO != null && 
                _thisSceneSO.SceneReference.editorAsset.name == gameObject.scene.name) return;

            var allSceneSOs = AssetFinder.FindAssetsWithType<SceneSO>();
            _thisSceneSO = allSceneSOs
                .Where(x => x.SceneReference.editorAsset.name == gameObject.scene.name)
                .FirstOrDefault();
        }

        private async UniTask Awake()
        {
            _isColdBoot = 
                !SceneManager.GetSceneByName(_sceneManagerSO.SceneReference.editorAsset.name).isLoaded
                && !_sceneManagerSO.SceneReference.OperationHandle.IsValid();
            if (!_isColdBoot) return;

            await _sceneManagerSO.SceneReference.TryLoadScene(LoadSceneMode.Single);
            _loadSceneEvent.RaiseEvent(_thisSceneSO);
        }
    }
}