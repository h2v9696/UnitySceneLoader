using H2V.ExtensionsCore.AssetReferences;
using H2V.ExtensionsCore.ScriptableObjects;
using UnityEngine;

namespace H2V.SceneManager.ScriptableObjects
{
    [CreateAssetMenu(fileName = "SceneSO", menuName = "H2V/SceneManager/SceneSO")]
    public class SceneSO : SerializableScriptableObject
    {
        [field: SerializeField]
        public SceneAssetReference SceneReference { get; private set; }
    }
}