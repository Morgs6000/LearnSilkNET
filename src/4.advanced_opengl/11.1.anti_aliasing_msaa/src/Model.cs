using System.Numerics;
using Silk.NET.Assimp;
using Silk.NET.OpenGL;
using StbImageSharp;
using AssimpMesh = Silk.NET.Assimp.Mesh;

namespace LearnSilkNET.src;

public class Model
{
    private GL _gl;
    private Assimp _assimp;

    // dados do modelo
    public List<Texture> textures_loaded = []; // armazena todas as texturas carregadas até o momento; uma otimização para garantir que as texturas não sejam carregadas mais de uma vez.
    public List<Mesh> meshes = [];
    public string directory = string.Empty;
    public bool gammaCorrection;

    // construtor, espera um caminho de arquivo para um modelo 3D.
    public Model(GL gl, string path, bool gamma = false)
    {
        _gl = gl;
        _assimp = Assimp.GetApi();

        gammaCorrection = gamma;

        LoadModel(path);
    }

    // desenha o modelo e, consequentemente, todas as suas malhas
    public void Draw(Shader shader)
    {
        for (int i = 0; i < meshes.Count(); i++)
        {
            meshes[i].Draw(shader);
        }
    }

    // carrega um modelo a partir de um arquivo, utilizando extensões suportadas pelo ASSIMP, e armazena as malhas resultantes no vetor de malhas.
    private unsafe void LoadModel(string path)
    {
        // ler arquivo via ASSIMP
        Scene* scene = _assimp.ImportFile(path, (uint)(PostProcessSteps.Triangulate | PostProcessSteps.GenerateSmoothNormals | PostProcessSteps.FlipUVs | PostProcessSteps.CalculateTangentSpace));

        // verificar se há erros
        if (scene == null || scene->MFlags == Assimp.SceneFlagsIncomplete || scene->MRootNode == null) // se não for zero
        {
            Console.WriteLine("ERROR::ASSIMP:: " + _assimp.GetErrorStringS());
            return;
        }

        // obtém o caminho do diretório a partir do caminho do arquivo
        directory = path.Substring(0, path.LastIndexOf('/'));

        // processa o nó raiz do ASSIMP recursivamente
        ProcessNode(scene->MRootNode, scene);
    }

    // processa um nó de forma recursiva. Processa cada malha individual localizada no nó e repete esse processo nos nós filhos (se houver).
    private unsafe void ProcessNode(Node* node, Scene* scene)
    {
        // processa cada malha localizada no nó atual
        for (int i = 0; i < node->MNumMeshes; i++)
        {
            // o objeto de nó contém apenas índices para referenciar os objetos reais na cena. 
            // a cena contém todos os dados; o nó serve apenas para manter as coisas organizadas (como as relações entre nós).
            AssimpMesh* mesh = scene->MMeshes[node->MMeshes[i]];
            meshes.Add(ProcessMesh(mesh, scene));
        }

        // após processarmos todas as malhas (se houver), processamos recursivamente cada um dos nós filhos
        for (int i = 0; i < node->MNumChildren; i++)
        {
            ProcessNode(node->MChildren[i], scene);
        }
    }

    private unsafe Mesh ProcessMesh(AssimpMesh* mesh, Scene* scene)
    {
        // dados a preencher
        List<Vertex> vertices = [];
        List<uint> indices = [];
        List<Texture> textures = [];

        // percorre cada um dos vértices da malha
        for (int i = 0; i < mesh->MNumVertices; i++)
        {
            Vertex vertex = new Vertex();
            Vector3 vector; // Declaramos um vetor temporário, já que o Assimp utiliza sua própria classe de vetor que não é convertida diretamente para a classe vec3 do GLM; portanto, transferimos os dados para esse glm::vec3 temporário primeiro.

            // posições
            vector.X = mesh->MVertices[i].X;
            vector.Y = mesh->MVertices[i].Y;
            vector.Z = mesh->MVertices[i].Z;

            vertex.Position = vector;

            // normais
            if (mesh->MNormals != null)
            {
                vector.X = mesh->MNormals[i].X;
                vector.Y = mesh->MNormals[i].Y;
                vector.Z = mesh->MNormals[i].Z;

                vertex.Normal = vector;
            }

            // coordenadas de textura
            if (mesh->MTextureCoords[0] != null) // a malha contém coordenadas de textura?
            {
                Vector2 vec;

                // Um ​​vértice pode conter até 8 coordenadas de textura diferentes. Portanto, assumimos que não
                // utilizaremos modelos nos quais um vértice possa ter múltiplas coordenadas de textura; assim, sempre utilizamos o primeiro conjunto (0).
                vec.X = mesh->MTextureCoords[0][i].X;
                vec.Y = mesh->MTextureCoords[0][i].Y;

                vertex.TexCoords = vec;

                // tangente
                vector.X = mesh->MTangents[i].X;
                vector.Y = mesh->MTangents[i].Y;
                vector.Z = mesh->MTangents[i].Z;

                vertex.Tangent = vector;

                // bitangente
                vector.X = mesh->MBitangents[i].X;
                vector.Y = mesh->MBitangents[i].Y;
                vector.Z = mesh->MBitangents[i].Z;

                vertex.Bitangent = vector;
            }
            else
            {
                vertex.TexCoords = new Vector2(0.0f, 0.0f);
            }

            vertices.Add(vertex);
        }

        // agora, percorra cada uma das faces da malha (uma face é um triângulo da malha) e obtenha os índices de vértice correspondentes.
        for (int i = 0; i < mesh->MNumFaces; i++)
        {
            Face face = mesh->MFaces[i];

            // recupera todos os índices da face e os armazena no vetor de índices
            for (int j = 0; j < face.MNumIndices; j++)
            {
                indices.Add(face.MIndices[j]);
            }
        }

        // processar materiais
        Material* material = scene->MMaterials[mesh->MMaterialIndex];

        // Adotamos uma convenção para os nomes dos samplers nos shaders. Cada textura difusa deve ser nomeada
        // como 'texture_diffuseN', onde N é um número sequencial de 1 a MAX_SAMPLER_NUMBER. 
        // O mesmo se aplica a outras texturas, conforme resumido na lista a seguir:
        // difusa: texture_diffuseN
        // especular: texture_specularN
        // normal: texture_normalN

        // 1. mapas de difusão
        List<Texture> diffuseMaps = LoadMaterialTextures(material, TextureType.Diffuse, "texture_diffuse");
        textures.AddRange(diffuseMaps);

        // 2. mapas de especularidade
        List<Texture> specularMaps = LoadMaterialTextures(material, TextureType.Specular, "texture_specular");
        textures.AddRange(specularMaps);

        // 3. mapas de normais
        List<Texture> normalMaps = LoadMaterialTextures(material, TextureType.Height, "texture_normal");
        textures.AddRange(normalMaps);

        // 4. mapas de altura
        List<Texture> heightMaps = LoadMaterialTextures(material, TextureType.Ambient, "texture_height");
        textures.AddRange(heightMaps);

        // retorna um objeto de malha criado a partir dos dados de malha extraídos
        return new Mesh(_gl, vertices, indices, textures);
    }

    // verifica todas as texturas de material de um determinado tipo e carrega as texturas caso ainda não tenham sido carregadas. 
    // as informações necessárias são retornadas como uma estrutura Texture.
    private unsafe List<Texture> LoadMaterialTextures(Material* mat, TextureType type, string typeName)
    {
        List<Texture> textures = [];

        for (uint i = 0; i < _assimp.GetMaterialTextureCount(mat, type); i++)
        {
            AssimpString str;
            _assimp.GetMaterialTexture(mat, type, i, &str, null, null, null, null, null, null);
            
            // verifica se a textura já foi carregada anteriormente e, em caso afirmativo, prossegue para a próxima iteração: pula o carregamento de uma nova textura
            bool skip = false;

            for (int j = 0; j < textures_loaded.Count(); j++)
            {
                if (textures_loaded[j].path == str)
                {
                    textures.Add(textures_loaded[j]);
                    skip = true; // uma textura com o mesmo caminho de arquivo já foi carregada; prossiga para a próxima. (otimização)
                    break;
                }
            }
            if (!skip)
            {
                // se a textura ainda não tiver sido carregada, carregue-a
                Texture texture;
                texture.id = TextureFromFile(str, directory);
                texture.type = typeName;
                texture.path = str;

                textures.Add(texture);
                textures_loaded.Add(texture); // Armazena como textura carregada para o modelo inteiro, para garantir que não carregaremos texturas duplicadas desnecessariamente.
            }
        }

        return textures;
    }

    private uint TextureFromFile(string path, string directory, bool gamma = false)
    {
        string filename = path;
        filename = directory + '/' + filename;

        uint textureID;
        _gl.GenTextures(1, out textureID);

        int width, height;
        byte[] data;

        ImageResult image;

        using (FileStream stream = System.IO.File.OpenRead(filename))
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

            _gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)Silk.NET.OpenGL.TextureWrapMode.Repeat);
            _gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)Silk.NET.OpenGL.TextureWrapMode.Repeat);
            _gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.LinearMipmapLinear);
            _gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);
        }
        else
        {
            Console.WriteLine("Falha ao carregar a textura no caminho: " + path);
        }

        return textureID;
    }
}
