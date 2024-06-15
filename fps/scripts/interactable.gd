class_name Interactable extends Area3D

@onready var text_label = $Label3D

func hover(active : bool):
	text_label.visible = active
