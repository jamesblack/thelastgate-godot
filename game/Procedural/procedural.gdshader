shader_type canvas_item;

// The dark window look from flat colours: value-noise grain over the gold and the fill, and frames drawn as a
// profile of rings counted in from the shape's own edge, so any size draws cleanly. It reads no textures.
uniform int shape = 0;
uniform vec3 gold_colour : source_color = vec3(0.407, 0.369, 0.231);
uniform vec3 blue_colour : source_color = vec3(0.0235, 0.0353, 0.0667);
uniform vec3 red_colour : source_color = vec3(0.071, 0.021, 0.021);
uniform vec3 selected_colour : source_color = vec3(0.07, 0.11, 0.19);
uniform vec3 green_colour : source_color = vec3(0.0, 0.38, 0.0);
uniform vec3 mark_colour : source_color = vec3(0.337, 0.341, 0.298);
uniform vec3 white_colour : source_color = vec3(0.86, 0.85, 0.80);
uniform float gold_grain = 6.0;
uniform float gold_variation = 0.16;
uniform float fill_grain = 3.5;
uniform float fill_variation = 0.25;
uniform float streak_angle = 0.6;
uniform int profile[8];
uniform int profile_width = 0;
uniform int fill = 0;
uniform int edges = 15;
uniform bool solid_rings = false;
// An isometric diamond (x, y, width, height in the panel's pixels) cut out of the panel; zero height cuts nothing.
uniform vec4 hole = vec4(0.0);

const int SHAPE_BOX = 0;
const int SHAPE_LINE = 1;
const int SHAPE_CLOSE = 2;
const int SHAPE_CIRCLE = 3;
const int SHAPE_DIAMOND = 4;
const int RING_GOLD = 0;
const int RING_BLUE = 1;
const int RING_RED = 2;
const int RING_WHITE = 3;
const int FILL_RED = 0;
const int FILL_BLUE = 1;
const int FILL_NONE = 2;
const int FILL_SELECTED = 3;
const int FILL_GREEN = 4;
const int EDGE_TOP = 1;
const int EDGE_RIGHT = 2;
const int EDGE_BOTTOM = 4;
const int EDGE_LEFT = 8;
// Far enough in that a side without rings never decides the inset.
const int NO_EDGE = 1 << 20;

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

// Two octaves, stretched along a slant so the grain streaks diagonally like the art.
float grain(vec2 pixel, float size) {
	mat2 slant = mat2(vec2(cos(streak_angle), sin(streak_angle)), vec2(-sin(streak_angle), cos(streak_angle)));
	vec2 point = slant * pixel / size * vec2(1.0, 1.8);
	return value_noise(point) * 0.7 + value_noise(point * 2.3 + 17.0) * 0.3;
}

vec3 grained(vec3 base, vec2 pixel, float size, float variation) {
	return base * (1.0 + variation * grain(pixel, size));
}

vec3 ring_colour(int ring, vec2 pixel) {
	if (ring == RING_BLUE) {
		return blue_colour;
	}
	if (ring == RING_RED) {
		return grained(red_colour, pixel, fill_grain, fill_variation);
	}
	if (ring == RING_WHITE) {
		return white_colour;
	}
	return grained(gold_colour, pixel, gold_grain, gold_variation);
}

void vertex() {
	local_position = VERTEX;
	canvas_position = (MODEL_MATRIX * vec4(VERTEX, 0.0, 1.0)).xy;
}

void fragment() {
	vec2 pixel = floor(canvas_position) + 0.5;
	if (shape == SHAPE_LINE) {
		COLOR = vec4(ring_colour(profile_width > 0 ? profile[0] : RING_GOLD, pixel), 1.0) * COLOR;
	} else {
		ivec2 local = ivec2(floor(local_position));
		// UV runs 0..1 across the rect while local_position runs 0..width in pixels, so their rates of change from
		// one pixel to the next give the rect's own size, whatever it has been resized to.
		vec2 uv_step = vec2(dFdx(UV.x), dFdy(UV.y));
		vec2 pixel_step = vec2(dFdx(local_position.x), dFdy(local_position.y));
		ivec2 size = ivec2(round(pixel_step / max(abs(uv_step), vec2(1e-6)) * sign(uv_step)));
		int from_left = (edges & EDGE_LEFT) != 0 ? local.x : NO_EDGE;
		int from_top = (edges & EDGE_TOP) != 0 ? local.y : NO_EDGE;
		int from_right = (edges & EDGE_RIGHT) != 0 ? size.x - 1 - local.x : NO_EDGE;
		int from_bottom = (edges & EDGE_BOTTOM) != 0 ? size.y - 1 - local.y : NO_EDGE;
		int inset = min(min(from_left, from_top), min(from_right, from_bottom));
		if (shape == SHAPE_CIRCLE) {
			float radius = float(min(size.x, size.y)) * 0.5;
			float from_centre = length(vec2(local) + 0.5 - vec2(size) * 0.5);
			if (from_centre > radius) {
				discard;
			}
			inset = int(floor(radius - from_centre));
		}
		// An isometric diamond, its rings stepping two pixels across for each row down like the art's 2:1 lines.
		// Each ring is a three-pixel run per row, so neighbouring rows' runs overlap by one pixel and read as unbroken.
		if (hole.w > 0.0) {
			int hole_row = local.y - int(hole.y);
			int hole_rows_from_point = min(hole_row, int(hole.w) - 1 - hole_row);
			float hole_from_middle = abs(float(local.x) + 0.5 - (hole.x + hole.z * 0.5));
			if (hole_rows_from_point >= 0 && 2.0 * float(hole_rows_from_point) - hole_from_middle >= -1.5) {
				discard;
			}
		}
		if (shape == SHAPE_DIAMOND) {
			int rows_from_point = min(local.y, size.y - 1 - local.y);
			float from_middle = abs(float(local.x) + 0.5 - float(size.x) * 0.5);
			float slack = 2.0 * float(rows_from_point) - from_middle;
			if (slack < -1.5) {
				discard;
			}
			inset = int(floor((slack + 1.5) / 3.0));
		}
		vec3 colour;
		bool ring = inset < profile_width;
		if (ring) {
			colour = ring_colour(profile[inset], pixel);
		} else if (fill == FILL_NONE) {
			discard;
		} else if (fill == FILL_GREEN) {
			colour = grained(green_colour, pixel, gold_grain, gold_variation);
		} else {
			vec3 fill_colour = fill == FILL_BLUE ? blue_colour : fill == FILL_SELECTED ? selected_colour : red_colour;
			colour = grained(fill_colour, pixel, fill_grain, fill_variation);
		}
		if (shape == SHAPE_CLOSE && inset >= profile_width + 1) {
			vec2 centred = vec2(local) + 0.5 - vec2(size) * 0.5;
			if (min(abs(centred.x - centred.y), abs(centred.x + centred.y)) <= 1.2) {
				colour = mark_colour;
			}
		}
		COLOR = vec4(colour, 1.0) * COLOR;
		if (ring && solid_rings) {
			COLOR.a = 1.0;
		}
	}
}
