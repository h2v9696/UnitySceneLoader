using System;
using H2V.ExtensionsCore.AssetReferences;
using H2V.ExtensionsCore.ScriptableObjects;
using UnityEngine;

namespace H2V.SceneLoader.ScriptableObjects
{
    [CreateAssetMenu(fileName = "SceneSO", menuName = "H2V/Scene Manager/Scene SO")]
    public class SceneSO : SerializableScriptableObject
    {
        [field: SerializeField]
        public SceneAssetReference SceneReference { get; private set; }

        [Tooltip("These scene will be loaded first when the scene is loaded")]
        [field: SerializeField]
        public SceneSO[] DependentScenes { get; private set; } = Array.Empty<SceneSO>();
    }
}