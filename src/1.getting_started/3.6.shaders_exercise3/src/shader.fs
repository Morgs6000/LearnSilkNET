#version 330 core
out vec4 FragColor;

// in vec3 ourColor;
in vec3 ourPosition;

void main()
{
    FragColor = vec4(ourPosition, 1.0f); // observe como o valor da posição é interpolado linearmente para obter todas as diferentes cores
}

/*
Resposta à pergunta: Você sabe por que o lado inferior esquerdo está preto?
-- --------------------------------------------------------------------
Pense nisto por um instante: a saída de cor do nosso fragmento é igual à coordenada (interpolada) do
triângulo. Qual é a coordenada do ponto inferior esquerdo do nosso triângulo? É (-0,5f, -0,5f, 0,0f). Como os
valores de x e y são negativos, eles são limitados (*clamped*) ao valor de 0,0f. Isso ocorre até chegar aos lados centrais do
triângulo, visto que, a partir desse ponto, os valores passarão a ser interpolados positivamente novamente. Valores de 0,0f são, naturalmente, pretos,
e isso explica o lado preto do triângulo.
*/
