using System;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.AddressableAssets.Build;
using UnityEditor.Build.Reporting;

namespace Game.AssetManagement.Publishing.Tests
{
    /// <summary>Creates disposable local build output in the system temp folder; no project assets, sockets or servers are touched.</summary>
    public abstract class AbstractPublishingFixture
    {
        protected const string LoadUrl = "https://sunstormstudio.com/RemoteContent/GlassMaster/main/Android";
        protected const string DestinationRoot = "/www/sunstormstudio.com/RemoteContent/GlassMaster";
        protected const string RemoteBuildFolder = DestinationRoot + "/main/Android";
        protected const string ProfileGuid = "profile-release";
        protected const string Password = "S3cret-Pass!";
        protected const string Fingerprint = "ssh-ed25519 255 SHA256:abcdefghijklmnop";

        protected string WorkDirectory;
        protected string BuildDirectory;
        protected string ApkPath;

        /// <summary>Creates a unique temp folder with current build files, a stale bundle and an APK.</summary>
        [SetUp]
        public void CreateWorkspace()
        {
            WorkDirectory = Path.Combine(Path.GetTempPath(), "WhatHappenedPublishTests_" + Guid.NewGuid().ToString("N"));
            BuildDirectory = Path.Combine(WorkDirectory, "ServerData", "main", "Android");
            Directory.CreateDirectory(Path.Combine(BuildDirectory, "sub"));
            File.WriteAllText(Path.Combine(BuildDirectory, "a.bundle"), "bundle-a");
            File.WriteAllText(Path.Combine(BuildDirectory, "sub", "b.bundle"), "bundle-b");
            File.WriteAllText(Path.Combine(BuildDirectory, "catalog_1.json"), "{}");
            File.WriteAllText(Path.Combine(BuildDirectory, "catalog_1.hash"), "hash-123");
            File.WriteAllText(Path.Combine(BuildDirectory, "addressables_content_state.bin"), "state");
            File.WriteAllText(Path.Combine(BuildDirectory, "old.bundle"), "from-previous-build");
            ApkPath = Path.Combine(WorkDirectory, "game.apk");
            File.WriteAllText(ApkPath, "apk");
        }

        /// <summary>Removes the temp folder created by this fixture.</summary>
        [TearDown]
        public void DeleteWorkspace()
        {
            if (Directory.Exists(WorkDirectory))
            {
                Directory.Delete(WorkDirectory, true);
            }
        }

        /// <summary>Output data whose registry lists only the current build's files; old.bundle exists on disk but is not registered.</summary>
        protected AddressablesOutputData CreateOutput()
        {
            var filesList = new List<string>
            {
                Path.Combine(BuildDirectory, "a.bundle"),
                Path.Combine(BuildDirectory, "sub", "b.bundle"),
                Path.Combine(BuildDirectory, "catalog_1.json"),
                Path.Combine(BuildDirectory, "catalog_1.hash"),
                Path.Combine(BuildDirectory, "addressables_content_state.bin")
            };
            var locationsList = new List<OutputLocationData> { new OutputLocationData(RemoteContentPlanBuilder.NormalizePath(BuildDirectory), LoadUrl) };
            return new AddressablesOutputData(locationsList, filesList);
        }

        /// <summary>Builds the upload plan for the default destination root.</summary>
        protected PublishPlanData CreatePlan()
        {
            return new RemoteContentPlanBuilder(File.Exists).Build(CreateOutput(), DestinationRoot);
        }

        /// <summary>Settings with the release profile enabled and quick hash checks.</summary>
        protected static RemoteContentPublishData CreateSettings(params string[] enabledProfiles)
        {
            return RemoteContentPublishData.Create("vh101.hoster.by", 22, "h140875", DestinationRoot, enabledProfiles, 2, 0f);
        }

        /// <summary>Connection data with a password and fingerprint supplied locally.</summary>
        protected static SftpConnectionData CreateConnection()
        {
            return new SftpConnectionData("vh101.hoster.by", 22, "h140875", Password, Fingerprint, DestinationRoot, string.Empty, 30);
        }

        /// <summary>Secret source that serves test values without touching environment variables or user files.</summary>
        protected RemoteContentSecretStore CreateSecrets(string password, string fingerprint)
        {
            return new RemoteContentSecretStore(name => name == RemoteContentSecretStore.PasswordVariableName ? password
                : name == RemoteContentSecretStore.FingerprintVariableName ? fingerprint : string.Empty, Path.Combine(WorkDirectory, "secrets.json"));
        }

        /// <summary>An Addressables player build result for the current registry, optionally with an error.</summary>
        protected AddressablesPlayerBuildResult CreateAddressablesResult(string error = "")
        {
            var result = AddressableAssetBuildResult.CreateResult<AddressablesPlayerBuildResult>("settings.json", 1, error);
            result.FileRegistry = new FileRegistry();
            return result;
        }

        /// <summary>A tracked run of the enabled profile with a successful Addressables result, started a minute ago.</summary>
        protected PublishRunData CreateRun(bool withAddressables = true, string profile = ProfileGuid, bool gradleExport = false)
        {
            var run = new PublishRunData(BuildTarget.Android, ApkPath, profile, gradleExport, DateTime.UtcNow.AddMinutes(-1));
            if (withAddressables)
            {
                run.SetAddressablesResult(CreateAddressablesResult());
            }
            return run;
        }

        /// <summary>A snapshot of a finished build that produced the APK after the run started.</summary>
        protected PublishBuildSnapshot CreateBuild(BuildResult result = BuildResult.Succeeded, string profile = ProfileGuid, string outputPath = null)
        {
            return new PublishBuildSnapshot(result, BuildTarget.Android, outputPath ?? ApkPath, profile, true, DateTime.UtcNow);
        }

        /// <summary>Creates a publisher over fakes with a no-op wait.</summary>
        protected static RemoteContentPublisher CreatePublisher(FakeSftpSessionFactory factory, FakeHashDownloader downloader)
        {
            return new RemoteContentPublisher(factory, new RemoteHashVerifier(downloader, _ => { }), null);
        }
    }
}
