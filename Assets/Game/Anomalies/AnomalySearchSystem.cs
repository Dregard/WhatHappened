using System;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Anomalies
{
    public sealed class AnomalySearchSystem
    {
        public event Action Changed;
        public event Action<AnomalyData> Found;
        public event Action Completed;
        private readonly List<IReadOnlyList<AnomalyData>> _stagesList;
        private readonly HashSet<string> _foundIdsSet = new(StringComparer.Ordinal);
        private readonly HashSet<string> _pendingIdsSet = new(StringComparer.Ordinal);
        private readonly IReadOnlyCollection<string> _warnings;
        public int StageIndex { get; private set; } = -1;
        public bool IsStarted { get; private set; }
        public bool IsPaused { get; set; }
        public bool IsCompleted { get; private set; }
        public bool IsTransitionPending { get; private set; }
        public bool IsStageFound
        {
            get
            {
                foreach (var anomaly in CurrentAnomalies)
                    if (!_foundIdsSet.Contains(anomaly.Id))
                        return false;
                return true;
            }
        }
        public IReadOnlyCollection<string> Warnings { get { return _warnings; } }
        public IReadOnlyList<IReadOnlyList<AnomalyData>> Stages { get { return _stagesList.AsReadOnly(); } }
        public IReadOnlyList<AnomalyData> CurrentAnomalies { get; private set; } = Array.Empty<AnomalyData>();

        /// <summary>Creates attempt-local selection and findings without writing authored configuration or profile data.</summary>
        public AnomalySearchSystem(AnomalyLevelData data, int seed)
        {
            var warningsList = new List<string>();
            _stagesList = AnomalyStageSelectionController.Select(data, new System.Random(seed), warningsList);
            _warnings = warningsList.AsReadOnly();
        }

        /// <summary>Ready enables the first stage exactly once. Empty levels also require the presentation gate.</summary>
        public void Start()
        {
            if (IsStarted)
                return;
            IsStarted = true;
            ActivateStage(0);
        }

        /// <summary>Reports whether an ID was found in any stage of this attempt.</summary>
        public bool IsFound(string id)
        {
            return _foundIdsSet.Contains(id);
        }

        /// <summary>Accepts at most one previously unfound image rectangle. Transparent pixels count; paused/preview/transition clicks do not.</summary>
        public bool TryFind(Vector2 imagePoint)
        {
            if (!IsStarted || IsPaused || IsCompleted || IsTransitionPending)
                return false;
            foreach (var anomaly in CurrentAnomalies)
            {
                if (!anomaly.Bounds.Contains(imagePoint) || !_foundIdsSet.Add(anomaly.Id))
                    continue;
                _pendingIdsSet.Add(anomaly.Id);
                Found?.Invoke(anomaly);
                Changed?.Invoke();
                return true;
            }
            return false;
        }

        /// <summary>Presentation acknowledges one finding by ID; duplicate/stale acknowledgements cannot advance progress.</summary>
        public void CompleteFinding(string id)
        {
            if (!_pendingIdsSet.Remove(id))
                return;
            CheckStage();
            Changed?.Invoke();
        }

        /// <summary>Releases only the current completed stage after its transition presentation finishes.</summary>
        public void CompleteTransition(int stageIndex)
        {
            if (!IsTransitionPending || StageIndex != stageIndex || IsCompleted)
                return;
            IsTransitionPending = false;
            if (StageIndex + 1 >= _stagesList.Count)
            {
                IsCompleted = true;
                Changed?.Invoke();
                Completed?.Invoke();
                return;
            }
            ActivateStage(StageIndex + 1);
        }

        /// <summary>Replaces the visible stage; empty stages enter the same transition gate instead of waiting for an impossible click.</summary>
        private void ActivateStage(int index)
        {
            StageIndex = index;
            CurrentAnomalies = index < _stagesList.Count ? _stagesList[index] : Array.Empty<AnomalyData>();
            CheckStage();
            Changed?.Invoke();
        }

        /// <summary>Defers stage completion until every accepted finding has finished its presentation.</summary>
        private void CheckStage()
        {
            if (_pendingIdsSet.Count != 0)
                return;
            foreach (var anomaly in CurrentAnomalies)
                if (!_foundIdsSet.Contains(anomaly.Id))
                    return;
            IsTransitionPending = true;
        }
    }
}
