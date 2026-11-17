using TMPro;
using UnityEngine;

namespace Game.UI.Buttons
{
    public class GameButtonWithText : GameButton
    {
        [SerializeField] private TMP_Text buttonText;


        public void SetText(string text)
        {
            buttonText.text = text;
        }
    }
}