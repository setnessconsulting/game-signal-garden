using System.Collections.Generic;
using UnityEngine;

namespace SignalGarden
{
    public static class SignalGardenRuntimeBuilder
    {
        public static void Build(SignalGardenGame game)
        {
            var focus = new GameObject("Garden camera focus");
            focus.transform.position = new Vector3(0f, -0.30f, 0f);
            var camera = Camera.main;
            if (camera == null)
            {
                var cameraObject = new GameObject("Garden Camera");
                camera = cameraObject.AddComponent<Camera>();
                cameraObject.AddComponent<AudioListener>();
                camera.tag = "MainCamera";
            }

            camera.orthographic = true;
            camera.orthographicSize = 4.70f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = Hex("#12323B");
            camera.allowHDR = true;
            camera.allowMSAA = true;
            camera.nearClipPlane = 0.1f;
            camera.farClipPlane = 100f;
            camera.useOcclusionCulling = false;
            camera.transform.position = new Vector3(0f, 11.5f, -13.4f);
            camera.transform.LookAt(focus.transform.position);

            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = Hex("#9BC5BD");
            RenderSettings.ambientEquatorColor = Hex("#557B73");
            RenderSettings.ambientGroundColor = Hex("#223D40");
            RenderSettings.reflectionIntensity = 0.22f;

            var keyLight = Object.FindAnyObjectByType<Light>();
            if (keyLight == null)
            {
                keyLight = new GameObject("Warm canopy light").AddComponent<Light>();
            }
            keyLight.type = LightType.Directional;
            keyLight.color = Hex("#FFE0B0");
            keyLight.intensity = 1.5f;
            keyLight.shadows = LightShadows.Soft;
            keyLight.shadowStrength = 0.45f;
            keyLight.transform.rotation = Quaternion.Euler(47f, -34f, 0f);

            var moss = new[]
            {
                Material("Moss deep", "#426B5C", 0.12f),
                Material("Moss fern", "#547D62", 0.14f),
                Material("Moss sage", "#678B68", 0.16f),
                Material("Moss sunlit", "#779771", 0.18f)
            };
            var ochre = Material("Island ochre", "#A9674C", 0.24f);
            var rose = Material("Island rose", "#854E45", 0.23f);
            var deepStone = Material("Floating stone", "#344C51", 0.25f);
            var rim = Material("Garden glaze", "#73B6A4", 0.32f, "#347D75", 0.25f);
            var path = Material("Warm path stones", "#D5BC80", 0.30f, "#B48637", 0.16f);
            var pathGlow = Material("Gold trail", "#F4D88B", 0.30f, "#F5B843", 1.05f);
            var spurStone = Material("Blind spur stones", "#788A80", 0.24f);
            var spurGlow = Material("Blind spur thread", "#9DAF9E", 0.32f, "#7F9588", 0.10f);
            var sourceBase = Material("Coral socket", "#4F5D56", 0.35f);
            var sourceOrb = Material("Coral source", "#FA705F", 0.48f, "#FF4938", 1.35f);
            var receiverBase = Material("Blue socket", "#425F64", 0.42f);
            var receiverCrystal = Material("Blue receiver", "#76DBCF", 0.62f, "#33D7C3", 0.52f);
            var active = Material("Active signal", "#C5F4D8", 0.25f, "#63FFD6", 1.8f);
            var failed = Material("Faded signal", "#FF9A75", 0.18f, "#F1544B", 0.85f);
            var success = Material("Living signal", "#DBF9B3", 0.36f, "#A6FF87", 2.0f);
            var leavesA = Material("Fern jade", "#377B62", 0.16f);
            var leavesB = Material("Fern green", "#5C936A", 0.17f);
            var flower = Material("Wildflower coral", "#E77D6D", 0.24f, "#B74048", 0.10f);
            var flowerCream = Material("Wildflower cream", "#F0D69A", 0.24f);
            var stone = Material("Moss pebble", "#647A70", 0.18f);
            var firefly = Material("Garden firefly", "#FFF0B7", 0.18f, "#FFD76A", 2.30f);

            var island = new GameObject("Floating Garden");
            CreatePrimitive(PrimitiveType.Sphere, "Upper warm earth band", island.transform,
                new Vector3(0f, -0.40f, 0f), new Vector3(9.95f, 1.17f, 6.45f), ochre);
            CreatePrimitive(PrimitiveType.Sphere, "Rose clay stratum", island.transform,
                new Vector3(0f, -1.08f, 0f), new Vector3(8.55f, 1.45f, 5.58f), rose);
            CreatePrimitive(PrimitiveType.Sphere, "Deep floating stone", island.transform,
                new Vector3(0f, -1.82f, 0f), new Vector3(6.75f, 1.35f, 4.40f), deepStone);
            var surface = new GameObject("Faceted moss surface");
            surface.transform.SetParent(island.transform, false);
            surface.AddComponent<MeshFilter>().sharedMesh = BuildIslandMesh();
            surface.AddComponent<MeshRenderer>().sharedMaterials = moss;

            CreateTrail(island.transform, RouteRules.StandardTrail, path, pathGlow, 0.78f, "Gold signal trail");
            CreateTrail(island.transform, RouteRules.DeadEndSpur, spurStone, spurGlow, 0.62f, "Blind spur");
            CreateRim(island.transform, rim);
            CreateGardenDetails(island.transform, leavesA, leavesB, flower, flowerCream, stone);
            CreateAtmosphere(island.transform, firefly);

            var source = CreateSource(island.transform, RouteRules.StandardTrail[0], sourceBase, path, sourceOrb, flowerCream);
            var receiver = CreateReceiver(island.transform, RouteRules.StandardTrail[RouteRules.StandardTrail.Length - 1], receiverBase, path, receiverCrystal, flowerCream);
            var sourceLight = PointLight("Source glow", new Vector3(source.position.x, 1.1f, source.position.z), Hex("#FF6250"), 1.35f, 5f);
            var receiverLight = PointLight("Receiver glow", new Vector3(receiver.position.x, 1.15f, receiver.position.z), Hex("#57E8D6"), 0.72f, 4f);

            var routeObject = new GameObject("Player signal line");
            var routeLine = routeObject.AddComponent<LineRenderer>();
            routeLine.enabled = false;
            routeLine.useWorldSpace = true;
            routeLine.alignment = LineAlignment.View;
            routeLine.textureMode = LineTextureMode.Stretch;
            routeLine.widthMultiplier = 0.14f;
            routeLine.numCornerVertices = 4;
            routeLine.numCapVertices = 5;
            routeLine.sharedMaterial = active;

            var audio = new GameObject("Optional feedback cues").AddComponent<AudioSource>();
            audio.playOnAwake = false;
            audio.spatialBlend = 0f;
            audio.volume = 0.65f;
            game.Configure(camera, focus.transform, source, receiver, routeLine, sourceLight, receiverLight,
                active, failed, success, audio);
        }

        private static void CreateTrail(Transform parent, IList<Vector2> points, Material stone, Material glow, float width, string groupName)
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

            CreateLine(groupName + " center thread", group.transform, thread, glow, groupName == "Blind spur" ? 0.035f : 0.045f);
            if (groupName == "Blind spur")
            {
                var end = points[points.Count - 1];
                CreatePrimitive(PrimitiveType.Sphere, "Quiet dead end", group.transform,
                    new Vector3(end.x, 0.60f, end.y), new Vector3(0.30f, 0.30f, 0.30f), stone);
                CreatePrimitive(PrimitiveType.Cube, "Dead end cross mark", group.transform,
                    new Vector3(end.x, 0.80f, end.y), new Vector3(0.34f, 0.04f, 0.06f), glow);
            }
        }

        private static Transform CreateSource(Transform parent, Vector2 point, Material baseMaterial, Material ringMaterial, Material orbMaterial, Material glintMaterial)
        {
            var root = new GameObject("Coral Source");
            root.transform.SetParent(parent, false);
            root.transform.position = new Vector3(point.x, 0f, point.y);
            CreatePrimitive(PrimitiveType.Cylinder, "Source stone socket", root.transform,
                new Vector3(0f, 0.52f, 0f), new Vector3(0.94f, 0.075f, 0.94f), baseMaterial);
            CreatePrimitive(PrimitiveType.Cylinder, "Source inner ring", root.transform,
                new Vector3(0f, 0.64f, 0f), new Vector3(0.68f, 0.045f, 0.68f), ringMaterial);
            CreatePrimitive(PrimitiveType.Sphere, "Coral signal", root.transform,
                new Vector3(0f, 1.07f, 0f), new Vector3(0.48f, 0.48f, 0.48f), orbMaterial);
            CreatePrimitive(PrimitiveType.Cylinder, "Source stem", root.transform,
                new Vector3(0f, 0.80f, 0f), new Vector3(0.08f, 0.14f, 0.08f), baseMaterial);
            CreatePrimitive(PrimitiveType.Sphere, "Source glint", root.transform,
                new Vector3(-0.10f, 1.19f, -0.18f), new Vector3(0.09f, 0.09f, 0.09f), glintMaterial);
            return root.transform;
        }

        private static Transform CreateReceiver(Transform parent, Vector2 point, Material baseMaterial, Material ringMaterial, Material crystalMaterial, Material glintMaterial)
        {
            var root = new GameObject("Blue Receiver");
            root.transform.SetParent(parent, false);
            root.transform.position = new Vector3(point.x, 0f, point.y);
            CreatePrimitive(PrimitiveType.Cylinder, "Receiver stone socket", root.transform,
                new Vector3(0f, 0.52f, 0f), new Vector3(0.98f, 0.075f, 0.98f), baseMaterial);
            CreatePrimitive(PrimitiveType.Cylinder, "Receiver glass dish", root.transform,
                new Vector3(0f, 0.64f, 0f), new Vector3(0.72f, 0.045f, 0.72f), ringMaterial);
            var crystalObject = new GameObject("Receiver crystal");
            crystalObject.transform.SetParent(root.transform, false);
            crystalObject.transform.localPosition = new Vector3(0f, 1.06f, 0f);
            crystalObject.transform.localScale = new Vector3(0.72f, 0.78f, 0.72f);
            crystalObject.AddComponent<MeshFilter>().sharedMesh = BuildCrystalMesh();
            crystalObject.AddComponent<MeshRenderer>().sharedMaterial = crystalMaterial;
            CreatePrimitive(PrimitiveType.Sphere, "Receiver heart", root.transform,
                new Vector3(0f, 0.87f, 0f), new Vector3(0.26f, 0.26f, 0.26f), crystalMaterial);
            CreatePrimitive(PrimitiveType.Sphere, "Receiver sparkle", root.transform,
                new Vector3(0.16f, 1.20f, -0.17f), new Vector3(0.08f, 0.08f, 0.08f), glintMaterial);
            return root.transform;
        }

        private static void CreateGardenDetails(Transform parent, Material leavesA, Material leavesB, Material flower, Material cream, Material stone)
        {
            var positions = new[]
            {
                new Vector2(-4.40f, 0.72f), new Vector2(-4.05f, -0.92f), new Vector2(-3.68f, -2.15f),
                new Vector2(-2.02f, -2.72f), new Vector2(-0.96f, -2.84f), new Vector2(0.86f, 2.67f),
                new Vector2(1.55f, 2.44f), new Vector2(3.72f, 1.95f), new Vector2(4.18f, 0.55f),
                new Vector2(4.15f, -0.96f), new Vector2(-1.78f, 2.72f), new Vector2(-3.95f, 1.05f)
            };
            for (var i = 0; i < positions.Length; i++)
            {
                if (RouteRules.DistanceToTrail(positions[i], RouteRules.StandardTrail) < 0.86f ||
                    RouteRules.DistanceToTrail(positions[i], RouteRules.DeadEndSpur) < 0.70f)
                {
                    continue;
                }

                var plant = new GameObject("Wild growth " + (i + 1));
                plant.transform.SetParent(parent, false);
                plant.transform.localPosition = new Vector3(positions[i].x, 0.31f, positions[i].y);
                var height = 0.78f + (i % 4) * 0.11f;
                CreatePrimitive(PrimitiveType.Cylinder, "Fern stem", plant.transform,
                    new Vector3(0f, 0.18f, 0f), new Vector3(0.075f, 0.24f, 0.075f), stone);
                CreatePrimitive(PrimitiveType.Sphere, "Fern crown", plant.transform,
                    new Vector3(0f, height * 0.62f, 0f), new Vector3(0.47f, height * 0.60f, 0.46f), i % 2 == 0 ? leavesA : leavesB);
                CreatePrimitive(PrimitiveType.Sphere, "Fern side leaf", plant.transform,
                    new Vector3(0.25f, height * 0.40f, -0.17f), new Vector3(0.38f, 0.26f, 0.33f), leavesB);
                CreatePrimitive(PrimitiveType.Sphere, "Wildflower center", plant.transform,
                    new Vector3(-0.13f, height + 0.06f, 0.10f), new Vector3(0.14f, 0.14f, 0.14f), cream);
                for (var petal = 0; petal < 5; petal++)
                {
                    var angle = petal * Mathf.PI * 2f / 5f;
                    CreatePrimitive(PrimitiveType.Sphere, "Wildflower petal", plant.transform,
                        new Vector3(-0.13f + Mathf.Cos(angle) * 0.18f, height + 0.045f, 0.10f + Mathf.Sin(angle) * 0.18f),
                        new Vector3(0.16f, 0.11f, 0.14f), flower);
                }
            }
        }

        private static void CreateAtmosphere(Transform parent, Material fireflyMaterial)
        {
            var positions = new[]
            {
                new Vector3(-5.65f, 1.65f, 4.20f), new Vector3(-4.95f, 2.40f, 3.70f),
                new Vector3(4.95f, 2.05f, 3.85f), new Vector3(5.55f, 1.25f, 2.75f),
                new Vector3(-5.20f, 1.10f, -3.30f), new Vector3(5.45f, 1.45f, -2.80f),
                new Vector3(0.05f, 2.85f, 4.95f), new Vector3(-1.60f, 2.20f, 4.65f),
                new Vector3(2.35f, 2.60f, 4.35f)
            };

            for (var index = 0; index < positions.Length; index++)
            {
                var firefly = CreatePrimitive(
                    PrimitiveType.Sphere,
                    "Garden firefly " + (index + 1),
                    parent,
                    positions[index],
                    new Vector3(0.065f + (index % 3) * 0.018f, 0.065f + (index % 2) * 0.020f, 0.065f),
                    fireflyMaterial);
                firefly.transform.rotation = Quaternion.Euler(0f, index * 37f, index * 13f);
            }
        }

        private static void CreateRim(Transform parent, Material material)
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

        private static Mesh BuildIslandMesh()
        {
            const int segments = 72;
            const int rings = 8;
            var vertices = new List<Vector3>();
            var submeshes = new[] { new List<int>(), new List<int>(), new List<int>(), new List<int>() };

            Vector3 At(float radius, float angle)
            {
                var wobble = 1f + 0.025f * Mathf.Sin(angle * 5f) + 0.012f * Mathf.Cos(angle * 9f);
                return new Vector3(
                    Mathf.Cos(angle) * 5.05f * radius * wobble,
                    0.26f + 0.045f * Mathf.Sin(angle * 3f + radius * 4f) + 0.026f * Mathf.Cos(angle * 5f - radius * 3f),
                    Mathf.Sin(angle) * 3.30f * radius * (1f + 0.018f * Mathf.Sin(angle * 7f)));
            }

            void AddTriangle(Vector3 a, Vector3 b, Vector3 c, int material)
            {
                var start = vertices.Count;
                vertices.Add(a);
                vertices.Add(b);
                vertices.Add(c);
                submeshes[material].Add(start);
                submeshes[material].Add(start + 1);
                submeshes[material].Add(start + 2);
            }

            for (var segment = 0; segment < segments; segment++)
            {
                var a = segment * Mathf.PI * 2f / segments;
                var b = (segment + 1) * Mathf.PI * 2f / segments;
                AddTriangle(new Vector3(0f, 0.25f, 0f), At(1f / rings, b), At(1f / rings, a), segment % 4);
            }

            for (var ring = 0; ring < rings - 1; ring++)
            {
                var innerRadius = (ring + 1f) / rings;
                var outerRadius = (ring + 2f) / rings;
                for (var segment = 0; segment < segments; segment++)
                {
                    var a = segment * Mathf.PI * 2f / segments;
                    var b = (segment + 1) * Mathf.PI * 2f / segments;
                    var innerA = At(innerRadius, a);
                    var innerB = At(innerRadius, b);
                    var outerA = At(outerRadius, a);
                    var outerB = At(outerRadius, b);
                    var material = Mathf.Abs((ring * 7 + segment * 3) % 4);
                    AddTriangle(innerA, outerB, outerA, material);
                    AddTriangle(innerA, innerB, outerB, (material + (segment % 5 == 0 ? 1 : 0)) % 4);
                }
            }

            var mesh = new Mesh { name = "Faceted Garden Top" };
            mesh.SetVertices(vertices);
            mesh.subMeshCount = submeshes.Length;
            for (var i = 0; i < submeshes.Length; i++)
            {
                mesh.SetTriangles(submeshes[i], i, true);
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
                ring[i] = new Vector3(Mathf.Cos(angle) * radius, -0.08f, Mathf.Sin(angle) * radius);
            }

            void AddOutward(Vector3 a, Vector3 b, Vector3 c)
            {
                var normal = Vector3.Cross(b - a, c - a);
                if (Vector3.Dot(normal, (a + b + c) / 3f) < 0f)
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

        private static GameObject CreatePrimitive(PrimitiveType primitive, string name, Transform parent, Vector3 position, Vector3 scale, Material material)
        {
            var instance = GameObject.CreatePrimitive(primitive);
            instance.name = name;
            instance.transform.SetParent(parent, false);
            instance.transform.localPosition = position;
            instance.transform.localScale = scale;
            instance.GetComponent<Renderer>().sharedMaterial = material;
            instance.GetComponent<Renderer>().receiveShadows = true;
            var collider = instance.GetComponent<Collider>();
            if (collider != null) Object.Destroy(collider);
            return instance;
        }

        private static void CreateLine(string name, Transform parent, Vector3[] points, Material material, float width)
        {
            var lineObject = new GameObject(name);
            lineObject.transform.SetParent(parent, false);
            var line = lineObject.AddComponent<LineRenderer>();
            line.useWorldSpace = false;
            line.positionCount = points.Length;
            line.SetPositions(points);
            line.startWidth = width;
            line.endWidth = width;
            line.numCornerVertices = 3;
            line.numCapVertices = 3;
            line.alignment = LineAlignment.View;
            line.sharedMaterial = material;
            line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            line.receiveShadows = false;
        }

        private static Light PointLight(string name, Vector3 position, Color color, float intensity, float range)
        {
            var light = new GameObject(name).AddComponent<Light>();
            light.type = LightType.Point;
            light.transform.position = position;
            light.color = color;
            light.intensity = intensity;
            light.range = range;
            light.shadows = LightShadows.None;
            return light;
        }

        private static Material Material(string name, string baseColorHex, float smoothness, string emissionHex = null, float emissionStrength = 0f)
        {
            var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            var material = new Material(shader) { name = name };
            var color = Hex(baseColorHex);
            if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", color);
            if (material.HasProperty("_Color")) material.SetColor("_Color", color);
            if (material.HasProperty("_Smoothness")) material.SetFloat("_Smoothness", smoothness);
            if (material.HasProperty("_Metallic")) material.SetFloat("_Metallic", 0.03f);
            if (!string.IsNullOrEmpty(emissionHex) && emissionStrength > 0f)
            {
                material.EnableKeyword("_EMISSION");
                if (material.HasProperty("_EmissionColor")) material.SetColor("_EmissionColor", Hex(emissionHex) * emissionStrength);
            }
            return material;
        }

        private static Color Hex(string value)
        {
            ColorUtility.TryParseHtmlString(value, out var color);
            return color;
        }
    }
}
