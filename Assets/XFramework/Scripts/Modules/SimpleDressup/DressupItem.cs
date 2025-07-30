using System;
using UnityEngine;
using XFramework.Utils;
using System.Collections.Generic;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace XFramework.SimpleDressup
{
    /// <summary>
    /// 服装类型 - 定义角色可以装备的部位
    /// </summary>
    [Serializable]
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
    /// 外观部件
    /// 包含模型的三个重要信息：网格、材质和骨骼
    /// </summary>
    [Serializable]
    public class DressupItem
    {
        [SerializeField] private DressupType _dressupType = DressupType.None;
        [SerializeField] private SkinnedMeshRenderer _renderer;

        public DressupType DressupType => _dressupType;
        public SkinnedMeshRenderer Renderer => _renderer;

        public Mesh Mesh { get; private set; }             // 网格
        public Material[] Materials { get; private set; }  // 材质
        public Transform[] Bones { get; private set; }     // 骨骼
        public Transform RootBone { get; private set; }    // 根骨骼

        public bool IsValid => Mesh != null && Materials != null && Materials.Length > 0;
        public int SubmeshCount => Mesh != null ? Mesh.subMeshCount : 0;

        public void Init()
        {
            if (_renderer == null)
            {
                Log.Warning("[DressupItem] SkinnedMeshRenderer is null.");
                return;
            }

            Mesh = _renderer.sharedMesh;
            Materials = _renderer.sharedMaterials;
            Bones = _renderer.bones;
            RootBone = _renderer.rootBone;
        }

        /// <summary>
        /// 按骨骼名字重映射到新的骨骼
        /// </summary>
        /// <param name="boneMap">骨骼映射字典</param>
        /// <returns>重映射是否成功</returns>
        public bool RemapBones(Dictionary<string, Transform> boneMap)
        {
            if (Bones == null || Bones.Length == 0 || RootBone == null)
            {
                Log.Warning("[DressupItem] Bones or RootBone is null or empty.");
                return false;
            }

            if (boneMap == null || boneMap.Count == 0)
            {
                Log.Warning("[DressupItem] Bone map is null or empty.");
                return false;
            }

            for (int i = 0; i < Bones.Length; i++)
            {
                if (boneMap.TryGetValue(Bones[i].name, out var targetBone))
                {
                    Bones[i] = targetBone;
                }
                else
                {
                    Log.Warning($"[DressupItem] Bone '{Bones[i].name}' not found in boneMap.");
                    return false;
                }
            }

            if (boneMap.TryGetValue(RootBone.name, out var targetRootBone))
            {
                RootBone = targetRootBone;
            }
            else
            {
                Log.Warning($"[DressupItem] Root bone '{RootBone.name}' not found in boneMap.");
                return false;
            }

            _renderer.bones = Bones;
            _renderer.rootBone = RootBone;

            return true;
        }
    }

#if UNITY_EDITOR
    /// <summary>
    /// 让_dressupType和_renderer两个字段并排显示
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

            EditorGUI.PropertyField(typeRect, dressupTypeProperty, GUIContent.none);
            EditorGUI.PropertyField(rendererRect, rendererProperty, GUIContent.none);

            // 恢复缩进
            EditorGUI.indentLevel = indent;

            EditorGUI.EndProperty();
        }
    }
#endif
}