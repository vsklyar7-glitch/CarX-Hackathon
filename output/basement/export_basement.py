"""Export the downloaded Blender scene to FBX without modifying its source."""
import bpy
import json
import os
from pathlib import Path

root = Path(__file__).resolve().parents[2]
asset = root / "Assets/Prefabs/basement"
textures = asset / "textures"
known = {p.name.casefold(): p for p in textures.rglob("*") if p.is_file() and p.suffix != ".meta"}
report = {"blender_version": bpy.app.version_string, "objects": [], "images": [], "materials": []}

for image in bpy.data.images:
    if image.source not in {"FILE", "TILED"}:
        continue
    original = image.filepath
    resolved = Path(bpy.path.abspath(original)) if original else None
    basename = os.path.basename(original.replace("\\", "/"))
    match = known.get(basename.casefold()) or known.get(image.name.casefold())
    if match:
        image.filepath = str(match)
    elif image.packed_file:
        unpacked = textures / "Unpacked"
        unpacked.mkdir(exist_ok=True)
        target = unpacked / (Path(basename or image.name).stem + ".png")
        if not target.exists():
            image.filepath_raw = str(target)
            image.file_format = "PNG"
            image.save()
        image.filepath = str(target)
    report["images"].append({"name": image.name, "original": original, "resolved": image.filepath, "exists": Path(bpy.path.abspath(image.filepath)).is_file()})

for material in bpy.data.materials:
    entry = {"name": material.name, "color": list(material.diffuse_color), "textures": []}
    if material.use_nodes:
        for node in material.node_tree.nodes:
            if node.type == "BSDF_PRINCIPLED":
                for key in ("Base Color", "Roughness", "Metallic", "Alpha"):
                    if key in node.inputs:
                        value = node.inputs[key].default_value
                        entry[key] = list(value) if hasattr(value, "__len__") else value
            if node.type == "TEX_IMAGE" and node.image:
                entry["textures"].append({"image": node.image.name, "path": node.image.filepath, "targets": [link.to_socket.name for output in node.outputs for link in output.links]})
    report["materials"].append(entry)

for obj in bpy.context.scene.objects:
    report["objects"].append({"name": obj.name, "type": obj.type, "visible": obj.visible_get(), "dimensions": list(obj.dimensions)})

bpy.ops.object.mode_set(mode="OBJECT") if bpy.context.object and bpy.context.object.mode != "OBJECT" else None
target = asset / "source/basement.fbx"
bpy.ops.export_scene.fbx(
    filepath=str(target),
    use_selection=False,
    use_visible=True,
    object_types={"MESH", "EMPTY", "ARMATURE"},
    axis_forward="-Z",
    axis_up="Y",
    global_scale=1.0,
    apply_unit_scale=True,
    apply_scale_options="FBX_SCALE_ALL",
    use_mesh_modifiers=True,
    mesh_smooth_type="FACE",
    add_leaf_bones=False,
    bake_anim=False,
    path_mode="RELATIVE",
    embed_textures=False,
)
report["fbx"] = str(target)
report["fbx_bytes"] = target.stat().st_size
(root / "output/basement/export-report.json").write_text(json.dumps(report, indent=2, ensure_ascii=False), encoding="utf-8")
print("BASEMENT_EXPORT_COMPLETE", report["fbx_bytes"], "bytes;", len(report["materials"]), "materials")
