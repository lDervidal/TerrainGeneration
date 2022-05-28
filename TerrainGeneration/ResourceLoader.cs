using System;
using System.IO;
using System.Drawing;
using System.Drawing.Imaging;
using System.Diagnostics;

using OpenTK.Graphics.OpenGL;

namespace TerrainGeneration
{
    /// <summary>
    /// Класс загрузчика ресурсов для загрузки текстур и шейдеров
    /// </summary>
    public static class ResourceLoader
    {
        public static int LoadTextureFromFile(string filename)
        {
            // Подтвердить имя
            if (String.IsNullOrEmpty(filename))
                throw new ArgumentException(filename);

            // Создание и привязка новой 2D текстурки
            int id = GL.GenTexture();
            GL.BindTexture(TextureTarget.Texture2D, id);

            // Загрузить bitmap data
            Bitmap bmp = new Bitmap(filename);
            BitmapData bmp_data = bmp.LockBits(new Rectangle(0, 0, bmp.Width, bmp.Height), ImageLockMode.ReadOnly, System.Drawing.Imaging.PixelFormat.Format32bppArgb);

            // Копировать данные
            GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba, bmp_data.Width, bmp_data.Height, 0,
                OpenTK.Graphics.OpenGL.PixelFormat.Bgra, PixelType.UnsignedByte, bmp_data.Scan0);

            bmp.UnlockBits(bmp_data);

            // Генерация mipmaps
            GL.GenerateMipmap(GenerateMipmapTarget.Texture2D);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.LinearMipmapLinear);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);

            return id;
        }

        public static int LoadProgramFromFile(string vertexShaderSource, string fragmentShaderSource)
        {
            //На основе кода отсюда: http://www.opengl-tutorial.org/
            int vertexShaderHandle = GL.CreateShader(ShaderType.VertexShader);
            int fragmentShaderHandle = GL.CreateShader(ShaderType.FragmentShader);

            string strVertex;
            string strFragment;

            // Чтение файлов
            try
            {
                StreamReader file = new StreamReader(vertexShaderSource);
                strVertex = file.ReadToEnd();
                file.Close();

                file = new StreamReader(fragmentShaderSource);
                strFragment = file.ReadToEnd();
                file.Close();
            }
            catch (Exception e)
            {
                throw e;
            }

            string log;
            int status_code;

            // Скомпилировать вершинный шейдер
            Debug.WriteLine("Compiling " + vertexShaderSource + "...");
            GL.ShaderSource(vertexShaderHandle, strVertex);
            GL.CompileShader(vertexShaderHandle);
            GL.GetShaderInfoLog(vertexShaderHandle, out log);
            GL.GetShader(vertexShaderHandle, ShaderParameter.CompileStatus, out status_code);
            Debug.Write(log);

            if (status_code != 1)
                return 0;

            // Скомпилировать вершинный шейдер
            Debug.WriteLine("Compiling " + fragmentShaderSource + "...");
            GL.ShaderSource(fragmentShaderHandle, strFragment);
            GL.CompileShader(fragmentShaderHandle);
            GL.GetShaderInfoLog(fragmentShaderHandle, out log);
            GL.GetShader(fragmentShaderHandle, ShaderParameter.CompileStatus, out status_code);
            Debug.Write(log);

            if (status_code != 1)
                return 0;

            // Создание шейдера
            int shaderProgram = GL.CreateProgram();

            // Закрепить шейдеры
            GL.AttachShader(shaderProgram, vertexShaderHandle);
            GL.AttachShader(shaderProgram, fragmentShaderHandle);

            // Связка шейдеров
            Debug.WriteLine("Linking " + vertexShaderSource + " and " + fragmentShaderSource + "...");
            GL.LinkProgram(shaderProgram);

            // Вывод информации журнала программы
            GL.GetProgramInfoLog(shaderProgram, out log);
            Debug.Write(log);

            // Удалить использованные шейдеры
            GL.DeleteShader(vertexShaderHandle);
            GL.DeleteShader(fragmentShaderHandle);

            return shaderProgram;
        }
    }
}
