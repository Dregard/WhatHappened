using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;

namespace Game.Anomalies.Tests
{
    public sealed class AnomalySearchTests
    {
        private Texture2D _texture;
        private Sprite _sprite;

        /// <summary>Creates an in-memory image; tests open no transports, scenes or external services.</summary>
        [SetUp]
        public void SetUp()
        {
            _texture = new Texture2D(2, 2);
            _sprite = Sprite.Create(_texture, new Rect(0, 0, 2, 2), Vector2.zero);
        }

        /// <summary>Releases test-owned image objects.</summary>
        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_sprite);
            Object.DestroyImmediate(_texture);
        }

        /// <summary>Later authored IDs cannot be consumed by earlier random slots, across many random seeds.</summary>
        [Test]
        public void ReservesExplicitFutureAssignmentsWithoutRepetition()
        {
            var data = Data(4);
            data.StagesList.Add(new AnomalyStageData { Count = 2 });
            data.StagesList.Add(new AnomalyStageData { Count = 1, AnomalyIdsList = new List<string> { "0" } });
            for (var seed = 0; seed < 100; seed++)
            {
                var search = new AnomalySearchSystem(data, seed);
                Assert.That(search.Stages[0].Count, Is.EqualTo(2));
                Assert.That(search.Stages[1].Single().Id, Is.EqualTo("0"));
                Assert.That(search.Stages.SelectMany(stage => stage).Select(anomaly => anomaly.Id).Distinct().Count(), Is.EqualTo(3));
            }
        }

        /// <summary>Explicit assignment wins over random candidates even when its slot appears after a random slot.</summary>
        [Test]
        public void ExplicitAssignmentsConstrainRandomSelection()
        {
            var data = Data(3);
            data.AnomaliesList[0].Bounds = new Rect(0, 0, 0.5f, 0.5f);
            data.AnomaliesList[1].Bounds = new Rect(0.3f, 0, 0.4f, 0.4f);
            data.StagesList.Add(new AnomalyStageData { Count = 3, AnomalyIdsList = new List<string> { "", "0" } });
            var search = new AnomalySearchSystem(data, 2);
            CollectionAssert.AreEquivalent(new[] { "0", "2" }, search.Stages[0].Select(anomaly => anomaly.Id));
            Assert.That(search.Warnings, Is.Not.Empty);
        }

        /// <summary>Maximum subset search avoids a greedy large rectangle reducing a feasible pair to one anomaly.</summary>
        [Test]
        public void FindsFeasibleCountAndWarnsOnShortage()
        {
            var data = Data(3);
            data.AnomaliesList[0].Bounds = new Rect(0, 0, 0.8f, 0.8f);
            data.StagesList.Add(new AnomalyStageData { Count = 3 });
            for (var seed = 0; seed < 30; seed++)
            {
                var search = new AnomalySearchSystem(data, seed);
                Assert.That(search.Stages[0].Count, Is.EqualTo(2));
                Assert.That(search.Stages[0][0].Bounds.Overlaps(search.Stages[0][1].Bounds), Is.False);
                Assert.That(search.Warnings, Is.Not.Empty);
            }
        }

        /// <summary>Ready, duplicate clicks, parallel findings, pause, stale callbacks and final presentation gates remain independent.</summary>
        [Test]
        public void WaitsForEveryFindingAndTransitionBeforeCompletion()
        {
            var data = Data(2);
            data.StagesList.Add(new AnomalyStageData { Count = 2 });
            var search = new AnomalySearchSystem(data, 0);
            var completed = 0;
            search.Completed += () => completed++;
            Assert.That(search.TryFind(new Vector2(0.1f, 0.1f)), Is.False);
            search.Start();
            search.IsPaused = true;
            Assert.That(search.TryFind(new Vector2(0.1f, 0.1f)), Is.False);
            search.IsPaused = false;
            Assert.That(search.TryFind(new Vector2(0.1f, 0.1f)), Is.True);
            Assert.That(search.TryFind(new Vector2(0.1f, 0.1f)), Is.False);
            Assert.That(search.TryFind(new Vector2(0.35f, 0.1f)), Is.True);
            Assert.That(search.IsStageFound, Is.True);
            Assert.That(search.IsTransitionPending, Is.False);
            search.CompleteFinding("0");
            search.CompleteFinding("0");
            search.CompleteTransition(0);
            Assert.That(completed, Is.Zero);
            search.CompleteFinding("1");
            Assert.That(search.IsTransitionPending, Is.True);
            search.CompleteTransition(-1);
            Assert.That(search.IsCompleted, Is.False);
            search.CompleteTransition(0);
            search.CompleteTransition(0);
            Assert.That(search.IsCompleted, Is.True);
            Assert.That(completed, Is.EqualTo(1));
            Assert.That(new AnomalySearchSystem(data, 0).IsFound("0"), Is.False);
        }

        /// <summary>Zero, exhausted and conflicting explicit stages can all pass through their finite presentation gates.</summary>
        [Test]
        public void EmptyAndExhaustedStagesDoNotBlock()
        {
            var data = Data(1);
            data.StagesList.Add(new AnomalyStageData());
            data.StagesList.Add(new AnomalyStageData { Count = 1, AnomalyIdsList = new List<string> { "0" } });
            data.StagesList.Add(new AnomalyStageData { Count = 3, AnomalyIdsList = new List<string> { "0", "missing" } });
            var search = new AnomalySearchSystem(data, 0);
            search.Start();
            Assert.That(search.IsTransitionPending, Is.True);
            search.CompleteTransition(0);
            Assert.That(search.CurrentAnomalies.Count, Is.EqualTo(1));
            search.TryFind(new Vector2(0.1f, 0.1f));
            search.CompleteFinding("0");
            search.CompleteTransition(1);
            Assert.That(search.CurrentAnomalies, Is.Empty);
            Assert.That(search.IsTransitionPending, Is.True);
            search.CompleteTransition(2);
            Assert.That(search.IsCompleted, Is.True);
        }

        /// <summary>Even a level with no configured stages completes only after Ready and final presentation acknowledgement.</summary>
        [Test]
        public void NoStagesCompletesThroughGate()
        {
            var search = new AnomalySearchSystem(Data(0), 0);
            search.Start();
            Assert.That(search.IsCompleted, Is.False);
            search.CompleteTransition(0);
            Assert.That(search.IsCompleted, Is.True);
        }

        /// <summary>Builds non-overlapping normalized rectangles using a test-owned image.</summary>
        private AnomalyLevelData Data(int count)
        {
            var data = new AnomalyLevelData { MainImage = _sprite };
            for (var i = 0; i < count; i++)
                data.AnomaliesList.Add(new AnomalyData { Id = i.ToString(), Image = _sprite, Bounds = new Rect(i * 0.25f, 0, 0.2f, 0.2f) });
            return data;
        }
    }
}
