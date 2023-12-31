﻿using System.Collections.Frozen;
using System.Diagnostics;
using System.Runtime.InteropServices;
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
        { "twenty", "20" }
    }.ToFrozenDictionary();

    internal static FrozenDictionary<string, string> MonthsDigit = new Dictionary<string, string>()
    {
        { "JAN", "1" },
        { "FEB", "2" },
        { "MAR", "3" },
        { "APR", "4" },
        { "MAY", "5" },
        { "JUN", "6" },
        { "JUL", "7" },
        { "AUG", "8" },
        { "SEP", "9" },
        { "OCT", "10" },
        { "NOV", "11" },
        { "DEC", "12" }
    }.ToFrozenDictionary();

    internal static (bool IsSatisfied, bool NoMergeFile, string DatasetPath, string DLResult) IsRuntimeSatisfied()
    {
        string[] CommandArgs = Environment.GetCommandLineArgs();
        if (CommandArgs.Contains("--dataset") && CommandArgs.Contains("--result"))
        {
            int IndexOfDatasetPath = Array.FindIndex(CommandArgs, x => x.Contains("--dataset")) + 1;
            int IndexOfDLResult = Array.FindIndex(CommandArgs, x => x.Contains("--result")) + 1;
            bool NoMergeFile = CommandArgs.Contains("--no-merge");
            string DatasetPath = CommandArgs[IndexOfDatasetPath].Replace("\"", string.Empty);
            string DLResult = CommandArgs[IndexOfDLResult].Replace("\"", string.Empty);
            if (!Path.Exists(DatasetPath) | !Path.Exists(DLResult))
            {
                return (false, false, string.Empty, string.Empty);
            }
            return (true, NoMergeFile, DatasetPath, DLResult);
        }
        return (false, false, string.Empty, string.Empty);
    }
    
    internal static bool IsSpecialToken(string Value)
    => Value.Equals("DATE", StringComparison.Ordinal) | Value.Equals("TIME", StringComparison.Ordinal)
        | Value.Equals("DURATION") | Value.Equals("SET", StringComparison.Ordinal);

    internal static void OpenEditor(string Filename)
    {
        ProcessStartInfo StartInfo = new()
        {
            FileName = GetEditorByPlatform(),
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

    private static string GetEditorByPlatform()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            return "notepad.exe";
        }

        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            return "/Applications/Visual Studio Code.app/Contents/Resources/app/bin";
        }

        throw new NotImplementedException("The platform is unsupported");
    }

    #region Convert Dates in PHI into ISO-8601 format
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
