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
            if (Application.isPlaying && controller.AtlasMaterial != null)
            {
                EditorGUILayout.Space();
                EditorGUILayout.LabelField("Combined Material Preview", EditorStyles.boldLabel);

                using (new EditorGUI.DisabledGroupScope(true))
                {
                    EditorGUILayout.ObjectField("Combined Material", controller.AtlasMaterial, typeof(Material), false);
                }

                DrawMaterialPreview(controller.AtlasMaterial);
            }
        }

        private void DrawMaterialPreview(Material material)
        {
            if (material == null) return;

            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.LabelField("Shader", material.shader.name);

                var mainTexture = material.mainTexture;
                if (mainTexture != null)
                {
                    EditorGUILayout.ObjectField("Main Texture", mainTexture, typeof(Texture), false);
                }
                else
                {
                    EditorGUILayout.LabelField("Main Texture", "None");
                }
            }
        }
    }
}
