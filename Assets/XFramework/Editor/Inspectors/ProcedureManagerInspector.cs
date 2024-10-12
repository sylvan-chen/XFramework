using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;

namespace XFramework.Editor
{
    [CustomEditor(typeof(ProcedureManager))]
    internal sealed class ProcedureManagerInspector : InspectorBase
    {
        private SerializedProperty _availableProcedureTypeNames;
        private SerializedProperty _startupProcedureTypeName;

        private string[] _allProcedureTypeNames;                  // 当前项目中存在的所有 Procedure 类型名称
        private List<string> _currentAvailableProcedureTypeNames = new(); // 当前可用的 Procedure 列表，发生变化时写入到序列化属性中
        private int _currentStartupProcedureTypeNameIndex = -1;   // 当前 Startup Procedure 的索引，发生变化时写入到序列化属性中

        private void OnEnable()
        {
            _availableProcedureTypeNames = serializedObject.FindProperty("_availableProcedureTypeNames");
            _startupProcedureTypeName = serializedObject.FindProperty("_startupProcedureTypeName");

            UpdateSubtypeNames();
        }

        protected override void OnCompileFinish()
        {
            base.OnCompileFinish();

            UpdateSubtypeNames();
        }

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            serializedObject.Update();

            var procedureManager = target as ProcedureManager;

            // 游戏运行时，显示当前 Procedure
            if (EditorApplication.isPlaying)
            {
                EditorGUILayout.LabelField("Current Procedure", procedureManager.CurrentProcedure == null ? "None" : procedureManager.CurrentProcedure.GetType().Name);
                EditorGUILayout.LabelField("Current Procedure Time", procedureManager.CurrentProcedureTime.ToString("N2"));
                EditorGUILayout.Separator();
            }
            else if (string.IsNullOrEmpty(_startupProcedureTypeName.stringValue))
            {
                EditorGUILayout.HelpBox("First procedure invalid.", MessageType.Error);
                EditorGUILayout.Separator();
            }

            using (new EditorGUI.DisabledGroupScope(EditorApplication.isPlaying))
            {
                EditorGUILayout.LabelField("Available Procedures", EditorStyles.boldLabel);
                if (_allProcedureTypeNames.Length > 0)
                {
                    // 显示可选的 Procedure 列表
                    using (new EditorGUILayout.VerticalScope("box"))
                    {
                        // 所有 Procedure 都会被展示，但只有被选择的 Procedure 真正写入到序列化属性中
                        foreach (string typeName in _allProcedureTypeNames)
                        {
                            bool selectStatus = _currentAvailableProcedureTypeNames.Contains(typeName);
                            bool isSelectedByUser = EditorGUILayout.ToggleLeft(typeName, selectStatus);
                            // 如果选择状态发生变化，则更新 _currentAvailableProcedureTypeNames 列表
                            if (isSelectedByUser != selectStatus)
                            {
                                if (isSelectedByUser)
                                {
                                    _currentAvailableProcedureTypeNames.Add(typeName);
                                    WritePropertyAvailableProcedureTypeNames();
                                }
                                else if (typeName != _startupProcedureTypeName.stringValue) // 不能删除 Startup Procedure
                                {
                                    _currentAvailableProcedureTypeNames.Remove(typeName);
                                    WritePropertyAvailableProcedureTypeNames();
                                }
                            }
                        }
                    }
                }
                else
                {
                    EditorGUILayout.HelpBox("No available procedures.", MessageType.Warning);
                }

                // 显示 Startup Procedure 选择框
                if (_currentAvailableProcedureTypeNames.Count > 0)
                {
                    EditorGUILayout.Separator();
                    if (string.IsNullOrEmpty(_startupProcedureTypeName.stringValue))
                    {
                        EditorGUILayout.HelpBox("Select a startup procedure.", MessageType.Warning);
                    }
                    int selectedIndexByUser = EditorGUILayout.Popup("Startup Procedure", _currentStartupProcedureTypeNameIndex, _currentAvailableProcedureTypeNames.ToArray());
                    if (selectedIndexByUser != _currentStartupProcedureTypeNameIndex)
                    {
                        _currentStartupProcedureTypeNameIndex = selectedIndexByUser;
                        WritePropertyStartupProcedure(_currentAvailableProcedureTypeNames[_currentStartupProcedureTypeNameIndex]);
                    }
                }
            }

            serializedObject.ApplyModifiedProperties();
            Repaint();
        }

        /// <summary>
        /// 更新所有 Procedure 类型名称，并根据是否变动写入序列化属性
        /// </summary>
        private void UpdateSubtypeNames()
        {
            _allProcedureTypeNames = TypeHelper.GetSubtypeNamesRuntime(typeof(ProcedureBase));
            // 读取原来属性中的可用列表，用于跟新的 _allProcedureTypeNames 进行比较
            _currentAvailableProcedureTypeNames.Clear();
            for (int i = 0; i < _availableProcedureTypeNames.arraySize; i++)
            {
                _currentAvailableProcedureTypeNames.Add(_availableProcedureTypeNames.GetArrayElementAtIndex(i).stringValue);
            }
            int countBeforeFilter = _currentAvailableProcedureTypeNames.Count;
            // 过滤掉已经不存在的类型名称，获得新的可用列表
            _currentAvailableProcedureTypeNames = _currentAvailableProcedureTypeNames.Where(x => _allProcedureTypeNames.Contains(x)).ToList();
            // 如果过滤前后长度发生变化，则需要写入新的属性
            if (countBeforeFilter != _currentAvailableProcedureTypeNames.Count)
            {
                WritePropertyAvailableProcedureTypeNames();
            }
            else if (!string.IsNullOrEmpty(_startupProcedureTypeName.stringValue))
            {
                // 如果 Startup Procedure 名称已经不在当前可选列表中，则清空 Startup Procedure 名称
                _currentStartupProcedureTypeNameIndex = _currentAvailableProcedureTypeNames.IndexOf(_startupProcedureTypeName.stringValue);
                if (_currentStartupProcedureTypeNameIndex < 0)
                {
                    WritePropertyStartupProcedure(null);
                }
            }

            serializedObject.ApplyModifiedProperties();
        }

        /// <summary>
        /// 写入 AvailableProcedureTypeNames 属性
        /// </summary>
        private void WritePropertyAvailableProcedureTypeNames()
        {
            _availableProcedureTypeNames.ClearArray();
            if (_currentAvailableProcedureTypeNames == null)
            {
                return;
            }
            _currentAvailableProcedureTypeNames.Sort();
            for (int i = 0; i < _currentAvailableProcedureTypeNames.Count; i++)
            {
                _availableProcedureTypeNames.InsertArrayElementAtIndex(i);
                _availableProcedureTypeNames.GetArrayElementAtIndex(i).stringValue = _currentAvailableProcedureTypeNames[i];
            }
            // 如果 Startup Procedure 已经不在可选列表中，则清空 Startup Procedure 名称
            if (!string.IsNullOrEmpty(_startupProcedureTypeName.stringValue))
            {
                _currentStartupProcedureTypeNameIndex = _currentAvailableProcedureTypeNames.IndexOf(_startupProcedureTypeName.stringValue);
                if (_currentStartupProcedureTypeNameIndex < 0)
                {
                    WritePropertyStartupProcedure(null);
                }
            }
        }

        /// <summary>
        /// 写入 StartupProcedure 属性
        /// </summary>
        private void WritePropertyStartupProcedure(string typeName)
        {
            _startupProcedureTypeName.stringValue = typeName ?? null;
        }
    }
}