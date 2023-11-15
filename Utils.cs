using System.Reflection;

namespace ConceptPHIRegex;

internal class Utils
{
    internal static string ReadIntroTexts()
    {
        Assembly CurrentASM = Assembly.GetExecutingAssembly();
        using Stream? stream = CurrentASM.GetManifestResourceStream("Concept-PHIRegex.welcome.txt") ?? throw new FileNotFoundException();
        using StreamReader Reader = new(stream);
        return Reader.ReadToEnd();
    }

    internal static string ReadRawData(string Filepath)
    {
        using StreamReader sr = new(Filepath);
        return sr.ReadToEnd();
    }

    internal static string ConvertToISO8601(ConvertForm Form, string originalData)
    {
        switch (Form)
        {
            case ConvertForm.Date:
                {
                    //Spilt into 0: MM, 1: d, 2: yyyy
                    string[] SpiltedDate = originalData.Contains('/')
                                            ? originalData.Split("/")
                                            : originalData.Split(".");
                    //If there is only one digit in day, padding 0 in front of it
                    if (SpiltedDate[0].Length < 2)
                    {
                        SpiltedDate[0] = $"0{SpiltedDate[0]}";
                    }
                    //If there is only one digit in month, padding 0 in front of it
                    if (SpiltedDate[1].Length < 2)
                    {
                        SpiltedDate[1] = $"0{SpiltedDate[1]}";
                    }
                    //If there is only two digit in year, padding 20 in front of it
                    //The answer shows they padded 20 instead of 19 in front the year, weird
                    if (SpiltedDate[2].Length < 4)
                    {
                        SpiltedDate[2] = $"20{SpiltedDate[2]}";
                    }
                    return string.Format("{0}-{1}-{2}", SpiltedDate[2].Trim(), SpiltedDate[1].Trim(), SpiltedDate[0].Trim());
                }
            case ConvertForm.Time:
                {
                    string[] SpiltedTime = [];
                    if (originalData.Contains("at"))
                    {
                        SpiltedTime = originalData.Split("at");
                        return $"{ConvertToISO8601(ConvertForm.Date, SpiltedTime[0])}T{SpiltedTime[1].Trim()}";
                    }
                    else if (originalData.Contains("on"))
                    {
                        SpiltedTime = originalData.Split("on");
                        string TimeDigits = SpiltedTime[0].Replace("Hrs", string.Empty).Trim();
                        if (TimeDigits.Length < 4)
                        {
                            TimeDigits = TimeDigits.Insert(0, "0");
                        }
                        return $"{ConvertToISO8601(ConvertForm.Date, SpiltedTime[1])}T{TimeDigits.Insert(2, ":")}";
                    }
                    else
                        throw new NotImplementedException();
                }
            default:
                throw new NotImplementedException();
        }
    }
}
