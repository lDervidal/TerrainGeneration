using System;
using System.Diagnostics;
using OpenTK.Graphics.OpenGL;
using OpenTK;

namespace TerrainGeneration
{
    /// <summary>
    /// Класс, представляющий Mesh-объект, состоящий из нескольких буферов вершин и индексного буфера.
    /// </summary>
    public class Mesh : IDisposable
    {
        public VertexBuffer[] VertexAttributeBuffers;
        public IndexBuffer IndexBuffer;
        public bool IsIndexed;
        public PrimitiveType PrimitiveType;
        public int PrimitiveCount;

        /// <summary>
        /// Включить Mesh-объект для рендеринга
        /// </summary>
        public void Enable()
        {
            for (int i = 0, length = VertexAttributeBuffers.Length; i < length; ++i)
            {
                // Bind each attribute buffer
                GL.EnableVertexAttribArray(i);
                GL.BindBuffer(BufferTarget.ArrayBuffer, VertexAttributeBuffers[i].Handle);
                GL.VertexAttribPointer(i, VertexAttributeBuffers[i].ComponentsPerAttribute,
                    VertexAttributeBuffers[i].AttributeType, VertexAttributeBuffers[i].ShouldNormalize,
                    VertexAttributeBuffers[i].Stride, VertexAttributeBuffers[i].Offset);
            }

            // Привязать индексный буфер, если есть необходимость
            if (IsIndexed)
                GL.BindBuffer(BufferTarget.ElementArrayBuffer, IndexBuffer.Handle);
        }

        /// <summary>
        /// Нарисовать сетку (сначала должен быть активен)
        /// </summary>
        public void Draw()
        {
            if (IsIndexed)
                GL.DrawElements(PrimitiveType, PrimitiveCount, IndexBuffer.Type, 0);
            else
                GL.DrawArrays(PrimitiveType, 0, PrimitiveCount);
        }

        /// <summary>
        /// Отключить Mesh-объект
        /// </summary>
        public void Disable()
        {
            for (int i = 0, length = VertexAttributeBuffers.Length; i < length; ++i)
                GL.DisableVertexAttribArray(i);
        }

        /// <summary>
        /// Удалить буферы этого Mesh-объекта
        /// </summary>
        public void Dispose()
        {
            Debug.WriteLine("Disposing Mesh...");

            foreach (var vertBuffer in VertexAttributeBuffers)
                vertBuffer.Dispose();

            if (IsIndexed)
                IndexBuffer.Dispose();
        }
    }
}