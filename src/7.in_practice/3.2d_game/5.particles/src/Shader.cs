/**************************************************
** Este código faz parte do Breakout.
**
** O Breakout é um software livre: você pode redistribuí-lo e/ou modificá-lo
** sob os termos da licença CC BY 4.0, conforme publicada pela
** Creative Commons, seja a versão 4 da Licença ou (a seu
** critério) qualquer versão posterior.
**************************************************/

using System.Numerics;
using Silk.NET.OpenGL;

namespace LearnSilkNET.src;

// Objeto de shader de propósito geral. Compila a partir de um arquivo, gera
// mensagens de erro de compilação/vinculação e disponibiliza várias funções
// utilitárias para facilitar o gerenciamento.
public class Shader
{
    private GL _gl;

    // estado
    public uint ID;

    // construtor
    public Shader(GL gl)
    {
        _gl = gl;
    }

    // define o shader atual como ativo
    public Shader Use()
    {
        _gl.UseProgram(ID);

        return this;
    }

    // compila o shader a partir do código-fonte fornecido
    public void Compile(string vertexSource, string fragmentSource, string? geometrySource = null)
    {
        uint sVertex, sFragment, gShader = 0;

        // vertex Shader
        sVertex = _gl.CreateShader(ShaderType.VertexShader);
        _gl.ShaderSource(sVertex, vertexSource);
        _gl.CompileShader(sVertex);
        CheckCompileErrors(sVertex, "VERTEX");

        // fragment Shader
        sFragment = _gl.CreateShader(ShaderType.FragmentShader);
        _gl.ShaderSource(sFragment, fragmentSource);
        _gl.CompileShader(sFragment);
        CheckCompileErrors(sFragment, "FRAGMENT");

        // se o código-fonte do shader de geometria for fornecido, compile também o shader de geometria
        if (geometrySource != null)
        {
            gShader = _gl.CreateShader(ShaderType.GeometryShader);
            _gl.ShaderSource(gShader, geometrySource);
            _gl.CompileShader(gShader);
            CheckCompileErrors(gShader, "GEOMETRY");
        }

        // shader program
        ID = _gl.CreateProgram();
        
        _gl.AttachShader(ID, sVertex);
        _gl.AttachShader(ID, sFragment);
        if (geometrySource != null)
        {
            _gl.AttachShader(ID, gShader);
        }

        _gl.LinkProgram(ID);
        CheckCompileErrors(ID, "PROGRAM");

        // exclua os shaders, pois eles já estão vinculados ao nosso programa e não são mais necessários
        _gl.DeleteShader(sVertex);
        _gl.DeleteShader(sFragment);
        if (geometrySource != null)
        {
            _gl.DeleteShader(gShader);
        }
    }

    // funções utilitárias
    public void SetFloat(string name, float value, bool useShader = false)
    {
        if (useShader)
        {
            Use();
        }

        int location = _gl.GetUniformLocation(ID, name);
        _gl.Uniform1(location, value);
    }

    public void SetInteger(string name, int value, bool useShader = false)
    {
        if (useShader)
        {
            Use();
        }

        int location = _gl.GetUniformLocation(ID, name);
        _gl.Uniform1(location, value);
    }  

    public void SetVector2f(string name, float x, float y, bool useShader = false)
    {
        if (useShader)
        {
            Use();
        }

        int location = _gl.GetUniformLocation(ID, name);
        _gl.Uniform2(location, x, y);
    }    

    public void SetVector2f(string name, Vector2 value, bool useShader = false)
    {
        if (useShader)
        {
            Use();
        }

        int location = _gl.GetUniformLocation(ID, name);
        _gl.Uniform2(location, value);
    }  

    public void SetVector3f(string name, float x, float y, float z, bool useShader = false)
    {
        if (useShader)
        {
            Use();
        }

        int location = _gl.GetUniformLocation(ID, name);
        _gl.Uniform3(location, x, y, z);
    }    

    public void SetVector3f(string name, Vector3 value, bool useShader = false)
    {
        if (useShader)
        {
            Use();
        }

        int location = _gl.GetUniformLocation(ID, name);
        _gl.Uniform3(location, value);
    }  

    public void SetVector4f(string name, float x, float y, float z, float w, bool useShader = false)
    {
        if (useShader)
        {
            Use();
        }

        int location = _gl.GetUniformLocation(ID, name);
        _gl.Uniform4(location, x, y, z, w);
    }    

    public void SetVector4f(string name, Vector4 value, bool useShader = false)
    {
        if (useShader)
        {
            Use();
        }

        int location = _gl.GetUniformLocation(ID, name);
        _gl.Uniform4(location, value);
    }  

    public void SetMatrix4(string name, Matrix4x4 matrix, bool useShader = false)
    {
        if (useShader)
        {
            Use();
        }

        int location = _gl.GetUniformLocation(ID, name);
        unsafe
        {
            _gl.UniformMatrix4(location, 1, false, (float*)&matrix);
        }
    } 

    // verifica se a compilação ou vinculação falhou e, em caso afirmativo, imprime os logs de erros
    private void CheckCompileErrors(uint obj, string type)
    {
        int success;
        string infoLog;

        if (type != "PROGRAM")
        {
            _gl.GetShader(obj, ShaderParameterName.CompileStatus, out success);
            if (success == 0)
            {
                _gl.GetShaderInfoLog(obj, out infoLog);
                Console.WriteLine(
                    "| ERROR::SHADER: Compile-time error: Type: " + type + "\n" +
                    infoLog + "\n" + 
                    " -- --------------------------------------------------- -- "
                );
            }
        }
        else
        {
            _gl.GetProgram(obj, ProgramPropertyARB.LinkStatus, out success);
            if (success == 0)
            {
                _gl.GetProgramInfoLog(obj, out infoLog);
                Console.WriteLine(
                    "| ERROR::Shader: Link-time error: Type: " + type + "\n" +
                    infoLog + "\n" + 
                    " -- --------------------------------------------------- -- "
                );
            }
        }
    }
}
