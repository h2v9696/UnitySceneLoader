using System.Collections;
using H2V.ExtensionsCore.AssetReferences;
using H2V.ExtensionsCore.Editor.Helpers;
using H2V.ExtensionsCore.Events.ScriptableObjects;
using H2V.SceneLoader.ScriptableObjects;
using H2V.SceneLoader.ScriptableObjects.Events;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.ResourceProviders;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace H2V.SceneLoader.Tests
{
    [TestFixture, Category("Integration Tests")]
    public class SceneLoaderTests
    {
        private const string ASSET_FOLDER_PATH = "Packages/h2v.scene-loader";
        private const string LINEAR_SCENE_LOADER_PREFAB = "LinearSceneLoader";
        private const string ADDITIVE_SCENE_LOADER_PREFAB = "AdditiveSceneLoader";
        private const string SCENE_LOADED_EVENT = "SceneLoadedEvent";
        private const string LINEAR_LOAD_SCENE_EVENT_NAME = "LinearLoadSceneEvent";
        private const string ADDITIVE_LOAD_SCENE_EVENT_NAME = "AdditiveLoadSceneEvent";
        private const string FIRST_SCENE_GUID = "a88f1ce33dcfd3c4d8f131e7c5370cf1";
        private const string LINEAR_SCENE_GUID = "d78fc1c1dd9254f4bacff6f658c6b859";
        private const string ADDITIVE_SCENE_GUID = "50bc3e1bcd96c124ea487fe724528979";
        private const string FIRST_DEPENDENT_SCENE_GUID = "362f6da6d2b0e9d46909bbe51fe8bb18";
        private const string SECOND_DEPENDENT_SCENE_GUID = "386cab2bf697cfc459ef0a06a0f03ed8";
        private const string THIRD_DEPENDENT_SCENE_GUID = "b6dbcf532a193754d810caa369505933";

        private const string TEST_GROUP = "TestGroup"; 

        private SceneEventChannelSO _linearLoadSceneEvent;
        private SceneEventChannelSO _additiveLoadSceneEvent;
        private VoidEventChannelSO _sceneLoadedEvent;

        private SceneSO _firstSceneSO;
        private SceneSO _linearSceneSO;
        private SceneSO _additiveSceneSO;
        private SceneSO _firstDependentSceneSO;
        private SceneSO _secondDependentSceneSO;
        private SceneSO _thirdDependentSceneSO;

        private bool _isSceneLoaded;

        [OneTimeSetUp]
        public void OneTimeSetup()
        {
            _linearLoadSceneEvent = AssetFinder.FindAssetWithNameInPath<SceneEventChannelSO>(
                LINEAR_LOAD_SCENE_EVENT_NAME, ASSET_FOLDER_PATH);
            _additiveLoadSceneEvent = AssetFinder.FindAssetWithNameInPath<SceneEventChannelSO>(
                ADDITIVE_LOAD_SCENE_EVENT_NAME, ASSET_FOLDER_PATH);
            _sceneLoadedEvent = AssetFinder.FindAssetWithNameInPath<VoidEventChannelSO>(
                SCENE_LOADED_EVENT, ASSET_FOLDER_PATH);

            _firstDependentSceneSO = InitSceneSO(FIRST_DEPENDENT_SCENE_GUID);
            _secondDependentSceneSO = InitSceneSO(SECOND_DEPENDENT_SCENE_GUID);
            _thirdDependentSceneSO = InitSceneSO(THIRD_DEPENDENT_SCENE_GUID);

            _firstSceneSO = InitSceneSO(FIRST_SCENE_GUID, new[] {_firstDependentSceneSO});
            _linearSceneSO = InitSceneSO(LINEAR_SCENE_GUID, new[] {_firstDependentSceneSO, _secondDependentSceneSO});
            _additiveSceneSO = InitSceneSO(ADDITIVE_SCENE_GUID, new[] {_firstDependentSceneSO, _thirdDependentSceneSO});

            var linearSceneLoader = AssetFinder.FindAssetWithNameInPath<GameObject>(
                LINEAR_SCENE_LOADER_PREFAB, ASSET_FOLDER_PATH);
            GameObject.Instantiate(linearSceneLoader);
            var additiveSceneLoader = AssetFinder.FindAssetWithNameInPath<GameObject>(
                ADDITIVE_SCENE_LOADER_PREFAB, ASSET_FOLDER_PATH);
            GameObject.Instantiate(additiveSceneLoader);
        }

        private SceneSO InitSceneSO(string sceneGuid, params SceneSO[] dependentScenes)
        {
            AddAssetToTempGroup(sceneGuid);
            return CreateSceneSO(sceneGuid, dependentScenes);
        }

        private SceneSO CreateSceneSO(string sceneGuid, params SceneSO[] dependentScenes)
        {
            var sceneSO = ScriptableObject.CreateInstance<SceneSO>();
            sceneSO.SetPrivateProperty("SceneReference",
                new SceneAssetReference(sceneGuid), true);
            sceneSO.SetPrivateArrayProperty("DependentScenes", dependentScenes, true);
            return sceneSO;
        }

        private void AddAssetToTempGroup(string assetGuid)
        {
            AddressableExtensions.SetObjectToAddressableGroup(assetGuid, TEST_GROUP);
        }

        private void OnSceneLoaded()
        {
            _isSceneLoaded = true;
        }

        [Test]
        public void LoadSceneEventChannel_Exists()
        {
            Assert.IsNotNull(_linearLoadSceneEvent);
        }

        [Test]
        public void SceneSOs_CreatedCorrectly()
        {
            AssertSceneSO(_firstSceneSO);
            AssertSceneSO(_linearSceneSO);
            AssertSceneSO(_additiveSceneSO);
        }

        private void AssertSceneSO(SceneSO sceneSO)
        {
            Assert.IsNotEmpty(sceneSO.SceneReference.AssetGUID);
        }

        [UnityTest]
        public IEnumerator LinearLoadScene_SceneLoaded_AllDependentScenesLoaded()
        {
            _sceneLoadedEvent.EventRaised += OnSceneLoaded;
            _linearLoadSceneEvent.RaiseEvent(_firstSceneSO);

            yield return new WaitUntil(() => _isSceneLoaded);

            AssertSceneLoadedProperly(_firstSceneSO, FIRST_SCENE_GUID);
        }

        [UnityTest]
        public IEnumerator AdditiveLoadScene_SceneLoaded_AllDependentScenesLoaded()
        {
            _sceneLoadedEvent.EventRaised += OnSceneLoaded;
            _additiveLoadSceneEvent.RaiseEvent(_firstSceneSO);

            yield return new WaitUntil(() => _isSceneLoaded);

            AssertSceneLoadedProperly(_firstSceneSO, FIRST_SCENE_GUID);
        }

        private void AssertSceneLoadedProperly(SceneSO sceneSO, string guid)
        {
            var activeScene = SceneManager.GetActiveScene();
            var scenePath = AssetDatabase.GUIDToAssetPath(guid);
            Assert.AreEqual(activeScene.path, scenePath);

            foreach (var scene in sceneSO.DependentScenes)
            {
                Assert.IsNotNull(scene.SceneReference.OperationHandle.Result);
                var dependentScene = (SceneInstance) scene.SceneReference.OperationHandle.Result;
                Assert.IsTrue(dependentScene.Scene.isLoaded);
            }
        }

        [UnityTest]
        public IEnumerator ColdBoot_AllDependentScenesLoaded()
        {
            yield return null;
            Assert.False(true);
        }

        [UnityTest]
        public IEnumerator LoadScene_LinearLoadOtherScene_FirstSceneUnloadCorrectly()
        {
            yield return null;
            Assert.False(true);
        }

        [UnityTest]
        public IEnumerator LoadScene_AdditiveLoadOtherScene_FirstSceneStillLoaded()
        {
            yield return null;
            Assert.False(true);
        }

        [UnityTest]
        public IEnumerator LoadScene_LinearLoadOtherScene_DependentScenesLoadedCorrectly()
        {
            yield return null;
            Assert.False(true);
        }

        [UnityTest]
        public IEnumerator LoadScene_AdditiveLoadOtherScene_DependentScenesLoadedCorrectly()
        {
            yield return null;
            Assert.False(true);
        }

        [UnityTest]
        public IEnumerator LoadScene_AdditiveLoadOtherScene_LinearLoadOtherScene_ScenesUnloadCorrectly()
        {
            yield return null;
            Assert.False(true);
        }

        [TearDown]
        public void TearDown()
        {
            _sceneLoadedEvent.EventRaised -= OnSceneLoaded;
            _isSceneLoaded = false;
        }

        [OneTimeTearDown]
        public void OneTimeTearDown()
        {
            var settings = AddressableAssetSettingsDefaultObject.Settings;
            settings.RemoveGroup(settings.FindGroup(TEST_GROUP));
        }
    }
}