shader_type spatial;

uniform sampler2DArray texture_array;

void fragment() {
    // Get the blend factors from the vertex colors
    vec4 blend_factors = COLOR;

    // Get the texture indices from UV2
    int index0 = int(UV2.x);
    int index1 = int(UV2.y);
    int index2 = int(UV2.z);

    // Sample the textures from the texture array
    vec4 tex0 = texture(texture_array, vec3(UV, index0));
    vec4 tex1 = texture(texture_array, vec3(UV, index1));
    vec4 tex2 = texture(texture_array, vec3(UV, index2));

    // Blend the textures based on the blend factors
    vec4 blended_texture = tex0 * blend_factors.r +
                           tex1 * blend_factors.g +
                           tex2 * blend_factors.b;

    ALBEDO = blended_texture.rgb;
}
