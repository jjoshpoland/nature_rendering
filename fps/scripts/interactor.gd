class_name Interactor extends RayCast3D

var current_target : Interactable
# Called when the node enters the scene tree for the first time.
func _ready():
	pass # Replace with function body.


# Called every frame. 'delta' is the elapsed time since the previous frame.
func _process(delta):
	if is_colliding():
		var target = get_collider()
		# Need to check type because an object being on the wrong layer could cause an error later on
		if target is Interactable:
			if current_target != target:
				current_target = target
				trigger_hover(current_target)
		else:
			end_hover()
			current_target = null
	else:
		end_hover()
		current_target = null

func trigger_hover(target : Interactable):
	target.hover(true)
	
func end_hover():
	if current_target != null:
		current_target.hover(false)
	
