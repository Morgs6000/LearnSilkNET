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
    private static float _lastX = SCR_WIDTH / 2.0f;
    private static float _lastY = SCR_HEIGHT / 2.0f;
    private static bool _firstMouse = true;

    // tempo
    private static float _deltaTime = 0.0f;
    private static float _lastFrame = 0.0f;

    private static Shader _shader = null!;
    private static Shader _skyboxShader = null!;

    private static uint _cubeVAO, _cubeVBO;
    private static uint _skyboxVAO, _skyboxVBO;

    private static uint _cubeTexture;
    private static uint _cubemapTexture;

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
        _shader = new Shader(_gl, "src/cubemaps.vs", "src/cubemaps.fs");
        _skyboxShader = new Shader(_gl, "src/skybox.vs", "src/skybox.fs");

        // configurar dados de vértice (e buffer(s)) e configurar atributos de vértice
        // --------------------------------------------------
        float[] cubeVertices =
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

        float[] skyboxVertices =
        {
            // posições
            -1.0f, -1.0f, -1.0f,
            -1.0f, -1.0f,  1.0f,
            -1.0f,  1.0f,  1.0f,
            -1.0f, -1.0f, -1.0f,
            -1.0f,  1.0f,  1.0f,
            -1.0f,  1.0f, -1.0f,
            
             1.0f, -1.0f,  1.0f,
             1.0f, -1.0f, -1.0f,
             1.0f,  1.0f, -1.0f,
             1.0f, -1.0f,  1.0f,
             1.0f,  1.0f, -1.0f,
             1.0f,  1.0f,  1.0f,
            
            -1.0f, -1.0f, -1.0f,
             1.0f, -1.0f, -1.0f,
             1.0f, -1.0f,  1.0f,
            -1.0f, -1.0f, -1.0f,
             1.0f, -1.0f,  1.0f,
            -1.0f, -1.0f,  1.0f,
            
            -1.0f,  1.0f,  1.0f,
             1.0f,  1.0f,  1.0f,
             1.0f,  1.0f, -1.0f,
            -1.0f,  1.0f,  1.0f,
             1.0f,  1.0f, -1.0f,
            -1.0f,  1.0f, -1.0f,
            
             1.0f, -1.0f, -1.0f,
            -1.0f, -1.0f, -1.0f,
            -1.0f,  1.0f, -1.0f,
             1.0f, -1.0f, -1.0f,
            -1.0f,  1.0f, -1.0f,
             1.0f,  1.0f, -1.0f,
            
            -1.0f, -1.0f,  1.0f,
             1.0f, -1.0f,  1.0f,
             1.0f,  1.0f,  1.0f,
            -1.0f, -1.0f,  1.0f,
             1.0f,  1.0f,  1.0f,
            -1.0f,  1.0f,  1.0f
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
            _gl.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, 6 * sizeof(float), (void*)0);
        }

        _gl.EnableVertexAttribArray(1);
        unsafe
        {
            _gl.VertexAttribPointer(1, 3, VertexAttribPointerType.Float, false, 6 * sizeof(float), (void*)(3 * sizeof(float)));
        }

        _gl.BindVertexArray(0);

        // skybox VAO
        _gl.GenVertexArrays(1, out _skyboxVAO);
        _gl.GenBuffers(1, out _skyboxVBO);

        _gl.BindVertexArray(_skyboxVAO);

        _gl.BindBuffer(BufferTargetARB.ArrayBuffer, _skyboxVBO);
        unsafe
        {
            fixed (float* buf = skyboxVertices)
            {
                _gl.BufferData(BufferTargetARB.ArrayBuffer, (uint)(skyboxVertices.Length * sizeof(float)), buf, BufferUsageARB.StaticDraw);
            }
        }

        _gl.EnableVertexAttribArray(0);
        unsafe
        {
            _gl.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, 3 * sizeof(float), (void*)0);
        }

        // carregar texturas
        // --------------------------------------------------
        _cubeTexture = LoadTexture("res/textures/container.jpg");

        List<string> faces = new List<string>()
        {
            "res/textures/skybox/right.jpg",
            "res/textures/skybox/left.jpg",
            "res/textures/skybox/top.jpg",
            "res/textures/skybox/bottom.jpg",
            "res/textures/skybox/front.jpg",
            "res/textures/skybox/back.jpg"
        };

        _cubemapTexture = LoadCubemap(faces);

        // configuração do shader
        // --------------------------------------------------
        _shader.Use();
        _shader.SetInt("texture1", 0);

        _skyboxShader.Use();
        _skyboxShader.SetInt("skybox", 0);
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

        // desenha a cena normalmente
        _shader.Use();

        Matrix4x4 model = Matrix4x4.Identity;
        Matrix4x4 view = _camera.GetViewMatrix();
        Matrix4x4 projection = Matrix4x4.CreatePerspectiveFieldOfView(
            fieldOfView:       MathHelper.DegreesToRadians(_camera.Zoom), 
            aspectRatio:       (float)SCR_WIDTH / (float)SCR_HEIGHT, 
            nearPlaneDistance: 0.1f, 
            farPlaneDistance:  100.0f
        );

        _shader.SetMat4("model", model);
        _shader.SetMat4("view", view);
        _shader.SetMat4("projection", projection);
        _shader.SetVec3("cameraPos", _camera.Position);

        // cubes
        _gl.BindVertexArray(_cubeVAO);
        _gl.ActiveTexture(TextureUnit.Texture0);
        _gl.BindTexture(TextureTarget.Texture2D, _cubeTexture);
        _gl.DrawArrays(PrimitiveType.Triangles, 0, 36);

        //desenha a skybox por último
        _gl.DepthFunc(DepthFunction.Lequal); // altera a função de profundidade para que o teste de profundidade passe quando os valores forem iguais ao conteúdo do buffer de profundidade
        _skyboxShader.Use();
        Matrix4x4 originalView = _camera.GetViewMatrix();
        view = new Matrix4x4( // remove a translação da matriz de visualização
            originalView.M11, originalView.M12, originalView.M13, 0.0f,
            originalView.M21, originalView.M22, originalView.M23, 0.0f,
            originalView.M31, originalView.M32, originalView.M33, 0.0f,
            0.0f,             0.0f,             0.0f,             1.0f
        );
        _skyboxShader.SetMat4("view", view);
        _skyboxShader.SetMat4("projection", projection);

        // cubo de skybox
        _gl.BindVertexArray(_skyboxVAO);
        _gl.ActiveTexture(TextureUnit.Texture0);
        _gl.BindTexture(TextureTarget.TextureCubeMap, _cubemapTexture);
        _gl.DrawArrays(PrimitiveType.Triangles, 0, 36);
        
        _gl.BindVertexArray(0);

        _gl.DepthFunc(DepthFunction.Less); // redefine a função de profundidade para o padrão
    }

    private static void OnClosing()
    {
        // opcional: desalocar todos os recursos assim que não forem mais necessários:
        // --------------------------------------------------
        _gl.DeleteVertexArrays(1, ref _cubeVAO);
        _gl.DeleteVertexArrays(1, ref _skyboxVAO);
        _gl.DeleteBuffers(1, ref _cubeVBO);
        _gl.DeleteBuffers(1, ref _skyboxVBO);
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
