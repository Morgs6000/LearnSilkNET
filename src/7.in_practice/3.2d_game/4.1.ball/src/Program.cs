/**************************************************
** Este código faz parte do Breakout.
**
** O Breakout é um software livre: você pode redistribuí-lo e/ou modificá-lo
** sob os termos da licença CC BY 4.0, conforme publicada pela
** Creative Commons, seja a versão 4 da Licença ou (a seu
** critério) qualquer versão posterior.
**************************************************/

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

    // A largura da tela
    private const uint SCREEN_WIDTH = 800;

    // A altura da tela
    private const uint SCREEN_HEIGHT = 600;

    private static Game Breakout = null!;

    // variáveis ​​de deltaTime
    // --------------------------------------------------
    private static float _deltaTime = 0.0f;
    private static float _lastFrame = 0.0f;

    private static void Main(string[] args)
    {
        WindowOptions options = WindowOptions.Default;

        options.Size = new Vector2D<int>((int)SCREEN_WIDTH, (int)SCREEN_HEIGHT);
        options.Title = "Breakout";
        options.IsVisible = false;

        _window = Window.Create(options);

        _window.Load += OnLoad;
        _window.Resize += OnResize;
        _window.Update += OnUpdate;
        _window.Render += OnRender;
        _window.Closing += OnClosing;

        _window.Run();
    }

    private static void OnLoad()
    {
        if (OperatingSystem.IsWindows())
        {
            _window.Center();
        }
        _window.IsVisible = true;

        IInputContext input = _window.CreateInput();

        for (int i = 0; i < input.Keyboards.Count; i++)
        {
            input.Keyboards[i].KeyDown += OnKeyDown;
            input.Keyboards[i].KeyUp += OnKeyUp;
        }        

        _gl = _window.CreateOpenGL();

        // Configuração do OpenGL
        // --------------------------------------------------
        _gl.Viewport(0, 0, SCREEN_WIDTH, SCREEN_HEIGHT);
        _gl.Enable(EnableCap.Blend);
        _gl.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);

        // inicializar o jogo
        // --------------------------------------------------
        Breakout = new Game(_gl, SCREEN_WIDTH, SCREEN_HEIGHT);
        Breakout.Init();
    }

    private static void OnResize(Vector2D<int> newSize)
    {
        FramebufferSizeCallback(newSize.X, newSize.Y);
    }

    private static void OnUpdate(double deltaTime)
    {
        // calcular o delta de tempo
        // --------------------------------------------------
        float currentFrame = (float)Glfw.GetApi().GetTime();
        _deltaTime = currentFrame - _lastFrame;
        _lastFrame = currentFrame;

        // gerenciar a entrada do usuário
        // --------------------------------------------------
        Breakout.ProceessInput(_deltaTime);

        // atualizar estado do jogo
        // --------------------------------------------------
        Breakout.Update(_deltaTime);
    }

    private static void OnRender(double deltaTime)
    {
        // render
        // --------------------------------------------------
        _gl.ClearColor(0.0f, 0.0f, 0.0f, 1.0f);
        _gl.Clear(ClearBufferMask.ColorBufferBit);

        Breakout.Render();
    }

    private static void OnClosing()
    {
        // exclui todos os recursos carregados usando o gerenciador de recursos
        // --------------------------------------------------
        ResourceManager.Clear(_gl);

        Breakout.Dispose();
    }

    private static void OnKeyDown(IKeyboard keyboard, Key key, int keyCode)
    {
        // quando o usuário pressiona a tecla Esc, definimos a propriedade WindowShouldClose como true, fechando o aplicativo
        if (key == Key.Escape)
        {
            _window.Close();
        }

        if (key >= (Key)0 && key < (Key)1024)
        {
            Breakout.Keys[(int)key] = true;
        }
    }

    private static void OnKeyUp(IKeyboard keyboard, Key key, int keyCode)
    {
        if (key >= (Key)0 && key < (Key)1024)
        {
            Breakout.Keys[(int)key] = false;
        }
    }

    private static void FramebufferSizeCallback(int width, int height)
    {
        // certifique-se de que a viewport corresponda às novas dimensões da janela; observe que a largura e
        // a altura serão significativamente maiores do que as especificadas em telas Retina.
        _gl.Viewport(0, 0, (uint)width, (uint)height);
    }
}
