/**************************************************
** Este código faz parte do Breakout.
**
** O Breakout é um software livre: você pode redistribuí-lo e/ou modificá-lo
** sob os termos da licença CC BY 4.0, conforme publicada pela
** Creative Commons, seja a versão 4 da Licença ou (a seu
** critério) qualquer versão posterior.
**************************************************/

using System.Numerics;
using Silk.NET.Input;
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

    // Tamanho inicial da raquete do jogador
    public Vector2 PLAYER_SIZE = new Vector2(100.0f, 20.0f);

    // Velocidade inicial da raquete do jogador
    public const float PLAYER_VELOCITY = 500.0f;

    // estado do jogo
    public GameState State;
    public bool[] Keys = new bool[1024];
    public uint Width, Height;
    public List<GameLevel> Levels = [];
    public int Level;

    // Dados de estado relacionados ao jogo
    public SpriteRenderer Renderer = null!;
    public GameObject Player = null!;

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
        ResourceManager.LoadTexture(_gl, "res/textures/background.jpg", false, "background");
        ResourceManager.LoadTexture(_gl, "res/textures/awesomeface.png", true, "face");
        ResourceManager.LoadTexture(_gl, "res/textures/block.png", false, "block");
        ResourceManager.LoadTexture(_gl, "res/textures/block_solid.png", false, "block_solid");
        ResourceManager.LoadTexture(_gl, "res/textures/paddle.png", true, "paddle");

        // carregar níveis
        GameLevel one = new GameLevel();
        GameLevel two = new GameLevel();
        GameLevel three = new GameLevel();
        GameLevel four = new GameLevel();

        one.Load("src/levels/one.lvl", Width, Height / 2);
        two.Load("src/levels/two.lvl", Width, Height / 2);
        three.Load("src/levels/three.lvl", Width, Height / 2);
        four.Load("src/levels/four.lvl", Width, Height / 2);

        Levels.Add(one);
        Levels.Add(two);
        Levels.Add(three);
        Levels.Add(four);

        Level = 0;

        // configurar objetos do jogo
        Vector2 playerPos = new Vector2(Width / 2.0f - PLAYER_SIZE.X / 2.0f, Height - PLAYER_SIZE.Y);

        Player = new GameObject(playerPos, PLAYER_SIZE, ResourceManager.GetTexture("paddle"));
    }

    // loop do jogo
    public void ProceessInput(float dt)
    {
        if (State == GameState.GAME_ACTIVE)
        {
            float velocity = PLAYER_VELOCITY * dt;

            // mover o tabuleiro do jogador
            if (Keys[(int)Key.A])
            {
                if (Player.Position.X >= 0.0f)
                {
                    Player.Position.X -= velocity;
                }
            }
            if (Keys[(int)Key.D])
            {
                if (Player.Position.X <= Width - Player.Size.X)
                {
                    Player.Position.X += velocity;
                }
            }
        }
    }

    public void Update(float dt)
    {
        
    }

    public void Render()
    {
        if (State == GameState.GAME_ACTIVE)
        {
            // desenhar fundo
            Renderer.DrawSprite(ResourceManager.GetTexture("background"), new Vector2(0.0f, 0.0f), new Vector2(Width, Height), 0.0f);

            // desenhar nível
            Levels[Level].Draw(Renderer);

            // desenhar jogador
            Player.Draw(Renderer);
        }
    }
}
