/*
using System.Buffers.Binary;
using System.Text;
using Silk.NET.OpenAL;

namespace LearnSilkNET.src;

public class ISoundEngine
{   
    private ALContext _alc = null!;
    private AL _al = null!;

    private unsafe Device* _device;
    private unsafe Context* _context;

    private uint _source;
    private uint _buffer;

    public void Play2D(string filePath, bool shouldLoop)
    {
        // if (args.Length != 1)
        // {
        //     Console.WriteLine("Deve ser fornecido exatamente um argumento: o caminho para o arquivo .wav que deve ser reproduzido.");
        //     return;
        // }

        // string filePath = args[0];
        ReadOnlySpan<byte> file = File.ReadAllBytes(filePath);
        int index = 0;

        if (file[index++] != 'R' || file[index++] != 'I' || file[index++] != 'F' || file[index++] != 'F')
        {
            Console.WriteLine("O arquivo fornecido não está no formato RIFF.");
            return;
        }

        int chunkSize = BinaryPrimitives.ReadInt32LittleEndian(file.Slice(index,  4));
        index += 4;

        if (file[index++] != 'W' || file[index++] != 'A' || file[index++] != 'V' || file[index++] != 'E')
        {
            Console.WriteLine("O arquivo fornecido não está no formato WAVE.");
            return;
        }

        short numChannels = -1;
        int sampleRate = -1;
        int byteRate = -1;
        short blockAlign = -1;
        short bitsPerSample = -1;
        BufferFormat format = 0;

        _alc = ALContext.GetApi();
        _al = AL.GetApi();
        unsafe
        {
            _device = _alc.OpenDevice("");

            if (_device == null)
            {
                Console.WriteLine("Não foi possível criar o dispositivo.");
                return;
            }

            _context = _alc.CreateContext(_device, null);
            _alc.MakeContextCurrent(_context);
        }

        _al.GetError();

        _source = _al.GenSource();
        _buffer = _al.GenBuffer();
        _al.SetSourceProperty(_source, SourceBoolean.Looping, shouldLoop);

        while (index + 4 < file.Length)
        {
            string identifier = "" + (char) file[index++] + (char) file[index++] + (char) file[index++] + (char) file[index++];
            int size = BinaryPrimitives.ReadInt32LittleEndian(file.Slice(index, 4));
            index += 4;

            if (identifier == "fmt ")
            {
                if (size != 16)
                {
                    Console.WriteLine($"Formato de áudio desconhecido com tamanho de subchunk1 {size}.");
                }
                else
                {
                    short audioFormat = BinaryPrimitives.ReadInt16LittleEndian(file.Slice(index, 2));
                    index += 2;

                    if (audioFormat != 1)
                    {
                        Console.WriteLine($"Formato de áudio desconhecido com ID {audioFormat}.");
                    }
                    else
                    {
                        numChannels = BinaryPrimitives.ReadInt16LittleEndian(file.Slice(index, 2));
                        index += 2;

                        sampleRate = BinaryPrimitives.ReadInt32LittleEndian(file.Slice(index, 4));
                        index += 4;

                        byteRate = BinaryPrimitives.ReadInt32LittleEndian(file.Slice(index, 4));
                        index += 4;

                        blockAlign = BinaryPrimitives.ReadInt16LittleEndian(file.Slice(index, 2));
                        index += 2;

                        bitsPerSample = BinaryPrimitives.ReadInt16LittleEndian(file.Slice(index, 2));
                        index += 2;

                        if (numChannels == 1)
                        {
                            if (bitsPerSample == 8)
                            {
                                format = BufferFormat.Mono8;
                            }
                            else if (bitsPerSample == 16)
                            {
                                format = BufferFormat.Mono16;
                            }
                            else
                            {
                                Console.WriteLine($"Não é possível reproduzir áudio mono de {bitsPerSample} bits por amostra.");
                            }
                        }
                        else if (numChannels == 2)
                        {
                            if (bitsPerSample == 8)
                            {
                                format = BufferFormat.Stereo8;
                            }
                            else if (bitsPerSample == 16)
                            {
                                format = BufferFormat.Stereo16;
                            }
                            else
                            {
                                Console.WriteLine($"Não é possível reproduzir som estéreo de {bitsPerSample} bits.");
                            }
                        }
                        else
                        {
                            Console.WriteLine($"Não é possível reproduzir áudio com {numChannels} canais de som.");
                        }
                    }
                }
            }            
            else if (identifier == "data")
            {
                ReadOnlySpan<byte> data = file.Slice(index, size);
                index += size;

                unsafe
                {
                    fixed (byte* pData = data)
                    {
                        _al.BufferData(_buffer, format, pData, size, sampleRate);
                    }
                }

                Console.WriteLine($"Ler {size} bytes de dados.");
            }
            else if (identifier == "JUNK")
            {
                // isso existe para alinhar as coisas
                index += size;
            }
            else if (identifier == "iXML")
            {
                ReadOnlySpan<byte> v = file.Slice(index, size);
                string str = Encoding.ASCII.GetString(v);
                Console.WriteLine($"iXML Chunk: {str};");
                index += size;
            }
            else
            {
                Console.WriteLine($"Seção desconhecida: {identifier}.");
                index += size;
            }
        }

        Console.WriteLine($"Sucesso. Arquivo de áudio RIFF-WAVE detectado, codificação PCM. {numChannels} canais, {sampleRate} taxa de amostragem, {byteRate} taxa de bytes, {blockAlign} alinhamento de bloco, {bitsPerSample} bits por amostra.");

        _al.SetSourceProperty(_source, SourceInteger.Buffer, _buffer);
        _al.SourcePlay(_source);

        // Console.WriteLine("Pressione Enter para sair...");
        // Console.ReadLine();

        // al.SourceStop(source);

        // al.DeleteSource(source);
        // al.DeleteBuffer(buffer);

        // unsafe
        // {
        //     alc.DestroyContext(_context);
        //     alc.CloseDevice(_device);
        // }

        // al.Dispose();
        // alc.Dispose();
    }

    public void Drop()
    {
        _al.SourceStop(_source);

        _al.DeleteSource(_source);
        _al.DeleteBuffer(_buffer);

        unsafe
        {
            _alc.DestroyContext(_context);
            _alc.CloseDevice(_device);
        }

        _al.Dispose();
        _alc.Dispose();
    }
}
*/
using NAudio.Wave;
using NLayer.NAudioSupport;
using Silk.NET.OpenAL;
using Silk.NET.OpenGL;

namespace LearnSilkNET.src;

public class ISoundEngine
{
    private ALContext _alc = null!;
    private AL _al = null!;

    private unsafe Device* _device;
    private unsafe Context* _context;

    private List<uint> _sources = new List<uint>();
    private List<uint> _buffers = new List<uint>();

    public ISoundEngine()
    {
        _alc = ALContext.GetApi();
        _al = AL.GetApi();

        unsafe
        {
            _device = _alc.OpenDevice("");

            if (_device == null)
            {
                Console.WriteLine("Não foi possível criar o dispositivo.");
                return;
            }

            _context = _alc.CreateContext(_device, null);
            _alc.MakeContextCurrent(_context);
        }

        _al.GetError();
    }

    public void Play2D(string filePath, bool shouldLoop)
    {
        byte[] audioData;
        int sampleRate;
        int channels;
        
        string extension = Path.GetExtension(filePath).ToLower();
        
        if (extension == ".mp3")
        {
            // Para MP3, usa Mp3FileReaderBase
            using (var reader = new Mp3FileReaderBase(filePath, wf => new Mp3FrameDecompressor(wf)))
            {
                sampleRate = reader.Mp3WaveFormat.SampleRate;
                channels = reader.Mp3WaveFormat.Channels;
                
                Console.WriteLine($"Formato MP3: {reader.Mp3WaveFormat}");
                Console.WriteLine($"Bits por sample: {reader.Mp3WaveFormat.BitsPerSample}");
                
                // Lê os dados
                using (var memoryStream = new MemoryStream())
                {
                    reader.CopyTo(memoryStream);
                    byte[] rawData = memoryStream.ToArray();
                    
                    // Verifica o formato dos dados
                    if (reader.Mp3WaveFormat.BitsPerSample == 16)
                    {
                        // Já está em 16-bit PCM
                        audioData = rawData;
                        Console.WriteLine("Dados já em 16-bit PCM");
                    }
                    else
                    {
                        // Converte de float (32-bit) para 16-bit
                        int floatSampleCount = rawData.Length / 4;
                        audioData = new byte[floatSampleCount * 2];
                        
                        for (int i = 0; i < floatSampleCount; i++)
                        {
                            float sample = BitConverter.ToSingle(rawData, i * 4);
                            sample = Math.Clamp(sample, -1.0f, 1.0f);
                            short sample16 = (short)(sample * 32767.0f);
                            audioData[i * 2] = (byte)(sample16 & 0xFF);
                            audioData[i * 2 + 1] = (byte)((sample16 >> 8) & 0xFF);
                        }
                        Console.WriteLine("Convertido de float para 16-bit PCM");
                    }
                }
            }
        }
        else if (extension == ".wav")
        {
            // Para WAV, usa WaveFileReader
            using (var reader = new WaveFileReader(filePath))
            {
                sampleRate = reader.WaveFormat.SampleRate;
                channels = reader.WaveFormat.Channels;
                
                Console.WriteLine($"Formato WAV: {reader.WaveFormat}");
                
                // Lê os dados
                using (var memoryStream = new MemoryStream())
                {
                    reader.CopyTo(memoryStream);
                    audioData = memoryStream.ToArray();
                }
                
                // Se não for 16-bit PCM, converte
                if (reader.WaveFormat.BitsPerSample != 16 || 
                    reader.WaveFormat.Encoding != WaveFormatEncoding.Pcm)
                {
                    Console.WriteLine("Convertendo WAV para 16-bit PCM...");
                    
                    int bytesPerSample = reader.WaveFormat.BitsPerSample / 8;
                    int sampleCount = audioData.Length / bytesPerSample;
                    byte[] convertedData = new byte[sampleCount * 2];
                    
                    for (int i = 0; i < sampleCount; i++)
                    {
                        int offset = i * bytesPerSample;
                        float sample = 0;
                        
                        switch (reader.WaveFormat.BitsPerSample)
                        {
                            case 8:
                                // 8-bit PCM (unsigned)
                                sample = (audioData[offset] - 128) / 128.0f;
                                break;
                            case 24:
                                // 24-bit PCM
                                int value24 = audioData[offset] | 
                                             (audioData[offset + 1] << 8) | 
                                             (audioData[offset + 2] << 16);
                                if ((value24 & 0x800000) != 0)
                                    value24 |= unchecked((int)0xFF000000);
                                sample = value24 / 8388608.0f;
                                break;
                            case 32:
                                // 32-bit float ou PCM
                                sample = BitConverter.ToSingle(audioData, offset);
                                break;
                        }
                        
                        sample = Math.Clamp(sample, -1.0f, 1.0f);
                        short sample16 = (short)(sample * 32767.0f);
                        convertedData[i * 2] = (byte)(sample16 & 0xFF);
                        convertedData[i * 2 + 1] = (byte)((sample16 >> 8) & 0xFF);
                    }
                    
                    audioData = convertedData;
                }
            }
        }
        else
        {
            Console.WriteLine($"Formato não suportado: {extension}");
            return;
        }

        // Determina o formato OpenAL
        BufferFormat format;
        if (channels == 1)
        {
            format = BufferFormat.Mono16;
        }
        else if (channels == 2)
        {
            format = BufferFormat.Stereo16;
        }
        else
        {
            Console.WriteLine($"Não é possível reproduzir áudio com {channels} canais");
            return;
        }

        // Cria source e buffer
        uint source = _al.GenSource();
        uint buffer = _al.GenBuffer();
        
        _sources.Add(source);
        _buffers.Add(buffer);
        
        _al.SetSourceProperty(source, SourceBoolean.Looping, shouldLoop);

        // Carrega os dados de áudio
        unsafe
        {
            fixed (byte* pData = audioData)
            {
                _al.BufferData(buffer, format, pData, audioData.Length, sampleRate);
            }
        }

        // Verifica erros do OpenAL
        AudioError error = _al.GetError();
        if (error != AudioError.NoError)
        {
            Console.WriteLine($"Erro OpenAL: {error}");
        }
        
        // Reproduz o áudio
        _al.SetSourceProperty(source, SourceInteger.Buffer, buffer);
        _al.SourcePlay(source);
        
        Console.WriteLine($"Áudio carregado: {channels} canais, {sampleRate} Hz, {audioData.Length} bytes");
    }

    public void Drop()
    {
        foreach (var source in _sources)
        {
            _al.SourceStop(source);
            _al.DeleteSource(source);
        }
        
        foreach (var buffer in _buffers)
        {
            _al.DeleteBuffer(buffer);
        }
        
        unsafe
        {
            _alc.DestroyContext(_context);
            _alc.CloseDevice(_device);
        }

        _al.Dispose();
        _alc.Dispose();
    }
}
