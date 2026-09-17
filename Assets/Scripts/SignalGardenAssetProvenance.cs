using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;

namespace SignalGarden
{
    [Serializable]
    public sealed class SignalGardenAssetProvenanceRecord
    {
        public int schemaVersion = 1;
        public string assetId = "";
        public SignalGardenSourceIdentity source = new SignalGardenSourceIdentity();
        public SignalGardenExportIdentity export = new SignalGardenExportIdentity();
        public SignalGardenUnityIdentity unity = new SignalGardenUnityIdentity();
        public SignalGardenAssetBudgets budgets = new SignalGardenAssetBudgets();
        public SignalGardenApi10Evidence api10 = new SignalGardenApi10Evidence();
    }

    [Serializable]
    public sealed class SignalGardenSourceIdentity
    {
        public string relativePath = "";
        public string sha256 = "";
        public long byteLength;
        public string blenderVersion = "";
        public string sceneName = "";
        public string license = "";
        public string humanModificationStatus = "";
    }

    [Serializable]
    public sealed class SignalGardenExportIdentity
    {
        public string format = "FBX";
        public string relativePath = "";
        public string sha256 = "";
        public long byteLength;
        public string exporter = "";
        public string options = "";
        public string coordinateSystem = "";
        public string unitScale = "";
        public string namingConvention = "";
        public string materialDecision = "";
        public string animationDecision = "none";
        public string collisionDecision = "none";
    }

    [Serializable]
    public sealed class SignalGardenUnityIdentity
    {
        public string fbxMetaRelativePath = "";
        public string fbxGuid = "";
        public string prefabRelativePath = "";
        public string prefabGuid = "";
        public string sceneRelativePath = "Assets/Scenes/SignalGarden.unity";
        public string receiverChildName = "SignalGardenReceiver";
        public float scaleFactor = 1f;
        public bool useFileScale = true;
        public string meshCompression = "Off";
        public bool isReadable;
        public bool importAnimation;
        public bool importCameras;
        public bool importLights;
        public bool generateColliders;
        public bool preserveHierarchy = true;
        public string materialMap = "";
    }

    [Serializable]
    public sealed class SignalGardenAssetBudgets
    {
        public int maxTriangles = 5000;
        public int maxMaterialSlots = 4;
        public int maxTextureDimension = 1024;
        public long maxTextureBytes;
        public long maxSourceBytes = 10000000;
        public long maxExportBytes = 2000000;
        public int measuredTriangles;
        public int measuredMaterialSlots;
        public int measuredTextureCount;
        public int measuredMaxTextureDimension;
    }

    [Serializable]
    public sealed class SignalGardenApi10Evidence
    {
        public string repository = "setnessconsulting/project-blender-api";
        public string repositorySha = "";
        public string validationProfile = "realtime-prop";
        public string sceneInspectCommand = "";
        public string assetValidateCommand = "";
        public string auxiliaryGltfCommand = "";
        public string handoffCommand = "";
        public string status = "NOT_RUN";
        public string evidencePath = "";
    }

    public static class SignalGardenAssetProvenance
    {
        public const string AssetId = "signal-garden.receiver-shrine";
        public const string SourceRelativePath = "SourceArt/Blender/SignalGardenReceiver/SignalGardenReceiver.blend";
        public const string FbxRelativePath = "Assets/Art/SignalGarden/SignalGardenReceiver.fbx";
        public const string FbxMetaRelativePath = "Assets/Art/SignalGarden/SignalGardenReceiver.fbx.meta";
        public const string PrefabRelativePath = "Assets/Art/SignalGarden/SignalGardenReceiver.prefab";
        public const string SceneRelativePath = "Assets/Scenes/SignalGarden.unity";
        public const string ManifestRelativePath = "SourceArt/Blender/SignalGardenReceiver/SignalGardenReceiver.provenance.json";
        public const string ReceiverChildName = "SignalGardenReceiver";

        public static string ResolveRepositoryPath(string repositoryRoot, string relativePath)
        {
            if (string.IsNullOrWhiteSpace(repositoryRoot) || string.IsNullOrWhiteSpace(relativePath))
            {
                return "";
            }

            var root = Path.GetFullPath(repositoryRoot).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            var path = Path.GetFullPath(Path.Combine(root, relativePath.Replace('/', Path.DirectorySeparatorChar)));
            var prefix = root + Path.DirectorySeparatorChar;
            if (!path.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            {
                return "";
            }

            return path;
        }

        public static bool TryLoadManifest(string repositoryRoot, out SignalGardenAssetProvenanceRecord record, out List<string> errors)
        {
            errors = new List<string>();
            record = null;
            var path = ResolveRepositoryPath(repositoryRoot, ManifestRelativePath);
            if (string.IsNullOrEmpty(path) || !File.Exists(path))
            {
                errors.Add("Provenance manifest is missing: " + ManifestRelativePath);
                return false;
            }

            try
            {
                record = JsonUtility.FromJson<SignalGardenAssetProvenanceRecord>(File.ReadAllText(path, Encoding.UTF8));
            }
            catch (Exception exception)
            {
                errors.Add("Provenance manifest could not be parsed: " + exception.Message);
                return false;
            }

            if (record == null)
            {
                errors.Add("Provenance manifest is empty.");
                return false;
            }

            return ValidateTrackedFiles(repositoryRoot, record, errors);
        }

        public static bool ValidateTrackedFiles(string repositoryRoot, SignalGardenAssetProvenanceRecord record, List<string> errors)
        {
            if (record == null)
            {
                errors.Add("Provenance record is missing.");
                return false;
            }

            var budgets = record.budgets;
            if (record.schemaVersion != 1)
            {
                errors.Add("Unsupported provenance schema version: " + record.schemaVersion);
            }

            if (!string.Equals(record.assetId, AssetId, StringComparison.Ordinal))
            {
                errors.Add("Unexpected asset id: " + record.assetId);
            }

            if (record.source == null || string.IsNullOrWhiteSpace(record.source.relativePath))
            {
                errors.Add("Source identity is missing.");
            }
            else
            {
                if (!string.Equals(record.source.relativePath, SourceRelativePath, StringComparison.Ordinal))
                {
                    errors.Add("Source path must be " + SourceRelativePath + ".");
                }

                ValidateFile(repositoryRoot, record.source.relativePath, record.source.sha256, record.source.byteLength, "source", errors);
                if (budgets != null && record.source.byteLength > budgets.maxSourceBytes)
                {
                    errors.Add("Source exceeds the configured source byte budget.");
                }

                RequireValue(record.source.blenderVersion, "Blender version", errors);
                RequireValue(record.source.sceneName, "Blender source scene name", errors);
                RequireValue(record.source.license, "Source license/provenance", errors);
                RequireValue(record.source.humanModificationStatus, "Human-modification status", errors);
            }

            if (record.export == null || string.IsNullOrWhiteSpace(record.export.relativePath))
            {
                errors.Add("Export identity is missing.");
            }
            else
            {
                if (!string.Equals(record.export.format, "FBX", StringComparison.OrdinalIgnoreCase))
                {
                    errors.Add("Runtime export format must be FBX.");
                }

                if (!string.Equals(record.export.relativePath, FbxRelativePath, StringComparison.Ordinal))
                {
                    errors.Add("Runtime export path must be " + FbxRelativePath + ".");
                }

                ValidateFile(repositoryRoot, record.export.relativePath, record.export.sha256, record.export.byteLength, "FBX export", errors);
                if (budgets != null && record.export.byteLength > budgets.maxExportBytes)
                {
                    errors.Add("FBX export exceeds the configured export byte budget.");
                }

                RequireValue(record.export.exporter, "FBX exporter", errors);
                RequireValue(record.export.options, "FBX exporter options", errors);
                RequireValue(record.export.coordinateSystem, "FBX coordinate convention", errors);
                RequireValue(record.export.unitScale, "FBX unit scale", errors);
                RequireValue(record.export.namingConvention, "FBX naming convention", errors);
                RequireValue(record.export.materialDecision, "FBX material decision", errors);
                RequireValue(record.export.animationDecision, "FBX animation decision", errors);
                RequireValue(record.export.collisionDecision, "FBX collision decision", errors);
            }

            if (record.unity == null)
            {
                errors.Add("Unity import identity is missing.");
            }
            else
            {
                RequireValue(record.unity.fbxGuid, "Unity FBX GUID", errors);
                RequireValue(record.unity.prefabRelativePath, "Unity prefab path", errors);
                RequireValue(record.unity.prefabGuid, "Unity prefab GUID", errors);
                if (!string.Equals(record.unity.fbxMetaRelativePath, FbxMetaRelativePath, StringComparison.Ordinal))
                {
                    errors.Add("Unity FBX metadata path must be " + FbxMetaRelativePath + ".");
                }
                if (!string.Equals(record.unity.prefabRelativePath, PrefabRelativePath, StringComparison.Ordinal))
                {
                    errors.Add("Unity prefab path must be " + PrefabRelativePath + ".");
                }
                if (!string.Equals(record.unity.sceneRelativePath, SceneRelativePath, StringComparison.OrdinalIgnoreCase))
                {
                    errors.Add("Unity destination scene must be " + SceneRelativePath + ".");
                }

                ValidateMetaGuid(repositoryRoot, record.unity.fbxMetaRelativePath, record.unity.fbxGuid, "Unity FBX", errors);
                ValidateMetaGuid(repositoryRoot, record.unity.prefabRelativePath + ".meta", record.unity.prefabGuid, "Unity prefab", errors);
            }

            if (record.budgets == null)
            {
                errors.Add("Asset budgets are missing.");
            }
            else
            {
                if (record.budgets.maxTriangles <= 0 || record.budgets.maxMaterialSlots <= 0 || record.budgets.maxTextureDimension <= 0 ||
                    record.budgets.maxSourceBytes <= 0 || record.budgets.maxExportBytes <= 0)
                {
                    errors.Add("Asset budget limits must be positive.");
                }

                if (record.budgets.measuredTriangles < 0 || record.budgets.measuredMaterialSlots < 0 ||
                    record.budgets.measuredTextureCount < 0 || record.budgets.measuredMaxTextureDimension < 0)
                {
                    errors.Add("Measured asset budgets cannot be negative.");
                }

                if (record.budgets.measuredTriangles > record.budgets.maxTriangles)
                {
                    errors.Add("Measured triangles exceed the configured budget.");
                }

                if (record.budgets.measuredMaterialSlots > record.budgets.maxMaterialSlots)
                {
                    errors.Add("Measured material slots exceed the configured budget.");
                }

                if (record.budgets.measuredMaxTextureDimension > record.budgets.maxTextureDimension)
                {
                    errors.Add("Measured texture dimensions exceed the configured budget.");
                }
            }

            ValidateRequiredFile(repositoryRoot, FbxMetaRelativePath, "Unity FBX metadata", errors);
            ValidateRequiredFile(repositoryRoot, PrefabRelativePath, "Unity receiver prefab", errors);
            ValidateRequiredFile(repositoryRoot, SceneRelativePath, "SignalGarden scene", errors);

            if (record.api10 == null || string.IsNullOrWhiteSpace(record.api10.status))
            {
                errors.Add("API-10 evidence status is missing.");
            }
            else if (!string.Equals(record.api10.status, "PASS", StringComparison.OrdinalIgnoreCase) &&
                     !string.Equals(record.api10.status, "PARTIAL", StringComparison.OrdinalIgnoreCase) &&
                     !string.Equals(record.api10.status, "BLOCKED", StringComparison.OrdinalIgnoreCase) &&
                     !string.Equals(record.api10.status, "NOT_RUN", StringComparison.OrdinalIgnoreCase))
            {
                errors.Add("API-10 evidence status is not a recognized classification.");
            }

            return errors.Count == 0;
        }

        public static string ComputeSha256(string path)
        {
            using (var stream = File.OpenRead(path))
            using (var sha256 = SHA256.Create())
            {
                var bytes = sha256.ComputeHash(stream);
                var builder = new StringBuilder(bytes.Length * 2);
                for (var index = 0; index < bytes.Length; index++)
                {
                    builder.Append(bytes[index].ToString("X2"));
                }

                return builder.ToString();
            }
        }

        private static void ValidateFile(string repositoryRoot, string relativePath, string expectedHash, long expectedLength, string label, List<string> errors)
        {
            var path = ResolveRepositoryPath(repositoryRoot, relativePath);
            if (string.IsNullOrEmpty(path))
            {
                errors.Add(label + " path escapes the repository root.");
                return;
            }

            if (!File.Exists(path))
            {
                errors.Add(label + " is missing: " + relativePath);
                return;
            }

            var actualLength = new FileInfo(path).Length;
            if (expectedLength <= 0 || actualLength != expectedLength)
            {
                errors.Add(label + " byte length does not match the manifest.");
            }

            if (string.IsNullOrWhiteSpace(expectedHash))
            {
                errors.Add(label + " SHA-256 is missing from the manifest.");
                return;
            }

            var actualHash = ComputeSha256(path);
            if (!string.Equals(actualHash, expectedHash, StringComparison.OrdinalIgnoreCase))
            {
                errors.Add(label + " SHA-256 does not match the manifest.");
            }
        }

        private static void ValidateRequiredFile(string repositoryRoot, string relativePath, string label, List<string> errors)
        {
            var path = ResolveRepositoryPath(repositoryRoot, relativePath);
            if (string.IsNullOrEmpty(path))
            {
                errors.Add(label + " path escapes the repository root.");
            }
            else if (!File.Exists(path))
            {
                errors.Add(label + " is missing: " + relativePath);
            }
        }

        private static void ValidateMetaGuid(string repositoryRoot, string relativePath, string expectedGuid, string label, List<string> errors)
        {
            if (string.IsNullOrWhiteSpace(relativePath) || string.IsNullOrWhiteSpace(expectedGuid))
            {
                return;
            }

            var path = ResolveRepositoryPath(repositoryRoot, relativePath);
            if (string.IsNullOrEmpty(path) || !File.Exists(path))
            {
                return;
            }

            string actualGuid = null;
            foreach (var line in File.ReadAllLines(path))
            {
                var trimmed = line.Trim();
                if (!trimmed.StartsWith("guid:", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                actualGuid = trimmed.Substring("guid:".Length).Trim();
                break;
            }

            if (!string.Equals(actualGuid, expectedGuid, StringComparison.OrdinalIgnoreCase))
            {
                errors.Add(label + " GUID does not match the provenance manifest.");
            }
        }

        private static void RequireValue(string value, string label, List<string> errors)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                errors.Add(label + " is missing.");
            }
        }
    }
}
