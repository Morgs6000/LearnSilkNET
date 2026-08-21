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

    // Propriedades
    private static uint _screenWidth = 800, _screenHeight = 600;

    // câmera
    private static Camera _camera = new Camera(new Vector3(0.0f, 0.0f, 3.0f));
    private static float _lastX = 400, _lastY = 300;
    private static bool _firstMouse = true;

    private static float _deltaTime = 0.0f;
    private static float _lastFrame = 0.0f;

    private static Shader _shader = null!;

    private static uint _cubeVAO, _cubeVBO;
    private static uint _planeVAO, _planeVBO;

    private static uint _cubeTexture;
    private static uint _floorTexture;

    private static void Main(string[] args)
    {
        WindowOptions options = WindowOptions.Default;

        options.Size = new Vector2D<int>((int)_screenWidth, (int)_screenHeight);
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
            _primaryKeyboard.KeyDown += KeyCallback;
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

        // Define as dimensões da viewport
        _gl.Viewport(0, 0, _screenWidth, _screenHeight);

        // configurar estado global do OpenGL
        // --------------------------------------------------
        _gl.Enable(EnableCap.DepthTest);
        // _gl.DepthFunc(DepthFunction.Always); // Define para sempre passar no teste de profundidade (mesmo efeito de glDisable(GL_DEPTH_TEST))

        // construir e compilar nosso programa de shader
        // --------------------------------------------------
        _shader = new Shader(_gl, "src/csm.vs", "src/csm.fs");

        // Define os dados do objeto (buffers, atributos de vértice)
        float[] cubeVertices =
        {
            // Posições            // Coordenadas de textura
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
            // Posições            // Coordenadas de textura (Observe que definimos esses valores como maiores que 1, o que, juntamente com GL_REPEAT como modo de repetição de textura, fará com que a textura do chão se repita)
            -5.0f, -0.5f, -5.0f,   0.0f, 0.0f,
             5.0f, -0.5f, -5.0f,   2.0f, 0.0f,
             5.0f, -0.5f,  5.0f,   2.0f, 2.0f,
            -5.0f, -0.5f, -5.0f,   0.0f, 0.0f,
             5.0f, -0.5f,  5.0f,   2.0f, 2.0f,
            -5.0f, -0.5f,  5.0f,   0.0f, 2.0f
        };

        // Setup cube VAO
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

        // Setup plane VAO
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

        _gl.BindVertexArray(0);

        // Carregar texturas
        _cubeTexture = LoadTexture("res/textures/marble.jpg");
        _floorTexture = LoadTexture("res/textures/metal.png");
    }

    private static void OnResize(Vector2D<int> newSize)
    {
        
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
        DoMovement();
    }

    // loop de renderização
    // --------------------------------------------------
    private static void OnRender(double deltaTime)
    {
        // Limpa o buffer de cor
        _gl.ClearColor(0.1f, 0.1f, 0.1f, 1.0f);
        _gl.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

        // Desenhar objetos
        _shader.Use();

        Matrix4x4 model;
        Matrix4x4 view = _camera.GetViewMatrix();
        Matrix4x4 projection = Matrix4x4.CreatePerspectiveFieldOfView(
            fieldOfView:       MathHelper.DegreesToRadians(_camera.Zoom), 
            aspectRatio:       (float)_screenWidth / (float)_screenHeight, 
            nearPlaneDistance: 0.1f, 
            farPlaneDistance:  100.0f
        );

        _shader.SetMat4("view", view);
        _shader.SetMat4("projection", projection);

        // Cubos
        _gl.BindVertexArray(_cubeVAO);
        _gl.BindTexture(TextureTarget.Texture2D, _cubeTexture); // Omitimos a parte do glActiveTexture, pois TEXTURE0 já é a unidade de textura ativa padrão. (o sampler usado no fragment shader também está definido como 0 por padrão)

        model = Matrix4x4.CreateTranslation(new Vector3(-1.0f, 0.0f, -1.0f));
        _shader.SetMat4("model", model);

        _gl.DrawArrays(PrimitiveType.Triangles, 0, 36);

        model = Matrix4x4.Identity;
        model *= Matrix4x4.CreateTranslation(new Vector3(2.0f, 0.0f, 0.0f));
        _shader.SetMat4("model", model);

        _gl.DrawArrays(PrimitiveType.Triangles, 0, 36);

        // Chão
        _gl.BindVertexArray(_planeVAO);
        _gl.BindTexture(TextureTarget.Texture2D, _floorTexture);

        model = Matrix4x4.Identity;
        _shader.SetMat4("model", model);

        _gl.DrawArrays(PrimitiveType.Triangles, 0, 6);

        _gl.BindVertexArray(0);
    }

    private static void OnClosing()
    {
        
    }

    // Esta função carrega uma textura a partir de um arquivo. Nota: funções de carregamento de textura como estas
    // geralmente são gerenciadas por um 'Gerenciador de Recursos' que administra todos os recursos (como texturas, modelos e áudio).
    // Para fins de aprendizado, vamos defini-la apenas como uma função utilitária.
    private static void FramebufferSizeCallback(int width, int height)
    {
        // certifique-se de que a viewport corresponda às novas dimensões da janela; observe que a largura e
        // a altura serão significativamente maiores do que as especificadas em telas Retina.
        _gl.Viewport(0, 0, (uint)width, (uint)height);
    }

    // função utilitária para carregar uma textura 2D a partir de um arquivo
    // --------------------------------------------------
    private static uint LoadTexture(string path)
    {
        //Gerar ID de textura e carregar dados de textura
        uint textureID;
        _gl.GenTextures(1, out textureID);

        int width, height;
        byte[] data;

        using (FileStream stream = File.OpenRead(path))
        {
            ImageResult image = ImageResult.FromStream(stream, ColorComponents.RedGreenBlue);

            width = image.Width;
            height = image.Height;
            data = image.Data;
        }

        _gl.BindTexture(TextureTarget.Texture2D, textureID);
        unsafe
        {
            fixed (byte* ptr = data)
            {
                _gl.TexImage2D(TextureTarget.Texture2D, 0, InternalFormat.Rgb, (uint)width, (uint)height, 0, PixelFormat.Rgb, PixelType.UnsignedByte, ptr);
            }
        }
        _gl.GenerateMipmap(TextureTarget.Texture2D);

        _gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.Repeat);
        _gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.Repeat);
        _gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.LinearMipmapLinear);
        _gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);

        return textureID;
    }

    // Move/altera as posições da câmera com base na entrada do usuário
    private static void DoMovement()
    {
        // Controles da câmera
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

    // Chamado sempre que uma tecla é pressionada ou liberada via GLFW
    private static void KeyCallback(IKeyboard keyboard, Key key, int keyCode)
    {
        if (key == Key.Escape)
        {
            _window.Close();
        }
    }

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
        float yoffset = _lastY - ypos;

        _lastX = xpos;
        _lastY = ypos;

        _camera.ProcessMouseMovement(xoffset, yoffset);
    }

    private static void ScrollCallback(IMouse mouse, ScrollWheel scrollWheel)
    {
        _camera.ProcessMouseScroll(scrollWheel.Y);
    }
}
