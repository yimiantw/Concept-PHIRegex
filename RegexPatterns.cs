using System.Text.RegularExpressions;

namespace ConceptPHIRegex;
internal partial class RegexPatterns
{
    #region PHI: ID
    [GeneratedRegex(@"\d{2}[A-Z]\d{5,7}[A-Z0-9]?")]
    internal static partial Regex ID();

    [GeneratedRegex(@"\d{5,7}.[A-Z]{3}")]
    internal static partial Regex MedicalRecord();
    #endregion

    #region PHI: Name
    [GeneratedRegex(@"^[A-Za-z]{2,}\s?[A-Za-z]{2,},\s?[A-Za-z]{2,}\s?[A-Za-z]+", RegexOptions.Multiline)]
    internal static partial Regex Patient();

    [GeneratedRegex(@"(?:Dr|DR|PRO)[\s?|.]\s?([A-Z]+\s?.?\s?\w+?\s?\w+(?:\s\w{3,})?)")]
    internal static partial Regex DoctorVariantOne();
    [GeneratedRegex(@"^\w+:\s+\(([A-Z]+\s+\w+)\)", RegexOptions.Multiline)]
    internal static partial Regex DoctorVariantTwo();
    [GeneratedRegex(@"(?:Result)\s?\w+\.?\s?(\w+\s?\.?\s?\w+\.?\w+)")]
    internal static partial Regex DoctorVariantThree();

    [GeneratedRegex("^to\\s?(?:Dr|DR)\\.\\s?")]
    internal static partial Regex DocUnusedString();

    [GeneratedRegex(@"[A-Za-z]{2,}\w?\d{3,}")]
    internal static partial Regex Username();
    #endregion

    #region PHI: Profession
    [GeneratedRegex(@"(?:is\sa\s(\w+)|(\w+)\sjob)")]
    internal static partial Regex Profession();
    #endregion

    #region PHI: Location
    [GeneratedRegex(@"^Location:\s{2}((?:\d\/\d\s)?\w+\s?\w+\s?\w+\s?)\-\s?(\w+\s?\w+.?\w+)", RegexOptions.Multiline)]
    internal static partial Regex DeptAndHospital(); //Including Department and Hospital
    
    [GeneratedRegex(@"(?:\((\w+(?:\W|\s)?\w+\s(?:Corporation|Company))|(?:Performed\sat\s)(\w+\s?(?:&|\s)\s?\w+\s?\w+)|(?:Department\sof\s\w+\s?\w?)\,\s(\w+)\,\s\w)")]
    internal static partial Regex Organization();

    [GeneratedRegex(@"^(?:(\w+(?:\s?\w)+)|((?:PO|P.O.)\s(?:BOX)\s\d{2,4}))\n(\w+(?:\s?\w)+)\s{2}((?:[A-Za-z]+\s){1,})\s{1,2}(\d{4})", RegexOptions.Multiline)]
    internal static partial Regex Address(); //Including Street, City, State, Zip, Location-Other
    #endregion

    #region PHI: Age
    [GeneratedRegex(@"(?:in\s)?(\d{1,3})\s?(?:yo|yr|years\sold)")]
    internal static partial Regex AgeVariantOne();

    [GeneratedRegex(@"(?:age)\s?(\d{1,3})")]
    internal static partial Regex AgeVariantTwo();
    #endregion

    #region PHI: Date
    [GeneratedRegex(@"(?:(\d{3,4})Hrs\s{1}on\s)?(\d{1,2}[/|\.]\d{1,2}[/|\.]\d{2,4})(?:\s{1}at\s{1}(\d{1,2}:\d{2}))?")]
    internal static partial Regex DateAndTime();

    [GeneratedRegex(@"(?:((?:\d{1,3}|\w{3,}))\s?(?i)((?:(?:week|wk)|(?:year|yr)|month|day|time|(?:hour|hr)|(?:minute|min)|(?:second|sec))(?:s)?))")]
    internal static partial Regex Duration();

    [GeneratedRegex(@"(?i)(?:once|twice|thrice)")]
    internal static partial Regex Set();
    #endregion

    #region PHI: Contract
    [GeneratedRegex(@"\((\d{4}\s?\d{4})\)")]
    internal static partial Regex Phone();

    [GeneratedRegex(@"\d{2}-\d{4}-\d{4}")]
    internal static partial Regex Fax();

    [GeneratedRegex(@"\w.+@\w.+\.\w+")]
    internal static partial Regex Email();

    //From: https://stackoverflow.com/questions/3809401/what-is-a-good-regular-expression-to-match-a-url
    [GeneratedRegex(@"([-a-zA-Z0-9@:%._\+~#=]{1,256}\.[a-zA-Z0-9()]{1,6})\b[-a-zA-Z0-9()@:%_\+.~#?&//=]*")]
    internal static partial Regex URL();

    [GeneratedRegex(@"\d{1,3}\.\d{1,3}\.\d{1,3}\.\d{1,3}")]
    internal static partial Regex IPv4Address();
    #endregion
}
