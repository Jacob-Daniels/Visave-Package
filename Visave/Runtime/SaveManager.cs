using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using UnityEngine;

/// <summary>
/// The SaveManager is the only script that needs to be assigned within the game hierarchy.
/// </summary>
/// <remarks>
/// It ensures the user has a 'SaveManager' within their scene as it is the only access point to the 'Save Window' (Which allows the user to edit saves).
/// </remarks>

namespace Visave
{
    [System.Serializable]
    public sealed class SaveManager : MonoBehaviour
    {
        #region Members
        public string m_fileName = "Profile";
        public List<VisaveProfile> m_profiles = new();
        public SerializeType m_saveType;
        public string m_savePath;
        public enum SerializeType
        {
            JSON,
            Visave
        }
#if UNITY_EDITOR
        public List<string> m_foundProfiles = new();
#endif
        #endregion

        // ========================================================================================================================= //

        #region Getters & Setters
        public List<VisaveProfile> GetProfiles() { return m_profiles; }
        public void CreateNewProfile() { m_profiles.Add(new VisaveProfile(m_profiles.Count)); }
        #endregion

        // ========================================================================================================================= //

        #region Game Loop
        private void Update()
        {
            // TEST CODE
            if (Input.GetKeyDown(KeyCode.R))
            {
                // Check Serialization type and save data
                if (m_saveType == SerializeType.JSON)
                {
                    // JSON Format
                    var jsonText = JsonUtility.ToJson(m_profiles[0].GetProfileSaves()[0]);
                    Save(m_fileName, jsonText);
                    Debug.Log("JSON SAVE");
                }
                else
                {
                    Save(m_fileName, Serializer.ToVisave(m_profiles[0].GetProfileSaves()[0], m_profiles[0].GetProfileSaves()[0].GetType()));
                    Debug.Log("VISAVE SAVE");
                }
                Debug.Log("R Pressed - SAVED");
            }
            else if (Input.GetKeyDown(KeyCode.T))
            {
                if (m_saveType == SerializeType.JSON)
                {
                    // JSON Format
                    Load(m_fileName);
                    Debug.Log("Load JSON");
                }
                else
                {
                    ViLoad(m_fileName);
                    Debug.Log("Load VISAVE");
                }
                Debug.Log("T Pressed - LOADED");
            }
        }
        #endregion

        // ========================================================================================================================= //

        #region Serialization
        public void Save(string fileName = "DefaultProfile", string saveData = "", bool overwrite = true)
        {
            // Check the file type, depending on the serialization type
            string savePath = m_savePath + Path.AltDirectorySeparatorChar + fileName;
            //string savePath = Serializer.SAVE_FOLDER_PATH + Path.AltDirectorySeparatorChar + fileName;
            savePath += m_saveType == SerializeType.JSON ? Serializer.JSON_FILE_TYPE : Serializer.VISAVE_FILE_TYPE;

            #region Check data is valid to save
            // Is string empty
            if (saveData == string.Empty) { Debug.LogWarning("Warning: Unable to save profile as it has no data!"); return; }

            // Is fileName valid - Only allows letters, numbers, underscore and dash in file name
            if (!Regex.IsMatch(fileName, @"^[a-zA-Z0-9]+$")) { Debug.LogWarning("Warning: File name has invalid characters!\nCan only contain the following: a-z, A-Z, 0-9"); return; }

            // Check whether a save instance exists with the name and that it can't be overwritten
            if (File.Exists(savePath) && !overwrite) { Debug.LogWarning("Warning: Unable to overwrite the file - " + fileName + "\nPass in the correct parameter to overwrite files!"); return; }
            #endregion

            // Create writer to savePath
            using StreamWriter writer = new StreamWriter(savePath);

            writer.Write(saveData);

            if (m_saveType == SerializeType.JSON)
            {
                Debug.Log("Saved JSON data to: " + savePath);
            }
            else
            {
                Debug.Log("Saved Visave data to: " + savePath);
            }
        }
        public void ViLoad(string fileName)
        {
            VisaveInstance loadedSave = Serializer.FromVisave(fileName, m_savePath);
            if (loadedSave != null)
            {
                Debug.Log("ViLoad Complete: " + fileName);
                Debug.Log("Save Instance name: " + loadedSave.name);
            } else
            {
                Debug.Log("ViLoad failed: " + fileName + " is null!");
            }
        }
        public void Load(string fileName)
        {
            // Check string is valid
            if (string.IsNullOrEmpty(fileName)) { return; }
            // Read file

            VisaveInstance loadedSave = JsonUtility.FromJson<VisaveInstance>(Serializer.LoadJSONFile(fileName));
            if (loadedSave != null)
            {
                Debug.Log("ViLoad Complete: " + fileName);
                Debug.Log("Save Instance name: " + loadedSave.name);
                // Loop saves to file new save
                GameObject foundObject = GameObject.Find(loadedSave.m_saveInstance.name);
                if (foundObject == null)
                {
                    Debug.Log("CREATED NEW OBJECT");
                    foundObject = Instantiate(loadedSave.m_saveInstance);
                    // Set properties of object below
                    // NOT IMPLEMENTED
                }

                foreach (VisaveComponentData data in loadedSave.m_components)
                {
                    if (data.m_componentType.GetType() == typeof(Transform))
                    {
                        Debug.Log("Old object: " + loadedSave.m_saveInstance.transform.position);
                        Debug.Log("Transform found on object: " + foundObject.name);
                        Debug.Log("Object position: " + foundObject.transform.position);
                        Debug.Log("Saved position: " + ((Transform)data.m_componentType).position);
                        foundObject.transform.position = ((Transform)data.m_componentType).position;
                        foundObject.transform.rotation = ((Transform)data.m_componentType).rotation;
                        foundObject.transform.localScale = ((Transform)data.m_componentType).localScale;
                    }
                    Debug.Log("LOOPING COMPS");

                }

                Debug.Log("Transform Component Type: " + typeof(Transform));

                // Will eventually need a check for the profile and the saves in the profile.
                // Loading data should overwrite the in game object, not the saves/profiles as these will be custom to the runtime data.
            }
            else
            {
                Debug.Log("ViLoad failed: " + fileName + " is null!");
            }
        }
        #endregion
    }
}