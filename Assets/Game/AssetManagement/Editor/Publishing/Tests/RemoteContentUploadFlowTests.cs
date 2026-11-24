using System.Linq;
using NUnit.Framework;

namespace Game.AssetManagement.Publishing.Tests
{
    /// <summary>Verifies path mapping, ordering, temporary names, failure handling and .hash verification with in-memory fakes only.</summary>
    public sealed class RemoteContentUploadFlowTests : AbstractPublishingFixture
    {
        /// <summary>Shared root segments are not duplicated and different root styles reach the same server folder.</summary>
        [TestCase("/www/sunstormstudio.com/RemoteContent/GlassMaster", "/www/sunstormstudio.com/RemoteContent/GlassMaster/main/Android/a.bundle")]
        [TestCase("/www/sunstormstudio.com/RemoteContent", "/www/sunstormstudio.com/RemoteContent/GlassMaster/main/Android/a.bundle")]
        [TestCase("\\www\\sunstormstudio.com\\RemoteContent\\GlassMaster\\main\\Android\\", "/www/sunstormstudio.com/RemoteContent/GlassMaster/main/Android/a.bundle")]
        [TestCase("/www/site", "/www/site/RemoteContent/GlassMaster/main/Android/a.bundle")]
        public void Mapper_DoesNotDuplicateSegments(string root, string expected)
        {
            Assert.AreEqual(expected, RemoteContentPathMapper.MapToRemotePath(root, LoadUrl + "/a.bundle"));
        }

        /// <summary>The plan contains only current-build files, keeps subfolders and orders bundles, catalog, hash.</summary>
        [Test]
        public void Plan_UsesRegistryFilesPreservesSubfoldersAndOrder()
        {
            var plan = CreatePlan();
            CollectionAssert.AreEqual(new[]
            {
                RemoteBuildFolder + "/a.bundle", RemoteBuildFolder + "/sub/b.bundle",
                RemoteBuildFolder + "/catalog_1.json", RemoteBuildFolder + "/catalog_1.hash"
            }, plan.FilesList.Select(file => file.RemotePath).ToArray());
            CollectionAssert.AreEqual(new[] { PublishStageAlias.Bundles, PublishStageAlias.Bundles, PublishStageAlias.Catalog, PublishStageAlias.Hash },
                plan.FilesList.Select(file => file.Stage).ToArray());
            Assert.AreEqual(LoadUrl + "/sub/b.bundle", plan.FilesList[1].Url);
            Assert.IsFalse(plan.FilesList.Any(file => file.LocalPath.EndsWith("old.bundle") || file.LocalPath.EndsWith(".bin")));
        }

        /// <summary>Each file goes to a temporary name first, is renamed afterwards, old remote bundles stay and the .hash matches.</summary>
        [Test]
        public void Publish_UploadsViaTemporaryNamesKeepsOldBundlesAndVerifiesHash()
        {
            var factory = new FakeSftpSessionFactory();
            factory.Session.DirectoriesSet.Add(DestinationRoot);
            factory.Session.FilesByPath[RemoteBuildFolder + "/old.bundle"] = "previous";
            var downloader = new FakeHashDownloader();
            downloader.TextByUrl[LoadUrl + "/catalog_1.hash"] = "hash-123\n";
            var result = CreatePublisher(factory, downloader).Publish(CreateConnection(), CreatePlan(), CreateSettings(ProfileGuid));
            Assert.AreEqual(PublishStatusAlias.Succeeded, result.Status, result.Message);
            var operationsList = factory.Session.OperationsList;
            var uploads = operationsList.Where(operation => operation.StartsWith("upload ")).ToList();
            Assert.AreEqual(4, uploads.Count);
            Assert.IsTrue(uploads.All(upload => upload.EndsWith(".uploading")));
            var moves = operationsList.Where(operation => operation.StartsWith("move ")).ToList();
            Assert.AreEqual(4, moves.Count);
            for (var index = 0; index < 4; index++)
            {
                Assert.Less(operationsList.IndexOf(uploads[index]), operationsList.IndexOf(moves[index]));
            }
            Assert.IsTrue(moves[0].EndsWith("/a.bundle") && moves[2].EndsWith("/catalog_1.json") && moves[3].EndsWith("/catalog_1.hash"));
            Assert.IsFalse(operationsList.Any(operation => operation.StartsWith("delete ")));
            Assert.AreEqual("previous", factory.Session.FilesByPath[RemoteBuildFolder + "/old.bundle"]);
            Assert.AreEqual("hash-123", factory.Session.FilesByPath[RemoteBuildFolder + "/catalog_1.hash"]);
            Assert.IsFalse(factory.Session.FilesByPath.Keys.Any(path => path.EndsWith(".uploading")));
            Assert.IsTrue(factory.Session.Disposed);
            CollectionAssert.AreEqual(new[] { LoadUrl + "/catalog_1.hash" }, downloader.RequestedUrlsList);
        }

        /// <summary>A failed bundle upload stops the publication: no catalog, no .hash and no verification.</summary>
        [Test]
        public void Publish_StopsAtFirstFailureAndSkipsLaterStages()
        {
            var factory = new FakeSftpSessionFactory();
            factory.Session.DirectoriesSet.Add(DestinationRoot);
            factory.Session.FailUploadContaining = "b.bundle";
            var downloader = new FakeHashDownloader();
            var result = CreatePublisher(factory, downloader).Publish(CreateConnection(), CreatePlan(), CreateSettings(ProfileGuid));
            Assert.AreEqual(PublishStatusAlias.Failed, result.Status);
            Assert.AreEqual(PublishStageAlias.Bundles, result.Stage);
            Assert.IsFalse(factory.Session.OperationsList.Any(operation => operation.Contains("catalog_1")));
            Assert.AreEqual(0, downloader.RequestedUrlsList.Count);
        }

        /// <summary>A catalog failure prevents the .hash from being uploaded, so clients never see a hash without its catalog.</summary>
        [Test]
        public void Publish_DoesNotUploadHashAfterCatalogFailure()
        {
            var factory = new FakeSftpSessionFactory();
            factory.Session.DirectoriesSet.Add(DestinationRoot);
            factory.Session.FailUploadContaining = "catalog_1.json";
            var result = CreatePublisher(factory, new FakeHashDownloader()).Publish(CreateConnection(), CreatePlan(), CreateSettings(ProfileGuid));
            Assert.AreEqual(PublishStageAlias.Catalog, result.Stage);
            Assert.IsFalse(factory.Session.OperationsList.Any(operation => operation.Contains("catalog_1.hash")));
        }

        /// <summary>Hash mismatch and download errors both mean an incomplete publication.</summary>
        [Test]
        public void Publish_ReportsVerificationFailures()
        {
            var factory = new FakeSftpSessionFactory();
            factory.Session.DirectoriesSet.Add(DestinationRoot);
            var downloader = new FakeHashDownloader();
            downloader.TextByUrl[LoadUrl + "/catalog_1.hash"] = "other-hash";
            var mismatch = CreatePublisher(factory, downloader).Publish(CreateConnection(), CreatePlan(), CreateSettings(ProfileGuid));
            Assert.AreEqual(PublishStatusAlias.Failed, mismatch.Status);
            Assert.AreEqual(PublishStageAlias.Verification, mismatch.Stage);
            Assert.AreEqual(2, downloader.RequestedUrlsList.Count);
            var missing = CreatePublisher(factory, new FakeHashDownloader()).Publish(CreateConnection(), CreatePlan(), CreateSettings(ProfileGuid));
            Assert.AreEqual(PublishStageAlias.Verification, missing.Stage);
        }

        /// <summary>A server that refuses to overwrite gets the same-named target replaced only after the new copy is complete.</summary>
        [Test]
        public void Publish_ReplacesSameNamedTargetWhenServerRefusesOverwrite()
        {
            var factory = new FakeSftpSessionFactory();
            factory.Session.DirectoriesSet.Add(DestinationRoot);
            factory.Session.RenameFailsWhenTargetExists = true;
            factory.Session.FilesByPath[RemoteBuildFolder + "/catalog_1.json"] = "previous-catalog";
            var downloader = new FakeHashDownloader();
            downloader.TextByUrl[LoadUrl + "/catalog_1.hash"] = "hash-123";
            var result = CreatePublisher(factory, downloader).Publish(CreateConnection(), CreatePlan(), CreateSettings(ProfileGuid));
            Assert.AreEqual(PublishStatusAlias.Succeeded, result.Status, result.Message);
            Assert.AreEqual("{}", factory.Session.FilesByPath[RemoteBuildFolder + "/catalog_1.json"]);
        }

        /// <summary>A missing destination folder fails before any transfer.</summary>
        [Test]
        public void Publish_FailsBeforeUploadWhenDestinationIsMissing()
        {
            var factory = new FakeSftpSessionFactory();
            var result = CreatePublisher(factory, new FakeHashDownloader()).Publish(CreateConnection(), CreatePlan(), CreateSettings(ProfileGuid));
            Assert.AreEqual(PublishStageAlias.Connection, result.Stage);
            Assert.IsFalse(factory.Session.OperationsList.Any(operation => operation.StartsWith("upload ")));
        }
    }
}
