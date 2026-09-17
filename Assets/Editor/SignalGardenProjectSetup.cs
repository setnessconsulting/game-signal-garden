using System;
using System.Collections.Generic;
using System.IO;
using SignalGarden;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace SignalGarden.Editor
{
    public static class SignalGardenProjectSetup
    {
        private const string ScenePath = "Assets/Scenes/SignalGarden.unity";

        [MenuItem("Signal Garden/Generate First Playable Scene")]
        public static void GenerateFirstPlayableScene()
        {
            ConfigureProject();
            EnsureFolder("Assets/Generated");
            EnsureFolder("Assets/Generated/Meshes");
            EnsureFolder("Assets/Materials");
            EnsureFolder("Assets/Scenes");
            var materials = CreateMaterials();
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            ConfigureLighting();

            var island = new GameObject("Floating Garden");
            var islandForm = new GameObject("Strata and floating stone");
            islandForm.transform.SetParent(island.transform, false);
            CreateIslandSurface(island.transform, materials.topPalette);
            CreateIslandUnderside(islandForm.transform, materials);
            CreateGardenRim(island.transform, materials.edgeGlow);

            CreateTrail(island.transform, RouteRules.StandardTrail, materials.trailStone, materials.trailMarker, 0.78f, "Gold signal trail");
            CreateTrail(island.transform, RouteRules.DeadEndSpur, materials.deadEndStone, materials.deadEndGlow, 0.62f, "Blind spur");
            var source = CreateSource(island.transform, RouteRules.StandardTrail[0], materials);
            var receiver = CreateReceiver(island.transform, RouteRules.StandardTrail[RouteRules.StandardTrail.Length - 1], materials);
            CreateGardenDetails(island.transform, materials);

            var focus = new GameObject("Camera focus");
            focus.transform.position = new Vector3(0f, -0.30f, 0f);
            var camera = CreateCamera(focus.transform);
            CreateKeyLight(island.transform);
            var sourceLight = CreatePointLight("Source glow", new Vector3(source.position.x, 1.1f, source.position.z), new Color(1f, 0.30f, 0.23f), 1.35f, 5.0f);
            var receiverLight = CreatePointLight("Receiver glow", new Vector3(receiver.position.x, 1.15f, receiver.position.z), new Color(0.34f, 0.91f, 0.86f), 0.72f, 4.0f);

            var routeObject = new GameObject("Player signal line");
            var routeLine = routeObject.AddComponent<LineRenderer>();
            routeLine.enabled = false;
            routeLine.useWorldSpace = true;
            routeLine.alignment = LineAlignment.View;
            routeLine.textureMode = LineTextureMode.Stretch;
            routeLine.widthMultiplier = 0.14f;
            routeLine.numCornerVertices = 4;
            routeLine.numCapVertices = 5;
            routeLine.shadowCastingMode = ShadowCastingMode.Off;
            routeLine.receiveShadows = false;
            routeLine.sharedMaterial = materials.activeSignal;

            var audioObject = new GameObject("Optional feedback cues");
            var audio = audioObject.AddComponent<AudioSource>();
            audio.playOnAwake = false;
            audio.spatialBlend = 0f;
            audio.volume = 0.65f;

            var gameObject = new GameObject("Signal Garden Game");
            var game = gameObject.AddComponent<SignalGardenGame>();
            game.Configure(
                camera,
                focus.transform,
                source,
                receiver,
                routeLine,
                sourceLight,
                receiverLight,
                materials.activeSignal,
                materials.failedSignal,
                materials.successSignal,
                audio);

            EditorBuildSettings.scenes = new[]
            {
                new EditorBuildSettingsScene(ScenePath, true)
            };
            var sceneAsset = EditorSceneManager.SaveScene(scene, ScenePath);
            if (!sceneAsset)
            {
                throw new InvalidOperationException("Could not save the SignalGarden scene.");
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("Signal Garden scene generated at " + ScenePath + ".");
        }

        public static void BuildWebGL()
        {
            ConfigureProject();
            if (!HasGeneratedGardenScene())
            {
                GenerateFirstPlayableScene();
            }

            EditorBuildSettings.scenes = new[]
            {
                new EditorBuildSettingsScene(ScenePath, true)
            };
            AssetDatabase.SaveAssets();

            var projectRoot = Directory.GetParent(Application.dataPath).FullName;
            var output = Path.Combine(projectRoot, "Builds", "WebGL");
            Directory.CreateDirectory(output);
            var options = new BuildPlayerOptions
            {
                scenes = new[] { ScenePath },
                locationPathName = output,
                target = BuildTarget.WebGL,
                options = BuildOptions.None
            };
            var report = BuildPipeline.BuildPlayer(options);
            if (report.summary.result != BuildResult.Succeeded)
            {
                throw new InvalidOperationException("WebGL build finished with result " + report.summary.result + ".");
            }

            Debug.Log("Signal Garden WebGL build succeeded: " + output + " (" + report.summary.totalSize + " bytes).");
        }

        private static bool HasGeneratedGardenScene()
        {
            if (!File.Exists(ScenePath))
            {
                return false;
            }

            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            if (!scene.IsValid())
            {
                return false;
            }

            foreach (var root in scene.GetRootGameObjects())
            {
                if (root.name == "Floating Garden")
                {
                    return true;
                }
            }

            return false;
        }

        private static void ConfigureProject()
        {
            EditorSettings.serializationMode = SerializationMode.ForceText;
            PlayerSettings.companyName = "Setness Consulting";
            PlayerSettings.productName = "SignalGarden";
            PlayerSettings.defaultWebScreenWidth = 1920;
            PlayerSettings.defaultWebScreenHeight = 1080;
            PlayerSettings.runInBackground = true;
            PlayerSettings.colorSpace = ColorSpace.Linear;
            PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Brotli;
            PlayerSettings.WebGL.decompressionFallback = false;
            QualitySettings.antiAliasing = 4;
            QualitySettings.vSyncCount = 0;
        }

        private static void ConfigureLighting()
        {
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = Hex("#9BC5BD");
            RenderSettings.ambientEquatorColor = Hex("#557B73");
            RenderSettings.ambientGroundColor = Hex("#223D40");
            RenderSettings.reflectionIntensity = 0.22f;
            RenderSettings.fog = false;
        }

        private static GardenMaterials CreateMaterials()
        {
            var materials = new GardenMaterials
            {
                topPalette = new[]
                {
                    MaterialAsset("Garden Moss Deep", "#426B5C", 0.12f),
                    MaterialAsset("Garden Moss Fern", "#547D62", 0.14f),
                    MaterialAsset("Garden Moss Sage", "#678B68", 0.16f),
                    MaterialAsset("Garden Moss Sunlit", "#779771", 0.18f)
                },
                clayUpper = MaterialAsset("Island Clay Ochre", "#A9674C", 0.24f),
                clayMiddle = MaterialAsset("Island Clay Rose", "#854E45", 0.23f),
                clayLower = MaterialAsset("Island Stone Deep", "#344C51", 0.25f),
                edgeGlow = MaterialAsset("Garden Edge Glaze", "#73B6A4", 0.32f, "#347D75", 0.25f),
                trailStone = MaterialAsset("Trail Stone Warm", "#D5BC80", 0.30f, "#B48637", 0.16f),
                trailMarker = MaterialAsset("Trail Thread Gold", "#F4D88B", 0.3f, "#F5B843", 1.05f),
                deadEndStone = MaterialAsset("Blind Spur Stone", "#788A80", 0.24f),
                deadEndGlow = MaterialAsset("Blind Spur Signal", "#9DAF9E", 0.32f, "#7F9588", 0.10f),
                sourceBase = MaterialAsset("Source Pedestal", "#4F5D56", 0.35f),
                sourceOrb = MaterialAsset("Source Coral Glass", "#FA705F", 0.48f, "#FF4938", 1.35f),
                receiverBase = MaterialAsset("Receiver Pedestal", "#425F64", 0.42f),
                receiverCrystal = MaterialAsset("Receiver Sea Glass", "#76DBCF", 0.62f, "#33D7C3", 0.52f),
                activeSignal = MaterialAsset("Active Signal Ribbon", "#C5F4D8", 0.25f, "#63FFD6", 1.8f),
                failedSignal = MaterialAsset("Faded Signal Ribbon", "#FF9A75", 0.18f, "#F1544B", 0.85f),
                successSignal = MaterialAsset("Living Signal Ribbon", "#DBF9B3", 0.36f, "#A6FF87", 2.0f),
                leafA = MaterialAsset("Fern Leaf Jade", "#377B62", 0.16f),
                leafB = MaterialAsset("Fern Leaf Green", "#5C936A", 0.17f),
                leafC = MaterialAsset("Fern Leaf Soft", "#90A76C", 0.18f),
                flowerCoral = MaterialAsset("Wildflower Coral", "#E77D6D", 0.24f, "#B74048", 0.10f),
                flowerCream = MaterialAsset("Wildflower Cream", "#F0D69A", 0.24f),
                rock = MaterialAsset("Garden Stone", "#647A70", 0.18f),
                rockLight = MaterialAsset("Garden Stone Light", "#9AA58A", 0.20f)
            };
            return materials;
        }

        private static Material MaterialAsset(string name, string colorHex, float smoothness, string emissionHex = null, float emission = 0f)
        {
            var path = "Assets/Materials/" + name.Replace(' ', '-') + ".mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            var shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null)
            {
                shader = Shader.Find("Standard");
            }

            if (material == null)
            {
                material = new Material(shader) { name = name };
                AssetDatabase.CreateAsset(material, path);
            }

            var baseColor = Hex(colorHex);
            if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", baseColor);
            if (material.HasProperty("_Color")) material.SetColor("_Color", baseColor);
            if (material.HasProperty("_Smoothness")) material.SetFloat("_Smoothness", smoothness);
            if (material.HasProperty("_Metallic")) material.SetFloat("_Metallic", 0.03f);
            if (!string.IsNullOrEmpty(emissionHex) && emission > 0f)
            {
                material.EnableKeyword("_EMISSION");
                var emissionColor = Hex(emissionHex) * emission;
                if (material.HasProperty("_EmissionColor")) material.SetColor("_EmissionColor", emissionColor);
            }

            EditorUtility.SetDirty(material);
            return material;
        }

        private static Camera CreateCamera(Transform focus)
        {
            var cameraObject = new GameObject("Garden Camera");
            var camera = cameraObject.AddComponent<Camera>();
            camera.tag = "MainCamera";
            camera.orthographic = true;
            camera.orthographicSize = 4.70f;
            camera.nearClipPlane = 0.1f;
            camera.farClipPlane = 100f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = Hex("#12323B");
            camera.allowHDR = true;
            camera.allowMSAA = true;
            camera.useOcclusionCulling = false;
            cameraObject.transform.position = new Vector3(0f, 11.8f, -13.4f);
            cameraObject.transform.LookAt(focus.position);
            cameraObject.AddComponent<AudioListener>();
            return camera;
        }

        private static Light CreateKeyLight(Transform parent)
        {
            var lightObject = new GameObject("Warm canopy light");
            lightObject.transform.SetParent(parent, false);
            lightObject.transform.rotation = Quaternion.Euler(47f, -34f, 0f);
            var light = lightObject.AddComponent<Light>();
            light.type = LightType.Directional;
            light.color = Hex("#FFE0B0");
            light.intensity = 1.5f;
            light.shadows = LightShadows.Soft;
            light.shadowStrength = 0.45f;
            return light;
        }

        private static Light CreatePointLight(string name, Vector3 position, Color color, float intensity, float range)
        {
            var lightObject = new GameObject(name);
            lightObject.transform.position = position;
            var light = lightObject.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = color;
            light.intensity = intensity;
            light.range = range;
            light.shadows = LightShadows.None;
            return light;
        }

        private static Transform CreateSource(Transform parent, Vector2 point, GardenMaterials materials)
        {
            var root = new GameObject("Coral Source");
            root.transform.SetParent(parent, false);
            root.transform.position = new Vector3(point.x, 0f, point.y);
            CreatePrimitive(PrimitiveType.Cylinder, "Source stone socket", root.transform,
                new Vector3(0f, 0.52f, 0f), new Vector3(0.94f, 0.075f, 0.94f), materials.sourceBase);
            CreatePrimitive(PrimitiveType.Cylinder, "Source inner ring", root.transform,
                new Vector3(0f, 0.64f, 0f), new Vector3(0.68f, 0.045f, 0.68f), materials.trailStone);
            CreatePrimitive(PrimitiveType.Sphere, "Coral signal", root.transform,
                new Vector3(0f, 1.07f, 0f), new Vector3(0.48f, 0.48f, 0.48f), materials.sourceOrb);
            CreatePrimitive(PrimitiveType.Cylinder, "Source stem", root.transform,
                new Vector3(0f, 0.80f, 0f), new Vector3(0.08f, 0.14f, 0.08f), materials.sourceBase);
            CreatePrimitive(PrimitiveType.Sphere, "Source glint", root.transform,
                new Vector3(-0.10f, 1.19f, -0.18f), new Vector3(0.09f, 0.09f, 0.09f), materials.flowerCream);
            return root.transform;
        }

        private static Transform CreateReceiver(Transform parent, Vector2 point, GardenMaterials materials)
        {
            var root = new GameObject("Blue Receiver");
            root.transform.SetParent(parent, false);
            root.transform.position = new Vector3(point.x, 0f, point.y);
            CreatePrimitive(PrimitiveType.Cylinder, "Receiver stone socket", root.transform,
                new Vector3(0f, 0.52f, 0f), new Vector3(0.98f, 0.075f, 0.98f), materials.receiverBase);
            CreatePrimitive(PrimitiveType.Cylinder, "Receiver glass dish", root.transform,
                new Vector3(0f, 0.64f, 0f), new Vector3(0.72f, 0.045f, 0.72f), materials.trailStone);
            var crystalMesh = StoreMesh(BuildCrystalMesh(), "Assets/Generated/Meshes/ReceiverCrystal.asset");
            var crystal = new GameObject("Receiver crystal");
            crystal.transform.SetParent(root.transform, false);
            crystal.transform.localPosition = new Vector3(0f, 1.06f, 0f);
            crystal.transform.localScale = new Vector3(0.72f, 0.78f, 0.72f);
            crystal.AddComponent<MeshFilter>().sharedMesh = crystalMesh;
            crystal.AddComponent<MeshRenderer>().sharedMaterial = materials.receiverCrystal;
            CreatePrimitive(PrimitiveType.Sphere, "Receiver heart", root.transform,
                new Vector3(0f, 0.87f, 0f), new Vector3(0.26f, 0.26f, 0.26f), materials.receiverCrystal);
            CreatePrimitive(PrimitiveType.Sphere, "Receiver sparkle", root.transform,
                new Vector3(0.16f, 1.20f, -0.17f), new Vector3(0.08f, 0.08f, 0.08f), materials.flowerCream);
            return root.transform;
        }

        private static void CreateIslandSurface(Transform parent, Material[] palette)
        {
            var mesh = StoreMesh(BuildIslandSurfaceMesh(), "Assets/Generated/Meshes/GardenTop.asset");
            var surface = new GameObject("Faceted moss surface");
            surface.transform.SetParent(parent, false);
            var filter = surface.AddComponent<MeshFilter>();
            filter.sharedMesh = mesh;
            var renderer = surface.AddComponent<MeshRenderer>();
            renderer.sharedMaterials = palette;
            renderer.receiveShadows = true;
        }

        private static Mesh BuildIslandSurfaceMesh()
        {
            const int segments = 96;
            const int rings = 9;
            var vertices = new List<Vector3>(segments * rings * 6 * 3);
            var submeshIndices = new List<int>[4];
            for (var i = 0; i < submeshIndices.Length; i++)
            {
                submeshIndices[i] = new List<int>();
            }

            Vector3 At(float radius, float angle)
            {
                var wobble = 1f + 0.025f * Mathf.Sin(angle * 5f) + 0.012f * Mathf.Cos(angle * 9f);
                var x = Mathf.Cos(angle) * 5.05f * radius * wobble;
                var z = Mathf.Sin(angle) * 3.30f * radius * (1f + 0.018f * Mathf.Sin(angle * 7f));
                var y = 0.26f +
                        0.045f * Mathf.Sin(angle * 3f + radius * 4f) +
                        0.026f * Mathf.Cos(angle * 5f - radius * 3f) +
                        0.020f * Mathf.Sin(radius * 11f + angle * 6f);
                return new Vector3(x, y, z);
            }

            void AddTriangle(Vector3 first, Vector3 second, Vector3 third, int paletteIndex)
            {
                var start = vertices.Count;
                vertices.Add(first);
                vertices.Add(second);
                vertices.Add(third);
                submeshIndices[paletteIndex].Add(start);
                submeshIndices[paletteIndex].Add(start + 1);
                submeshIndices[paletteIndex].Add(start + 2);
            }

            var center = new Vector3(0f, 0.25f, 0f);
            for (var segment = 0; segment < segments; segment++)
            {
                var angleA = segment * Mathf.PI * 2f / segments;
                var angleB = (segment + 1) * Mathf.PI * 2f / segments;
                var edgeA = At(1f / rings, angleA);
                var edgeB = At(1f / rings, angleB);
                AddTriangle(center, edgeB, edgeA, (segment * 3) % submeshIndices.Length);
            }

            for (var ring = 0; ring < rings - 1; ring++)
            {
                var innerRadius = (ring + 1f) / rings;
                var outerRadius = (ring + 2f) / rings;
                for (var segment = 0; segment < segments; segment++)
                {
                    var angleA = segment * Mathf.PI * 2f / segments;
                    var angleB = (segment + 1) * Mathf.PI * 2f / segments;
                    var innerA = At(innerRadius, angleA);
                    var innerB = At(innerRadius, angleB);
                    var outerA = At(outerRadius, angleA);
                    var outerB = At(outerRadius, angleB);
                    var paletteIndex = Mathf.Abs((ring * 7 + segment * 3 + (segment % 5) * ring) % submeshIndices.Length);
                    AddTriangle(innerA, outerB, outerA, paletteIndex);
                    AddTriangle(innerA, innerB, outerB, (paletteIndex + ((segment + ring) % 3 == 0 ? 1 : 0)) % submeshIndices.Length);
                }
            }

            var mesh = new Mesh { name = "Faceted Garden Top" };
            mesh.SetVertices(vertices);
            mesh.subMeshCount = submeshIndices.Length;
            for (var i = 0; i < submeshIndices.Length; i++)
            {
                mesh.SetTriangles(submeshIndices[i], i, true);
            }

            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }

        private static Mesh BuildCrystalMesh()
        {
            var vertices = new List<Vector3>();
            var triangles = new List<int>();
            const int sides = 7;
            var top = new Vector3(0f, 1f, 0f);
            var bottom = new Vector3(0f, -0.88f, 0f);
            var ring = new Vector3[sides];
            for (var i = 0; i < sides; i++)
            {
                var angle = i * Mathf.PI * 2f / sides;
                var radius = i % 2 == 0 ? 0.62f : 0.54f;
                ring[i] = new Vector3(Mathf.Cos(angle) * radius, -0.08f + (i % 2 == 0 ? 0.06f : -0.03f), Mathf.Sin(angle) * radius);
            }

            void AddOutward(Vector3 a, Vector3 b, Vector3 c)
            {
                var normal = Vector3.Cross(b - a, c - a);
                var center = (a + b + c) / 3f;
                if (Vector3.Dot(normal, center) < 0f)
                {
                    var swap = b;
                    b = c;
                    c = swap;
                }

                var start = vertices.Count;
                vertices.Add(a);
                vertices.Add(b);
                vertices.Add(c);
                triangles.Add(start);
                triangles.Add(start + 1);
                triangles.Add(start + 2);
            }

            for (var i = 0; i < sides; i++)
            {
                var next = (i + 1) % sides;
                AddOutward(top, ring[i], ring[next]);
                AddOutward(bottom, ring[next], ring[i]);
            }

            var mesh = new Mesh { name = "Receiver Sea Glass Crystal" };
            mesh.SetVertices(vertices);
            mesh.SetTriangles(triangles, 0);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }

        private static Mesh StoreMesh(Mesh generated, string path)
        {
            var existing = AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if (existing == null)
            {
                AssetDatabase.CreateAsset(generated, path);
                return generated;
            }

            existing.Clear();
            existing.indexFormat = generated.indexFormat;
            existing.vertices = generated.vertices;
            existing.subMeshCount = generated.subMeshCount;
            for (var i = 0; i < generated.subMeshCount; i++)
            {
                existing.SetTriangles(generated.GetTriangles(i), i, true);
            }

            existing.RecalculateNormals();
            existing.RecalculateBounds();
            EditorUtility.SetDirty(existing);
            UnityEngine.Object.DestroyImmediate(generated);
            return existing;
        }

        private static void CreateIslandUnderside(Transform parent, GardenMaterials materials)
        {
            CreatePrimitive(PrimitiveType.Sphere, "Upper warm earth band", parent,
                new Vector3(0f, -0.40f, 0f), new Vector3(9.95f, 1.17f, 6.45f), materials.clayUpper);
            CreatePrimitive(PrimitiveType.Sphere, "Rose clay stratum", parent,
                new Vector3(0f, -1.08f, 0f), new Vector3(8.55f, 1.45f, 5.58f), materials.clayMiddle);
            CreatePrimitive(PrimitiveType.Sphere, "Deep floating stone", parent,
                new Vector3(0f, -1.82f, 0f), new Vector3(6.75f, 1.35f, 4.40f), materials.clayLower);

            var shards = new[]
            {
                new Vector3(-3.4f, -2.56f, 0.4f), new Vector3(-2.1f, -2.66f, 1.3f),
                new Vector3(2.9f, -2.54f, 0.7f), new Vector3(3.3f, -2.43f, -0.8f),
                new Vector3(-3.1f, -2.45f, -0.9f), new Vector3(0.8f, -2.71f, 1.55f),
                new Vector3(-0.8f, -2.52f, -1.7f)
            };
            for (var i = 0; i < shards.Length; i++)
            {
                var rock = CreatePrimitive(PrimitiveType.Cube, "Suspended basalt shard " + (i + 1), parent,
                    shards[i], new Vector3(0.34f + (i % 3) * 0.08f, 0.20f, 0.31f + (i % 2) * 0.10f),
                    i % 2 == 0 ? materials.clayMiddle : materials.clayLower);
                rock.transform.rotation = Quaternion.Euler(i * 11f, i * 29f, i * 7f);
            }
        }

        private static void CreateGardenRim(Transform parent, Material material)
        {
            var points = new Vector3[97];
            for (var i = 0; i < points.Length; i++)
            {
                var angle = i * Mathf.PI * 2f / (points.Length - 1);
                var wobble = 1f + 0.025f * Mathf.Sin(angle * 5f) + 0.012f * Mathf.Cos(angle * 9f);
                points[i] = new Vector3(
                    Mathf.Cos(angle) * 5.02f * wobble,
                    0.25f + 0.045f * Mathf.Sin(angle * 3f + 4f) + 0.026f * Mathf.Cos(angle * 5f - 3f),
                    Mathf.Sin(angle) * 3.28f * (1f + 0.018f * Mathf.Sin(angle * 7f)));
            }

            CreateLine("Glazed garden edge", parent, points, material, 0.075f);
        }

        private static void CreateTrail(
            Transform parent,
            IList<Vector2> points,
            Material stone,
            Material marker,
            float width,
            string groupName)
        {
            var group = new GameObject(groupName);
            group.transform.SetParent(parent, false);
            for (var i = 0; i < points.Count - 1; i++)
            {
                var start = points[i];
                var end = points[i + 1];
                var direction = new Vector3(end.x - start.x, 0f, end.y - start.y);
                var midpoint = new Vector3((start.x + end.x) * 0.5f, 0.35f, (start.y + end.y) * 0.5f);
                var segment = CreatePrimitive(PrimitiveType.Cube, groupName + " path stone " + (i + 1), group.transform,
                    midpoint, new Vector3(width, 0.11f, direction.magnitude + 0.18f), stone);
                segment.transform.rotation = Quaternion.LookRotation(direction.normalized, Vector3.up);
            }

            for (var i = 0; i < points.Count; i++)
            {
                var point = points[i];
                CreatePrimitive(PrimitiveType.Cylinder, groupName + " waypoint " + (i + 1), group.transform,
                    new Vector3(point.x, 0.405f, point.y), new Vector3(width * 0.66f, 0.035f, width * 0.66f), stone);
            }

            var thread = new Vector3[points.Count];
            for (var i = 0; i < points.Count; i++)
            {
                thread[i] = new Vector3(points[i].x, 0.445f, points[i].y);
            }

            CreateLine(groupName + " center thread", group.transform, thread, marker, groupName == "Blind spur" ? 0.035f : 0.045f);
            if (groupName == "Blind spur")
            {
                var end = points[points.Count - 1];
                CreatePrimitive(PrimitiveType.Sphere, "Quiet dead end", group.transform,
                    new Vector3(end.x, 0.60f, end.y), new Vector3(0.30f, 0.30f, 0.30f), stone);
                CreatePrimitive(PrimitiveType.Cube, "Dead end cross mark", group.transform,
                    new Vector3(end.x, 0.80f, end.y), new Vector3(0.34f, 0.04f, 0.06f), marker);
            }
        }

        private static void CreateGardenDetails(Transform parent, GardenMaterials materials)
        {
            var plants = new[]
            {
                new Vector2(-4.40f, 0.72f), new Vector2(-4.05f, -0.92f), new Vector2(-3.68f, -2.15f),
                new Vector2(-2.02f, -2.72f), new Vector2(-0.96f, -2.84f), new Vector2(0.86f, 2.67f),
                new Vector2(1.55f, 2.44f), new Vector2(3.72f, 1.95f), new Vector2(4.18f, 0.55f),
                new Vector2(4.15f, -0.96f), new Vector2(-1.78f, 2.72f), new Vector2(-3.95f, 1.05f)
            };

            for (var i = 0; i < plants.Length; i++)
            {
                if (RouteRules.DistanceToTrail(plants[i], RouteRules.StandardTrail) < 0.86f ||
                    RouteRules.DistanceToTrail(plants[i], RouteRules.DeadEndSpur) < 0.70f)
                {
                    continue;
                }

                var group = new GameObject("Wild growth " + (i + 1));
                group.transform.SetParent(parent, false);
                group.transform.localPosition = new Vector3(plants[i].x, 0.31f, plants[i].y);
                CreatePlant(group.transform, i, materials);
            }

            var stonePositions = new[]
            {
                new Vector2(-4.55f, -1.75f), new Vector2(-3.3f, 2.75f), new Vector2(-2.15f, -1.95f),
                new Vector2(-0.35f, 2.82f), new Vector2(1.0f, 1.96f), new Vector2(2.9f, 2.15f),
                new Vector2(4.55f, -0.02f), new Vector2(3.85f, -1.7f), new Vector2(0.95f, -2.9f),
                new Vector2(-3.0f, -2.55f)
            };
            for (var i = 0; i < stonePositions.Length; i++)
            {
                if (RouteRules.DistanceToTrail(stonePositions[i], RouteRules.StandardTrail) < 0.68f)
                {
                    continue;
                }

                var rock = CreatePrimitive(PrimitiveType.Sphere, "Moss-polished pebble " + (i + 1), parent,
                    new Vector3(stonePositions[i].x, 0.42f, stonePositions[i].y),
                    new Vector3(0.34f + (i % 3) * 0.08f, 0.18f + (i % 2) * 0.06f, 0.29f),
                    i % 3 == 0 ? materials.rockLight : materials.rock);
                rock.transform.rotation = Quaternion.Euler(i * 7f, i * 19f, i * 5f);
            }
        }

        private static void CreatePlant(Transform parent, int seed, GardenMaterials materials)
        {
            var height = 0.78f + (seed % 4) * 0.11f;
            var trunk = CreatePrimitive(PrimitiveType.Cylinder, "Fern stem", parent,
                new Vector3(0f, 0.18f, 0f), new Vector3(0.075f, 0.24f, 0.075f), materials.clayMiddle);
            trunk.transform.rotation = Quaternion.Euler(0f, seed * 23f, (seed % 3 - 1) * 5f);
            var canopyMaterial = seed % 3 == 0 ? materials.leafC : seed % 2 == 0 ? materials.leafB : materials.leafA;
            CreatePrimitive(PrimitiveType.Sphere, "Fern crown", parent,
                new Vector3(0f, height * 0.62f, 0f), new Vector3(0.47f, height * 0.60f, 0.46f), canopyMaterial);
            CreatePrimitive(PrimitiveType.Sphere, "Fern side leaf", parent,
                new Vector3(0.25f, height * 0.40f, -0.17f), new Vector3(0.38f, 0.26f, 0.33f), materials.leafB);
            CreatePrimitive(PrimitiveType.Sphere, "Wildflower center", parent,
                new Vector3(-0.13f, height + 0.06f, 0.10f), new Vector3(0.14f, 0.14f, 0.14f), materials.flowerCream);
            var petalMaterial = seed % 2 == 0 ? materials.flowerCoral : materials.flowerCream;
            for (var i = 0; i < 5; i++)
            {
                var angle = i * Mathf.PI * 2f / 5f;
                var petal = new Vector3(-0.13f + Mathf.Cos(angle) * 0.18f, height + 0.045f, 0.10f + Mathf.Sin(angle) * 0.18f);
                CreatePrimitive(PrimitiveType.Sphere, "Wildflower petal " + (i + 1), parent,
                    petal, new Vector3(0.16f, 0.11f, 0.14f), petalMaterial);
            }
        }

        private static GameObject CreatePrimitive(
            PrimitiveType primitive,
            string name,
            Transform parent,
            Vector3 localPosition,
            Vector3 localScale,
            Material material)
        {
            var gameObject = GameObject.CreatePrimitive(primitive);
            gameObject.name = name;
            gameObject.transform.SetParent(parent, false);
            gameObject.transform.localPosition = localPosition;
            gameObject.transform.localScale = localScale;
            if (material != null)
            {
                var renderer = gameObject.GetComponent<Renderer>();
                renderer.sharedMaterial = material;
                renderer.receiveShadows = true;
                renderer.shadowCastingMode = ShadowCastingMode.On;
            }

            var collider = gameObject.GetComponent<Collider>();
            if (collider != null)
            {
                UnityEngine.Object.DestroyImmediate(collider);
            }

            return gameObject;
        }

        private static void CreateLine(string name, Transform parent, Vector3[] points, Material material, float width)
        {
            var gameObject = new GameObject(name);
            gameObject.transform.SetParent(parent, false);
            var line = gameObject.AddComponent<LineRenderer>();
            line.useWorldSpace = false;
            line.positionCount = points.Length;
            line.SetPositions(points);
            line.startWidth = width;
            line.endWidth = width;
            line.numCornerVertices = 3;
            line.numCapVertices = 3;
            line.alignment = LineAlignment.View;
            line.textureMode = LineTextureMode.Stretch;
            line.shadowCastingMode = ShadowCastingMode.Off;
            line.receiveShadows = false;
            line.sharedMaterial = material;
            line.startColor = Color.white;
            line.endColor = Color.white;
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path))
            {
                return;
            }

            var parent = Path.GetDirectoryName(path).Replace('\\', '/');
            var folderName = Path.GetFileName(path);
            if (!AssetDatabase.IsValidFolder(parent))
            {
                EnsureFolder(parent);
            }

            AssetDatabase.CreateFolder(parent, folderName);
        }

        private static Color Hex(string value)
        {
            if (!ColorUtility.TryParseHtmlString(value, out var color))
            {
                throw new InvalidOperationException("Invalid color value: " + value);
            }

            return color;
        }

        private sealed class GardenMaterials
        {
            public Material[] topPalette;
            public Material clayUpper;
            public Material clayMiddle;
            public Material clayLower;
            public Material edgeGlow;
            public Material trailStone;
            public Material trailMarker;
            public Material deadEndStone;
            public Material deadEndGlow;
            public Material sourceBase;
            public Material sourceOrb;
            public Material receiverBase;
            public Material receiverCrystal;
            public Material activeSignal;
            public Material failedSignal;
            public Material successSignal;
            public Material leafA;
            public Material leafB;
            public Material leafC;
            public Material flowerCoral;
            public Material flowerCream;
            public Material rock;
            public Material rockLight;
        }
    }
}
