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

    // câmera
    private static Camera _camera = new Camera(new Vector3(0.0f, 0.0f, 5.0f));
    private static float _lastX = (float)SCR_WIDTH / 2.0f;
    private static float _lastY = (float)SCR_HEIGHT / 2.0f;
    private static bool _firstMouse = true;

    // tempo
    private static float _deltaTime = 0.0f;
    private static float _lastFrame = 0.0f;

    private static Shader _shaderGeometryPass = null!;
    private static Shader _shaderLightingPass = null!;
    private static Shader _shaderLightBox = null!;

    private static Model _backpack = null!;

    private static List<Vector3> _objectPositions = [];

    private static uint _gBuffer;
    private static uint _gPosition, _gNormal, _gAlbedoSpec;

    private static GLEnum[] _attachments = new GLEnum[3];
    private static uint _rboDepth;

    private const uint NR_LIGHTS = 32;
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

        // instrua a stb_image.h a inverter as texturas carregadas no eixo Y (antes de carregar o modelo).
        StbImage.stbi_set_flip_vertically_on_load(1);

        // construir e compilar nosso programa de shader
        // --------------------------------------------------
        _shaderGeometryPass = new Shader(_gl, "src/g_buffer.vs", "src/g_buffer.fs");
        _shaderLightingPass = new Shader(_gl, "src/deferred_shading.vs", "src/deferred_shading.fs");
        _shaderLightBox = new Shader(_gl, "src/deferred_light_box.vs", "src/deferred_light_box.fs");

        // carregar modelos
        // --------------------------------------------------
        _backpack = new Model(_gl, "res/objects/backpack/backpack.obj");

        _objectPositions.Add(new Vector3(-3.0f,  -0.5f, -3.0f));
        _objectPositions.Add(new Vector3( 0.0f,  -0.5f, -3.0f));
        _objectPositions.Add(new Vector3( 3.0f,  -0.5f, -3.0f));
        _objectPositions.Add(new Vector3(-3.0f,  -0.5f,  0.0f));
        _objectPositions.Add(new Vector3( 0.0f,  -0.5f,  0.0f));
        _objectPositions.Add(new Vector3( 3.0f,  -0.5f,  0.0f));
        _objectPositions.Add(new Vector3(-3.0f,  -0.5f,  3.0f));
        _objectPositions.Add(new Vector3( 0.0f,  -0.5f,  3.0f));
        _objectPositions.Add(new Vector3( 3.0f,  -0.5f,  3.0f));

        // configurar o framebuffer do g-buffer
        // --------------------------------------------------
        _gl.GenFramebuffers(1, out _gBuffer);
        _gl.BindFramebuffer(FramebufferTarget.Framebuffer, _gBuffer);

        // buffer de cor de posição
        _gl.GenTextures(1, out _gPosition);
        _gl.BindTexture(TextureTarget.Texture2D, _gPosition);

        unsafe
        {
            _gl.TexImage2D(TextureTarget.Texture2D, 0, InternalFormat.Rgba16f, SCR_WIDTH, SCR_HEIGHT, 0, PixelFormat.Rgba, PixelType.Float, null);
        }

        _gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Nearest);
        _gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Nearest);

        _gl.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment0, TextureTarget.Texture2D, _gPosition, 0);

        // buffer de cor normal
        _gl.GenTextures(1, out _gNormal);
        _gl.BindTexture(TextureTarget.Texture2D, _gNormal);

        unsafe
        {
            _gl.TexImage2D(TextureTarget.Texture2D, 0, InternalFormat.Rgba16f, SCR_WIDTH, SCR_HEIGHT, 0, PixelFormat.Rgba, PixelType.Float, null);
        }

        _gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Nearest);
        _gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Nearest);

        _gl.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment0, TextureTarget.Texture2D, _gNormal, 0);

        // buffer de cor + cor especular
        _gl.GenTextures(1, out _gAlbedoSpec);
        _gl.BindTexture(TextureTarget.Texture2D, _gAlbedoSpec);

        unsafe
        {
            _gl.TexImage2D(TextureTarget.Texture2D, 0, InternalFormat.Rgba16f, SCR_WIDTH, SCR_HEIGHT, 0, PixelFormat.Rgba, PixelType.Float, null);
        }

        _gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Nearest);
        _gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Nearest);

        _gl.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment0, TextureTarget.Texture2D, _gAlbedoSpec, 0);

        // informar ao OpenGL quais anexos de cor (deste framebuffer) usaremos para a renderização
        _attachments = new GLEnum[3] { GLEnum.ColorAttachment0, GLEnum.ColorAttachment1, GLEnum.ColorAttachment2 };

        _gl.DrawBuffers(3, _attachments);

        // criar e anexar buffer de profundidade (renderbuffer)
        _gl.GenRenderbuffers(1, out _rboDepth);
        _gl.BindRenderbuffer(RenderbufferTarget.Renderbuffer, _rboDepth);
        _gl.RenderbufferStorage(RenderbufferTarget.Renderbuffer, InternalFormat.DepthComponent, SCR_WIDTH, SCR_HEIGHT);
        _gl.FramebufferRenderbuffer(FramebufferTarget.Framebuffer, FramebufferAttachment.DepthAttachment, RenderbufferTarget.Renderbuffer, _rboDepth);

        // finalmente, verifica se o framebuffer está completo
        if (_gl.CheckFramebufferStatus(FramebufferTarget.Framebuffer) != GLEnum.FramebufferComplete)
        {
            Console.WriteLine("Framebuffer not complete!");
        }

        _gl.BindFramebuffer(FramebufferTarget.Framebuffer, 0);

        // informações de iluminação
        // --------------------------------------------------
        Random srand = new Random(13);

        for (int i = 0; i < NR_LIGHTS; i++)
        {
            // calcular deslocamentos levemente aleatórios
            float xPos = (float)(((srand.Next() % 100) / 100.0f) * 6.0f - 3.0f);
            float yPos = (float)(((srand.Next() % 100) / 100.0f) * 6.0f - 4.0f);
            float zPos = (float)(((srand.Next() % 100) / 100.0f) * 6.0f - 3.0f);

            _lightPositions.Add(new Vector3(xPos, yPos, zPos));

            float rColor = (float)(((srand.Next() % 100) / 200.0f) + 0.5f); // entre 0,5 e 1,0
            float gColor = (float)(((srand.Next() % 100) / 200.0f) + 0.5f); // entre 0,5 e 1,0
            float bColor = (float)(((srand.Next() % 100) / 200.0f) + 0.5f); // entre 0,5 e 1,0

            _lightColors.Add(new Vector3(rColor, gColor, bColor));
        }

        // configuração do shader
        // --------------------------------------------------
        _shaderLightingPass.Use();
        _shaderLightingPass.SetInt("gPosition", 0);
        _shaderLightingPass.SetInt("gNormal", 1);
        _shaderLightingPass.SetInt("gAlbedoSpec", 2);
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
        _gl.BindFramebuffer(FramebufferTarget.Framebuffer, _gBuffer);
            _gl.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

            Matrix4x4 projection = Matrix4x4.CreatePerspectiveFieldOfView(
                fieldOfView:       MathHelper.DegreesToRadians(_camera.Zoom), 
                aspectRatio:       (float)SCR_WIDTH / (float)SCR_HEIGHT, 
                nearPlaneDistance: 0.1f, 
                farPlaneDistance:  100.0f
            );
            Matrix4x4 view = _camera.GetViewMatrix();
            Matrix4x4 model = Matrix4x4.Identity;

            _shaderGeometryPass.Use();
            _shaderGeometryPass.SetMat4("projection", projection);
            _shaderGeometryPass.SetMat4("view", view);

            for (int i = 0; i < _objectPositions.Count(); i++)
            {
                model = Matrix4x4.Identity;
                model *= Matrix4x4.CreateScale(new Vector3(0.5f));
                model *= Matrix4x4.CreateTranslation(_objectPositions[i]);
                _shaderGeometryPass.SetMat4("model", model);

                _backpack.Draw(_shaderGeometryPass);
            }
        _gl.BindFramebuffer(FramebufferTarget.Framebuffer, 0);

        // 2. passo de iluminação: calcular a iluminação iterando pixel a pixel sobre um quadrilátero que preenche a tela, utilizando o conteúdo do G-buffer.
        // --------------------------------------------------
        _gl.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

        _shaderLightingPass.Use();

        _gl.ActiveTexture(TextureUnit.Texture0);
        _gl.BindTexture(TextureTarget.Texture2D, _gPosition);

        _gl.ActiveTexture(TextureUnit.Texture1);
        _gl.BindTexture(TextureTarget.Texture2D, _gNormal);

        _gl.ActiveTexture(TextureUnit.Texture2);
        _gl.BindTexture(TextureTarget.Texture2D, _gAlbedoSpec);

        // enviar uniformes leves e adequados
        for (int i = 0; i < _lightPositions.Count(); i++)
        {
            _shaderLightingPass.SetVec3($"lights[{i}].Position", _lightPositions[i]);
            _shaderLightingPass.SetVec3($"lights[{i}].Color", _lightColors[i]);

            // atualizar parâmetros de atenuação e calcular o raio
            const float linear = 0.7f;
            const float quadratic = 1.8f;

            _shaderLightingPass.SetFloat($"lights[{i}].Linear", linear);
            _shaderLightingPass.SetFloat($"lights[{i}].Quadratic", quadratic);
        }

        _shaderLightingPass.SetVec3("viewPos", _camera.Position);

        // finalmente renderiza o quadrilátero
        RenderQuad();

        // 2.5. copiar o conteúdo do buffer de profundidade da geometria para o buffer de profundidade do framebuffer padrão
        // --------------------------------------------------
        _gl.BindFramebuffer(FramebufferTarget.ReadFramebuffer, _gBuffer);
        _gl.BindFramebuffer(FramebufferTarget.DrawFramebuffer, 0); // gravar no framebuffer padrão

        // Realiza o blit para o framebuffer padrão. Observe que isso pode ou não funcionar, pois os formatos internos do FBO e do framebuffer padrão precisam ser compatíveis. 
        // Os formatos internos são definidos pela implementação. Isso funciona em todos os meus sistemas, mas, se não funcionar no seu, provavelmente você terá que gravar no
        // buffer de profundidade em outro estágio do shader (ou, de alguma forma, fazer com que o formato interno do framebuffer padrão corresponda ao formato interno do FBO).
        _gl.BlitFramebuffer(0, 0, (int)SCR_WIDTH, (int)SCR_HEIGHT, 0, 0, (int)SCR_WIDTH, (int)SCR_HEIGHT, ClearBufferMask.DepthBufferBit, BlitFramebufferFilter.Nearest);
        _gl.BindFramebuffer(FramebufferTarget.Framebuffer, 0);

        // 3. renderizar luzes sobre a cena
        // --------------------------------------------------
        _shaderLightBox.Use();
        _shaderLightBox.SetMat4("projection", projection);
        _shaderLightBox.SetMat4("view", view);

        for (int i = 0; i < _lightPositions.Count(); i++)
        {
            model = Matrix4x4.Identity;
            model *= Matrix4x4.CreateScale(new Vector3(0.125f));
            model *= Matrix4x4.CreateTranslation(_lightPositions[i]);
            _shaderLightBox.SetMat4("model", model);
            
            _shaderLightBox.SetVec3("lightColor", _lightColors[i]);

            RenderCube();
        }
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
}
