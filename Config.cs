using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ConceptPHIRegex;

public class ConfigData
{
    public string PreviousLocation { get; set; } = string.Empty;
    public string SaveLocation { get; set; } = string.Empty;
    public string SaveFilename { get; set; } = "answer.txt";
    public string EditorLocation { get; set; } = Config.GetEditorByPlatform();
    public IEnumerable<string> ValidateFileLocations { get; set; } = [];
    public string ValidateResultLocation { get; set; } = string.Empty;
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

    internal static string GetEditorByPlatform()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            return "notepad.exe";
        }

        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            return "/Applications/Visual Studio Code.app/Contents/Resources/app/bin";
        }

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            return "vi";
        }

        throw new NotImplementedException("The platform is unsupported");
    }
}
