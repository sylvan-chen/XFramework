using System;
using UnityEngine;
using XFramework.Utils;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace XFramework.SimpleDressup
{
    /// <summary>
    /// 服装类型 - 定义角色可以装备的部位
    /// </summary>
    [Flags]
    public enum DressupType
    {
        None = 0,
        Body = 1 << 0,      // 身体
        Face = 1 << 1,      // 脸部
        Hair = 1 << 2,      // 头发  
        Top = 1 << 3,       // 上衣
        Bottom = 1 << 4,    // 下衣
        Shoes = 1 << 5,     // 鞋子
        Gloves = 1 << 6,    // 手套
        Hat = 1 << 7,       // 帽子
        All = ~0            // 所有部位
    }

    /// <summary>
    /// 换装部件
    /// 包含合并需要的三个重要信息：网格、材质和骨骼
    /// </summary>
    [Serializable]
    public class DressupItem
    {
        [SerializeField] private DressupType _dressupType = DressupType.None;
        [SerializeField] private Renderer _renderer;

        // 基础信息
        public DressupType DressupType => _dressupType;
        public Renderer Renderer => _renderer;
        public bool IsSkinnedMesh => _renderer is SkinnedMeshRenderer;
        // 渲染信息
        public Mesh Mesh { get; private set; }
        public Material[] Materials { get; private set; }
        // 骨骼信息
        public Transform[] Bones { get; private set; }
        public Transform RootBone { get; private set; }

        public bool IsValid => Mesh != null && Materials != null && Materials.Length > 0;
        public int SubmeshCount => Mesh != null ? Mesh.subMeshCount : 0;

        public void Init()
        {
            if (_renderer == null)
            {
                Log.Error("[DressupItem] Renderer is null.");
                return;
            }

            // 获取网格和材质
            if (_renderer is SkinnedMeshRenderer skinnedRenderer)
            {
                // SkinnedMeshRenderer处理
                if (skinnedRenderer.sharedMesh == null)
                {
                    Log.Error("[DressupItem] SkinnedMeshRenderer's sharedMesh is null.");
                    return;
                }

                Mesh = skinnedRenderer.sharedMesh;
                Materials = skinnedRenderer.sharedMaterials;
                Bones = skinnedRenderer.bones;
                RootBone = skinnedRenderer.rootBone;
            }
            else if (_renderer is MeshRenderer meshRenderer)
            {
                // MeshRenderer处理
                var meshFilter = meshRenderer.GetComponent<MeshFilter>();
                if (meshFilter == null || meshFilter.sharedMesh == null)
                {
                    Log.Error("[DressupItem] MeshRenderer's MeshFilter or sharedMesh is null.");
                    return;
                }

                Mesh = meshFilter.sharedMesh;
                Materials = meshRenderer.sharedMaterials;
                // 静态网格没有骨骼信息
                Bones = null;
                RootBone = null;
            }
            else
            {
                Log.Error($"[DressupItem] Unsupported renderer type: {_renderer.GetType().Name}");
            }
        }
    }

#if UNITY_EDITOR
    /// <summary>
    /// 让_dressupType和_renderer两个字段并排显示
    /// 支持SkinnedMeshRenderer和MeshRenderer的选择
    /// </summary>
    [CustomPropertyDrawer(typeof(DressupItem))]
    public class DressupItemDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);

            // 绘制标签
            position = EditorGUI.PrefixLabel(position, GUIUtility.GetControlID(FocusType.Passive), label);

            // 不缩进子属性
            var indent = EditorGUI.indentLevel;
            EditorGUI.indentLevel = 0;

            // 计算每个字段的矩形区域
            var typeRect = new Rect(position.x, position.y, position.width * 0.3f, position.height);
            var rendererRect = new Rect(position.x + position.width * 0.3f + 5, position.y, position.width * 0.7f - 5, position.height);

            var dressupTypeProperty = property.FindPropertyRelative("_dressupType");
            var rendererProperty = property.FindPropertyRelative("_renderer");

            // 绘制字段
            EditorGUI.PropertyField(typeRect, dressupTypeProperty, GUIContent.none);
            EditorGUI.PropertyField(rendererRect, rendererProperty, GUIContent.none);

            // 恢复缩进
            EditorGUI.indentLevel = indent;

            EditorGUI.EndProperty();
        }
    }
#endif
}