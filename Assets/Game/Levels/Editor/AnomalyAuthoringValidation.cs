using System;
using System.Collections.Generic;
using Game.Anomalies;

namespace Game.Levels.Editor
{
    public static class AnomalyAuthoringValidation
    {
        /// <summary>Reports malformed IDs/references even outside active slots; never edits the level or reduces authored counts.</summary>
        public static List<string> Validate(AnomalyLevelData data)
        {
            var warningsList = new List<string>();
            if (data.MainImage == null)
                warningsList.Add("Assign Main Image to enable anomaly search when this level loads.");
            if (data.ClueImage == null)
                warningsList.Add("Clue Image is not assigned.");
            if (data.StagesList.Count == 0)
                warningsList.Add("No stages configured: this level has no search slots.");
            var idsSet = new HashSet<string>(StringComparer.Ordinal);
            for (var i = 0; i < data.AnomaliesList.Count; i++)
            {
                var anomaly = data.AnomaliesList[i];
                if (anomaly == null || string.IsNullOrWhiteSpace(anomaly.Id))
                    warningsList.Add($"Anomaly {i + 1}: ID is empty.");
                else if (!idsSet.Add(anomaly.Id))
                    warningsList.Add($"Anomaly {i + 1}: duplicate ID '{anomaly.Id}'.");
            }
            var assignedSet = new HashSet<string>(StringComparer.Ordinal);
            long totalRequested = 0;
            for (var i = 0; i < data.StagesList.Count; i++)
            {
                var stage = data.StagesList[i];
                if (stage == null)
                {
                    warningsList.Add($"Stage {i + 1}: missing stage data.");
                    continue;
                }
                if (stage.Count < 0)
                    warningsList.Add($"Stage {i + 1}: Count must not be negative.");
                totalRequested += Math.Max(0, stage.Count);
                for (var slot = 0; slot < stage.AnomalyIdsList.Count; slot++)
                {
                    var id = stage.AnomalyIdsList[slot];
                    if (string.IsNullOrWhiteSpace(id))
                        continue;
                    if (!idsSet.Contains(id))
                        warningsList.Add($"Stage {i + 1}, slot {slot + 1}: missing ID '{id}'.");
                    if (slot >= stage.Count)
                        warningsList.Add($"Stage {i + 1}, slot {slot + 1}: ID '{id}' is outside Count and will not be used.");
                    else if (!assignedSet.Add(id))
                        warningsList.Add($"Stage {i + 1}, slot {slot + 1}: ID '{id}' is already assigned to an earlier slot/stage.");
                }
            }
            if (totalRequested > idsSet.Count)
                warningsList.Add($"Stages request {totalRequested} anomalies in total, but only {idsSet.Count} unique IDs exist. IDs cannot repeat across stages.");
            return warningsList;
        }
    }
}
