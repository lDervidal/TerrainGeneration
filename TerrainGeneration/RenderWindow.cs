﻿using System;
using System.Drawing;
using System.Xml.Linq;

using System.Diagnostics;
using OpenTK;
using OpenTK.Input;

namespace TerrainGeneration
{
    /// <summary>
    /// Список различных типов визуализации
    /// </summary>
    public enum RenderMode
    {
        NoTexture,
        Textured,
        Multitextured
    }

    /// <summary>
    /// Опции приложения
    /// </summary>
    public class ApplicationOptions
    {
        public RenderMode TerrainRenderMode { get; set; }
        public Vector2 UVScale { get; set; }
        public float ErrorConstant { get; set; }
        public float MaxSeedHeight { get; set; }
        public int Iterations { get; set; }
        public Vector3 CellSize { get; set; }
        public bool Fullscreen { get; set; }
        public int ChuckSize { get; set; }

        /// <summary>
        /// Генерация ландшафта по умолчанию
        /// </summary>
        public static ApplicationOptions SmallTerrain
        {
            get
            {
                return new ApplicationOptions()
                {
                    TerrainRenderMode = RenderMode.Multitextured,
                    UVScale = new Vector2(1f / 64f, 1f / 64f),
                    ErrorConstant = 1.5f,
                    MaxSeedHeight = 70f,
                    Iterations = 7,
                    CellSize = new Vector3(4f, 1f, 4f),
                    Fullscreen = false,
                    ChuckSize = 128
                };
            }
        }

        /// <summary>
        /// Генерация большого ландшафта
        /// </summary>
        public static ApplicationOptions Default
        {
            get
            {
                return new ApplicationOptions()
                {
                    TerrainRenderMode = RenderMode.Multitextured,
                    UVScale = new Vector2(1f / 64f, 1f / 64f),
                    ErrorConstant = 0.5f,
                    MaxSeedHeight = 100f,
                    Iterations = 8,
                    CellSize = new Vector3(2f, 1f, 2f),
                    Fullscreen = false,
                    ChuckSize = 128
                };
            }
        }

        /// <summary>
        /// Загрузить параметры из XML-файла
        /// </summary>
        /// <param name="filename">XML-файл для загрузки</param>
        /// <returns>Параметры приложения</returns>
        
        // Взято с гитхаба (ссылку уже не найти)
        public static ApplicationOptions FromFile(string filename)
        {
            try
            {
                var document = XDocument.Load(filename);
                var root = document.Element("ApplicationOptions");
                var preset = root.Element("Preset");

                // If preset specified, then use that
                if (preset.Value == "Default")
                    return ApplicationOptions.Default;
                else if (preset.Value == "SmallTerrain")
                    return ApplicationOptions.SmallTerrain;

                // Otherwise copy in the values specified
                var options = new ApplicationOptions();

                var renderMode = root.Element("RenderMode").Attribute("value");
                if (renderMode.Value == "Multitextured")
                    options.TerrainRenderMode = RenderMode.Multitextured;
                else if (renderMode.Value == "Textured")
                    options.TerrainRenderMode = RenderMode.Textured;
                else
                    options.TerrainRenderMode = RenderMode.NoTexture;

                var invUVScale = root.Element("InverseUVScale");
                options.UVScale = new Vector2(1.0f / Single.Parse(invUVScale.Attribute("x").Value),
                    1.0f / Single.Parse(invUVScale.Attribute("y").Value));
                options.ErrorConstant = Single.Parse(root.Element("ErrorConstant").Attribute("value").Value);
                options.MaxSeedHeight = Single.Parse(root.Element("MaxSeedHeight").Attribute("value").Value);
                options.Iterations = Int32.Parse(root.Element("Iterations").Attribute("value").Value);

                var cellSize = root.Element("CellSize");
                options.CellSize = new Vector3(Single.Parse(cellSize.Attribute("x").Value),
                    Single.Parse(cellSize.Attribute("y").Value),
                    Single.Parse(cellSize.Attribute("z").Value));

                options.Fullscreen = bool.Parse(root.Element("Fullscreen").Attribute("value").Value);
                options.ChuckSize = Int32.Parse(root.Element("ChunkSize").Attribute("value").Value);

                return options;
            }
            catch (Exception e)
            {
                Debug.Write("Error Parsing XML: ");
                Debug.WriteLine(e.Message);
                return ApplicationOptions.Default;
            }
        }
    }

    /// <summary>
    /// Класс окна для отображения результатов рендеринга
    /// </summary>
    public class RenderWindow : GameWindow
    {
        public Renderer Renderer { get; protected set; }
        public Scene Scene { get; protected set; }
        public CameraController CameraController { get; protected set; }
        public ApplicationOptions Options;

        /// <summary>
        /// Установите флаг в значение true, когда ландшафт необходимо регенерировать
        /// </summary>
        protected bool bRegenerateTerrain = false;

        protected ShaderProgram TerrainShader { get; set; }
        protected Material TerrainMaterial { get; set; }
        protected Size HeightMapSize { get; set; }

        public RenderWindow(ApplicationOptions options)
            : base(800, 600, new OpenTK.Graphics.GraphicsMode(new OpenTK.Graphics.ColorFormat(8), 24, 0, 2))
        {
            Options = options;
            Title = "Terrain Generation Project";
            Icon = Properties.Resources.ProgramIcon;

            if (Options.Fullscreen)
                WindowState = OpenTK.WindowState.Fullscreen;
        }

        public RenderWindow() : this(ApplicationOptions.Default)
        {

        }

        protected virtual Renderer CreateRenderer()
        {
            return new Renderer();
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);

            Keyboard.KeyDown += OnKeyDown;

            // Создание визуализатора
            Debug.WriteLine("Initializing Renderer...");
            Renderer = CreateRenderer();
            Renderer.Initialize(this);

            // Создание сцены для визуализации
            Debug.WriteLine("Creating Scene...");
            CreateScene();
        }

        protected void OnKeyDown(object obj, KeyboardKeyEventArgs args)
        {
            // Переключить полигон
            if (args.Key == Key.Tilde)
                if (Renderer != null)
                    Renderer.UseWireframe = !Renderer.UseWireframe;

            // Регенерировать местность
            if (args.Key == Key.G)
                bRegenerateTerrain = true;

            // Переключатель типа камеры
            if (args.Key == Key.C)
            {
                // Переключиться с камеры вращения на камеру от первого лица
                if (CameraController is RotationCameraController)
                {
                    CameraController.Dispose();
                    CameraController = new FirstPersonCameraController(Renderer.Camera, Keyboard, Mouse);
                }

                // Переключиться с камеры от первого лица на камеру вращения
                else if (CameraController is FirstPersonCameraController)
                {
                    var cellSize = Options.CellSize;
                    CameraController.Dispose();
                    CameraController = new RotationCameraController(Renderer.Camera, Keyboard, Mouse)
                    {
                        CameraCenter = new Vector3(cellSize.X * (float)HeightMapSize.Width / 2f, 0f, cellSize.Z * (float)HeightMapSize.Height / 2f),
                        Phi = (float)Math.PI / 4f,
                        Radius = cellSize.X * (float)HeightMapSize.Width / 2f + cellSize.Z * (float)HeightMapSize.Height / 2f
                    };
                }

                // Обновление параметров камеры
                CameraController.UpdateCameraParams();
            }
        }

        protected override void OnResize(EventArgs e)
        {
            // Если размер экрана изменился
            Renderer.OnResize(ClientSize);

            base.OnResize(e);
        }

        /// <summary>
        /// Загрузка материалов и текстур для объектов ландшафта.
        /// </summary>
        /// <param name="terrainData">The terrain data</param>
        /// <returns>A material for the terrain</returns>
        protected virtual Material LoadTerrainMaterial(TerrainData terrainData)
        {
            // Загрузка шейдера по умолчанию
            ShaderProgram terrainShader = -1;
            Material terrainMaterial = null;

            // Загрузка материалов
            switch (Options.TerrainRenderMode)
            {
                case RenderMode.Multitextured:
                    {
                        // Загрузка текстур
                        Texture grassTexture = ResourceLoader.LoadTextureFromFile("Textures\\Grass.jpg");
                        Texture snowTexture = ResourceLoader.LoadTextureFromFile("Textures\\Snow.jpg");
                        Texture dirtTexture = ResourceLoader.LoadTextureFromFile("Textures\\Dirt.jpg");
                        Texture rockTexture = ResourceLoader.LoadTextureFromFile("Textures\\Rock.jpg");

                        // Загрузка материалов
                        terrainShader = ResourceLoader.LoadProgramFromFile("Shaders\\TerrainMultiTextured.vert", "Shaders\\TerrainMultiTextured.frag");

                        var textureArray = new[] { grassTexture, snowTexture, dirtTexture, rockTexture };
                        var samplerUniforms = new[] { "grassSampler", "snowSampler", "dirtSampler", "rockSampler" };

                        terrainMaterial = new TerrainMultiTextureMaterial(terrainShader, samplerUniforms, textureArray)
                        {
                            UVScale = Options.UVScale,
                            MaxTerrainHeight = terrainData.MaxHeight,
                            MinTerrainHeight = terrainData.MinHeight
                        };
                    }
                    break;

                case RenderMode.Textured:
                    {
                        // Загрузка текстур
                        Texture grassTexture = ResourceLoader.LoadTextureFromFile("Textures\\Grass.jpg");

                        // Загрузка материалов
                        terrainShader = ResourceLoader.LoadProgramFromFile("Shaders\\TerrainTextured.vert", "Shaders\\TerrainTextured.frag");
                        terrainMaterial = new TerrainTextureMaterial(terrainShader, grassTexture)
                        {
                            UVScale = Options.UVScale
                        };
                    }
                    break;

                case RenderMode.NoTexture:
                    {
                        // Загрузка дефолтных материалов
                        terrainShader = ResourceLoader.LoadProgramFromFile("Shaders\\Terrain.vert", "Shaders\\Terrain.frag");
                        terrainMaterial = new DefaultMaterial(terrainShader);
                    }
                    break;
            }

            return terrainMaterial;
        }

        protected virtual void CreateTerrainChunks(TerrainData terrainData, Material terrainMaterial)
        {
            // Генерация чанков
            var terrainChunks = terrainData.CreateMeshChunks(Options.ChuckSize, MeshCreationOptions.Default);

            foreach (var chunk in terrainChunks)
            {
                // Создаение чанка установленного по умолчанию
                var terrainChunkEntity = new Entity()
                {
                    EntityMesh = chunk,
                    Transform = Matrix4.Identity,
                    EntityMaterial = terrainMaterial
                };

                // Добавить ресурсы в сцену (автоматическая очистка)
                Scene.Resources.Add(chunk);
                Scene.Entities.Add(terrainChunkEntity);
            }
        }

        /// <summary>
        /// Создание сцены для рендеринга
        /// </summary>
        protected virtual void CreateScene()
        {
            // Очиська сцены (если потребуется)
            if (Scene != null)
            {
                Scene.Dispose();
                Scene = null;
            }

            // Создание сцены
            Scene = new Scene();
            var cellSize = Options.CellSize;

            // Создание данных mesh-объекта
            Debug.WriteLine("Creating Height Data...");
            var heightMap = DiamondSquare.GenerateRandom(Options.ErrorConstant, Options.MaxSeedHeight, Options.Iterations);
            var terrainData = heightMap.ToTerrainData(cellSize);

            // Загрузить материалы, если необходимо
            if (TerrainMaterial == null)
            {
                Debug.WriteLine("Loading Materials...");
                TerrainMaterial = LoadTerrainMaterial(terrainData);
            }
            else
            {
                // Убедитесь, что материал знает новую минимальную/максимальную высоту
                TerrainMaterial.SetParameter("MinTerrainHeight", terrainData.MinHeight);
                TerrainMaterial.SetParameter("MaxTerrainHeight", terrainData.MaxHeight);
            }

            // Создание данных mesh-объекта
            Debug.WriteLine("Creating Mesh Data...");
            CreateTerrainChunks(terrainData, TerrainMaterial);

            HeightMapSize = new System.Drawing.Size(heightMap.Width, heightMap.Height);

            // Создание и расположение контроллера камеры
            CameraController = new RotationCameraController(Renderer.Camera, Keyboard, Mouse)
            {
                CameraCenter = new Vector3(cellSize.X * (float)HeightMapSize.Width / 2f, 0f, cellSize.Z * (float)HeightMapSize.Height / 2f),
                Phi = (float)Math.PI / 4f,
                Radius = cellSize.X * (float)HeightMapSize.Width / 2f + cellSize.Z * (float)HeightMapSize.Height / 2f
            };

            CameraController.UpdateCameraParams();
        }

        protected override void OnUpdateFrame(FrameEventArgs e)
        {
            // Закройте приложение, если пользователь нажмет escape
            if (Keyboard[Key.Escape])
                Close();

            // Регенерация рельефа
            if (bRegenerateTerrain)
            {
                bRegenerateTerrain = false;
                CreateScene();
            }

            // Обновление камеры
            if (CameraController != null)
                CameraController.UpdateCamera(e);

            // Общновление рендера
            Renderer.OnUpdateFrame(e);
        }

        protected override void OnRenderFrame(FrameEventArgs e)
        {
            // Визуализация сцены
            Renderer.Render(e, Scene);

            SwapBuffers();
        }

        protected override void OnClosed(EventArgs e)
        {
            // Уничтожение контроллера камеры
            if (CameraController != null)
                CameraController.Dispose();

            Debug.WriteLine("Disposing Resources...");

            // Уничтожение материалов
            if (TerrainMaterial != null)
            {
                TerrainMaterial.DisposeTextures();
                TerrainMaterial.DisposeShader();
            }

            // Уничтожение всех оставшихся ресурсов
            Scene.Dispose();
            Renderer.Dispose();
            Renderer = null;

            base.OnClosed(e);
        }
    }
}
