/**************************************************
** Este código faz parte do Breakout.
**
** O Breakout é um software livre: você pode redistribuí-lo e/ou modificá-lo
** sob os termos da licença CC BY 4.0, conforme publicada pela
** Creative Commons, seja a versão 4 da Licença ou (a seu
** critério) qualquer versão posterior.
**************************************************/

using System.Numerics;
using Silk.NET.GLFW;
using Silk.NET.Input;
using Silk.NET.OpenGL;

namespace LearnSilkNET.src;

// Define um typedef Collision que representa dados de colisão
using Collision = (bool collision, Direction direction, Vector2 difference); // <colisão?, qual direção?, vetor diferença entre centro e ponto mais próximo>

// Representa o estado atual do jogo
public enum GameState
{
    GAME_ACTIVE,
    GAME_MENU,
    GAME_WIN
}

// Representa as quatro direções possíveis (de colisão)
public enum Direction
{
    UP,
    RIGHT,
    DOWN,
    LEFT
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
    public List<PowerUp> PowerUps = [];
    public int Level;

    // Dados de estado relacionados ao jogo
    public SpriteRenderer Renderer = null!;
    public GameObject Player = null!;
    public BallObject Ball = null!;
    public ParticleGenerator Particles = null!;
    public PostProcessor Effects = null!;

    public float ShakeTime = 0.0f;

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
        ResourceManager.LoadShader(_gl, "src/particle.vs", "src/particle.fs", null, "particle");
        ResourceManager.LoadShader(_gl, "src/post_processing.vs", "src/post_processing.fs", null, "postprocessing");

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

        ResourceManager.GetShader("particle").Use().SetInteger("sprite", 0);
        ResourceManager.GetShader("particle").SetMatrix4("projection", projection);

        // carregar texturas
        ResourceManager.LoadTexture(_gl, "res/textures/background.jpg", false, "background");
        ResourceManager.LoadTexture(_gl, "res/textures/awesomeface.png", true, "face");
        ResourceManager.LoadTexture(_gl, "res/textures/block.png", false, "block");
        ResourceManager.LoadTexture(_gl, "res/textures/block_solid.png", false, "block_solid");
        ResourceManager.LoadTexture(_gl, "res/textures/paddle.png", true, "paddle");
        ResourceManager.LoadTexture(_gl, "res/textures/particle.png", true, "particle");
        ResourceManager.LoadTexture(_gl, "res/textures/powerup_speed.png", true, "powerup_speed");
        ResourceManager.LoadTexture(_gl, "res/textures/powerup_sticky.png", true, "powerup_sticky");
        ResourceManager.LoadTexture(_gl, "res/textures/powerup_increase.png", true, "powerup_increase");
        ResourceManager.LoadTexture(_gl, "res/textures/powerup_confuse.png", true, "powerup_confuse");
        ResourceManager.LoadTexture(_gl, "res/textures/powerup_chaos.png", true, "powerup_chaos");
        ResourceManager.LoadTexture(_gl, "res/textures/powerup_passthrough.png", true, "powerup_passthrough");

        // definir controles específicos de renderização
        Renderer = new SpriteRenderer(_gl, ResourceManager.GetShader("sprite"));

        Particles = new ParticleGenerator(_gl, ResourceManager.GetShader("particle"), ResourceManager.GetTexture("particle"), 500);

        Effects = new PostProcessor(_gl, ResourceManager.GetShader("postprocessing"), Width, Height);

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

        // atualizar partículas
        Particles.Update(dt, Ball, 2, new Vector2(Ball.Radius / 2.0f));

        // atualizar PowerUps
        UpdatePowerUps(dt);

        // reduzir o tempo de vibração
        if (ShakeTime > 0.0f)
        {
            ShakeTime -= dt;

            if (ShakeTime <= 0.0f)
            {
                Effects.Shake = false;
            }
        }

        // verifica a condição de derrota
        if (Ball.Position.Y >= Height) // a bola chegou à borda inferior?
        {
            ResetLevel();
            ResetPlayer();
        }
    }

    public void Render()
    {
        if (State == GameState.GAME_ACTIVE)
        {
            // iniciar a renderização para o framebuffer de pós-processamento
            Effects.BeingRender();

                // desenhar fundo
                Renderer.DrawSprite(ResourceManager.GetTexture("background"), new Vector2(0.0f, 0.0f), new Vector2(Width, Height), 0.0f);

                // desenhar nível
                Levels[Level].Draw(Renderer);

                // desenhar jogador
                Player.Draw(Renderer);

                // desenhar PowerUps
                foreach (PowerUp powerUp in PowerUps)
                {
                    if (!powerUp.Destroyed)
                    {
                        powerUp.Draw(Renderer);
                    }
                }

                // desenhar partículas
                Particles.Draw();

                // desenhar bola
                Ball.Draw(Renderer);

            // finalizar a renderização para o framebuffer de pós-processamento
            Effects.EndRender();

            // renderizar quad de pós-processamento
            Effects.Render((float)Glfw.GetApi().GetTime());
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

    private Collision CheckCollision(BallObject one, GameObject two) // AABB - Circle collision
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

        if (difference.Length() < one.Radius) // não <=, pois, nesse caso, também ocorre uma colisão quando o objeto um toca exatamente o objeto dois — situação em que eles se encontram ao final de cada etapa de resolução de colisões.
        {
            return (true, VectorDirection(difference), difference);
        }
        else
        {
            return (false, Direction.UP, new Vector2(0.0f, 0.0f));
        }
    }

    // calcula a direção para a qual um vetor aponta (N, L, S ou O)
    private Direction VectorDirection(Vector2 target)
    {
        Vector2[] compass =
        {
            new Vector2( 0.0f,  1.0f), // cima
            new Vector2( 1.0f,  0.0f), // direita
            new Vector2( 0.0f, -1.0f), // baixo
            new Vector2(-1.0f,  0.0f)  // esquerda
        };

        float max = 0.0f;
        int best_match = -1;

        for (int i = 0; i < 4; i++)
        {
            float dot_product = Vector2.Dot(Vector2.Normalize(target), compass[i]);

            if (dot_product > max)
            {
                max = dot_product;
                best_match = i;
            }
        }

        return (Direction)best_match;
    }

    public void DoCollisions()
    {
        foreach (GameObject box in Levels[Level].Bricks)
        {
            if (!box.Destroyed)
            {
                Collision collision = CheckCollision(Ball, box);

                if (collision.collision) // se a colisão for verdadeira
                {
                    // destrói o bloco se não for sólido
                    if (!box.IsSolid)
                    {
                        box.Destroyed = true;
                        SpawnPowerUps(box);
                    }
                    else
                    {
                        // se o bloco for sólido, habilite o efeito de tremor
                        ShakeTime = 0.05f;
                        Effects.Shake = true;
                    }

                    // resolução de colisões
                    Direction dir = collision.direction;
                    Vector2 diff_vector = collision.difference;

                    if (dir == Direction.LEFT || dir == Direction.RIGHT) // colisão horizontal
                    {
                        Ball.Velocity.X = -Ball.Velocity.X; // inverte a velocidade horizontal

                        // realocar
                        float penetration = Ball.Radius - MathF.Abs(diff_vector.X);

                        if (dir == Direction.LEFT)
                        {
                            Ball.Position.X += penetration; // mover a bola para a direita
                        }
                        else
                        {
                            Ball.Position.X -= penetration; // move a bola para a esquerda;
                        }
                    }
                    else // colisão vertical
                    {
                        Ball.Velocity.Y = -Ball.Velocity.Y; // inverter velocidade vertical

                        // realocar
                        float penetration = Ball.Radius - MathF.Abs(diff_vector.Y);

                        if (dir == Direction.UP)
                        {
                            Ball.Position.Y -= penetration; // mover a bola de volta para cima
                        }
                        else
                        {
                            Ball.Position.Y += penetration; // mover a bola de volta para baixo
                        }
                    }
                }
            }

            // verifique também colisões com PowerUps e, se houver, ative-os
            foreach (PowerUp powerUp in PowerUps)
            {
                if (!powerUp.Destroyed)
                {
                    // primeiro, verifique se o power-up ultrapassou a borda inferior; se sim: mantenha-o inativo e destrua-o
                    if (powerUp.Position.Y >= Height)
                    {
                        powerUp.Destroyed = true;
                    }
                    if (CheckCollision(Player, powerUp))
                    {
                        // colidiu com o jogador, agora ativa o power-up
                        ActivatePowerUp(powerUp);
                        powerUp.Destroyed = true;
                        powerUp.Activated = true;
                    }
                }
            }

            // verificar colisões para a raquete do jogador (a menos que esteja travada)
            Collision result = CheckCollision(Ball, Player);

            if (!Ball.Stuck && result.collision)
            {
                // verifica onde atingiu a plataforma e altera a velocidade com base no ponto de impacto
                float centerBoard = Player.Position.X + Player.Size.X / 2.0f;
                float distance = (Ball.Position.X + Ball.Radius) - centerBoard;
                float percentage = distance / (Player.Size.X / 2.0f);

                // então, mova-se de acordo
                float strength = 2.0f;
                Vector2 oldVelocity = Ball.Velocity;

                Ball.Velocity.X = INITIAL_BALL_VELOCITY.X * percentage * strength;

                //Ball->Velocity.y = -Ball->Velocity.y;
                Ball.Velocity = Vector2.Normalize(Ball.Velocity) * oldVelocity.Length(); // mantém a velocidade constante em ambos os eixos (multiplica pelo comprimento da velocidade antiga, para que a intensidade total não seja alterada)

                // corrigir raquete travando
                Ball.Velocity.Y = -1.0f * MathF.Abs(Ball.Velocity.Y);
            }
        }
    }

    // reset
    public void ResetLevel()
    {
        if (Level == 0)
        {
            Levels[0].Load("src/levels/one.lvl", Width, Height / 2);
        }
        else if (Level == 1)
        {
            Levels[1].Load("src/levels/two.lvl", Width, Height / 2);
        }
        else if (Level == 2)
        {
            Levels[2].Load("src/levels/three.lvl", Width, Height / 2);
        }
        else if (Level == 3)
        {
            Levels[3].Load("src/levels/four.lvl", Width, Height / 2);
        }
    }

    public void ResetPlayer()
    {
        // redefinir estatísticas do jogador/bola
        Player.Size = PLAYER_SIZE;
        Player.Position = new Vector2(Width / 2.0f - PLAYER_SIZE.X / 2.0f, Height - PLAYER_SIZE.Y);

        Ball.Reset(Player.Position + new Vector2(PLAYER_SIZE.X / 2.0f - BALL_RADIUS, -(BALL_RADIUS * 2.0f)), INITIAL_BALL_VELOCITY);

        // desative também todos os power-ups ativos
        Effects.Chaos = Effects.Confuse = false;
        Ball.PassThrough = Ball.Sticky = false;
        Player.Color = new Vector3(1.0f);
        Ball.Color = new Vector3(1.0f);
    }

    private bool ShouldSpawn(uint chance)
    {
        uint random = (uint)(new Random().Next() % chance);

        return random == 0;
    }

    // powerups
    public void SpawnPowerUps(GameObject block)
    {
        if (ShouldSpawn(75)) // Chance de 1 em 75
        {
            PowerUps.Add(new PowerUp("speed", new Vector3(0.5f, 0.5f, 1.0f), 0.0f, block.Position, ResourceManager.GetTexture("powerup_speed")));
        }
        if (ShouldSpawn(75))
        {
            PowerUps.Add(new PowerUp("sticky", new Vector3(1.0f, 0.5f, 1.0f), 20.0f, block.Position, ResourceManager.GetTexture("powerup_sticky")));
        }
        if (ShouldSpawn(75))
        {
            PowerUps.Add(new PowerUp("pass-through", new Vector3(0.5f, 1.0f, 0.5f), 10.0f, block.Position, ResourceManager.GetTexture("powerup_passthrough")));
        }
        if (ShouldSpawn(75))
        {
            PowerUps.Add(new PowerUp("pad-size-increase", new Vector3(1.0f, 0.6f, 0.4f), 0.0f, block.Position, ResourceManager.GetTexture("powerup_increase")));
        }
        if (ShouldSpawn(15)) // Power-ups negativos devem surgir com mais frequência
        {
            PowerUps.Add(new PowerUp("confuse", new Vector3(1.0f, 0.3f, 0.3f), 15.0f, block.Position, ResourceManager.GetTexture("powerup_confuse")));
        }
        if (ShouldSpawn(15))
        {
            PowerUps.Add(new PowerUp("chaos", new Vector3(0.9f, 0.25f, 0.25f), 15.0f, block.Position, ResourceManager.GetTexture("powerup_chaos")));
        }
    }

    public void UpdatePowerUps(float dt)
    {
        foreach (PowerUp powerUp in PowerUps)
        {
            powerUp.Position += powerUp.Velocity * dt;
            
            if (powerUp.Activated)
            {
                powerUp.Duration -= dt;

                if (powerUp.Duration <= 0.0f)
                {
                    // remove o power-up da lista (será removido posteriormente)
                    powerUp.Activated = false;

                    // desativar efeitos
                    if (powerUp.Type == "sticky")
                    {
                        if (!IsOtherPowerUpActive(PowerUps, "sticky"))
                        {
                            // redefinir apenas se nenhum outro PowerUp do tipo "sticky" estiver ativo
                            Ball.Sticky = false;
                            Player.Color = new Vector3(1.0f);
                        }
                    }
                    else if (powerUp.Type == "pass-through")
                    {
                        if (!IsOtherPowerUpActive(PowerUps, "pass-through"))
                        {
                            // redefinir apenas se nenhum outro PowerUp do tipo "pass-through" estiver ativo
                            Ball.PassThrough = false;
                            Ball.Color = new Vector3(1.0f);
                        }
                    }
                    else if (powerUp.Type == "confuse")
                    {
                        if (!IsOtherPowerUpActive(PowerUps, "confuse"))
                        {
                            // redefinir apenas se nenhum outro PowerUp do tipo "confusão" estiver ativo
                            Effects.Confuse = false;
                        }
                    }
                    else if (powerUp.Type == "chaos")
                    {
                        if (!IsOtherPowerUpActive(PowerUps, "chaos"))
                        {
                            // reinicia apenas se nenhum outro PowerUp do tipo caos estiver ativo
                            Effects.Chaos = false;
                        }
                    }
                }
            }
        }

        // Remove do vetor todos os PowerUps que estejam destruídos E NÃO ativados (ou seja, fora do mapa ou já encerrados)
        // Nota: utilizamos uma expressão lambda para remover cada PowerUp que esteja destruído e não ativado
        PowerUps.RemoveAll(powerUp => powerUp.Destroyed && !powerUp.Activated);
    }

    private bool IsOtherPowerUpActive(List<PowerUp> powerUps, string type)
    {
        // Verifica se outro PowerUp do mesmo tipo ainda está ativo
        // caso em que não desativamos seu efeito (ainda)
        foreach (PowerUp powerUp in powerUps)
        {
            if (powerUp.Activated)
            {
                if (powerUp.Type == type)
                {
                    return true;
                }
            }
        }

        return false;
    }

    private void ActivatePowerUp(PowerUp powerUp)
    {
        if (powerUp.Type == "speed")
        {
            Ball.Velocity *= 1.2f;
        }
        else if (powerUp.Type == "sticky")
        {
            Ball.Sticky = true;
            Player.Color = new Vector3(1.0f, 0.5f, 1.0f);
        }
        else if (powerUp.Type == "pass-through")
        {
            Ball.PassThrough = true;
            Ball.Color = new Vector3(1.0f, 0.5f, 0.5f);
        }
        else if (powerUp.Type == "pad-size-increase")
        {
            Player.Size.X += 50;
        }
        else if (powerUp.Type == "confuse")
        {
            if (!Effects.Chaos)
            {
                Effects.Confuse = true; // ativar apenas se o caos ainda não estivesse ativo
            }
        }
        else if (powerUp.Type == "chaos")
        {
            if (!Effects.Confuse)
            {
                Effects.Chaos = true;
            }
        }
    }
}
