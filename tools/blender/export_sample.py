"""Generate a placeholder cover prop to prove Blender -> FBX -> Unity, not final art."""
from pathlib import Path
import bpy

root = Path(__file__).resolve().parents[2]
source = root / 'art' / 'blender'
output = root / 'game' / 'BadGuy' / 'Assets' / 'BadGuy' / 'Art' / 'Models'
source.mkdir(parents=True, exist_ok=True)
output.mkdir(parents=True, exist_ok=True)
bpy.ops.object.select_all(action='SELECT')
bpy.ops.object.delete(use_global=False)
bpy.ops.mesh.primitive_cube_add(size=1, location=(0,0,.5))
crate = bpy.context.object
crate.name = 'SM_CoverCrate_Prototype'
crate.dimensions = (1.2,.8,1)
bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
bevel = crate.modifiers.new('Soft edges', 'BEVEL')
bevel.width = .025; bevel.segments = 2
bpy.ops.wm.save_as_mainfile(filepath=str(source / 'cover_crate.blend'))
bpy.ops.export_scene.fbx(filepath=str(output / 'SM_CoverCrate_Prototype.fbx'), use_selection=True,
                         apply_unit_scale=True, axis_forward='-Z', axis_up='Y', add_leaf_bones=False,
                         bake_anim=False, object_types={'MESH'})
print('BADGUY_BLENDER_EXPORT_OK', output)
