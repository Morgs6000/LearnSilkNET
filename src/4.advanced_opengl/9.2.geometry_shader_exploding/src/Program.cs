using System.Numerics;
using Silk.NET.GLFW;
using Silk.NET.Input;
using Silk.NET.Maths;
using Silk.NET.OpenGL;
using Silk.NET.Windowing;

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
    private static float _lastX = (float)SCR_WIDTH / 2.0f;
    private static float _lastY = (float)SCR_HEIGHT / 2.0f;
    private static bool _firstMouse = true;

    // tempo
    private static float _deltaTime = 0.0f;
    private static float _lastFrame = 0.0f;

    private static Shader _shader = null!;

    private static Model _nanosuit = null!;

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
        _shader = new Shader(_gl, "src/geometry_shader.vs", "src/geometry_shader.fs", "src/geometry_shader.gs");

        // carregar modelos
        // --------------------------------------------------
        _nanosuit = new Model(_gl, "res/objects/nanosuit/nanosuit.obj");
    }

    private static void OnResize(Vector2D<int> newSize)
    {
        FramebufferSizeCallback(newSize.X, newSize.Y);
    }

    private static void OnUpdate(double deltaTime)
    {
        // lógica de tempo por quadro
        // --------------------------------------------------
        float currentFrame = (float)Glfw.GetApi().GetTime();
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
        _gl.ClearColor(0.1f, 0.1f, 0.1f, 1.0f);
        _gl.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

        // configurar matrizes de transformação
        Matrix4x4 projection = Matrix4x4.CreatePerspectiveFieldOfView(
            fieldOfView:       MathHelper.DegreesToRadians(45.0f), 
            aspectRatio:       (float)SCR_WIDTH / (float)SCR_HEIGHT, 
            nearPlaneDistance: 0.1f, 
            farPlaneDistance:  100.0f
        );
        Matrix4x4 view = _camera.GetViewMatrix();
        Matrix4x4 model = Matrix4x4.Identity;
        
        _shader.Use();
        _shader.SetMat4("projection", projection);
        _shader.SetMat4("view", view);
        _shader.SetMat4("model", model);

        // adicionar componente de tempo ao shader de geometria na forma de um uniform
        _shader.SetFloat("time", (float)Glfw.GetApi().GetTime());

        // desenhar modelo
        _nanosuit.Draw(_shader);
    }

    private static void OnClosing()
    {
        
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
