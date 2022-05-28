using System;
using System.Collections.Generic;

using OpenTK;

namespace TerrainGeneration
{
    /// <summary>
    /// Представляет сцену, которую можно отрендерить
    /// </summary>
    public class Scene : IDisposable
    {
        // Лист объектов
        public List<Entity> Entities = new List<Entity>();
        public List<IDisposable> Resources = new List<IDisposable>();

        public void Dispose()
        {
            foreach (var resource in Resources)
                resource.Dispose();
        }
    }

    /// <summary>
    /// Объект в сцене
    /// </summary>
    public class Entity
    {
        public Material EntityMaterial;
        public Mesh EntityMesh;
        public Matrix4 Transform;
    }
}
