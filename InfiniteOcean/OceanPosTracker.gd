@tool
extends Node3D


# Called when the node enters the scene tree for the first time.
func _process(delta):
	RenderingServer.global_shader_parameter_set("ocean_pos", self.global_position); # Update global shader parameter 'ocean_pos' to match the ocean node position

