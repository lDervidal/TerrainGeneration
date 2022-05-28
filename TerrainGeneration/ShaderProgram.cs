using System;
using System.Diagnostics;

using OpenTK.Graphics.OpenGL;

namespace TerrainGeneration
{
    /// <summary>
    /// Структура для OpenGL текстуры
    /// </summary>
    public struct Texture : IDisposable
    {
        public int Handle;

        public static implicit operator int(Texture program)
        {
            return program.Handle;
        }

        public static implicit operator Texture(int handle)
        {
            return new Texture()
            {
                Handle = handle
            };
        }

        public void Dispose()
        {
            Debug.WriteLine("Disposing Texture...");

            GL.DeleteTexture(Handle);
        }
    }

    /// <summary>
    /// Структура для OpenGL шейдеров
    /// </summary>
    public struct ShaderProgram : IDisposable
    {
        public int Handle;

        public static implicit operator int(ShaderProgram program)
        {
            return program.Handle;
        }

        public static implicit operator ShaderProgram(int handle)
        {
            return new ShaderProgram()
            {
                Handle = handle
            };
        }

        public void Dispose()
        {
            Debug.WriteLine("Disposing Shader...");

            GL.DeleteProgram(Handle);
        }
    }
}
