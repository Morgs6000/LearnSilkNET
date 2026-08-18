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

// Representa o estado atual do jogo
public enum GameState
{
    GAME_ACTIVE,
    GAME_MENU,
    GAME_WIN
}

// A classe Game encapsula todo o estado e a funcionalidade relacionados ao jogo.
// Ela reúne todos os dados do jogo em uma única classe para
// facilitar o acesso a cada um dos componentes e o gerenciamento.
public class Game : IDisposable
{
    private GL _gl;

    // estado do jogo
    public GameState State;
    public bool[] Keys = new bool[1024];
    public uint Width, Height;

    // Dados de estado relacionados ao jogo
    public SpriteRenderer Renderer = null!;

    // construtor
    public Game(GL gl, uint width, uint height)
    {
        _gl = gl;

        State = GameState.GAME_ACTIVE;
        
        Width = width;
        Height = height;
    }

    // destrutor
    public void Dispose()
    {
        Renderer.Dispose();
    }

    // inicializar o estado do jogo (carregar todos os shaders/texturas/níveis)
    public void Init()
    {
        // carregar shaders
        ResourceManager.LoadShader(_gl, "src/sprite.vs", "src/sprite.fs", null, "sprite");

        // configurar shaders
        Matrix4x4 projection = Matrix4x4.CreateOrthographicOffCenter(
            left:        0.0f, 
            right:       (float)Width, 
            bottom:      (float)Height, 
            top:         0.0f, 
            zNearPlane: -1.0f, 
            zFarPlane:   1.0f
        );

        ResourceManager.GetShader("sprite").Use().SetInteger("image", 0);
        ResourceManager.GetShader("sprite").SetMatrix4("projection", projection);

        // definir controles específicos de renderização
        Renderer = new SpriteRenderer(_gl, ResourceManager.GetShader("sprite"));

        // carregar texturas
        ResourceManager.LoadTexture(_gl, "res/textures/awesomeface.png", true, "face");
    }

    // loop do jogo
    public void ProceessInput(float dt)
    {
        
    }

    public void Update(float dt)
    {
        
    }

    public void Render()
    {
        Renderer.DrawSprite(ResourceManager.GetTexture("face"), new Vector2(200.0f, 200.0f), new Vector2(300.0f, 400.0f), 45.0f, new Vector3(0.0f, 1.0f, 0.0f));
    }
}
