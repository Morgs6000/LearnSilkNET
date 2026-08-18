using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using FreeTypeSharp;
using Silk.NET.GLFW;
using Silk.NET.Input;
using Silk.NET.Maths;
using Silk.NET.OpenGL;
using Silk.NET.Windowing;
using static FreeTypeSharp.FT;
using static FreeTypeSharp.FT_LOAD;

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

    // Armazena todas as informações de estado relevantes para um caractere, conforme carregado usando o FreeType.
    private struct Character
    {
        public uint TextureID;  // Identificador da textura do glifo
        public Vector2 Size;    // Tamanho do glifo
        public Vector2 Bearing; // Deslocamento da linha de base até a esquerda/topo do glifo
        public uint Advance;    // Deslocamento horizontal para avançar para o próximo glifo
    };

    private static Dictionary<uint, Character> _characters = [];
    private static uint _vertexArrayObject;
    private static uint _vertexBufferObject;

    private static Shader _shader = null!;

    private static uint _texture;

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
        _gl.Enable(EnableCap.CullFace);
        _gl.Enable(EnableCap.Blend);
        _gl.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);

        // construir e compilar nosso programa de shader
        // --------------------------------------------------
        _shader = new Shader(_gl, "src/text.vs", "src/text.fs");

        Matrix4x4 projection = Matrix4x4.CreateOrthographicOffCenter(
            left:        0.0f, 
            right:       (float)SCR_WIDTH, 
            bottom:      0.0f, 
            top:         (float)SCR_HEIGHT, 
            zNearPlane: -1.0f, 
            zFarPlane:   1.0f
        );

        _shader.Use();
        unsafe
        {
            _gl.UniformMatrix4(_gl.GetUniformLocation(_shader.ID, "projection"), 1, false, (float*)&projection);
        }

        // FreeType
        // --------------------------------------------------
        unsafe
        {
            FT_LibraryRec_* ft;

            // Todas as funções retornam um valor diferente de 0 sempre que ocorre um erro
            if (FT_Init_FreeType(&ft) != 0)
            {
                Console.WriteLine("ERROR::FREETYPE: Could not init FreeType Library");
                return;
            }

            // encontrar caminho para a fonte
            string font_name = "res/fonts/Antonio-Bold.ttf";

            if (font_name == string.Empty)
            {
                Console.WriteLine("ERROR::FREETYPE: Failed to load font_name");
                return;
            }

            // carregar fonte como face
            FT_FaceRec_* face;

            if (FT_New_Face(ft, (byte*)Marshal.StringToHGlobalAnsi(font_name), 0, &face) != 0)
            {
                Console.WriteLine("ERROR::FREETYPE: Failed to load font");
                return;
            }
            else
            {
                // Defina o tamanho para carregar os glifos como
                FT_Set_Pixel_Sizes(face, 0, 48);

                // desativar a restrição de alinhamento de bytes
                _gl.PixelStore(PixelStoreParameter.UnpackAlignment, 1);

                // carrega os primeiros 128 caracteres do conjunto ASCII
                for (uint c = 0; c < 128; c++)
                {
                    // Carregar glifo do caractere
                    if (FT_Load_Char(face, c, FT_LOAD_RENDER) != 0)
                    {
                        Console.WriteLine("ERROR::FREETYTPE: Failed to load Glyph");
                        continue;
                    }

                    // gerar textura
                    _gl.GenTextures(1, out _texture);
                    _gl.BindTexture(TextureTarget.Texture2D, _texture);

                    _gl.TexImage2D(
                        TextureTarget.Texture2D,
                        0,
                        InternalFormat.Red,
                        face->glyph->bitmap.width,
                        face->glyph->bitmap.rows,
                        0,
                        PixelFormat.Red,
                        PixelType.UnsignedByte,
                        face->glyph->bitmap.buffer
                    );

                    // definir opções de textura
                    _gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.ClampToEdge);
                    _gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.ClampToEdge);
                    _gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Linear);
                    _gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);

                    // agora armazene o caractere para uso posterior
                    Character character = new Character()
                    {
                        TextureID = _texture,
                        Size = new Vector2(face->glyph->bitmap.width, face->glyph->bitmap.rows),
                        Bearing = new Vector2(face->glyph->bitmap_left, face->glyph->bitmap_top),
                        Advance = (uint)face->glyph->advance.x
                    };

                    _characters.Add(c, character);
                }

                _gl.BindTexture(TextureTarget.Texture2D, 0);
            }

            // destruir o FreeType assim que terminarmos
            FT_Done_Face(face);
            FT_Done_FreeType(ft);
        }

        // configurar VAO/VBO para quads de textura
        // --------------------------------------------------
        _gl.GenVertexArrays(1, out _vertexArrayObject);
        _gl.GenBuffers(1, out _vertexBufferObject);

        _gl.BindVertexArray(_vertexArrayObject);

        _gl.BindBuffer(BufferTargetARB.ArrayBuffer, _vertexBufferObject);
        unsafe
        {
            _gl.BufferData(BufferTargetARB.ArrayBuffer, (uint)(sizeof(float) * 6 * 4), null, BufferUsageARB.DynamicDraw);
        }

        _gl.EnableVertexAttribArray(0);
        unsafe
        {
            _gl.VertexAttribPointer(0, 4, VertexAttribPointerType.Float, false, 4 * sizeof(float), (void*)0);
        }

        _gl.BindBuffer(BufferTargetARB.ArrayBuffer, 0);
        _gl.BindVertexArray(0);
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
        _gl.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

        RenderText(_shader, "This is sample text", 25.0f, 25.0f, 1.0f, new Vector3(0.5f, 0.8f, 0.2f));
        RenderText(_shader, "(C) LearnOpenGL.com", 540.0f, 570.0f, 0.5f, new Vector3(0.3f, 0.7f, 0.9f));
    }

    private static void OnClosing()
    {
        
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

    // renderizar linha de texto
    // --------------------------------------------------
    private static void RenderText(Shader shader, string text, float x, float y, float scale, Vector3 color)
    {
        // ativar o estado de renderização correspondente
        shader.Use();
        _gl.Uniform3(_gl.GetUniformLocation(shader.ID, "textColor"), color.X, color.Y, color.Z);

        _gl.ActiveTexture(TextureUnit.Texture0);
        _gl.BindVertexArray(_vertexArrayObject);

        // percorrer todos os caracteres
        foreach (char c in text)
        {
            Character ch = _characters[c];

            float xpos = x + ch.Bearing.X * scale;
            float ypos = y - (ch.Size.Y - ch.Bearing.Y) * scale;

            float w = ch.Size.X * scale;
            float h = ch.Size.Y * scale;

            // atualizar o VBO para cada caractere
            float[,] vertices = new float[6, 4]
            {
                  // posição // coordendas de textura
                { xpos,     ypos,     0.0f, 1.0f },
                { xpos + w, ypos,     1.0f, 1.0f },
                { xpos + w, ypos + h, 1.0f, 0.0f },
                { xpos,     ypos,     0.0f, 1.0f },
                { xpos + w, ypos + h, 1.0f, 0.0f },
                { xpos,     ypos + h, 0.0f, 0.0f }
            };

            // renderizar textura de glifo sobre quadrilátero
            _gl.BindTexture(TextureTarget.Texture2D, ch.TextureID);

            // atualizar o conteúdo da memória VBO
            _gl.BindBuffer(BufferTargetARB.ArrayBuffer, _vertexBufferObject);
            unsafe
            {
                fixed (float* buf = vertices)
                {
                    _gl.BufferSubData(BufferTargetARB.ArrayBuffer, 0, (uint)(vertices.Length * sizeof(float)), buf); // certifique-se de usar glBufferSubData e não glBufferData
                }
            }

            _gl.BindBuffer(BufferTargetARB.ArrayBuffer, 0);

            // renderizar quadrilátero
            _gl.DrawArrays(PrimitiveType.Triangles, 0, 6);

            // agora avance os cursores para o próximo glifo (note que o avanço é em unidades de 1/64 de pixel)
            x += (ch.Advance >> 6) * scale; // deslocamento de bits de 6 posições para obter o valor em pixels (2^6 = 64 (divida a quantidade de 1/64 de pixel por 64 para obter a quantidade de pixels))
        }

        _gl.BindVertexArray(0);
        _gl.BindTexture(TextureTarget.Texture2D, 0);
    }
}
