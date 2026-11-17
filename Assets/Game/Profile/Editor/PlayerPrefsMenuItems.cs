using UnityEditor;
using UnityEngine;

namespace Game.Profile.Editor
{
    public static class PlayerPrefsMenuItems
    {
        [MenuItem("Game/PlayerPrefs/Delete")]
        public static void ClearPlayerPrefs()
        {
            PlayerPrefs.DeleteAll();
        }
    }
}