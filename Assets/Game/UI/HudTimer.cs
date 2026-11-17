using System;
using TMPro;
using UnityEngine;

namespace Game.UI
{
    public class HudTimer : MonoBehaviour
    {
        [SerializeField] private TMP_Text _timeTmp;
        

        public void SetTime(float time)
        {
            var timeSpan = TimeSpan.FromSeconds(time);
            _timeTmp.text = timeSpan.ToString(@"mm\:ss");
        }

        public void ResetTimer()
        {
            SetTime(0);
        }
    }
}
