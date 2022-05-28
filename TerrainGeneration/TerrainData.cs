using System;
using System.Collections.Generic;
using System.Linq;
using System.Drawing;
using System.Diagnostics;

using OpenTK;
using OpenTK.Graphics.OpenGL;

namespace TerrainGeneration
{
    /// <summary>
    /// Варианты создания сетки из данных о местности
    /// </summary>
    public struct MeshCreationOptions
    {
        /// <summary>
        /// Должна ли созданная сетка иметь нормальные данные?
        /// </summary>
        public bool bCreateNormals;

        /// <summary>
        /// Должна ли созданная сетка использовать треугольные полоски?
        /// </summary>
        public bool bUseTriangleStrips;

        /// <summary>
        /// Границы места создания сетки
        /// </summary>
        public Nullable<Rectangle> MeshBounds;

        public static MeshCreationOptions Default
        {
            get
            {
                return new MeshCreationOptions()
                {
                    bCreateNormals = true,
                    bUseTriangleStrips = true,
                    MeshBounds = null
                };
            }
        }
    }

    /// <summary>
    /// Класс, который используется для хранения необработанных данных ландшафта, может быть преобразован в сетку.
    /// </summary>
    public class TerrainData
    {
        public Vector3[] VertexPositions { get; protected set; }
        public int DataSizeX { get; protected set; }
        public int DataSizeZ { get; protected set; }

        /// <summary>
        /// Минимальная высота данных
        /// </summary>
        public float MinHeight
        {
            get
            {
                return VertexPositions.Min(v => v.Y);
            }
        }

        /// <summary>
        /// Максимальная высота данных
        /// </summary>
        public float MaxHeight
        {
            get
            {
                return VertexPositions.Max(v => v.Y);
            }
        }

        protected Vector3[] normalComputionArray;

        public Vector3 this[int x, int z]
        {
            get
            {
                return VertexPositions[x + z * DataSizeX];
            }
            set
            {
                VertexPositions[x + z * DataSizeX] = value;
            }
        }

        /// <summary>
        /// Получить нормаль определенного лица в данных
        /// </summary>
        /// <param name="x">The x position of the face</param>
        /// <param name="z">The z position of the face</param>
        /// <returns>The normal of the face</returns>
        public Vector3 GetFaceNormal(int x, int z)
        {
            if (x >= 0 && x < DataSizeX - 1 && z >= 0 && z < DataSizeZ - 1)
            {
                var v12 = this[x, z + 1] - this[x, z];
                var v13 = this[x + 1, z] - this[x, z];
                var v24 = this[x + 1, z + 1] - this[x, z + 1];
                var v34 = this[x + 1, z + 1] - this[x + 1, z];

                // Надеюсь, это даст нам нормальный вид((
                return Vector3.Cross(v12, v13) + Vector3.Cross(v12, v24) + Vector3.Cross(v34, v13) + Vector3.Cross(v34, v24);
            }
            else
                return Vector3.Zero;
        }

        /// <summary>
        /// Получить нормаль конкретной вершины в данных
        /// </summary>
        /// <param name="x">The x position of the vertex</param>
        /// <param name="z">The z position of the vertex</param>
        /// <returns>The normal of the vertex</returns>
        public Vector3 GetVertexNormal(int x, int z)
        {
            var normal = Vector3.Zero;
            var vertexPos = this[x, z];
            var zDif = Vector3.Zero;
            var xDif = Vector3.Zero;

            if (x > 0 && z > 0)
            {
                xDif = vertexPos - this[x - 1, z];
                zDif = vertexPos - this[x, z - 1];
                normal += Vector3.Normalize(Vector3.Cross(zDif, xDif));
            }

            if (x < DataSizeX - 1 && z > 0)
            {
                xDif = this[x + 1, z] - vertexPos;
                zDif = vertexPos - this[x, z - 1];
                normal += Vector3.Normalize(Vector3.Cross(zDif, xDif));
            }

            if (x > 0 && z < DataSizeZ - 1)
            {
                xDif = vertexPos - this[x - 1, z];
                zDif = this[x, z + 1] - vertexPos;
                normal += Vector3.Normalize(Vector3.Cross(zDif, xDif));
            }

            if (x < DataSizeX - 1 && z < DataSizeZ - 1)
            {
                xDif = this[x + 1, z] - vertexPos;
                zDif = this[x, z + 1] - vertexPos;
                normal += Vector3.Normalize(Vector3.Cross(zDif, xDif));
            }

            return normal;
        }

        public int GetIndex(int x, int z)
        {
            return x + z * DataSizeX;
        }

        public int GetIndex(int x, int z, int dataSizeX)
        {
            return x + z * dataSizeX;
        }

        public TerrainData(Vector3[] vertexPositions, int dataSizeX, int dataSizeZ)
        {
            VertexPositions = vertexPositions;
            DataSizeX = dataSizeX;
            DataSizeZ = dataSizeZ;
        }

        public static TerrainData CreateFlatTerrain(int sizeX, int sizeZ, Vector2 cellSize)
        {
            var data = from z in Enumerable.Range(0, sizeX)
                       from x in Enumerable.Range(0, sizeZ)
                       select new Vector3((float)x * cellSize.X, 0.0f, (float)z * cellSize.Y);

            return new TerrainData(data.ToArray(), sizeX, sizeZ);
        }

        public static TerrainData CreateGaussian(int sizeX, int sizeZ, Vector2 cellSize, float magnitude, Vector2 centerPosition, float standardDev)
        {
            var data = from z in Enumerable.Range(0, sizeX)
                       from x in Enumerable.Range(0, sizeZ)
                       let vecPosition = new Vector2((float)x * cellSize.X, (float)z * cellSize.Y)
                       select new Vector3(vecPosition.X, magnitude * (float)Math.Exp(-(centerPosition - vecPosition).LengthSquared / (2.0f * standardDev * standardDev)), vecPosition.Y);

            return new TerrainData(data.ToArray(), sizeX, sizeZ);
        }

        public Mesh CreateMesh()
        {
            return CreateMesh(MeshCreationOptions.Default);
        }

        /// <summary>
        /// Создание сетки из данных о местности
        /// </summary>
        /// <param name="options">Options for mesh creation</param>
        /// <returns></returns>
        public Mesh CreateMesh(MeshCreationOptions options)
        {
            var beginX = (options.MeshBounds.HasValue ? options.MeshBounds.Value.X : 0);
            var beginZ = (options.MeshBounds.HasValue ? options.MeshBounds.Value.Y : 0);
            var countX = (options.MeshBounds.HasValue ? options.MeshBounds.Value.Width : DataSizeX);
            var countZ = (options.MeshBounds.HasValue ? options.MeshBounds.Value.Height : DataSizeZ);

            var positionVertexBuffer = new VertexBuffer(GL.GenBuffer(), 3, VertexAttribPointerType.Float);
            var positionIndexBuffer = new IndexBuffer(GL.GenBuffer(), DrawElementsType.UnsignedShort);
            var vertexAttributeBuffers = new List<VertexBuffer>();

            short[] indicies = null;

            if (!options.bUseTriangleStrips)
            {
                // Смещения вершин для граней
                var vertexOffsets = new[] 
                { 
                    new[] { 0, 0 }, 
                    new[] { 0, 1 }, 
                    new[] { 1, 0 }, 
                    new[] { 1, 0 }, 
                    new[] { 0, 1 }, 
                    new[] { 1, 1 }
                };

                // Создание индексново массива
                indicies = (from z in Enumerable.Range(0, countZ - 1)
                            from x in Enumerable.Range(0, countX - 1)
                            from offset in vertexOffsets
                            select (short)GetIndex(x + offset[0], z + offset[1], countX)).ToArray();
            }
            else
            {
                // Построение треугольных полос
                List<short> tempIndicies = new List<short>();

                // Это генерирует перевернутые треугольники, поэтому нужно изменить топологию на противоположную.
                for (int z = 0; z < countZ - 1; ++z)
                {
                    tempIndicies.Add((short)GetIndex(0, z, countX));
                    tempIndicies.AddRange(from x in Enumerable.Range(0, countX)
                                          from dz in Enumerable.Range(0, 2)
                                          select (short)GetIndex(x, z + dz, countX));
                    tempIndicies.Add((short)GetIndex(countX - 1, z + 1, countX));
                }

                // Для разворота треугольной полосы нам потребуется нечетное число вершин
                if (tempIndicies.Count % 2 == 0)
                    tempIndicies.Add((short)GetIndex(countX - 1, countZ - 1, countX));
                tempIndicies.Reverse();

                // Создание индексного массива
                indicies = tempIndicies.ToArray();
            }

            var vertexBufferData = VertexPositions;
            if (options.MeshBounds.HasValue)
            {
                vertexBufferData = (from z in Enumerable.Range(beginZ, countZ)
                                    from x in Enumerable.Range(beginX, countX)
                                    select this[x, z]).ToArray();
            }

            // Загрузка данных буфера
            GL.BindBuffer(BufferTarget.ArrayBuffer, positionVertexBuffer.Handle);
            GL.BufferData(BufferTarget.ArrayBuffer, (IntPtr)(Vector3.SizeInBytes * vertexBufferData.Length), vertexBufferData, BufferUsageHint.StaticDraw);

            GL.BindBuffer(BufferTarget.ElementArrayBuffer, positionIndexBuffer.Handle);
            GL.BufferData(BufferTarget.ElementArrayBuffer, (IntPtr)(sizeof(short) * indicies.Length), indicies, BufferUsageHint.StaticDraw);

            // Добавляем это в буферы атрибутов
            vertexAttributeBuffers.Add(positionVertexBuffer);

            // Создать обычные данные?
            if (options.bCreateNormals)
            {
                var normalsData = new Vector3[countX * countZ];

                // Вычисляем нормали по средневзвешенному значению соседних граней (с 3 раза написал правильно средневзвешенному)
                for (int z = 0; z < countZ; ++z)
                    for (int x = 0; x < countX; ++x)
                    {
                        var normal = GetVertexNormal(x + beginX, z + beginZ);
                        normal.Normalize();
                        normalsData[x + z * countX] = normal;
                    }

                // Загрузить данные в буфер OpenGL
                var normalVertexBuffer = new VertexBuffer(GL.GenBuffer(), 3, VertexAttribPointerType.Float);
                GL.BindBuffer(BufferTarget.ArrayBuffer, normalVertexBuffer.Handle);
                GL.BufferData(BufferTarget.ArrayBuffer, (IntPtr)(Vector3.SizeInBytes * normalsData.Length), normalsData, BufferUsageHint.StaticDraw);

                // Добавление буфера нормалей к буферам атрибутов
                vertexAttributeBuffers.Add(normalVertexBuffer);
            }

            Debug.WriteLine("Created Terrain Mesh Chunk...");
            Debug.WriteLine("Primitive Type: " + (options.bUseTriangleStrips ? PrimitiveType.TriangleStrip : PrimitiveType.Triangles));
            Debug.WriteLine("Vertex Count: " + vertexBufferData.Length);
            Debug.WriteLine("Index Count: " + indicies.Length);

            // Создание сетки
            return new Mesh()
            {
                IsIndexed = true,
                IndexBuffer = positionIndexBuffer,
                PrimitiveCount = indicies.Length,
                PrimitiveType = options.bUseTriangleStrips ? PrimitiveType.TriangleStrip : PrimitiveType.Triangles,
                VertexAttributeBuffers = vertexAttributeBuffers.ToArray()
            };
        }

        public Mesh[] CreateMeshChunks(int chunkSize)
        {
            return CreateMeshChunks(chunkSize, MeshCreationOptions.Default);
        }
        
        public Mesh[] CreateMeshChunks(int chunkSize, MeshCreationOptions options)
        {
            var chunkCountX = (DataSizeX - 1) / chunkSize;
            var chunkCountZ = (DataSizeZ - 1) / chunkSize;
            
            if (chunkCountX * chunkSize < DataSizeX - 1)
                chunkCountX++;
            if (chunkCountZ * chunkSize < DataSizeZ - 1)
                chunkCountZ++;

            var chunks = new Mesh[chunkCountX * chunkCountZ];

            for (int z = 0; z < chunkCountZ; ++z)
            {
                for (int x = 0; x < chunkCountX; ++x)
                {
                    var beginX = x * chunkSize;
                    var beginZ = z * chunkSize;
                    var width = Math.Min(chunkSize + 1, DataSizeX - beginX);
                    var height = Math.Min(chunkSize + 1, DataSizeZ - beginZ);

                    options.MeshBounds = new Rectangle(beginX, beginZ, width, height);
                    chunks[x + chunkCountX * z] = CreateMesh(options);
                }
            }

            return chunks;
        }
    }
}