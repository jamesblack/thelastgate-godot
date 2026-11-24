shader_type canvas_item;

// The dark UI's gold is one 90x102 texture that every line in a window shows a strip of, starting from the
// window's top-left corner. This keeps the drawn shape and its alpha (so the window fade still works) and takes
// the colour from that texture at the pixel's place in the window.
uniform sampler2D gold : filter_nearest, repeat_enable;
uniform vec2 window_origin;

varying vec2 canvas_position;

void vertex() {
	canvas_position = (MODEL_MATRIX * vec4(VERTEX, 0.0, 1.0)).xy;
}

void fragment() {
	ivec2 size = textureSize(gold, 0);
	ivec2 texel = ivec2(floor(canvas_position - window_origin));
	vec3 tile = texelFetch(gold, ivec2(texel.x % size.x, texel.y % size.y), 0).rgb;
	COLOR = vec4(tile, COLOR.a);
}
