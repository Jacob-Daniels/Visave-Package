using System;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

public static class DeserializationReflectionFactory
{
    private static Dictionary<string, Type> sm_componentTypes = new Dictionary<string, Type>();

    public static Dictionary<string, Type> GetDictionary()
    {
        if (sm_componentTypes == null) { sm_componentTypes = new Dictionary<string, Type>(); }
        return sm_componentTypes;
    }

    // ========================================================================================================================= //

    #region  Methods
    public static Type FindType(string typeName)
    {
        Type val;
        sm_componentTypes.TryGetValue(typeName, out val);
        return val;
    }

    public static void PopulateDictionary()
    {
        sm_componentTypes.Clear();
        foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
        {
            // Add all component types from the unity engine to the dictionary
            foreach (var currentType in assembly.GetTypes().Where(t => typeof(Component).IsAssignableFrom(t)))
            {
                sm_componentTypes.TryAdd(currentType.ToString(), currentType);
            }

            // Continue if assembly is not CSharp scripts
            if (assembly.GetName().Name != "Assembly-CSharp") { continue; }
            // Add custom CSharp scripts from current assembly that inherit from MonoBehaviour
            foreach (var currentType in assembly.GetTypes().Where(t => typeof(MonoBehaviour).IsAssignableFrom(t)))
            {
                sm_componentTypes.TryAdd(currentType.ToString(), currentType);
            }
        }
    }

    public static void PrintDictionary()
    {
        foreach (KeyValuePair<string, Type> pair in sm_componentTypes)
        {
            Debug.Log("Name: " + pair.Key + "\tType: " + pair.Value);
        }
    }
    #endregion

    // ========================================================================================================================= //
}
