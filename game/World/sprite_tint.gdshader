shader_type canvas_item;

// The tints, applied after the light has darkened the sprite through its modulate colour, and its
// highlight, which doubles the brightness after the tints, and the selected character's green.
uniform bool grey = false;
uniform bool infrared = false;
uniform bool bloody = false;
uniform bool underwater = false;
uniform bool highlight = false;
uniform bool selected = false;

void fragment() {
	vec4 color = COLOR;
	if (grey) {
		float average = (color.r + color.g + color.b) / 3.0 * 0.5;
		color.rgb = vec3(average);
	}
	if (bloody) {
		float average = (color.r + color.g + color.b) / 3.0;
		color.rgb = vec3(average, 0.0, 0.0);
	} else if (infrared) {
		float average = (color.r + color.g + color.b) / 3.0;
		color.rgb = average * vec3(0.95, 0.6, 0.55);
	}
	if (underwater) {
		float average = (color.r + color.g + color.b) / 3.0;
		color.r = (color.r + average) / 3.0;
		color.g = (color.g + average) / 2.0;
		color.b = color.b + average * 1.2;
		color.rgb = clamp(color.rgb, 0.0, 1.0);
	}
	if (highlight) {
		color.rgb *= 2.0;
	}
	if (selected) {
		color.g += 0.5;
	}
	COLOR = color;
}
