using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using SignalGarden.Editor;
using UnityEngine;

namespace SignalGarden.Tests
{
    public sealed class SignalGardenAssetProvenanceTests
    {
        private readonly List<string> temporaryRoots = new List<string>();

        [TearDown]
        public void CleanupFixtures()
        {
            foreach (var root in temporaryRoots)
            {
                if (Directory.Exists(root))
                {
                    Directory.Delete(root, true);
                }
            }

            temporaryRoots.Clear();
        }

        [Test]
        public void ValidManifestAndImportedPrefabChainPasses()
        {
            var record = LoadCurrentRecord();

            Assert.That(SignalGardenAssetProvenanceEditor.ValidateRecordForTests(record), Is.Empty);
        }

        [Test]
        public void MissingSourceFailsClosed()
        {
            var fixture = CreateFixture();
            File.Delete(Path.Combine(fixture.Root, SignalGardenAssetProvenance.SourceRelativePath.Replace('/', Path.DirectorySeparatorChar)));

            AssertContains(fixture.Validate(), "source");
        }

        [Test]
        public void StaleSourceHashFailsClosed()
        {
            var fixture = CreateFixture();
            var sourcePath = Path.Combine(fixture.Root, SignalGardenAssetProvenance.SourceRelativePath.Replace('/', Path.DirectorySeparatorChar));
            File.AppendAllText(sourcePath, "stale-source");

            AssertContains(fixture.Validate(), "source");
        }

        [Test]
        public void MismatchedFbxHashFailsClosed()
        {
            var fixture = CreateFixture();
            var fbxPath = Path.Combine(fixture.Root, SignalGardenAssetProvenance.FbxRelativePath.Replace('/', Path.DirectorySeparatorChar));
            var bytes = File.ReadAllBytes(fbxPath);
            bytes[bytes.Length - 1] ^= 0x01;
            File.WriteAllBytes(fbxPath, bytes);

            AssertContains(fixture.Validate(), "FBX export");
        }

        [Test]
        public void GuidMismatchFailsClosed()
        {
            var fixture = CreateFixture();
            var metaPath = Path.Combine(fixture.Root, SignalGardenAssetProvenance.FbxMetaRelativePath.Replace('/', Path.DirectorySeparatorChar));
            var lines = File.ReadAllLines(metaPath);
            for (var index = 0; index < lines.Length; index++)
            {
                if (lines[index].TrimStart().StartsWith("guid:", StringComparison.OrdinalIgnoreCase))
                {
                    lines[index] = "guid: 00000000000000000000000000000000";
                    break;
                }
            }

            File.WriteAllLines(metaPath, lines);
            AssertContains(fixture.Validate(), "GUID");
        }

        [Test]
        public void MissingPrefabFailsClosed()
        {
            var fixture = CreateFixture();
            File.Delete(Path.Combine(fixture.Root, SignalGardenAssetProvenance.PrefabRelativePath.Replace('/', Path.DirectorySeparatorChar)));

            AssertContains(fixture.Validate(), "prefab");
        }

        [Test]
        public void BudgetViolationFailsClosed()
        {
            var fixture = CreateFixture();
            fixture.Record.budgets.maxTriangles = fixture.Record.budgets.measuredTriangles - 1;
            fixture.WriteManifest();

            AssertContains(fixture.Validate(), "budget");
        }

        private static SignalGardenAssetProvenanceRecord LoadCurrentRecord()
        {
            var root = Directory.GetParent(Application.dataPath).FullName;
            SignalGardenAssetProvenanceRecord record;
            List<string> errors;
            Assert.That(SignalGardenAssetProvenance.TryLoadManifest(root, out record, out errors), Is.True, string.Join("; ", errors));
            return record;
        }

        private Fixture CreateFixture()
        {
            var root = Path.Combine(Path.GetTempPath(), "signal-garden-sg05-" + Guid.NewGuid().ToString("N"));
            temporaryRoots.Add(root);
            var sourceRoot = Directory.GetParent(Application.dataPath).FullName;
            var record = LoadCurrentRecord();

            CopyTracked(sourceRoot, root, SignalGardenAssetProvenance.SourceRelativePath);
            CopyTracked(sourceRoot, root, SignalGardenAssetProvenance.FbxRelativePath);
            CopyTracked(sourceRoot, root, SignalGardenAssetProvenance.FbxMetaRelativePath);
            CopyTracked(sourceRoot, root, SignalGardenAssetProvenance.PrefabRelativePath);
            CopyTracked(sourceRoot, root, SignalGardenAssetProvenance.PrefabRelativePath + ".meta");
            CopyTracked(sourceRoot, root, SignalGardenAssetProvenance.SceneRelativePath);

            var fixture = new Fixture(root, record);
            fixture.WriteManifest();
            return fixture;
        }

        private static void CopyTracked(string sourceRoot, string destinationRoot, string relativePath)
        {
            var sourcePath = Path.Combine(sourceRoot, relativePath.Replace('/', Path.DirectorySeparatorChar));
            var destinationPath = Path.Combine(destinationRoot, relativePath.Replace('/', Path.DirectorySeparatorChar));
            Directory.CreateDirectory(Path.GetDirectoryName(destinationPath));
            File.Copy(sourcePath, destinationPath);
        }

        private static void AssertContains(IReadOnlyCollection<string> errors, string expectedText)
        {
            Assert.That(errors.Any(error => error.IndexOf(expectedText, StringComparison.OrdinalIgnoreCase) >= 0), Is.True,
                "Expected an error containing '" + expectedText + "', got: " + string.Join("; ", errors));
        }

        private sealed class Fixture
        {
            public Fixture(string root, SignalGardenAssetProvenanceRecord record)
            {
                Root = root;
                Record = record;
            }

            public string Root { get; }
            public SignalGardenAssetProvenanceRecord Record { get; }

            public IReadOnlyCollection<string> Validate()
            {
                var errors = new List<string>();
                SignalGardenAssetProvenance.TryLoadManifest(Root, out _, out errors);
                return errors;
            }

            public void WriteManifest()
            {
                var path = Path.Combine(Root, SignalGardenAssetProvenance.ManifestRelativePath.Replace('/', Path.DirectorySeparatorChar));
                Directory.CreateDirectory(Path.GetDirectoryName(path));
                File.WriteAllText(path, JsonUtility.ToJson(Record, true));
            }
        }
    }
}
