using System;
using OpenTK.Graphics.OpenGL;

namespace TerrainGeneration
{
    /// <summary>
    ///Структура данных для индексного буфера OpenGL
    /// </summary>

    public struct IndexBuffer : IDisposable
    {
        public int Handle;
        public DrawElementsType Type;
    
        public IndexBuffer(int handle, DrawElementsType type)
        {
            Handle = handle;
            Type = type;
        }
        /// <summary>
        /// Удаление буфера
        /// </summary>
        public void Dispose()
        {
            GL.DeleteBuffer(Handle);
        }
    }
}