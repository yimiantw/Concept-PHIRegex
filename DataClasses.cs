namespace ConceptPHIRegex;

internal class RegexData
{
    internal string Value { get; set; } = string.Empty;
    internal int StartIndex { get; set; } = 0;
    internal int EndIndex { get; set; } = 0;
}

internal class PHIData
{
    #region PHI: ID
    internal List<RegexData> IDs { get; set; } = [];
    internal RegexData MedicalRecord { get; set; } = new();
    #endregion

    #region PHI: Name
    internal RegexData Patient { get; set; } = new();
    internal List<RegexData> Doctors { get; set; } = [];
    internal RegexData Username { get; set; } = new();
    #endregion

    //PHI: Profession
    internal RegexData Profession { get; set; } = new();

    #region PHI: Location
    internal RegexData Room { get; set; } = new();
    internal RegexData Department { get; set; } = new();
    internal RegexData Hospital { get; set; } = new();
    internal List<RegexData> Orgainzations { get; set; } = [];
    internal RegexData Street { get; set; } = new();
    internal RegexData City { get; set; } = new();
    internal RegexData State { get; set; } = new();
    internal RegexData Country { get; set; } = new();
    internal RegexData Zip { get; set; } = new();
    internal RegexData LocationOther { get; set; } = new();
    #endregion

    //PHI: Age
    internal RegexData Age { get; set; } = new();

    #region PHI: Date
    internal List<RegexData> Dates { get; set; } = [];
    internal List<RegexData> Times { get; set; } = [];
    internal RegexData Duration { get; set; } = new();
    internal RegexData Set { get; set; } = new();
    #endregion

    #region PHI: Contract
    internal RegexData Phone { get; set; } = new();
    internal RegexData Fax { get; set; } = new();
    internal RegexData Email { get; set; } = new();
    internal RegexData URL { get; set; } = new();
    internal RegexData IPAddr { get; set; } = new();
    #endregion
}
