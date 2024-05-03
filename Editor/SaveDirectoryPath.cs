#if UNITY_STANDALONE_WIN
using System;
using System.Windows.Forms;
using UnityEngine;

// Make a function to open the file dialog and see if it lets you select a folder or not (if it does. Set it up and look into mac and linux setup too)

namespace Visave
{
    public static class SaveDirectoryPath
    {
        private static FolderBrowserDialog sm_FBD;
        public static void OpenFBDWindow(ref string path)
        {
            if (sm_FBD == null)
            {
                sm_FBD = new FolderBrowserDialog();
                sm_FBD.Description = "Select the folder to save your game data:";
                sm_FBD.ShowNewFolderButton = false;
                sm_FBD.RootFolder = Environment.SpecialFolder.Desktop;
            }
            if (sm_FBD.ShowDialog() == DialogResult.OK)
            {
                path = sm_FBD.SelectedPath;
            }
        }
    }
}

#endif