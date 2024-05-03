using System;
using System.Globalization;
using UnityEngine;

public static class SerializeEngineType
{
    #region Serialization Methods
    public static T GetType<T>(object data)
    {
        return (T)data;
    }
    public static Vector4 QuaternionToVector4(Quaternion quat)
    {
        // Convert Quaternion to Vector4 - So that it can be displayed correctly in the editor
        return new Vector4(quat.x, quat.y, quat.z, quat.w);
    }
    #endregion

    // ========================================================================================================================= //

    #region Deserialization Methods
    public static object StringToType(string str, string val)
    {
        // Convert the 'val' into the correct data type for Deserializing data correctly
        switch (str)
        {
            case "Int32":
                return Int32.Parse(val);
            case "Single":
                return float.Parse(val, CultureInfo.InvariantCulture.NumberFormat);
            case "Boolean":
                return val == "True" ? true : false;
            case "Vector2":
                return StringToVector2(val);
            case "Vector3":
                return StringToVector3(val);
            case "Vector4":
                return StringToVector4(val);
            case "Quaternion":
                Vector4 vec4 = StringToVector4(val);
                return new Quaternion(vec4.x, vec4.y, vec4.z, vec4.w);
            case "LayerMask":
                return Int32.Parse(val);
            default:
                return val;
        }
    }
    public static Vector2 StringToVector2(string str)
    {
        string[] sArray = SplitVectorString(str);
        return new Vector2(float.Parse(sArray[0]), float.Parse(sArray[1]));
    }
    public static Vector3 StringToVector3(string str)
    {
        string[] sArray = SplitVectorString(str);
        return new Vector3(float.Parse(sArray[0]), float.Parse(sArray[1]), float.Parse(sArray[2]));
    }
    public static Vector4 StringToVector4(string str)
    {
        string[] sArray = SplitVectorString(str);
        return new Vector4(float.Parse(sArray[0]), float.Parse(sArray[1]), float.Parse(sArray[2]), float.Parse(sArray[3]));
    }
    public static string[] SplitVectorString(string str)
    {
        // Remove the parentheses from string
        if (str.StartsWith("(") && str.EndsWith(")"))
        {
            str = str.Substring(1, str.Length - 2);
        }
        // split the string into separate strings 
        return str.Split(',');
    }
    #endregion
}
