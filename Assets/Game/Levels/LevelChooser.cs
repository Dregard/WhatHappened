using UnityEngine;

namespace Game.Levels
{
    public class LevelChooser : MonoBehaviour
    {
        [SerializeField, HideInInspector] 
        private GameObject _currentLevelInstance;
        
        [SerializeField, HideInInspector]
        private string _selectedLevelAddress;

        public GameObject CurrentLevelInstance
        {
            get => _currentLevelInstance;
            set => _currentLevelInstance = value;
        }

        public string SelectedLevelAddress
        {
            get => _selectedLevelAddress;
            set => _selectedLevelAddress = value;
        }
    }
}