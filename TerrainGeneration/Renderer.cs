using System;
using System.Linq;
using System.Drawing;

using OpenTK;
using OpenTK.Graphics.OpenGL;

namespace TerrainGeneration
{
    public class Renderer : IDisposable
    {
        /// <summary>
        /// Получает или устанавливает камеру
        /// </summary>
        public Camera Camera { get; set; }

        /// <summary>
        /// Получает или задает использование ландшафта
        /// </summary>
        public bool UseWireframe
        {
            get { return bUseWireframe; }
            set
            {
                bUseWireframe = value;
                GL.PolygonMode(MaterialFace.FrontAndBack, (bUseWireframe ? PolygonMode.Line : PolygonMode.Fill));
            }
        }

        /// <summary>
        /// Получает размер пользовательской области
        /// </summary>
        public Size ClientSize
        {
            get { return clientSize; }
        }

        protected Size clientSize;
        protected int vertexArray = -1;
        protected bool bUseWireframe = false;

        public void Initialize(RenderWindow window)
        {
            clientSize = window.ClientSize;

            // Создание камеры
            Camera = new TerrainGeneration.Camera(clientSize.Width, clientSize.Height);

            // Очищение и смена цвета
            GL.ClearColor(Color.CornflowerBlue);

            // Создание и привязка массива вершин
            vertexArray = GL.GenVertexArray();
            GL.BindVertexArray(vertexArray);

            // Включение необходимых функций
            GL.Enable(EnableCap.Texture2D);
            GL.Enable(EnableCap.DepthTest);
            GL.Enable(EnableCap.AlphaTest);
            GL.Enable(EnableCap.Blend);
            GL.Enable(EnableCap.CullFace);
            GL.PolygonMode(MaterialFace.FrontAndBack, (bUseWireframe ? PolygonMode.Line : PolygonMode.Fill));
        }

        public void OnResize(Size ClientSize)
        {
            // Размер окна изменен, замена размера области просмотра на пользовательский
            clientSize = ClientSize;
            GL.Viewport(ClientSize);
        }

        public void OnUpdateFrame(FrameEventArgs e)
        {

        }

        public void Render(FrameEventArgs e, Scene scene)
        {
            // Очистить буфер
            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

            Matrix4 Projection;
            Matrix4 View;

            // Получение обзора и проекцию матриц
            Camera.GetProjection(out Projection, clientSize.Width, clientSize.Height);
            Camera.GetView(out View);

            // Соединение обзора и проекции матриц
            Matrix4 ViewProj = View * Projection;

            // Визуализация всех объектов в сцене
            if (scene != null)
            {
                // Группировка по шейдеру
                var shaderIterator = scene.Entities.GroupBy(t => t.EntityMaterial.Shader);

                foreach (var shaderGroup in shaderIterator)
                {
                    // Смена шейдера
                    GL.UseProgram(shaderGroup.Key);

                    // Группировка по материалу
                    var materialIterator = shaderGroup.GroupBy(t => t.EntityMaterial);

                    foreach (var materialGroup in materialIterator)
                    {
                        // Смена материала если потребуется
                        materialGroup.Key.Apply();
                        GL.UniformMatrix4(materialGroup.Key.ViewProjectionUniform, false, ref ViewProj);

                        // Группировка по mesh-объектам
                        var meshIterator = materialGroup.GroupBy(t => t.EntityMesh);

                        foreach (var meshGroup in meshIterator)
                        {
                            // Отображение mesh-объекта
                            meshGroup.Key.Enable();

                            foreach (var entity in meshGroup)
                            {
                                // Визуализация
                                RenderEntity(entity, ref ViewProj);
                            }

                            // Отключить mesh-объект
                            meshGroup.Key.Disable();
                        }
                    }
                }
            }
        }

        public void RenderEntity(Entity entity, ref Matrix4 ViewProj)
        {
            // Установить изменения
            GL.UniformMatrix4(entity.EntityMaterial.WorldUniform, false, ref entity.Transform);

            // Отрисовка mesh-объесктв
            entity.EntityMesh.Draw();
        }

        public void Dispose()
        {
            GL.DeleteVertexArray(vertexArray);
        }
    }
}
