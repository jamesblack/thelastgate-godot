shader_type canvas_item;

// A dark UI box drawn without a sprite: the frame is a profile of gold and gap pixels counted in from the nearest
// edge, and everything inside it is the fill. Gold and fill are the art's 90x102 textures, both starting at the
// window's top-left corner, so a box lines up with the window it sits in at any size.
uniform sampler2D gold : filter_nearest, repeat_enable;
uniform sampler2D fill : filter_nearest, repeat_enable;
uniform vec2 window_origin;
uniform vec2 box_size;
uniform vec3 gap_colour : source_color = vec3(0.0235, 0.0353, 0.0667);
// 0 = gold, 1 = gap, from the outside in.
uniform int profile[8];
uniform int profile_width = 6;

varying vec2 local_position;
varying vec2 canvas_position;

vec3 tile(sampler2D source, ivec2 texel) {
	ivec2 size = textureSize(source, 0);
	return texelFetch(source, ivec2(texel.x % size.x, texel.y % size.y), 0).rgb;
}

void vertex() {
	local_position = VERTEX;
	canvas_position = (MODEL_MATRIX * vec4(VERTEX, 0.0, 1.0)).xy;
}

void fragment() {
	ivec2 local = ivec2(floor(local_position));
	ivec2 size = ivec2(box_size);
	int inset = min(min(local.x, local.y), min(size.x - 1 - local.x, size.y - 1 - local.y));
	ivec2 texel = ivec2(floor(canvas_position - window_origin));
	vec3 colour;
	if (inset >= profile_width) {
		colour = tile(fill, texel);
	} else if (profile[inset] == 1) {
		colour = gap_colour;
	} else {
		colour = tile(gold, texel);
	}
	COLOR = vec4(colour, COLOR.a);
}
