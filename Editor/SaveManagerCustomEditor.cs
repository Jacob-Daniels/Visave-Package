using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using System.Drawing.Design;
using UnityEditor.SceneManagement;

/// <summary>
/// The SaveManagerEditor is a simple script that tidies up the 'SaveManager' data within the inspector.
/// </summary>
/// <remarks>
/// It allows the user to open the Save Window from within the 'SaveManager' inspector
/// </remarks>

namespace Visave
{
    [CustomEditor(typeof(SaveManager))]
    public class SaveManagerCustomEditor : Editor
    {
        #region Methods
        public override void OnInspectorGUI()
        {
            SaveManager saveManager = (SaveManager)target;

            // Create a button to open custom window
            if (GUILayout.Button("Open Save Manager", GUILayout.Height(50)))
            {
                SaveManagerCustomWindow.Open(saveManager);
            }

            GUILayout.Space(40);
            GUILayout.BeginHorizontal();
            GUILayout.Label("File Name:", GUILayout.MaxWidth(80));
            saveManager.m_fileName = GUILayout.TextField(saveManager.m_fileName, GUILayout.MinWidth(50));
            GUILayout.EndHorizontal();

            GUILayout.Space(20);
            // Save Data
            /*
            if (GUILayout.Button("Save", GUILayout.Height(50)))
            {
                Debug.Log("BUTTON IS NOT LINKED TO A 'SAVE()' FUNCTION");
                // Update save list in editor
                saveManager.m_foundProfiles = Serializer.GetAllSavesFromDirectory();
            }
            // Load Data
            if (GUILayout.Button("Load", GUILayout.Height(50)))
            {
                Debug.Log("BUTTON IS NOT LINKED TO A 'LOAD()' FUNCTION");
            }
            */
            GUILayout.Space(20);

            saveManager.m_saveType = (SaveManager.SerializeType)EditorGUILayout.EnumPopup(new GUIContent("Save Format", "Set the data format for serializing objects"), saveManager.m_saveType);
            // Error message for JSON (As it currently doesn't work / isn't setup for the visave tool)
            if (saveManager.m_saveType == SaveManager.SerializeType.JSON)
            {
                EditorGUILayout.HelpBox("JSON is a WIP. Please select 'visave'", MessageType.Error);
            }
            GUILayout.Space(10);

            EditorGUILayout.LabelField("Save path: " + saveManager.m_savePath);
            GUILayout.Space(10);

            // List profiles stored in save manager
            GUILayout.Label("All component data components:", EditorStyles.boldLabel);
            GUILayout.BeginVertical();
            foreach (VisaveProfile saveProfile in saveManager.m_profiles)
            {
                // List save instances of profile
                foreach (VisaveInstance saveInstance in saveProfile.m_profileSaves)
                {
                    // List component data of save instance
                    foreach (VisaveComponentData compData in saveInstance.m_components)
                    {
                        GUILayout.Label(compData.name);
                        // List save list of component
                        string values = string.Empty;
                        foreach (int saveValue in compData.m_saveToken)
                        {
                            if (string.IsNullOrEmpty(values))
                            {
                                values += saveValue;
                                continue;
                            }
                            values += ", " + saveValue;
                        }
                        if (string.IsNullOrEmpty(values)) { values = "Null!"; }
                        GUILayout.Label(values);
                    }
                }
            }
            GUILayout.EndVertical();


            // List of profiles in directory
            GUILayout.Label("\nCurrent Profiles:", EditorStyles.boldLabel);
            GUILayout.BeginVertical();
            foreach (string save in saveManager.m_foundProfiles)
            {
                GUILayout.Label(save);
            }
            GUILayout.EndVertical();
        }
        #endregion
    }
}