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
    private static float _lastX = SCR_WIDTH / 2.0f;
    private static float _lastY = SCR_HEIGHT / 2.0f;
    private static bool _firstMouse = true;

    // tempo
    private static float _deltaTime = 0.0f;
    private static float _lastFrame = 0.0f;

    // iluminação
    private static Vector3 _lightPos = new Vector3(1.2f, 1.0f, 2.0f);

    private static Shader _lightingShader = null!;
    private static Shader _lightCubeShader = null!;

    private static uint _cubeVAO;
    private static uint _VBO;

    private static uint _lightCubeVAO;

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
        _lightingShader = new Shader(_gl, "src/basic_lighting.vs", "src/basic_lighting.fs");
        _lightCubeShader = new Shader(_gl, "src/light_cube.vs", "src/light_cube.fs");

        // configurar dados de vértice (e buffer(s)) e configurar atributos de vértice
        // --------------------------------------------------
        float[] vertices =
        {
            // posições            // normais
            -0.5f, -0.5f, -0.5f,   -1.0f,  0.0f,  0.0f,
            -0.5f, -0.5f,  0.5f,   -1.0f,  0.0f,  0.0f,
            -0.5f,  0.5f,  0.5f,   -1.0f,  0.0f,  0.0f,
            -0.5f, -0.5f, -0.5f,   -1.0f,  0.0f,  0.0f,
            -0.5f,  0.5f,  0.5f,   -1.0f,  0.0f,  0.0f,
            -0.5f,  0.5f, -0.5f,   -1.0f,  0.0f,  0.0f,
            
             0.5f, -0.5f,  0.5f,    1.0f,  0.0f,  0.0f,
             0.5f, -0.5f, -0.5f,    1.0f,  0.0f,  0.0f,
             0.5f,  0.5f, -0.5f,    1.0f,  0.0f,  0.0f,
             0.5f, -0.5f,  0.5f,    1.0f,  0.0f,  0.0f,
             0.5f,  0.5f, -0.5f,    1.0f,  0.0f,  0.0f,
             0.5f,  0.5f,  0.5f,    1.0f,  0.0f,  0.0f,
            
            -0.5f, -0.5f, -0.5f,    0.0f, -1.0f,  0.0f,
             0.5f, -0.5f, -0.5f,    0.0f, -1.0f,  0.0f,
             0.5f, -0.5f,  0.5f,    0.0f, -1.0f,  0.0f,
            -0.5f, -0.5f, -0.5f,    0.0f, -1.0f,  0.0f,
             0.5f, -0.5f,  0.5f,    0.0f, -1.0f,  0.0f,
            -0.5f, -0.5f,  0.5f,    0.0f, -1.0f,  0.0f,
            
            -0.5f,  0.5f,  0.5f,    0.0f,  1.0f,  0.0f,
             0.5f,  0.5f,  0.5f,    0.0f,  1.0f,  0.0f,
             0.5f,  0.5f, -0.5f,    0.0f,  1.0f,  0.0f,
            -0.5f,  0.5f,  0.5f,    0.0f,  1.0f,  0.0f,
             0.5f,  0.5f, -0.5f,    0.0f,  1.0f,  0.0f,
            -0.5f,  0.5f, -0.5f,    0.0f,  1.0f,  0.0f,
            
             0.5f, -0.5f, -0.5f,    0.0f,  0.0f, -1.0f,
            -0.5f, -0.5f, -0.5f,    0.0f,  0.0f, -1.0f,
            -0.5f,  0.5f, -0.5f,    0.0f,  0.0f, -1.0f,
             0.5f, -0.5f, -0.5f,    0.0f,  0.0f, -1.0f,
            -0.5f,  0.5f, -0.5f,    0.0f,  0.0f, -1.0f,
             0.5f,  0.5f, -0.5f,    0.0f,  0.0f, -1.0f,
            
            -0.5f, -0.5f,  0.5f,    0.0f,  0.0f,  1.0f,
             0.5f, -0.5f,  0.5f,    0.0f,  0.0f,  1.0f,
             0.5f,  0.5f,  0.5f,    0.0f,  0.0f,  1.0f,
            -0.5f, -0.5f,  0.5f,    0.0f,  0.0f,  1.0f,
             0.5f,  0.5f,  0.5f,    0.0f,  0.0f,  1.0f,
            -0.5f,  0.5f,  0.5f,    0.0f,  0.0f,  1.0f
        };

        // primeiro, configure o VAO (e o VBO) do cubo
        _gl.GenVertexArrays(1, out _cubeVAO);
        _gl.GenBuffers(1, out _VBO);

        _gl.BindVertexArray(_cubeVAO);

        _gl.BindBuffer(BufferTargetARB.ArrayBuffer, _VBO);
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
            _gl.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, 6 * sizeof(float), (void*)0);
        }
        _gl.EnableVertexAttribArray(0);

        // atributo de normais
        unsafe
        {
            _gl.VertexAttribPointer(1, 3, VertexAttribPointerType.Float, false, 6 * sizeof(float), (void*)(3 * sizeof(float)));
        }
        _gl.EnableVertexAttribArray(1);

        // segundo, configure o VAO da luz (o VBO permanece o mesmo; os vértices são os mesmos para o objeto de luz, que também é um cubo 3D)
        _gl.GenVertexArrays(1, out _lightCubeVAO);
        _gl.BindVertexArray(_lightCubeVAO);

        // precisamos apenas vincular o VBO (para associá-lo ao glVertexAttribPointer), sem necessidade de preenchê-lo; os dados do VBO já contêm tudo o que precisamos (ele já está vinculado, mas fazemos isso novamente para fins didáticos)
        _gl.BindBuffer(BufferTargetARB.ArrayBuffer, _VBO);

        unsafe
        {
            _gl.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, 6 * sizeof(float), (void*)0);
        }
        _gl.EnableVertexAttribArray(0);
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
        _gl.ClearColor(0.1f, 0.1f, 0.1f, 1.0f);
        _gl.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

        // certifique-se de ativar o shader ao definir uniforms ou desenhar objetos
        _lightingShader.Use();
        _lightingShader.SetVec3("objectColor", 1.0f, 0.5f, 0.31f);
        _lightingShader.SetVec3("lightColor",  1.0f, 1.0f, 1.0f);
        _lightingShader.SetVec3("lightPos", _lightPos);
        
        // transformações de visualização/projeção
        Matrix4x4 projection = Matrix4x4.CreatePerspectiveFieldOfView(
            fieldOfView:       MathHelper.DegreesToRadians(_camera.Zoom), 
            aspectRatio:       (float)SCR_WIDTH / (float)SCR_HEIGHT, 
            nearPlaneDistance: 0.1f, 
            farPlaneDistance:  100.0f
        );
        Matrix4x4 view = _camera.GetViewMatrix();

        _lightingShader.SetMat4("projection", projection);
        _lightingShader.SetMat4("view", view);

        // transformação do mundo
        Matrix4x4 model = Matrix4x4.Identity;
        _lightingShader.SetMat4("model", model);
        
        // renderiza o cubo
        _gl.BindVertexArray(_cubeVAO);
        _gl.DrawArrays(PrimitiveType.Triangles, 0, 36);

        // também desenhe o objeto da lâmpada
        _lightCubeShader.Use();
        _lightCubeShader.SetMat4("projection", projection);
        _lightCubeShader.SetMat4("view", view);

        model = Matrix4x4.Identity;
        model *= Matrix4x4.CreateScale(new Vector3(0.2f)); // um cubo menor
        model *= Matrix4x4.CreateTranslation(_lightPos);
        _lightCubeShader.SetMat4("model", model);      

        _gl.BindVertexArray(_lightCubeVAO);
        _gl.DrawArrays(PrimitiveType.Triangles, 0, 36); 
    }

    private static void OnClosing()
    {
        // opcional: desalocar todos os recursos assim que não forem mais necessários:
        // --------------------------------------------------
        _gl.DeleteVertexArrays(1, ref _cubeVAO);
        _gl.DeleteVertexArrays(1, ref _lightCubeVAO);
        _gl.DeleteBuffers(1, ref _VBO);
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
