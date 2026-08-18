/**************************************************
** Este código faz parte do Breakout.
**
** O Breakout é um software livre: você pode redistribuí-lo e/ou modificá-lo
** sob os termos da licença CC BY 4.0, conforme publicada pela
** Creative Commons, seja a versão 4 da Licença ou (a seu
** critério) qualquer versão posterior.
**************************************************/

using System.Numerics;

namespace LearnSilkNET.src;

// PowerUp herda seu estado e suas funções de renderização de
// GameObject, mas também armazena informações adicionais para
// indicar sua duração de atividade e se está ativado ou não.
// O tipo de PowerUp é armazenado como uma string.
public class PowerUp : GameObject
{
    // O tamanho de um bloco PowerUp
    public static Vector2 POWERUP_SIZE = new Vector2(60.0f, 20.0f);

    // Velocidade que um bloco PowerUp possui ao ser gerado
    public static Vector2 VELOCITY = new Vector2(0.0f, 150.0f);

    // estado do power-up
    public string Type;
    public float Duration;
    public bool Activated;

    // construtor
    public PowerUp(string type, Vector3 color, float duration, Vector2 position, Texture2D texture) : base(position, POWERUP_SIZE, texture, color, VELOCITY)
    {
        Type = type;
        Duration = duration;
    }
}
