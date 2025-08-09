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
            if (Application.isPlaying && controller.MaterialCombineResult.CombinedMaterial != null)
            {
                EditorGUILayout.Space();
                EditorGUILayout.LabelField("Combined Material Preview", EditorStyles.boldLabel);

                using (new EditorGUI.DisabledGroupScope(true))
                {
                    EditorGUILayout.ObjectField("Combined Material", controller.MaterialCombineResult.CombinedMaterial, typeof(Material), false);
                }

                DrawMaterialPreview(controller.MaterialCombineResult);
            }
        }

        private void DrawMaterialPreview(MaterialCombiner.MaterialCombineResult combineResult)
        {
            if (combineResult.CombinedMaterial == null) return;

            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.LabelField("Shader", combineResult.CombinedMaterial.shader.name);

                var mainTexture = combineResult.BaseAtlas;
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
