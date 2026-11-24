using System;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.AddressableAssets.Build;
using UnityEditor.Build.Reporting;

namespace Game.AssetManagement.Publishing.Tests
{
    /// <summary>Verifies that publication starts only for a successful, profile-enabled Android APK/AAB run with a same-run Addressables build.</summary>
    public sealed class RemoteContentPublishGateTests : AbstractPublishingFixture
    {
        /// <summary>The happy path is allowed.</summary>
        [Test]
        public void Allows_SuccessfulEnabledProfileRunWithAddressablesBuild()
        {
            Assert.IsTrue(RemoteContentPublishGate.Evaluate(CreateRun(), CreateBuild(), CreateSettings(ProfileGuid)).ShouldPublish);
        }

        /// <summary>Failed and cancelled builds never publish.</summary>
        [TestCase(BuildResult.Failed)]
        [TestCase(BuildResult.Cancelled)]
        public void Skips_WhenBuildDidNotSucceed(BuildResult result)
        {
            Assert.IsFalse(RemoteContentPublishGate.Evaluate(CreateRun(), CreateBuild(result), CreateSettings(ProfileGuid)).ShouldPublish);
        }

        /// <summary>Unity may report Unknown while the postprocessor runs; the fresh artifact and same-run checks still decide.</summary>
        [Test]
        public void Allows_UnknownResultWhenArtifactIsFreshAndRunMatches()
        {
            Assert.IsTrue(RemoteContentPublishGate.Evaluate(CreateRun(), CreateBuild(BuildResult.Unknown), CreateSettings(ProfileGuid)).ShouldPublish);
            Assert.IsFalse(RemoteContentPublishGate.Evaluate(CreateRun(withAddressables: false), CreateBuild(BuildResult.Unknown), CreateSettings(ProfileGuid)).ShouldPublish);
        }

        /// <summary>Gradle project export is not an APK/AAB.</summary>
        [Test]
        public void Skips_WhenGradleProjectIsExported()
        {
            Assert.IsFalse(RemoteContentPublishGate.Evaluate(CreateRun(gradleExport: true), CreateBuild(), CreateSettings(ProfileGuid)).ShouldPublish);
            var directoryBuild = new PublishBuildSnapshot(BuildResult.Succeeded, BuildTarget.Android, WorkDirectory, ProfileGuid, false, DateTime.UtcNow);
            Assert.IsFalse(RemoteContentPublishGate.Evaluate(CreateRun(), directoryBuild, CreateSettings(ProfileGuid)).ShouldPublish);
        }

        /// <summary>Profiles without explicit enabling, or a different profile than the run's, never publish.</summary>
        [Test]
        public void Skips_WhenProfileIsNotEnabledOrDiffersFromRun()
        {
            Assert.IsFalse(RemoteContentPublishGate.Evaluate(CreateRun(), CreateBuild(), CreateSettings()).ShouldPublish);
            Assert.IsFalse(RemoteContentPublishGate.Evaluate(CreateRun(), CreateBuild(), CreateSettings("other-profile")).ShouldPublish);
            Assert.IsFalse(RemoteContentPublishGate.Evaluate(CreateRun(profile: ""), CreateBuild(profile: ""), CreateSettings(ProfileGuid)).ShouldPublish);
            Assert.IsFalse(RemoteContentPublishGate.Evaluate(CreateRun(), CreateBuild(profile: "other-profile"), CreateSettings(ProfileGuid, "other-profile")).ShouldPublish);
        }

        /// <summary>A run without its own Addressables build (only older output on disk) or with a build error never publishes.</summary>
        [Test]
        public void Skips_WhenAddressablesWasNotBuiltInThisRunOrFailed()
        {
            Assert.IsFalse(RemoteContentPublishGate.Evaluate(CreateRun(withAddressables: false), CreateBuild(), CreateSettings(ProfileGuid)).ShouldPublish);
            var failedRun = CreateRun(withAddressables: false);
            failedRun.SetAddressablesResult(CreateAddressablesResult("boom"));
            Assert.IsFalse(RemoteContentPublishGate.Evaluate(failedRun, CreateBuild(), CreateSettings(ProfileGuid)).ShouldPublish);
            Assert.IsFalse(RemoteContentPublishGate.Evaluate(null, CreateBuild(), CreateSettings(ProfileGuid)).ShouldPublish);
        }

        /// <summary>An APK written before the run started, or at another path, is a previous build's output.</summary>
        [Test]
        public void Skips_WhenArtifactBelongsToAnotherRun()
        {
            var oldBuild = new PublishBuildSnapshot(BuildResult.Succeeded, BuildTarget.Android, ApkPath, ProfileGuid, true, DateTime.UtcNow.AddHours(-1));
            Assert.IsFalse(RemoteContentPublishGate.Evaluate(CreateRun(), oldBuild, CreateSettings(ProfileGuid)).ShouldPublish);
            Assert.IsFalse(RemoteContentPublishGate.Evaluate(CreateRun(), CreateBuild(outputPath: WorkDirectory + "/other.apk"), CreateSettings(ProfileGuid)).ShouldPublish);
        }

        /// <summary>Non-Android targets and non APK/AAB files are skipped.</summary>
        [Test]
        public void Skips_ForNonAndroidOrUnsupportedArtifact()
        {
            var windows = new PublishBuildSnapshot(BuildResult.Succeeded, BuildTarget.StandaloneWindows64, ApkPath, ProfileGuid, true, DateTime.UtcNow);
            Assert.IsFalse(RemoteContentPublishGate.Evaluate(CreateRun(), windows, CreateSettings(ProfileGuid)).ShouldPublish);
            var zip = CreateBuild(outputPath: WorkDirectory + "/game.zip");
            Assert.IsFalse(RemoteContentPublishGate.Evaluate(CreateRun(), zip, CreateSettings(ProfileGuid)).ShouldPublish);
        }

        /// <summary>The tracker attaches Addressables results only to the open run, and a new run discards the previous one.</summary>
        [Test]
        public void Tracker_BindsResultsToOneRunOnly()
        {
            var tracker = new PublishRunTracker();
            tracker.RegisterAddressablesResult(CreateAddressablesResult());
            Assert.IsNull(tracker.Take());
            tracker.Begin(CreateRun(withAddressables: false));
            tracker.Begin(CreateRun(withAddressables: false));
            tracker.RegisterAddressablesResult(CreateAddressablesResult());
            var run = tracker.Take();
            Assert.IsNotNull(run.AddressablesResult);
            Assert.IsNull(tracker.Take());
            tracker.Begin(CreateRun(withAddressables: false));
            Assert.IsNull(tracker.Take().AddressablesResult);
        }

        /// <summary>A skipped run neither opens a session nor builds Addressables.</summary>
        [Test]
        public void System_DoesNotConnectOrRebuildWhenSkipped()
        {
            var factory = new FakeSftpSessionFactory();
            var buildCount = 0;
            Action<AddressableAssetBuildResult> counter = _ => buildCount++;
            BuildScript.buildCompleted += counter;
            try
            {
                var system = new RemoteContentPublishSystem(CreateSettings(ProfileGuid), CreateSecrets(Password, Fingerprint),
                    CreatePublisher(factory, new FakeHashDownloader()), _ => CreateOutput(), System.IO.File.Exists, new RecordingPublishLog());
                var result = system.OnPlayerBuilt(CreateRun(), CreateBuild(BuildResult.Failed));
                Assert.AreEqual(PublishStatusAlias.Skipped, result.Status);
            }
            finally
            {
                BuildScript.buildCompleted -= counter;
            }
            Assert.AreEqual(0, factory.OpenCount);
            Assert.AreEqual(0, buildCount);
        }
    }
}
