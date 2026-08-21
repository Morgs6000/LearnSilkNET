using System.Numerics;
using System.Runtime.CompilerServices;
using Silk.NET.GLFW;
using Silk.NET.Input;
using Silk.NET.Maths;
using Silk.NET.OpenGL;
using Silk.NET.Windowing;
using StbImageSharp;
using ErrorCode = Silk.NET.OpenGL.ErrorCode;

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

    private static ErrorCode CheckError([CallerFilePath]string file = "", [CallerLineNumber]int line = 0)
    {
        ErrorCode errorCode;

        while((errorCode = (ErrorCode)_gl.GetError()) != ErrorCode.NoError)
        {
            string error = string.Empty;

            switch (errorCode)
            {
                case ErrorCode.InvalidEnum:
                    error = "INVALID_ENUM";
                    break;
                case ErrorCode.InvalidValue:
                    error = "INVALID_VALUE";
                    break;
                case ErrorCode.InvalidOperation:
                    error = "INVALID_OPERATION";
                    break;
                case ErrorCode.StackOverflow:
                    error = "STACK_OVERFLOW";
                    break;
                case ErrorCode.StackUnderflow:
                    error = "STACK_UNDERFLOW";
                    break;
                case ErrorCode.OutOfMemory:
                    error = "OUT_OF_MEMORY";
                    break;
                case ErrorCode.InvalidFramebufferOperation:
                    error = "INVALID_FRAMEBUFFER_OPERATION";
                    break;
            }

            Console.WriteLine(error + " | " + file + " (" + line + ")");
        }

        return errorCode;
    }

    private static void DebugOutput(
        GLEnum source, 
        GLEnum type, 
        int id, 
        GLEnum severity, 
        int length, 
        nint message, 
        nint userParam
    )
    {
        if (id == 131169 || id == 131185 || id == 131218 || id == 131204)
        {
            return; // ignore estes códigos de erro não significativos
        }

        Console.WriteLine("---------------");
        Console.WriteLine("Debug message (" + id + "): " + message);

        switch (source)
        {
            case GLEnum.DebugSourceApi:
                Console.Write("Source: API");
                break;
            case GLEnum.DebugSourceWindowSystem:
                Console.Write("Source: Window System");
                break;
            case GLEnum.DebugSourceShaderCompiler:
                Console.Write("Source: Shader Compiler");
                break;
            case GLEnum.DebugSourceThirdParty:
                Console.Write("Source: Third Party");
                break;
            case GLEnum.DebugSourceApplication:
                Console.Write("Source: Application");
                break;
            case GLEnum.DebugSourceOther:
                Console.Write("Source: Other");
                break;
        }

        Console.WriteLine();

        switch (type)
        {
            case GLEnum.DebugTypeError:
                Console.Write("Type: Error");
                break;
            case GLEnum.DebugTypeDeprecatedBehavior:
                Console.Write("Type: Deprecated Behaviour");
                break;
            case GLEnum.DebugTypeUndefinedBehavior:
                Console.Write("Type: Undefined Behaviour");
                break;
            case GLEnum.DebugTypePortability:
                Console.Write("Type: Portability");
                break;
            case GLEnum.DebugTypePerformance:
                Console.Write("Type: Performance");
                break;
            case GLEnum.DebugTypeMarker:
                Console.Write("Type: Marker");
                break;
            case GLEnum.DebugTypePushGroup:
                Console.Write("Type: Push Group");
                break;
            case GLEnum.DebugTypePopGroup:
                Console.Write("Type: Pop Group");
                break;
            case GLEnum.DebugTypeOther:
                Console.Write("Type: Other");
                break;
        }

        Console.WriteLine();

        switch (severity)
        {
            case GLEnum.DebugSeverityHigh:
                Console.Write("Severity: high");
                break;
            case GLEnum.DebugSeverityMedium:
                Console.Write("Severity: medium");
                break;
            case GLEnum.DebugSeverityLow:
                Console.Write("Severity: low");
                break;
            case GLEnum.DebugSeverityNotification:
                Console.Write("Severity: notification");
                break;
        }

        Console.WriteLine();

        Console.WriteLine();
    }

    private static Shader _shader = null!;

    private static uint _cubeVAO, _cubeVBO;

    private static uint _texture;

    private static void Main(string[] args)
    {
        // criação da janela glfw
        // --------------------------------------------------
        WindowOptions options = WindowOptions.Default;

        options.Size = new Vector2D<int>((int)SCR_WIDTH, (int)SCR_HEIGHT);
        options.Title = "Learn Silk.NET";
        options.IsVisible = false;
        options.VSync = false;
        options.API = WindowOptions.Default.API with
        {
            Flags = ContextFlags.Debug
        };

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

        _gl = _window.CreateOpenGL();
        _glfw = Glfw.GetApi();

        // instruir o GLFW a capturar o mouse
        _primaryMouse!.Cursor.CursorMode = CursorMode.Raw;

        // habilita o contexto de depuração do OpenGL se o contexto permitir um contexto de depuração
        int flags;
        _gl.GetInteger(GetPName.ContextFlags, out flags);

        if ((flags & (int)ContextFlags.Debug) != 0)
        {
            _gl.Enable(EnableCap.DebugOutput);
            _gl.Enable(EnableCap.DebugOutputSynchronous); // garante que os erros sejam exibidos de forma síncrona
            unsafe
            {
                _gl.DebugMessageCallback(DebugOutput, null);
                _gl.DebugMessageControl(DebugSource.DontCare, DebugType.DontCare, DebugSeverity.DontCare, 0, null, true);
            }            
        }

        // configurar estado global do OpenGL
        // --------------------------------------------------
        _gl.Enable(EnableCap.DepthTest);
        _gl.Enable(EnableCap.CullFace);

        // construir e compilar nosso programa de shader
        // --------------------------------------------------
        _shader = new Shader(_gl, "src/debugging.vs", "src/debugging.fs");

        // configura dados de vértice (e buffer(s)) e configura atributos de vértice
        // --------------------------------------------------
        float[] vertices =
        {
            // posições            // coordendas de textura

            // face esquerda
            -0.5f, -0.5f, -0.5f,   0.0f, 0.0f,
            -0.5f, -0.5f,  0.5f,   1.0f, 0.0f,
            -0.5f,  0.5f,  0.5f,   1.0f, 1.0f,
            -0.5f, -0.5f, -0.5f,   0.0f, 0.0f,
            -0.5f,  0.5f,  0.5f,   1.0f, 1.0f,
            -0.5f,  0.5f, -0.5f,   0.0f, 1.0f,
            
            // face direita
             0.5f, -0.5f,  0.5f,   0.0f, 0.0f,
             0.5f, -0.5f, -0.5f,   1.0f, 0.0f,
             0.5f,  0.5f, -0.5f,   1.0f, 1.0f,
             0.5f, -0.5f,  0.5f,   0.0f, 0.0f,
             0.5f,  0.5f, -0.5f,   1.0f, 1.0f,
             0.5f,  0.5f,  0.5f,   0.0f, 1.0f,
            
            // face inferior
            -0.5f, -0.5f, -0.5f,   0.0f, 0.0f,
             0.5f, -0.5f, -0.5f,   1.0f, 0.0f,
             0.5f, -0.5f,  0.5f,   1.0f, 1.0f,
            -0.5f, -0.5f, -0.5f,   0.0f, 0.0f,
             0.5f, -0.5f,  0.5f,   1.0f, 1.0f,
            -0.5f, -0.5f,  0.5f,   0.0f, 1.0f,
            
            // face superior
            -0.5f,  0.5f,  0.5f,   0.0f, 0.0f,
             0.5f,  0.5f,  0.5f,   1.0f, 0.0f,
             0.5f,  0.5f, -0.5f,   1.0f, 1.0f,
            -0.5f,  0.5f,  0.5f,   0.0f, 0.0f,
             0.5f,  0.5f, -0.5f,   1.0f, 1.0f,
            -0.5f,  0.5f, -0.5f,   0.0f, 1.0f,
            
            // face posterior
             0.5f, -0.5f, -0.5f,   0.0f, 0.0f,
            -0.5f, -0.5f, -0.5f,   1.0f, 0.0f,
            -0.5f,  0.5f, -0.5f,   1.0f, 1.0f,
             0.5f, -0.5f, -0.5f,   0.0f, 0.0f,
            -0.5f,  0.5f, -0.5f,   1.0f, 1.0f,
             0.5f,  0.5f, -0.5f,   0.0f, 1.0f,
            
            // face frontal
            -0.5f, -0.5f,  0.5f,   0.0f, 0.0f,
             0.5f, -0.5f,  0.5f,   1.0f, 0.0f,
             0.5f,  0.5f,  0.5f,   1.0f, 1.0f,
            -0.5f, -0.5f,  0.5f,   0.0f, 0.0f,
             0.5f,  0.5f,  0.5f,   1.0f, 1.0f,
            -0.5f,  0.5f,  0.5f,   0.0f, 1.0f
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
            _gl.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, 5 * sizeof(float), (void*)0);
        }

        _gl.EnableVertexAttribArray(1);
        unsafe
        {
            _gl.VertexAttribPointer(1, 2, VertexAttribPointerType.Float, false, 5 * sizeof(float), (void*)(3 * sizeof(float)));
        }

        _gl.BindBuffer(BufferTargetARB.ArrayBuffer, 0);
        _gl.BindVertexArray(0);

        // carregar textura de cubo
        _gl.GenTextures(1, out _texture);
        _gl.BindTexture(TextureTarget.Texture2D, _texture);

        int width, height;
        byte[] data;

        using (FileStream stream = File.OpenRead("res/textures/wood.png"))
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
                    _gl.TexImage2D((TextureTarget)FramebufferTarget.Framebuffer, 0, InternalFormat.Rgb, (uint)width, (uint)height, 0, PixelFormat.Rgb, PixelType.UnsignedByte, ptr);
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
            Console.WriteLine("Falha ao carregar a textura");
        }

        // configurar a matriz de projeção
        Matrix4x4 projection = Matrix4x4.CreatePerspectiveFieldOfView(
            fieldOfView:       MathHelper.DegreesToRadians(45.0f), 
            aspectRatio:       (float)SCR_WIDTH / (float)SCR_HEIGHT, 
            nearPlaneDistance: 0.1f, 
            farPlaneDistance:  10.0f
        );
        unsafe
        {
            _gl.UniformMatrix4(_gl.GetUniformLocation(_shader.ID, "projection"), 1, false, (float*)&projection);
        }
        _gl.Uniform1(_gl.GetUniformLocation(_shader.ID, "tex"), 0);
    }

    private static void OnResize(Vector2D<int> newSize)
    {
        FramebufferSizeCallback(newSize.X, newSize.Y);
    }

    private static void OnUpdate(double deltaTime)
    {
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
        _gl.ClearColor(0.0f, 0.0f, 0.0f, 1.0f);
        _gl.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

        _shader.Use();

        float rotationSpeed = 10.0f;
        float angle = (float)Glfw.GetApi().GetTime() * rotationSpeed;

        Matrix4x4 model = Matrix4x4.Identity;
        model *= Matrix4x4.CreateFromAxisAngle(Vector3.Normalize(new Vector3(1.0f, 1.0f, 1.0f)), MathHelper.DegreesToRadians(angle));
        model *= Matrix4x4.CreateTranslation(new Vector3(0.0f, 0.0f, -2.5f));
        unsafe
        {
            _gl.UniformMatrix4(_gl.GetUniformLocation(_shader.ID, "model"), 1, false, (float*)&model);
        }

        _gl.BindTexture(TextureTarget.Texture2D, _texture);
        _gl.BindVertexArray(_cubeVAO);
            _gl.DrawArrays(PrimitiveType.Triangles, 0, 36);
        _gl.BindVertexArray(0);
    }

    private static void OnClosing()
    {
        
    }

    // renderQuad() renderiza um quadrilátero XY de 1x1 em NDC
    // --------------------------------------------------
    private uint _quadVAO = 0;
    private uint _quadVBO;

    private void RenderQuad()
    {
        if (_quadVAO == 0)
        {
            float[] quadVertices =
            {
                // posições           // coordenadas de textura
                -1.0f,  1.0f, 0.0f,   0.0f, 1.0f,
                -1.0f, -1.0f, 0.0f,   0.0f, 0.0f,
                 1.0f,  1.0f, 0.0f,   1.0f, 1.0f,
                 1.0f, -1.0f, 0.0f,   1.0f, 0.0f
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
        
    }

    // glfw: sempre que o tamanho da janela é alterado (pelo SO ou por redimensionamento do usuário), esta função de callback é executada
    // --------------------------------------------------
    private static void FramebufferSizeCallback(int width, int height)
    {
        // certifique-se de que a viewport corresponda às novas dimensões da janela; observe que a largura e
        // a altura serão significativamente maiores do que as especificadas em telas Retina.
        _gl.Viewport(0, 0, (uint)width, (uint)height);
    }
}
