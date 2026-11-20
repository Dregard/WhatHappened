using System;
using System.Collections.Generic;
using Game.Anomalies;
using Game.UI.Buttons;
using Game.UI.Windows.General;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Gameplay.Chapters
{
    /// <summary>Shows the found evidence of the current chapter and the unsolved culprit with the remaining-evidence prompt.</summary>
    public sealed class ChapterInvestigationWindow : Window<ChapterInvestigationWindowModel>
    {
        [SerializeField] private RectTransform _content;
        [SerializeField] private TMP_Text _foundLabel;
        [SerializeField] private RectTransform _contour;
        [SerializeField] private ChapterEvidenceRow _row;
        [SerializeField] private RectTransform _beams;
        [SerializeField] private Image _beamTemplate;
        [SerializeField] private TMP_Text _promptLabel;
        [SerializeField] private GameButton _closeButton;

        private readonly List<GameObject> _beamsList = new();
        private Action _closeAction;

        protected override void Setup(ChapterInvestigationWindowModel model)
        {
            var total = model.CluesArray.Length;
            var found = 0;
            foreach (var clue in model.CluesArray)
                if (clue != null)
                    found++;
            _foundLabel.text = $"You found {found}/{total} evidences";
            _row.Build(658f, 153f, 760f, model.CluesArray, 0.44f);
            ClearBeams();
            for (var i = 0; i < _row.Slots.Count; i++)
                if (model.CluesArray[i] != null)
                    DrawBeam(_row.Slots[i].Rect, 173f);
            var remaining = total - found;
            _promptLabel.text = remaining == 0 ? "All evidences found!" : remaining == 1 ? "Find the last evidence to solve the case!" : $"Find {remaining} more evidences to solve the case!";
            _closeAction = model.CloseAction;
            _closeButton.ClickEvent -= OnClose;
            _closeButton.ClickEvent += OnClose;
        }

        private void OnClose()
        {
            _closeAction?.Invoke();
        }

        /// <summary>Draws a thin gold beam from an evidence circle toward the culprit circle's edge.</summary>
        private void DrawBeam(RectTransform from, float stopRadius)
        {
            var beam = Instantiate(_beamTemplate, _beams, false);
            _beamsList.Add(beam.gameObject);
            var start = AnomalyUi.LocalRect(from, _content).center;
            var end = AnomalyUi.LocalRect(_contour, _content).center;
            var direction = (end - start).normalized;
            start += direction * (AnomalyUi.LocalRect(from, _content).width * 0.5f);
            end -= direction * stopRadius * (AnomalyUi.LocalRect(_contour, _content).width / 400f);
            var rect = beam.rectTransform;
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0f);
            rect.sizeDelta = new Vector2(6f, Vector2.Distance(start, end));
            rect.localPosition = start;
            rect.localRotation = Quaternion.Euler(0, 0, Vector2.SignedAngle(Vector2.up, end - start));
        }

        private void ClearBeams()
        {
            foreach (var beam in _beamsList)
                if (beam != null)
                    Destroy(beam);
            _beamsList.Clear();
        }
    }
}
