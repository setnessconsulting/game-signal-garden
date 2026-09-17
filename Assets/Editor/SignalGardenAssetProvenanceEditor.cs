using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using SignalGarden;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace SignalGarden.Editor
{
    public static class SignalGardenAssetProvenanceEditor
    {
        private const string FbxAssetPath = SignalGardenAssetProvenance.FbxRelativePath;
        private const string PrefabAssetPath = SignalGardenAssetProvenance.PrefabRelativePath;

        [MenuItem("Signal Garden/Create SG-05 Receiver Prefab")]
        public static void CreateSignalGardenReceiverPrefab()
        {
            EnsureReceiverPrefab();
            Debug.Log("Signal Garden SG-05 receiver prefab created at " + PrefabAssetPath + ".");
        }

        [MenuItem("Signal Garden/Validate SG-05 Asset Provenance")]
        public static void ValidateSignalGardenAssetProvenance()
        {
            var errors = ValidateInternal(checkScene: true);
            if (errors.Count > 0)
            {
                throw new InvalidOperationException("Signal Garden SG-05 provenance validation failed:\n- " + string.Join("\n- ", errors));
            }

            Debug.Log("Signal Garden SG-05 asset provenance PASS.");
        }

        public static void ValidateForBuild()
        {
            var errors = ValidateInternal(checkScene: true);
            if (errors.Count > 0)
            {
                throw new InvalidOperationException("Signal Garden SG-05 provenance is required before a build:\n- " + string.Join("\n- ", errors));
            }
        }

        public static GameObject EnsureReceiverPrefab()
        {
            if (!File.Exists(ToAbsolutePath(FbxAssetPath)))
            {
                throw new InvalidOperationException("The SG-05 FBX is missing: " + FbxAssetPath);
            }

            AssetDatabase.ImportAsset(FbxAssetPath, ImportAssetOptions.ForceUpdate);
            var importer = AssetImporter.GetAtPath(FbxAssetPath) as ModelImporter;
            if (importer == null)
            {
                throw new InvalidOperationException("Unity did not import the SG-05 FBX as a model: " + FbxAssetPath);
            }

            ConfigureImporter(importer);
            importer.SaveAndReimport();
            AssetDatabase.ImportAsset(FbxAssetPath, ImportAssetOptions.ForceUpdate);

            var model = AssetDatabase.LoadAssetAtPath<GameObject>(FbxAssetPath);
            if (model == null)
            {
                throw new InvalidOperationException("Unity could not load the imported SG-05 model: " + FbxAssetPath);
            }

            EnsureFolder("Assets/Art");
            EnsureFolder("Assets/Art/SignalGarden");
            if (File.Exists(ToAbsolutePath(PrefabAssetPath)))
            {
                AssetDatabase.DeleteAsset(PrefabAssetPath);
            }

            var wrapper = new GameObject(SignalGardenAssetProvenance.ReceiverChildName);
            var instance = PrefabUtility.InstantiatePrefab(model) as GameObject;
            if (instance == null)
            {
                UnityEngine.Object.DestroyImmediate(wrapper);
                throw new InvalidOperationException("Unity could not instantiate the imported SG-05 model.");
            }

            instance.name = "Receiver Shrine Model";
            instance.transform.SetParent(wrapper.transform, false);
            PrefabUtility.UnpackPrefabInstance(instance, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
            AssignReceiverMaterials(wrapper);

            var prefab = PrefabUtility.SaveAsPrefabAsset(wrapper, PrefabAssetPath);
            UnityEngine.Object.DestroyImmediate(wrapper);
            AssetDatabase.SaveAssets();
            AssetDatabase.ImportAsset(PrefabAssetPath, ImportAssetOptions.ForceUpdate);
            if (prefab == null || AssetDatabase.LoadAssetAtPath<GameObject>(PrefabAssetPath) == null)
            {
                throw new InvalidOperationException("Unity could not save the SG-05 receiver prefab.");
            }

            WriteManifest();
            return AssetDatabase.LoadAssetAtPath<GameObject>(PrefabAssetPath);
        }

        public static Transform InstantiateReceiverVisual(Transform parent)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabAssetPath);
            if (prefab == null)
            {
                throw new InvalidOperationException("The SG-05 receiver prefab is missing: " + PrefabAssetPath);
            }

            var instance = PrefabUtility.InstantiatePrefab(prefab) as GameObject;
            if (instance == null)
            {
                throw new InvalidOperationException("The SG-05 receiver prefab could not be instantiated.");
            }

            instance.name = SignalGardenAssetProvenance.ReceiverChildName;
            instance.transform.SetParent(parent, false);
            return instance.transform;
        }

        public static void WriteManifest()
        {
            var root = RepositoryRoot;
            var sourcePath = SignalGardenAssetProvenance.ResolveRepositoryPath(root, SignalGardenAssetProvenance.SourceRelativePath);
            var fbxPath = SignalGardenAssetProvenance.ResolveRepositoryPath(root, SignalGardenAssetProvenance.FbxRelativePath);
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabAssetPath);
            if (string.IsNullOrEmpty(sourcePath) || string.IsNullOrEmpty(fbxPath) || !File.Exists(sourcePath) || !File.Exists(fbxPath) || prefab == null)
            {
                throw new InvalidOperationException("Cannot write SG-05 provenance until the source, FBX, and prefab exist.");
            }

            var record = LoadExistingRecord();
            record.schemaVersion = 1;
            record.assetId = SignalGardenAssetProvenance.AssetId;
            record.source.relativePath = SignalGardenAssetProvenance.SourceRelativePath;
            record.source.sha256 = SignalGardenAssetProvenance.ComputeSha256(sourcePath);
            record.source.byteLength = new FileInfo(sourcePath).Length;
            record.source.blenderVersion = "5.2.1 LTS (9e2066aef7ef)";
            record.source.sceneName = "SignalGardenReceiver";
            record.source.license = "Original Setness Consulting asset; no third-party content.";
            record.source.humanModificationStatus = "Script-assisted original design; owner visual review required.";

            record.export.format = "FBX";
            record.export.relativePath = SignalGardenAssetProvenance.FbxRelativePath;
            record.export.sha256 = SignalGardenAssetProvenance.ComputeSha256(fbxPath);
            record.export.byteLength = new FileInfo(fbxPath).Length;
            record.export.exporter = "Blender 5.2.1 LTS FBX exporter";
            record.export.options = "use_selection=true; object_types=MESH; use_mesh_modifiers=true; apply_unit_scale=true; apply_scale_options=FBX_SCALE_ALL; axis_forward=-Z; axis_up=Y; bake_anim=false; add_leaf_bones=false; use_custom_props=true; embed_textures=false.";
            record.export.coordinateSystem = "Blender right-handed Z-up exported with axis_forward=-Z and axis_up=Y for Unity.";
            record.export.unitScale = "1.0 meters";
            record.export.namingConvention = "SM_ mesh names and MAT_ material names.";
            record.export.materialDecision = "Three material slots mapped to existing URP materials; no texture files.";
            record.export.animationDecision = "none";
            record.export.collisionDecision = "none; route interaction uses the existing screen-space receiver marker.";

            record.unity.fbxMetaRelativePath = SignalGardenAssetProvenance.FbxMetaRelativePath;
            record.unity.fbxGuid = AssetDatabase.AssetPathToGUID(FbxAssetPath);
            record.unity.prefabRelativePath = SignalGardenAssetProvenance.PrefabRelativePath;
            record.unity.prefabGuid = AssetDatabase.AssetPathToGUID(PrefabAssetPath);
            record.unity.sceneRelativePath = SignalGardenAssetProvenance.SceneRelativePath;
            record.unity.receiverChildName = SignalGardenAssetProvenance.ReceiverChildName;
            record.unity.scaleFactor = 1f;
            record.unity.useFileScale = true;
            record.unity.meshCompression = ModelImporterMeshCompression.Off.ToString();
            record.unity.isReadable = false;
            record.unity.importAnimation = false;
            record.unity.importCameras = false;
            record.unity.importLights = false;
            record.unity.generateColliders = false;
            record.unity.preserveHierarchy = true;
            record.unity.materialMap = "Base -> Assets/Materials/Receiver-Pedestal.mat; Crystal -> Assets/Materials/Receiver-Sea-Glass.mat; Inlay/crown/fins -> Assets/Materials/Trail-Thread-Gold.mat.";

            var metrics = MeasurePrefab(prefab);
            record.budgets.maxTriangles = 5000;
            record.budgets.maxMaterialSlots = 4;
            record.budgets.maxTextureDimension = 1024;
            record.budgets.maxTextureBytes = 0;
            record.budgets.maxSourceBytes = 10000000;
            record.budgets.maxExportBytes = 2000000;
            record.budgets.measuredTriangles = metrics.triangles;
            record.budgets.measuredMaterialSlots = metrics.materialSlots;
            record.budgets.measuredTextureCount = metrics.textureCount;
            record.budgets.measuredMaxTextureDimension = metrics.maxTextureDimension;

            if (string.IsNullOrWhiteSpace(record.api10.repositorySha))
            {
                record.api10.repositorySha = "9783b64 (verify at qualification time)";
            }
            record.api10.validationProfile = "prop";
            if (string.IsNullOrWhiteSpace(record.api10.status))
            {
                record.api10.status = "NOT_RUN";
            }

            var manifestPath = SignalGardenAssetProvenance.ResolveRepositoryPath(root, SignalGardenAssetProvenance.ManifestRelativePath);
            Directory.CreateDirectory(Path.GetDirectoryName(manifestPath));
            File.WriteAllText(manifestPath, JsonUtility.ToJson(record, true));
        }

        public static List<string> ValidateFilesOnly(string repositoryRoot)
        {
            var errors = new List<string>();
            SignalGardenAssetProvenanceRecord record;
            if (!SignalGardenAssetProvenance.TryLoadManifest(repositoryRoot, out record, out errors))
            {
                return errors;
            }

            return errors;
        }

        /// <summary>
        /// Test hook for exercising fail-closed manifest and Unity import checks with a modified record.
        /// It does not write assets or change the project.
        /// </summary>
        public static List<string> ValidateRecordForTests(SignalGardenAssetProvenanceRecord record, bool checkScene = true)
        {
            var errors = new List<string>();
            SignalGardenAssetProvenance.ValidateTrackedFiles(RepositoryRoot, record, errors);
            if (errors.Count == 0)
            {
                ValidateUnityImporter(record, errors);
                ValidateImportedModel(record, errors);
                ValidatePrefab(record, errors);
                if (checkScene)
                {
                    ValidateScene(record, errors);
                }
            }

            return errors;
        }

        private static List<string> ValidateInternal(bool checkScene)
        {
            AssetDatabase.Refresh();
            var errors = new List<string>();
            SignalGardenAssetProvenanceRecord record;
            if (!SignalGardenAssetProvenance.TryLoadManifest(RepositoryRoot, out record, out errors))
            {
                return errors;
            }

            ValidateUnityImporter(record, errors);
            ValidateImportedModel(record, errors);
            ValidatePrefab(record, errors);
            if (checkScene)
            {
                ValidateScene(record, errors);
            }

            return errors;
        }

        private static void ValidateUnityImporter(SignalGardenAssetProvenanceRecord record, List<string> errors)
        {
            if (!File.Exists(ToAbsolutePath(SignalGardenAssetProvenance.FbxMetaRelativePath)))
            {
                errors.Add("Unity FBX metadata is missing.");
            }

            var actualFbxGuid = AssetDatabase.AssetPathToGUID(FbxAssetPath);
            if (!string.Equals(actualFbxGuid, record.unity.fbxGuid, StringComparison.OrdinalIgnoreCase))
            {
                errors.Add("Unity FBX GUID does not match the provenance manifest.");
            }

            var importer = AssetImporter.GetAtPath(FbxAssetPath) as ModelImporter;
            if (importer == null)
            {
                errors.Add("Unity ModelImporter is missing for the SG-05 FBX.");
                return;
            }

            if (Mathf.Abs(importer.globalScale - record.unity.scaleFactor) > 0.0001f)
            {
                errors.Add("Unity FBX scale factor does not match the manifest.");
            }

            if (importer.useFileScale != record.unity.useFileScale)
            {
                errors.Add("Unity FBX file-scale setting does not match the manifest.");
            }

            if (!string.Equals(importer.meshCompression.ToString(), record.unity.meshCompression, StringComparison.OrdinalIgnoreCase) ||
                importer.isReadable != record.unity.isReadable ||
                importer.importAnimation != record.unity.importAnimation ||
                importer.importCameras != record.unity.importCameras ||
                importer.importLights != record.unity.importLights ||
                importer.addCollider != record.unity.generateColliders ||
                importer.preserveHierarchy != record.unity.preserveHierarchy)
            {
                errors.Add("Unity FBX importer settings do not match the provenance manifest.");
            }
        }

        private static void ValidateImportedModel(SignalGardenAssetProvenanceRecord record, List<string> errors)
        {
            var model = AssetDatabase.LoadAssetAtPath<GameObject>(FbxAssetPath);
            if (model == null)
            {
                errors.Add("Imported SG-05 FBX model is missing.");
                return;
            }

            if (model.transform.localScale != Vector3.one || model.transform.localRotation != Quaternion.identity)
            {
                errors.Add("Imported SG-05 FBX root transform is not identity.");
            }

            var meshFilters = model.GetComponentsInChildren<MeshFilter>(true);
            if (meshFilters.Length == 0)
            {
                errors.Add("Imported SG-05 FBX contains no mesh objects.");
            }

            foreach (var filter in meshFilters)
            {
                if (!filter.name.StartsWith("SM_", StringComparison.Ordinal))
                {
                    errors.Add("Imported mesh name does not use the SM_ convention: " + filter.name);
                }

                if (filter.transform.localScale != Vector3.one)
                {
                    errors.Add("Imported mesh has unapplied scale: " + filter.name);
                }

                if (filter.sharedMesh == null || filter.sharedMesh.vertexCount == 0)
                {
                    errors.Add("Imported mesh has no geometry: " + filter.name);
                }
                else if (filter.sharedMesh.uv == null || filter.sharedMesh.uv.Length != filter.sharedMesh.vertexCount)
                {
                    errors.Add("Imported mesh is missing a complete UV channel: " + filter.name);
                }
            }

            foreach (var renderer in model.GetComponentsInChildren<Renderer>(true))
            {
                foreach (var material in renderer.sharedMaterials)
                {
                    if (material == null)
                    {
                        errors.Add("Imported SG-05 FBX has an empty material slot.");
                    }
                    else if (!material.name.StartsWith("MAT_", StringComparison.Ordinal))
                    {
                        errors.Add("Imported material name does not use the MAT_ convention: " + material.name);
                    }
                }
            }
        }

        private static void ValidatePrefab(SignalGardenAssetProvenanceRecord record, List<string> errors)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabAssetPath);
            if (prefab == null)
            {
                errors.Add("SG-05 receiver prefab is missing.");
                return;
            }

            var actualPrefabGuid = AssetDatabase.AssetPathToGUID(PrefabAssetPath);
            if (!string.Equals(actualPrefabGuid, record.unity.prefabGuid, StringComparison.OrdinalIgnoreCase))
            {
                errors.Add("Unity receiver prefab GUID does not match the provenance manifest.");
            }

            var metrics = MeasurePrefab(prefab);
            if (metrics.triangles != record.budgets.measuredTriangles)
            {
                errors.Add("Imported mesh triangle count does not match the provenance manifest.");
            }

            if (metrics.triangles > record.budgets.maxTriangles)
            {
                errors.Add("Imported mesh exceeds the triangle budget.");
            }

            if (metrics.materialSlots != record.budgets.measuredMaterialSlots || metrics.materialSlots > record.budgets.maxMaterialSlots)
            {
                errors.Add("Imported material slots do not match the configured budget.");
            }

            if (metrics.textureCount != 0)
            {
                errors.Add("SG-05 receiver unexpectedly contains texture assets.");
            }

            if (metrics.maxTextureDimension > record.budgets.maxTextureDimension)
            {
                errors.Add("SG-05 receiver texture dimensions exceed the configured budget.");
            }

            if (prefab.GetComponentsInChildren<Animator>(true).Length > 0 ||
                prefab.GetComponentsInChildren<Animation>(true).Length > 0 ||
                prefab.GetComponentsInChildren<Collider>(true).Length > 0)
            {
                errors.Add("SG-05 receiver prefab contains animation or collider components despite the static-asset contract.");
            }

            var renderers = prefab.GetComponentsInChildren<Renderer>(true);
            if (renderers.Length == 0 || renderers.Any(renderer => renderer.sharedMaterials.Any(material => material == null)))
            {
                errors.Add("SG-05 receiver prefab has missing renderer materials.");
            }
        }

        private static void ValidateScene(SignalGardenAssetProvenanceRecord record, List<string> errors)
        {
            var scene = EditorSceneManager.OpenScene(SignalGardenAssetProvenance.SceneRelativePath, OpenSceneMode.Single);
            if (!scene.IsValid())
            {
                errors.Add("SignalGarden scene could not be opened.");
                return;
            }

            var receiver = FindTransform(scene.GetRootGameObjects(), "Blue Receiver");
            if (receiver == null)
            {
                errors.Add("Blue Receiver marker is missing from the SignalGarden scene.");
                return;
            }

            var visual = receiver.Find(record.unity.receiverChildName);
            if (visual == null)
            {
                errors.Add("Imported SG-05 receiver prefab is not parented under the Blue Receiver marker.");
                return;
            }

            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabAssetPath);
            if (prefab != null && PrefabUtility.GetCorrespondingObjectFromSource(visual.gameObject) != prefab)
            {
                errors.Add("The receiver visual in SignalGarden is not linked to the tracked SG-05 prefab.");
            }
        }

        private static Transform FindTransform(IEnumerable<GameObject> roots, string name)
        {
            foreach (var root in roots)
            {
                var match = FindTransform(root.transform, name);
                if (match != null)
                {
                    return match;
                }
            }

            return null;
        }

        private static Transform FindTransform(Transform root, string name)
        {
            if (root.name == name)
            {
                return root;
            }

            foreach (Transform child in root)
            {
                var match = FindTransform(child, name);
                if (match != null)
                {
                    return match;
                }
            }

            return null;
        }

        private static void ConfigureImporter(ModelImporter importer)
        {
            importer.globalScale = 1f;
            importer.useFileScale = true;
            importer.meshCompression = ModelImporterMeshCompression.Off;
            importer.isReadable = false;
            importer.importAnimation = false;
            importer.importCameras = false;
            importer.importLights = false;
            importer.addCollider = false;
            importer.preserveHierarchy = true;
        }

        private static void AssignReceiverMaterials(GameObject root)
        {
            var pedestal = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Receiver-Pedestal.mat");
            var glass = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Receiver-Sea-Glass.mat");
            var accent = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Trail-Thread-Gold.mat");
            if (pedestal == null || glass == null || accent == null)
            {
                throw new InvalidOperationException("Existing receiver URP materials could not be loaded.");
            }

            foreach (var renderer in root.GetComponentsInChildren<Renderer>(true))
            {
                var name = renderer.gameObject.name;
                renderer.sharedMaterial = name.IndexOf("Crystal", StringComparison.OrdinalIgnoreCase) >= 0 ? glass :
                    name.IndexOf("Base", StringComparison.OrdinalIgnoreCase) >= 0 ? pedestal : accent;
            }
        }

        private static SignalGardenAssetProvenanceRecord LoadExistingRecord()
        {
            var path = SignalGardenAssetProvenance.ResolveRepositoryPath(RepositoryRoot, SignalGardenAssetProvenance.ManifestRelativePath);
            if (!File.Exists(path))
            {
                return new SignalGardenAssetProvenanceRecord();
            }

            try
            {
                var record = JsonUtility.FromJson<SignalGardenAssetProvenanceRecord>(File.ReadAllText(path));
                return record ?? new SignalGardenAssetProvenanceRecord();
            }
            catch
            {
                return new SignalGardenAssetProvenanceRecord();
            }
        }

        private static (int triangles, int materialSlots, int textureCount, int maxTextureDimension) MeasurePrefab(GameObject prefab)
        {
            var triangles = 0;
            var materials = new HashSet<Material>();
            var textures = new HashSet<Texture>();
            var maxTextureDimension = 0;
            foreach (var filter in prefab.GetComponentsInChildren<MeshFilter>(true))
            {
                if (filter.sharedMesh != null)
                {
                    triangles += filter.sharedMesh.triangles.Length / 3;
                }
            }

            foreach (var renderer in prefab.GetComponentsInChildren<Renderer>(true))
            {
                foreach (var material in renderer.sharedMaterials)
                {
                    if (material == null)
                    {
                        continue;
                    }

                    materials.Add(material);
                    foreach (var property in material.GetTexturePropertyNames())
                    {
                        var texture = material.GetTexture(property);
                            if (texture != null)
                            {
                                textures.Add(texture);
                                maxTextureDimension = Mathf.Max(maxTextureDimension, Mathf.Max(texture.width, texture.height));
                            }
                    }
                }
            }

            return (triangles, materials.Count, textures.Count, maxTextureDimension);
        }

        private static string RepositoryRoot
        {
            get { return Directory.GetParent(Application.dataPath).FullName; }
        }

        private static string ToAbsolutePath(string relativePath)
        {
            return SignalGardenAssetProvenance.ResolveRepositoryPath(RepositoryRoot, relativePath);
        }

        private static void EnsureFolder(string folder)
        {
            if (!AssetDatabase.IsValidFolder(folder))
            {
                var parent = Path.GetDirectoryName(folder).Replace('\\', '/');
                var name = Path.GetFileName(folder);
                AssetDatabase.CreateFolder(parent, name);
            }
        }
    }
}
