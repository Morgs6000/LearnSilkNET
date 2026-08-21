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
    private static Camera _camera = new Camera(new Vector3(0.0f, 0.0f, 3.0f));
    private static float _lastX = (float)SCR_WIDTH / 2.0f;
    private static float _lastY = (float)SCR_HEIGHT / 2.0f;
    private static bool _firstMouse = true;

    // tempo
    private static float _deltaTime = 0.0f;
    private static float _lastFrame = 0.0f;

    // malhas
    private static uint _planeVAO;

    private static Shader _shader = null!;
    private static Shader _simpleDepthShader = null!;
    private static Shader _debugDepthQuad = null!;

    private static uint _planeVBO;

    private static uint _woodTexture;

    private const uint SHADOW_WIDTH = 1024, SHADOW_HEIGHT = 1024;
    private static uint _depthMapFBO;
    private static uint _depthMap;

    // informações de iluminação
    // --------------------------------------------------
    private static Vector3 _lightPos = new Vector3(-2.0f, 4.0f, -1.0f);

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
        _shader = new Shader(_gl, "src/shadow_mapping.vs", "src/shadow_mapping.fs");
        _simpleDepthShader = new Shader(_gl, "src/shadow_mapping_depth.vs", "src/shadow_mapping_depth.fs");
        _debugDepthQuad = new Shader(_gl, "src/debug_quad.vs", "src/debug_quad_depth.fs");

        // configurar dados de vértice (e buffer(s)) e configurar atributos de vértice
        // --------------------------------------------------
        float[] planeVertices =
        {
            // posições              // normais          // coordenadas de textura
            -25.0f, -0.5f, -25.0f,   0.0f, 1.0f, 0.0f,    0.0f,  0.0f,
             25.0f, -0.5f, -25.0f,   0.0f, 1.0f, 0.0f,   25.0f,  0.0f,
             25.0f, -0.5f,  25.0f,   0.0f, 1.0f, 0.0f,   25.0f, 25.0f,
            -25.0f, -0.5f, -25.0f,   0.0f, 1.0f, 0.0f,    0.0f,  0.0f,
             25.0f, -0.5f,  25.0f,   0.0f, 1.0f, 0.0f,   25.0f, 25.0f,
            -25.0f, -0.5f,  25.0f,   0.0f, 1.0f, 0.0f,    0.0f, 25.0f
        };

        // plane VAO
        _gl.GenVertexArrays(1, out _planeVAO);
        _gl.GenBuffers(1, out _planeVBO);

        _gl.BindVertexArray(_planeVAO);

        _gl.BindBuffer(BufferTargetARB.ArrayBuffer, _planeVBO);
        unsafe
        {
            fixed (float* buf = planeVertices)
            {
                _gl.BufferData(BufferTargetARB.ArrayBuffer, (uint)(planeVertices.Length * sizeof(float)), buf, BufferUsageARB.StaticDraw);
            }
        }

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

        _gl.BindVertexArray(0);

        // carregar texturas
        // --------------------------------------------------
        _woodTexture = LoadTexture("res/textures/wood.png");

        // configurar FBO do mapa de profundidade
        // --------------------------------------------------
        _gl.GenFramebuffers(1, out _depthMapFBO);

        // criar textura de profundidade
        _gl.GenTextures(1, out _depthMap);
        _gl.BindTexture(TextureTarget.Texture2D, _depthMap);

        unsafe
        {
            _gl.TexImage2D(TextureTarget.Texture2D, 0, InternalFormat.DepthComponent, SHADOW_WIDTH, SHADOW_HEIGHT, 0, PixelFormat.DepthComponent, PixelType.Float, null);
        }

        _gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Nearest);
        _gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Nearest);
        _gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.ClampToBorder);
        _gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.ClampToBorder);

        float[] borderColor = { 1.0f, 1.0f, 1.0f, 1.0f };

        _gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureBorderColor, borderColor);

        // anexa a textura de profundidade como buffer de profundidade do FBO
        _gl.BindFramebuffer(FramebufferTarget.Framebuffer, _depthMapFBO);
        _gl.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.DepthAttachment, TextureTarget.Texture2D, _depthMap, 0);

        _gl.DrawBuffer(DrawBufferMode.None);
        _gl.ReadBuffer(ReadBufferMode.None);

        _gl.BindFramebuffer(FramebufferTarget.Framebuffer, 0);

        // configuração do shader
        // --------------------------------------------------
        _shader.Use();
        _shader.SetInt("diffuseTexture", 0);
        _shader.SetInt("shadowMap", 1);

        _debugDepthQuad.Use();
        _debugDepthQuad.SetInt("depthMap", 0);
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
        // alterar a posição da luz ao longo do tempo
        // _lightPos.X = MathF.Sin((float)Glfw.GetApi().GetTime()) * 3.0f;
        // _lightPos.Z = MathF.Cos((float)Glfw.GetApi().GetTime()) * 2.0f;
        // _lightPos.Y = 5.0f + MathF.Cos((float)Glfw.GetApi().GetTime()) * 1.0f;

        // render
        // --------------------------------------------------
        _gl.ClearColor(0.1f, 0.1f, 0.1f, 1.0f);
        _gl.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

        // 1. renderizar a profundidade da cena em uma textura (da perspectiva da luz)
        // --------------------------------------------------
        Matrix4x4 lightProjection, lightView;
        Matrix4x4 lightSpaceMatrix;

        float near_plane = 1.0f, far_plane = 7.5f;

        lightProjection = Matrix4x4.CreateOrthographicOffCenter(
            left:       -10.0f, 
            right:       10.0f, 
            bottom:     -10.0f, 
            top:         10.0f,
            zNearPlane:  near_plane, 
            zFarPlane:   far_plane
        );
        lightView = Matrix4x4.CreateLookAt(
            cameraPosition: _lightPos, 
            cameraTarget:   new Vector3(0.0f), 
            cameraUpVector: new Vector3(0.0f, 1.0f, 0.0f)
        );
        lightSpaceMatrix = lightView * lightProjection;

        // renderizar a cena do ponto de vista da luz
        _simpleDepthShader.Use();
        _simpleDepthShader.SetMat4("lightSpaceMatrix", lightSpaceMatrix);

        _gl.Viewport(0, 0, SHADOW_WIDTH, SHADOW_HEIGHT);
        _gl.BindFramebuffer(FramebufferTarget.Framebuffer, _depthMapFBO);
            _gl.Clear(ClearBufferMask.DepthBufferBit);
            _gl.ActiveTexture(TextureUnit.Texture0);
            _gl.BindTexture(TextureTarget.Texture2D, _woodTexture);
            RenderScene(_simpleDepthShader);
        _gl.BindFramebuffer(FramebufferTarget.Framebuffer, 0);

        // redefinir viewport
        _gl.Viewport(0, 0, SCR_WIDTH, SCR_HEIGHT);
        _gl.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

        // 2. renderizar a cena normalmente usando o mapa de profundidade/sombra gerado
        // --------------------------------------------------
        _shader.Use();

        Matrix4x4 projection = Matrix4x4.CreatePerspectiveFieldOfView(
            fieldOfView:       MathHelper.DegreesToRadians(_camera.Zoom), 
            aspectRatio:       (float)SCR_WIDTH / (float)SCR_HEIGHT, 
            nearPlaneDistance: 0.1f, 
            farPlaneDistance:  100.0f
        );
        Matrix4x4 view = _camera.GetViewMatrix();

        _shader.SetMat4("projection", projection);
        _shader.SetMat4("view", view);

        // definir uniformes claros
        _shader.SetVec3("viewPos", _camera.Position);
        _shader.SetVec3("lightPos", _lightPos);
        _shader.SetMat4("lightSpaceMatrix", lightSpaceMatrix);

        _gl.ActiveTexture(TextureUnit.Texture0);
        _gl.BindTexture(TextureTarget.Texture2D, _woodTexture);

        _gl.ActiveTexture(TextureUnit.Texture1);
        _gl.BindTexture(TextureTarget.Texture2D, _depthMap);

        RenderScene(_shader);

        // renderiza o mapa de profundidade em um quad para depuração visual
        // --------------------------------------------------
        _debugDepthQuad.Use();
        _debugDepthQuad.SetFloat("near_plane", near_plane);
        _debugDepthQuad.SetFloat("far_plane", far_plane);

        _gl.ActiveTexture(TextureUnit.Texture0);
        _gl.BindTexture(TextureTarget.Texture2D, _depthMap);

        // RenderQuad();
    }

    private static void OnClosing()
    {
        // opcional: desalocar todos os recursos assim que não forem mais necessários:
        // --------------------------------------------------
        _gl.DeleteVertexArrays(1, ref _planeVAO);
        _gl.DeleteBuffers(1, ref _planeVBO);
    }

    // renderiza a cena 3D
    // --------------------------------------------------
    private static void RenderScene(Shader shader)
    {
        // chão
        Matrix4x4 model = Matrix4x4.Identity;
        shader.SetMat4("model", model);
        _gl.BindVertexArray(_planeVAO);
        _gl.DrawArrays(PrimitiveType.Triangles, 0, 6);

        // cubos
        model = Matrix4x4.Identity;
        model *= Matrix4x4.CreateScale(new Vector3(0.5f));
        model *= Matrix4x4.CreateTranslation(new Vector3(0.0f, 1.5f, 0.0f));
        shader.SetMat4("model", model);
        RenderCube();
        
        model = Matrix4x4.Identity;
        model *= Matrix4x4.CreateScale(new Vector3(0.5f));
        model *= Matrix4x4.CreateTranslation(new Vector3(2.0f, 0.0f, 1.0f));
        shader.SetMat4("model", model);
        RenderCube();
        
        model = Matrix4x4.Identity;
        model *= Matrix4x4.CreateScale(new Vector3(0.25f));
        model *= Matrix4x4.CreateFromAxisAngle(Vector3.Normalize(new Vector3(1.0f, 0.0f, 1.0f)), MathHelper.DegreesToRadians(60.0f));
        model *= Matrix4x4.CreateTranslation(new Vector3(-1.0f, 0.0f, 2.0f));
        shader.SetMat4("model", model);
        RenderCube();
    }

    // renderCube() renders a 1x1 3D cube in NDC.
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
                // posições            // coordenadas de textura
                -1.0f,  1.0f, 0.0f,   0.0f, 1.0f,
                -1.0f, -1.0f, 0.0f,   0.0f, 0.0f,
                 1.0f,  1.0f, 0.0f,   1.0f, 1.0f,
                 1.0f, -1.0f, 0.0f,   1.0f, 0.0f,
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
            unsafe
            {
                _gl.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, 5 * sizeof(float), (void*)0);
            }

            _gl.EnableVertexAttribArray(1);
            unsafe
            {
                _gl.VertexAttribPointer(1, 2, VertexAttribPointerType.Float, false, 5 * sizeof(float), (void*)(3 * sizeof(float)));
            }
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

    // função utilitária para carregar uma textura 2D a partir de um arquivo
    // --------------------------------------------------
    private static uint LoadTexture(string path)
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
                internalFormat = InternalFormat.Rgb;
                pixelFormat = PixelFormat.Rgb;
            }
            else if (image.Comp == ColorComponents.RedGreenBlueAlpha)
            {
                internalFormat = InternalFormat.Rgba;
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

            _gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, internalFormat == InternalFormat.Rgba ? (int)TextureWrapMode.ClampToEdge : (int)TextureWrapMode.Repeat); // para este tutorial: use GL_CLAMP_TO_EDGE para evitar bordas semitransparentes. Devido à interpolação, são amostrados texels da repetição seguinte.
            _gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, internalFormat == InternalFormat.Rgba ? (int)TextureWrapMode.ClampToEdge : (int)TextureWrapMode.Repeat);
            _gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.LinearMipmapLinear);
            _gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);
        }
        else
        {
            Console.WriteLine("Texture failed to load at path: " + path);
        }

        return textureID;
    }
}
