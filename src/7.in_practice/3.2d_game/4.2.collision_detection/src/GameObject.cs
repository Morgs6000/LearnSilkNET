/**************************************************
** Este código faz parte do Breakout.
**
** O Breakout é um software livre: você pode redistribuí-lo e/ou modificá-lo
** sob os termos da licença CC BY 4.0, conforme publicada pela
** Creative Commons, seja a versão 4 da Licença ou (a seu
** critério) qualquer versão posterior.
**************************************************/

using System.Numerics;
using Silk.NET.OpenGL;

namespace LearnSilkNET.src;

// Objeto contêiner para armazenar todo o estado relevante para uma única
// entidade de objeto de jogo. É provável que cada objeto no jogo precise
// do estado mínimo descrito em GameObject.
public class GameObject
{
    // estado do objeto
    public Vector2 Position, Size, Velocity;
    public Vector3 Color;
    public float Rotation;
    public bool IsSolid;
    public bool Destroyed;

    // estado de renderização
    public Texture2D Sprite;

    // construtor(es)
    public GameObject(GL gl)
    {
        Position = new Vector2(0.0f, 0.0f);
        Size = new Vector2(1.0f, 1.0f);
        Velocity = new Vector2(0.0f);

        Color = new Vector3(1.0f);

        Rotation = 0.0f;

        Sprite = new Texture2D(gl);

        IsSolid = false;
        Destroyed = false;
    }

    public GameObject(Vector2 pos, Vector2 size, Texture2D sprite, Vector3? color = null, Vector2? velocity = null)
    {
        Vector3 _color = color ?? new Vector3(1.0f);
        Vector2 _velocity = velocity ?? new Vector2(0.0f, 0.0f);

        Position = pos;
        Size = size;
        Velocity = _velocity;

        Color = _color;

        Rotation = 0.0f;

        Sprite = sprite;

        IsSolid = false;
        Destroyed = false;
    }

    // desenhar sprite
    public virtual void Draw(SpriteRenderer renderer)
    {
        renderer.DrawSprite(Sprite, Position, Size, Rotation, Color);
    }
}
