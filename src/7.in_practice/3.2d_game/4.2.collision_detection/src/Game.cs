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

    // Velocidade inicial da bola
    public Vector2 INITIAL_BALL_VELOCITY = new Vector2(100.0f, -350.0f);

    // Raio do objeto bola
    public const float BALL_RADIUS = 12.5f;

    // estado do jogo
    public GameState State;
    public bool[] Keys = new bool[1024];
    public uint Width, Height;
    public List<GameLevel> Levels = [];
    public int Level;

    // Dados de estado relacionados ao jogo
    public SpriteRenderer Renderer = null!;
    public GameObject Player = null!;
    public BallObject Ball = null!;

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

        Vector2 ballPos = playerPos + new Vector2(PLAYER_SIZE.X / 2.0f - BALL_RADIUS, -BALL_RADIUS * 2.0f);

        Ball = new BallObject(ballPos, BALL_RADIUS, INITIAL_BALL_VELOCITY, ResourceManager.GetTexture("face"));
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

                    if (Ball.Stuck)
                    {
                        Ball.Position.X -= velocity;
                    }
                }
            }
            if (Keys[(int)Key.D])
            {
                if (Player.Position.X <= Width - Player.Size.X)
                {
                    Player.Position.X += velocity;

                    if (Ball.Stuck)
                    {
                        Ball.Position.X += velocity;
                    }
                }
            }
            if (Keys[(int)Key.Space])
            {
                Ball.Stuck = false;
            }
        }
    }

    public void Update(float dt)
    {
        // atualizar objetos
        Ball.Move(dt, Width);

        // verificar colisões
        DoCollisions();
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

            Ball.Draw(Renderer);
        }
    }

    private bool CheckCollision(GameObject one, GameObject two) // AABB - AABB collision
    {
        // colisão no eixo X?
        bool collisionX = one.Position.X + one.Size.X >= two.Position.X &&
            two.Position.X + two.Size.X >= one.Position.X;
        
        // colisão no eixo Y?
        bool collisionY = one.Position.Y + one.Size.Y >= two.Position.Y &&
            two.Position.Y + two.Size.Y >= one.Position.Y;
        
        // colisão apenas se ocorrer em ambos os eixos
        return collisionX && collisionY;
    }

    private bool CheckCollision(BallObject one, GameObject two) // AABB - Circle collision
    {
        // obter primeiro o ponto central do círculo
        Vector2 center = new Vector2(one.Position.X + one.Radius, one.Position.Y + one.Radius);

        // calcular informações da AABB (centro, semi-extensões)
        Vector2 aabb_half_extents = new Vector2(two.Size.X / 2.0f, two.Size.Y / 2.0f);
        Vector2 aabb_center = new Vector2(
            two.Position.X + aabb_half_extents.X,
            two.Position.Y + aabb_half_extents.Y
        );

        // obtém o vetor diferença entre os dois centros
        Vector2 difference = center - aabb_center;
        Vector2 clamped = Vector2.Clamp(difference, -aabb_half_extents, aabb_half_extents);

        // adicionamos o valor fixado a AABB_center e obtemos o valor da caixa mais próxima do círculo
        Vector2 closet = aabb_center + clamped;

        // obtém o vetor entre o centro do círculo e o ponto mais próximo da AABB e verifica se o comprimento é menor ou igual ao raio
        difference = closet - center;

        return difference.Length() < one.Radius;
    }

    public void DoCollisions()
    {
        foreach (GameObject box in Levels[Level].Bricks)
        {
            if (!box.Destroyed)
            {
                if (CheckCollision(Ball, box))
                {
                    if (!box.IsSolid)
                    {
                        box.Destroyed = true;
                    }
                }
            }
        }
    }
}
