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

    private static Shader _shader = null!;

    private static uint _vertexArrayObject;
    private static uint _vertexBufferObject;

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

        _gl = _window.CreateOpenGL();

        // configurar estado global do OpenGL
        // --------------------------------------------------
        _gl.Enable(EnableCap.DepthTest);

        // construir e compilar nosso programa de shader
        // --------------------------------------------------
        _shader = new Shader(_gl, "src/geometry_shader.vs", "src/geometry_shader.fs", "src/geometry_shader.gs");

        // configurar dados de vértice (e buffer(s)) e configurar atributos de vértice
        // --------------------------------------------------
        float[] points =
        {
            // posições     // cores
            -0.5f, -0.5f,   1.0f, 0.0f, 0.0f, // inferior esquerdo
             0.5f, -0.5f,   0.0f, 1.0f, 0.0f, // inferior direito
             0.5f,  0.5f,   0.0f, 0.0f, 1.0f, // superior direito
            -0.5f,  0.5f,   1.0f, 1.0f, 0.0f  // superior esquerdo
        };

        // cube VAO
        _gl.GenVertexArrays(1, out _vertexArrayObject);
        _gl.GenBuffers(1, out _vertexBufferObject);

        _gl.BindVertexArray(_vertexArrayObject);

        _gl.BindBuffer(BufferTargetARB.ArrayBuffer, _vertexBufferObject);
        unsafe
        {
            fixed (float* buf = points)
            {
                _gl.BufferData(BufferTargetARB.ArrayBuffer, (uint)(points.Length * sizeof(float)), buf, BufferUsageARB.StaticDraw);
            }
        }

        _gl.EnableVertexAttribArray(0);
        unsafe
        {
            _gl.VertexAttribPointer(0, 2, VertexAttribPointerType.Float, false, 5 * sizeof(float), (void*)0);
        }

        _gl.EnableVertexAttribArray(1);
        unsafe
        {
            _gl.VertexAttribPointer(1, 3, VertexAttribPointerType.Float, false, 5 * sizeof(float), (void*)(2 * sizeof(float)));
        }

        _gl.BindVertexArray(0);
    }

    private static void OnResize(Vector2D<int> newSize)
    {
        FramebufferSizeCallback(newSize.X, newSize.Y);
    }

    private static void OnUpdate(double deltaTime)
    {
        
    }

    // loop de renderização
    // --------------------------------------------------
    private static void OnRender(double deltaTime)
    {
        // render
        // --------------------------------------------------
        _gl.ClearColor(0.1f, 0.1f, 0.1f, 1.0f);
        _gl.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

        // desenhar pontos
        _shader.Use();
        _gl.BindVertexArray(_vertexArrayObject);
        _gl.DrawArrays(PrimitiveType.Points, 0, 4);
    }

    private static void OnClosing()
    {
        // opcional: desalocar todos os recursos assim que não forem mais necessários:
        // --------------------------------------------------
        _gl.DeleteVertexArrays(1, ref _vertexArrayObject);
        _gl.DeleteBuffers(1, ref _vertexBufferObject);
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
