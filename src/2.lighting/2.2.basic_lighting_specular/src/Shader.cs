using System.Numerics;
using Silk.NET.OpenGL;

namespace LearnSilkNET.src;

public class Shader
{
    private GL _gl;

    public uint ID;

    // o construtor gera o shader em tempo de execução
    // --------------------------------------------------
    public Shader(GL gl, string vertexPath, string fragmentPath)
    {
        _gl = gl;

        // 1. recuperar o código-fonte do vértice/fragmento a partir de filePath
        string vertexCode = string.Empty;
        string fragmentCode = string.Empty;

        try
        {
            vertexCode = File.ReadAllText(vertexPath);
            fragmentCode = File.ReadAllText(fragmentPath);
        }
        catch (Exception e)
        {
            Console.WriteLine("ERROR::SHADER::FILE_NOT_SUCCESSFULLY_READ: " + e.Message);
        }

        string vShaderCode = vertexCode;
        string fShaderCode = fragmentCode;

        // 2. compilar shaders
        uint vertex, fragment;

        // shader de vértice
        vertex = _gl.CreateShader(ShaderType.VertexShader);
        _gl.ShaderSource(vertex, vShaderCode);
        _gl.CompileShader(vertex);
        CheckCompileErrors(vertex, "VERTEX");

        // shader de fragmento
        fragment = _gl.CreateShader(ShaderType.FragmentShader);
        _gl.ShaderSource(fragment, fShaderCode);
        _gl.CompileShader(fragment);
        CheckCompileErrors(fragment, "FRAGMENT");

        // programa de shader
        ID = _gl.CreateProgram();
        _gl.AttachShader(ID, vertex);
        _gl.AttachShader(ID, fragment);
        _gl.LinkProgram(ID);
        CheckCompileErrors(ID, "PROGRAM");

        // exclua os shaders, pois eles já estão vinculados ao nosso programa e não são mais necessários
        _gl.DeleteShader(vertex);
        _gl.DeleteShader(fragment);
    }

    // ativa o shader
    // --------------------------------------------------
    public void Use()
    {
        _gl.UseProgram(ID);
    }

    // funções utilitárias de uniformes
    // --------------------------------------------------
    public void SetBool(string name, bool value)
    {
        int location = _gl.GetUniformLocation(ID, name);
        _gl.Uniform1(location, value ? 1 : 0);
    }
    // --------------------------------------------------
    public void SetInt(string name, int value)
    {
        int location = _gl.GetUniformLocation(ID, name);
        _gl.Uniform1(location, value);
    }
    // --------------------------------------------------
    public void SetFloat(string name, float value)
    {
        int location = _gl.GetUniformLocation(ID, name);
        _gl.Uniform1(location, value);
    }
    // --------------------------------------------------
    public void SetVec2(string name, float x, float y)
    {
        int location = _gl.GetUniformLocation(ID, name);
        _gl.Uniform2(location, x, y);
    }
    public void SetVec2(string name, Vector2 value)
    {
        int location = _gl.GetUniformLocation(ID, name);
        _gl.Uniform2(location, value);
    }
    // --------------------------------------------------
    public void SetVec3(string name, float x, float y, float z)
    {
        int location = _gl.GetUniformLocation(ID, name);
        _gl.Uniform3(location, x, y, z);
    }
    public void SetVec3(string name, Vector3 value)
    {
        int location = _gl.GetUniformLocation(ID, name);
        _gl.Uniform3(location, value);
    }
    // --------------------------------------------------
    public void SetVec4(string name, float x, float y, float z, float w)
    {
        int location = _gl.GetUniformLocation(ID, name);
        _gl.Uniform4(location, x, y, z, w);
    }
    public void SetVec4(string name, Vector4 value)
    {
        int location = _gl.GetUniformLocation(ID, name);
        _gl.Uniform4(location, value);
    }
    // --------------------------------------------------
    public void SetMat4(string name, Matrix4x4 mat)
    {
        int location = _gl.GetUniformLocation(ID, name);
        unsafe
        {
            _gl.UniformMatrix4(location, 1, false, (float*)&mat);
        }
    }

    // função utilitária para verificar erros de compilação/vinculação de shader.
    // --------------------------------------------------
    private void CheckCompileErrors(uint shader, string type)
    {
        int success;
        string infoLog;

        if (type != "PROGRAM")
        {
            _gl.GetShader(shader, ShaderParameterName.CompileStatus, out success);
            if (success == 0)
            {
                _gl.GetShaderInfoLog(shader, out infoLog);
                Console.WriteLine(
                    "ERROR::SHADER_COMPILATION_ERROR of type: " + type + "\n" +
                    infoLog + "\n" +
                    " -- --------------------------------------------------- -- "
                );
            }
        }
        else
        {
            _gl.GetProgram(shader, ProgramPropertyARB.LinkStatus, out success);
            if (success == 0)
            {
                _gl.GetProgramInfoLog(shader, out infoLog);
                Console.WriteLine(
                    "ERROR::PROGRAM_LINKING_ERROR of type: " + type + "\n" +
                    infoLog + "\n" +
                    " -- --------------------------------------------------- -- "
                );
            }
        }
    }
}
