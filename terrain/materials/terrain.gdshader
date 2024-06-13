shader_type spatial;
render_mode specular_disabled;

uniform vec2 uv_scale = vec2(1.0,1.0);
uniform sampler2DArray texture_array : filter_nearest, source_color, repeat_enable;

varying vec3 textures;

void vertex() {
	textures = CUSTOM0.xyz;
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

    // Sample the textures from the texture array
    vec4 tex0 = texture(texture_array, vec3(UV * uv_scale, index0));
    vec4 tex1 = texture(texture_array, vec3(UV * uv_scale, index1));
    vec4 tex2 = texture(texture_array, vec3(UV * uv_scale, index2));

    // Blend the textures based on the blend factors
    vec4 blended_texture = (tex0 * blend_factors.r) +
                           (tex1 * blend_factors.g) +
                           (tex2 * blend_factors.b);

    ALBEDO = blended_texture.rgb;
}
