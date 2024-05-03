using System;
using System.Linq;
using System.Reflection;
using Unity.VisualScripting;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

/// <summary>
/// SaveDataWindow is the base class for the main user interface of the save system.
/// </summary>
/// <remarks>
/// This is the base class in managing how data is displayed to the user from within the custom editor.
/// It stores all the data for serialized objects and the active 'SaveManager' from within the open scene.
/// </remarks>

namespace Visave
{
    public class CustomWindowBase : EditorWindow
    {
        #region Members
        protected SaveManager sm_saveManager;
        protected SerializedObject m_serializedObject;
        protected SerializedProperty m_currentProperty;

        protected string m_selectedSavePropertyPath;
        protected SerializedProperty m_selectedSaveProperty;
        protected string m_selectedProfilePropPath;
        protected SerializedProperty m_selectedProfileProp;

        protected int m_selectedSaveIndex;
        protected int m_selectedProfileIndex = -1;
        // Temp variable for Developer notes in editor
        protected bool m_openDebug = false;
        #endregion

        // ========================================================================================================================= //

        #region Property Display
        protected void DrawProperties(SerializedProperty property, bool drawChildren, int index)
        {
            // Initial property is of VisaveInstance
            int compIndex = 0;
            string lastPropPath = string.Empty;
            
            foreach (SerializedProperty prop in property)
            {
                ApplyChanges();
                PopulateComponentList(prop, index);

                // Recurse through properties if current property is generic (A list)
                if (prop.isArray && prop.propertyType == SerializedPropertyType.Generic)
                {
                    EditorGUILayout.BeginHorizontal();
                    prop.isExpanded = EditorGUILayout.Foldout(prop.isExpanded, prop.displayName);
                    EditorGUILayout.EndHorizontal();
                    // Slider for editor
                    sm_saveManager.GetProfiles()[m_selectedProfileIndex].GetProfileSaves()[index].m_scrollPosition = EditorGUILayout.BeginScrollView(sm_saveManager.GetProfiles()[m_selectedProfileIndex].GetProfileSaves()[index].m_scrollPosition);

                    // Display properties if foldout is expanded
                    if (prop.isExpanded)
                    {
                        ApplyChanges(); // Prevent errors in editor
                        EditorGUI.indentLevel++;
                        // Draw properties for each component data instance
                        DrawProperties(prop, drawChildren, index);
                        EditorGUI.indentLevel--;
                    }
                    lastPropPath = prop.propertyPath;
                    EditorGUILayout.EndScrollView();
                }
                else
                {
                    if (!string.IsNullOrEmpty(lastPropPath) && prop.propertyPath.Contains(lastPropPath)) { continue; }
                    lastPropPath = prop.propertyPath;
                    CheckData(prop, drawChildren, index, ref compIndex);
                }
            }
        }

        // ========================================================================================================================= //

        #region Format Data

        private void CheckData(SerializedProperty property, bool drawChildren, int index, ref int compIndex)
        {
            // Format how the data is displayed within the editor
            if (property.name == "data")
            {
                ApplyChanges(); // Prevent errors in editor
                GameObject obj = sm_saveManager.GetProfiles()[m_selectedProfileIndex].GetProfileSaves()[index].m_saveInstance;
                if (obj.GetComponents<Component>().Length <= compIndex) { compIndex = 0; }  // Fix error when repainting new object components with a higher or lower index
                var component = obj.GetComponents<Component>()[compIndex];
                if (component == null) { return; }
                
                // Create a foldout for component data
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.PropertyField(property, true, GUILayout.MinWidth(400));
                // Toggle to save
                sm_saveManager.GetProfiles()[m_selectedProfileIndex].GetProfileSaves()[index].GetComponents()[compIndex]
                    .UpdateSaveState(EditorGUILayout.Toggle("Save Component:", sm_saveManager.GetProfiles()[m_selectedProfileIndex].GetProfileSaves()[index].GetComponents()[compIndex].m_save));
                EditorGUILayout.EndHorizontal();

                // If foldout is closed, then return
                if (!property.isExpanded) { compIndex++;  return; }

                EditorGUI.indentLevel++; EditorGUI.indentLevel++;

                // Read and loop the component field data
                BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
                MemberInfo[] memberArray = component.GetType().GetFields(flags).Cast<MemberInfo>().Concat(component.GetType().GetProperties(flags)).ToArray();
                bool loopedFields = false; int slIndex = 0;
                EditorGUIUtility.labelWidth = 220;
                foreach (MemberInfo memberInfo in memberArray)
                {
                    // Disable specific fields in editor
                    if (memberInfo.Name == "parent" || memberInfo.Name == "parentInternal" || memberInfo.Name == "sharedMaterial") { continue; }
                    slIndex++;

                    // Field data
                    if (memberInfo.MemberType == MemberTypes.Field)
                    {
                        FieldInfo fieldInfo = (FieldInfo)memberInfo;
                        if (fieldInfo.CanWrite())
                        {
                            EditorGUILayout.BeginHorizontal();
                            EditorGUI.BeginDisabledGroup(true);
                            // Check data type is an Enum
                            if (fieldInfo.FieldType.IsEnum)
                            {
                                EditorGUILayout.EnumPopup(fieldInfo.Name,
                                    (Enum)fieldInfo.GetValue(component), GUILayout.MaxWidth(400));
                                EditorGUI.EndDisabledGroup();
                                sm_saveManager.GetProfiles()[m_selectedProfileIndex].GetProfileSaves()[index].GetComponents()[compIndex].UpdateSaveListElement(
                                    EditorGUILayout.Toggle("Save " + fieldInfo.Name, sm_saveManager.GetProfiles()[m_selectedProfileIndex].GetProfileSaves()[index].GetComponents()[compIndex].ContainsSave(slIndex)), slIndex);
                                EditorGUILayout.EndHorizontal();
                                continue;
                            }

                            // Check component data type to display it correctly
                            switch (fieldInfo.GetValue(component).GetType().Name)
                            {
                                case "Int32":
                                    EditorGUILayout.IntField(fieldInfo.Name,
                                        (int)fieldInfo.GetValue(component), GUILayout.MaxWidth(400));
                                    break;
                                case "Single":
                                    EditorGUILayout.FloatField(fieldInfo.Name,
                                        (float)fieldInfo.GetValue(component), GUILayout.MaxWidth(400));
                                    break;
                                case "Boolean":
                                    EditorGUILayout.Toggle(fieldInfo.Name,
                                        (bool)fieldInfo.GetValue(component), GUILayout.MaxWidth(400));
                                    break;
                                case "String":
                                    EditorGUILayout.TextField(fieldInfo.Name,
                                        (string)fieldInfo.GetValue(component), GUILayout.MaxWidth(400));
                                    break;
                                case "Vector2":
                                    EditorGUILayout.Vector2Field(fieldInfo.Name,
                                        (Vector2)fieldInfo.GetValue(component), GUILayout.MaxWidth(400));
                                    break;
                                case "Vector3":
                                    // Display property info
                                    EditorGUILayout.Vector3Field(fieldInfo.Name,
                                        (Vector3)fieldInfo.GetValue(component), GUILayout.MaxWidth(400));
                                    break;
                                case "Vector4":
                                    EditorGUILayout.Vector4Field(fieldInfo.Name,
                                        (Vector4)fieldInfo.GetValue(component), GUILayout.MaxWidth(400));
                                    break;
                                case "Quaternion":
                                    EditorGUILayout.Vector4Field(fieldInfo.Name,
                                        SerializeEngineType.QuaternionToVector4(
                                            (Quaternion)fieldInfo.GetValue(component)), GUILayout.MaxWidth(400));
                                    break;
                                default:
                                    EditorGUI.EndDisabledGroup();
                                    EditorGUILayout.LabelField("Unable to format:  " + memberInfo.Name + "\t| Type: " + fieldInfo.GetValue(component).GetType().Name, new GUIStyle() { fontStyle = FontStyle.Bold, normal = { textColor = Color.black }}, GUILayout.MaxWidth(400));
                                    EditorGUI.BeginDisabledGroup(true);
                                    break;
                            }

                            EditorGUI.EndDisabledGroup();
                            // Display boolean button to save property
                            sm_saveManager.GetProfiles()[m_selectedProfileIndex].GetProfileSaves()[index].GetComponents()[compIndex].UpdateSaveListElement(
                                EditorGUILayout.Toggle("Save " + fieldInfo.Name, sm_saveManager.GetProfiles()[m_selectedProfileIndex].GetProfileSaves()[index].GetComponents()[compIndex].ContainsSave(slIndex)), slIndex);
                            EditorGUILayout.EndHorizontal();
                            loopedFields = true;
                            continue;
                        }
                    }
                    // Property Data
                    if (memberInfo.MemberType == MemberTypes.Property)
                    {
                        if (loopedFields) { break; }

                        // Check to clear list (Prevent memory leak)
                        int saveListSize = sm_saveManager.GetProfiles()[m_selectedProfileIndex].GetProfileSaves()[index].GetComponents()[compIndex].m_saveToken.Count();
                        if (saveListSize > memberArray.Length)
                        {
                            sm_saveManager.GetProfiles()[m_selectedProfileIndex].GetProfileSaves()[index].GetComponents()[compIndex].m_saveToken.Clear();
                        }
                        // Populate the save list
                        if (saveListSize < memberArray.Length)
                        {
                            //sm_saveManager.GetProfiles()[m_selectedProfileIndex].GetProfileSaves()[index].GetComponents()[compIndex].PopulateSaveList(memberArray.Length);
                        }

                        // Display property info that is writable
                        PropertyInfo propertyInfo = (PropertyInfo)memberInfo;
                        if (propertyInfo.CanWrite)
                        {
                            // Check properties with attributes (serializable)
                            EditorGUILayout.BeginHorizontal();
                            EditorGUI.BeginDisabledGroup(true);
                            // Check data type is an Enum
                            if (propertyInfo.PropertyType.IsEnum)
                            {
                                EditorGUILayout.EnumPopup(propertyInfo.Name, (Enum)propertyInfo.GetValue(component), GUILayout.MaxWidth(400));
                                EditorGUI.EndDisabledGroup();
                                sm_saveManager.GetProfiles()[m_selectedProfileIndex].GetProfileSaves()[index].GetComponents()[compIndex].UpdateSaveListElement(
                                    EditorGUILayout.Toggle("Save " + propertyInfo.Name, sm_saveManager.GetProfiles()[m_selectedProfileIndex].GetProfileSaves()[index].GetComponents()[compIndex].ContainsSave(slIndex)), slIndex);
                                EditorGUILayout.EndHorizontal();
                                continue;
                            }
                            
                            // Check component data type to display it correctly
                            switch (propertyInfo.GetValue(component).GetType().Name)
                            {
                                case "Int32":
                                    EditorGUILayout.IntField(propertyInfo.Name, (int)propertyInfo.GetValue(component), GUILayout.MaxWidth(400));
                                    break;
                                case "Single":
                                    EditorGUILayout.FloatField(propertyInfo.Name, (float)propertyInfo.GetValue(component), GUILayout.MaxWidth(400));
                                    break;
                                case "Boolean":
                                    EditorGUILayout.Toggle(propertyInfo.Name, (bool)propertyInfo.GetValue(component), GUILayout.MaxWidth(400));
                                    break;
                                case "String":
                                    EditorGUILayout.TextField(propertyInfo.Name, (string)propertyInfo.GetValue(component), GUILayout.MaxWidth(400));
                                    break;
                                case "Vector2":
                                    EditorGUILayout.Vector2Field(propertyInfo.Name, (Vector2)propertyInfo.GetValue(component), GUILayout.MaxWidth(400));
                                    break;
                                case "Vector3":
                                    // Display property info
                                    EditorGUILayout.Vector3Field(propertyInfo.Name, (Vector3)propertyInfo.GetValue(component), GUILayout.MaxWidth(400));
                                    break;
                                case "Vector4":
                                    EditorGUILayout.Vector4Field(propertyInfo.Name, (Vector4)propertyInfo.GetValue(component), GUILayout.MaxWidth(400));
                                    break;
                                case "Quaternion":
                                    EditorGUILayout.Vector4Field(propertyInfo.Name, SerializeEngineType.QuaternionToVector4((Quaternion)propertyInfo.GetValue(component)), GUILayout.MaxWidth(400));
                                    break;
                                case "LayerMask":
                                    EditorGUILayout.MaskField(propertyInfo.Name, InternalEditorUtility.LayerMaskToConcatenatedLayersMask((LayerMask)propertyInfo.GetValue(component)), InternalEditorUtility.layers);
                                    break;
                                default:
                                    EditorGUILayout.LabelField("Unable to format:  " + memberInfo.Name + "\t| Type: " + propertyInfo.GetValue(component).GetType().Name, new GUIStyle() { normal = { textColor = Color.red } }, GUILayout.MaxWidth(400));
                                    break;
                            }
                            EditorGUI.EndDisabledGroup();
                            // Display boolean button to save property
                            sm_saveManager.GetProfiles()[m_selectedProfileIndex].GetProfileSaves()[index].GetComponents()[compIndex].UpdateSaveListElement(
                                EditorGUILayout.Toggle("Save " + propertyInfo.Name, sm_saveManager.GetProfiles()[m_selectedProfileIndex].GetProfileSaves()[index].GetComponents()[compIndex].ContainsSave(slIndex)), slIndex);
                            EditorGUILayout.EndHorizontal();
                        }
                    }
                }
                EditorGUI.indentLevel--; EditorGUI.indentLevel--;
                compIndex++;
            }
            else
            {
                // Display default property field
                EditorGUILayout.PropertyField(property, drawChildren);
            }
        }
        private void PopulateComponentList(SerializedProperty property, int index)
        {
            // Check property is a gameobject - Populate component list
            if (property.name == "m_saveInstance")
            {
                // Add components to the list
                GameObject gameObj = sm_saveManager.GetProfiles()[m_selectedProfileIndex].GetProfileSaves()[index].m_saveInstance;
                if (gameObj != null)
                {
                    Component[] comps = gameObj.GetComponents<Component>();
                    // Check to clear list if a new object has been added
                    sm_saveManager.GetProfiles()[m_selectedProfileIndex].GetProfileSaves()[index].CheckToResetComponentList(gameObj);
                    // Update the component list (encase any were added or removed from the object in the scene)
                    sm_saveManager.GetProfiles()[m_selectedProfileIndex].GetProfileSaves()[index].UpdateComponentData(comps);
                }
                else
                {
                    // Reset the component list if object is null
                    sm_saveManager.GetProfiles()[m_selectedProfileIndex].GetProfileSaves()[index].ClearComponentList();
                }
            }
        }
        #endregion

        // ========================================================================================================================= //

        #region Topbar & Sidebar
        protected void AddProfilesToTopbar(SerializedProperty property)
        {
            // Loop profiles and display them within the top bar
            for (int i = 0; i < property.arraySize; i++)
            {
                if (GUILayout.Button(property.GetArrayElementAtIndex(i).displayName, GUILayout.Height(40), GUILayout.MaxWidth(80)))
                {
                    m_selectedProfilePropPath = property.GetArrayElementAtIndex(i).propertyPath;
                    m_selectedProfileIndex = i;
                    // Check for right click
                    if (Event.current.button == 1)
                    {
                        GenericMenu dropMenu = new GenericMenu();
                        dropMenu.AddItem(new GUIContent("Delete Save"), false, delegate { DeleteProfile(m_selectedProfileIndex); });
                        dropMenu.ShowAsContext();
                    }
                    // Deselect input field (prevents error with string not updating)
                    GUI.FocusControl(null);
                    // Reset the selected save - Prevents displaying data from another profile
                        m_selectedSaveProperty = null;
                        m_selectedSavePropertyPath = string.Empty;
                }
            }
            // Set the selected element
            if (!string.IsNullOrEmpty(m_selectedProfilePropPath))
            {
                m_selectedProfileProp = m_serializedObject.FindProperty(m_selectedProfilePropPath);
            }
        }
        
        protected void AddSaveInstancesToSidebar(SerializedProperty property)
        {
            // Loop profile saves and display them within the side bar
            for (int i = 0; i < property.arraySize; i++)
            {
                if (GUILayout.Button(property.GetArrayElementAtIndex(i).displayName, GUILayout.Height(25)))
                {
                    m_selectedSavePropertyPath = property.GetArrayElementAtIndex(i).propertyPath;
                    m_selectedSaveIndex = i;
                    // Check for right click
                    if (Event.current.button == 1)
                    {
                        GenericMenu dropMenu = new GenericMenu();
                        dropMenu.AddItem(new GUIContent("Delete Save"), false, delegate { DeleteProfileSave(m_selectedSaveIndex); });
                        dropMenu.ShowAsContext();
                    }

                    // Deselect input field (prevents error with string not updating)
                    GUI.FocusControl(null);
                }
            }

            // Set the selected element
            if (!string.IsNullOrEmpty(m_selectedSavePropertyPath))
            {
                m_selectedSaveProperty = m_serializedObject.FindProperty(m_selectedSavePropertyPath);
            }
        }
        #endregion

        // ========================================================================================================================= //
        public void DeleteProfile(int index)
        {
            // Delete profile by index
            if (index != -1)
            {
                // Remove save from list
                sm_saveManager.GetProfiles().RemoveAt(m_selectedProfileIndex);
                // Reset selected data
                m_selectedProfileProp = null;
                m_selectedProfilePropPath = string.Empty;
                m_selectedProfileIndex = -1;
            }
        }
        public void DeleteProfileSave(int index)
        {
            // Delete profile save by index
            if (index != -1)
            {
                sm_saveManager.GetProfiles()[m_selectedProfileIndex].GetProfileSaves().RemoveAt(m_selectedSaveIndex);
                // Reset selected data
                m_selectedSaveProperty = null;
                m_selectedSavePropertyPath = string.Empty;
                m_selectedSaveIndex = -1;
            }
        }
        protected void ApplyChanges()
        {
            m_serializedObject.ApplyModifiedProperties();
            m_serializedObject.Update();
        }
        #endregion
    }
}