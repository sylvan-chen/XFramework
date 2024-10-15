using UnityEditor;

namespace XFramework.Editor
{
    [CustomEditor(typeof(EventManager))]
    internal sealed class EventManagerInspector : InspectorBase
    {
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            if (!EditorApplication.isPlaying)
            {
                EditorGUILayout.HelpBox("Available in play mode.", MessageType.Info);
                return;
            }

            EventManager eventManager = target as EventManager;
            EditorGUILayout.LabelField("Subscribed Event Count", eventManager.SubscribedEventCount.ToString());
            EditorGUILayout.LabelField("Delayed Event Count", eventManager.DelayedEventCount.ToString());

            Repaint();
        }
    }
}