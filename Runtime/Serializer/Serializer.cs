using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization.Formatters;
using UnityEditor.Experimental.GraphView;
using UnityEditor.UIElements;
using UnityEngine;
using Visave;
using Component = UnityEngine.Component;

/// <summary>
/// Serializer handles all data conversion. It interacts with the Lexer and Parser to serialize and deserialize data correctly.
/// </summary>
/// <remarks>
/// It formats generic types using C# reflection. The two scripts that are accessed using reflection are SaveInstance & SaveComponentData.
/// Reflection is used to get the data of all the objects / variables stored in the above two scripts.
/// </remarks>

public static class Serializer
{
    #region Members
    public static readonly string SAVE_FOLDER_PATH = Application.persistentDataPath + "/Saves";
    public static readonly string VISAVE_FILE_TYPE = ".visave";
    public static readonly string JSON_FILE_TYPE = ".json";
    // String format variables
    private static readonly string OBJECT = ".OBJECT";
    private static readonly string END_OBJECT = ".END_OBJECT";
    private static readonly string COMPONENT = ".COMPONENT";
    private static readonly string PROPERTY = ".{0}\t\t\t> \"{1}\"";
    private static readonly string ARRAY = ".{0}\t\t\t> .GROUP";
    private static readonly string ELEMENT = ".{0}\t> \"{1}\"";
    #endregion

    // ========================================================================================================================= //
    // JSON Serialization is one of the data formats for saving objects.
    #region JSON Methods - FOR TESTING
    public static string ToJSON()
    {
        return "";
    }
    private static void InitJSONDirectory() { if (!System.IO.Directory.Exists(JSON_FILE_TYPE)) { System.IO.Directory.CreateDirectory(JSON_FILE_TYPE); } }   // Initialise the directory
    public static string LoadJSONFile(string fileName)
    {
        if (string.IsNullOrEmpty(fileName)) { Debug.LogWarning("Warning: File name is null!"); return string.Empty; }

        InitJSONDirectory();

        // Open directory
        DirectoryInfo dirInfo = new DirectoryInfo(SAVE_FOLDER_PATH);
        FileInfo[] saveFiles = dirInfo.GetFiles();
        // Loop files in directory to find correct file
        foreach (FileInfo file in saveFiles)
        {
            // File found - Tokenize & Parse data
            if (file.Name.Equals(fileName + JSON_FILE_TYPE))
            {
                // Find file
                using StreamReader reader = new StreamReader(file.FullName);

                string fileData = reader.ReadToEnd(); ;
                return fileData;
            }
        }
        Debug.LogWarning("Warning: Could not load file - " + fileName);
        return string.Empty;
    }
    #endregion

    // ========================================================================================================================= //
    // Accessible methods when saving objects using Visave data format
    #region Public Visave Methods
    /* SavesInDirectory returns all file types of VISAVE_FILE_TYPE.  */
    public static List<string> GetAllSavesFromDirectory()
    {
        List<string> foundSaves = new List<string>();
        // Loop directory for files
        DirectoryInfo dirInfo = new DirectoryInfo(SAVE_FOLDER_PATH);
        FileInfo[] saveFiles = dirInfo.GetFiles("*" + VISAVE_FILE_TYPE);
        foreach (FileInfo file in saveFiles)
        {
            foundSaves.Add(file.Name);
        }
        return foundSaves;
    }
    #endregion

    // ========================================================================================================================= //

    #region Deserializer Methods
    /* FromVisave calls a private/internal function of iteself and converts the correct file type into a SaveInstance */
    public static VisaveInstance FromVisave(string fileName, string path) { return (VisaveInstance)FromVisaveInternal(fileName, typeof(VisaveInstance), path); }

    private static void InitVisaveDirectory() { if (!System.IO.Directory.Exists(VISAVE_FILE_TYPE)) { System.IO.Directory.CreateDirectory(VISAVE_FILE_TYPE); } }   // Initialise the directory
    private static object FromVisaveInternal(string fileName, Type type, string path)
    {
        if (string.IsNullOrEmpty(fileName) || type == null) { Debug.LogWarning("Warning: File name is null!"); return null; }

        InitVisaveDirectory();

        // Open directory
        DirectoryInfo dirInfo = new DirectoryInfo(path);
        FileInfo[] saveFiles = dirInfo.GetFiles();
        FileInfo foundFile = null;
        // Find and grab file from directory
        foreach (FileInfo file in saveFiles)
        {
            if (file.Name.Equals(fileName + VISAVE_FILE_TYPE))
            {
                foundFile = file;
                break;
            }
        }
        // Check file exists
        if (foundFile == null) { return null; }

        // Tokenize file - List of tokens
        Lexer.TokenizeFile(foundFile);

        // Create Parse Tree - AST (Tree) of tokens
        Parser.CreateAST();

        // Populate object with data from Token Tree (Parser Tree)
        VisaveInstance obj = new VisaveInstance("Loaded Save");

        // Create and populate dictionary
        DeserializationReflectionFactory.PopulateDictionary();
        // Loop tree & set appropriate fields/properties
        DeserializeVisaveInstanceFromParserTree(ref obj, Parser.GetTree().root.GetLastChild());

        // Loop files in directory to find correct file
        return obj;
    }
    private static void DeserializeVisaveInstanceFromParserTree(ref VisaveInstance baseObject, Parser.Node node)
    {
        Lexer.Token.Type tokenType = node.GetToken().m_type;
        // Check parent type
        if (tokenType == Lexer.Token.Type.OBJECT)
        {
            // Get the first 2 fields (Name and GameObject)
            FieldInfo fieldInfo = baseObject.GetType().GetField(node.GetChildren()[0].GetToken().m_value);
            fieldInfo.SetValue(baseObject, node.GetChildren()[0].GetLastChild().GetToken().m_value);

            // GameObject
            fieldInfo = baseObject.GetType().GetField(node.GetChildren()[1].GetToken().m_value);
            GameObject newgameobj = GameObject.Find(node.GetChildren()[1].GetLastChild().GetToken().m_value);

            fieldInfo.SetValue(baseObject, newgameobj);

            // Loop the rest of the children
            int startIndex = 2;
            for (int i = startIndex; i < node.GetChildren().Count; i++)
            {
                // loop all component data
                DeserializeVisaveInstanceFromParserTree(ref baseObject, node.GetChildren()[i]);
            }
        }
        else if (tokenType == Lexer.Token.Type.COMPONENT)
        {
            // 'name' field
            FieldInfo fieldInfo = baseObject.GetType().GetField(node.GetChildren()[0].GetToken().m_value);
            fieldInfo.SetValue(baseObject, node.GetChildren()[0].GetLastChild().GetToken().m_value);

            // Find component from factory
            Type compType = DeserializationReflectionFactory.FindType(node.GetChildren()[1].GetLastChild().GetToken().m_value);

            // Get component from object (Add it to the GameObject if it can't be found)
            Component foundComp = baseObject.m_saveInstance.GetComponent(compType);
            if (foundComp == null) { foundComp = baseObject.m_saveInstance.AddComponent(compType); }

            // Get all member fields to find the correct one
            BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
            MemberInfo[] memberArray = foundComp.GetType().GetFields(flags).Cast<MemberInfo>().Concat(foundComp.GetType().GetProperties(flags)).ToArray();
            MemberInfo memberInfo;

            // Loop all children of save list variable
            Debug.Log("Parent to childnode = " + node.GetChildren()[2].GetToken().m_value);
            Debug.Log("Found comp = " + foundComp.GetType());
            foreach (Parser.Node childNode in node.GetChildren()[2].GetChildren())
            {
                Debug.Log("Child value: " + childNode.GetToken().m_value);

                // Validation check - Is value found as property or field on component
                memberInfo = foundComp.GetType().GetProperty(childNode.GetToken().m_value, flags) != null ?
                    foundComp.GetType().GetProperty(childNode.GetToken().m_value, flags) : foundComp.GetType().GetField(childNode.GetToken().m_value, flags);
                if (memberInfo == null) { Debug.LogWarning("Member Info is NULL!"); continue; }

                Debug.Log("name = " + memberInfo);

                if (memberInfo.MemberType == MemberTypes.Field)
                {
                    fieldInfo = (FieldInfo)memberInfo;
                    if (fieldInfo.FieldType.ToString() == "UnityEngine.LayerMask")
                    {
                        // Get the field and overwrite its values for LayerMasks
                        LayerMask newMask = (LayerMask)fieldInfo.GetValue(foundComp);
                        object maskValue = SerializeEngineType.StringToType(fieldInfo.FieldType.Name, childNode.GetLastChild().GetToken().m_value);
                        BitArray bitArray = new BitArray(new int[] { (int)maskValue });
                        for (int i = 0; i < bitArray.Length; i++)
                        {
                            if (bitArray[i]) { newMask |= (1 << i); }
                        }
                        fieldInfo.SetValue(foundComp, newMask);
                        continue;
                    }
                    if (fieldInfo.FieldType.IsEnum)
                    {
                        // Format enum types
                        object enumValue = Convert.ChangeType(fieldInfo.GetValue(sm_currentComp), Enum.GetUnderlyingType(fieldInfo.GetValue(sm_currentComp).GetType()));
                        fieldInfo.SetValue(foundComp, enumValue);
                        continue;
                    }

                    // Convert data to correct type from field name
                    object testObj = SerializeEngineType.StringToType(fieldInfo.FieldType.Name, childNode.GetLastChild().GetToken().m_value);
                    try
                    {
                        fieldInfo.SetValue(foundComp, testObj);
                    }
                    catch
                    {
                        Debug.LogError("Unable to deserialize: " + fieldInfo.Name + ", type: " + fieldInfo.FieldType);
                        continue;
                    }
                }

                if (memberInfo.MemberType == MemberTypes.Property)
                {
                    PropertyInfo propertyInfo = (PropertyInfo)memberInfo;

                    // Check data type is an Enum
                    if (propertyInfo.PropertyType.ToString() == "UnityEngine.LayerMask")
                    {
                        // Get the field and overwrite its values for LayerMasks
                        LayerMask newMask = (LayerMask)propertyInfo.GetValue(foundComp);
                        object maskValue = SerializeEngineType.StringToType(propertyInfo.PropertyType.Name, childNode.GetLastChild().GetToken().m_value);
                        BitArray bitArray = new BitArray(new int[] { (int)maskValue });
                        for (int i = 0; i < bitArray.Length; i++)
                        {
                            if (bitArray[i]) { newMask |= (1 << i); }
                        }
                        propertyInfo.SetValue(foundComp, newMask);
                        continue;
                    }
                    if (propertyInfo.PropertyType.IsEnum)
                    {
                        // Format enum types
                        try
                        {
                            object enumValue = Convert.ChangeType(propertyInfo.GetValue(sm_currentComp),
                                Enum.GetUnderlyingType(propertyInfo.GetValue(sm_currentComp).GetType()));
                        }
                        catch
                        {
                            Debug.LogError("Error: Deserialization of ENUM type has been disabled in Visave Serializer!");
                        }
                    }


                    // Convert data to correct type from field name
                    object testObj = SerializeEngineType.StringToType(propertyInfo.PropertyType.Name, childNode.GetLastChild().GetToken().m_value);
                    try
                    {
                        propertyInfo.SetValue(foundComp, testObj);
                    }
                    catch
                    {
                        Debug.LogError("Unable to deserialize: " + fieldInfo.Name + ", type: " + fieldInfo.FieldType);
                    }
                }
            }
        }
    }
    #endregion

    // ========================================================================================================================= //

    #region Serialization Methods
    /* ToVisave calls a private/internal function of iteself to convert the passed in object into the correct data format */
    public static string ToVisave(object obj, Type type)
    {
        // Return nothing if parameters are null
        if (obj == null || type == null)
            return "";
        return ToVisaveInternal(obj, type);
    }

    /* FormatGenericType is a custom function that uses 'Reflection' to get variable (field) data. Recursion is used to ensure each field is formatted correctly. */
    public static object FormatGenericType<T>(T obj, int indent = 0, bool arrayElement = false)
    {
        var fileData = string.Empty;

        // Return single formatted object
        if (obj is not IEnumerable) { return CheckFields(obj, indent); }

        // Cast currentObject to IEnumerable (array)
        foreach (object o in obj as IEnumerable)
        {
            fileData += CheckFields(o, indent, arrayElement);
        }
        return fileData;
    }
    private static string ToVisaveInternal(object obj, Type type)
    {
        string fileText = OBJECT;

        // Pass in object to be formatted into string type
        MethodInfo methInfo = typeof(Serializer).GetMethod("FormatGenericType").MakeGenericMethod(obj.GetType());
        object[] objects = { obj, 1, false };
        fileText += methInfo.Invoke(null, objects);
        fileText += "\n" + END_OBJECT;
        return fileText;
    }
    private static UnityEngine.Component sm_currentComp;
    private static object CheckFields(object obj, int indent = 0, bool arrayElement = false)
    {
        var fieldData = string.Empty;

        // Return object data if it is a default System.Type (Looping this would cause further breakdown of the field that is not needed)
        if (obj.GetType().GetTypeInfo().IsValueType && arrayElement)
        {
            // Before returning, grab the correct field data of the current element value (m_saveToken stores an index which we use here to get the appropriate field data to serialize)
            string memberName = string.Empty;
            string elementData = string.Empty;
            // Get the component field to loop its properties
            if (sm_currentComp != null)
            {
                // loop member fields of component and add the correct data after the object
                BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
                MemberInfo[] memberArray = sm_currentComp.GetType().GetFields(flags).Cast<MemberInfo>().Concat(sm_currentComp.GetType().GetProperties(flags)).ToArray();
                int slIndex = 0;
                foreach (MemberInfo memberInfo in memberArray)
                {
                    // Check memberInfo is valid info to display in editor
                    if (memberInfo.Name == "parent" || memberInfo.Name == "parentInternal" || memberInfo.Name == "sharedMaterial" || memberInfo.Name == "hideFlags") { continue; }
                    slIndex++;
                    if (slIndex == (int)obj)
                    {
                        memberName = memberInfo.Name;
                        if (memberInfo.MemberType == MemberTypes.Field)
                        {
                            FieldInfo fieldInfo = (FieldInfo)memberInfo;
                            if (fieldInfo.FieldType.ToString() == "UnityEngine.LayerMask")
                            {
                                // Format layermask value into a bit array (easier to deserialize)
                                LayerMask mask = (LayerMask)fieldInfo.GetValue(sm_currentComp);
                                elementData += mask.value;
                                continue;
                            }
                            if (fieldInfo.FieldType.IsEnum)
                            {
                                object enumValue = Convert.ChangeType(fieldInfo.GetValue(sm_currentComp), Enum.GetUnderlyingType(fieldInfo.GetValue(sm_currentComp).GetType()));
                                Debug.Log("ENUM FOUND - field: " + enumValue);
                                elementData += enumValue;
                                continue;
                            }
                            elementData += fieldInfo.GetValue(sm_currentComp).ToString();
                            //elementData += ((FieldInfo)memberInfo).GetValue(sm_currentComp).ToString();
                        }
                        else if (memberInfo.MemberType == MemberTypes.Property)
                        {
                            PropertyInfo propertyInfo = (PropertyInfo)memberInfo;
                            if (propertyInfo.PropertyType.ToString() == "UnityEngine.LayerMask")
                            {
                                // Format layermask value into a bit array (easier to deserialize)
                                LayerMask mask = (LayerMask)propertyInfo.GetValue(sm_currentComp);
                                elementData += mask.value;
                                continue;
                            }
                            if (propertyInfo.PropertyType.IsEnum)
                            {
                                // Format enum types
                                object enumValue = Convert.ChangeType(propertyInfo.GetValue(sm_currentComp), Enum.GetUnderlyingType(propertyInfo.GetValue(sm_currentComp).GetType()));
                                elementData += enumValue;
                                continue;
                            }
                            // Convert field data into a string to serialize
                            elementData += propertyInfo.GetValue(sm_currentComp).ToString();
                        }
                        else
                        {
                            Debug.Log("Member type is: " + memberInfo.MemberType);
                        }
                    }
                }
            }
            return ELEMENT_FORMAT(memberName, elementData, indent + 1);
        }

        // Add Component header to format data in file
        VisaveComponentData viCompObj = obj as VisaveComponentData;
        if (viCompObj != null) { fieldData += "\n" + Indent(indent - 1) + COMPONENT; }

        // Format fields of object passed in
        FieldInfo[] fields = obj.GetType().GetFields();
        foreach (FieldInfo fieldInfo in fields)
        {
            // Check field shouldn't be saved (HARD CODED - VISAVECOMPONENTDATA)
            switch (fieldInfo.Name) { case "m_save": case "m_scrollPosition": case "m_addAllFields": continue; }

            // Check each field type (E.g. list, gameobject, default { int, byte, string etc.. })
            if (fieldInfo.FieldType.IsGenericType && fieldInfo.FieldType.GetGenericTypeDefinition() == typeof(List<>))
            {
                // Check value type is not generic (of System)
                Type newType = fieldInfo.FieldType.GetGenericArguments()[0];
                MethodInfo methInfo = typeof(Serializer).GetMethod("FormatGenericType").MakeGenericMethod(fieldInfo.FieldType);

                if (newType.IsValueType)
                {
                    // Initial variable name for list
                    fieldData += ARRAY_FORMAT(fieldInfo.Name, indent);
                    // Is ValueType - Format fields onto a single line
                    object[] objects = { fieldInfo.GetValue(obj), indent, true };
                    fieldData += methInfo.Invoke(null, objects);
                    fieldData += "\n" + Indent(indent) + ".END";
                }
                else
                {
                    // Is not value type - Do not format fields onto a single line
                    object[] objects = { fieldInfo.GetValue(obj), indent + 1, false };
                    fieldData += methInfo.Invoke(null, objects);
                }
            }
            else
            {
                // GameObject type
                if (fieldInfo.GetValue(obj).GetType() == typeof(GameObject)) { fieldData += PROPERTY_FORMAT(fieldInfo.Name, (fieldInfo.GetValue(obj) as GameObject).name as object, indent); continue; }

                // Component Type
                UnityEngine.Component compObj = fieldInfo.GetValue(obj) as UnityEngine.Component;
                if (compObj != null)
                {
                    sm_currentComp = compObj;
                    fieldData += PROPERTY_FORMAT(fieldInfo.Name, (fieldInfo.GetValue(obj) as Component).GetType() as object, indent);
                    continue;
                }

                // Default format
                fieldData += PROPERTY_FORMAT(fieldInfo.Name, fieldInfo.GetValue(obj), indent);
            }
        }
        return fieldData;
    }
    #endregion

    // ========================================================================================================================= //

    #region Formatting
    private static string PROPERTY_FORMAT(object obj, object value, int indent) { return "\n" + Indent(indent) + string.Format(PROPERTY, obj, value); }
    private static string ARRAY_FORMAT(object obj, int indent) { return "\n" + Indent(indent) + string.Format(ARRAY, obj); }
    private static string ELEMENT_FORMAT(object obj, string value, int indent) { return "\n" + Indent(indent) + string.Format(ELEMENT, obj, value); }
    private static string Indent(int indent)
    {
        if (indent == 0) { return ""; }
        string indentVal = string.Empty;
        for (int i = 0; i < indent; i++) { indentVal += "\t"; }
        return indentVal;
    }
    #endregion
}