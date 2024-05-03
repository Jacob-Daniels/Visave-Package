using System;
using System.IO;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;
using System.Windows.Forms;
using UnityEditor;
using UnityEngine;
using UnityEngine.TextCore.Text;

/// <summary>
/// The SaveWindow displays all the data stored within the 'SaveManager'. It is a seperate unity window to allow data to be clearly shown to the user.
/// </summary>
/// <remarks>
/// The user can edit each save from within the editor and decide what data they want to save through selecting a series of booleans.
/// It creates an organised view to the player of all the saves for the selected save file.
/// NOTICE: All saves are assigned to a single file for now - Multifile saving will be implemented at a later date
/// </remarks>

namespace Visave
{
    public class SaveManagerCustomWindow : CustomWindowBase
    {
        #region Methods
        public static void Open(SaveManager saveObject)
        {
            //if (Application.isPlaying) { return; }

            // Opens the Custom Editor Window for the Save Manager
            SaveManagerCustomWindow window = GetWindow<SaveManagerCustomWindow>("Visave Manager");
            window.m_serializedObject = new SerializedObject(saveObject);
        }

        public void OnGUI()
        {
            if (m_serializedObject == null)
            {
                // Try find save manager in scene
                m_serializedObject = new SerializedObject(GameObject.FindObjectOfType<SaveManager>());
                if (m_serializedObject == null)
                {
                    // Prevent editor trying to read null values (create new save manager for scene - if editor is open) - Might be best to just advise the player to create their own instead of force creating one.
                    Debug.LogError("Unable to find Save Manager in scene. Creating a new game object for save manager");
                    m_serializedObject = new SerializedObject(new GameObject("Save Manager").AddComponent<SaveManager>());
                    return;
                }
            }
            m_serializedObject.Update();
            sm_saveManager = m_serializedObject.targetObject as SaveManager;

            #region Topbar
            EditorGUILayout.BeginVertical("Box", GUILayout.MaxHeight(100), GUILayout.ExpandHeight(true));
            EditorGUILayout.BeginHorizontal();
            // Create new profile button
            if (GUILayout.Button("Create New Profile", GUILayout.Height(40), GUILayout.MaxWidth(160)))
            {
                sm_saveManager.CreateNewProfile();
            }
            GUIStyle debugStyle = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleLeft, fontStyle = FontStyle.Bold, normal = { textColor = Color.cyan } };
            EditorGUILayout.BeginVertical();

            // Directory Fields
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Directory: ", GUILayout.MaxWidth(80));
            EditorGUI.BeginDisabledGroup(true);
            EditorGUILayout.LabelField(sm_saveManager.m_savePath, new GUIStyle(GUI.skin.textArea) { wordWrap = true });
            EditorGUI.EndDisabledGroup();
            // Button to open a file panel, to select the desired directory
            if (GUILayout.Button("Browse...", GUILayout.MaxWidth(80)))
            {
                // Setup OFD
                SaveDirectoryPath.OpenFBDWindow(ref sm_saveManager.m_savePath);
            }
            EditorGUILayout.EndHorizontal();
            m_openDebug = EditorGUILayout.Foldout(m_openDebug, "Developer Notes:");
            if (m_openDebug)
            {
                EditorGUILayout.HelpBox(
                    "DEV NOTE: Add a tooltip / clean note for user saying \"A Profile represents a single file.\"",
                    MessageType.Info);
                EditorGUILayout.HelpBox(
                    "DEV NOTE: Maybe add a button to open a popup or additional window for help / options for the save manager?.",
                    MessageType.Info);
                EditorGUILayout.HelpBox(
                    "Fix bug with save manager not keeping new data on Save project. (Currently resets to original value)",
                    MessageType.Error);
            }
            EditorGUILayout.EndVertical();
            EditorGUILayout.EndHorizontal();

            // List of profiles
            EditorGUILayout.BeginHorizontal();
            m_currentProperty = m_serializedObject.FindProperty("m_profiles");
            AddProfilesToTopbar(m_currentProperty);
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndVertical();

            if (m_selectedProfileIndex == -1)
            {
                GUILayout.Label("\n\nSelect a profile above.", new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Bold, fontSize = 20 } );
                ApplyChanges();
                return;
            }

            // Selected profile details
            EditorGUILayout.BeginHorizontal();
            GUIStyle labelStyle = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleRight, fontStyle = FontStyle.BoldAndItalic, normal = { textColor = Color.yellow } };
            GUILayout.Label("Profile Name", labelStyle, GUILayout.MaxWidth(150));
            sm_saveManager.GetProfiles()[m_selectedProfileIndex].m_profileName = EditorGUILayout.TextField(sm_saveManager.GetProfiles()[m_selectedProfileIndex].m_profileName, GUILayout.MaxWidth(150));
            EditorGUILayout.EndHorizontal();
            #endregion

            EditorGUILayout.BeginHorizontal();
            #region Sidebar
            // Panel to display a list of all save properties
            EditorGUILayout.BeginVertical("Box", GUILayout.MaxWidth(150), GUILayout.ExpandHeight(true));

            // Create new save
            if (GUILayout.Button("Create new save", GUILayout.Height(40)))
            {
                sm_saveManager.GetProfiles()[m_selectedProfileIndex].AddProfileSave(new VisaveInstance("New Save " + sm_saveManager.GetProfiles()[m_selectedProfileIndex].GetProfileSaves().Count));
            }

            // Find selected profiles save list
            m_currentProperty = m_selectedProfileProp.FindPropertyRelative("m_profileSaves");
            AddSaveInstancesToSidebar(m_currentProperty);
            EditorGUILayout.EndVertical();
            #endregion

            #region Selected Property Data Display
            // Panel to display the selected objects properties
            EditorGUILayout.BeginVertical("Box", GUILayout.ExpandHeight(true));
            if (m_selectedSaveProperty != null)
            {
                // Draw properties of selected save instance
                DrawProperties(m_selectedSaveProperty, true, m_selectedSaveIndex);
            }
            else
            {
                EditorGUILayout.LabelField("Select a save.", new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Bold, fontSize = 15 }, GUILayout.MaxHeight(50));
            }

            EditorGUILayout.EndVertical();
            #endregion
            EditorGUILayout.EndHorizontal();

            ApplyChanges();
        }
        #endregion
    }
}