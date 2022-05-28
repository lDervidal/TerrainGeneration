using System.Collections.Generic;
using System.Linq;
using System.Diagnostics;

using OpenTK.Graphics.OpenGL;
using OpenTK;

namespace TerrainGeneration
{
    /// <summary>
    /// Материал — это набор параметров для определенного шейдера
    /// </summary>
    public abstract class Material
    {
        /// <summary>
        /// The shader this material corresonds to
        /// </summary>
        public ShaderProgram Shader { get; set; }
        /// <summary>
        /// The world transform uniform of the shader
        /// </summary>
        public int WorldUniform { get; protected set; }
        /// <summary>
        /// The view projection uniform of the shader
        /// </summary>
        public int ViewProjectionUniform { get; protected set; }
        /// <summary>
        /// A list of textures this material uses
        /// </summary>
        public List<Texture> Textures = new List<Texture>();

        /// <summary>
        /// Сделайте этот материал активным и установите параметры шейдера
        /// </summary>
        public abstract void Apply();

        /// <summary>
        /// Установите параметр этого материала
        /// </summary>
        /// <param name="parameterName">The name of the parameter</param>
        /// <param name="value">The value to set it to</param>
        public abstract void SetParameter(string parameterName, object value);

        public Material(ShaderProgram shader)
        {
            Shader = shader;
            LocateTransformUniforms();
        }
        //!!!!!!
        public void LocateTransformUniforms()
        {
            WorldUniform = GL.GetUniformLocation(Shader, "WorldTransform");
            ViewProjectionUniform = GL.GetUniformLocation(Shader, "ViewProjectionTransform");

            if (WorldUniform < 0)
                Debug.WriteLine("Could not find WorldTransform uniform!");
            if (ViewProjectionUniform < 0)
                Debug.WriteLine("Could not find ViewProjection uniform!");
        }

        public void DisposeTextures()
        {
            foreach (var texture in Textures)
                texture.Dispose();
        }

        public void DisposeShader()
        {
            Shader.Dispose();
        }
    }

    /// <summary>
    /// Материал по умолчанию, абсолютно ничего не делает
    /// </summary>
    public class DefaultMaterial : Material
    {
        public DefaultMaterial(ShaderProgram program) : base(program)
        {
        }

        public override void Apply()
        {
        }

        public override void SetParameter(string parameterName, object value)
        {
        }
    }

    /// <summary>
    /// Текстурированный материал
    /// </summary>
    public class TerrainTextureMaterial : Material
    {
        /// <summary>
        /// Масштаб текстуры
        /// </summary>
        public Vector2 UVScale = Vector2.One;
        protected int uvScaleUniform;

        public TerrainTextureMaterial(ShaderProgram program) 
            : base(program)
        {
            uvScaleUniform = GL.GetUniformLocation(program, "UVScale");
            if (uvScaleUniform < 0)
                Debug.WriteLine("Could not find UVScale uniform!");
        }

        public TerrainTextureMaterial(ShaderProgram program, Texture texture)
            : this(program)
        {
            Textures.Add(texture);
        }

        public override void Apply()
        {
            // Привязать текстуру
            GL.ActiveTexture(TextureUnit.Texture0);
            GL.BindTexture(TextureTarget.Texture2D, Textures[0]);

            // Установить масштаб
            GL.Uniform2(uvScaleUniform, ref UVScale);
        }

        public override void SetParameter(string parameterName, object value)
        {
            if (parameterName == "UVScale")
                UVScale = (Vector2)value;
        }
    }

    /// <summary>
    /// Материал с несколькими текстурами
    /// </summary>
    public class TerrainMultiTextureMaterial : TerrainTextureMaterial
    {
        public float MinTerrainHeight { get; set; }
        public float MaxTerrainHeight { get; set; }

        protected int minTerrainHeightUniform = -1;
        protected int maxTerrainHeightUniform = -1;
        protected int[] samplerUniformLocations;

        public TerrainMultiTextureMaterial(ShaderProgram shader, string[] samplerUniforms, Texture[] textures)
            : base(shader)
        {
            Textures.AddRange(textures);

            minTerrainHeightUniform = GL.GetUniformLocation(shader, "MinTerrainHeight");
            maxTerrainHeightUniform = GL.GetUniformLocation(shader, "MaxTerrainHeight");

            if (minTerrainHeightUniform < 0 || maxTerrainHeightUniform < 0)
                Debug.WriteLine("Could not find min/max terrain height uniform!");

            samplerUniformLocations = (from samplerUniform in samplerUniforms
                                      select GL.GetUniformLocation(shader, samplerUniform)).ToArray();

            if (samplerUniformLocations.Contains(-1))
                Debug.WriteLine("Failed to find a sampler uniform!");
        }

        public override void Apply()
        {
            for (int i = 0; i < Textures.Count; ++i)
            {
                GL.ActiveTexture(TextureUnit.Texture0 + i);
                GL.BindTexture(TextureTarget.Texture2D, Textures[i]);
                GL.Uniform1(samplerUniformLocations[i], i);
            }

            GL.Uniform1(minTerrainHeightUniform, MinTerrainHeight);
            GL.Uniform1(maxTerrainHeightUniform, MaxTerrainHeight);
            GL.Uniform2(uvScaleUniform, ref UVScale);
        }

        public override void SetParameter(string parameterName, object value)
        {
            switch (parameterName)
            {
                case "MinTerrainHeight":
                    MinTerrainHeight = (float)value;
                    break;

                case "MaxTerrainHeight":
                    MaxTerrainHeight = (float)value;
                    break;

                default:
                    base.SetParameter(parameterName, value);
                    break;
            }
        }
    }
}
