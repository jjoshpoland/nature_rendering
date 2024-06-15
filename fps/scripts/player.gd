extends CharacterBody3D

@export_category("Speed")
@export var running_speed = 5.0
@export var sprinting_speed = 10.0
@export var crouch_speed = 2.0
@export var jump_velocity = 4.5

@export_category("Look")
@export var mouse_sensitivity = 0.25
@export var vertical_look_max = 89
@export var vertical_look_min = -89
@export var slide_look_tilt = 5.0

@export_category("Smoothing")
@export var interpolates : bool = true
@export var interpolation_speed = 10

@export_category("Headbobbing")
@export var bob_speed_sprint : float = 22
@export var bob_speed_run : float = 14
@export var bob_speed_crouch : float = 10
@export var bob_intensity_sprint : float = 0.2
@export var bob_intensity_run : float = 0.1
@export var bob_intensity_crouch : float = 0.05

var current_speed = 5.0
var crouching_depth = -.5
var direction : Vector3
var free_look_tilt : float = 10
var slide_vector
var bob_vector : Vector2
var bob_index : float = 0
var bob_intensity : float

var running : bool
var sprinting : bool
var crouching : bool
var sliding : bool
var free_look : bool

@onready var head = $Neck/Head
@onready var neck = $Neck
@onready var standing_height = $Neck.position.y
@onready var crouching_shape = $CrouchingCollisionShape
@onready var standing_collision_shape = $StandingCollisionShape
@onready var standing_room_checker = $StandingRoomChecker
@onready var camera_3d = $Neck/Head/Camera3D
@onready var slide_timer = $Slide_Timer

# Get the gravity from the project settings to be synced with RigidBody nodes.
var gravity = ProjectSettings.get_setting("physics/3d/default_gravity")

func _ready():
	Input.mouse_mode = Input.MOUSE_MODE_CAPTURED
	crouching_shape.shape.height = standing_height + crouching_depth
	crouching_shape.position.y = crouching_shape.shape.height / 2
	standing_room_checker.target_position.y = standing_collision_shape.shape.height
	
func _input(event):
	if event is InputEventMouseMotion:
		if free_look:
			neck.rotate_y(deg_to_rad(-event.relative.x * mouse_sensitivity))
			neck.rotation.y = clamp(neck.rotation.y, deg_to_rad(vertical_look_min), deg_to_rad(vertical_look_max))
		else:
			rotate_y(deg_to_rad(-event.relative.x * mouse_sensitivity))
			
		head.rotate_x(deg_to_rad(-event.relative.y * mouse_sensitivity))
		head.rotation.x = clamp(head.rotation.x, deg_to_rad(vertical_look_min), deg_to_rad(vertical_look_max))

func _physics_process(delta):
	var input_dir = Input.get_vector("left", "right", "forward", "back")
	# Handle movement state
	if Input.is_action_pressed("crouch"):
		if Input.is_action_pressed("sprint") && current_speed > 0 && input_dir.length() != 0 && !sliding && !crouching:
			_slide(delta, input_dir)
		_crouch(delta)
	elif sliding || standing_room_checker.is_colliding():
		_crouch(delta)
	elif !standing_room_checker.is_colliding():
		_stand(delta)
		if Input.is_action_pressed("sprint"):
			_sprint(delta)
		else:
			_run(delta)
	
			
	if Input.is_action_pressed("free_look") || sliding:
		free_look = true
		camera_3d.rotation.z = -deg_to_rad(neck.rotation.y * free_look_tilt) 
		if sliding:
			camera_3d.rotation.z = lerp(camera_3d.rotation.z, -deg_to_rad(slide_look_tilt), delta * interpolation_speed)
	else:
		free_look = false
		neck.rotation.y = lerp(neck.rotation.y, 0.0, delta * interpolation_speed)
		camera_3d.rotation.z = lerp(camera_3d.rotation.z, 0.0, delta * interpolation_speed)
		
	# Add the gravity, otherwise bob head
	if not is_on_floor():
		velocity.y -= gravity * delta
		
	if is_on_floor() && !sliding && input_dir.length() > 0:
		_bob_head(delta)
	else:
		# Reset head position when not moving or falling
		head.position.y = lerp(head.position.y, 0.0, delta * interpolation_speed)
		head.position.x = lerp(head.position.x, 0.0, delta * interpolation_speed)

	# Handle jump.
	if Input.is_action_just_pressed("jump") and is_on_floor():
		velocity.y = jump_velocity
		slide_timer.stop()
		_end_slide()

	# Get the input direction and handle the movement/deceleration.
	if sliding:
		direction = (transform.basis * Vector3(slide_vector.x, 0, slide_vector.y)).normalized()
		current_speed = sprinting_speed * (slide_timer.time_left / slide_timer.wait_time)
	elif interpolates:
		direction = lerp(direction, (transform.basis * Vector3(input_dir.x, 0, input_dir.y)).normalized(), delta * interpolation_speed)
	else:
		direction = (transform.basis * Vector3(input_dir.x, 0, input_dir.y)).normalized()
	
	if direction:
		velocity.x = direction.x * current_speed
		velocity.z = direction.z * current_speed
	else:
		velocity.x = move_toward(velocity.x, 0, current_speed)
		velocity.z = move_toward(velocity.z, 0, current_speed)

	move_and_slide()
	
func _crouch(delta):
	current_speed = crouch_speed
	bob_intensity = bob_intensity_crouch
	bob_index += bob_speed_crouch * delta
	neck.position.y = lerp( neck.position.y, standing_height + crouching_depth, delta * interpolation_speed) 
	standing_collision_shape.disabled = true
	crouching_shape.disabled = false
	crouching = true
	sprinting = false
	running = false
	
func _run(delta):
	current_speed = running_speed
	bob_intensity = bob_intensity_run
	bob_index += bob_speed_run * delta
	crouching = false
	sprinting = false
	running = true
	
func _sprint(delta):
	current_speed = sprinting_speed
	bob_intensity = bob_intensity_sprint
	bob_index += bob_speed_sprint * delta
	crouching = false
	sprinting = true
	running = false
		
func _stand(delta):
	neck.position.y = lerp( neck.position.y, standing_height, delta * interpolation_speed) 
	standing_collision_shape.disabled = false
	crouching_shape.disabled = true
	crouching = false

func _slide(delta, direction):
	slide_vector = direction
	slide_timer.start()
	sliding = true
	free_look = true
	
func _bob_head(delta):
	bob_vector.y = sin(bob_index)
	bob_vector.x = sin(bob_index / 2) + .5
	
	head.position.y = lerp(head.position.y, bob_vector.y * (bob_intensity / 2), delta * interpolation_speed)
	head.position.x = lerp(head.position.x, bob_vector.x * (bob_intensity), delta * interpolation_speed)

func _end_slide():
	sliding = false
	free_look = false
