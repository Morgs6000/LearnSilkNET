/**************************************************
** Este código faz parte do Breakout.
**
** O Breakout é um software livre: você pode redistribuí-lo e/ou modificá-lo
** sob os termos da licença CC BY 4.0, conforme publicada pela
** Creative Commons, seja a versão 4 da Licença ou (a seu
** critério) qualquer versão posterior.
**************************************************/

using System.Security.Cryptography;
using Silk.NET.OpenGL;
using StbImageSharp;

namespace LearnSilkNET.src;

// Uma classe singleton estática ResourceManager que disponibiliza diversas
// funções para carregar texturas e shaders. Cada textura e/ou shader carregado
// também é armazenado para referência futura por meio de identificadores de string.
// Todas as funções e recursos são estáticos, e nenhum construtor público é definido.
public class ResourceManager
{
    // armazenamento de recursos
    public static Dictionary<string, Shader> Shaders = [];
    public static Dictionary<string, Texture2D> Textures = [];

    // Carrega (e gera) um programa de shader a partir de arquivos, carregando o código-fonte dos shaders de vértice, fragmento (e geometria). Se gShaderFile não for nullptr, também carrega um shader de geometria.
    public static Shader LoadShader(GL gl, string vShaderFile, string fShaderFile, string? gShaderFile, string name)
    {
        Shaders[name] = LoadShaderFromFile(gl, vShaderFile, fShaderFile, gShaderFile);
        
        return Shaders[name];
    }

    // recupera um sader armazenado
    public static Shader GetShader(string name)
    {
        return Shaders[name];
    }

    // carrega (e gera) uma textura a partir de um arquivo
    public static Texture2D LoadTexture(GL gl, string file, bool alpha, string name)
    {
        Textures[name] = LoadTextureFromFile(gl, file, alpha);
        
        return Textures[name];
    }

    // recupera uma textura armazenada
    public static Texture2D GetTexture(string name)
    {
        return Textures[name];
    }

    // desaloca corretamente todos os recursos carregados
    public static void Clear(GL gl)
    {
        // excluir (corretamente) todos os shaders
        foreach (var iter in Shaders)
        {
            gl.DeleteProgram(iter.Value.ID);
        }

        // excluir (corretamente) todas as texturas
        foreach (var iter in Textures)
        {
            gl.DeleteTextures(1, ref iter.Value.ID);
        }
    }

    // Construtor privado; ou seja, não queremos instâncias reais do gerenciador de recursos. Seus membros e funções devem estar disponíveis publicamente (estáticos).
    private ResourceManager()
    {
        
    }

    // carrega e gera um shader a partir de um arquivo
    private static Shader LoadShaderFromFile(GL gl, string vShaderFile, string fShaderFile, string? gShaderFile = null)
    {
        // 1. recuperar o código-fonte do vértice/fragmento a partir de filePath
        string vertexCode = string.Empty;
        string fragmentCode = string.Empty;
        string geometryCode = string.Empty;

        try
        {
            vertexCode = File.ReadAllText(vShaderFile);
            fragmentCode = File.ReadAllText(fShaderFile);

            if (gShaderFile != null)
            {
                geometryCode = File.ReadAllText(gShaderFile);
            }
        }
        catch (Exception)
        {
            Console.WriteLine("ERROR::SHADER: Failed to read shader files");
        }

        string vShaderCode = vertexCode;
        string fShaderCode = fragmentCode;
        string gShaderCode = geometryCode;

        // 2. agora crie o objeto de shader a partir do código-fonte
        Shader shader = new Shader(gl);
        shader.Compile(vShaderCode, fShaderCode, gShaderFile != null ? gShaderCode : null);

        return shader;
    }

    // carrega uma única textura a partir de um arquivo
    private static Texture2D LoadTextureFromFile(GL gl, string file, bool alpha)
    {
        // criar objeto de textura
        Texture2D texture = new Texture2D(gl);

        if (alpha)
        {
            texture.Internal_Format = InternalFormat.Rgba;
            texture.Image_Format = PixelFormat.Rgba;
        }

        // carregar imagem
        int width, height;
        byte[] data;

        using (FileStream stream = File.OpenRead(file))
        {
            ImageResult image = ImageResult.FromStream(stream, ColorComponents.Default);

            width = image.Width;
            height = image.Height;
            data = image.Data;
        }

        // agora gera a textura
        texture.Generate((uint)width, (uint)height, data);

        // e, finalmente, liberar os dados da imagem
        return texture;
    }
}
