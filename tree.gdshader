shader_type spatial;
render_mode depth_prepass_alpha, cull_disabled,  specular_disabled;

uniform sampler2D texture_albedo : source_color,filter_nearest;
uniform vec4 transmission : source_color;

uniform float sway_speed = 1.0;
uniform vec3 sway_strength = vec3(0.5, 0.5, 0.5);
uniform float sway_phase_len = .5;
uniform float sway_scale = 1;
uniform float pivot = 1;
uniform float wave = .5;
global uniform vec3 wind_direction = vec3(1, 0, 0);
uniform vec3 shake_strength = vec3(1, 1, 1); 
uniform float shake_speed = 1.0;
uniform sampler2D wind_noise_texture;
instance uniform vec3 rand;

vec4 SmoothCurve( vec4 x ) {   
	return x * x *( 3.0 - 2.0 * x ); 
	}
vec4 TriangleWave( vec4 x ) {   
	return abs( fract( x + 0.5 ) * 2.0 - 1.0 ); 
	}
vec4 SmoothTriangleWave( vec4 x ) {   
	return SmoothCurve( TriangleWave( x ) ); 
	} 
	
float random (vec2 uv) {
    return fract(sin(dot(uv.xy,
        vec2(12.9898,78.233))) * 43758.5453123);
}

void vertex() {
	vec2 world_xy = (MODEL_MATRIX * vec4(VERTEX, 1.0)).xz;
	float noise = (texture(wind_noise_texture, NODE_POSITION_WORLD.xz / 40.0).r * 2.0) - 1.0;
	float local_time = TIME + (noise * 1000.0);
	//local_time = TIME + (rand.r * 10.0);
	//float fObjPhase = dot(NODE_POSITION_WORLD.xyz, vec3(1.0,1.0,1.0)); 
	//float fBranchPhase = 8.0; 
	//float fDetailPhase = 5.0;
	//float fVtxPhase = dot(VERTEX.xyz, vec3(fDetailPhase, fBranchPhase, 1));  
	//vec2 vWavesIn = TIME * 0.03 * vec2(fVtxPhase, fBranchPhase ); // 1.975, 0.793, 0.375, 0.193 are good frequencies    
	//vec4 vWaves = (fract( vWavesIn.xxyy * vec4(1.975, 0.793, 0.375, 0.193) ) * 0.2 - 1.0 ) * shake_speed; 
	//vWaves = SmoothTriangleWave( vWaves ); 
	//vec2 vWavesSum = vWaves.xz + vWaves.yw; 
	//VERTEX.xyz += vWavesSum.xxy * vec3(25.0 * length( wind_direction) * sway_strength.xy * NORMAL.xy, sway_scale * sway_speed) * shake_strength * wind_direction * 3.0; 
	
	vec3 strength = COLOR.r * sway_strength;
	float pivot_angle = cos(local_time) * 0.1 * pivot * length(wind_direction) * noise ;
	mat2 rotation_matrix = mat2(vec2(cos(pivot_angle), -sin(pivot_angle)), vec2(sin(pivot_angle), cos(pivot_angle)));
	VERTEX.xz += cos(local_time + VERTEX.y / 10.0) * length(wind_direction) * wave * COLOR.r * noise;
	VERTEX.xz = mix(VERTEX.xz, rotation_matrix * VERTEX.xz , wave);
	VERTEX.x += sin(local_time * sway_phase_len * sway_speed * noise) * COLOR.r * wind_direction.x ;
	VERTEX.z += sin(local_time * sway_phase_len * sway_speed * noise) * COLOR.r * wind_direction.z ;
    VERTEX.x += sin(VERTEX.x  * shake_speed * 100.0 * 1.123 + local_time * length(wind_direction)) * shake_strength.x * length(wind_direction ) * 0.1 ;
    VERTEX.y += sin(VERTEX.y  * shake_speed * 100.0 + local_time * length(wind_direction) * 1.12412) * shake_strength.y * length(wind_direction ) * 0.1 ;
    VERTEX.z += sin(VERTEX.z  * shake_speed * 100.0 * 0.9123 + local_time * length(wind_direction) * 1.3123) * shake_strength.z * length(wind_direction ) * 0.1 ;
}

void fragment() {
	// Called for every pixel the material is visible on.
	vec4 albedo_tex = texture(texture_albedo, UV);
    ALBEDO = albedo_tex.rgb;
    ALPHA = albedo_tex.a;
	ALPHA_SCISSOR_THRESHOLD = .75;
    METALLIC = 0.0;
    ROUGHNESS = 1.0;
}


//void light() {
	// Called for every pixel for every light affecting the material.
	// Uncomment to replace the default light processing function with this one.
//}
