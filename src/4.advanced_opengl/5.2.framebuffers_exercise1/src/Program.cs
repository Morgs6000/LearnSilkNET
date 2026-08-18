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

    private static IKeyboard _keyboard = null!;
    private static IMouse _mouse = null!;

    // configurações
    private const int SCR_WIDTH = 800;
    private const int SCR_HEIGHT = 600;

    // câmera
    private static Camera _camera = new Camera(new Vector3(0.0f, 0.0f, 3.0f));
    private static float _lastX = SCR_WIDTH / 2.0f;
    private static float _lastY = SCR_HEIGHT / 2.0f;
    private static bool _firstMouse = true;

    // tempo
    private static float _deltaTime = 0.0f;
    private static float _lastFrame = 0.0f;

    private static Shader _shader = null!;
    private static Shader _screenShader = null!;

    private static uint _cubeVAO, _cubeVBO;
    private static uint _planeVAO, _planeVBO;
    private static uint _quadVAO, _quadVBO;

    private static uint _cubeTexture;
    private static uint _floorTexture;
    
    private static uint _framebuffer;
    private static uint _textureColorbuffer;
    private static uint _rbo;

    private static void Main(string[] args)
    {
        // criação da janela glfw
        // --------------------------------------------------
        WindowOptions options = WindowOptions.Default;

        options.Size = new Vector2D<int>(SCR_WIDTH, SCR_HEIGHT);
        options.Title = "Learn Silk.NET";
        options.IsVisible = false;

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
        _keyboard = input.Keyboards[0];
        _mouse = input.Mice[0];

        _mouse.MouseMove += MouseCallback;
        _mouse.Scroll += ScrollCallback;

        _gl = _window.CreateOpenGL();

        // instruir o GLFW a capturar o mouse
        _mouse.Cursor.CursorMode = CursorMode.Raw;

        // configurar estado global do OpenGL
        // --------------------------------------------------
        _gl.Enable(EnableCap.DepthTest);

        // construir e compilar nosso programa de shader
        // --------------------------------------------------
        _shader = new Shader(_gl, "src/framebuffers.vs", "src/framebuffers.fs");
        _screenShader = new Shader(_gl, "src/framebuffers_screen.vs", "src/framebuffers_screen.fs");

        // configurar dados de vértice (e buffer(s)) e configurar atributos de vértice
        // --------------------------------------------------
        float[] cubeVertices =
        {
            // posições            // coordenadas de textura
            -0.5f, -0.5f, -0.5f,   0.0f, 0.0f,
            -0.5f, -0.5f,  0.5f,   1.0f, 0.0f,
            -0.5f,  0.5f,  0.5f,   1.0f, 1.0f,
            -0.5f, -0.5f, -0.5f,   0.0f, 0.0f,
            -0.5f,  0.5f,  0.5f,   1.0f, 1.0f,
            -0.5f,  0.5f, -0.5f,   0.0f, 1.0f,
            
             0.5f, -0.5f,  0.5f,   0.0f, 0.0f,
             0.5f, -0.5f, -0.5f,   1.0f, 0.0f,
             0.5f,  0.5f, -0.5f,   1.0f, 1.0f,
             0.5f, -0.5f,  0.5f,   0.0f, 0.0f,
             0.5f,  0.5f, -0.5f,   1.0f, 1.0f,
             0.5f,  0.5f,  0.5f,   0.0f, 1.0f,
            
            -0.5f, -0.5f, -0.5f,   0.0f, 0.0f,
             0.5f, -0.5f, -0.5f,   1.0f, 0.0f,
             0.5f, -0.5f,  0.5f,   1.0f, 1.0f,
            -0.5f, -0.5f, -0.5f,   0.0f, 0.0f,
             0.5f, -0.5f,  0.5f,   1.0f, 1.0f,
            -0.5f, -0.5f,  0.5f,   0.0f, 1.0f,
            
            -0.5f,  0.5f,  0.5f,   0.0f, 0.0f,
             0.5f,  0.5f,  0.5f,   1.0f, 0.0f,
             0.5f,  0.5f, -0.5f,   1.0f, 1.0f,
            -0.5f,  0.5f,  0.5f,   0.0f, 0.0f,
             0.5f,  0.5f, -0.5f,   1.0f, 1.0f,
            -0.5f,  0.5f, -0.5f,   0.0f, 1.0f,
            
             0.5f, -0.5f, -0.5f,   0.0f, 0.0f,
            -0.5f, -0.5f, -0.5f,   1.0f, 0.0f,
            -0.5f,  0.5f, -0.5f,   1.0f, 1.0f,
             0.5f, -0.5f, -0.5f,   0.0f, 0.0f,
            -0.5f,  0.5f, -0.5f,   1.0f, 1.0f,
             0.5f,  0.5f, -0.5f,   0.0f, 1.0f,
            
            -0.5f, -0.5f,  0.5f,   0.0f, 0.0f,
             0.5f, -0.5f,  0.5f,   1.0f, 0.0f,
             0.5f,  0.5f,  0.5f,   1.0f, 1.0f,
            -0.5f, -0.5f,  0.5f,   0.0f, 0.0f,
             0.5f,  0.5f,  0.5f,   1.0f, 1.0f,
            -0.5f,  0.5f,  0.5f,   0.0f, 1.0f
        };

        float[] planeVertices =
        {
            // posições            // coordenadas de textura
            -5.0f, -0.5f, -5.0f,   0.0f, 0.0f,
             5.0f, -0.5f, -5.0f,   2.0f, 0.0f,
             5.0f, -0.5f,  5.0f,   2.0f, 2.0f,
            -5.0f, -0.5f, -5.0f,   0.0f, 0.0f,
             5.0f, -0.5f,  5.0f,   2.0f, 2.0f,
            -5.0f, -0.5f,  5.0f,   0.0f, 2.0f
        };

        float[] quadVertices = // atributos de vértice para um quadrilátero que preenche a tela inteira em Coordenadas de Dispositivo Normalizadas. NOTE que este plano agora é muito menor e está no topo da tela
        {
            // posições     // coordenadas de textura 
            -0.3f,  0.7f,   0.0f, 0.0f,
             0.3f,  0.7f,   1.0f, 0.0f,
             0.3f,  1.0f,   1.0f, 1.0f,
            -0.3f,  0.7f,   0.0f, 0.0f,
             0.3f,  1.0f,   1.0f, 1.0f,
            -0.3f,  1.0f,   0.0f, 1.0f
        };

        // cube VAO
        _gl.GenVertexArrays(1, out _cubeVAO);
        _gl.GenBuffers(1, out _cubeVBO);

        _gl.BindVertexArray(_cubeVAO);

        _gl.BindBuffer(BufferTargetARB.ArrayBuffer, _cubeVBO);
        unsafe
        {
            fixed (float* buf = cubeVertices)
            {
                _gl.BufferData(BufferTargetARB.ArrayBuffer, (uint)(cubeVertices.Length * sizeof(float)), buf, BufferUsageARB.StaticDraw);
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

        _gl.BindVertexArray(0);

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
            _gl.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, 5 * sizeof(float), (void*)0);
        }

        _gl.EnableVertexAttribArray(1);
        unsafe
        {
            _gl.VertexAttribPointer(1, 2, VertexAttribPointerType.Float, false, 5 * sizeof(float), (void*)(3 * sizeof(float)));
        }

        // screen quad VAO
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
            _gl.VertexAttribPointer(0, 2, VertexAttribPointerType.Float, false, 4 * sizeof(float), (void*)0);
        }

        _gl.EnableVertexAttribArray(1);
        unsafe
        {
            _gl.VertexAttribPointer(1, 2, VertexAttribPointerType.Float, false, 4 * sizeof(float), (void*)(2 * sizeof(float)));
        }

        _gl.BindVertexArray(0);

        // carregar texturas
        // --------------------------------------------------
        _cubeTexture = LoadTexture("res/textures/container.jpg");
        _floorTexture = LoadTexture("res/textures/metal.png");

        // configuração do shader
        // --------------------------------------------------
        _shader.Use();
        _shader.SetInt("texture1", 0);

        _screenShader.Use();
        _screenShader.SetInt("screenTexture", 0);

        // configuração do framebuffer
        // --------------------------------------------------
        _gl.GenFramebuffers(1, out _framebuffer);
        _gl.BindFramebuffer(FramebufferTarget.Framebuffer, _framebuffer);

        // criar uma textura de anexo de cor
        _gl.GenTextures(1, out _textureColorbuffer);
        _gl.BindTexture(TextureTarget.Texture2D, _textureColorbuffer);
        unsafe
        {
            _gl.TexImage2D(TextureTarget.Texture2D, 0, InternalFormat.Rgb, (uint)SCR_WIDTH, (uint)SCR_HEIGHT, 0, PixelFormat.Rgb, PixelType.UnsignedByte, null);
        }
        _gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Linear);
        _gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);
        _gl.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment0, TextureTarget.Texture2D, _textureColorbuffer, 0);

        // cria um objeto renderbuffer para anexos de profundidade e stencil (não faremos amostragem deles)
        _gl.GenRenderbuffers(1, out _rbo);
        _gl.BindRenderbuffer(RenderbufferTarget.Renderbuffer, _rbo);
        _gl.RenderbufferStorage(RenderbufferTarget.Renderbuffer, InternalFormat.Depth24Stencil8, (uint)SCR_WIDTH, (uint)SCR_HEIGHT); // use um único objeto renderbuffer tanto para o buffer de profundidade quanto para o buffer de stencil.
        _gl.FramebufferRenderbuffer(FramebufferTarget.Framebuffer, FramebufferAttachment.DepthStencilAttachment, RenderbufferTarget.Renderbuffer, _rbo); // agora, de fato, anexe-o

        // agora que criamos o framebuffer e adicionamos todos os anexos, queremos verificar se ele está realmente completo agora
        if (_gl.CheckFramebufferStatus(FramebufferTarget.Framebuffer) != GLEnum.FramebufferComplete)
        {
            Console.WriteLine("ERROR::FRAMEBUFFER:: Framebuffer is not complete!");
        }

        _gl.BindFramebuffer(FramebufferTarget.Framebuffer, 0);

        // desenhar como estrutura de arame
        // _gl.PolygonMode(TriangleFace.FrontAndBack, PolygonMode.Line);
    }

    private static void OnResize(Vector2D<int> newSize)
    {
        FramebufferSizeCallback(newSize.X, newSize.Y);
    }

    private static void OnUpdate(double deltaTime)
    {
        // lógica de tempo por quadro
        // --------------------------------------------------
        float currentTime = (float)Glfw.GetApi().GetTime();
        _deltaTime = currentTime - _lastFrame;
        _lastFrame = currentTime;

        // input
        // --------------------------------------------------
        ProcessInput();
    }

    // loop de renderização
    // --------------------------------------------------
    private static void OnRender(double deltaTime)
    {
        // primeira passada de renderização: textura de espelho. 
        // vincular ao framebuffer e desenhar na textura de cor como faríamos
        // normalmente, mas com a câmera de visualização invertida. 
        // vincular ao framebuffer e desenhar a cena na textura de cor como faríamos normalmente.
        // --------------------------------------------------
        _gl.BindFramebuffer(FramebufferTarget.Framebuffer, _framebuffer);
        _gl.Enable(EnableCap.DepthTest); // habilita o teste de profundidade (desabilitado para renderizar o quad no espaço da tela)

        // certifique-se de limpar o conteúdo do framebuffer
        _gl.ClearColor(0.1f, 0.1f, 0.1f, 1.0f);
        _gl.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

        _shader.Use();
        Matrix4x4 model = Matrix4x4.Identity;
        _camera.Yaw += 180.0f; // rotaciona a guinada da câmera em 180 graus
        _camera.ProcessMouseMovement(0, 0, false); // chame isto para garantir que os vetores da câmera sejam atualizados; note que desativamos as restrições de pitch para este caso específico (caso contrário, não conseguimos inverter os valores de pitch da câmera)
        Matrix4x4 view = _camera.GetViewMatrix();
        _camera.Yaw -= 180.0f; // redefina-o para sua orientação original
        _camera.ProcessMouseMovement(0, 0, true);
        Matrix4x4 projection = Matrix4x4.CreatePerspectiveFieldOfView(
            fieldOfView:       MathHelper.DegreesToRadians(_camera.Zoom), 
            aspectRatio:       (float)SCR_WIDTH / (float)SCR_HEIGHT, 
            nearPlaneDistance: 0.1f, 
            farPlaneDistance:  100.0f
        );
        _shader.SetMat4("view", view);
        _shader.SetMat4("projection", projection);

        // cubes
        _gl.BindVertexArray(_cubeVAO);
        _gl.ActiveTexture(TextureUnit.Texture0);
        _gl.BindTexture(TextureTarget.Texture2D, _cubeTexture);
        model *= Matrix4x4.CreateTranslation(new Vector3(-1.0f, 0.0f, -1.0f));
        _shader.SetMat4("model", model);
        _gl.DrawArrays(PrimitiveType.Triangles, 0, 36);

        model = Matrix4x4.Identity;
        model *= Matrix4x4.CreateTranslation(new Vector3(2.0f, 0.0f, 0.0f));
        _shader.SetMat4("model", model);
        _gl.DrawArrays(PrimitiveType.Triangles, 0, 36);

        // floor
        _gl.BindVertexArray(_planeVAO);
        _gl.BindTexture(TextureTarget.Texture2D, _floorTexture);
        _shader.SetMat4("model", Matrix4x4.Identity);
        _gl.DrawArrays(PrimitiveType.Triangles, 0, 6);
        _gl.BindVertexArray(0);

        // segunda passada de renderização: desenhar normalmente
        // --------------------------------------------------
        _gl.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
        
        _gl.ClearColor(0.1f, 0.1f, 0.1f, 1.0f);
        _gl.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

        model = Matrix4x4.Identity;
        view = _camera.GetViewMatrix();
        _shader.SetMat4("view", view);

        // cubes
        _gl.BindVertexArray(_cubeVAO);
        _gl.ActiveTexture(TextureUnit.Texture0);
        _gl.BindTexture(TextureTarget.Texture2D, _cubeTexture);
        model *= Matrix4x4.CreateTranslation(new Vector3(-1.0f, 0.0f, -1.0f));
        _shader.SetMat4("model", model);
        _gl.DrawArrays(PrimitiveType.Triangles, 0, 36);

        model = Matrix4x4.Identity;
        model *= Matrix4x4.CreateTranslation(new Vector3(2.0f, 0.0f, 0.0f));
        _shader.SetMat4("model", model);
        _gl.DrawArrays(PrimitiveType.Triangles, 0, 36);

        // floor
        _gl.BindVertexArray(_planeVAO);
        _gl.BindTexture(TextureTarget.Texture2D, _floorTexture);        
        _shader.SetMat4("model", Matrix4x4.Identity);
        _gl.DrawArrays(PrimitiveType.Triangles, 0, 6);
        _gl.BindVertexArray(0);

        // agora desenhe o quadrilátero do espelho com a textura da tela
        // --------------------------------------------------
        _gl.Disable(EnableCap.DepthTest); // desabilita o teste de profundidade para que o quad no espaço da tela não seja descartado pelo teste de profundidade.

        _screenShader.Use();
        _gl.BindVertexArray(_quadVAO);
        _gl.BindTexture(TextureTarget.Texture2D, _textureColorbuffer); // use a textura de anexo de cor como a textura do plano quadrangular
        _gl.DrawArrays(PrimitiveType.Triangles, 0, 6);
    }

    private static void OnClosing()
    {
        // opcional: desalocar todos os recursos assim que não forem mais necessários:
        // --------------------------------------------------
        _gl.DeleteVertexArrays(1, ref _cubeVAO);
        _gl.DeleteVertexArrays(1, ref _planeVAO);
        _gl.DeleteVertexArrays(1, ref _quadVAO);
        _gl.DeleteBuffers(1, ref _cubeVBO);
        _gl.DeleteBuffers(1, ref _planeVBO);
        _gl.DeleteBuffers(1, ref _quadVBO);
        _gl.DeleteRenderbuffers(1, ref _rbo);
        _gl.DeleteFramebuffers(1, ref _framebuffer);
    }

    // processar toda a entrada: consultar a GLFW para saber se teclas relevantes foram pressionadas ou liberadas neste quadro e reagir de acordo
    // --------------------------------------------------
    private static void ProcessInput()
    {
        if (_keyboard.IsKeyPressed(Key.Escape))
        {
            _window.Close();
        }

        if (_keyboard.IsKeyPressed(Key.W))
        {
            _camera.ProcessKeyboard(Camera_Movement.FORWARD, _deltaTime);
        }
        if (_keyboard.IsKeyPressed(Key.S))
        {
            _camera.ProcessKeyboard(Camera_Movement.BACKWARD, _deltaTime);
        }
        if (_keyboard.IsKeyPressed(Key.A))
        {
            _camera.ProcessKeyboard(Camera_Movement.LEFT, _deltaTime);
        }
        if (_keyboard.IsKeyPressed(Key.D))
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
