using H2V.ExtensionsCore.AssetReferences;

#if UNITY_EDITOR
    using H2V.ExtensionsCore.Editor.Helpers;
#endif

namespace H2V.SceneLoader.ScriptableObjects
{
    public class SceneManagerSO : SceneSO
    {
        private const string SCENE_MANAGER_GUID = "c93932688e24e0843b61b851607ef2c6";
        private const string SCENE_GROUP = "SceneScriptableObjects";

#if UNITY_EDITOR
        protected override void OnValidate()
        {
            base.OnValidate();
            if (SceneReference.RuntimeKeyIsValid()) return;
            AddressableExtensions.SetObjectToAddressableGroup(SCENE_MANAGER_GUID, SCENE_GROUP, true);
            this.SetPrivateProperty("SceneReference",
                new SceneAssetReference(SCENE_MANAGER_GUID), true);
        }
#endif
    }
}