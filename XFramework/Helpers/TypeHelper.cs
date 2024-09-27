using System;
using System.Collections.Generic;
using System.Reflection;

public static class TypeHelper
{
    private static string[] GetAssignableTypeNamesFromBaseType(Type baseType, string[] assemblyNames)
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
