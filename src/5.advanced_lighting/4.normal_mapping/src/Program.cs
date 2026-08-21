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

    private static Shader _shader = null!;

    private static uint _diffuseMap;
    private static uint _normalMap;

    // informações de iluminação
    // --------------------------------------------------
    private static Vector3 _lightPos = new Vector3(0.5f, 1.0f, 0.3f);

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
        _shader = new Shader(_gl, "src/normal_mapping.vs", "src/normal_mapping.fs");

        // carregar texturas
        // --------------------------------------------------
        _diffuseMap = LoadTexture("res/textures/brickwall.jpg");
        _normalMap = LoadTexture("res/textures/brickwall_normal.jpg");

        // configuração do shader
        // --------------------------------------------------
        _shader.Use();
        _shader.SetInt("diffuseMap", 0);
        _shader.SetInt("normalMap", 1);
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

        // configurar matrizes de visualização/projeção
        Matrix4x4 projection = Matrix4x4.CreatePerspectiveFieldOfView(
            fieldOfView:       MathHelper.DegreesToRadians(_camera.Zoom), 
            aspectRatio:       (float)SCR_WIDTH / (float)SCR_HEIGHT, 
            nearPlaneDistance: 0.1f, 
            farPlaneDistance:  100.0f
        );
        Matrix4x4 view = _camera.GetViewMatrix();

        _shader.Use();
        _shader.SetMat4("projection", projection);
        _shader.SetMat4("view", view);

        // renderizar quadrilátero mapeado por normais
        Matrix4x4 model = Matrix4x4.Identity;
        model *= Matrix4x4.CreateFromAxisAngle(Vector3.Normalize(new Vector3(1.0f, 0.0f, 1.0f)), MathHelper.DegreesToRadians((float)_glfw.GetTime() * -10.0f)); // rotaciona o quad para exibir o normal mapping a partir de múltiplas direções
        _shader.SetMat4("model", model);

        _shader.SetVec3("viewPos", _camera.Position);
        _shader.SetVec3("lightPos", _lightPos);

        _gl.ActiveTexture(TextureUnit.Texture0);
        _gl.BindTexture(TextureTarget.Texture2D, _diffuseMap);

        _gl.ActiveTexture(TextureUnit.Texture1);
        _gl.BindTexture(TextureTarget.Texture2D, _normalMap);

        RenderQuad();

        // renderiza a fonte de luz (simplesmente renderiza novamente um plano menor na posição da luz para depuração/visualização)
        model = Matrix4x4.Identity;
        model *= Matrix4x4.CreateScale(new Vector3(0.1f));
        model *= Matrix4x4.CreateTranslation(_lightPos);
        _shader.SetMat4("model", model);

        RenderQuad();
    }

    private static void OnClosing()
    {
        
    }

    // renderiza um quad 1x1 em NDC com vetores tangentes calculados manualmente
    // --------------------------------------------------
    private static uint _quadVAO = 0;
    private static uint _quadVBO;

    private static void RenderQuad()
    {
        if (_quadVAO == 0)
        {
            // posições
            Vector3 pos1 = new Vector3(-1.0f,  1.0f,  0.0f);
            Vector3 pos2 = new Vector3(-1.0f, -1.0f,  0.0f);
            Vector3 pos3 = new Vector3( 1.0f, -1.0f,  0.0f);
            Vector3 pos4 = new Vector3( 1.0f,  1.0f,  0.0f);

            // coordenadas de textura
            Vector2 uv1 = new Vector2(0.0f, 1.0f);
            Vector2 uv2 = new Vector2(0.0f, 0.0f);
            Vector2 uv3 = new Vector2(1.0f, 0.0f);
            Vector2 uv4 = new Vector2(1.0f, 1.0f);

            // vetor normal
            Vector3 nm = new Vector3(0.0f, 0.0f, 1.0f);

            // calcular vetores tangente/bitangente de ambos os triângulos
            Vector3 tangent1, bitangent1;
            Vector3 tangent2, bitangent2;

            // triângulo 1
            // --------------------------------------------------
            Vector3 edge1 = pos2 - pos1;
            Vector3 edge2 = pos3 - pos1;
            Vector2 deltaUV1 = uv2 - uv1;
            Vector2 deltaUV2 = uv3 - uv1;

            float f = 1.0f / (deltaUV1.X * deltaUV2.Y - deltaUV2.X * deltaUV1.Y);

            tangent1.X = f * (deltaUV2.Y * edge1.X - deltaUV1.Y * edge2.X);
            tangent1.Y = f * (deltaUV2.Y * edge1.Y - deltaUV1.Y * edge2.Y);
            tangent1.Z = f * (deltaUV2.Y * edge1.Z - deltaUV1.Y * edge2.Z);

            bitangent1.X = f * (-deltaUV2.X * edge1.X + deltaUV1.X * edge2.X);
            bitangent1.Y = f * (-deltaUV2.X * edge1.Y + deltaUV1.X * edge2.Y);
            bitangent1.Z = f * (-deltaUV2.X * edge1.Z + deltaUV1.X * edge2.Z);

            // triângulo 2
            // --------------------------------------------------
            edge1 = pos3 - pos1;
            edge2 = pos4 - pos1;
            deltaUV1 = uv3 - uv1;
            deltaUV2 = uv4 - uv1;

            f = 1.0f / (deltaUV1.X * deltaUV2.Y - deltaUV2.X * deltaUV1.Y);

            tangent2.X = f * (deltaUV2.Y * edge1.X - deltaUV1.Y * edge2.X);
            tangent2.Y = f * (deltaUV2.Y * edge1.Y - deltaUV1.Y * edge2.Y);
            tangent2.Z = f * (deltaUV2.Y * edge1.Z - deltaUV1.Y * edge2.Z);

            bitangent2.X = f * (-deltaUV2.X * edge1.X + deltaUV1.X * edge2.X);
            bitangent2.Y = f * (-deltaUV2.X * edge1.Y + deltaUV1.X * edge2.Y);
            bitangent2.Z = f * (-deltaUV2.X * edge1.Z + deltaUV1.X * edge2.Z);

            float[] quadVertices =
            {
                // positions            // normal         // texcoords  // tangent                          // bitangent
                pos1.X, pos1.Y, pos1.Z, nm.X, nm.Y, nm.Z, uv1.X, uv1.Y, tangent1.X, tangent1.Y, tangent1.Z, bitangent1.X, bitangent1.Y, bitangent1.Z,
                pos2.X, pos2.Y, pos2.Z, nm.X, nm.Y, nm.Z, uv2.X, uv2.Y, tangent1.X, tangent1.Y, tangent1.Z, bitangent1.X, bitangent1.Y, bitangent1.Z,
                pos3.X, pos3.Y, pos3.Z, nm.X, nm.Y, nm.Z, uv3.X, uv3.Y, tangent1.X, tangent1.Y, tangent1.Z, bitangent1.X, bitangent1.Y, bitangent1.Z,

                pos1.X, pos1.Y, pos1.Z, nm.X, nm.Y, nm.Z, uv1.X, uv1.Y, tangent2.X, tangent2.Y, tangent2.Z, bitangent2.X, bitangent2.Y, bitangent2.Z,
                pos3.X, pos3.Y, pos3.Z, nm.X, nm.Y, nm.Z, uv3.X, uv3.Y, tangent2.X, tangent2.Y, tangent2.Z, bitangent2.X, bitangent2.Y, bitangent2.Z,
                pos4.X, pos4.Y, pos4.Z, nm.X, nm.Y, nm.Z, uv4.X, uv4.Y, tangent2.X, tangent2.Y, tangent2.Z, bitangent2.X, bitangent2.Y, bitangent2.Z
            };

            // configurar o VAO do plano
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
            _gl.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, 14 * sizeof(float), 0);

            _gl.EnableVertexAttribArray(1);
            _gl.VertexAttribPointer(1, 3, VertexAttribPointerType.Float, false, 14 * sizeof(float), 3 * sizeof(float));

            _gl.EnableVertexAttribArray(2);
            _gl.VertexAttribPointer(2, 2, VertexAttribPointerType.Float, false, 14 * sizeof(float), 6 * sizeof(float));

            _gl.EnableVertexAttribArray(3);
            _gl.VertexAttribPointer(3, 3, VertexAttribPointerType.Float, false, 14 * sizeof(float), 8 * sizeof(float));

            _gl.EnableVertexAttribArray(4);
            _gl.VertexAttribPointer(4, 3, VertexAttribPointerType.Float, false, 14 * sizeof(float), 11 * sizeof(float));
        }

        _gl.BindVertexArray(_quadVAO);
        _gl.DrawArrays(PrimitiveType.Triangles, 0, 6);
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
