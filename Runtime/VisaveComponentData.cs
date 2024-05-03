using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// SaveComponentData stores the active components during game runtime & within the editor.
/// </summary>
/// <remarks>
/// When the serializer saves data, it will access this instance to save the selected properties of the component assigned.
/// For example, if a component is 'ticked' to be saved. Then it will save the component and its data
/// </remarks>

namespace Visave
{
    [Serializable]
    public sealed class VisaveComponentData
    {
        [HideInInspector] public string name;
        [ReadOnlyInspector] public Component m_componentType;
        [HideInInspector] public bool m_save;
        [SerializeField, HideInInspector] public List<int> m_saveToken = new();

        [NonSerialized] public bool m_addAllFields;
        public VisaveComponentData(Component newComp)
        {
            m_componentType = newComp;
            m_save = true;
            name = "Component: " + m_componentType.GetType().Name;
        }

        // ========================================================================================================================= //

        #region Methods
        public bool ContainsSave(int indexValue)
        {
            return m_saveToken.Contains(indexValue);
        }
        public void UpdateSaveState(bool newState)
        {
            // Return if state hasn't changed
            if (newState == m_save) { return; }
            // Update variables on whether all fields should be enabled or not
            m_save = newState;
            m_addAllFields = m_save;
            if (m_save == false)
            {
                m_saveToken.Clear();
            }
            /* Bug: (HIGH PRIORITY)
             *   - Loop all fields to add the index? - Atm the values are only added when unfolded, this needs Fixing!
             */
        }
        public void UpdateSaveListElement(bool newState, int indexValue)
        {
            // Check to add value to list (if its enabled in editor or whole component has been toggled)
            if (newState || m_addAllFields)
            {
                // Deal with new save state
                if (!m_saveToken.Contains(indexValue))
                {
                    m_saveToken.Add(indexValue);
                }
                else
                {
                    m_addAllFields = false;
                }
            }
            else
            {
                // Remove field indexValue from list
                m_saveToken.Remove(indexValue);
            }
            // Update save component state
            m_save = m_saveToken.Count == 0 ? false : true;
        }
        #endregion
    }
}