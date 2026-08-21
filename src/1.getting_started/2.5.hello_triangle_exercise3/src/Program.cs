using Silk.NET.Input;
using Silk.NET.Maths;
using Silk.NET.OpenGL;
using Silk.NET.Windowing;

namespace LearnSilkNET.src;

public class Program
{
    private static IWindow _window = null!;
    private static GL _gl = null!;

    // configurações
    private const uint SCR_WIDTH = 800;
    private const uint SCR_HEIGHT = 600;

    private const string _vertexShaderSource =
    @"
        #version 330 core
        layout (location = 0) in vec3 aPos;

        void main()
        {
            gl_Position = vec4(aPos.x, aPos.y, aPos.z, 1.0);
        }
    ";

    private const string _fragmentShader1Source =
    @"
        #version 330 core
        out vec4 FragColor;

        void main()
        {
            FragColor = vec4(1.0f, 0.5f, 0.2f, 1.0f);
        } 
    ";

    private const string _fragmentShader2Source =
    @"
        #version 330 core
        out vec4 FragColor;

        void main()
        {
            FragColor = vec4(1.0f, 1.0f, 0.0f, 1.0f);
        } 
    ";

    private static uint _shaderProgramOrange;
    private static uint _shaderProgramYellow;

    private static uint[] _vertexArrayObjects = new uint[2];
    private static uint[] _vertexBufferObjects = new uint[2];

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

        for (int i = 0; i < input.Keyboards.Count; i++)
        {
            input.Keyboards[i].KeyDown += OnKeyDown;
        }

        _gl = _window.CreateOpenGL();

        // construir e compilar nosso programa de shader
        // --------------------------------------------------

        // desta vez, omitimos as verificações do log de compilação para facilitar a leitura (se você encontrar problemas, adicione as verificações de compilação; consulte os exemplos de código anteriores)
        uint vertexShader = _gl.CreateShader(ShaderType.VertexShader);

        uint fragmentShaderOrange = _gl.CreateShader(ShaderType.FragmentShader); // o primeiro shader de fragmento que gera a cor laranja
        uint fragmentShaderYellow = _gl.CreateShader(ShaderType.FragmentShader); // o segundo shader de fragmento que gera a cor amarela

        _shaderProgramOrange = _gl.CreateProgram();
        _shaderProgramYellow = _gl.CreateProgram();

        _gl.ShaderSource(vertexShader, _vertexShaderSource);
        _gl.CompileShader(vertexShader);

        _gl.ShaderSource(fragmentShaderOrange, _fragmentShader1Source);
        _gl.CompileShader(fragmentShaderOrange);

        _gl.ShaderSource(fragmentShaderYellow, _fragmentShader2Source);
        _gl.CompileShader(fragmentShaderYellow);

        // vincular o primeiro objeto de programa
        _gl.AttachShader(_shaderProgramOrange, vertexShader);
        _gl.AttachShader(_shaderProgramOrange, fragmentShaderOrange);
        _gl.LinkProgram(_shaderProgramOrange);

        // em seguida, vincule o segundo objeto de programa usando um shader de fragmento diferente (mas o mesmo shader de vértice)
        // isso é perfeitamente permitido, uma vez que as entradas e saídas de ambos os shaders — de vértice e de fragmento — são compatíveis.
        _gl.AttachShader(_shaderProgramYellow, vertexShader);
        _gl.AttachShader(_shaderProgramYellow, fragmentShaderYellow);
        _gl.LinkProgram(_shaderProgramYellow);

        // configurar dados de vértice (e buffer(s)) e configurar atributos de vértice
        // --------------------------------------------------
        float[] firstTriangle =
        {
            -0.9f,  -0.5f,   0.0f,
             0.0f,  -0.5f,   0.0f,
            -0.45f,  0.5f,   0.0f
        };
        
        float[] secondTriangle =
        {
             0.0f,  -0.5f,   0.0f,
             0.9f,  -0.5f,   0.0f,
             0.45f,  0.5f,   0.0f
        };

        _gl.GenVertexArrays(2, _vertexArrayObjects); // também podemos gerar múltiplos VAOs ou buffers ao mesmo tempo
        _gl.GenBuffers(2, _vertexBufferObjects);

        // configuração do primeiro triângulo
        // --------------------------------------------------
        _gl.BindVertexArray(_vertexArrayObjects[0]);

        _gl.BindBuffer(BufferTargetARB.ArrayBuffer, _vertexBufferObjects[0]);
        unsafe
        {
            fixed (float* buf = firstTriangle)
            {
                _gl.BufferData(BufferTargetARB.ArrayBuffer, (uint)(firstTriangle.Length * sizeof(float)), buf, BufferUsageARB.StaticDraw);
            }
        }

        unsafe
        {
            _gl.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, 3 * sizeof(float), (void*)0); // Os atributos de vértice permanecem os mesmos
        }
        _gl.EnableVertexAttribArray(0);
        
        // _gl.BindVertexArray(0); // não é necessário desfazer a vinculação, pois vinculamos diretamente um VAO diferente nas próximas linhas

        // configuração do segundo triângulo
        // --------------------------------------------------
        _gl.BindVertexArray(_vertexArrayObjects[1]); // observe que agora vinculamos a um VAO diferente

        _gl.BindBuffer(BufferTargetARB.ArrayBuffer, _vertexBufferObjects[1]);
        unsafe
        {
            fixed (float* buf = secondTriangle)
            {
                _gl.BufferData(BufferTargetARB.ArrayBuffer, (uint)(secondTriangle.Length * sizeof(float)), buf, BufferUsageARB.StaticDraw);
            }
        }

        unsafe
        {
            _gl.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, 3 * sizeof(float), (void*)0); // como os dados dos vértices estão compactados, também podemos especificar 0 como o stride do atributo de vértice para deixar o OpenGL determiná-lo
        }
        _gl.EnableVertexAttribArray(0);

        // _gl.BindVertexArray(0); // também não é estritamente necessário, mas cuidado com chamadas que possam afetar VAOs enquanto este estiver vinculado (como vincular *element buffer objects* ou habilitar/desabilitar atributos de vértice)

        // descomente esta chamada para desenhar polígonos em wireframe.
        // _gl.PolygonMode(TriangleFace.FrontAndBack, PolygonMode.Line);
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
        _gl.ClearColor(0.2f, 0.3f, 0.3f, 1.0f);
        _gl.Clear(ClearBufferMask.ColorBufferBit);

        // agora, ao desenhar o triângulo, usamos primeiro o shader de vértice e o shader de fragmento laranja do primeiro programa
        _gl.UseProgram(_shaderProgramOrange);

        // desenha o primeiro triângulo usando os dados do nosso primeiro VAO
        _gl.BindVertexArray(_vertexArrayObjects[0]);
        _gl.DrawArrays(PrimitiveType.Triangles, 0, 3); // esta chamada deve gerar um triângulo laranja

        // então, desenhamos o segundo triângulo usando os dados do segundo VAO
        // ao desenhar o segundo triângulo, queremos usar um programa de shader diferente; por isso, alternamos para o programa de shader que utiliza nosso shader de fragmento amarelo.
        _gl.UseProgram(_shaderProgramYellow);
        _gl.BindVertexArray(_vertexArrayObjects[1]);
        _gl.DrawArrays(PrimitiveType.Triangles, 0, 3); // esta chamada deve gerar um triângulo amarelo
    }

    private static void OnClosing()
    {
        // opcional: desalocar todos os recursos assim que não forem mais necessários:
        // --------------------------------------------------
        _gl.DeleteVertexArrays(2, _vertexArrayObjects);
        _gl.DeleteBuffers(2, _vertexBufferObjects);
        _gl.DeleteProgram(_shaderProgramOrange);
        _gl.DeleteProgram(_shaderProgramYellow);
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
