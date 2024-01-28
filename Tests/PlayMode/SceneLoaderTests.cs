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
    public class SceneLoaderTests
    {
        private const string ASSET_FOLDER_PATH = "Packages/h2v.scene-loader";
        private const string SCENE_MANAGER_SO = "SceneManagerSO";
        private const string COLD_BOOT_PREFAB = "ColdBoot";
        private const string SCENE_LOADED_EVENT = "SceneLoadedEvent";
        private const string LINEAR_LOAD_SCENE_EVENT_NAME = "LinearLoadSceneEvent";
        private const string ADDITIVE_LOAD_SCENE_EVENT_NAME = "AdditiveLoadSceneEvent";
        private const string FIRST_SCENE_GUID = "a88f1ce33dcfd3c4d8f131e7c5370cf1";
        private const string LINEAR_SCENE_GUID = "d78fc1c1dd9254f4bacff6f658c6b859";
        private const string ADDITIVE_SCENE_GUID = "50bc3e1bcd96c124ea487fe724528979";
        private const string FIRST_DEPENDENT_SCENE_GUID = "362f6da6d2b0e9d46909bbe51fe8bb18";
        private const string SECOND_DEPENDENT_SCENE_GUID = "386cab2bf697cfc459ef0a06a0f03ed8";
        private const string THIRD_DEPENDENT_SCENE_GUID = "b6dbcf532a193754d810caa369505933";

        private SceneEventChannelSO _linearLoadSceneEvent;
        private SceneEventChannelSO _additiveLoadSceneEvent;
        private VoidEventChannelSO _sceneLoadedEvent;

        private SceneSO _firstSceneSO;
        private SceneSO _linearSceneSO;
        private SceneSO _additiveSceneSO;
        private SceneSO _firstDependentSceneSO;
        private SceneSO _secondDependentSceneSO;
        private SceneSO _thirdDependentSceneSO;
        private SceneSO _sceneManagerSO;
        private GameObject _coldBootObject;
        private string _firstSceneSOPath = $"{ASSET_FOLDER_PATH}/FirstSceneSO.asset";

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

            _firstDependentSceneSO = SceneTestHelper.InitSceneSO(FIRST_DEPENDENT_SCENE_GUID);
            _secondDependentSceneSO = SceneTestHelper.InitSceneSO(SECOND_DEPENDENT_SCENE_GUID);
            _thirdDependentSceneSO = SceneTestHelper.InitSceneSO(THIRD_DEPENDENT_SCENE_GUID);

            _firstSceneSO = SceneTestHelper.InitAndCreateSceneSOAsset(FIRST_SCENE_GUID, _firstSceneSOPath, true,
                new[] {_firstDependentSceneSO});
            _linearSceneSO = SceneTestHelper.InitSceneSO(LINEAR_SCENE_GUID, new[] {_secondDependentSceneSO, _thirdDependentSceneSO});
            _additiveSceneSO = SceneTestHelper.InitSceneSO(ADDITIVE_SCENE_GUID, new[] {_firstDependentSceneSO, _thirdDependentSceneSO});

            _sceneManagerSO = AssetFinder.FindAssetWithNameInPath<SceneManagerSO>(
                SCENE_MANAGER_SO, ASSET_FOLDER_PATH);
            _coldBootObject = AssetFinder.FindAssetWithNameInPath<GameObject>(
                COLD_BOOT_PREFAB, ASSET_FOLDER_PATH);
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
            yield return UniTask.ToCoroutine(
                () => _sceneManagerSO.SceneReference.TryLoadScene(LoadSceneMode.Single)
            );
            _sceneLoadedEvent.EventRaised += OnSceneLoaded;
            _linearLoadSceneEvent.RaiseEvent(_firstSceneSO);

            yield return new WaitUntil(() => _isSceneLoaded);

            AssertSceneLoadedProperly(_firstSceneSO, FIRST_SCENE_GUID);
        }

        [UnityTest]
        public IEnumerator AdditiveLoadScene_SceneLoaded_AllDependentScenesLoaded()
        {
            yield return UniTask.ToCoroutine(
                () => _sceneManagerSO.SceneReference.TryLoadScene(LoadSceneMode.Single)
            );
            _sceneLoadedEvent.EventRaised += OnSceneLoaded;
            _additiveLoadSceneEvent.RaiseEvent(_firstSceneSO);

            yield return new WaitUntil(() => _isSceneLoaded);

            AssertSceneLoadedProperly(_firstSceneSO, FIRST_SCENE_GUID);
        }

        private void AssertSceneLoadedProperly(SceneSO sceneSO, string guid)
        {
            SceneTestHelper.AssertSceneLoadedProperly(sceneSO, guid);
        }

        private void AssertSceneNotLoaded(SceneSO sceneSO)
        {
            SceneTestHelper.AssertSceneNotLoaded(sceneSO);
        }

        [UnityTest]
        public IEnumerator ColdBoot_AllDependentScenesLoaded()
        {
            yield return Addressables.LoadSceneAsync(_firstSceneSO.SceneReference, LoadSceneMode.Single);

            var coldBoot = _coldBootObject.GetComponent<ColdBoot>();
            coldBoot.SetPrivateProperty("_thisSceneSO", _firstSceneSO);
            GameObject.Instantiate(_coldBootObject);

            _sceneLoadedEvent.EventRaised += OnSceneLoaded;
            yield return new WaitUntil(() => _isSceneLoaded);
            AssertSceneLoadedProperly(_firstSceneSO, FIRST_SCENE_GUID);
        }

        [UnityTest]
        public IEnumerator LoadScene_LinearLoadOtherScene_FirstSceneUnloadCorrectly()
        {
            yield return LinearLoadScene_SceneLoaded_AllDependentScenesLoaded();

            _isSceneLoaded = false;
            _linearLoadSceneEvent.RaiseEvent(_linearSceneSO);

            yield return new WaitUntil(() => _isSceneLoaded);
            AssertSceneLoadedProperly(_linearSceneSO, LINEAR_SCENE_GUID);
            AssertSceneNotLoaded(_firstSceneSO);
            AssertSceneNotLoaded(_firstDependentSceneSO);
        }

        [UnityTest]
        public IEnumerator LoadScene_AdditiveLoadOtherScene_FirstSceneStillLoaded()
        {
            yield return LinearLoadScene_SceneLoaded_AllDependentScenesLoaded();

            _isSceneLoaded = false;
            _additiveLoadSceneEvent.RaiseEvent(_additiveSceneSO);

            yield return new WaitUntil(() => _isSceneLoaded);
            AssertSceneLoadedProperly(_firstSceneSO, FIRST_SCENE_GUID);
            AssertSceneLoadedProperly(_additiveSceneSO, ADDITIVE_SCENE_GUID);
        }

        [UnityTest]
        public IEnumerator LoadScene_AdditiveLoadOtherScene_LinearLoadOtherScene_ScenesUnloadCorrectly()
        {
            yield return LoadScene_AdditiveLoadOtherScene_FirstSceneStillLoaded();

            _isSceneLoaded = false;
            _linearLoadSceneEvent.RaiseEvent(_linearSceneSO);

            yield return new WaitUntil(() => _isSceneLoaded);
            AssertSceneLoadedProperly(_linearSceneSO, LINEAR_SCENE_GUID);
            AssertSceneNotLoaded(_firstSceneSO);
            AssertSceneNotLoaded(_additiveSceneSO);
            AssertSceneNotLoaded(_firstDependentSceneSO);
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
            var coldBoot = _coldBootObject.GetComponent<ColdBoot>();
            coldBoot.SetPrivateProperty("_thisSceneSO", null);
            AssetDatabase.DeleteAsset(_firstSceneSOPath);

            var settings = AddressableAssetSettingsDefaultObject.Settings;
            settings.RemoveGroup(settings.FindGroup(SceneTestHelper.TEST_GROUP));
            _sceneManagerSO.SceneReference.ReleaseAsset();
        }
    }
}