namespace TerrainGeneration
{
    public class Program
    {
        public const bool bLoadConfig = true;
        public const string ConfigFile = "Config\\Config.xml";

        static void Main(string[] args)
        {
            // Загрузить конфигурацию
            var options = ApplicationOptions.Default;
            if (bLoadConfig)
                options = ApplicationOptions.FromFile(ConfigFile);

            // Создание и запуск окна 
            var window = new RenderWindow(options);
            window.Run();

            System.Diagnostics.Debug.WriteLine("Closing Program...");
        }
    }
}