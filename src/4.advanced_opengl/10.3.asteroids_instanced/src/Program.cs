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
    private static Camera _camera = new Camera(new Vector3(0.0f, 0.0f, 155.0f));
    private static float _lastX = (float)SCR_WIDTH / 2.0f;
    private static float _lastY = (float)SCR_HEIGHT / 2.0f;
    private static bool _firstMouse = true;

    // tempo
    private static float _deltaTime = 0.0f;
    private static float _lastFrame = 0.0f;

    private static Shader _asteroidShader = null!;
    private static Shader _planetShader = null!;

    private static Model _rock = null!;
    private static Model _planet = null!;

    private static uint _amount;
    private static Matrix4x4[] _modelMatrices = [];

    private static uint _buffer;

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
        _asteroidShader = new Shader(_gl, "src/asteroids.vs", "src/asteroids.fs");
        _planetShader = new Shader(_gl, "src/planet.vs", "src/planet.fs");

        // carregar modelos
        // --------------------------------------------------
        _rock = new Model(_gl, "res/objects/rock/rock.obj");
        _planet = new Model(_gl, "res/objects/planet/planet.obj");

        // gerar uma lista grande de matrizes de transformação de modelo semialeatórias
        // --------------------------------------------------
        _amount = 100000;
        _modelMatrices = new Matrix4x4[_amount];

        Random rand = new Random((int)_glfw.GetTime()); // inicializar a semente de números aleatórios

        float radius = 150.0f;
        float offset = 25.0f;

        for (int i = 0; i < _amount; i++)
        {
            Matrix4x4 model = Matrix4x4.Identity;

            // 1. rotação: adicionar rotação aleatória em torno de um vetor de eixo de rotação escolhido de forma (semi)aleatória
            float rotAngle = (float)(rand.Next() % 360);
            model *= Matrix4x4.CreateFromAxisAngle(Vector3.Normalize(new Vector3(0.4f, 0.6f, 0.8f)), rotAngle);

            // 2. scale: Escala entre 0,05 e 0,25f
            float scale = (float)((rand.Next() % 20) / 100.0f + 0.05f);
            model *= Matrix4x4.CreateScale(new Vector3(scale));

            // 3. tradução: deslocar ao longo de um círculo com 'raio' no intervalo [-offset, offset]
            float angle = (float)i / (float)_amount * 360.0f;
            float displacement = (rand.Next() % (int)(2 * offset * 100)) / 100.0f - offset;
            float x = MathF.Sin(angle) * radius + displacement;
            displacement = (rand.Next() % (int)(2 * offset * 100)) / 100.0f - offset;
            float y = displacement * 0.4f; // mantenha a altura do campo de asteroides menor em relação à largura nos eixos x e z
            displacement = (rand.Next() % (int)(2 * offset * 100)) / 100.0f - offset;
            float z = MathF.Cos(angle) * radius + displacement;
            model *= Matrix4x4.CreateTranslation(new Vector3(x, y, z));
            
            // 4. agora adicione à lista de matrizes
            _modelMatrices[i] = model;
        }

        // configurar array instanciado
        // --------------------------------------------------
        _gl.GenBuffers(1, out _buffer);
        _gl.BindBuffer(BufferTargetARB.ArrayBuffer, _buffer);
        unsafe
        {
            fixed (Matrix4x4* buf = _modelMatrices)
            {
                _gl.BufferData(BufferTargetARB.ArrayBuffer, (uint)(_amount * sizeof(Matrix4x4)), buf, BufferUsageARB.StaticDraw);
            }
        }

        // define as matrizes de transformação como um atributo de vértice de instância (com divisor 1)
        // nota: estamos "trapaceando" um pouco ao pegar o VAO — agora declarado publicamente — da(s) malha(s) do modelo e adicionar novos vertexAttribPointers
        // normalmente, o ideal seria fazer isso de uma maneira mais organizada, mas, para fins de aprendizado, isso serve.
        // --------------------------------------------------
        for (int i = 0; i < _rock.meshes.Count(); i++)
        {
            uint VAO = _rock.meshes[i].VAO;
            _gl.BindVertexArray(VAO);

            // definir ponteiros de atributo para a matriz (4 vezes vec4)
            _gl.EnableVertexAttribArray(3);
            unsafe
            {
                _gl.VertexAttribPointer(3, 4, VertexAttribPointerType.Float, false, (uint)(sizeof(Matrix4x4)), (void*)0);
            }

            _gl.EnableVertexAttribArray(4);
            unsafe
            {
                _gl.VertexAttribPointer(4, 4, VertexAttribPointerType.Float, false, (uint)(sizeof(Matrix4x4)), (void*)(sizeof(Vector4)));
            }

            _gl.EnableVertexAttribArray(5);
            unsafe
            {
                _gl.VertexAttribPointer(5, 4, VertexAttribPointerType.Float, false, (uint)(sizeof(Matrix4x4)), (void*)(2 * sizeof(Vector4)));
            }

            _gl.EnableVertexAttribArray(6);
            unsafe
            {
                _gl.VertexAttribPointer(6, 4, VertexAttribPointerType.Float, false, (uint)(sizeof(Matrix4x4)), (void*)(3 * sizeof(Vector4)));
            }

            _gl.VertexAttribDivisor(3, 1);
            _gl.VertexAttribDivisor(4, 1);
            _gl.VertexAttribDivisor(5, 1);
            _gl.VertexAttribDivisor(6, 1);

            _gl.BindVertexArray(0);
        }
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
        _gl.ClearColor(0.1f, 0.1f, 0.1f, 1.0f);
        _gl.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

        // configurar matrizes de transformação
        Matrix4x4 projection = Matrix4x4.CreatePerspectiveFieldOfView(
            fieldOfView:       MathHelper.DegreesToRadians(45.0f), 
            aspectRatio:       (float)SCR_WIDTH / (float)SCR_HEIGHT, 
            nearPlaneDistance: 0.1f, 
            farPlaneDistance:  1000.0f
        );
        Matrix4x4 view = _camera.GetViewMatrix();

        _asteroidShader.Use();
        _asteroidShader.SetMat4("projection", projection);
        _asteroidShader.SetMat4("view", view);

        _planetShader.Use();
        _planetShader.SetMat4("projection", projection);
        _planetShader.SetMat4("view", view);

        // desenhar planeta
        Matrix4x4 model = Matrix4x4.Identity;
        model *= Matrix4x4.CreateScale(new Vector3(4.0f, 4.0f, 4.0f));
        model *= Matrix4x4.CreateTranslation(new Vector3(0.0f, -3.0f, 0.0f));
        _planetShader.SetMat4("model", model);
        _planet.Draw(_planetShader);

        // desenhar meteoritos
        _asteroidShader.Use();
        _asteroidShader.SetInt("texture_diffuse1", 0);

        _gl.ActiveTexture(TextureUnit.Texture0);
        _gl.BindTexture(TextureTarget.Texture2D, _rock.textures_loaded[0].id); // nota: também tornamos público (em vez de privado) o vetor textures_loaded da classe model.

        for (int i = 0; i < _rock.meshes.Count(); i++)
        {
            _gl.BindVertexArray(_rock.meshes[i].VAO);
            unsafe
            {
                _gl.DrawElementsInstanced(PrimitiveType.Triangles, (uint)_rock.meshes[i].indices.Count(), DrawElementsType.UnsignedInt, (void*)0, _amount);
            }
            _gl.BindVertexArray(0);
        }
    }

    private static void OnClosing()
    {
        
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
