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

    // configurações
    private const uint SCR_WIDTH = 800;
    private const uint SCR_HEIGHT = 600;

    private static Shader _shader = null!;

    private static uint _instanceVBO;

    private static uint _quadVAO, _quadVBO;

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

        _gl = _window.CreateOpenGL();
        _glfw = Glfw.GetApi();

        // configurar estado global do OpenGL
        // --------------------------------------------------
        _gl.Enable(EnableCap.DepthTest);

        // construir e compilar nosso programa de shader
        // --------------------------------------------------
        _shader = new Shader(_gl, "src/instancing.vs", "src/instancing.fs");

        // gerar uma lista de 100 localizações de quad/vetores de translação
        // --------------------------------------------------
        Vector2[] translations = new Vector2[100];

        int index = 0;
        float offset = 0.1f;

        for (int y = -10; y < 10; y += 2)
        {
            for (int x = -10; x < 10; x += 2)
            {
                Vector2 translation;
                translation.X = (float)x / 10.0f + offset;
                translation.Y = (float)y / 10.0f + offset;

                translations[index++] = translation;
            }
        }

        // armazena dados da instância em um buffer de array
        // --------------------------------------------------
        _gl.GenBuffers(1, out _instanceVBO);
        _gl.BindBuffer(BufferTargetARB.ArrayBuffer, _instanceVBO);
        unsafe
        {
            fixed (Vector2* buf = translations)
            {
                _gl.BufferData(BufferTargetARB.ArrayBuffer, (uint)(sizeof(Vector2) * 100), buf, BufferUsageARB.StaticDraw);
            }
        }
        _gl.BindBuffer(BufferTargetARB.ArrayBuffer, 0);

        // configurar dados de vértice (e buffer(s)) e configurar atributos de vértice
        // --------------------------------------------------
        float[] quadVertices =
        {
            // posições       // cores
            -0.05f, -0.05f,   1.0f, 0.0f, 0.0f,
             0.05f, -0.05f,   0.0f, 1.0f, 0.0f,
             0.05f,  0.05f,   0.0f, 0.0f, 1.0f,
            -0.05f, -0.05f,   1.0f, 0.0f, 0.0f,
             0.05f,  0.05f,   0.0f, 0.0f, 1.0f,
            -0.05f,  0.05f,   1.0f, 1.0f, 0.0f
        };

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
            _gl.VertexAttribPointer(0, 2, VertexAttribPointerType.Float, false, 5 * sizeof(float), (void*)0);
        }

        _gl.EnableVertexAttribArray(1);
        unsafe
        {
            _gl.VertexAttribPointer(1, 3, VertexAttribPointerType.Float, false, 5 * sizeof(float), (void*)(2 * sizeof(float)));
        }

        // define também os dados da instância
        _gl.EnableVertexAttribArray(2);
        _gl.BindBuffer(BufferTargetARB.ArrayBuffer, _instanceVBO); // este atributo vem de um buffer de vértices diferente     
        unsafe
        {
            _gl.VertexAttribPointer(2, 2, VertexAttribPointerType.Float, false, 2 * sizeof(float), (void*)0);
        }
        _gl.BindBuffer(BufferTargetARB.ArrayBuffer, 0);
        _gl.VertexAttribDivisor(2, 1); // informe ao OpenGL que este é um atributo de vértice instanciado.
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

        // desenha 100 quads instanciados
        _shader.Use();
        _gl.BindVertexArray(_quadVAO);
        _gl.DrawArraysInstanced(PrimitiveType.Triangles, 0, 6, 100); // 100 triângulos de 6 vértices cada
        _gl.BindVertexArray(0);
    }

    private static void OnClosing()
    {
        // opcional: desalocar todos os recursos assim que não forem mais necessários:
        // --------------------------------------------------
        _gl.DeleteVertexArrays(1, ref _quadVAO);
        _gl.DeleteBuffers(1, ref _quadVBO);
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
