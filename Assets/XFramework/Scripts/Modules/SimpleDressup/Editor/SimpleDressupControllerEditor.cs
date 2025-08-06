using UnityEngine;
using UnityEditor;

namespace XFramework.SimpleDressup.Editor
{
    [CustomEditor(typeof(SimpleDressupController))]
    public class SimpleDressupControllerEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            var controller = (SimpleDressupController)target;

            // 在 Inspector 中显示合并后的材质
            if (Application.isPlaying && controller.CombinedMaterial != null)
            {
                EditorGUILayout.Space();
                EditorGUILayout.LabelField("Combined Material Preview", EditorStyles.boldLabel);

                // 创建一个临时的材质编辑器来显示材质球
                var materialEditor = (MaterialEditor)CreateEditor(controller.CombinedMaterial);
                if (materialEditor != null)
                {
                    materialEditor.OnInspectorGUI();
                    // 清理临时的编辑器实例
                    DestroyImmediate(materialEditor);
                }
            }
        }
    }
}
