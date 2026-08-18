#version 330 core
out vec4 FragColor;

in vec3 ourColor;
in vec2 TexCoord;

// amostrador de textura
uniform sampler2D texture1;
uniform sampler2D texture2;

void main()
{
	// interpola linearmente entre ambas as texturas (80% container, 20% awesomeface)
	FragColor = mix(texture(texture1, TexCoord), texture(texture2, TexCoord), 0.2);
}
