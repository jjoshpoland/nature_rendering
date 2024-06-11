extends Node

@export var size : int
@export var chunk_size : int
@export var chunk : PackedScene
@export var needs_regen : bool

var heightmap
var chunks : Array

# Called when the node enters the scene tree for the first time.
func _ready():
	var heightmap = get_node("heightmap")
	heightmap.Init(size * chunk_size)
	pass # Replace with function body.

func Generate():
	ClearChunks()
	chunks = [[]]
	for x in size:
		for y in size:
			var chunk_instance = chunk.instantiate()
			add_child(chunk_instance)
			chunk_instance.chunk_size = chunk_size
			chunks[x][y] = chunk_instance
	needs_regen = false
			
func ClearChunks():
	for x in chunks.size():
		for y in chunks[x].size():
			if chunks[x][y] != null:
				chunks[x][y].queue_free()

# Called every frame. 'delta' is the elapsed time since the previous frame.
func _process(delta):
	if needs_regen:
		Generate()
