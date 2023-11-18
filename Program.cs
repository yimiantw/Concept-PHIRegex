using System.Diagnostics;
using System.Text.RegularExpressions;

namespace ConceptPHIRegex;

internal partial class Program
{
    static void Main()
    {
        //Read config
        Config.ReadConfig();

        try
        {
#if !DEBUG
            string InputPath = GetConsoleInput();
            IEnumerable<string> FilesArray = Enumerable.Empty<string>();
            //Check whether the input is a directory or a file
            if (File.GetAttributes(InputPath).HasFlag(FileAttributes.Directory))
            {
                //Enumerate all text (.txt) files
                FilesArray = Directory.EnumerateFiles(InputPath, "*.txt", SearchOption.TopDirectoryOnly);

                //Check whether there is any text file in the target directory
                if (!FilesArray.Any())
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
                FilesArray = new string[] { InputPath };
            }

            //Save path to config
            Config.AppConfig.PreviousLocation = InputPath;
            Config.WriteConfig();
#else
            IEnumerable<string> FilesArray = new[] { @"D:\Second_Phase_Text_Dataset\1702.txt" };
            //IEnumerable<string> FilesArray = new[] { @"D:\First_Phase_Text_Dataset\491.txt" };
#endif
            //Calcuate process time
            Stopwatch ProcessTime = new();
            ProcessTime.Start();
            Console.Title = "Processing data...";
            Console.WriteLine("Processing data...Please don't close the window");

            //Initialize data class
            List<PHIData> List_PHIData = [];
            List<string> opt = [];
            foreach (string item in FilesArray)
            {
                using StreamReader sr = new(item);
                string RawData = sr.ReadToEnd();
                sr.Close();
                string Filename = Path.GetFileNameWithoutExtension(item);
                PHIData ProcessedData = ProcessRegexFromRawData(RawData);
                List_PHIData.Add(ProcessedData);
                opt.Add(GenerateOutput(Filename, ProcessedData));
            }

            //Display processed time
            ProcessTime.Stop();
            Console.Title = "Done!";
            Console.WriteLine("\nTotal files: {0} | Process Time: {1}ms | Memory Used: {2} MB\n", FilesArray.Count(), ProcessTime.ElapsedMilliseconds, Math.Round(((float)Process.GetCurrentProcess().WorkingSet64 / 1048576), 3));

            // Write results into text file
            string SaveLocation = Path.Combine(!string.IsNullOrEmpty(Config.AppConfig.SaveLocation)
                                    ? Config.AppConfig.SaveLocation
                                    : AppContext.BaseDirectory, Config.AppConfig.SaveFilename);
            StreamWriter sw = new(SaveLocation);
            sw.WriteLine(string.Join(Environment.NewLine, opt));
            sw.Close();
            Console.WriteLine("The results are saved to {0}, press Y key open the file or any key to close.", SaveLocation);

            //Ask user whether want to open the result file
            if (Console.ReadKey().Key is ConsoleKey.Y)
            {
                ProcessStartInfo info = new()
                {
                    FileName = Config.AppConfig.EditorLocation,
                    Arguments = !string.IsNullOrEmpty(Config.AppConfig.SaveFilename)
                                ? SaveLocation
                                : AppContext.BaseDirectory + Config.AppConfig.SaveFilename,
                };
                Process.Start(info);
            };
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.ToString());
            Console.ReadKey();
        }
    }

    internal static string GetConsoleInput()
    {
        //Load intro texts here, not in the loop
        string IntroTexts = Utils.ReadIntroTexts();
        while (true)
        {
            if (!string.IsNullOrEmpty(Config.AppConfig.PreviousLocation))
            {
                Console.WriteLine($"Found previous location: {Config.AppConfig.PreviousLocation}");
                Console.WriteLine($"Type \"Y\" to use it");
            }
            Console.Write(IntroTexts);
            string? Input = Console.ReadLine();
            if (Input != null)
            {
                //Clear console
                Console.Clear();
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
                        string Street = Regex_Address.Groups[1].Value;
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
                        string LocationOther = Regex_Address.Groups[2].Value;
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
            string City = Regex_Address.Groups[3].Value;
            Data.City = new()
            {
                Value = City,
                StartIndex = RawData.IndexOf(City),
                EndIndex = RawData.IndexOf(City) + City.Length
            };
            string State = Regex_Address.Groups[4].Value;
            Data.State = new()
            {
                Value = State,
                StartIndex = RawData.IndexOf(State),
                EndIndex = RawData.IndexOf(State) + State.Length
            };
            string Zip = Regex_Address.Groups[5].Value;
            Data.Zip = new()
            {
                Value = Zip,
                StartIndex = RawData.IndexOf(Zip),
                EndIndex = RawData.IndexOf(Zip) + Zip.Length
            };
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
                                            .Concat(RegexPatterns.DoctorVariantThree().Matches(RawData));
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
        if (Regex_Address.Success)
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

    internal static string GenerateOutput(string Filename, PHIData Data)
    {
        List<string> OutputText = [];
        (object Data, string Text)[] PHI = new (object, string)[]
        {
            (Data.IDs, "IDNUM"), (Data.MedicalRecord, "MEDICALRECORD"), (Data.Patient, "PATIENT"), (Data.Doctors, "DOCTOR"),
            (Data.Username, "USERNAME"), (Data.Profession, "PROFESSION"), (Data.Department, "DEPARTMENT"), (Data.Hospital, "HOSPITAL"),
            (Data.Orgainzations, "ORGANIZATION"), (Data.Street, "STREET"), (Data.City, "CITY"), (Data.State, "STATE"),
            (Data.Zip, "ZIP"), (Data.LocationOther, "LOCATION-OTHER"), (Data.Age, "AGE"), (Data.Dates, "DATE"), 
            (Data.Times, "TIME"), (Data.Durations, "DURATION"), (Data.Sets, "SET"), (Data.Phone, "PHONE"), (Data.Fax, "FAX"), (Data.Email, "EMAIL"), 
            (Data.URL, "URL"), (Data.IPAddr, "IPADDR"), 
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
        string Text = string.Join(Environment.NewLine, OutputText).Replace("_", "\t");
        Console.WriteLine(Text);
        return Text;
    }
}