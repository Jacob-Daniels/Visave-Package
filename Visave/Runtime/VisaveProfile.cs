using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;
using Visave;

namespace Visave
{
    [Serializable]
    public sealed class VisaveProfile
    {
        #region Members
        public string m_profileName;
        public List<VisaveInstance> m_profileSaves;

        public VisaveProfile(int index)
        {
            m_profileName = "Profile: " + index;
            m_profileSaves = new List<VisaveInstance>() { new("New Save 0") };
        }
        #endregion

        // ========================================================================================================================= //

        #region Getters & Setters
        public List<VisaveInstance> GetProfileSaves() { return m_profileSaves; }
        public void AddProfileSave(VisaveInstance newSave) { m_profileSaves.Add(newSave); }
        #endregion

        // ========================================================================================================================= //

        #region  Methods

        public VisaveInstance FindVisaveInstanceByName(string _name)
        {
            foreach (VisaveInstance visaveInstance in m_profileSaves)
            {
                if (visaveInstance.name == _name) return visaveInstance;
            }
            return null;
        }
        
        #endregion
    }
}