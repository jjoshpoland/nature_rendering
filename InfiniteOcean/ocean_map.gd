@tool
extends Node3D

@export var player_object : Node3D
@export var sea_level : float = 0.0
# Called when the node enters the scene tree for the first time.
func _ready():
	pass # Replace with function body.


# Called every frame. 'delta' is the elapsed time since the previous frame.
func _process(delta):
	if player_object:
		global_position = Vector3(player_object.global_position.x, sea_level, player_object.global_position.z)
