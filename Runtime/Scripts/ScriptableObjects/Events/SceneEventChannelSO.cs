using UnityEngine;
using H2V.ExtensionsCore.Events.ScriptableObjects;

namespace H2V.SceneLoader.ScriptableObjects.Events
{
    [CreateAssetMenu(fileName = "SceneEvent", menuName = "H2V/Scene Manager/Events/Scene SO Event Channel")]
    public class SceneEventChannelSO : GenericEventChannelSO<SceneSO>
    {
        protected override void OnRaiseEvent(SceneSO sceneSO)
        {
            if (sceneSO == null)
            {
                Debug.LogWarning("OnRaiseEvent:: Raise Scene event with null");
                return;
            }
            
            base.OnRaiseEvent(sceneSO);
        }
    }
}