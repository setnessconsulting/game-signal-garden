"""Create the original Signal Garden SG-05 receiver shrine asset.

This script is intentionally repository-owned and repeatable. It recreates a
small static Blender source scene and exports the runtime FBX with the axis and
unit settings used by Unity's ModelImporter. The committed hashes identify the
captured source and export; rerunning the script regenerates equivalent geometry
and should be followed by a manifest refresh. It accepts only explicit output
paths after ``--`` when invoked by Blender.
"""

from __future__ import annotations

import math
import sys
from pathlib import Path

import bpy


def _arguments() -> tuple[Path, Path]:
    try:
        separator = sys.argv.index("--")
    except ValueError as exc:
        raise SystemExit("Expected Blender arguments after --: --source <path> --fbx <path>") from exc

    args = sys.argv[separator + 1 :]
    values: dict[str, str] = {}
    index = 0
    while index < len(args):
        key = args[index]
        if key not in {"--source", "--fbx"} or index + 1 >= len(args):
            raise SystemExit("Expected only --source <path> and --fbx <path>")
        values[key[2:]] = args[index + 1]
        index += 2

    if set(values) != {"source", "fbx"}:
        raise SystemExit("Both --source and --fbx are required")
    return Path(values["source"]).resolve(), Path(values["fbx"]).resolve()


def _material(name: str, color: tuple[float, float, float], roughness: float, metallic: float = 0.0) -> bpy.types.Material:
    material = bpy.data.materials.new(name)
    material.use_nodes = True
    nodes = material.node_tree.nodes
    shader = nodes.get("Principled BSDF")
    if shader is None:
        raise RuntimeError(f"Material {name} has no Principled BSDF node")
    shader.inputs["Base Color"].default_value = (*color, 1.0)
    shader.inputs["Roughness"].default_value = roughness
    shader.inputs["Metallic"].default_value = metallic
    if "Specular IOR Level" in shader.inputs:
        shader.inputs["Specular IOR Level"].default_value = 0.45
    return material


def _link_to_collection(obj: bpy.types.Object, collection: bpy.types.Collection) -> None:
    for owner in list(obj.users_collection):
        owner.objects.unlink(obj)
    collection.objects.link(obj)


def _apply_mesh_transform(obj: bpy.types.Object) -> None:
    bpy.context.view_layer.objects.active = obj
    obj.select_set(True)
    bpy.ops.object.transform_apply(location=False, rotation=True, scale=True)
    obj.select_set(False)


def _bevel(obj: bpy.types.Object, width: float) -> None:
    modifier = obj.modifiers.new("Soft low-poly edges", "BEVEL")
    modifier.width = width
    modifier.segments = 2
    modifier.limit_method = "ANGLE"
    bpy.context.view_layer.objects.active = obj
    obj.select_set(True)
    bpy.ops.object.modifier_apply(modifier=modifier.name)
    obj.select_set(False)


def _cube(
    collection: bpy.types.Collection,
    name: str,
    dimensions: tuple[float, float, float],
    location: tuple[float, float, float],
    material: bpy.types.Material,
    bevel: float,
) -> bpy.types.Object:
    bpy.ops.mesh.primitive_cube_add(size=1.0, location=location)
    obj = bpy.context.object
    obj.name = name
    obj.dimensions = dimensions
    _apply_mesh_transform(obj)
    _bevel(obj, bevel)
    obj.data.materials.append(material)
    _link_to_collection(obj, collection)
    return obj


def _cylinder(
    collection: bpy.types.Collection,
    name: str,
    radius: float,
    depth: float,
    location: tuple[float, float, float],
    material: bpy.types.Material,
    vertices: int = 12,
    bevel: float = 0.0,
) -> bpy.types.Object:
    bpy.ops.mesh.primitive_cylinder_add(vertices=vertices, radius=radius, depth=depth, location=location)
    obj = bpy.context.object
    obj.name = name
    _apply_mesh_transform(obj)
    if bevel:
        _bevel(obj, bevel)
    obj.data.materials.append(material)
    _link_to_collection(obj, collection)
    return obj


def _torus(
    collection: bpy.types.Collection,
    name: str,
    major_radius: float,
    minor_radius: float,
    location: tuple[float, float, float],
    material: bpy.types.Material,
) -> bpy.types.Object:
    bpy.ops.mesh.primitive_torus_add(
        major_segments=16,
        minor_segments=6,
        location=location,
        major_radius=major_radius,
        minor_radius=minor_radius,
    )
    obj = bpy.context.object
    obj.name = name
    _apply_mesh_transform(obj)
    obj.data.materials.append(material)
    _link_to_collection(obj, collection)
    return obj


def _crystal(collection: bpy.types.Collection, material: bpy.types.Material) -> bpy.types.Object:
    sides = 7
    rings = ((0.44, 0.0), (0.36, 0.45), (0.25, 1.05))
    vertices: list[tuple[float, float, float]] = []
    for radius, height in rings:
        for side in range(sides):
            angle = side * math.tau / sides + math.radians(8.0)
            vertices.append((math.cos(angle) * radius, math.sin(angle) * radius, height))
    vertices.append((0.0, 0.0, 1.52))
    tip = len(vertices) - 1

    faces: list[tuple[int, ...]] = []
    faces.append(tuple(range(sides - 1, -1, -1)))
    for ring in range(len(rings) - 1):
        start = ring * sides
        next_start = (ring + 1) * sides
        for side in range(sides):
            nxt = (side + 1) % sides
            faces.append((start + side, next_start + side, next_start + nxt, start + nxt))
    top_start = (len(rings) - 1) * sides
    for side in range(sides):
        nxt = (side + 1) % sides
        faces.append((top_start + side, tip, top_start + nxt))

    mesh = bpy.data.meshes.new("SM_ReceiverShrine_CrystalMesh")
    mesh.from_pydata(vertices, [], faces)
    mesh.update()
    uv_layer = mesh.uv_layers.new(name="UVMap")
    for loop in mesh.loops:
        vertex = mesh.vertices[loop.vertex_index].co
        uv_layer.data[loop.index].uv = (0.5 + vertex.x * 0.45, max(0.0, min(1.0, vertex.z / 1.52)))

    obj = bpy.data.objects.new("SM_ReceiverShrine_Crystal", mesh)
    collection.objects.link(obj)
    obj.location = (0.0, 0.0, 0.55)
    obj.data.materials.append(material)
    for polygon in mesh.polygons:
        polygon.use_smooth = False
    return obj


def _build_scene() -> bpy.types.Collection:
    bpy.ops.wm.read_factory_settings(use_empty=True)
    scene = bpy.context.scene
    scene.name = "SignalGardenReceiver"
    scene.unit_settings.system = "METRIC"
    scene.unit_settings.scale_length = 1.0
    scene.unit_settings.length_unit = "METERS"

    collection = bpy.data.collections.new("SignalGardenReceiver")
    scene.collection.children.link(collection)
    root = bpy.data.objects.new("SG_ReceiverShrine", None)
    collection.objects.link(root)
    root.empty_display_type = "CUBE"
    root.empty_display_size = 0.2
    root["asset_id"] = "signal-garden.receiver-shrine"
    root["asset_role"] = "static receiver visual"

    stone = _material("MAT_ReceiverStone", (0.16, 0.27, 0.28), 0.46)
    glass = _material("MAT_ReceiverGlass", (0.16, 0.78, 0.72), 0.18, 0.05)
    accent = _material("MAT_ReceiverAccent", (0.90, 0.69, 0.30), 0.30, 0.10)

    base = _cylinder(collection, "SM_ReceiverShrine_Base", 0.62, 0.42, (0.0, 0.0, 0.22), stone, vertices=10, bevel=0.08)
    base.parent = root
    base["component_role"] = "stone socket"

    lower_inlay = _torus(collection, "SM_ReceiverShrine_Inlay", 0.47, 0.065, (0.0, 0.0, 0.46), accent)
    lower_inlay.parent = root
    lower_inlay["component_role"] = "signal inlay"

    crystal = _crystal(collection, glass)
    crystal.parent = root
    crystal["component_role"] = "receiver crystal"

    crown = _cylinder(collection, "SM_ReceiverShrine_Crown", 0.20, 0.16, (0.0, 0.0, 1.98), accent, vertices=7, bevel=0.025)
    crown.parent = root
    crown["component_role"] = "crown cap"

    for side in range(3):
        angle = side * math.tau / 3.0 + math.radians(30.0)
        fin = _cube(
            collection,
            f"SM_ReceiverShrine_Fin_{side + 1}",
            (0.11, 0.62, 0.22),
            (math.cos(angle) * 0.42, math.sin(angle) * 0.42, 0.83),
            accent,
            0.025,
        )
        fin.rotation_euler[2] = angle
        _apply_mesh_transform(fin)
        fin.parent = root
        fin["component_role"] = "crystal fin"

    for obj in collection.objects:
        if obj.type == "MESH":
            obj["license"] = "Original Setness Consulting asset"
            obj["human_modification_status"] = "script-assisted original design"
    return collection


def _export_fbx(path: Path, collection: bpy.types.Collection) -> None:
    path.parent.mkdir(parents=True, exist_ok=True)
    bpy.ops.object.select_all(action="DESELECT")
    meshes = [obj for obj in collection.objects if obj.type == "MESH"]
    for obj in meshes:
        obj.select_set(True)
    bpy.context.view_layer.objects.active = meshes[0]
    result = bpy.ops.export_scene.fbx(
        filepath=str(path),
        use_selection=True,
        object_types={"MESH"},
        use_mesh_modifiers=True,
        apply_unit_scale=True,
        apply_scale_options="FBX_SCALE_ALL",
        axis_forward="-Z",
        axis_up="Y",
        bake_anim=False,
        add_leaf_bones=False,
        use_custom_props=True,
        path_mode="AUTO",
        embed_textures=False,
    )
    if "FINISHED" not in result or not path.is_file():
        raise RuntimeError("Blender FBX export did not produce an artifact")


def main() -> None:
    source, fbx = _arguments()
    source.parent.mkdir(parents=True, exist_ok=True)
    collection = _build_scene()
    bpy.ops.wm.save_as_mainfile(filepath=str(source))
    _export_fbx(fbx, collection)
    triangle_count = sum(
        sum(max(0, len(polygon.vertices) - 2) for polygon in obj.data.polygons)
        for obj in collection.objects
        if obj.type == "MESH"
    )
    print(f"SIGNAL_GARDEN_SOURCE={source}")
    print(f"SIGNAL_GARDEN_FBX={fbx}")
    print(f"SIGNAL_GARDEN_TRIANGLES={triangle_count}")
    print("SIGNAL_GARDEN_MATERIALS=3")
    print("SIGNAL_GARDEN_ANIMATION=none")
    print("SIGNAL_GARDEN_COLLISION=none")


if __name__ == "__main__":
    main()
