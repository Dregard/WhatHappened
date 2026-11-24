using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEngine;

namespace Game.AssetManagement.Publishing.Tests
{
    /// <summary>Verifies the read-only connection check, readable configuration errors and that secrets stay out of settings and messages.</summary>
    public sealed class RemoteContentConnectionAndSecretsTests : AbstractPublishingFixture
    {
        /// <summary>The check opens a session and lists the destination only.</summary>
        [Test]
        public void Check_OnlyOpensAndListsDestination()
        {
            var factory = new FakeSftpSessionFactory();
            factory.Session.DirectoriesSet.Add(DestinationRoot);
            var result = new RemoteContentConnectionChecker(factory).Check(CreateConnection());
            Assert.AreEqual(PublishStatusAlias.Succeeded, result.Status);
            CollectionAssert.AreEqual(new[] { "list " + DestinationRoot }, factory.Session.OperationsList);
            Assert.AreEqual(1, factory.OpenCount);
        }

        /// <summary>Missing settings produce a readable list, name the variables to set and never open a session.</summary>
        [Test]
        public void Check_ReportsMissingSettingsWithoutConnecting()
        {
            var factory = new FakeSftpSessionFactory();
            var connection = new SftpConnectionData("vh101.hoster.by", 22, "h140875", "", "", "", "", 30);
            var result = new RemoteContentConnectionChecker(factory).Check(connection);
            Assert.AreEqual(PublishStatusAlias.Failed, result.Status);
            StringAssert.Contains("FileZilla", result.Message);
            StringAssert.Contains(RemoteContentSecretStore.PasswordVariableName, result.Message);
            StringAssert.Contains(RemoteContentSecretStore.FingerprintVariableName, result.Message);
            Assert.AreEqual(0, factory.OpenCount);
        }

        /// <summary>A rejected SSH key surfaces as a connection error with the password masked.</summary>
        [Test]
        public void Check_ReportsWrongFingerprintWithoutLeakingPassword()
        {
            var factory = new FakeSftpSessionFactory { OpenError = "Отпечаток SSH-ключа не совпадает, пароль " + Password };
            var result = new RemoteContentConnectionChecker(factory).Check(CreateConnection());
            Assert.AreEqual(PublishStageAlias.Connection, result.Stage);
            StringAssert.Contains("Отпечаток", result.Message);
            StringAssert.DoesNotContain(Password, result.Message);
            Assert.IsEmpty(factory.Session.OperationsList);
        }

        /// <summary>The serialized project settings contain no password or fingerprint, and the connection description is secret-free.</summary>
        [Test]
        public void Settings_AndDescriptionsContainNoSecrets()
        {
            var json = JsonUtility.ToJson(CreateSettings(ProfileGuid));
            StringAssert.DoesNotContain(Password, json);
            StringAssert.DoesNotContain(Fingerprint, json);
            StringAssert.DoesNotContain(Password, CreateConnection().ToString());
            StringAssert.DoesNotContain(Password, CreateSecrets(Password, Fingerprint).DescribeSources());
        }

        /// <summary>Environment values win; locally saved values live only in the injected user file.</summary>
        [Test]
        public void SecretStore_ReadsEnvironmentThenLocalFile()
        {
            var store = CreateSecrets("env-pass", "");
            Assert.AreEqual("env-pass", store.GetPassword());
            Assert.AreEqual(string.Empty, store.GetHostKeyFingerprint());
            store.SaveHostKeyFingerprint(" " + Fingerprint + " ");
            Assert.AreEqual(Fingerprint, store.GetHostKeyFingerprint());
            Assert.IsTrue(File.Exists(store.FilePath));
            Assert.IsFalse(store.FilePath.StartsWith(Directory.GetCurrentDirectory()));
        }

        /// <summary>An upload failure after a successful Android build is reported as «APK/AAB created, publication not completed», never as success, and without secrets.</summary>
        [Test]
        public void System_SeparatesPublicationFailureFromBuildSuccess()
        {
            var factory = new FakeSftpSessionFactory();
            factory.Session.DirectoriesSet.Add(DestinationRoot);
            factory.Session.FailUploadContaining = "a.bundle";
            var log = new RecordingPublishLog();
            var system = new RemoteContentPublishSystem(CreateSettings(ProfileGuid), CreateSecrets(Password, Fingerprint),
                CreatePublisher(factory, new FakeHashDownloader()), _ => CreateOutput(), File.Exists, log);
            var result = system.OnPlayerBuilt(CreateRun(), CreateBuild());
            Assert.AreEqual(PublishStatusAlias.Failed, result.Status);
            Assert.AreEqual(1, log.ErrorList.Count);
            StringAssert.Contains("APK/AAB создан", log.ErrorList[0]);
            StringAssert.Contains("НЕ завершена", log.ErrorList[0]);
            Assert.IsFalse(log.InfoList.Any(line => line.Contains("завершена:")));
            Assert.IsFalse(log.ErrorList.Concat(log.InfoList).Any(line => line.Contains(Password) || line.Contains(Fingerprint)));
        }

        /// <summary>Missing secrets after a good build produce the same explicit incomplete-publication error without opening a session.</summary>
        [Test]
        public void System_ReportsMissingSecretsAsIncompletePublication()
        {
            var factory = new FakeSftpSessionFactory();
            var log = new RecordingPublishLog();
            var system = new RemoteContentPublishSystem(CreateSettings(ProfileGuid), CreateSecrets("", ""),
                CreatePublisher(factory, new FakeHashDownloader()), _ => CreateOutput(), File.Exists, log);
            Assert.AreEqual(PublishStatusAlias.Failed, system.OnPlayerBuilt(CreateRun(), CreateBuild()).Status);
            StringAssert.Contains("НЕ завершена", log.ErrorList.Single());
            Assert.AreEqual(0, factory.OpenCount);
        }

        /// <summary>A fully successful publication is logged as success without any error line.</summary>
        [Test]
        public void System_PublishesSuccessfully()
        {
            var factory = new FakeSftpSessionFactory();
            factory.Session.DirectoriesSet.Add(DestinationRoot);
            var downloader = new FakeHashDownloader();
            downloader.TextByUrl[LoadUrl + "/catalog_1.hash"] = "hash-123";
            var log = new RecordingPublishLog();
            var system = new RemoteContentPublishSystem(CreateSettings(ProfileGuid), CreateSecrets(Password, Fingerprint),
                CreatePublisher(factory, downloader), _ => CreateOutput(), File.Exists, log);
            Assert.AreEqual(PublishStatusAlias.Succeeded, system.OnPlayerBuilt(CreateRun(), CreateBuild()).Status);
            Assert.IsEmpty(log.ErrorList);
        }
    }
}
