using System.Collections.Generic;
using UnityEditor;

namespace XFramework.Editor
{
    [CustomEditor(typeof(ProcedureManager))]
    internal sealed class ProcedureManagerInspector : BaseInspector
    {
        private SerializedProperty _propertyProcedureTypeNames;
        private SerializedProperty _propertyFirstProcedureTypeName;

        private string[] _allProcedureTypeNames;
        private List<string> _availableProcedureTypeNames;
        private int _firstProcedureTypeNameIndex = -1;

        private void OnEnable()
        {
            _propertyProcedureTypeNames = serializedObject.FindProperty("_procedureTypeNames");
            _propertyFirstProcedureTypeName = serializedObject.FindProperty("_firstProcedureTypeName");

            Refresh();
        }

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            serializedObject.Update();
            var procedureManager = target as ProcedureManager;
        }

        private void Refresh()
        {
            // _allProcedureTypeNames = Type.GetRuntimeTypeNames(typeof(IProcedureManager));
        }
    }
}