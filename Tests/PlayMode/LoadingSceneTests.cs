using System.Collections;
using Cysharp.Threading.Tasks;
using H2V.ExtensionsCore.Editor.Helpers;
using H2V.ExtensionsCore.Events.ScriptableObjects;
using H2V.SceneLoader.Editor;
using H2V.SceneLoader.ScriptableObjects;
using H2V.SceneLoader.ScriptableObjects.Events;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace H2V.SceneLoader.Tests
{
    [TestFixture, Category("Integration Tests")]
    public class LoadingSceneTests
    {
        private const string ASSET_FOLDER_PATH = "Packages/h2v.scene-loader";
        private const string SCENE_MANAGER_SO = "SceneManagerSO";
        private const string COLD_BOOT_PREFAB = "ColdBoot";
        private const string LOADING_SCENE_PREFAB = "LoadingSceneBehaviour";
        
        private const string SCENE_LOADED_EVENT = "SceneLoadedEvent";
        private const string SCENE_UNLOADED_EVENT = "SceneUnloadedEvent";
        private const string LINEAR_LOAD_SCENE_EVENT_NAME = "LinearLoadSceneEvent";
        private const string FIRST_SCENE_GUID = "a88f1ce33dcfd3c4d8f131e7c5370cf1";
        private const string LOADING_SCENE_GUID = "c2481a5b7fa3cc74c883fbb3e1efd2cb";

        private SceneEventChannelSO _linearLoadSceneEvent;
        private VoidEventChannelSO _sceneLoadedEvent;
        private VoidEventChannelSO _sceneUnloadedEvent;

        private SceneSO _firstSceneSO;
        private SceneSO _loadingSceneSO;
        private SceneSO _sceneManagerSO;
        private GameObject _coldBootObject;
        private GameObject _loadingSceneObject;
        private string _firstSceneSOPath = $"{ASSET_FOLDER_PATH}/FirstSceneSO2.asset";
        private string _loadingSceneSOPath = $"{ASSET_FOLDER_PATH}/LoadingSceneSO.asset";

        private bool _isSceneLoaded;
        private bool _isSceneUnloaded;

        [OneTimeSetUp]
        public void OneTimeSetup()
        {
            _linearLoadSceneEvent = AssetFinder.FindAssetWithNameInPath<SceneEventChannelSO>(
                LINEAR_LOAD_SCENE_EVENT_NAME, ASSET_FOLDER_PATH);
            _sceneLoadedEvent = AssetFinder.FindAssetWithNameInPath<VoidEventChannelSO>(
                SCENE_LOADED_EVENT, ASSET_FOLDER_PATH);
            _sceneUnloadedEvent = AssetFinder.FindAssetWithNameInPath<VoidEventChannelSO>(
                SCENE_UNLOADED_EVENT, ASSET_FOLDER_PATH);

            _loadingSceneSO = SceneTestHelper.InitAndCreateSceneSOAsset(LOADING_SCENE_GUID,
                _loadingSceneSOPath);
            _firstSceneSO = SceneTestHelper.InitAndCreateSceneSOAsset(FIRST_SCENE_GUID, _firstSceneSOPath, true,
                new[] {_loadingSceneSO});

            _sceneManagerSO = AssetFinder.FindAssetWithNameInPath<SceneManagerSO>(
                SCENE_MANAGER_SO, ASSET_FOLDER_PATH);
            _coldBootObject = AssetFinder.FindAssetWithNameInPath<GameObject>(
                COLD_BOOT_PREFAB, ASSET_FOLDER_PATH);
            _loadingSceneObject = AssetFinder.FindAssetWithNameInPath<GameObject>(
                LOADING_SCENE_PREFAB, ASSET_FOLDER_PATH);
            var loadingSceneBehaviour = _loadingSceneObject.GetComponent<LoadingSceneBehaviour>();
            loadingSceneBehaviour.SetPrivateProperty("_thisSceneSO", _loadingSceneSO);
        }

        private void OnSceneLoaded()
        {
            _isSceneLoaded = true;
        }

        private void OnSceneUnloaded()
        {
            _isSceneUnloaded = true;
        }

        [UnityTest]
        public IEnumerator LinearLoadScene_SceneLoaded_LoadingSceneUnloaded()
        {
            yield return UniTask.ToCoroutine(
                () => _sceneManagerSO.SceneReference.TryLoadScene(LoadSceneMode.Single)
            );
            _sceneLoadedEvent.EventRaised += OnSceneLoaded;
            _sceneUnloadedEvent.EventRaised += OnSceneUnloaded;
            _linearLoadSceneEvent.RaiseEvent(_firstSceneSO);

            yield return new WaitUntil(() => _isSceneLoaded);

            yield return new WaitUntil(() => _isSceneUnloaded);

            SceneTestHelper.AssertSceneNotLoaded(_loadingSceneSO);
        }

        [UnityTest]
        public IEnumerator ColdBoot_LoadingSceneUnloaded()
        {
            yield return Addressables.LoadSceneAsync(_firstSceneSO.SceneReference, LoadSceneMode.Single);

            var coldBoot = _coldBootObject.GetComponent<ColdBoot>();
            coldBoot.SetPrivateProperty("_thisSceneSO", _firstSceneSO);
            GameObject.Instantiate(_coldBootObject);

            _sceneLoadedEvent.EventRaised += OnSceneLoaded;
            _sceneUnloadedEvent.EventRaised += OnSceneUnloaded;
            yield return new WaitUntil(() => _isSceneLoaded);

            yield return new WaitUntil(() => _isSceneUnloaded);
            SceneTestHelper.AssertSceneNotLoaded(_loadingSceneSO);
        }

        [TearDown]
        public void TearDown()
        {
            _sceneUnloadedEvent.EventRaised -= OnSceneUnloaded;
            _sceneLoadedEvent.EventRaised -= OnSceneLoaded;
            _isSceneLoaded = false;
        }

        [OneTimeTearDown]
        public void OneTimeTearDown()
        {
            var coldBoot = _coldBootObject.GetComponent<ColdBoot>();
            coldBoot.SetPrivateProperty("_thisSceneSO", null);
            var loadingSceneBehaviour = _loadingSceneObject.GetComponent<LoadingSceneBehaviour>();
            loadingSceneBehaviour.SetPrivateProperty("_thisSceneSO", null);

            AssetDatabase.DeleteAsset(_firstSceneSOPath);
            AssetDatabase.DeleteAsset(_loadingSceneSOPath);

            var settings = AddressableAssetSettingsDefaultObject.Settings;
            settings.RemoveGroup(settings.FindGroup(SceneTestHelper.TEST_GROUP));
            _sceneManagerSO.SceneReference.ReleaseHandle();
        }
    }
}