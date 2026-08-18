#version 330 core
out vec4 FragColor;

in vec3 LightingColor; 

uniform vec3 objectColor;

void main()
{
   FragColor = vec4(LightingColor * objectColor, 1.0);
}

/*
Então, o que vemos?
Você pode ver (por si mesmo ou na imagem fornecida) a distinção clara dos dois triângulos na frente do
cubo. Esta 'faixa' é visível devido à interpolação de fragmentos. Na imagem de exemplo, podemos ver que o canto superior direito
o vértice da face frontal do cubo é iluminado com destaques especulares. Como o vértice superior direito do triângulo inferior direito é
acesos e os outros 2 vértices do triângulo não, os valores brilhantes interpolam para os outros 2 vértices. O mesmo
acontece para o triângulo superior esquerdo. Como as cores dos fragmentos intermediários não vêm diretamente da fonte de luz
mas são resultado de interpolação, a iluminação está incorreta nos fragmentos intermediários e nas partes superior esquerda e
triângulo inferior direito colidem em seu brilho, resultando em uma faixa visível entre os dois triângulos.

Este efeito ficará mais aparente ao usar formas mais complicadas.
*/
