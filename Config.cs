using System.Text.Json;
using System.Text.Json.Serialization;

namespace ConceptPHIRegex;

public class ConfigData
{
    public string PreviousLocation { get; set; } = string.Empty;
    public string SaveLocation { get; set; } = string.Empty;
    public string SaveFilename { get; set; } = "answer.txt";
    public string EditorLocation { get; set; } = "notepad.exe";
    public IEnumerable<string> ValidateFileLocations { get; set; } = [];
}

[JsonSourceGenerationOptions(WriteIndented = true)]
[JsonSerializable(typeof(ConfigData))]
[JsonSerializable(typeof(string))]
internal partial class SourceGenerationContext : JsonSerializerContext;


internal class Config
{
    internal static ConfigData AppConfig = new();

    internal static void ReadConfig()
    {
        if (!File.Exists("config.json"))
        {
            //If config not exist, create one
            WriteConfig();
        }
        using StreamReader ConfigReader = new("config.json");
        ConfigData? SerializedJson = JsonSerializer.Deserialize(ConfigReader.ReadToEnd(), SourceGenerationContext.Default.ConfigData);
        if (SerializedJson != null)
        {
            AppConfig = SerializedJson;
        }
        ConfigReader.Close();
    }

    internal static void WriteConfig()
    {
        using StreamWriter ConfigWriter = new("config.json");
        ConfigWriter.WriteLine(JsonSerializer.Serialize(AppConfig, SourceGenerationContext.Default.ConfigData));
        ConfigWriter.Close();
    }
}
