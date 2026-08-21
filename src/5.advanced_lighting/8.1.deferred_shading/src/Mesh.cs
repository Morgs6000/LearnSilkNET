using System.Numerics;
using System.Runtime.InteropServices;
using Silk.NET.OpenGL;

namespace LearnSilkNET.src;

[StructLayout(LayoutKind.Sequential)]
public struct Vertex
{
    private const int MAX_BONE_INFLUENCE = 4;

    // posição
    public Vector3 Position;

    // normal
    public Vector3 Normal;

    // coordenadas de textura
    public Vector2 TexCoords;

    // tangente
    public Vector3 Tangent;

    // bitangente
    public Vector3 Bitangent;

    // índices de ossos que influenciarão este vértice
    public unsafe fixed int m_BoneIDs[MAX_BONE_INFLUENCE];

    // pesos de cada osso
    public unsafe fixed int m_Weights[MAX_BONE_INFLUENCE];
}

public struct Texture
{
    public uint id;
    public string type;
    public string path;
}

public class Mesh
{
    private GL _gl;

    // Dados da malha
    public List<Vertex> vertices = [];
    public List<uint> indices = [];
    public List<Texture> textures = [];
    public uint VAO;

    // construtor
    public Mesh(GL gl, List<Vertex> vertices, List<uint> indices, List<Texture> textures)
    {
        _gl = gl;

        this.vertices = vertices;
        this.indices = indices;
        this.textures = textures;

        // agora que temos todos os dados necessários, defina os buffers de vértices e seus ponteiros de atributos.
        SetupMesh();
    }

    // renderizar a malha
    public void Draw(Shader shader)
    {
        // vincular as texturas apropriadas
        uint diffseNr = 1;
        uint specularNr = 1;
        uint normalNr = 1;
        uint heightNr = 1;

        for (int i = 0; i < textures.Count; i++)
        {
            _gl.ActiveTexture(TextureUnit.Texture0 + i); // ativar a unidade de textura correta antes de vincular

            // obtém o número da textura (o N em diffuse_textureN)
            string number = string.Empty;
            string name = textures[i].type;

            if (name == "texture_diffuse")
            {
                number = diffseNr++.ToString();
            }
            else if (name == "texture_specular")
            {
                number = specularNr++.ToString(); // converte unsigned int para string
            }
            else if (name == "texture_normal")
            {
                number = normalNr++.ToString(); // converte unsigned int para string
            }
            else if (name == "texture_height")
            {
                number = heightNr++.ToString(); // converte unsigned int para string
            }

            // agora defina o sampler para a unidade de textura correta
            _gl.Uniform1(_gl.GetUniformLocation(shader.ID, name + number), i);

            // e, finalmente, vincule a textura
            _gl.BindTexture(TextureTarget.Texture2D, textures[i].id);
        }

        // desenhar malha
        _gl.BindVertexArray(VAO);
        unsafe
        {
            _gl.DrawElements(PrimitiveType.Triangles, (uint)indices.Count, DrawElementsType.UnsignedInt, (void*)0);
        }
        _gl.BindVertexArray(0);

        // É sempre uma boa prática restaurar tudo para os padrões após a configuração.
        _gl.ActiveTexture(TextureUnit.Texture0);
    }

    // renderizar dados
    private uint VBO, EBO;

    // inicializa todos os objetos/arrays de buffer
    private void SetupMesh()
    {
        // criar buffers/arrays
        _gl.GenVertexArrays(1, out VAO);
        _gl.GenBuffers(1, out VBO);
        _gl.GenBuffers(1, out EBO);

        _gl.BindVertexArray(VAO);

        // carregar dados em buffers de vértices
        _gl.BindBuffer(BufferTargetARB.ArrayBuffer, VBO);

        // Uma grande vantagem das structs é que o layout de memória de seus itens é sequencial. 
        // Isso significa que podemos simplesmente passar um ponteiro para a struct, e ela é traduzida perfeitamente para um array de glm::vec3/2,
        // que por sua vez é traduzido para 3 ou 2 valores do tipo float, resultando finalmente em um array de bytes.
        unsafe
        {
            fixed (Vertex* buf = vertices.ToArray())
            {
                _gl.BufferData(BufferTargetARB.ArrayBuffer, (uint)(vertices.Count * sizeof(Vertex)), buf, BufferUsageARB.StaticDraw);
            }
        }

        _gl.BindBuffer(BufferTargetARB.ElementArrayBuffer, EBO);
        unsafe
        {
            fixed (uint* buf = indices.ToArray())
            {
                _gl.BufferData(BufferTargetARB.ElementArrayBuffer, (uint)(indices.Count * sizeof(uint)), buf, BufferUsageARB.StaticDraw);
            }
        }

        // define os ponteiros de atributos de vértice

        // vertex Positions
        _gl.EnableVertexAttribArray(0);
        unsafe
        {
            _gl.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, (uint)sizeof(Vertex), (void*)0);
        }

        // normais dos vértices
        _gl.EnableVertexAttribArray(1);
        unsafe
        {
            _gl.VertexAttribPointer(1, 3, VertexAttribPointerType.Float, false, (uint)sizeof(Vertex), (void*)Marshal.OffsetOf<Vertex>(nameof(Vertex.Normal)));
        }

        // coordenadas de textura do vértice
        _gl.EnableVertexAttribArray(2);
        unsafe
        {
            _gl.VertexAttribPointer(2, 2, VertexAttribPointerType.Float, false, (uint)sizeof(Vertex), (void*)Marshal.OffsetOf<Vertex>(nameof(Vertex.TexCoords)));
        }

        // tangente do vértice
        _gl.EnableVertexAttribArray(3);
        unsafe
        {
            _gl.VertexAttribPointer(3, 3, VertexAttribPointerType.Float, false, (uint)sizeof(Vertex), (void*)Marshal.OffsetOf<Vertex>(nameof(Vertex.Tangent)));
        }

        // bitangente do vértice
        _gl.EnableVertexAttribArray(4);
        unsafe
        {
            _gl.VertexAttribPointer(4, 3, VertexAttribPointerType.Float, false, (uint)sizeof(Vertex), (void*)Marshal.OffsetOf<Vertex>(nameof(Vertex.Bitangent)));
        }

        // ids
        _gl.EnableVertexAttribArray(5);
        unsafe
        {
            _gl.VertexAttribIPointer(5, 4, GLEnum.Int, (uint)sizeof(Vertex), 
            (void*)Marshal.OffsetOf<Vertex>(nameof(Vertex.m_BoneIDs)));
        }

        // pesos
        _gl.EnableVertexAttribArray(6);
        unsafe
        {
            _gl.VertexAttribPointer(6, 4, VertexAttribPointerType.Float, false, (uint)sizeof(Vertex), (void*)Marshal.OffsetOf<Vertex>(nameof(Vertex.m_Weights)));
        }

        _gl.BindVertexArray(0);
    }
}
