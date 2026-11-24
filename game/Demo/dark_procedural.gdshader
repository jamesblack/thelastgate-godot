shader_type canvas_item;

// Flat colours only: the grain is value noise made here, in the window's pixel grid, so nothing is read from the
// art. Mode 0 draws a box from a frame profile (gold/gap counted in from the edge, fill inside). Mode 1 keeps the
// drawn shape, such as a 1 px outline, and recolours it as grained gold.
uniform int mode = 0;
uniform vec3 gold_colour : source_color = vec3(0.407, 0.369, 0.231);
uniform vec3 gap_colour : source_color = vec3(0.0235, 0.0353, 0.0667);
uniform vec3 fill_colour : source_color = vec3(0.071, 0.021, 0.021);
uniform vec3 mark_colour : source_color = vec3(0.337, 0.341, 0.298);
uniform float gold_grain = 6.0;
uniform float gold_variation = 0.16;
uniform float fill_grain = 3.5;
uniform float fill_variation = 0.25;
uniform float streak_angle = 0.6;
uniform vec2 window_origin;
// 0 = gold, 1 = blue gap, 2 = red gap, from the outside in.
uniform int profile[8];
uniform int profile_width = 6;
// Inside the profile: 0 = the red fill, 1 = the blue (the HUD background, also the gap colour), 2 = nothing.
uniform int fill_kind = 0;

varying vec2 local_position;
varying vec2 canvas_position;

float hash(vec2 cell) {
	return fract(sin(dot(cell, vec2(127.1, 311.7))) * 43758.5453);
}

float value_noise(vec2 point) {
	vec2 cell = floor(point);
	vec2 part = fract(point);
	vec2 blend = part * part * (3.0 - 2.0 * part);
	float bottom = mix(hash(cell), hash(cell + vec2(1.0, 0.0)), blend.x);
	float top = mix(hash(cell + vec2(0.0, 1.0)), hash(cell + vec2(1.0, 1.0)), blend.x);
	return mix(bottom, top, blend.y) * 2.0 - 1.0;
}

// Two octaves, stretched along a slant so the fill streaks diagonally like the art.
float grain(vec2 pixel, float size) {
	mat2 slant = mat2(vec2(cos(streak_angle), sin(streak_angle)), vec2(-sin(streak_angle), cos(streak_angle)));
	vec2 point = slant * pixel / size * vec2(1.0, 1.8);
	return value_noise(point) * 0.7 + value_noise(point * 2.3 + 17.0) * 0.3;
}

vec3 grained(vec3 base, vec2 pixel, float size, float variation) {
	return base * (1.0 + variation * grain(pixel, size));
}

void vertex() {
	local_position = VERTEX;
	canvas_position = (MODEL_MATRIX * vec4(VERTEX, 0.0, 1.0)).xy;
}

void fragment() {
	vec2 pixel = floor(canvas_position - window_origin) + 0.5;
	vec3 gold = grained(gold_colour, pixel, gold_grain, gold_variation);
	vec3 colour = gold;
	if (mode == 0 || mode == 2 || mode == 3) {
		ivec2 local = ivec2(floor(local_position));
		// UV runs 0..1 across the rect while local_position runs 0..width in pixels, so their rates of change from
		// one pixel to the next give the rect's own size, whatever it has been resized to.
		vec2 uv_step = vec2(dFdx(UV.x), dFdy(UV.y));
		vec2 pixel_step = vec2(dFdx(local_position.x), dFdy(local_position.y));
		ivec2 size = ivec2(round(pixel_step / max(abs(uv_step), vec2(1e-6)) * sign(uv_step)));
		int inset = min(min(local.x, local.y), min(size.x - 1 - local.x, size.y - 1 - local.y));
		// Mode 3 is a circle: the profile is counted in from the rim instead of from the nearest edge.
		if (mode == 3) {
			float radius = float(min(size.x, size.y)) * 0.5;
			float from_centre = length(vec2(local) + 0.5 - vec2(size) * 0.5);
			if (from_centre > radius) {
				discard;
			}
			inset = int(floor(radius - from_centre));
		}
		if (inset >= profile_width) {
			if (fill_kind == 2) {
				discard;
			}
			colour = grained(fill_kind == 1 ? gap_colour : fill_colour, pixel, fill_grain, fill_variation);
		} else if (profile[inset] == 1) {
			colour = gap_colour;
		} else if (profile[inset] == 2) {
			colour = grained(fill_colour, pixel, fill_grain, fill_variation);
		}
		// Mode 2 is the close button: the same box with an X across it, inset two pixels from the frame.
		if (mode == 2 && inset >= profile_width + 1) {
			vec2 centred = vec2(local) + 0.5 - vec2(size) * 0.5;
			if (min(abs(centred.x - centred.y), abs(centred.x + centred.y)) <= 1.2) {
				colour = mark_colour;
			}
		}
	}
	COLOR = vec4(colour, COLOR.a);
}
