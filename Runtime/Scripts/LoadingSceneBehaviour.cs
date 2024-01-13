using H2V.SceneLoader.ScriptableObjects;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;
using H2V.ExtensionsCore.AssetReferences;
using H2V.SceneLoader.ScriptableObjects.Events;
using H2V.ExtensionsCore.Events.ScriptableObjects;


#if UNITY_EDITOR
using UnityEditor;
    using H2V.ExtensionsCore.Editor.Helpers;
#endif

namespace H2V.SceneLoader
{
    /// <summary>
    /// Add this to your loading scene so it will unload it self after loading is done.
    /// You can override this for your own loading scene behaviour if you dont want it 
    /// to unload right after scene loaded but after all asset loaded
    /// </summary>
    public class LoadingSceneBehaviour : MonoBehaviour
    {
        [SerializeField] private SceneSO _thisSceneSO;

        [Header("Listen to")]
        [SerializeField] private VoidEventChannelSO _sceneLoadedEvent;

        [Header("Raise")]
        [SerializeField] private SceneEventChannelSO _unloadSceneEvent;

        protected virtual void OnEnable()
        {
            _sceneLoadedEvent.EventRaised += UnloadThisScene;
        }

        protected virtual void OnDisable()
        {
            _sceneLoadedEvent.EventRaised -= UnloadThisScene;
        }

        protected virtual void UnloadThisScene()
        {
            _unloadSceneEvent.RaiseEvent(_thisSceneSO);
        }
    }
}