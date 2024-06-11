@tool
class_name Terrain_Chunk
extends Node3D

@export var needs_regen : bool

@export var chunk_size : int
@export var chunk_coords : Vector2
@export var height_scale : float
var mesh_instance : MeshInstance3D

# Called when the node enters the scene tree for the first time.
func _ready():
	mesh_instance = MeshInstance3D.new()
	add_child(mesh_instance)
	pass


# Called every frame. 'delta' is the elapsed time since the previous frame.
func _process(delta):
	if needs_regen:
		needs_regen = false
		update_chunk()

func update_chunk():
	var plane_mesh = PlaneMesh.new()
	plane_mesh.size = Vector2(chunk_size, chunk_size)
	plane_mesh.subdivide_depth = chunk_size 
	plane_mesh.subdivide_width = chunk_size
	
	var surface_tool = SurfaceTool.new()
	var data_tool = MeshDataTool.new()

	surface_tool.create_from(plane_mesh, 0)
	var array_plane = surface_tool.commit()
	var error = data_tool.create_from_surface(array_plane, 0)
	
	for i in range(data_tool.get_vertex_count()):
		var vertex = data_tool.get_vertex(i)
		
		var noise = FastNoiseLite.new()
		noise.noise_type = FastNoiseLite.TYPE_SIMPLEX_SMOOTH
		vertex.y = noise.get_noise_3d(vertex.x + chunk_coords.x, vertex.y, vertex.z + chunk_coords.y) * height_scale
		
		data_tool.set_vertex(i, vertex)
		array_plane.clear_surfaces()
	
	data_tool.commit_to_surface(array_plane)
	surface_tool.begin(Mesh.PRIMITIVE_TRIANGLES)
	surface_tool.create_from(array_plane, 0)
	surface_tool.generate_normals()
	
	
	mesh_instance.mesh = surface_tool.commit()
	mesh_instance.create_trimesh_collision()
	mesh_instance.cast_shadow = GeometryInstance3D.SHADOW_CASTING_SETTING_OFF
		
