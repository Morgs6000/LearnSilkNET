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

    private static Shader _shaderRed = null!;
    private static Shader _shaderGreen = null!;
    private static Shader _shaderBlue = null!;
    private static Shader _shaderYellow = null!;

    private static uint _cubeVAO, _cubeVBO;

    private static uint _uboMatrices;

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
        _shaderRed = new Shader(_gl, "src/advanced_glsl.vs", "src/red.fs");
        _shaderGreen = new Shader(_gl, "src/advanced_glsl.vs", "src/green.fs");
        _shaderBlue = new Shader(_gl, "src/advanced_glsl.vs", "src/blue.fs");
        _shaderYellow = new Shader(_gl, "src/advanced_glsl.vs", "src/yellow.fs");

        // configurar dados de vértice (e buffer(s)) e configurar atributos de vértice
        // --------------------------------------------------
        float[] cubeVertices =
        {
            // posições
            -0.5f, -0.5f, -0.5f,
            -0.5f, -0.5f,  0.5f,
            -0.5f,  0.5f,  0.5f,
            -0.5f, -0.5f, -0.5f,
            -0.5f,  0.5f,  0.5f,
            -0.5f,  0.5f, -0.5f,
            
             0.5f, -0.5f,  0.5f,
             0.5f, -0.5f, -0.5f,
             0.5f,  0.5f, -0.5f,
             0.5f, -0.5f,  0.5f,
             0.5f,  0.5f, -0.5f,
             0.5f,  0.5f,  0.5f,
            
            -0.5f, -0.5f, -0.5f,
             0.5f, -0.5f, -0.5f,
             0.5f, -0.5f,  0.5f,
            -0.5f, -0.5f, -0.5f,
             0.5f, -0.5f,  0.5f,
            -0.5f, -0.5f,  0.5f,
            
            -0.5f,  0.5f,  0.5f,
             0.5f,  0.5f,  0.5f,
             0.5f,  0.5f, -0.5f,
            -0.5f,  0.5f,  0.5f,
             0.5f,  0.5f, -0.5f,
            -0.5f,  0.5f, -0.5f,
            
             0.5f, -0.5f, -0.5f,
            -0.5f, -0.5f, -0.5f,
            -0.5f,  0.5f, -0.5f,
             0.5f, -0.5f, -0.5f,
            -0.5f,  0.5f, -0.5f,
             0.5f,  0.5f, -0.5f,
            
            -0.5f, -0.5f,  0.5f,
             0.5f, -0.5f,  0.5f,
             0.5f,  0.5f,  0.5f,
            -0.5f, -0.5f,  0.5f,
             0.5f,  0.5f,  0.5f,
            -0.5f,  0.5f,  0.5f
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
            _gl.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, 3 * sizeof(float), (void*)0);
        }

        // configurar um objeto de buffer uniforme
        // --------------------------------------------------

        // primeiro. Obtemos os índices de bloco relevantes
        uint uniformBlockIndexRed = _gl.GetUniformBlockIndex(_shaderRed.ID, "Matrices");
        uint uniformBlockIndexGreen = _gl.GetUniformBlockIndex(_shaderGreen.ID, "Matrices");
        uint uniformBlockIndexBlue = _gl.GetUniformBlockIndex(_shaderBlue.ID, "Matrices");
        uint uniformBlockIndexYellow = _gl.GetUniformBlockIndex(_shaderYellow.ID, "Matrices");

        // então, vinculamos o bloco de uniformes de cada shader a este ponto de vinculação de uniformes
        _gl.UniformBlockBinding(_shaderRed.ID, uniformBlockIndexRed, 0);
        _gl.UniformBlockBinding(_shaderGreen.ID, uniformBlockIndexGreen, 0);
        _gl.UniformBlockBinding(_shaderBlue.ID, uniformBlockIndexBlue, 0);
        _gl.UniformBlockBinding(_shaderYellow.ID, uniformBlockIndexYellow, 0);

        // Agora, de fato, crie o buffer
        _gl.GenBuffers(1, out _uboMatrices);
        _gl.BindBuffer(BufferTargetARB.UniformBuffer, _uboMatrices);
        unsafe
        {
            _gl.BufferData(BufferTargetARB.UniformBuffer, (uint)(2 * sizeof(Matrix4x4)), null, BufferUsageARB.StaticDraw);
        }
        _gl.BindBuffer(BufferTargetARB.UniformBuffer, 0);

        // define o intervalo do buffer que se conecta a um ponto de vinculação de uniform
        unsafe
        {
            _gl.BindBufferRange(BufferTargetARB.UniformBuffer, 0, _uboMatrices, 0, (uint)(2 * sizeof(Matrix4x4)));
        }

        // armazena a matriz de projeção (agora fazemos isso apenas uma vez) (nota: não usamos mais o zoom alterando o FoV)
        Matrix4x4 projection = Matrix4x4.CreatePerspectiveFieldOfView(
            fieldOfView:       MathHelper.DegreesToRadians(45.0f), 
            aspectRatio:       (float)SCR_WIDTH / (float)SCR_HEIGHT, 
            nearPlaneDistance: 0.1f, 
            farPlaneDistance:  100.0f
        );
        _gl.BindBuffer(BufferTargetARB.UniformBuffer, _uboMatrices);
        unsafe
        {
            _gl.BufferSubData(BufferTargetARB.UniformBuffer, 0, (uint)(sizeof(Matrix4x4)), (float*)&projection);
        }
        _gl.BindBuffer(BufferTargetARB.UniformBuffer, 0);
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

        // define as matrizes de visualização e projeção no bloco uniform — só precisamos fazer isso uma vez por iteração do loop.
        Matrix4x4 view = _camera.GetViewMatrix();
        _gl.BindBuffer(BufferTargetARB.UniformBuffer, _uboMatrices);
        unsafe
        {
            _gl.BufferSubData(BufferTargetARB.UniformBuffer, sizeof(Matrix4x4), (uint)(sizeof(Matrix4x4)), (float*)&view);
        }
        _gl.BindBuffer(BufferTargetARB.UniformBuffer, 0);

        // desenhar 4 cubos

        // RED
        _gl.BindVertexArray(_cubeVAO);
        _shaderRed.Use();
        Matrix4x4 model = Matrix4x4.Identity;
        model *= Matrix4x4.CreateTranslation(new Vector3(-0.75f, 0.75f, 0.0f)); // mover para o canto superior esquerdo
        _shaderRed.SetMat4("model", model);
        _gl.DrawArrays(PrimitiveType.Triangles, 0, 36);

        // GREEN
        _gl.BindVertexArray(_cubeVAO);
        _shaderGreen.Use();
        model = Matrix4x4.Identity;
        model *= Matrix4x4.CreateTranslation(new Vector3(0.75f, 0.75f, 0.0f)); // mover para o canto superior direito
        _shaderGreen.SetMat4("model", model);
        _gl.DrawArrays(PrimitiveType.Triangles, 0, 36);

        // YELLOW
        _gl.BindVertexArray(_cubeVAO);
        _shaderYellow.Use();
        model = Matrix4x4.Identity;
        model *= Matrix4x4.CreateTranslation(new Vector3(-0.75f, -0.75f, 0.0f)); // mover para baixo à esquerda
        _shaderYellow.SetMat4("model", model);
        _gl.DrawArrays(PrimitiveType.Triangles, 0, 36);

        // BLUE
        _gl.BindVertexArray(_cubeVAO);
        _shaderBlue.Use();
        model = Matrix4x4.Identity;
        model *= Matrix4x4.CreateTranslation(new Vector3(0.75f, -0.75f, 0.0f)); // mover para baixo e para a direita
        _shaderBlue.SetMat4("model", model);
        _gl.DrawArrays(PrimitiveType.Triangles, 0, 36);
    }

    private static void OnClosing()
    {
        // opcional: desalocar todos os recursos assim que não forem mais necessários:
        // --------------------------------------------------
        _gl.DeleteVertexArrays(1, ref _cubeVAO);
        _gl.DeleteBuffers(1, ref _cubeVBO);
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

    // carrega uma textura cubemap a partir de 6 faces de textura individuais
    // ordem:
    // +X (direita)
    // -X (esquerda)
    // +Y (topo)
    // -Y (base)
    // +Z (frente)
    // -Z (trás)
    // --------------------------------------------------
    private static uint LoadCubemap(List<string> faces)
    {
        uint textureID;
        _gl.GenTextures(1, out textureID);
        _gl.BindTexture(TextureTarget.TextureCubeMap, textureID);

        int width, height;
        byte[] data;

        for (int i = 0; i < faces.Count(); i++)
        {
            using (FileStream stream = File.OpenRead(faces[i]))
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
                        _gl.TexImage2D(TextureTarget.TextureCubeMapPositiveX + i, 0, InternalFormat.Rgb, (uint)width, (uint)height, 0, PixelFormat.Rgb, PixelType.UnsignedByte, ptr);
                    }
                }
            }
            else
            {
                Console.WriteLine("A textura de cubemap falhou ao carregar no caminho: " + faces[i]);
            }
        }

        _gl.TexParameter(TextureTarget.TextureCubeMap, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Linear);
        _gl.TexParameter(TextureTarget.TextureCubeMap, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);
        _gl.TexParameter(TextureTarget.TextureCubeMap, TextureParameterName.TextureWrapS, (int)TextureWrapMode.ClampToEdge);
        _gl.TexParameter(TextureTarget.TextureCubeMap, TextureParameterName.TextureWrapT, (int)TextureWrapMode.ClampToEdge);
        _gl.TexParameter(TextureTarget.TextureCubeMap, TextureParameterName.TextureWrapR, (int)TextureWrapMode.ClampToEdge);

        return textureID;
    }
}
