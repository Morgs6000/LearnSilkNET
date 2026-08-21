using System.Numerics;
using Silk.NET.GLFW;
using Silk.NET.Input;
using Silk.NET.Maths;
using Silk.NET.OpenGL;
using Silk.NET.Windowing;
using StbImageSharp;

namespace LearnSilkNET.src;

public class Program
{
    private static IWindow _window = null!;
    private static GL _gl = null!;
    private static Glfw _glfw = null!;

    private static IKeyboard _primaryKeyboard = null!;
    private static IMouse _primaryMouse = null!;

    // configurações
    private const uint SCR_WIDTH = 800;
    private const uint SCR_HEIGHT = 600;
    private static bool _bloom = true;
    private static float _exposure = 1.0f;

    // câmera
    private static Camera _camera = new Camera(new Vector3(0.0f, 0.0f, 5.0f));
    private static float _lastX = (float)SCR_WIDTH / 2.0f;
    private static float _lastY = (float)SCR_HEIGHT / 2.0f;
    private static bool _firstMouse = true;

    // tempo
    private static float _deltaTime = 0.0f;
    private static float _lastFrame = 0.0f;

    private static Shader _shader = null!;
    private static Shader _shaderLight = null!;
    private static Shader _shaderBlur = null!;
    private static Shader _shaderBloomFinal = null!;

    private static uint _woodTexture;
    private static uint _containerTexture;

    private static uint _hdrFBO;
    private static uint[] _colorBuffers = new uint[2];

    private static uint _rboDepth;
    private static GLEnum[] _attachments = new GLEnum[2];

    private static uint[] _pingpongFBO = new uint[2];
    private static uint[] _pingpongColorbuffers = new uint[2];

    private static List<Vector3> _lightPositions = [];
    private static List<Vector3> _lightColors = [];

    private static void Main(string[] args)
    {
        // criação da janela glfw
        // --------------------------------------------------
        WindowOptions options = WindowOptions.Default;

        options.Size = new Vector2D<int>((int)SCR_WIDTH, (int)SCR_HEIGHT);
        options.Title = "Learn Silk.NET";
        options.IsVisible = false;
        options.VSync = false;

        _window = Window.Create(options);
        
        _window.Load += OnLoad;
        _window.Resize += OnResize;
        _window.Update += OnUpdate;
        _window.Render += OnRender;
        _window.Closing += OnClosing;

        try
        {
            _window.Run();
        }
        catch (Exception e)
        {
            Console.WriteLine(
                "Falha ao criar a janela Sik.NET" + "\n" +
                e
            );
        }
    }

    private static void OnLoad()
    {
        if (OperatingSystem.IsWindows())
        {
            _window.Center();
        }
        _window.IsVisible = true;

        IInputContext input = _window.CreateInput();

        _primaryKeyboard = input.Keyboards.FirstOrDefault()!;
        _primaryMouse = input.Mice.FirstOrDefault()!;

        if (_primaryKeyboard != null)
        {
            _primaryKeyboard.KeyDown += OnKeyDown;
        }
        if (_primaryMouse != null)
        {
            _primaryMouse.MouseMove += MouseCallback;
            _primaryMouse.Scroll += ScrollCallback;
        }

        _gl = _window.CreateOpenGL();
        _glfw = Glfw.GetApi();

        // instruir o GLFW a capturar o mouse
        _primaryMouse!.Cursor.CursorMode = CursorMode.Raw;

        // configurar estado global do OpenGL
        // --------------------------------------------------
        _gl.Enable(EnableCap.DepthTest);

        // construir e compilar nosso programa de shader
        // --------------------------------------------------
        _shader = new Shader(_gl, "src/bloom.vs", "src/bloom.fs");
        _shaderLight = new Shader(_gl, "src/bloom.vs", "src/light_box.fs");
        _shaderBlur = new Shader(_gl, "src/blur.vs", "src/blur.fs");
        _shaderBloomFinal = new Shader(_gl, "src/bloom_final.vs", "src/bloom_final.fs");

        // carregar texturas
        // --------------------------------------------------
        _woodTexture = LoadTexture("res/textures/wood.png", true); // observe que estamos carregando a textura como uma textura sRGB
        _containerTexture = LoadTexture("res/textures/container2.png", true); // observe que estamos carregando a textura como uma textura sRGB

        // configurar buffers de quadro (ponto flutuante)
        // --------------------------------------------------
        _gl.GenFramebuffers(1, out _hdrFBO);
        _gl.BindFramebuffer(FramebufferTarget.Framebuffer, _hdrFBO);

        // cria 2 buffers de cor de ponto flutuante (um para renderização normal, outro para valores de limiar de brilho)
        _gl.GenTextures(2, _colorBuffers);

        for (int i = 0; i < 2; i++)
        {
            _gl.BindTexture(TextureTarget.Texture2D, _colorBuffers[i]);

            unsafe
            {
                _gl.TexImage2D(TextureTarget.Texture2D, 0, InternalFormat.Rgba16f, SCR_WIDTH, SCR_HEIGHT, 0, PixelFormat.Rgba, PixelType.Float, null);
            }

            _gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Linear);
            _gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);
            _gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.ClampToEdge); // Limitamos à borda, pois, caso contrário, o filtro de desfoque amostraria valores de textura repetidos!
            _gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.ClampToEdge);

            // anexar textura ao framebuffer
            _gl.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment0 + i, TextureTarget.Texture2D, _colorBuffers[i], 0);
        }

        // criar e anexar buffer de profundidade (renderbuffer)
        _gl.GenRenderbuffers(1, out _rboDepth);
        _gl.BindRenderbuffer(RenderbufferTarget.Renderbuffer, _rboDepth);
        _gl.RenderbufferStorage(RenderbufferTarget.Renderbuffer, InternalFormat.DepthComponent, SCR_WIDTH, SCR_HEIGHT);
        _gl.FramebufferRenderbuffer(FramebufferTarget.Framebuffer, FramebufferAttachment.DepthAttachment, RenderbufferTarget.Renderbuffer, _rboDepth);

        // informar ao OpenGL quais anexos de cor (deste framebuffer) usaremos para a renderização
        _attachments = new GLEnum[2]{ GLEnum.ColorAttachment0, GLEnum.ColorAttachment1 };

        _gl.DrawBuffers(2, _attachments);

        // finalmente, verifica se o framebuffer está completo
        if (_gl.CheckFramebufferStatus(FramebufferTarget.Framebuffer) != GLEnum.FramebufferComplete)
        {
            Console.WriteLine("Framebuffer not complete!");
        }

        _gl.BindFramebuffer(FramebufferTarget.Framebuffer, 0);

        // framebuffer ping-pong para desfoque
        _gl.GenFramebuffers(2, _pingpongFBO);
        _gl.GenTextures(2, _pingpongColorbuffers);

        for (int i = 0; i < 2; i++)
        {
            _gl.BindFramebuffer(FramebufferTarget.Framebuffer, _pingpongFBO[i]);
            _gl.BindTexture(TextureTarget.Texture2D, _pingpongColorbuffers[i]);

            unsafe
            {
                _gl.TexImage2D(TextureTarget.Texture2D, 0, InternalFormat.Rgba16f, SCR_WIDTH, SCR_HEIGHT, 0, PixelFormat.Rgba, PixelType.Float, null);
            }

            _gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Linear);
            _gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);
            _gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.ClampToEdge); // Limitamos à borda, pois, caso contrário, o filtro de desfoque amostraria valores de textura repetidos!
            _gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.ClampToEdge);

            _gl.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment0, TextureTarget.Texture2D, _pingpongColorbuffers[i], 0);

            // verifique também se os framebuffers estão completos (não é necessário buffer de profundidade)
            if (_gl.CheckFramebufferStatus(FramebufferTarget.Framebuffer) != GLEnum.FramebufferComplete)
            {
                Console.WriteLine("Framebuffer not complete!");
            }
        }

        // informações de iluminação
        // --------------------------------------------------

        // posições
        _lightPositions.Add(new Vector3( 0.0f, 0.5f,  1.5f));
        _lightPositions.Add(new Vector3(-4.0f, 0.5f, -3.0f));
        _lightPositions.Add(new Vector3( 3.0f, 0.5f,  1.0f));
        _lightPositions.Add(new Vector3(-0.8f, 2.4f, -1.0f));

        // cores
        _lightColors.Add(new Vector3(5.0f,   5.0f,  5.0f));
        _lightColors.Add(new Vector3(10.0f,  0.0f,  0.0f));
        _lightColors.Add(new Vector3(0.0f,   0.0f,  15.0f));
        _lightColors.Add(new Vector3(0.0f,   5.0f,  0.0f));

        // configuração do shader
        // --------------------------------------------------
        _shader.Use();
        _shader.SetInt("diffuseTexture", 0);

        _shaderBlur.Use();
        _shaderBlur.SetInt("image", 0);

        _shaderBloomFinal.Use();
        _shaderBloomFinal.SetInt("scene", 0);
        _shaderBloomFinal.SetInt("bloomBlur", 1);
    }

    private static void OnResize(Vector2D<int> newSize)
    {
        FramebufferSizeCallback(newSize.X, newSize.Y);
    }

    private static void OnUpdate(double deltaTime)
    {
        // lógica de tempo por quadro
        // --------------------------------------------------
        float currentFrame = (float)_glfw.GetTime();
        _deltaTime = currentFrame - _lastFrame;
        _lastFrame = currentFrame;

        // input
        // --------------------------------------------------
        ProcessInput();
    }

    // loop de renderização
    // --------------------------------------------------
    private static void OnRender(double deltaTime)
    {
        // render
        // --------------------------------------------------
        _gl.ClearColor(0.0f, 0.0f, 0.0f, 1.0f);
        _gl.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

        // 1. renderizar a cena em um framebuffer de ponto flutuante
        // --------------------------------------------------
        _gl.BindFramebuffer(FramebufferTarget.Framebuffer, _hdrFBO);
        _gl.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

        Matrix4x4 projection = Matrix4x4.CreatePerspectiveFieldOfView(
            fieldOfView:       MathHelper.DegreesToRadians(_camera.Zoom), 
            aspectRatio:       (float)SCR_WIDTH / (float)SCR_HEIGHT, 
            nearPlaneDistance: 0.1f, 
            farPlaneDistance:  100.0f
        );
        Matrix4x4 view = _camera.GetViewMatrix();

        _shader.Use();
        _shader.SetMat4("projection", projection);
        _shader.SetMat4("view", view);

        _gl.ActiveTexture(TextureUnit.Texture0);
        _gl.BindTexture(TextureTarget.Texture2D, _woodTexture);

        // definir uniforms de iluminação
        for (int i = 0; i < _lightPositions.Count(); i++)
        {
            _shader.SetVec3($"lights[{i}].Position", _lightPositions[i]);
            _shader.SetVec3($"lights[{i}].Color", _lightColors[i]);
        }
        _shader.SetVec3("viewPos", _camera.Position);

        // cria um cubo grande que serve como chão
        Matrix4x4 model = Matrix4x4.Identity;
        model *= Matrix4x4.CreateScale(new Vector3(12.5f, 0.5f, 12.5f));
        model *= Matrix4x4.CreateTranslation(new Vector3(0.0f, -1.0f, 0.0f));
        _shader.SetMat4("model", model);
        RenderCube();
        
        // em seguida, crie vários cubos para compor o cenário
        _gl.BindTexture(TextureTarget.Texture2D, _containerTexture);

        model = Matrix4x4.Identity;
        model *= Matrix4x4.CreateScale(new Vector3(0.5f));
        model *= Matrix4x4.CreateTranslation(new Vector3(0.0f, 1.5f, 0.0f));
        _shader.SetMat4("model", model);
        RenderCube();

        model = Matrix4x4.Identity;
        model *= Matrix4x4.CreateScale(new Vector3(0.5f));
        model *= Matrix4x4.CreateTranslation(new Vector3(2.0f, 0.0f, 1.0f));
        _shader.SetMat4("model", model);
        RenderCube();

        model = Matrix4x4.Identity;
        model *= Matrix4x4.CreateFromAxisAngle(Vector3.Normalize(new Vector3(1.0f, 0.0f, 1.0f)), MathHelper.DegreesToRadians(60.0f));
        model *= Matrix4x4.CreateTranslation(new Vector3(-1.0f, -1.0f, 2.0f));
        _shader.SetMat4("model", model);
        RenderCube();

        model = Matrix4x4.Identity;
        model *= Matrix4x4.CreateScale(new Vector3(1.25f));
        model *= Matrix4x4.CreateFromAxisAngle(Vector3.Normalize(new Vector3(1.0f, 0.0f, 1.0f)), MathHelper.DegreesToRadians(23.0f));
        model *= Matrix4x4.CreateTranslation(new Vector3(0.0f, 2.7f, 4.0f));
        _shader.SetMat4("model", model);
        RenderCube();

        model = Matrix4x4.Identity;
        model *= Matrix4x4.CreateFromAxisAngle(Vector3.Normalize(new Vector3(1.0f, 0.0f, 1.0f)), MathHelper.DegreesToRadians(124.0f));
        model *= Matrix4x4.CreateTranslation(new Vector3(-2.0f, 1.0f, -3.0f));
        _shader.SetMat4("model", model);
        RenderCube();

        model = Matrix4x4.Identity;
        model *= Matrix4x4.CreateScale(new Vector3(0.5f));
        model *= Matrix4x4.CreateTranslation(new Vector3(-3.0f, 0.0f, 0.0f));
        _shader.SetMat4("model", model);
        RenderCube();

        // finalmente mostra todas as fontes de luz como cubos brilhantes
        _shaderLight.Use();
        _shaderLight.SetMat4("projection", projection);
        _shaderLight.SetMat4("view", view);

        for (int i = 0; i < _lightPositions.Count(); i++)
        {
            model = Matrix4x4.Identity;
            model *= Matrix4x4.CreateScale(new Vector3(0.25f));
            model *= Matrix4x4.CreateTranslation(_lightPositions[i]);
            _shaderLight.SetMat4("model", model);

            _shaderLight.SetVec3("lightColor", _lightColors[i]);

            RenderCube();
        }

        _gl.BindFramebuffer(FramebufferTarget.Framebuffer, 0);

        // 2. aplicar desfoque gaussiano de duas passagens aos fragmentos brilhantes
        // --------------------------------------------------
        bool horizontal = true, first_iteration = true;
        uint amount = 10;

        _shaderBlur.Use();

        for (int i = 0; i < amount; i++)
        {
            _gl.BindFramebuffer(FramebufferTarget.Framebuffer, _pingpongFBO[horizontal ? 1 : 0]);

            _shaderBlur.SetInt("horizontal", horizontal ? 1 : 0);

            _gl.BindTexture(TextureTarget.Texture2D, first_iteration ? _colorBuffers[1] : _pingpongColorbuffers[horizontal ? 0 : 1]); // vincula a textura do outro framebuffer (ou da cena, se for a primeira iteração)

            RenderQuad();

            horizontal = !horizontal;

            if (first_iteration)
            {
                first_iteration = false;
            }
        }

        _gl.BindFramebuffer(FramebufferTarget.Framebuffer, 0);

        // 3. agora, renderize o buffer de cores de ponto flutuante em um quad 2D e aplique o mapeamento de tons (tonemapping) das cores HDR para o intervalo de cores (limitado) do framebuffer padrão
        // --------------------------------------------------
        _gl.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

        _shaderBloomFinal.Use();

        _gl.ActiveTexture(TextureUnit.Texture0);
        _gl.BindTexture(TextureTarget.Texture2D, _colorBuffers[0]);

        _gl.ActiveTexture(TextureUnit.Texture1);
        _gl.BindTexture(TextureTarget.Texture2D, _pingpongColorbuffers[horizontal ? 0 : 1]);

        _shaderBloomFinal.SetInt("bloom", _bloom ? 1 : 0);
        _shaderBloomFinal.SetFloat("exposure", _exposure);

        RenderQuad();

        Console.WriteLine((_bloom ? "on" : "off") + "| exposure: " + _exposure);
    }

    private static void OnClosing()
    {
        
    }

    // renderCube() renderiza um cubo 3D de 1x1 em NDC.
    // --------------------------------------------------
    private static uint _cubeVAO = 0;
    private static uint _cubeVBO = 0;

    private static void RenderCube()
    {
        // inicializar (se necessário)
        if (_cubeVAO == 0)
        {
            float[] vertices =
            {
                // posições            // normais             // coordenadas de textura

                // face esquerda
                -1.0f, -1.0f, -1.0f,   -1.0f,  0.0f,  0.0f,   0.0f, 0.0f,
                -1.0f, -1.0f,  1.0f,   -1.0f,  0.0f,  0.0f,   1.0f, 0.0f,
                -1.0f,  1.0f,  1.0f,   -1.0f,  0.0f,  0.0f,   1.0f, 1.0f,
                -1.0f, -1.0f, -1.0f,   -1.0f,  0.0f,  0.0f,   0.0f, 0.0f,
                -1.0f,  1.0f,  1.0f,   -1.0f,  0.0f,  0.0f,   1.0f, 1.0f,
                -1.0f,  1.0f, -1.0f,   -1.0f,  0.0f,  0.0f,   0.0f, 1.0f,
                
                // face direita
                 1.0f, -1.0f,  1.0f,    1.0f,  0.0f,  0.0f,   0.0f, 0.0f,
                 1.0f, -1.0f, -1.0f,    1.0f,  0.0f,  0.0f,   1.0f, 0.0f,
                 1.0f,  1.0f, -1.0f,    1.0f,  0.0f,  0.0f,   1.0f, 1.0f,
                 1.0f, -1.0f,  1.0f,    1.0f,  0.0f,  0.0f,   0.0f, 0.0f,
                 1.0f,  1.0f, -1.0f,    1.0f,  0.0f,  0.0f,   1.0f, 1.0f,
                 1.0f,  1.0f,  1.0f,    1.0f,  0.0f,  0.0f,   0.0f, 1.0f,
                
                // face inferior
                -1.0f, -1.0f, -1.0f,    0.0f, -1.0f,  0.0f,   0.0f, 0.0f,
                 1.0f, -1.0f, -1.0f,    0.0f, -1.0f,  0.0f,   1.0f, 0.0f,
                 1.0f, -1.0f,  1.0f,    0.0f, -1.0f,  0.0f,   1.0f, 1.0f,
                -1.0f, -1.0f, -1.0f,    0.0f, -1.0f,  0.0f,   0.0f, 0.0f,
                 1.0f, -1.0f,  1.0f,    0.0f, -1.0f,  0.0f,   1.0f, 1.0f,
                -1.0f, -1.0f,  1.0f,    0.0f, -1.0f,  0.0f,   0.0f, 1.0f,
                
                // face superior
                -1.0f,  1.0f,  1.0f,    0.0f,  1.0f,  0.0f,   0.0f, 0.0f,
                 1.0f,  1.0f,  1.0f,    0.0f,  1.0f,  0.0f,   1.0f, 0.0f,
                 1.0f,  1.0f, -1.0f,    0.0f,  1.0f,  0.0f,   1.0f, 1.0f,
                -1.0f,  1.0f,  1.0f,    0.0f,  1.0f,  0.0f,   0.0f, 0.0f,
                 1.0f,  1.0f, -1.0f,    0.0f,  1.0f,  0.0f,   1.0f, 1.0f,
                -1.0f,  1.0f, -1.0f,    0.0f,  1.0f,  0.0f,   0.0f, 1.0f,
                
                // face posterior
                 1.0f, -1.0f, -1.0f,    0.0f,  0.0f, -1.0f,   0.0f, 0.0f,
                -1.0f, -1.0f, -1.0f,    0.0f,  0.0f, -1.0f,   1.0f, 0.0f,
                -1.0f,  1.0f, -1.0f,    0.0f,  0.0f, -1.0f,   1.0f, 1.0f,
                 1.0f, -1.0f, -1.0f,    0.0f,  0.0f, -1.0f,   0.0f, 0.0f,
                -1.0f,  1.0f, -1.0f,    0.0f,  0.0f, -1.0f,   1.0f, 1.0f,
                 1.0f,  1.0f, -1.0f,    0.0f,  0.0f, -1.0f,   0.0f, 1.0f,
                
                // face frontal
                -1.0f, -1.0f,  1.0f,    0.0f,  0.0f,  1.0f,   0.0f, 0.0f,
                 1.0f, -1.0f,  1.0f,    0.0f,  0.0f,  1.0f,   1.0f, 0.0f,
                 1.0f,  1.0f,  1.0f,    0.0f,  0.0f,  1.0f,   1.0f, 1.0f,
                -1.0f, -1.0f,  1.0f,    0.0f,  0.0f,  1.0f,   0.0f, 0.0f,
                 1.0f,  1.0f,  1.0f,    0.0f,  0.0f,  1.0f,   1.0f, 1.0f,
                -1.0f,  1.0f,  1.0f,    0.0f,  0.0f,  1.0f,   0.0f, 1.0f
            };

            _gl.GenVertexArrays(1, out _cubeVAO);
            _gl.GenBuffers(1, out _cubeVBO);

            // preencher buffer
            _gl.BindBuffer(BufferTargetARB.ArrayBuffer, _cubeVBO);
            unsafe
            {
                fixed (float* buf = vertices)
                {
                    _gl.BufferData(BufferTargetARB.ArrayBuffer, (uint)(vertices.Length * sizeof(float)), buf, BufferUsageARB.StaticDraw);
                }
            }            

            // vincular atributos de vértice
            _gl.BindVertexArray(_cubeVAO);

            _gl.EnableVertexAttribArray(0);
            unsafe
            {
                _gl.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, 8 * sizeof(float), (void*)0);
            }

            _gl.EnableVertexAttribArray(1);
            unsafe
            {
                _gl.VertexAttribPointer(1, 3, VertexAttribPointerType.Float, false, 8 * sizeof(float), (void*)(3 * sizeof(float)));
            }

            _gl.EnableVertexAttribArray(2);
            unsafe
            {
                _gl.VertexAttribPointer(2, 2, VertexAttribPointerType.Float, false, 8 * sizeof(float), (void*)(6 * sizeof(float)));
            }

            _gl.BindBuffer(BufferTargetARB.ArrayBuffer, 0);
            _gl.BindVertexArray(0);
        }

        // renderizar cubo
        _gl.BindVertexArray(_cubeVAO);
        _gl.DrawArrays(PrimitiveType.Triangles, 0, 36);
        _gl.BindVertexArray(0);
    }

    // renderQuad() renderiza um quadrilátero XY de 1x1 em NDC
    // --------------------------------------------------
    private static uint _quadVAO = 0;
    private static uint _quadVBO;

    private static void RenderQuad()
    {
        if (_quadVAO == 0)
        {
            float[] quadVertices =
            {
                // posições           // coordenadas de textura
                -1.0f,  1.0f, 0.0f,   0.0f, 1.0f,
                -1.0f, -1.0f, 0.0f,   0.0f, 0.0f,
                 1.0f,  1.0f, 0.0f,   1.0f, 1.0f,
                 1.0f, -1.0f, 0.0f,   1.0f, 0.0f
            };

            // setup plane VAO
            _gl.GenVertexArrays(1, out _quadVAO);
            _gl.GenBuffers(1, out _quadVBO);

            _gl.BindVertexArray(_quadVAO);

            _gl.BindBuffer(BufferTargetARB.ArrayBuffer, _quadVBO);
            unsafe
            {
                fixed (float* buf = quadVertices)
                {
                    _gl.BufferData(BufferTargetARB.ArrayBuffer, (uint)(quadVertices.Length * sizeof(float)), buf, BufferUsageARB.StaticDraw);
                }
            }            

            _gl.EnableVertexAttribArray(0);
            _gl.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, 5 * sizeof(float), 0);

            _gl.EnableVertexAttribArray(1);
            _gl.VertexAttribPointer(1, 2, VertexAttribPointerType.Float, false, 5 * sizeof(float), 3 * sizeof(float));
        }

        _gl.BindVertexArray(_quadVAO);
        _gl.DrawArrays(PrimitiveType.TriangleStrip, 0, 4);
        _gl.BindVertexArray(0);
    }

    private static void OnKeyDown(IKeyboard keyboard, Key key, int keyCode)
    {
        if (key == Key.Escape)
        {
            _window.Close();
        }

        if (key == Key.Space)
        {
            _bloom = !_bloom;
        }
    }

    // processar toda a entrada: consultar a GLFW para saber se teclas relevantes foram pressionadas ou liberadas neste quadro e reagir de acordo
    // --------------------------------------------------
    private static void ProcessInput()
    {
        if (_primaryKeyboard.IsKeyPressed(Key.W))
        {
            _camera.ProcessKeyboard(Camera_Movement.FORWARD, _deltaTime);
        }
        if (_primaryKeyboard.IsKeyPressed(Key.S))
        {
            _camera.ProcessKeyboard(Camera_Movement.BACKWARD, _deltaTime);
        }
        if (_primaryKeyboard.IsKeyPressed(Key.A))
        {
            _camera.ProcessKeyboard(Camera_Movement.LEFT, _deltaTime);
        }
        if (_primaryKeyboard.IsKeyPressed(Key.D))
        {
            _camera.ProcessKeyboard(Camera_Movement.RIGHT, _deltaTime);
        }

        if (_primaryKeyboard.IsKeyPressed(Key.Q))
        {
            if (_exposure > 0.0f)
            {
                _exposure -= 0.0001f;
            }
            else
            {
                _exposure = 0.0f;
            }
        }
        else if (_primaryKeyboard.IsKeyPressed(Key.E))
        {
            _exposure += 0.0001f;
        }
    }

    // glfw: sempre que o tamanho da janela é alterado (pelo SO ou por redimensionamento do usuário), esta função de callback é executada
    // --------------------------------------------------
    private static void FramebufferSizeCallback(int width, int height)
    {
        // certifique-se de que a viewport corresponda às novas dimensões da janela; observe que a largura e
        // a altura serão significativamente maiores do que as especificadas em telas Retina.
        _gl.Viewport(0, 0, (uint)width, (uint)height);
    }

    // glfw: sempre que o mouse se move, este callback é chamado
    // --------------------------------------------------
    private static void MouseCallback(IMouse mouse, Vector2 position)
    {
        float xpos = position.X;
        float ypos = position.Y;

        if (_firstMouse)
        {
            _lastX = xpos;
            _lastY = ypos;

            _firstMouse = false;
        }

        float xoffset = xpos - _lastX;
        float yoffset = _lastY - ypos; // invertido, já que as coordenadas y vão de baixo para cima

        _lastX = xpos;
        _lastY = ypos;

        _camera.ProcessMouseMovement(xoffset, yoffset);
    }

    // glfw: whenever the mouse scroll wheel scrolls, this callback is called
    // --------------------------------------------------
    private static void ScrollCallback(IMouse mouse, ScrollWheel scrollWheel)
    {
        _camera.ProcessMouseScroll(scrollWheel.Y);
    }

    // função utilitária para carregar uma textura 2D a partir de um arquivo
    // --------------------------------------------------
    private static uint LoadTexture(string path, bool gammaCorrection)
    {
        uint textureID;
        _gl.GenTextures(1, out textureID);

        int width, height;
        byte[] data;

        ImageResult image;

        using (FileStream stream = File.OpenRead(path))
        {
            image = ImageResult.FromStream(stream, ColorComponents.Default);

            width = image.Width;
            height = image.Height;
            data = image.Data;
        }

        if (data != null)
        {
            InternalFormat internalFormat = InternalFormat.Rgb;
            PixelFormat pixelFormat = PixelFormat.Rgb;

            if (image.Comp == ColorComponents.Grey)
            {
                internalFormat = InternalFormat.Red;
                pixelFormat = PixelFormat.Red;
            }
            else if (image.Comp == ColorComponents.RedGreenBlue)
            {
                internalFormat = gammaCorrection ? InternalFormat.Srgb : InternalFormat.Rgb;
                pixelFormat = PixelFormat.Rgb;
            }
            else if (image.Comp == ColorComponents.RedGreenBlueAlpha)
            {
                internalFormat = gammaCorrection ? InternalFormat.SrgbAlpha : InternalFormat.Rgba;
                pixelFormat = PixelFormat.Rgba;
            }

            _gl.BindTexture(TextureTarget.Texture2D, textureID);
            unsafe
            {
                fixed (byte* ptr = data)
                {
                    _gl.TexImage2D(TextureTarget.Texture2D, 0, internalFormat, (uint)width, (uint)height, 0, pixelFormat, PixelType.UnsignedByte, ptr);
                }
            }
            _gl.GenerateMipmap(TextureTarget.Texture2D);

            _gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.Repeat);
            _gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.Repeat);
            _gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.LinearMipmapLinear);
            _gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);
        }
        else
        {
            Console.WriteLine("Falha ao carregar a textura no caminho: " + path);
        }

        return textureID;
    }
}
