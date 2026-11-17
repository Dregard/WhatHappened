using System;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Anomalies
{
    [Serializable]
    public sealed class AnomalyStageData
    {
        [Min(0), Tooltip("Requested anomaly count. An impossible request is reduced to the feasible count; zero is an empty stage.")]
        public int Count;
        [Tooltip("Optional IDs by slot, up to Count. Empty or missing slots are random. Explicit assignments take priority and are reserved against earlier random slots.")]
        public List<string> AnomalyIdsList = new();
    }
}
