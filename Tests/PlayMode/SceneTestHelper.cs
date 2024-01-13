using H2V.ExtensionsCore.AssetReferences;
using H2V.SceneLoader.ScriptableObjects;
using UnityEngine;
using H2V.ExtensionsCore.Editor.Helpers;
using UnityEngine.SceneManagement;
using NUnit.Framework;
using UnityEditor;
using UnityEngine.ResourceManagement.ResourceProviders;

namespace H2V.SceneLoader.Tests
{
    public static class SceneTestHelper
    {
        public static string TEST_GROUP = "TestGroup";

        public static SceneSO InitAndCreateSceneSOAsset(string sceneGuid, string scenePath, bool isSave = false,
            params SceneSO[] dependentScenes)
        {
            var sceneSO = SceneTestHelper.InitSceneSO(sceneGuid, dependentScenes);
            AssetDatabase.DeleteAsset(scenePath);
            AssetDatabase.CreateAsset(sceneSO, scenePath);
            if (isSave)
            {
                AssetDatabase.SaveAssets();
            }

            return sceneSO;
        }

        public static SceneSO InitSceneSO(string sceneGuid, params SceneSO[] dependentScenes)
        {
            AddAssetToTempGroup(sceneGuid);
            return CreateSceneSO(sceneGuid, dependentScenes);
        }

        public static SceneSO CreateSceneSO(string sceneGuid, params SceneSO[] dependentScenes)
        {
            var sceneSO = ScriptableObject.CreateInstance<SceneSO>();
            sceneSO.SetPrivateProperty("SceneReference",
                new SceneAssetReference(sceneGuid), true);
            sceneSO.SetPrivateArrayProperty("DependentScenes", dependentScenes, true);
            return sceneSO;
        }

        public static void AddAssetToTempGroup(string assetGuid)
        {
            AddressableExtensions.SetObjectToAddressableGroup(assetGuid, TEST_GROUP);
        }
        
        public static void AssertSceneLoadedProperly(SceneSO sceneSO, string guid)
        {
            var loadedScene = SceneManager.GetSceneByName(sceneSO.SceneReference.editorAsset.name);
            Assert.IsTrue(loadedScene.isLoaded);
            var scenePath = AssetDatabase.GUIDToAssetPath(guid);
            Assert.AreEqual(loadedScene.path, scenePath);
 
            foreach (var scene in sceneSO.DependentScenes)
            {
                Assert.IsNotNull(scene.SceneReference.OperationHandle.Result);
                var dependentScene = (SceneInstance) scene.SceneReference.OperationHandle.Result;
                Assert.IsTrue(dependentScene.Scene.isLoaded);
            }
        }

        public static void AssertSceneNotLoaded(SceneSO sceneSO)
        {
            var loadedScene = SceneManager.GetSceneByName(sceneSO.SceneReference.editorAsset.name);
            Assert.IsTrue(!loadedScene.isLoaded);
        }
    }
}