shader_type spatial;
render_mode specular_disabled, world_vertex_coords;

uniform vec2 uv_scale = vec2(1.0,1.0);
uniform sampler2DArray texture_array : filter_nearest, source_color, repeat_enable;
uniform sampler2D detail_height_map;
uniform float rock_face_texture;
uniform float sand_texture;
uniform float slope_threshold = 0.75;
uniform float sea_level = 0.0;
uniform float beach_slope_threshold = 0.9;
uniform float beach_height = 3.0;

varying vec3 textures;
varying vec3 world_pos;
varying vec3 world_norm;

void vertex() {
	textures = CUSTOM0.xyz;
	world_pos = (MODEL_MATRIX * vec4(VERTEX, 1.0)).xyz;
	//NORMAL = normalize((INV_VIEW_MATRIX * vec4(NORMAL, 0.0)).xyz);
	
}

void fragment() {
    // Get the blend factors from the vertex colors
    vec4 blend_factors = COLOR;

    // Get the texture indices from UV2
    float index0 = textures.x;
    float index1 = textures.y;
    float index2 = textures.z;
	ROUGHNESS = 1.0;
	METALLIC = 0.0;
	SPECULAR = 0.0;
	//vec3 normal = normalize(NORMAL);
	//vec3 world_normal = normalize((MODEL_NORMAL_MATRIX * vec3(normal)).xyz);
	float slope = abs(dot(normalize((INV_VIEW_MATRIX * vec4(NORMAL, 0.0)).xyz), vec3(0.0, 1.0, 0.0)));
	
	//if(slope < slope_threshold) {
		//index0 = rock_face_texture;
		//index1 = rock_face_texture;
		//index2 = rock_face_texture;
	//}

    // Sample the textures from the texture array
    vec4 tex0 = texture(texture_array, vec3(UV * uv_scale, index0));
    vec4 tex1 = texture(texture_array, vec3(UV * uv_scale, index1));
    vec4 tex2 = texture(texture_array, vec3(UV * uv_scale, index2));
	vec4 tex_rock = texture(texture_array, vec3(UV * uv_scale, rock_face_texture));
	vec4 tex_sand = texture(texture_array, vec3(UV * uv_scale, sand_texture));
	
    // Blend the textures based on the blend factors
    vec4 blended_texture = (tex0 * blend_factors.r) +
                           (tex1 * blend_factors.g) +
                           (tex2 * blend_factors.b);
	if(world_pos.y < sea_level + beach_height) {
		float amt = clamp( abs(world_pos.y - (sea_level + beach_height)) / (5.0)* 0.5, 0.0, 1.0);
		blended_texture = (blended_texture * (1.0 - amt)) + (tex_sand * amt);
	}
	
	blended_texture = (blended_texture * (slope))  + (tex_rock * (1.0  - slope));
	if( world_pos.y <= sea_level + beach_height && world_pos.y >= sea_level - 2.0) {
		float amt = clamp( abs(world_pos.y - (sea_level + beach_height)) / (sea_level + beach_height) * 3.0, 0.0, 1.0);
		blended_texture = (blended_texture * (1.0 - amt)) + (tex_sand * amt * (slope / beach_slope_threshold));
	}
	
	
	

    ALBEDO = blended_texture.rgb;
}
