using System.Collections.Frozen;
using System.Diagnostics;
using System.Reflection;
using System.Text.RegularExpressions;

namespace ConceptPHIRegex;

internal class Utils
{
    internal static FrozenDictionary<string, string> NumberDigit = new Dictionary<string, string>()
        {
            { "one", "1" },
            { "two", "2" },
            { "three", "3" },
            { "four", "4" },
            { "five", "5" },
            { "six", "6" },
            { "seven", "7" },
            { "eight", "8" },
            { "nine", "9" },
            { "ten", "10" },
            { "eleven", "11" },
            { "twelve", "12" },
            { "thirteen", "13" },
            { "fourteen", "14" },
            { "fifteen", "15" },
            { "sixteen", "16" },
            { "seventeen", "17" },
            { "eighteen", "18" },
            { "nineteen", "19" },
            { "twenty", "20" },
        }.ToFrozenDictionary();


    internal static string GetAssemblyResource(string ResourceName)
    {
        Assembly CurrentASM = Assembly.GetExecutingAssembly();
        using Stream? stream = CurrentASM.GetManifestResourceStream(ResourceName) ?? throw new FileNotFoundException();
        using StreamReader Reader = new(stream);
        return Reader.ReadToEnd();
    }

    internal static void OpenEditor(string Filename)
    {
        ProcessStartInfo StartInfo = new()
        {
            FileName = Config.AppConfig.EditorLocation,
            Arguments = Filename
        };
        try
        {
            Process.Start(StartInfo);
        }
        catch (Exception ex)
        {
            Console.WriteLine("Unable to open editor\nFile:{0}\nArguments:{1}", StartInfo.FileName, StartInfo.Arguments);
            Console.WriteLine("Exception: {0}", ex.Message);
            Console.WriteLine("\nPress any key to exit...");
            Environment.Exit(0);
        }
    }

    #region Convert Dates in PHI into ISO-8601 format
    //TODO: Ugly implementation, fix it
    internal static string GetNormalizedString(ConvertForm Form, string originalData)
    {
        try
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
                        return string.Format("_{0}-{1}-{2}", SpiltedDate[2].Trim(), SpiltedDate[1].Trim(), SpiltedDate[0].Trim());
                    }
                case ConvertForm.Time:
                    {
                        string[] SpiltedTime = [];
                        if (originalData.Contains("at"))
                        {
                            SpiltedTime = originalData.Split("at");
                            return $"_{GetNormalizedString(ConvertForm.Date, SpiltedTime[0])}T{SpiltedTime[1].Trim()}";
                        }
                        else if (originalData.Contains("on"))
                        {
                            SpiltedTime = originalData.Split("on");
                            string TimeDigits = SpiltedTime[0].Replace("Hrs", string.Empty).Trim();
                            if (TimeDigits.Length < 4)
                            {
                                TimeDigits = TimeDigits.Insert(0, "0");
                            }
                            return $"_{GetNormalizedString(ConvertForm.Date, SpiltedTime[1])}T{TimeDigits.Insert(2, ":")}";
                        }
                        else
                            throw new NotImplementedException();
                    }
                case ConvertForm.Duration:
                    {
                        Match MatchedItem = RegexPatterns.Duration().Match(originalData);
                        if (MatchedItem.Success)
                        {
                            string DurationValue = int.TryParse(MatchedItem.Groups[1].Value, out _)
                                ? MatchedItem.Groups[1].Value
                                : NumberDigit.TryGetValue(MatchedItem.Groups[1].Value, out string? ValueFromDict)
                                    ? ValueFromDict
                                    : string.Empty;
                            string Designator = MatchedItem.Groups[2].Value.ToLower() switch
                            {
                                "year" or "years" or "yr" or "yrs" => "Y",
                                "month" or "months" => "M",
                                "week" or "weeks" or "wk" or "wks" => "W",
                                "day" or "days" => "D",
                                "time" or "times" => "T",
                                "hour" or "hours" or "hr" or "hrs" => "H",
                                "minute" or "minutes" or "min" or "mins" => "M",
                                "second" or "seconds" or "sec" or "secs" => "S",
                                _ => throw new NotImplementedException()
                            };
                            return $"_P{DurationValue}{Designator}";
                        }
                        throw new NotImplementedException();
                    }
                case ConvertForm.Set:
                    {
                        return $"_R{originalData.ToLower() switch
                        {
                            "once" => "1",
                            "twice" => "2",
                            "thrice" => "3",
                            _ => throw new NotImplementedException()
                        }}";
                    }
                default:
                    throw new NotImplementedException();
            }
        }
        catch (Exception)
        {
            return "_ERROR: Invalid format to normalize :(";
        }
    }
    #endregion
}
