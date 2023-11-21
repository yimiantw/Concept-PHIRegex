using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ConceptPHIRegex;

public class ConfigData
{
    public class CustomRegexConfigClass
    {
        public class CustomPatternsClass
        {
            public IEnumerable<string> IDNumber { get; set; } = [];
            public IEnumerable<string> MedicalRecord { get; set; } = [];
            public IEnumerable<string> PatientName { get; set; } = [];
            public IEnumerable<string> Doctor { get; set; } = [];
            public IEnumerable<string> Username { get; set; } = [];
            public IEnumerable<string> Profession { get; set; } = [];
            public IEnumerable<string> Department { get; set; } = [];
            public IEnumerable<string> Hospital { get; set; } = [];
            public IEnumerable<string> Organization { get; set; } = [];
            public IEnumerable<string> Street { get; set; } = [];
            public IEnumerable<string> City { get; set; } = [];
            public IEnumerable<string> State { get; set; } = [];
            public IEnumerable<string> Country { get; set; } = [];
            public IEnumerable<string> Zip { get; set; } = [];
            public IEnumerable<string> LocationOther { get; set; } = [];
            public IEnumerable<string> Age { get; set; } = [];
            public IEnumerable<string> Date { get; set; } = [];
            public IEnumerable<string> Time { get; set; } = [];
            public IEnumerable<string> Duration { get; set; } = [];
            public IEnumerable<string> Set { get; set; } = [];
            public IEnumerable<string> Phone { get; set; } = [];
            public IEnumerable<string> Fax { get; set; } = [];
            public IEnumerable<string> Email { get; set; } = [];
            public IEnumerable<string> URL { get; set; } = [];
            public IEnumerable<string> IPAddress { get; set; } = [];
        }

        public bool Enabled { get; set; } = false;
        public CustomPatternsClass Patterns { get; set; } = new();
    }

    public string PreviousLocation { get; set; } = string.Empty;
    public string SaveLocation { get; set; } = string.Empty;
    public string SaveFilename { get; set; } = "answer.txt";
    public string EditorLocation { get; set; } = Config.GetEditorByPlatform();
    public IEnumerable<string> ValidateFileLocations { get; set; } = [];
    public CustomRegexConfigClass CustomRegex { get; set; } = new();
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
