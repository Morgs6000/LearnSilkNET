/**************************************************
** Este código faz parte do Breakout.
**
** O Breakout é um software livre: você pode redistribuí-lo e/ou modificá-lo
** sob os termos da licença CC BY 4.0, conforme publicada pela
** Creative Commons, seja a versão 4 da Licença ou (a seu
** critério) qualquer versão posterior.
**************************************************/

using Silk.NET.OpenGL;

namespace LearnSilkNET.src;

// A classe PostProcessor gerencia todos os efeitos de pós-processamento para o jogo Breakout.
// Ela renderiza o jogo em um quadrilátero texturizado, permitindo ativar efeitos
// específicos por meio das variáveis ​​booleanas Confuse, Chaos ou Shake.
// Para que a classe funcione, é necessário chamar BeginRender() antes de renderizar
// o jogo e EndRender() após a renderização.
public class PostProcessor
{
    private GL _gl;

    // estado
    public Shader PostProcessingShader;
    public Texture2D Texture;
    public uint Width, Height;

    // opções
    public bool Confuse, Chaos, Shake;

    // construtor
    public PostProcessor(GL gl, Shader shader, uint width, uint height)
    {
        _gl = gl;

        PostProcessingShader = shader;

        Texture = new Texture2D(_gl);

        Width = width;
        Height = height;

        Confuse = false;
        Chaos = false;
        Shake = false;

        // inicializar objeto renderbuffer/framebuffer
        _gl.GenFramebuffers(1, out _multisampledFramebufferObject);
        _gl.GenFramebuffers(1, out _framebufferObject);
        _gl.GenRenderbuffers(1, out _renderbufferObject);

        // inicializa o armazenamento do renderbuffer com um buffer de cor multisampled (não é necessário um buffer de profundidade/stencil)
        _gl.BindFramebuffer(FramebufferTarget.Framebuffer, _multisampledFramebufferObject);
        _gl.BindRenderbuffer(RenderbufferTarget.Renderbuffer, _renderbufferObject);
        _gl.RenderbufferStorageMultisample(RenderbufferTarget.Renderbuffer, 4, InternalFormat.Rgb, width, height); // alocar armazenamento para o objeto de buffer de renderização
        _gl.FramebufferRenderbuffer(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment0, RenderbufferTarget.Renderbuffer, _renderbufferObject); // anexa o objeto de buffer de renderização MS ao framebuffer

        if (_gl.CheckFramebufferStatus(FramebufferTarget.Framebuffer) != GLEnum.FramebufferComplete)
        {
            Console.WriteLine("ERROR::POSTPROCESSOR: Failed to initialize MSFBO");
        }

        // inicializa também o FBO/textura para o qual o buffer de cor com multisampling será copiado (blit); usado para operações de shader (para efeitos de pós-processamento)
        _gl.BindFramebuffer(FramebufferTarget.Framebuffer, _framebufferObject);
        Texture.Generate(width, height, null);
        _gl.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment0, TextureTarget.Texture2D, Texture.ID, 0); // anexa a textura ao framebuffer como seu anexo de cor

        if (_gl.CheckFramebufferStatus(FramebufferTarget.Framebuffer) != GLEnum.FramebufferComplete)
        {
            Console.WriteLine("ERROR::POSTPROCESSOR: Failed to initialize FBO");
        }

        _gl.BindFramebuffer(FramebufferTarget.Framebuffer, 0);

        // inicializar dados de renderização e uniforms
        InitRenderData();
        PostProcessingShader.SetInteger("scene", 0, true);
        float offset = 1.0f / 300.0f;
        float[,] offsets = new float[9, 2]
        {
            { -offset,  offset  },  // superior esquerdo
            {  0.0f,    offset  },  // superior centro
            {  offset,  offset  },  // superior direito
            { -offset,  0.0f    },  // centro esquerdo
            {  0.0f,    0.0f    },  // centro centro
            {  offset,  0.0f    },  // centro direito
            { -offset, -offset  },  // inferior esquerdo
            {  0.0f,   -offset  },  // inferior centro
            {  offset, -offset  }   // inferior direito
        };

        unsafe
        {
            fixed (float* ptr = offsets)
            {
                _gl.Uniform2(_gl.GetUniformLocation(PostProcessingShader.ID, "offsets"), 9, (float*)ptr);
            }
        }

        int[] edge_kernel = new int[9]
        {
            -1, -1, -1,
            -1,  8, -1,
            -1, -1, -1
        };

        _gl.Uniform1(_gl.GetUniformLocation(PostProcessingShader.ID, "edge_kernel"), 9, edge_kernel);

        float[] blur_kernel = new float[9]
        {
            1.0f / 16.0f, 2.0f / 16.0f, 1.0f / 16.0f,
            2.0f / 16.0f, 4.0f / 16.0f, 2.0f / 16.0f,
            1.0f / 16.0f, 2.0f / 16.0f, 1.0f / 16.0f
        };

        _gl.Uniform1(_gl.GetUniformLocation(PostProcessingShader.ID, "blur_kernel"), 9, blur_kernel);
    }

    // prepara as operações de framebuffer do pós-processador antes de renderizar o jogo
    public void BeingRender()
    {
        _gl.BindFramebuffer(FramebufferTarget.Framebuffer, _multisampledFramebufferObject);
        _gl.ClearColor(0.0f, 0.0f, 0.0f, 1.0f);
        _gl.Clear(ClearBufferMask.ColorBufferBit);
    }

    // deve ser chamado após a renderização do jogo, para armazenar todos os dados renderizados em um objeto de textura
    public void EndRender()
    {
        // agora resolve o buffer de cor com multisampling para um FBO intermediário, para armazená-lo em uma textura
        _gl.BindFramebuffer(FramebufferTarget.ReadFramebuffer, _multisampledFramebufferObject);
        _gl.BindFramebuffer(FramebufferTarget.DrawFramebuffer, _framebufferObject);
        _gl.BlitFramebuffer(0, 0, (int)Width, (int)Height, 0, 0, (int)Width, (int)Height, ClearBufferMask.ColorBufferBit, BlitFramebufferFilter.Nearest);
        _gl.BindFramebuffer(FramebufferTarget.Framebuffer, 0); // associa os framebuffers de LEITURA e ESCRITA ao framebuffer padrão
    }

    // renderiza o quad de textura do PostProcessor (como um sprite grande que cobre a tela inteira)
    public void Render(float time)
    {
        // definir uniforms/opções
        PostProcessingShader.Use();
        PostProcessingShader.SetFloat("time", time);
        PostProcessingShader.SetInteger("confuse", Confuse ? 1 : 0);
        PostProcessingShader.SetInteger("chaos", Chaos ? 1 : 0);
        PostProcessingShader.SetInteger("shake", Shake ? 1 : 0);

        // renderiza quad texturizado
        _gl.ActiveTexture(TextureUnit.Texture0);
        Texture.Bind();

        _gl.BindVertexArray(_vertexArrayObject);
        _gl.DrawArrays(PrimitiveType.Triangles, 0, 6);
        _gl.BindVertexArray(0);
    }

    // estado de renderização
    private uint _multisampledFramebufferObject, _framebufferObject; // MSFBO = FBO com multisampling. O FBO é padrão, usado para copiar (blit) o ​​buffer de cor MS para uma textura.
    private uint _renderbufferObject; // O RBO é usado para buffer de cor com multisampling
    private uint _vertexArrayObject;

    // inicializa o quad para renderizar a textura de pós-processamento
    private void InitRenderData()
    {
        // configurar VAO/VBO
        uint vertexBufferObject;

        float[] vertices =
        {
            // pos          // tex
            -1.0f, -1.0f,   0.0f, 0.0f,
             1.0f, -1.0f,   1.0f, 0.0f,
             1.0f,  1.0f,   1.0f, 1.0f,
            -1.0f, -1.0f,   0.0f, 0.0f,
             1.0f,  1.0f,   1.0f, 1.0f,
            -1.0f,  1.0f,   0.0f, 1.0f
        };

        _gl.GenVertexArrays(1, out _vertexArrayObject);
        _gl.GenBuffers(1, out vertexBufferObject);

        _gl.BindBuffer(BufferTargetARB.ArrayBuffer, vertexBufferObject);
        unsafe
        {
            fixed (float* buf = vertices)
            {
                _gl.BufferData(BufferTargetARB.ArrayBuffer, (uint)(vertices.Length * sizeof(float)), buf, BufferUsageARB.StaticDraw);
            }
        }

        _gl.BindVertexArray(_vertexArrayObject);

        _gl.EnableVertexAttribArray(0);
        unsafe
        {
            _gl.VertexAttribPointer(0, 4, VertexAttribPointerType.Float, false, 4 * sizeof(float), (void*)0);
        }

        _gl.BindBuffer(BufferTargetARB.ArrayBuffer, 0);
        _gl.BindVertexArray(0);
    }
}
