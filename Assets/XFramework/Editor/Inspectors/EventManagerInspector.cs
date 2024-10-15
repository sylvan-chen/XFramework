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
                EditorGUILayout.HelpBox("Available in play mode only.", MessageType.Info);
                return;
            }

            EventManager targetObject = target as EventManager;
            EditorGUILayout.LabelField("Subscribed Event Count", targetObject.SubscribedEventCount.ToString());
            EditorGUILayout.LabelField("Delayed Event Count", targetObject.DelayedEventCount.ToString());

            Repaint();
        }
    }
}