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
    private static float _deltaTime = 0.0f; // tempo entre o quadro atual e o quadro anterior
    private static float _lastFrame = 0.0f;

    private static Shader _ourShader = null!;

    // posições dos nossos cubos no espaço do mundo
    private static Vector3[] _cubePositions =
    {
        new Vector3( 0.0f,  0.0f,  0.0f),
        new Vector3( 2.0f,  5.0f, -15.0f),
        new Vector3(-1.5f, -2.2f, -2.5f),
        new Vector3(-3.8f, -2.0f, -12.3f),
        new Vector3( 2.4f, -0.4f, -3.5f),
        new Vector3(-1.7f,  3.0f, -7.5f),
        new Vector3( 1.3f, -2.0f, -2.5f),
        new Vector3( 1.5f,  2.0f, -2.5f),
        new Vector3( 1.5f,  0.2f, -1.5f),
        new Vector3(-1.3f,  1.0f, -1.5f)
    };

    private static uint _vertexArrayObject;
    private static uint _vertexBufferObject;

    private static uint _texture1, _texture2;

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
        _ourShader = new Shader(_gl, "src/camera.vs", "src/camera.fs");

        // configurar dados de vértice (e buffer(s)) e configurar atributos de vértice
        // --------------------------------------------------
        float[] vertices =
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

        _gl.GenVertexArrays(1, out _vertexArrayObject);
        _gl.GenBuffers(1, out _vertexBufferObject);

        _gl.BindVertexArray(_vertexArrayObject);

        _gl.BindBuffer(BufferTargetARB.ArrayBuffer, _vertexBufferObject);
        unsafe
        {
            fixed (float* buf = vertices)
            {
                _gl.BufferData(BufferTargetARB.ArrayBuffer, (uint)(vertices.Length * sizeof(float)), buf, BufferUsageARB.StaticDraw);
            }
        }

        // atributo de posição
        unsafe
        {
            _gl.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, 5 * sizeof(float), (void*)0);
        }
        _gl.EnableVertexAttribArray(0);
        
        // atributo de coordenada de textura
        unsafe
        {
            _gl.VertexAttribPointer(1, 2, VertexAttribPointerType.Float, false, 5 * sizeof(float), (void*)(3 * sizeof(float)));
        }
        _gl.EnableVertexAttribArray(1);

        // carregar e criar uma textura
        // --------------------------------------------------

        // textura 1
        // --------------------------------------------------
        _gl.GenTextures(1, out _texture1);
        _gl.BindTexture(TextureTarget.Texture2D, _texture1); // todas as operações GL_TEXTURE_2D subsequentes agora afetam este objeto de textura

        // define os parâmetros de repetição da textura
        _gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.Repeat);
        _gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.Repeat);

        // definir parâmetros de filtragem de textura
        _gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Linear);
        _gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);

        // carregar imagem, criar textura e gerar mipmaps
        int width, height;
        byte[] data;

        StbImage.stbi_set_flip_vertically_on_load(1); // instrui a stb_image.h a inverter as texturas carregadas no eixo Y.

        using (FileStream stream = File.OpenRead("res/textures/container.jpg"))
        {
            ImageResult image = ImageResult.FromStream(stream, ColorComponents.RedGreenBlue);

            width = image.Width;
            height = image.Height;
            data = image.Data;
        }

        if (data != null)
        {
            unsafe
            {
                fixed (byte* ptr = data)
                {
                    _gl.TexImage2D(TextureTarget.Texture2D, 0, InternalFormat.Rgb, (uint)width, (uint)height, 0, PixelFormat.Rgb, PixelType.UnsignedByte, ptr);
                }
            }
            _gl.GenerateMipmap(TextureTarget.Texture2D);
        }
        else
        {
            Console.WriteLine("Falha ao carregar a textura");
        }

        // textura 2
        // --------------------------------------------------
        _gl.GenTextures(1, out _texture2);
        _gl.BindTexture(TextureTarget.Texture2D, _texture2);

        // define os parâmetros de repetição da textura
        _gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.Repeat);
        _gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.Repeat);

        // definir parâmetros de filtragem de textura
        _gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Linear);
        _gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);

        // carregar imagem, criar textura e gerar mipmaps
        using (FileStream stream = File.OpenRead("res/textures/awesomeface.png"))
        {
            ImageResult image = ImageResult.FromStream(stream, ColorComponents.RedGreenBlueAlpha);

            width = image.Width;
            height = image.Height;
            data = image.Data;
        }

        if (data != null)
        {
            // observe que o awesomeface.png possui transparência e, portanto, um canal alfa; certifique-se de informar ao OpenGL que o tipo de dado é GL_RGBA
            unsafe
            {
                fixed (byte* ptr = data)
                {
                    _gl.TexImage2D(TextureTarget.Texture2D, 0, InternalFormat.Rgba, (uint)width, (uint)height, 0, PixelFormat.Rgba, PixelType.UnsignedByte, ptr);
                }
            }
            _gl.GenerateMipmap(TextureTarget.Texture2D);
        }
        else
        {
            Console.WriteLine("Falha ao carregar a textura");
        }

        // informar ao OpenGL, para cada sampler, a qual unidade de textura ele pertence (isso só precisa ser feito uma vez)
        // --------------------------------------------------
        _ourShader.Use();
        _ourShader.SetInt("texture1", 0);
        _ourShader.SetInt("texture2", 1);
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
        // render
        // --------------------------------------------------
        _gl.ClearColor(0.2f, 0.3f, 0.3f, 1.0f);
        _gl.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit); // limpe também o buffer de profundidade agora!

        // vincular texturas às unidades de textura correspondentes
        _gl.ActiveTexture(TextureUnit.Texture0);
        _gl.BindTexture(TextureTarget.Texture2D, _texture1);

        _gl.ActiveTexture(TextureUnit.Texture1);
        _gl.BindTexture(TextureTarget.Texture2D, _texture2);

        // ativar shader
        _ourShader.Use();

        // passa a matriz de projeção para o shader (note que, neste caso, ela pode mudar a cada quadro)
        Matrix4x4 projection = Matrix4x4.CreatePerspectiveFieldOfView(
            fieldOfView:       MathHelper.DegreesToRadians(_camera.Zoom), 
            aspectRatio:       (float)SCR_WIDTH / (float)SCR_HEIGHT, 
            nearPlaneDistance: 0.1f, 
            farPlaneDistance:  100.0f
        );
        _ourShader.SetMat4("projection", projection);

        // transformação de câmera/visualização
        Matrix4x4 view = _camera.GetViewMatrix();
        _ourShader.SetMat4("view", view);

        // renderizar caixas
        _gl.BindVertexArray(_vertexArrayObject);
        
        for (int i = 0; i < 10; i++)
        {
            // calcula a matriz de modelo para cada objeto e a passa para o shader antes de desenhar
            Matrix4x4 model = Matrix4x4.Identity;
            
            float angle = 20.0f * i;
            model *= Matrix4x4.CreateFromAxisAngle(Vector3.Normalize(new Vector3(1.0f, 0.3f, 0.5f)), MathHelper.DegreesToRadians(angle));

            model *= Matrix4x4.CreateTranslation(_cubePositions[i]);

            _ourShader.SetMat4("model", model);

            _gl.DrawArrays(PrimitiveType.Triangles, 0, 36);
        }        
    }

    private static void OnClosing()
    {
        // opcional: desalocar todos os recursos assim que não forem mais necessários:
        // --------------------------------------------------
        _gl.DeleteVertexArrays(1, ref _vertexArrayObject);
        _gl.DeleteBuffers(1, ref _vertexBufferObject);
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
}
