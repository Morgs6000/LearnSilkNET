/**************************************************
** Este código faz parte do Breakout.
**
** O Breakout é um software livre: você pode redistribuí-lo e/ou modificá-lo
** sob os termos da licença CC BY 4.0, conforme publicada pela
** Creative Commons, seja a versão 4 da Licença ou (a seu
** critério) qualquer versão posterior.
**************************************************/

using System.Numerics;
using System.Runtime.CompilerServices;
using Silk.NET.OpenGL;

namespace LearnSilkNET.src;

public class SpriteRenderer : IDisposable
{
    private GL _gl;

    // Construtor (inicializa shaders/formas)
    public SpriteRenderer(GL gl, Shader shader)
    {
        _gl = gl;
        _shader = shader;

        InitRenderData();
    }

    // Destrutor
    public void Dispose()
    {
        _gl.DeleteVertexArrays(1, ref _quadVAO);
    }

    // Renderiza um quadrilátero definido texturizado com o sprite fornecido
    public void DrawSprite(Texture2D texture, Vector2 position, Vector2? size = null, float? rotate = null, Vector3? color = null)
    {
        Vector2 _size = size ?? new Vector2(10.0f, 10.0f);
        float _rotate = rotate ?? 0.0f;
        Vector3 _color = color ?? new Vector3(1.0f);

        // preparar transformações
        _shader.Use();

        Matrix4x4 model = Matrix4x4.Identity;
        model *= Matrix4x4.CreateScale(new Vector3(_size, 1.0f)); // última escala

        model *= Matrix4x4.CreateTranslation(new Vector3(-0.5f * _size.X, -0.5f * _size.Y, 0.0f)); // mover a origem de volta
        model *= Matrix4x4.CreateFromAxisAngle(Vector3.Normalize(new Vector3(0.0f, 0.0f, 1.0f)), MathHelper.DegreesToRadians(_rotate)); // depois rotacione
        model *= Matrix4x4.CreateTranslation(new Vector3(0.5f * _size.X, 0.5f * _size.Y, 0.0f)); //move a origem da rotação para o centro do quadrante

        model *= Matrix4x4.CreateTranslation(new Vector3(position, 0.0f)); // primeira translação (as transformações ocorrem nesta ordem: escala primeiro, depois rotação e, por fim, a translação final; ordem inversa)

        _shader.SetMatrix4("model", model);

        // renderizar quadrilátero texturizado
        _shader.SetVector3f("spriteColor", _color);

        _gl.ActiveTexture(TextureUnit.Texture0);
        texture.Bind();

        _gl.BindVertexArray(_quadVAO);
        _gl.DrawArrays(PrimitiveType.Triangles, 0, 6);
        _gl.BindVertexArray(0);
    }

    // Estado de renderização
    private Shader _shader;
    private uint _quadVAO;

    // Inicializa e configura o buffer e os atributos de vértice do quad
    private void InitRenderData()
    {
        // configurar VAO/VBO
        uint VBO;

        float[] vertices =
        {
            // pos        // tex
            0.0f, 0.0f,   0.0f, 0.0f,
            1.0f, 0.0f,   1.0f, 0.0f,
            1.0f, 1.0f,   1.0f, 1.0f,
            0.0f, 0.0f,   0.0f, 0.0f,
            1.0f, 1.0f,   1.0f, 1.0f,
            0.0f, 1.0f,   0.0f, 1.0f
        };

        _gl.GenVertexArrays(1, out _quadVAO);
        _gl.GenBuffers(1, out VBO);

        _gl.BindBuffer(BufferTargetARB.ArrayBuffer, VBO);
        unsafe
        {
            fixed (float* buf = vertices)
            {
                _gl.BufferData(BufferTargetARB.ArrayBuffer, (uint)(vertices.Length * sizeof(float)), buf, BufferUsageARB.StaticDraw);
            }
        }

        _gl.BindVertexArray(_quadVAO);

        _gl.EnableVertexAttribArray(0);
        unsafe
        {
            _gl.VertexAttribPointer(0, 4, VertexAttribPointerType.Float, false, 4 * sizeof(float), (void*)0);
        }

        _gl.BindBuffer(BufferTargetARB.ArrayBuffer, 0);
        _gl.BindVertexArray(0);
    }
}
