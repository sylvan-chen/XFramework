using System;
using System.Collections.Generic;
using System.Reflection;
using XFramework.Utils;

namespace XFramework
{
    public static class TypeHelper
    {
        private static readonly string[] RuntimeAssemblyNames =
        {
            "Assembly-CSharp",
        };

        private static readonly string[] EditorAssemblyNames =
        {
            "Assembly-CSharp-Editor",
        };

        public static string[] GetRuntimeSubtypeNames(Type baseType)
        {
            return FindSubtypeNames(baseType, RuntimeAssemblyNames);
        }

        public static string[] GetEditorSubtypeNames(Type baseType)
        {
            return FindSubtypeNames(baseType, EditorAssemblyNames);
        }

        public static string[] GetRuntimeAndEditorSubtypeNames(Type baseType)
        {
            string[] runtimeTypeNames = GetRuntimeSubtypeNames(baseType);
            string[] editorTypeNames = GetEditorSubtypeNames(baseType);
            string[] allTypeNames = new string[runtimeTypeNames.Length + editorTypeNames.Length];
            runtimeTypeNames.CopyTo(allTypeNames, 0);
            editorTypeNames.CopyTo(allTypeNames, runtimeTypeNames.Length);
            return allTypeNames;
        }

        /// <summary>
        /// 从指定程序集中查找指定基类的所有子类名称
        /// </summary>
        /// <param name="baseType">基类类型</param>
        /// <param name="assemblyNames">程序集名称数组</param>
        /// <returns></returns>
        private static string[] FindSubtypeNames(Type baseType, string[] assemblyNames)
        {
            var typeNames = new List<string>();
            foreach (string assemblyName in assemblyNames)
            {
                Assembly assembly = null;
                try
                {
                    assembly = Assembly.Load(assemblyName);
                }
                catch (Exception)
                {
                    Log.Warning($"[XFramework] [TypeHelper] Failed to load assembly {assemblyName}.");
                    continue;
                }

                if (assembly == null)
                {
                    continue;
                }

                foreach (Type type in assembly.GetTypes())
                {
                    if (type.IsClass && !type.IsAbstract && baseType.IsAssignableFrom(type))
                    {
                        typeNames.Add(type.FullName);
                    }
                }
            }
            typeNames.Sort();
            return typeNames.ToArray();
        }
    }
}
