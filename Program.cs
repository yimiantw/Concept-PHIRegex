﻿using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;

namespace ConceptPHIRegex;

internal partial class Program
{
    #region Main Entry
    static void Main()
    {
        while (true)
        {
            //Read config
            Config.ReadConfig();

            try
            {
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
                string[] FilesArray = Directory.GetFiles(@"D:\Test\DataSets\Second_Phase", "*.txt", SearchOption.TopDirectoryOnly);
                Config.AppConfig.ValidateFileLocations = new string[]
                {
                    @"D:\Test\DataSets\first_answer.txt",
                    @"D:\Test\DataSets\second_answer.txt"
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
                            ValidateResults(opt);
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
                    Console.WriteLine($"Type \"Y\" to use it");
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
            string State = Regex_Address.Groups[4].Value.Trim();
            Data.State = new()
            {
                Value = State,
                StartIndex = RawData.IndexOf(State),
                EndIndex = RawData.IndexOf(State) + State.Length
            };
            string Zip = Regex_Address.Groups[5].Value.Trim();
            Data.Zip = new()
            {
                Value = Zip,
                StartIndex = RawData.IndexOf(Zip),
                EndIndex = RawData.IndexOf(Zip) + Zip.Length
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
                                            .Concat(RegexPatterns.DoctorVariantFour().Matches(RawData));
        if (Regex_Doctors.Any())
        {
            foreach (Match Doc in Regex_Doctors)
            {
                string TrimmedDoc = RegexPatterns.DocUnusedString().Replace(Doc.Groups[1].Value, string.Empty).Trim();
                if (!Data.Doctors.Any(x => x.Value.Contains(TrimmedDoc)))
                {
                    Data.Doctors.Add(new()
                    {
                        Value = TrimmedDoc,
                        StartIndex = RawData.IndexOf(Doc.Groups[1].Value),
                        EndIndex = RawData.IndexOf(Doc.Groups[1].Value) + Doc.Groups[1].Value.Length,
                    });
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
    private static void ValidateResults(List<string> opt)
    {
        Console.Title = "Validating data...";

        //Read all validation files
        List<string> LoadedValidationFiles = [];
        foreach (string FilePath in Config.AppConfig.ValidateFileLocations.Where(Path.Exists))
        {
            using StreamReader sr = new(FilePath);
            LoadedValidationFiles.Add(sr.ReadToEnd());
            sr.Close();
        }

        //Initialize
        string[] ValidationSplited = string.Join("\n", LoadedValidationFiles).Split("\n").Where(x => !string.IsNullOrEmpty(x)).Select(x => x).ToArray();
        string[] OutputSplited = string.Join("\n", opt).Split("\n").Where(x => !string.IsNullOrEmpty(x)).Select(x => x).ToArray();
        List<string> ValidatedList = []; //For all validated answers
        List<string> MismatchList = []; //For all mismatched answers (from output)
        List<string> MissingList = []; //For all missing answers (from answer)
        List<string> ListToBeAnalyze = [.. OutputSplited]; //From OutputSplited, items left here will be analyzed further.

        for (int i = 0; i < ValidationSplited.Length; i++)
        {
            //Get filename
            string FilenameInCurrentLine = RegexPatterns.Answer().Match(ValidationSplited[i]).Groups[1].Value;

            //Display message
            Console.Clear();
            Console.WriteLine("Processing validation data... This may take a while...");
            Console.WriteLine("Current filename: {0} ({1}/{2}) | {3}%", FilenameInCurrentLine, i, ValidationSplited.Length, Math.Round((double)i / (double)ValidationSplited.Length * 100, 2));

            //Check whether the output has the same item from answer.txt
            if (OutputSplited.Contains(ValidationSplited[i]))
            {
                //Add to validated list
                ValidatedList.Add(ValidationSplited[i]);
                //Remove the item from to be analyze list
                ListToBeAnalyze.Remove(ValidationSplited[i]);
            }

            //Analyze ListToBeAnalyze
            Match MatchFromValidation = RegexPatterns.Answer().Match(ValidationSplited[i]);
            var SameFileandTypeList = ListToBeAnalyze.Where(x => x.Contains($"{MatchFromValidation.Groups[1].Value}\t{MatchFromValidation.Groups[2].Value}")).ToArray();
            foreach (string item in SameFileandTypeList)
            {
                Match mt = RegexPatterns.Answer().Match(item);
                //Make sure it's same file, eg: 10
                if (mt.Groups[1].Value.Trim().Equals(MatchFromValidation.Groups[1].Value.Trim()))
                {
                    Match ValidationTimeRegex = RegexPatterns.ValidationTime().Match(MatchFromValidation.Groups[5].Value.Trim());
                    //Check if it's a date or time
                    if (ValidationTimeRegex.Success)
                    {
                        Match TimeRegex = RegexPatterns.ValidationTime().Match(mt.Groups[5].Value.Trim());
                        bool IsStartIndexSame = mt.Groups[3].Value.Trim() == MatchFromValidation.Groups[3].Value.Trim();
                        bool IsEndIndexSame = mt.Groups[4].Value.Trim() == MatchFromValidation.Groups[4].Value.Trim();
                        bool IsValueSame = TimeRegex.Groups[1].Value.Trim() != ValidationTimeRegex.Groups[1].Value.Trim();
                        bool IsNormalizedValueSame = TimeRegex.Groups[2].Value.Trim() != ValidationTimeRegex.Groups[2].Value.Trim();
                        if ((IsStartIndexSame | IsEndIndexSame) && (IsValueSame | IsNormalizedValueSame))
                        {
                            if (!MismatchList.Contains(item))
                            {
                                MismatchList.Add(item);
                                ListToBeAnalyze.Remove(item);
                            }
                        }
                    }
                    else //Nah, it's not a date or time
                    {
                        if (mt.Groups[3].Value.Trim() == MatchFromValidation.Groups[3].Value.Trim() && mt.Groups[5].Value.Trim() != MatchFromValidation.Groups[5].Value.Trim() 
                            | mt.Groups[4].Value.Trim() == MatchFromValidation.Groups[4].Value.Trim() && mt.Groups[5].Value.Trim() != MatchFromValidation.Groups[5].Value.Trim())
                        {
                            if (!MismatchList.Contains(item))
                            {
                                MismatchList.Add(item);
                                ListToBeAnalyze.Remove(item);
                            }
                        }
                    }
                }

            }
        }

        //Generate missing value list
        Console.WriteLine("Generating missing values list...");
        MissingList = ValidationSplited.Where(x => !ValidatedList.Contains(x)).ToList();
        foreach (string item in MismatchList)
        {
            Match Mismatched = RegexPatterns.Answer().Match(item);
            foreach (string item2 in MissingList.ToArray())
            {
                Match Missing = RegexPatterns.Answer().Match(item2);
                //Check filename, type
                if (Mismatched.Groups[1].Value.Equals(Missing.Groups[1].Value) && Mismatched.Groups[2].Value.Equals(Missing.Groups[2].Value))
                {
                    if (Mismatched.Groups[3].Value.Equals(Missing.Groups[3].Value) | Mismatched.Groups[4].Value.Equals(Missing.Groups[4].Value))
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
        string ValidationSaveLocation = !string.IsNullOrEmpty(Config.AppConfig.ValidateResultLocation)
                                            ? "validated_answer.txt"
                                            : Path.Combine(AppContext.BaseDirectory, "validated_answer.txt");
        string MismatchSaveLocation = !string.IsNullOrEmpty(Config.AppConfig.ValidateResultLocation)
                                            ? "mismatch_answer.txt"
                                            : Path.Combine(AppContext.BaseDirectory, "mismatch_answer.txt");
        string MissingSaveLocation = !string.IsNullOrEmpty(Config.AppConfig.ValidateResultLocation)
                                            ? "missing_answer.txt"
                                            : Path.Combine(AppContext.BaseDirectory, "missing_answer.txt");

        //Save files
        //File.WriteAllText(ValidationSaveLocation, string.Join('\n', ValidatedList));
        //File.WriteAllText(MismatchSaveLocation, string.Join('\n', MismatchList));
        //File.WriteAllText(MissingSaveLocation, string.Join('\n', MissingList));
        for (int i = 1; i < 4; i++)
        {
            switch (i)
            {
                case 1:
                    {
                        using StreamWriter sw = new(ValidationSaveLocation);
                        sw.WriteLine(string.Format("Report generated at {0}", DateTime.Now));
                        sw.WriteLine(string.Format("Validation source(s): {0}", string.Join(", ", Config.AppConfig.ValidateFileLocations.Select(x => Path.GetFileName(x)))));
                        sw.WriteLine(string.Format("Output entries: {0} | Answer entries: {1}", OutputSplited.Length, ValidationSplited.Length));
                        sw.WriteLine(string.Format("Correct entries: {0} | Mismatch entries: {1} | Missing entries: {2} | Hit-rate: {3}%", ValidatedList.Count, MismatchList.Count, MissingList.Count, Math.Round(HitRate, 2)));
                        sw.WriteLine("======================================================================");
                        sw.WriteLine(string.Join('\n', ValidatedList).Trim());
                        sw.Close();
                    }
                    break;
                case 2:
                    {
                        using StreamWriter sw = new(MismatchSaveLocation);
                        sw.WriteLine(string.Format("Report generated at {0}", DateTime.Now));
                        sw.WriteLine(string.Format("Mismatch entries: {0}", MismatchList.Count));
                        sw.WriteLine("======================================================================");
                        sw.WriteLine(string.Join('\n', MismatchList).Trim());
                        sw.Close();
                    }
                    break;
                case 3:
                    {
                        using StreamWriter sw = new(MissingSaveLocation);
                        sw.WriteLine(string.Format("Report generated at {0}", DateTime.Now));
                        sw.WriteLine(string.Format("Missing entries: {0}", MissingList.Count));
                        sw.WriteLine("======================================================================");
                        sw.WriteLine(string.Join('\n', MissingList).Trim());
                        sw.Close();
                    }
                    break;
            }
        }

        //Show validation result
        Console.WriteLine("\nOutput entries: {0} | Answer entries: {1}", OutputSplited.Length, ValidationSplited.Length);
        Console.WriteLine("Correct entries: {0} | Mismatch entries: {1} | Missing entries: {2} | Hit-rate: {3}%", ValidatedList.Count, MismatchList.Count, MissingList.Count, Math.Round(HitRate, 2));
        Console.WriteLine("Validation result is saved to {0}", ValidationSaveLocation);
        Console.WriteLine("Mismatch result is saved to {0}", MismatchSaveLocation);
        Console.WriteLine("Missing result is saved to {0}", MissingSaveLocation);
        Console.WriteLine("Press [Y] to open validation file, [M] key return to main menu OR any key to exit.");
        switch (Console.ReadKey().Key)
        {
            case ConsoleKey.Y:
                Utils.OpenEditor(ValidationSaveLocation);
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