using System;
using Game.UI.Buttons;
using Game.UI.Windows.General;
using TMPro;
using UnityEngine;

namespace Game.UI.Windows.WinWindow
{
    public class WinWindow : Window<WinWindowModel>
    {
        [SerializeField] private GameButton _continueButton;
        [SerializeField] private TMP_Text _levelTmp;
        [SerializeField] private string _levelFormat;
        [SerializeField, Tooltip("Completed search time, formatted as minutes and seconds.")]
        private TMP_Text _timeTmp;
        [SerializeField, Tooltip("Shown when the completed attempt did not require additional time.")]
        private GameObject _timeLike;
        private Action _continueAction;

        /// <summary>Shows search time and requires explicit continuation before chapter victory or the next level.</summary>
        protected override void Setup(WinWindowModel model)
        {
            GetComponent<Game.UI.Windows.Controllers.WindowController>().SetCanCloseByReturn(false);
            _timeTmp.text = model.Statistics?.FormattedTime ?? string.Empty;
            _timeLike.SetActive(model.Statistics?.TimeLike == true);
            _continueAction = model.ContinueAction;
            _levelTmp.text = string.Format(_levelFormat, model.CurrentLevel);
            _continueButton.ClickEvent -= Continue;
            _continueButton.ClickEvent += Continue;
        }

        /// <summary>Releases the view's button subscription.</summary>
        private void OnDestroy()
        {
            _continueButton.ClickEvent -= Continue;
        }

        /// <summary>Accepts continuation once; rewards belong exclusively to the chapter system.</summary>
        private void Continue()
        {
            _continueButton.SetInteractable(false);
            _continueAction?.Invoke();
        }
    }
}
