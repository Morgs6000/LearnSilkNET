/**************************************************
** Este código faz parte do Breakout.
**
** O Breakout é um software livre: você pode redistribuí-lo e/ou modificá-lo
** sob os termos da licença CC BY 4.0, conforme publicada pela
** Creative Commons, seja a versão 4 da Licença ou (a seu
** critério) qualquer versão posterior.
**************************************************/

using System.Numerics;

namespace LearnSilkNET.src;

/// GameLevel armazena todos os blocos (tiles) de uma fase do tipo Breakout e
/// disponibiliza funcionalidades para carregar e renderizar fases a partir do disco rígido.
public class GameLevel
{
    // estado do nível
    public List<GameObject> Bricks = [];

    // construtor
    public GameLevel()
    {
        
    }

    // carrega a fase a partir de um arquivo
    public void Load(string file, uint levelWidth, uint levelHeight)
    {
        // limpar dados antigos
        Bricks.Clear();

        // carregar do arquivo
        string? line;
        List<List<uint>> tileData = [];

        if (File.Exists(file))
        {
            using (StreamReader reader = new StreamReader(file))
            {
                while ((line = reader.ReadLine()) != null)
                {
                    string[] tokens = line.Split(new char[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
                    List<uint> row = [];

                    foreach (string token in tokens)
                    {
                        if (uint.TryParse(token, out uint tileCode))
                        {
                            row.Add(tileCode);
                        }
                    }

                    if (row.Count > 0)
                    {
                        tileData.Add(row);
                    }
                }

                if (tileData.Count > 0)
                {
                    Init(tileData, levelWidth, levelHeight);
                }
            }
        }
    }

    // renderizar nível
    public void Draw(SpriteRenderer renderer)
    {
        foreach (GameObject tile in Bricks)
        {
            if (!tile.Destroyed)
            {
                tile.Draw(renderer);
            }
        }
    }

    // verifica se a fase foi concluída (todos os blocos não sólidos foram destruídos)
    public bool IsCompleted()
    {
        foreach (GameObject tile in Bricks)
        {
            if (!tile.IsSolid && !tile.Destroyed)
            {
                return false;
            }
        }

        return true;
    }

    // inicializa o nível a partir dos dados de tiles
    private void Init(List<List<uint>> tileData, uint levelWidth, uint levelHeight)
    {
        // calcular dimensões
        uint height = (uint)tileData.Count();
        uint width = (uint)tileData[0].Count(); // note que podemos acessar o vetor no índice [0], já que esta função só é chamada se height > 0

        float uint_width = levelWidth / (float)width, uint_height = levelHeight / height;

        // inicializa os blocos do nível com base em tileData
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                // verifica o tipo de bloco a partir dos dados do nível (matriz 2D do nível)
                if (tileData[y][x] == 1) // sólido
                {
                    Vector2 pos = new Vector2(uint_width * x, uint_height * y);
                    Vector2 size = new Vector2(uint_width, uint_height);

                    GameObject obj = new GameObject(pos, size, ResourceManager.GetTexture("block_solid"), new Vector3(0.8f, 0.8f, 0.7f));
                    obj.IsSolid = true;

                    Bricks.Add(obj);
                }
                else if (tileData[y][x] > 1) // não sólido; agora determine sua cor com base nos dados do nível
                {
                    Vector3 color = new Vector3(1.0f); // original: branco

                    if (tileData[y][x] == 2)
                    {
                        color = new Vector3(0.2f, 0.6f, 1.0f);
                    }
                    else if (tileData[y][x] == 3)
                    {
                        color = new Vector3(0.0f, 0.7f, 0.0f);
                    }
                    else if (tileData[y][x] == 4)
                    {
                        color = new Vector3(0.8f, 0.8f, 0.4f);
                    }
                    else if (tileData[y][x] == 5)
                    {
                        color = new Vector3(1.0f, 0.5f, 0.0f);
                    }

                    Vector2 pos = new Vector2(uint_width * x, uint_height * y);
                    Vector2 size = new Vector2(uint_width, uint_height);

                    Bricks.Add(new GameObject(pos, size, ResourceManager.GetTexture("block"), color));
                }
            }
        }
    }
}
