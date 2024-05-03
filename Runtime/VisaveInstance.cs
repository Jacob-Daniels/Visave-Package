using System.Collections.Generic;
using UnityEngine;
using System;
using System.Linq;
using UnityEditor.SceneManagement;

/// <summary>
/// SaveData class is a single save object within a save file.
/// </summary>
/// <remarks>
/// It allows the user to save a customisable object. This is done by selecting the desired components & its data from within the editor.
/// </remarks>


namespace Visave
{
    [Serializable]
    public sealed class VisaveInstance
    {
        public VisaveInstance(string _name) { name = _name; }

        // ========================================================================================================================= //

        #region Members
        // public properties
        public string name;
        public GameObject m_saveInstance;
        public List<VisaveComponentData> m_components = new();

        // Private properties
        private GameObject m_previousSaveInstance;
        private List<IPayload> m_payloads = new();  // For SaveInstance class

#if UNITY_EDITOR
        [NonSerialized] public Vector2 m_scrollPosition;
#endif
    #endregion

        // ========================================================================================================================= //

        #region Methods
        public List<VisaveComponentData> GetComponents() { return m_components; }
        public void UpdateComponentData(Component[] components)
        {
            // Loop new components from new object passed in
            // Remove old data
            foreach (VisaveComponentData data in m_components)
            {
                bool found = false;
                foreach (Component comp in components)
                {
                    if (data.m_componentType == comp)
                    {
                        found = true; break;
                    }
                }
                // Remove the data not found
                if (!found)
                {
                    m_components.Remove(data);
                }
            }

            // Add new data
            foreach (Component comp in components)
            {
                bool found = false;
                foreach (VisaveComponentData data in m_components)
                {
                    if (data.m_componentType == comp) {found = true; break; }
                }

                if (!found)
                {
                    // Add a new component
                    m_components.Add(new VisaveComponentData(comp));
                }
            }
        }
        public void CheckToResetComponentList(GameObject obj)
        {
            // Check previous object is different to new/current
            if (m_previousSaveInstance == null) { m_previousSaveInstance = obj; return; }   // Check for entering play mode - prevents resetting all save data
            if (obj != m_previousSaveInstance)
            {
                m_previousSaveInstance = obj;
                ClearComponentList();
            }
        }
        public void ClearComponentList()
        {
            if (m_components == null) { m_components = new(); }
            m_components.Clear();
        }

        public VisaveComponentData FindComponentData(string name)
        {
            foreach (VisaveComponentData data in m_components)
            {
                if (data.name == name) { return data; }
            }
            return null;
        }
        #endregion

        // ========================================================================================================================= //

        #region Save Methods
        private void CreatePayload()
        {
            // Create payload of variables that need to be saved
            if (m_payloads == null) { m_payloads = new(); } else { m_payloads.Clear(); }
            m_payloads.Add(new Payload<string>(name));
            m_payloads.Add(new Payload<GameObject>(m_saveInstance));
        }
        #endregion
    }
}