using System;
using System.Collections.Generic;

namespace Game.Anomalies
{
    public static class AnomalyStageSelectionController
    {
        /// <summary>Plans an attempt without reusing IDs. Explicit future assignments are reserved before random selection; impossible counts shrink.</summary>
        public static List<IReadOnlyList<AnomalyData>> Select(AnomalyLevelData data, Random random, List<string> warningsList)
        {
            var availableList = new List<AnomalyData>();
            var anomaliesById = new Dictionary<string, AnomalyData>(StringComparer.Ordinal);
            for (var index = 0; index < data.AnomaliesList.Count; index++)
            {
                var anomaly = data.AnomaliesList[index];
                var reason = GetExclusionReason(anomaly, anomaliesById);
                if (reason != null)
                {
                    warningsList.Add($"Anomaly {index + 1} ('{anomaly?.Id}') excluded: {reason}.");
                    continue;
                }
                anomaliesById.Add(anomaly.Id, anomaly);
                availableList.Add(anomaly);
            }
            var reservedSet = new HashSet<string>(StringComparer.Ordinal);
            foreach (var stage in data.StagesList)
                if (stage != null)
                    for (var i = 0; i < Math.Min(stage.Count, stage.AnomalyIdsList.Count); i++)
                        if (!string.IsNullOrWhiteSpace(stage.AnomalyIdsList[i]))
                            reservedSet.Add(stage.AnomalyIdsList[i]);

            var usedSet = new HashSet<string>(StringComparer.Ordinal);
            var resultList = new List<IReadOnlyList<AnomalyData>>();
            foreach (var stage in data.StagesList)
            {
                var selectedList = new List<AnomalyData>();
                var requested = Math.Max(0, stage?.Count ?? 0);
                var randomSlots = requested;
                if (stage != null)
                    for (var i = 0; i < Math.Min(requested, stage.AnomalyIdsList.Count); i++)
                    {
                        var id = stage.AnomalyIdsList[i];
                        if (string.IsNullOrWhiteSpace(id))
                            continue;
                        randomSlots--;
                        if (!anomaliesById.TryGetValue(id, out var anomaly))
                            warningsList.Add($"Stage {resultList.Count + 1}: explicit ID '{id}' is missing or was excluded as invalid (see exclusion warnings); slot omitted.");
                        else if (usedSet.Contains(id))
                            warningsList.Add($"Stage {resultList.Count + 1}: explicit ID '{id}' is already used by an earlier stage/slot; slot omitted.");
                        else if (!Fits(anomaly, selectedList))
                            warningsList.Add($"Stage {resultList.Count + 1}: explicit ID '{id}' overlaps an already selected anomaly; slot omitted.");
                        else
                        {
                            selectedList.Add(anomaly);
                            usedSet.Add(id);
                        }
                    }
                var candidatesList = availableList.FindAll(anomaly => !usedSet.Contains(anomaly.Id) &&
                    !reservedSet.Contains(anomaly.Id) && Fits(anomaly, selectedList));
                for (var i = candidatesList.Count - 1; i > 0; i--)
                {
                    var j = random.Next(i + 1);
                    (candidatesList[i], candidatesList[j]) = (candidatesList[j], candidatesList[i]);
                }
                var bestList = new List<AnomalyData>();
                FindCompatible(candidatesList, 0, randomSlots, new List<AnomalyData>(), bestList);
                foreach (var anomaly in bestList)
                {
                    selectedList.Add(anomaly);
                    usedSet.Add(anomaly.Id);
                }
                if (selectedList.Count < requested)
                    warningsList.Add($"Stage {resultList.Count + 1}: requested {requested}, available {selectedList.Count} non-overlapping anomalies.");
                resultList.Add(selectedList.AsReadOnly());
            }
            return resultList;
        }

        /// <summary>Finds a largest compatible subset up to the requested count; prunes branches that cannot improve the current result.</summary>
        private static void FindCompatible(List<AnomalyData> candidatesList, int index, int target,
            List<AnomalyData> currentList, List<AnomalyData> bestList)
        {
            if (currentList.Count > bestList.Count)
            {
                bestList.Clear();
                bestList.AddRange(currentList);
            }
            if (bestList.Count >= target || currentList.Count + candidatesList.Count - index <= bestList.Count)
                return;
            for (var i = index; i < candidatesList.Count; i++)
            {
                if (!Fits(candidatesList[i], currentList))
                    continue;
                currentList.Add(candidatesList[i]);
                FindCompatible(candidatesList, i + 1, target, currentList, bestList);
                currentList.RemoveAt(currentList.Count - 1);
                if (bestList.Count >= target)
                    return;
            }
        }

        /// <summary>Uses full image rectangles; touching edges without positive area are allowed.</summary>
        private static bool Fits(AnomalyData anomaly, List<AnomalyData> selectedList)
        {
            foreach (var selected in selectedList)
                if (anomaly.Bounds.Overlaps(selected.Bounds))
                    return false;
            return true;
        }

        /// <summary>Returns the concrete reason an anomaly cannot be selected, or null when it is valid.</summary>
        private static string GetExclusionReason(AnomalyData anomaly, Dictionary<string, AnomalyData> anomaliesById)
        {
            if (anomaly == null)
                return "entry is missing";
            if (string.IsNullOrWhiteSpace(anomaly.Id))
                return "ID is empty";
            if (anomaliesById.ContainsKey(anomaly.Id))
                return "duplicate ID";
            if (anomaly.Image == null)
                return "Image is not assigned";
            var b = anomaly.Bounds;
            if (!IsFinite(b.x) || !IsFinite(b.y) || !IsFinite(b.width) || !IsFinite(b.height))
                return "Bounds contain non-finite values";
            if (b.width <= 0 || b.height <= 0)
                return $"Bounds size must be positive (width {b.width}, height {b.height})";
            if (b.xMin < 0 || b.yMin < 0 || b.xMax > 1 || b.yMax > 1)
                return $"Bounds x[{b.xMin:0.###}..{b.xMax:0.###}] y[{b.yMin:0.###}..{b.yMax:0.###}] lie outside the normalized 0..1 image area";
            return null;
        }

        /// <summary>Rejects non-finite serialized geometry before rectangle comparisons.</summary>
        private static bool IsFinite(float value)
        {
            return !float.IsNaN(value) && !float.IsInfinity(value);
        }
    }
}
