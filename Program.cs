using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;

namespace ConceptPHIRegex;

internal partial class Program
{
    #region Main Entry
    static void Main(string[] args)
    {
        try
        {
            (bool ValidateEnabled, string ValidateFile, string SourceFile) = Utils.CheckValidateMode();
            #region Normal mode
            if (!ValidateEnabled)
            {
                while (true)
                {
                    //Read config
                    Config.ReadConfig();

#if !DEBUG
                string InputPath = GetConsoleInput();
                string[] FilesArray = [];
                //Check whether the input is a directory or a file
                if (File.GetAttributes(InputPath).HasFlag(FileAttributes.Directory))
                {
                    //Enumerate all text (.txt) files
                    FilesArray = Directory.GetFiles(InputPath, "*.txt", SearchOption.TopDirectoryOnly);

                    //Check whether there is any text file in the target directory
                    if (FilesArray.Length == 0)
                    {
                        Console.WriteLine($"Path: {InputPath}");
                        Console.WriteLine("\nThere is no text files available in target directory, operation cancelled.");
                        Console.WriteLine("Press any key to exit.");
                        Console.ReadKey();
                        Environment.Exit(0);
                    }
                }
                else
                {
                    FilesArray = [InputPath];
                }

                //Save path to config
                Config.AppConfig.PreviousLocation = InputPath;
            Config.WriteConfig();
#else
                    string[] FilesArray = Directory.GetFiles(@"D:\Test\DataSets\Mixed", "*.txt", SearchOption.TopDirectoryOnly);
                    //string[] FilesArray = [@"D:\Test\DataSets\First_Phase\103.txt"];
                    //Config.AppConfig.ValidateFileLocations = new string[]
                    //{
                    //    @"D:\Test\DataSets\first_answer.txt",
                    //    @"D:\Test\DataSets\second_answer.txt"
                    //};
                    Config.AppConfig.ValidateFileLocations = new string[]
                    {
                    @"D:\Test\DataSets\answer_DL.txt"
                    };

                    foreach (string FilePath in FilesArray.Where(x => !Path.Exists(x)))
                    {
                        Console.WriteLine("File: {0} is not exist, please check the \"FilesArray\" variable in the source code", FilePath);
                        Console.WriteLine("Press any key to exit...");
                        Console.ReadKey();
                        Environment.Exit(0);
                    }
#endif
                    //Check validation files
                    if (Config.AppConfig.ValidateFileLocations.Any())
                    {
                        Console.WriteLine("{0} validation files detected, which are: {1}",
                                            Config.AppConfig.ValidateFileLocations.Count(),
                                            string.Join(',', Config.AppConfig.ValidateFileLocations.Select(x => Path.GetFileName(x))));
                    }

                    //Calcuate process time
                    Stopwatch ProcessTime = new();
                    ProcessTime.Start();
                    Console.Title = "Processing data...";

                    //Initialize data class
                    List<PHIData> List_PHIData = [];
                    List<string> opt = [];

                    for (int i = 0; i < FilesArray.Length; i++)
                    {
                        Console.Clear();
                        Console.WriteLine("Processing data...Please don't close the window");
                        Console.WriteLine("Mode: {0}", Config.AppConfig.CustomRegex.Enabled ? "Custom Regex pattern from config" : "Regex from the program supplies");
                        Console.WriteLine("Current processing file: {0} ({1}/{2}) | {3}%\n", Path.GetFileName(FilesArray[i]), i, FilesArray.Length, Math.Round((double)i / (double)FilesArray.Length * 100, 2));

                        using StreamReader sr = new(FilesArray[i]);
                        string RawData = sr.ReadToEnd();
                        sr.Close();
                        string Filename = Path.GetFileNameWithoutExtension(FilesArray[i]);
                        PHIData ProcessedData = Config.AppConfig.CustomRegex.Enabled
                                                    ? ProcessRegexWithCustomPattern(RawData)
                                                    : ProcessRegexFromRawData(RawData);
                        List_PHIData.Add(ProcessedData);
                        opt.Add(GenerateOutput(Filename, ProcessedData));
                    }

                    //Display processed time
                    Console.WriteLine("Done!");
                    ProcessTime.Stop();
                    Console.Title = "Done!";
                    Console.WriteLine("\nTotal files: {0} | Process Time: {1}ms | Memory Used: {2} MB\n", FilesArray.Length, ProcessTime.ElapsedMilliseconds, Math.Round(((float)Process.GetCurrentProcess().WorkingSet64 / 1048576), 3));

                    // Write results into text file
                    string SaveLocation = !string.IsNullOrEmpty(Config.AppConfig.SaveLocation)
                                            ? Config.AppConfig.SaveLocation
                                            : Path.Combine(AppContext.BaseDirectory, Config.AppConfig.SaveFilename);
                    StreamWriter sw = new(SaveLocation);
                    sw.WriteLine(string.Join("\r\n", opt.Where(x => x.Length > 0).Select(x => x)));
                    sw.Close();
                    //Ask user whether want to open the result file
                    Console.WriteLine(Config.AppConfig.ValidateFileLocations.Any()
                                        ? "The results are saved to {0}, \npress [Y] key open the file, [V] key validate the result, [M] key return to main menu OR any key to close."
                                        : "The results are saved to {0}, \npress [Y] key open the file, [M] key return to main menu OR any key to close.", SaveLocation);

                    //Do things based on the key pressed
                    switch (Console.ReadKey().Key)
                    {
                        case ConsoleKey.Y:
                            {
                                Utils.OpenEditor(!string.IsNullOrEmpty(Config.AppConfig.SaveFilename)
                                                    ? SaveLocation
                                                    : AppContext.BaseDirectory + Config.AppConfig.SaveFilename);
                                break;
                            }
                        case ConsoleKey.V:
                            if (Config.AppConfig.ValidateFileLocations.Any())
                            {
                                ValidateResults(opt, Config.AppConfig.ValidateFileLocations.Where(Path.Exists));
                            }
                            break;
                        case ConsoleKey.M:
                            Console.Beep();
                            break;
                        default:
                            Environment.Exit(0);
                            break;
                    }

                }
            }
            #endregion
            #region Validation mode
            else
            {
                Console.WriteLine($"Validation only mode is Enabled\nThe file will be analyzed: {ValidateFile}\nValidation source(s): {SourceFile}");
                Console.WriteLine("Press any key to start validation process...");

                string[] SourceFileSplited = SourceFile.Split(',').Select(x => x.Trim()).ToArray();
                if (!File.Exists(ValidateFile) | (SourceFileSplited.Where(File.Exists).Count() != SourceFileSplited.Length))
                {
                    Console.WriteLine("\nInput path is not exist.\nPress any key to exit...");
                    Console.ReadKey();
                    Environment.Exit(0);
                }
                ValidateResults([.. File.ReadAllLines(ValidateFile)], SourceFileSplited);
            }
            #endregion

        }
#if !DEBUG
            catch (Exception ex)
            {

                Console.WriteLine("An error occured: \n{0}", ex.Message);
                Console.WriteLine("Press any to exit the program throw the debug info into IDE.");
                Console.ReadKey();
#else
        catch (Exception)
        {
#endif
            throw;
        }
    }
    #endregion

    #region Fetch input from console
    internal static string GetConsoleInput()
    {
        //Load intro texts here, not in the loop
        string IntroTexts = Utils.GetAssemblyResource("Concept-PHIRegex.welcome.txt");
        while (true)
        {
            //Clear console
            Console.Clear();
            //Found previous location
            if (!string.IsNullOrEmpty(Config.AppConfig.PreviousLocation))
            {
                if (Path.Exists(Config.AppConfig.PreviousLocation))
                {
                    Console.WriteLine($"Found previous location: {Config.AppConfig.PreviousLocation}");
                    Console.WriteLine($"Type [Y] to use it");
                }
                else
                    Config.AppConfig.PreviousLocation = string.Empty;
            }
            Console.Write(IntroTexts);
            string? Input = Console.ReadLine();
            if (Input != null)
            {
                Input = Input.Replace("\"", string.Empty);
                //If input is empty, use program's location
                if (string.IsNullOrEmpty(Input))
                {
                    return AppContext.BaseDirectory;
                }
                //If input is Y, use previous location
                if (!string.IsNullOrEmpty(Config.AppConfig.PreviousLocation) && Input.ToUpper().Equals("Y"))
                {
                    return Config.AppConfig.PreviousLocation;
                }
                //If input is NOT empty, use input path
                if (Path.Exists(Input))
                {
                    return Input;
                }
                Console.WriteLine("Error: The folder or file doesn't exist, please try again.");
            }
        }
    }
    #endregion

    #region ProcessRegexFromRawData
    internal static PHIData ProcessRegexFromRawData(string RawData)
    {
        PHIData Data = new();

        #region Match: ID
        MatchCollection Regex_ID = RegexPatterns.ID().Matches(RawData);
        if (Regex_ID.Count > 0)
        {
            foreach (Match MatchID in Regex_ID.Cast<Match>())
            {
                Data.IDs.Add(new()
                {
                    Value = MatchID.Value,
                    StartIndex = MatchID.Index,
                    EndIndex = MatchID.Index + MatchID.Length,
                });
            }
        }
        #endregion

        #region Match: Patient name
        Match Regex_Patient = RegexPatterns.Patient().Match(RawData);
        if (Regex_Patient.Success)
        {
            Data.Patient = new()
            {
                Value = Regex_Patient.Value,
                StartIndex = Regex_Patient.Index,
                EndIndex = Regex_Patient.Index + Regex_Patient.Length,
            };
        }
        #endregion

        #region Match: Medical Record
        Match Regex_MedicalRecord = RegexPatterns.MedicalRecord().Match(RawData);
        if (Regex_MedicalRecord.Success)
        {
            Data.MedicalRecord = new()
            {
                Value = Regex_MedicalRecord.Value,
                StartIndex = Regex_MedicalRecord.Index,
                EndIndex = Regex_MedicalRecord.Index + Regex_MedicalRecord.Length,
            };
        }
        #endregion

        #region Match: Address (STREET, CITY, STATE and ZIP)
        Match Regex_Address = RegexPatterns.Address().Match(RawData);
        if (Regex_Address.Success)
        {
            //Check it's Street or Location-Other by checking the first group in string
            switch (!string.IsNullOrEmpty(Regex_Address.Groups[1].Value))
            {
                case true:
                    {
                        //Street
                        IEnumerable<string> SpiltedString = Regex_Address.Value.Split("\n");
                        string Street = Regex_Address.Groups[1].Value.Trim();
                        Data.Street = new()
                        {
                            Value = Street,
                            StartIndex = RawData.IndexOf(Street),
                            EndIndex = RawData.IndexOf(Street) + Street.Length
                        };
                        break;
                    }
                case false:
                    {
                        //Location-Other
                        string LocationOther = Regex_Address.Groups[2].Value.Trim();
                        Data.LocationOther = new()
                        {
                            Value = LocationOther,
                            StartIndex = RawData.IndexOf(LocationOther),
                            EndIndex = RawData.IndexOf(LocationOther) + LocationOther.Length
                        };
                        break;
                    }
            }

            //City, State and Zip
            string City = Regex_Address.Groups[3].Value.Trim();
            Data.City = new()
            {
                Value = City,
                StartIndex = RawData.IndexOf(City),
                EndIndex = RawData.IndexOf(City) + City.Length
            };

            string State()
            {
                if (!string.IsNullOrEmpty(Regex_Address.Groups[4].Value))
                {
                    return Regex_Address.Groups[4].Value.Trim();
                }
                if (!string.IsNullOrEmpty(Regex_Address.Groups[6].Value))
                {
                    return Regex_Address.Groups[6].Value.Trim();
                }
                return string.Empty;
            };

            if (!string.IsNullOrEmpty(State()))
            {
                Data.State = new()
                {
                    Value = State(),
                    StartIndex = RawData.IndexOf(State()),
                    EndIndex = RawData.IndexOf(State()) + State().Length
                };
            }

            string Zip()
            {
                if (!string.IsNullOrEmpty(Regex_Address.Groups[5].Value))
                {
                    return Regex_Address.Groups[5].Value.Trim();
                }
                if (!string.IsNullOrEmpty(Regex_Address.Groups[7].Value))
                {
                    return Regex_Address.Groups[7].Value.Trim();
                }
                if (!string.IsNullOrEmpty(Regex_Address.Groups[8].Value))
                {
                    return Regex_Address.Groups[8].Value.Trim();
                }
                return string.Empty;
            };
            Data.Zip = new()
            {
                Value = Zip(),
                StartIndex = RawData.IndexOf(Zip()),
                EndIndex = RawData.IndexOf(Zip()) + Zip().Length
            };
        }
        #endregion

        #region Match: Country
        IEnumerable<Match> Regex_Countries = RegexPatterns.CountryFullnames().Matches(RawData).Concat(RegexPatterns.CountryShortnames().Matches(RawData));
        if (Regex_Countries.Any())
        {
            foreach (Match MatchedCountry in Regex_Countries)
            {
                string TrimmedValue = MatchedCountry.Value.Trim();
                Data.Country.Add(new()
                {
                    Value = TrimmedValue,
                    StartIndex = RawData.IndexOf(TrimmedValue),
                    EndIndex = RawData.IndexOf(TrimmedValue) + TrimmedValue.Length
                });
            }
        }
        #endregion

        #region Match: Date & Time
        MatchCollection Regex_DateAndTime = RegexPatterns.DateAndTime().Matches(RawData);
        if (Regex_DateAndTime.Count > 0)
        {
            IEnumerable<string> Dates = Regex_DateAndTime.Where(x => !x.Value.Contains("at") & !x.Value.Contains("on")).Select(x => x.Value);
            IEnumerable<string> Times = Regex_DateAndTime.Where(x => !Dates.Contains(x.Value)).Select(x => x.Value);
            foreach (string MatchDate in Dates)
            {
                Data.Dates.Add(new()
                {
                    Value = MatchDate,
                    StartIndex = RawData.IndexOf(MatchDate),
                    EndIndex = RawData.IndexOf(MatchDate) + MatchDate.Length,
                });
            }
            foreach (string MatchTime in Times)
            {
                Data.Times.Add(new()
                {
                    Value = MatchTime,
                    StartIndex = RawData.IndexOf(MatchTime),
                    EndIndex = RawData.IndexOf(MatchTime) + MatchTime.Length,
                });
            }
        }
        #endregion

        #region Match: Department & Hospital
        Match Regex_DeptHospital = RegexPatterns.DeptAndHospital().Match(RawData);
        if (Regex_DeptHospital.Success)
        {
            //Department
            string Dept = Regex_DeptHospital.Groups[1].Value.Trim();
            int DeptIndex = RawData.IndexOf(Dept);
            Data.Department = new()
            {
                Value = Dept,
                StartIndex = DeptIndex,
                EndIndex = DeptIndex + Dept.Length,
            };

            //Hospital
            string Hospital = Regex_DeptHospital.Groups[2].Value.Trim();
            int HospIndex = RawData.IndexOf(Hospital);
            Data.Hospital = new()
            {
                Value = Hospital,
                StartIndex = HospIndex,
                EndIndex = HospIndex + Hospital.Length,
            };
        }
        #endregion

        #region Match: Doctor
        IEnumerable<Match> Regex_Doctors = RegexPatterns.DoctorVariantOne().Matches(RawData)
                                            .Concat(RegexPatterns.DoctorVariantTwo().Matches(RawData))
                                            .Concat(RegexPatterns.DoctorVariantThree().Matches(RawData))
                                            .Concat(RegexPatterns.DoctorVariantFour().Matches(RawData))
                                            .Concat(RegexPatterns.DoctorVariantFive().Matches(RawData));
        if (Regex_Doctors.Any())
        {
            foreach (Match Doc in Regex_Doctors)
            {
                IEnumerable<Group> DocValues = Doc.Groups.Values.Skip(1);
                foreach (Group item in DocValues)
                {
                    string TrimmedDoc = RegexPatterns.DocUnusedString().Replace(item.Value, string.Empty).Trim();
                    if (!Data.Doctors.Any(x => x.Value.Contains(TrimmedDoc)))
                    {
                        if (item.Value.Length is 2)
                        {
                            Data.Doctors.Add(new()
                            {
                                Value = item.Value,
                                StartIndex = item.Index,
                                EndIndex = item.Index + item.Value.Length,
                            });
                        }
                        else
                        {
                            Data.Doctors.Add(new()
                            {
                                Value = TrimmedDoc,
                                StartIndex = RawData.IndexOf(item.Value),
                                EndIndex = RawData.IndexOf(item.Value) + item.Value.Length,
                            });
                        }
                    }
                }
            }
        }
        #endregion

        #region Match: Phone
        Match Regex_Phone = RegexPatterns.Phone().Match(RawData);
        if (Regex_Phone.Success)
        {
            Data.Phone = new()
            {
                Value = Regex_Phone.Groups[1].Value,
                StartIndex = RawData.IndexOf(Regex_Phone.Groups[1].Value),
                EndIndex = RawData.IndexOf(Regex_Phone.Groups[1].Value) + Regex_Phone.Groups[1].Value.Length,
            };
        }
        #endregion

        #region Match: Organization
        MatchCollection Regex_Organization = RegexPatterns.Organization().Matches(RawData);
        if (Regex_Organization.Count > 0)
        {
            foreach (Match MatchOrg in Regex_Organization.Cast<Match>())
            {
                string Org = MatchOrg.Groups.Values.Skip(1).Where(x => !string.IsNullOrEmpty(x.Value)).Select(x => x.Value).First();
                Data.Orgainzations.Add(new()
                {
                    Value = Org,
                    StartIndex = RawData.IndexOf(Org),
                    EndIndex = RawData.IndexOf(Org) + Org.Length,
                });
            }
        }
        #endregion

        #region Match: Age
        IEnumerable<Match> Regex_Age = Enumerable.Empty<Match>()
                                        .Append(RegexPatterns.AgeVariantOne().Match(RawData))
                                        .Append(RegexPatterns.AgeVariantTwo().Match(RawData));
        if (Regex_Age.Any())
        {
            string Age = Regex_Age.First().Groups[1].Value;
            Data.Age = new()
            {
                Value = Age,
                StartIndex = RawData.IndexOf(Age),
                EndIndex = RawData.IndexOf(Age) + Age.Length
            };
        }
        #endregion

        #region Match: Email
        Match Regex_Email = RegexPatterns.Email().Match(RawData);
        if (Regex_Email.Success)
        {
            Data.Email = new()
            {
                Value = Regex_Email.Value,
                StartIndex = RawData.IndexOf(Regex_Email.Value),
                EndIndex = RawData.IndexOf(Regex_Email.Value) + Regex_Email.Value.Length
            };
        }
        #endregion

        #region Match: URL
        Match Regex_URL = RegexPatterns.URL().Match(RawData);
        if (Regex_URL.Success)
        {
            if (!Data.MedicalRecord.Value.Contains(Regex_URL.Value))
            {
                Data.URL = new()
                {
                    Value = Regex_URL.Value,
                    StartIndex = RawData.IndexOf(Regex_URL.Value),
                    EndIndex = RawData.IndexOf(Regex_URL.Value) + Regex_URL.Value.Length
                };
            }
        }
        #endregion

        #region Match: IPv4 Address
        Match Regex_IPv4 = RegexPatterns.IPv4Address().Match(RawData);
        if (Regex_IPv4.Success)
        {
            Data.URL = new()
            {
                Value = Regex_IPv4.Value,
                StartIndex = RawData.IndexOf(Regex_IPv4.Value),
                EndIndex = RawData.IndexOf(Regex_IPv4.Value) + Regex_IPv4.Value.Length
            };
        }
        #endregion

        #region Match: Fax
        Match Regex_Fax = RegexPatterns.Fax().Match(RawData);
        if (Regex_Fax.Success)
        {
            Data.Fax = new()
            {
                Value = Regex_Fax.Value,
                StartIndex = RawData.IndexOf(Regex_Fax.Value),
                EndIndex = RawData.IndexOf(Regex_Fax.Value) + Regex_Fax.Value.Length
            };
        }
        #endregion

        #region Match: Username
        Match Regex_Username = RegexPatterns.Username().Match(RawData);
        if (Regex_Username.Success)
        {
            Data.Username = new()
            {
                Value = Regex_Username.Value,
                StartIndex = RawData.IndexOf(Regex_Username.Value),
                EndIndex = RawData.IndexOf(Regex_Username.Value) + Regex_Username.Value.Length
            };
        }
        #endregion

        #region Match: Profession (TODO: find better pattern)
        //Match Regex_Profession = RegexPatterns.Profession().Match(RawData);
        //if (Regex_Profession.Success)
        //{
        //    Data.Profession = new()
        //    {
        //        Value = Regex_Profession.Value,
        //        StartIndex = RawData.IndexOf(Regex_Profession.Value),
        //        EndIndex = RawData.IndexOf(Regex_Profession.Value) + Regex_Profession.Value.Length
        //    };
        //}
        #endregion

        #region Match: Duration
        MatchCollection Regex_Duration = RegexPatterns.Duration().Matches(RawData);
        if (Regex_Duration.Count > 0)
        {
            foreach (Match MatchDuration in Regex_Duration.Cast<Match>())
            {
                if (int.TryParse(MatchDuration.Groups[1].Value, out _) | Utils.NumberDigit.ContainsKey(MatchDuration.Groups[1].Value))
                {
                    Data.Durations.Add(new()
                    {
                        Value = MatchDuration.Value,
                        StartIndex = MatchDuration.Index,
                        EndIndex = MatchDuration.Index + MatchDuration.Value.Length,
                    });
                }
            }
        }
        #endregion

        #region Match: Set
        MatchCollection Regex_Set = RegexPatterns.Set().Matches(RawData);
        if (Regex_Set.Count > 0)
        {
            foreach (Match MatchSet in Regex_Set.Cast<Match>())
            {
                Data.Sets.Add(new()
                {
                    Value = MatchSet.Value,
                    StartIndex = MatchSet.Index,
                    EndIndex = MatchSet.Index + MatchSet.Value.Length,
                });
            }
        }
        #endregion

        return Data;
    }
    #endregion

    #region ProcessRegexWithCustomPattern
    internal static PHIData ProcessRegexWithCustomPattern(string RawData)
    {
        PHIData Data = new();

        //<Pattern>@@<index>
        #region TupleArray
        (IEnumerable<string> Patterns, object PHIData)[] TupleArray =
        [
            (Config.AppConfig.CustomRegex.Patterns.IDNumber, Data.IDs),
            (Config.AppConfig.CustomRegex.Patterns.MedicalRecord, Data.MedicalRecord),
            (Config.AppConfig.CustomRegex.Patterns.PatientName, Data.Patient),
            (Config.AppConfig.CustomRegex.Patterns.Doctor, Data.Doctors),
            (Config.AppConfig.CustomRegex.Patterns.Username, Data.IDs),
            (Config.AppConfig.CustomRegex.Patterns.Profession, Data.Profession),
            (Config.AppConfig.CustomRegex.Patterns.Department, Data.Department),
            (Config.AppConfig.CustomRegex.Patterns.Hospital, Data.Hospital),
            (Config.AppConfig.CustomRegex.Patterns.Organization, Data.Orgainzations),
            (Config.AppConfig.CustomRegex.Patterns.Street, Data.Street),
            (Config.AppConfig.CustomRegex.Patterns.City, Data.City),
            (Config.AppConfig.CustomRegex.Patterns.State, Data.State),
            (Config.AppConfig.CustomRegex.Patterns.Country, Data.Country),
            (Config.AppConfig.CustomRegex.Patterns.Zip, Data.Zip),
            (Config.AppConfig.CustomRegex.Patterns.LocationOther, Data.LocationOther),
            (Config.AppConfig.CustomRegex.Patterns.Age, Data.Age),
            (Config.AppConfig.CustomRegex.Patterns.Date, Data.Dates),
            (Config.AppConfig.CustomRegex.Patterns.Time, Data.Times),
            (Config.AppConfig.CustomRegex.Patterns.Duration, Data.Durations),
            (Config.AppConfig.CustomRegex.Patterns.Set, Data.Sets),
            (Config.AppConfig.CustomRegex.Patterns.Phone, Data.Phone),
            (Config.AppConfig.CustomRegex.Patterns.Fax, Data.Fax),
            (Config.AppConfig.CustomRegex.Patterns.Email, Data.Email),
            (Config.AppConfig.CustomRegex.Patterns.URL, Data.URL),
            (Config.AppConfig.CustomRegex.Patterns.IPAddress, Data.IPAddr)
        ];
        #endregion

        foreach ((IEnumerable<string> Patterns, object Data) DataTuple in TupleArray)
        {
            foreach (string Pattern in DataTuple.Patterns)
            {
                string[] SplitedPattern = Pattern.Split("@@");
                string RegexPattern = SplitedPattern[0];
                string[] Indexes = SplitedPattern.Length > 1 ? SplitedPattern[1].Split(',') : [];

                MatchCollection RegexCollection = Regex.Matches(RawData, Pattern);
                if (RegexCollection.Count > 0)
                {
                    foreach (Match MatchID in RegexCollection.Cast<Match>())
                    {
                        if (Indexes.Length > 0)
                        {
                            foreach (string Index in Indexes)
                            {
                                if (int.Parse(Index) < MatchID.Groups.Count)
                                {
                                    string Value = MatchID.Groups[Index].Value.Trim();
                                    int StartIndex = RawData.IndexOf(Value);
                                    int EndIndex = RawData.IndexOf(Value) + Value.Length;

                                    if (Equals(DataTuple.Data.GetType(), typeof(List<RegexData>)))
                                    {
                                        List<RegexData>? JustData = DataTuple.Data as List<RegexData>;
                                        JustData?.Add(new()
                                        {
                                            Value = Value,
                                            StartIndex = StartIndex,
                                            EndIndex = EndIndex,
                                        });
                                    }
                                    else
                                    {
                                        RegexData? JustData = DataTuple.Data as RegexData;
                                        if (JustData is not null)
                                        {
                                            JustData.Value = Value;
                                            JustData.StartIndex = StartIndex;
                                            JustData.EndIndex = EndIndex;
                                        }
                                    }
                                }
                            }
                        }
                        else
                        {
                            string Value = MatchID.Value.Trim();
                            int StartIndex = RawData.IndexOf(Value);
                            int EndIndex = RawData.IndexOf(Value) + Value.Length;

                            if (Equals(DataTuple.Data.GetType(), typeof(List<RegexData>)))
                            {
                                List<RegexData>? JustData = DataTuple.Data as List<RegexData>;
                                JustData?.Add(new()
                                {
                                    Value = Value,
                                    StartIndex = StartIndex,
                                    EndIndex = EndIndex,
                                });
                            }
                            else
                            {
                                RegexData? JustData = DataTuple.Data as RegexData;
                                if (JustData is not null)
                                {
                                    JustData.Value = Value;
                                    JustData.StartIndex = StartIndex;
                                    JustData.EndIndex = EndIndex;
                                }
                            }
                        }
                    }
                }
            }
        }



        return Data;
    }
    #endregion

    #region GenerateOutput
    internal static string GenerateOutput(string Filename, PHIData Data)
    {
        List<string> OutputText = [];
        (object Data, string Text)[] PHI = new (object, string)[]
        {
            (Data.IDs, "IDNUM"), (Data.MedicalRecord, "MEDICALRECORD"), (Data.Patient, "PATIENT"), (Data.Doctors, "DOCTOR"),
            (Data.Username, "USERNAME"), (Data.Profession, "PROFESSION"), (Data.Department, "DEPARTMENT"), (Data.Hospital, "HOSPITAL"),
            (Data.Orgainzations, "ORGANIZATION"), (Data.Street, "STREET"), (Data.City, "CITY"), (Data.State, "STATE"),
            (Data.Country, "COUNTRY"), (Data.Zip, "ZIP"), (Data.LocationOther, "LOCATION-OTHER"), (Data.Age, "AGE"),
            (Data.Dates, "DATE"), (Data.Times, "TIME"), (Data.Durations, "DURATION"), (Data.Sets, "SET"),
            (Data.Phone, "PHONE"), (Data.Fax, "FAX"), (Data.Email, "EMAIL"), (Data.URL, "URL"),
            (Data.IPAddr, "IPADDR"),
        };

        foreach ((object Data, string Text) Item in PHI)
        {
            if (Equals(Item.Data.GetType(), typeof(List<RegexData>)))
            {
                List<RegexData> ListofRegexData = (List<RegexData>)Item.Data;
                foreach (RegexData item in ListofRegexData)
                {
                    if (!string.IsNullOrEmpty(item.Value))
                    {
                        string NormalizedValue = Item.Text.Equals("DATE") | Item.Text.Equals("TIME") | Item.Text.Equals("DURATION") | Item.Text.Equals("SET") ? Utils.GetNormalizedString(Item.Text switch
                        {
                            "DATE" => ConvertForm.Date,
                            "TIME" => ConvertForm.Time,
                            "DURATION" => ConvertForm.Duration,
                            "SET" => ConvertForm.Set,
                            _ => throw new NotImplementedException()
                        }, item.Value) : string.Empty;

                        OutputText.Add($"{Filename}_{Item.Text}_{item.StartIndex}_{item.EndIndex}_{item.Value}{NormalizedValue}");
                    }
                }
            }
            else
            {
                RegexData ValueofRegexData = (RegexData)Item.Data;
                if (!string.IsNullOrEmpty(ValueofRegexData.Value))
                {
                    OutputText.Add($"{Filename}_{Item.Text}_{ValueofRegexData.StartIndex}_{ValueofRegexData.EndIndex}_{ValueofRegexData.Value}");
                }
            }
        }
        string Text = string.Join(Environment.NewLine, OutputText.Where(x => x.Length > 0).Select(x => x)).Replace("_", "\t");
        return Text;
    }
    #endregion

    #region ValidateResults
    private static void ValidateResults(List<string> opt, IEnumerable<string> ValidationSource)
    {
        Console.Title = "Validating data...";

        //Read all validation files
        List<string> LoadedValidationFiles = [];
        foreach (string FilePath in ValidationSource)
        {
            using StreamReader sr = new(FilePath);
            LoadedValidationFiles.Add(sr.ReadToEnd());
            sr.Close();
        }

        //Initialize
        string[] ValidationSplited = string.Join("\n", LoadedValidationFiles).Split("\n").Where(x => !string.IsNullOrEmpty(x)).Select(x => x).ToArray();
        string[] OutputSplited = string.Join("\n", opt).Split("\n").Where(x => !string.IsNullOrEmpty(x)).Select(x => x).ToArray();
        List<string> ValidatedList = []; //For all validated answers
        List<(string OriginalValue, string CorrectValue)> MismatchList = []; //For all mismatched answers (from output)
        List<string> MissingList = []; //For all missing answers (from answer)
        List<string> ListToBeAnalyze = [.. OutputSplited]; //From OutputSplited, items left here will be analyzed further.

        for (int i = 0; i < ValidationSplited.Length; i++)
        {
            string CurrentItem = ValidationSplited[i];
            Match RegexOfCurrentItem = RegexPatterns.Answer().Match(CurrentItem);

            //Get filename
            string CUR_Filename = RegexOfCurrentItem.Groups[1].Value;
            string CUR_PHIType = RegexOfCurrentItem.Groups[2].Value.Trim();
            string CUR_PHIStartIndex = RegexOfCurrentItem.Groups[3].Value.Trim();
            string CUR_PHIEndIndex = RegexOfCurrentItem.Groups[4].Value.Trim();
            string CUR_PHIValue = RegexOfCurrentItem.Groups[5].Value.Trim();
            //For time
            Match RegexOfValidTime = RegexPatterns.ValidationTime().Match(CUR_PHIValue);
            string CUR_TimeValue = RegexOfValidTime.Groups[1].Value.Trim();
            string CUR_NormalizedValue = RegexOfValidTime.Groups[2].Value.Trim();

            //Display message
            double ProcessPercent = Math.Round((double)i / (double)ValidationSplited.Length * 100, 2);
            Console.Clear();
            Console.WriteLine("Processing validation data... This may take a while...");
            Console.WriteLine("Current filename: {0} ({1}/{2}) | {3}%", CUR_Filename, i, ValidationSplited.Length, ProcessPercent);

            string PHIDataText = $"{CUR_Filename}\t{CUR_PHIType}\t{CUR_PHIStartIndex}\t{CUR_PHIEndIndex}\t{CUR_PHIValue}";
            if (OutputSplited.Any(x => x.Contains(PHIDataText)))
            {
                //Add to validated list
                ValidatedList.Add(CurrentItem);
                //Remove the item from to be analyze list
                ListToBeAnalyze.Remove(CurrentItem);
            }

            if (CUR_PHIType.Equals("TIME"))
            {
                foreach (string item in OutputSplited)
                {
                    Match RegexOfOutput = RegexPatterns.Answer().Match(item);
                    Match RegexOfOutputTime = RegexPatterns.ValidationTime().Match(CUR_PHIValue);
                    string OPT_Filename = RegexOfOutput.Groups[1].Value.Trim();
                    string OPT_PHIType = RegexOfOutput.Groups[2].Value.Trim();
                    string OPT_PHIStartIndex = RegexOfOutput.Groups[3].Value.Trim();
                    string OPT_PHIEndIndex = RegexOfOutput.Groups[4].Value.Trim();
                    string OPT_TimeValue = RegexOfOutputTime.Groups[1].Value.Trim();
                    string OPT_NormalizedValue = RegexOfOutputTime.Groups[2].Value.Trim();
                    if (OPT_Filename.Equals(CUR_Filename) && OPT_PHIType.Equals(CUR_PHIType) 
                        && OPT_PHIStartIndex.Equals(CUR_PHIStartIndex) && OPT_PHIEndIndex.Equals(CUR_PHIEndIndex) 
                        && CUR_TimeValue.Equals(OPT_TimeValue) && CUR_NormalizedValue.Equals(OPT_NormalizedValue))
                    {
                        ValidatedList.Add(CurrentItem);
                        ListToBeAnalyze.Remove(CurrentItem);
                    }
                }
            }

            //Analyze ListToBeAnalyze
            string[] SameFileandTypeList = ListToBeAnalyze.Where(x => x.Contains($"{CUR_Filename}\t{CUR_PHIType}")).ToArray();
            foreach (string item in SameFileandTypeList)
            {
                Match RegexOfLeftItem = RegexPatterns.Answer().Match(item);
                string Left_Filename = RegexOfLeftItem.Groups[1].Value.Trim();
                string Left_PHIStartIndex = RegexOfLeftItem.Groups[3].Value.Trim();
                string Left_PHIEndIndex = RegexOfLeftItem.Groups[4].Value.Trim();
                string Left_PHIValue = RegexOfCurrentItem.Groups[5].Value.Trim();
                //Make sure it's same file, eg: 10
                if (Left_Filename.Equals(CUR_Filename))
                {
                    Match RegexOfLeftTime = RegexPatterns.ValidationTime().Match(Left_PHIValue);
                    string Left_TimeValue = RegexOfLeftTime.Groups[1].Value.Trim();
                    string Left_NormalizedValue = RegexOfLeftTime.Groups[2].Value.Trim();
                    //Check if it's a date or time
                    if (RegexOfValidTime.Success)
                    {
                        bool IsStartIndexSame = Left_PHIStartIndex == CUR_PHIStartIndex; 
                        bool IsEndIndexSame = Left_PHIEndIndex == CUR_PHIEndIndex;
                        bool IsValueSame = Left_TimeValue != CUR_TimeValue;
                        bool IsNormalizedValueSame = Left_NormalizedValue != CUR_NormalizedValue;
                        if ((IsStartIndexSame | IsEndIndexSame) && (IsValueSame | IsNormalizedValueSame))
                        {
                            if (!MismatchList.Contains((item, CUR_PHIValue)))
                            {
                                MismatchList.Add((item, CUR_PHIValue));
                                ListToBeAnalyze.Remove(item);
                            }
                        }
                    }
                    else //Nah, it's not a date or time
                    {
                        bool IsValueSame = Left_PHIValue != CUR_PHIValue;
                        if (Left_TimeValue == CUR_PHIStartIndex && IsValueSame
                            | Left_NormalizedValue == CUR_PHIEndIndex && IsValueSame)
                        {
                            if (!MismatchList.Contains((item, CUR_PHIValue)))
                            {
                                MismatchList.Add((item, CUR_PHIValue));
                                ListToBeAnalyze.Remove(item);
                            }
                        }
                    }
                }
            }
        }

        //Generate missing value list
        MissingList = ValidationSplited.Where(x => !ValidatedList.Contains(x)).ToList();
        for (int i = 0; i < MismatchList.Count; i++)
        {
            Console.Clear();
            //Get filename
            Match Mismatched = RegexPatterns.Answer().Match(MismatchList[i].OriginalValue);
            double ProcessPercent = Math.Round((double)i / (double)MismatchList.Count * 100, 2);
            Console.WriteLine("Generating missing values list... This may take a while...");
            Console.WriteLine("Current filename: {0} ({1}/{2}) | {3}%", Mismatched.Groups[1].Value, i, MismatchList.Count, ProcessPercent);
            foreach (string item2 in MissingList.ToArray())
            {
                Match Missing = RegexPatterns.Answer().Match(item2);
                bool IsFilenameSame = Mismatched.Groups[1].Value.Equals(Missing.Groups[1].Value);
                bool IsTypeSame = Mismatched.Groups[2].Value.Equals(Missing.Groups[2].Value);
                bool IsStartIndexSame = Mismatched.Groups[3].Value.Equals(Missing.Groups[3].Value);
                bool IsEndIndexSame = Mismatched.Groups[4].Value.Equals(Missing.Groups[4].Value);
                //Check filename, type
                if (IsFilenameSame && IsTypeSame)
                {
                    if (IsStartIndexSame | IsEndIndexSame)
                    {
                        MissingList.Remove(item2);
                    }
                }
            }
        }

        Console.WriteLine("Analyze Done!");
        //Calculate hit-rate
        double HitRate = (double)ValidatedList.Count / (double)ValidationSplited.Length * 100;

        //Save validation result
        (string Validation, string Mismatch, string Missing) Location = (!string.IsNullOrEmpty(Config.AppConfig.ValidateResultLocation)
            ? "validated_answer.txt" : Path.Combine(AppContext.BaseDirectory, "validated_answer.txt"),
            !string.IsNullOrEmpty(Config.AppConfig.ValidateResultLocation) ? "mismatch_answer.txt"
                                  : Path.Combine(AppContext.BaseDirectory, "mismatch_answer.txt"),
                                  !string.IsNullOrEmpty(Config.AppConfig.ValidateResultLocation) ? "missing_answer.txt"
                                            : Path.Combine(AppContext.BaseDirectory, "missing_answer.txt"));

        //Save files
        for (int i = 1; i < 4; i++)
        {
            using StreamWriter sw = new(i switch
            {
                1 => Location.Validation,
                2 => Location.Mismatch,
                3 => Location.Missing,
                _ => throw new NotImplementedException()
            });
            sw.WriteLine(string.Format("Report generated at {0}", DateTime.Now));
            if (i is 1)
            {
                sw.WriteLine(string.Format("Validation source(s): {0}", string.Join(", ", ValidationSource.Select(x => Path.GetFileName(x)))));
                sw.WriteLine(string.Format("Output entries: {0} | Answer entries: {1}", OutputSplited.Length, ValidationSplited.Length));
                sw.WriteLine(string.Format("Correct entries: {0} | Mismatch entries: {1} | Missing entries: {2} | Hit-rate: {3}%", ValidatedList.Count, MismatchList.Count, MissingList.Count, Math.Round(HitRate, 2)));
                sw.WriteLine("======================================================================");
                sw.WriteLine(string.Join('\n', ValidatedList).Trim());
            }
            else
            {
                sw.WriteLine(i is 2
                    ? string.Format("Mismatch entries: {0}", MismatchList.Count)
                    : string.Format("Missing entries: {0}", MissingList.Count));
                sw.WriteLine("======================================================================");
                sw.WriteLine(string.Join('\n', i is 2 ? MismatchList.Select(x => $"{x.OriginalValue}\n| Correct => {x.CorrectValue}") : MissingList).Trim());
            }
            sw.Close();
        }

        //Show validation result
        Console.WriteLine("\nOutput entries: {0} | Answer entries: {1}", OutputSplited.Length, ValidationSplited.Length);
        Console.WriteLine("Correct entries: {0} | Mismatch entries: {1} | Missing entries: {2} | Hit-rate: {3}%", ValidatedList.Count, MismatchList.Count, MissingList.Count, Math.Round(HitRate, 2));
        Console.WriteLine("Validation result is saved to {0}\nMismatch result is saved to {1}\nMissing result is saved to {2}", Location.Validation, Location.Mismatch, Location.Missing);
        Console.WriteLine("Press [Y] to open validation file, [M] key return to main menu OR any key to exit.");
        switch (Console.ReadKey().Key)
        {
            case ConsoleKey.Y:
                Utils.OpenEditor(Location.Validation);
                break;
            case ConsoleKey.M:
                Console.Beep();
                break;
            default:
                Environment.Exit(0);
                break;
        }
    }
    #endregion
}
