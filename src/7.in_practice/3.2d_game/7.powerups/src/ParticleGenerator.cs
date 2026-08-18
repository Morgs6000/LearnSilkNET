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

// Representa uma única partícula e seu estado
public struct Particle
{
    public Vector2 Position, Velocity;
    public Vector4 Color;
    public float Life;

    public Particle()
    {
        Position = new Vector2(0.0f);
        Velocity = new Vector2(0.0f);

        Color = new Vector4(1.0f);

        Life = 0.0f;
    }
}

// O ParticleGenerator atua como um contêiner para a renderização de um grande número de
// partículas, gerando e atualizando partículas repetidamente e eliminando-as
// após um determinado período de tempo.
public class ParticleGenerator
{
    private GL _gl;

    // construtor
    public ParticleGenerator(GL gl, Shader shader, Texture2D texture, uint amount)
    {
        _gl = gl;

        _shader = shader;
        _texture = texture;
        _amount = amount;

        Init();
    }

    // atualizar todas as partículas
    public void Update(float dt, GameObject obj, uint newParticles, Vector2? offset = null)
    {
        Vector2 _offset = offset ?? new Vector2(0.0f, 0.0f);

        // adicionar novas partículas
        for (int i = 0; i < newParticles; i++)
        {
            int unusedParticle = FirstUnusedParticle();
            RespawnParticle(unusedParticle, obj, _offset);
        }

        // atualizar todas as partículas
        for (int i = 0; i < _amount; i++)
        {
            Particle p = _particles[i];
            p.Life -= dt; // reduzir vida

            if (p.Life > 0.0f)
            {
                // a partícula está ativa, portanto, atualize-a
                p.Position -= p.Velocity * dt;
                p.Color.W -= dt * 2.5f;
            }

            _particles[i] = p;
        }
    }

    // renderizar todas as partículas
    public void Draw()
    {
        // use a mesclagem aditiva para criar um efeito de brilho
        _gl.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.One);

        _shader.Use();

        foreach (Particle particle in _particles)
        {
            if (particle.Life > 0.0f)
            {
                _shader.SetVector2f("offset", particle.Position);
                _shader.SetVector4f("color", particle.Color);

                _texture.Bind();

                _gl.BindVertexArray(_vertexArrayObject);
                _gl.DrawArrays(PrimitiveType.Triangles, 0, 6);
                _gl.BindVertexArray(0);
            }
        }

        // não se esqueça de redefinir para o modo de mesclagem padrão
        _gl.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);
    }

    // estado
    private List<Particle> _particles = [];
    private uint _amount;

    // estado de renderização
    private Shader _shader;
    private Texture2D _texture;
    private uint _vertexArrayObject;

    // inicializa o buffer e os atributos de vértice
    private void Init()
    {
        // configurar propriedades da malha e de atributos
        uint vertexBufferObject;

        float[] particle_quad =
        {
            // posições   // coordenadas de textura
            0.0f, 0.0f,   0.0f, 0.0f,
            1.0f, 0.0f,   1.0f, 0.0f,
            1.0f, 1.0f,   1.0f, 1.0f,
            0.0f, 0.0f,   0.0f, 0.0f,
            1.0f, 1.0f,   1.0f, 1.0f,
            0.0f, 1.0f,   0.0f, 1.0f
        };

        _gl.GenVertexArrays(1, out _vertexArrayObject);
        _gl.GenBuffers(1, out vertexBufferObject);

        _gl.BindVertexArray(_vertexArrayObject);

        // preencher buffer da malha
        _gl.BindBuffer(BufferTargetARB.ArrayBuffer, vertexBufferObject);
        unsafe
        {
            fixed (float* buf = particle_quad)
            {
                _gl.BufferData(BufferTargetARB.ArrayBuffer, (uint)(particle_quad.Length * sizeof(float)), buf, BufferUsageARB.StaticDraw);
            }
        }

        // definir atributos da malha
        _gl.EnableVertexAttribArray(0);
        unsafe
        {
            _gl.VertexAttribPointer(0, 4, VertexAttribPointerType.Float, false, 4 * sizeof(float), (void*)0);
        }

        _gl.BindVertexArray(0);

        // cria this->amount instâncias de partícula padrão
        for (int i = 0; i < _amount; i++)
        {
            _particles.Add(new Particle());
        }
    }

    // armazena o índice da última partícula utilizada (para acesso rápido à próxima partícula inativa)
    private int _lastUsedParticle = 0;

    // retorna o índice da primeira Partícula que não está sendo usada atualmente (por exemplo, Life <= 0.0f) ou 0 se nenhuma partícula estiver inativa no momento
    private int FirstUnusedParticle()
    {
        // primeira busca a partir da última partícula utilizada; isso geralmente retorna quase instantaneamente
        for (int i = _lastUsedParticle; i < _amount; i++)
        {
            if (_particles[i].Life <= 0.0f)
            {
                _lastUsedParticle = i;

                return i;
            }
        }

        // caso contrário, realize uma busca linear
        for (int i = 0; i < _lastUsedParticle; i++)
        {
            if (_particles[i].Life <= 0.0f)
            {
                _lastUsedParticle = i;

                return i;
            }
        }

        // todas as partículas estão ocupadas; substitua a primeira (observe que, se esse caso ocorrer repetidamente, mais partículas deverão ser reservadas)
        _lastUsedParticle = 0;

        return 0;
    }

    // reaparece a partícula
    private void RespawnParticle(int particle, GameObject obj, Vector2? offset = null)
    {
        Vector2 _offset = offset ?? new Vector2(0.0f, 0.0f);

        float random = ((new Random().Next() % 100) - 50) / 10.0f;
        float rColor = 0.5f + ((new Random().Next() % 100) / 100.0f);

        Particle p = _particles[particle];

        p.Position = new Vector2(obj.Position.X + random + _offset.X, obj.Position.Y + random + _offset.Y);
        p.Color = new Vector4(rColor, rColor, rColor, 1.0f);
        p.Life = 1.0f;
        p.Velocity = obj.Velocity * 0.1f;

        _particles[particle] = p;
    }
}
