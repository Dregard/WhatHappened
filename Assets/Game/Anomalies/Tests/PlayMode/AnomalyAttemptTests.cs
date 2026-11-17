#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Game.Anomalies;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace Game.Anomalies.Tests
{
    public sealed class AnomalyAttemptTests
    {
        private const BindingFlags Flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

        /// <summary>Exercises production level lifecycle and actual frame ticks without startup, ads, profiles or network services.</summary>
        [UnityTest]
        public IEnumerator ReadyTimeoutContinuationAndUnload()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Game/Levels/LevelController.prefab");
            Assert.That(prefab, Is.Not.Null);
            var root = Object.Instantiate(prefab);
            var texture = new Texture2D(2, 2);
            var sprite = Sprite.Create(texture, new Rect(0, 0, 2, 2), Vector2.zero);
            var levelData = ScriptableObject.CreateInstance(Find("Game.Levels.LevelData"));
            try
            {
                var controller = root.GetComponent(Find("Game.Levels.LevelController"));
                Set(controller, "_timeoutWarningTimes", new List<float>());
                Set(levelData, "LevelTime", 2);
                var data = new AnomalyLevelData { MainImage = sprite, FindingDuration = 0, TransitionDuration = 0 };
                data.Presentation.PhotoDuration = 0;
                data.AnomaliesList.Add(new AnomalyData { Id = "a", Image = sprite, Bounds = new Rect(0, 0, 0.4f, 0.4f) });
                data.AnomaliesList.Add(new AnomalyData { Id = "b", Image = sprite, Bounds = new Rect(0.6f, 0.6f, 0.4f, 0.4f) });
                data.StagesList.Add(new AnomalyStageData { Count = 2 });
                Set(levelData, "SearchData", data);
                var progress = Activator.CreateInstance(Find("Game.Levels.LevelProgressController"));
                var preparation = Call(controller, "InitializeSearch", levelData, progress, 1);
                var awaiter = Call(preparation, "GetAwaiter");
                Assert.That(Get(awaiter, "IsCompleted"), Is.True);
                Call(awaiter, "GetResult");
                var search = (AnomalySearchSystem)Get(Get(controller, "AnomalyLevel"), "Search");
                Set(controller, "IsTimerPaused", false);
                yield return null;
                Assert.That((float)Get(controller, "TimerValue"), Is.EqualTo(2));
                Assert.That(search.TryFind(new Vector2(0.2f, 0.2f)), Is.False);
                var view = root.GetComponentInChildren<AnomalySearchView>();
                var ready = view.GetComponentInChildren<Button>();
                Assert.That(ready, Is.Not.Null);
                ready.onClick.Invoke();
                var readyDeadline = Time.realtimeSinceStartup + 2f;
                while (!search.IsStarted && Time.realtimeSinceStartup < readyDeadline)
                    yield return null;
                Assert.That(search.IsStarted, Is.True);
                Assert.That(search.TryFind(new Vector2(0.2f, 0.2f)), Is.True);
                yield return null;
                Assert.That((float)Get(controller, "TimerValue"), Is.LessThan(2));
                Set(controller, "IsTimerPaused", true);
                var pausedTime = (float)Get(controller, "TimerValue");
                yield return null;
                yield return null;
                Assert.That((float)Get(controller, "TimerValue"), Is.EqualTo(pausedTime));
                Assert.That(search.TryFind(new Vector2(0.8f, 0.8f)), Is.False);
                Set(controller, "IsTimerPaused", false);
                var lossesList = new List<object>();
                var eventInfo = controller.GetType().GetEvent("LevelLostEvent");
                var argument = System.Linq.Expressions.Expression.Parameter(Find("Game.Levels.LoseReason"));
                var body = System.Linq.Expressions.Expression.Call(System.Linq.Expressions.Expression.Constant(lossesList),
                    typeof(List<object>).GetMethod("Add"), System.Linq.Expressions.Expression.Convert(argument, typeof(object)));
                eventInfo.AddEventHandler(controller, System.Linq.Expressions.Expression.Lambda(eventInfo.EventHandlerType, body, argument).Compile());
                Set(controller, "_levelTimer", 0.0001f);
                yield return null;
                yield return null;
                Assert.That(lossesList.Count, Is.EqualTo(1));
                Assert.That(search.IsPaused, Is.True);
                Assert.That(search.IsFound("a"), Is.True);
                var attempt = Get(controller, "AttemptTime");
                Call(attempt, "MarkExtraTimeReceived");
                Call(controller, "AddTime", 10f);
                Assert.That(search.IsPaused, Is.False);
                Assert.That(search.IsFound("a"), Is.True);
                Assert.That((bool)Get(attempt, "ReceivedExtraTime"), Is.True);
                yield return null;
                Assert.That((float)Get(controller, "TimerValue"), Is.GreaterThan(9));
                Assert.That(lossesList.Count, Is.EqualTo(1));
                Call(controller, "ClearLevel");
                Assert.That(Get(controller, "AnomalyLevel"), Is.Null);
                Assert.That((float)Get(attempt, "ElapsedSeconds"), Is.Zero);
                Assert.That((bool)Get(attempt, "ReceivedExtraTime"), Is.False);
                var repeated = Call(controller, "InitializeSearch", levelData, progress, 1);
                Call(Call(repeated, "GetAwaiter"), "GetResult");
                Assert.That(((AnomalySearchSystem)Get(Get(controller, "AnomalyLevel"), "Search")).IsFound("a"), Is.False);
                Call(controller, "ClearLevel");
            }
            finally
            {
                Object.Destroy(root);
                Object.Destroy(levelData);
                Object.Destroy(sprite);
                Object.Destroy(texture);
            }
            yield return null;
        }

        /// <summary>Resolves production types without adding an assembly dependency on Assembly-CSharp.</summary>
        private static Type Find(string name)
        {
            return AppDomain.CurrentDomain.GetAssemblies().Select(assembly => assembly.GetType(name)).First(type => type != null);
        }

        /// <summary>Reads public state or isolated serialized fields.</summary>
        private static object Get(object target, string name)
        {
            var field = target.GetType().GetField(name, Flags);
            return field != null ? field.GetValue(target) : target.GetType().GetProperty(name, Flags).GetValue(target);
        }

        /// <summary>Sets isolated test instance state without changing production assets.</summary>
        private static void Set(object target, string name, object value)
        {
            var field = target.GetType().GetField(name, Flags);
            if (field != null)
                field.SetValue(target, value);
            else
                target.GetType().GetProperty(name, Flags).SetValue(target, value);
        }

        /// <summary>Invokes the production contract and surfaces its original exception.</summary>
        private static object Call(object target, string name, params object[] argumentsArray)
        {
            try
            {
                return target.GetType().GetMethod(name, Flags).Invoke(target, argumentsArray);
            }
            catch (TargetInvocationException error)
            {
                throw error.InnerException;
            }
        }
    }
}
#endif
