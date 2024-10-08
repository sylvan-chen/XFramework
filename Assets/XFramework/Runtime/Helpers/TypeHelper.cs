using System;
using System.Collections.Generic;
using System.Reflection;
using XFramework.Utils;

namespace XFramework
{
    public static class TypeHelper
    {
        public static readonly string[] RuntimeAssemblyNames =
        {
            "Assembly-CSharp",
        };

        public static readonly string[] EditorAssemblyNames =
        {
            "Assembly-CSharp-Editor",
        };

        public static readonly Assembly[] AllAssemblies = AppDomain.CurrentDomain.GetAssemblies();

        /// <summary>
        /// 从所有程序集中获取类型
        /// </summary>
        /// <param name="typeName">类型名称</param>
        /// <returns>获取到的类型</returns>
        public static Type GetType(string typeName)
        {
            if (string.IsNullOrEmpty(typeName))
            {
                throw new ArgumentException("Type name cannot be null or empty.", nameof(typeName));
            }

            Type type = Type.GetType(typeName);
            if (type != null)
            {
                return type;
            }

            foreach (Assembly assembly in AllAssemblies)
            {
                type = assembly.GetType(typeName);
                if (type != null)
                {
                    return type;
                }
            }

            return null;
        }

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
