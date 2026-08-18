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

    // configurações
    private const int SCR_WIDTH = 800;
    private const int SCR_HEIGHT = 600;

    private const string _vertexShaderSource =
    @"
        #version 330 core
        layout (location = 0) in vec3 aPos;

        void main()
        {
            gl_Position = vec4(aPos.x, aPos.y, aPos.z, 1.0);
        }
    ";

    private const string _fragmentShaderSource =
    @"
        #version 330 core
        out vec4 FragColor;

        void main()
        {
            FragColor = vec4(1.0f, 0.5f, 0.2f, 1.0f);
        } 
    ";

    private static uint _shaderProgram;

    private static uint _vertexArrayObject;
    private static uint _vertexBufferObject;
    private static uint _elementBufferObject;

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

        _gl = _window.CreateOpenGL();

        // construir e compilar nosso programa de shader
        // --------------------------------------------------

        // shader de vértice
        uint vertexShader = _gl.CreateShader(ShaderType.VertexShader);
        _gl.ShaderSource(vertexShader, _vertexShaderSource);
        _gl.CompileShader(vertexShader);

        // verificar erros de compilação de shader
        int success;
        string infoLog;

        _gl.GetShader(vertexShader, ShaderParameterName.CompileStatus, out success);
        if (success == 0)
        {
            _gl.GetShaderInfoLog(vertexShader, out infoLog);
            Console.WriteLine("ERROR::SHADER::VERTEX::COMPILATION_FAILED\n" + infoLog);
        }

        // shader de fragmento
        uint fragmentShader = _gl.CreateShader(ShaderType.FragmentShader);
        _gl.ShaderSource(fragmentShader, _fragmentShaderSource);
        _gl.CompileShader(fragmentShader);

        // verificar erros de compilação de shader
        _gl.GetShader(fragmentShader, ShaderParameterName.CompileStatus, out success);
        if (success == 0)
        {
            _gl.GetShaderInfoLog(fragmentShader, out infoLog);
            Console.WriteLine("ERROR::SHADER::FRAGMENT::COMPILATION_FAILED\n" + infoLog);
        }

        // vincular shaders
        _shaderProgram = _gl.CreateProgram();
        _gl.AttachShader(_shaderProgram, vertexShader);
        _gl.AttachShader(_shaderProgram, fragmentShader);
        _gl.LinkProgram(_shaderProgram);

        // verificar erros de vinculação
        _gl.GetProgram(_shaderProgram, ProgramPropertyARB.LinkStatus, out success);
        if (success == 0)
        {
            _gl.GetProgramInfoLog(_shaderProgram, out infoLog);
            Console.WriteLine("ERROR::SHADER::PROGRAM::LINKING_FAILED\n" + infoLog);
        }

        _gl.DeleteShader(vertexShader);
        _gl.DeleteShader(fragmentShader);

        // configurar dados de vértice (e buffer(s)) e configurar atributos de vértice
        // --------------------------------------------------
        float[] vertices =
        {
            -0.5f, -0.5f,  0.0f, // inferior esquerdo
             0.5f, -0.5f,  0.0f, // inferior direito
             0.5f,  0.5f,  0.0f, // superior direito
            -0.5f,  0.5f,  0.0f  // superior esquerdo
        };

        uint[] indices = // observe que começamos do 0!
        {
            0, 1, 2, // primeiro triângulo
            0, 2, 3  // segundo triângulo
        };

        _gl.GenVertexArrays(1, out _vertexArrayObject);
        _gl.GenBuffers(1, out _vertexBufferObject);
        _gl.GenBuffers(1, out _elementBufferObject);

        // primeiro vincule o Vertex Array Object, depois vincule e configure o(s) buffer(s) de vértices e, em seguida, configure o(s) atributo(s) de vértice.
        _gl.BindVertexArray(_vertexArrayObject);

        _gl.BindBuffer(BufferTargetARB.ArrayBuffer, _vertexBufferObject);
        unsafe
        {
            fixed (float* buf = vertices)
            {
                _gl.BufferData(BufferTargetARB.ArrayBuffer, (uint)(vertices.Length * sizeof(float)), buf, BufferUsageARB.StaticDraw);
            }
        }

        _gl.BindBuffer(BufferTargetARB.ElementArrayBuffer, _elementBufferObject);
        unsafe
        {
            fixed (uint* buf = indices)
            {
                _gl.BufferData(BufferTargetARB.ElementArrayBuffer, (uint)(indices.Length * sizeof(uint)), buf, BufferUsageARB.StaticDraw);
            }
        }

        unsafe
        {
            _gl.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, 3 * sizeof(float), (void*)0);
        }
        _gl.EnableVertexAttribArray(0);

        // observe que isso é permitido; a chamada para glVertexAttribPointer registrou o VBO como o objeto de buffer de vértices vinculado ao atributo de vértice, portanto, podemos desvinculá-lo com segurança logo em seguida
        _gl.BindBuffer(BufferTargetARB.ArrayBuffer, 0);

        // lembre-se: NÃO desvincule o EBO enquanto um VAO estiver ativo, pois o objeto de buffer de elementos vinculado ESTÁ armazenado no VAO; mantenha o EBO vinculado.
        // _gl.BindBuffer(BufferTargetARB.ElementArrayBuffer, 0);

        // Você pode desvincular o VAO posteriormente para que outras chamadas de VAO não modifiquem acidentalmente este VAO, mas isso raramente acontece. Modificar outros
        // VAOs exige uma chamada para glBindVertexArray de qualquer forma, então geralmente não desvinculamos VAOs (nem VBOs) quando não é diretamente necessário.
        _gl.BindVertexArray(0);

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

        // desenhar nosso primeiro triângulo
        _gl.UseProgram(_shaderProgram);
        _gl.BindVertexArray(_vertexArrayObject); // como temos apenas um único VAO, não há necessidade de vinculá-lo todas as vezes, mas faremos isso para manter as coisas um pouco mais organizadas
        // _gl.DrawArrays(PrimitiveType.Triangles, 0, 3);
        unsafe
        {
            _gl.DrawElements(PrimitiveType.Triangles, 6, DrawElementsType.UnsignedInt, (void*)0);
        }
        // _gl.BindVertexArray(0); // não é necessário desvinculá-lo toda vez
    }

    private static void OnClosing()
    {
        // opcional: desalocar todos os recursos assim que não forem mais necessários:
        // --------------------------------------------------
        _gl.DeleteVertexArrays(1, ref _vertexArrayObject);
        _gl.DeleteBuffers(1, ref _vertexBufferObject);
        _gl.DeleteBuffers(1, ref _elementBufferObject);
        _gl.DeleteProgram(_shaderProgram);
    }

    // processar toda a entrada: consultar a GLFW para saber se teclas relevantes foram pressionadas ou liberadas neste quadro e reagir de acordo
    // --------------------------------------------------
    private static void ProcessInput()
    {
        if (_keyboard.IsKeyPressed(Key.Escape))
        {
            _window.Close();
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
}
