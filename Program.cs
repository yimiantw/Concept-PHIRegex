﻿using System.Diagnostics;
using System.Text.RegularExpressions;

namespace ConceptPHIRegex;

internal partial class Program
{
    #region Main Entry
    static void Main(string[] args)
    {
        try
        {
            (bool IsSatisfied, bool NoMergeFile, string? DatasetPath, string? DLResult) = Utils.IsRuntimeSatisfied();
            string[] FilesArray = [];

            #region Normal mode
            if (!IsSatisfied)
            {
#if !DEBUG
                while (true)
                {
                    Console.Clear();
                    Console.Write(@"Dataset path (e.g. D:\TestDatasets):");
                    DatasetPath = Console.ReadLine()!.Replace("\"", string.Empty);
                    if (!string.IsNullOrEmpty(DatasetPath) && Path.Exists(DatasetPath) && Directory.EnumerateFiles(DatasetPath, "*.txt", SearchOption.TopDirectoryOnly).Any())
                    {
                        break;
                    }
                }

                while (true)
                {
                    Console.Clear();
                    Console.Write(@"Deep learning answer path (e.g. D:\dl_answer.txt):");
                    DLResult = Console.ReadLine()!.Replace("\"", string.Empty);
                    if (!string.IsNullOrEmpty(DLResult) && File.Exists(DLResult) && File.ReadAllLines(DLResult).Length > 0)
                    {
                        break;
                    }
                }
#else
                    string[] FilesArray = Directory.GetFiles(@"D:\TestDatasets\opendid_test", "*.txt", SearchOption.TopDirectoryOnly);
                    DLResult = @"D:\Test\DataSets\answer_DL.txt";
#endif
            }

            //Get files
            FilesArray = File.GetAttributes(DatasetPath).HasFlag(FileAttributes.Directory)
            ? Directory.GetFiles(DatasetPath, "*.txt", SearchOption.TopDirectoryOnly)
            : [DatasetPath];

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
                Console.WriteLine("Current processing file: {0} ({1}/{2}) | {3}%\n", Path.GetFileName(FilesArray[i]), i, FilesArray.Length - 1, Math.Round((double)i / (double)FilesArray.Length * 100, 2));

                using StreamReader sr = new(FilesArray[i]);
                string RawData = sr.ReadToEnd();
                sr.Close();
                string Filename = Path.GetFileNameWithoutExtension(FilesArray[i]);
                PHIData ProcessedData = ProcessRegexFromRawData(RawData);
                List_PHIData.Add(ProcessedData);
                opt.Add(GenerateOutput(Filename, ProcessedData));
            }

            ValidateResults(opt, [DLResult], NoMergeFile);

            //Display processed time
            ProcessTime.Stop();
            Console.WriteLine("Done!");
            Console.Title = "Done!";
            Console.WriteLine("\nTotal files: {0} | Process Time: {1}ms | Memory Used: {2} MB\n", FilesArray.Length, ProcessTime.ElapsedMilliseconds, Math.Round(((float)Process.GetCurrentProcess().WorkingSet64 / 1048576), 3));

            Console.WriteLine("Final result is saved to {0}", Path.Combine(AppContext.BaseDirectory, "answer.txt"));
            Console.WriteLine("Press [Y] to open validation file OR any key to exit.");
            switch (Console.ReadKey().Key)
            {
                case ConsoleKey.Y:
                    Utils.OpenEditor("answer.txt");
                    break;
                default:
                    Environment.Exit(0);
                    break;
            }
            #endregion

        }
#if !DEBUG
        catch (Exception ex)
        {
            Console.WriteLine("An error occured: \n{0}", ex.Message);
            Console.ReadKey();
#else
        catch (Exception)
        {
#endif
            throw;
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
        //Time
        MatchCollection Regex_TimeVariantOne = RegexPatterns.TimeVariantOne().Matches(RawData);
        if (Regex_TimeVariantOne.Count > 0)
        {
            foreach (Match MatchTime in Regex_TimeVariantOne.Cast<Match>())
            {
                string[] DateSplited = MatchTime.Groups[2].Value.Contains('/')
                                        ? MatchTime.Groups[2].Value.Split('/')
                                        : MatchTime.Groups[2].Value.Split('.');
                string ReplacedString = MatchTime.Groups[1].Value.Replace(":", string.Empty);
                string Hour = ReplacedString[..2];
                string Minute = ReplacedString[2..];
                string FT_Date_Month = DateSplited[1].Length is 1 ? $"0{DateSplited[1]}" : DateSplited[1];
                string FT_Date_Day = DateSplited[0].Length is 1 ? $"0{DateSplited[0]}" : DateSplited[0];
                string FT_Date_Year = DateSplited[2].Length is 2 ? $"20{DateSplited[2]}" : DateSplited[2];
                string FT_Date = $"{FT_Date_Year}-{FT_Date_Month}-{FT_Date_Day}";
                string FT_Time_Minute = Minute.Length is 1 ? $"{Minute}0" : Minute;
                FT_Time_Minute = Minute.Length < 1 ? "00" : FT_Time_Minute;
                string FT_Time = $"T{Hour}:{FT_Time_Minute}";
                string FT_Full = FT_Date.Trim() + FT_Time.Trim();
                Data.Times.Add(new()
                {
                    Value = MatchTime.Value.Trim(),
                    StartIndex = MatchTime.Index,
                    EndIndex = MatchTime.Index + MatchTime.Length,
                    NormalizedValue = $"_{FT_Date + FT_Time}".Trim()
                });
            }
        }
        MatchCollection Regex_TimeVariantTwo = RegexPatterns.TimeVariantTwo().Matches(RawData);
        if (Regex_TimeVariantTwo.Count > 0)
        {
            foreach (Match MatchTime in Regex_TimeVariantTwo.Cast<Match>())
            {
                string[] DateSplited = MatchTime.Groups[2].Value.Contains('/')
                                        ? MatchTime.Groups[2].Value.Split('/')
                                        : MatchTime.Groups[2].Value.Split('.');
                string ReplacedString = MatchTime.Groups[1].Value.Replace(":", string.Empty).Replace(".", string.Empty);
                string Hour = ReplacedString.Length is 4 ? ReplacedString[..2] : ReplacedString[..1];
                string Minute = ReplacedString.Length is 4 ? ReplacedString[2..] : ReplacedString[1..];
                string FT_Date_Month = DateSplited[1].Length is 1 ? $"0{DateSplited[1]}" : DateSplited[1];
                string FT_Date_Day = DateSplited[0].Length is 1 ? $"0{DateSplited[0]}" : DateSplited[0];
                string FT_Date_Year = DateSplited[2].Length is 2 ? $"20{DateSplited[2]}" : DateSplited[2];
                string FT_Date = $"{FT_Date_Year.Trim()}-{FT_Date_Month.Trim()}-{FT_Date_Day.Trim()}";

                string FT_Time_Hour = Hour.Length is 1 ? $"0{Hour}" : Hour;
                string FT_Time = $"T{FT_Time_Hour.Trim()}:{Minute.Trim()}";
                Data.Times.Add(new()
                {
                    Value = MatchTime.Value.Trim(),
                    StartIndex = MatchTime.Index,
                    EndIndex = MatchTime.Index + MatchTime.Length,
                    NormalizedValue = $"_{FT_Date + FT_Time}".Trim()
                });
            }
        }
        MatchCollection Regex_TimeVariantThree = RegexPatterns.TimeVariantThree().Matches(RawData);
        if (Regex_TimeVariantThree.Count > 0)
        {
            foreach (Match MatchTime in Regex_TimeVariantThree.Cast<Match>())
            {
                string[] DateSplited = MatchTime.Groups[1].Value.Contains('/')
                                        ? MatchTime.Groups[1].Value.Split('/')
                                        : MatchTime.Groups[1].Value.Split('.');
                string ReplacedString = MatchTime.Groups[2].Value.Replace(":", string.Empty).Replace(".", string.Empty);
                string Hour = ReplacedString.Length is 4 ? ReplacedString[..2] : ReplacedString[..1];
                string Minute = ReplacedString.Length is 4 ? ReplacedString[2..] : ReplacedString[1..];
                string FT_Date_Month = DateSplited[1].Length is 1 ? $"0{DateSplited[1]}" : DateSplited[1];
                string FT_Date_Day = DateSplited[0].Length is 1 ? $"0{DateSplited[0]}" : DateSplited[0];
                string FT_Date_Year = DateSplited[2].Length is 2 ? $"20{DateSplited[2]}" : DateSplited[2];
                string FT_Date = $"{FT_Date_Year.Trim()}-{FT_Date_Month.Trim()}-{FT_Date_Day.Trim()}";

                string FT_Time_Hour = Hour.Length is 1 ? $"0{Hour}" : Hour;
                string FT_Time = $"T{FT_Time_Hour.Trim()}:{Minute.Trim()}";
                Data.Times.Add(new()
                {
                    Value = MatchTime.Value.Trim(),
                    StartIndex = MatchTime.Index,
                    EndIndex = MatchTime.Index + MatchTime.Length,
                    NormalizedValue = $"_{FT_Date + FT_Time}".Trim()
                });
            }
        }

        //Date
        MatchCollection Regex_DateVariantOne = RegexPatterns.DateVariantOne().Matches(RawData);
        if (Regex_DateVariantOne.Count > 0)
        {
            foreach (Match item in Regex_DateVariantOne.Cast<Match>())
            {
                string Year = item.Groups[3].Value;
                Year = Year.Length is 2 ? $"20{Year}" : Year;
                string Month = Utils.MonthsDigit[item.Groups[2].Value.ToUpper()];
                string Day = item.Groups[1].Value;
                string Full = $"_{Year}-{Month}-{Day}";
                Data.Dates.Add(new()
                {
                    Value = item.Value.Trim(),
                    StartIndex = item.Index,
                    EndIndex = item.Index + Full.Length,
                    NormalizedValue = Full.Trim()
                });
            }
        }

        MatchCollection Regex_DateAndTime = RegexPatterns.DateAndTime().Matches(RawData);
        if (Regex_DateAndTime.Count > 0)
        {
            IEnumerable<string> Dates = Regex_DateAndTime.Where(x => !x.Value.Contains("at") & !x.Value.Contains("on")).Select(x => x.Value);
            IEnumerable<string> Times = Regex_DateAndTime.Where(x => !Dates.Contains(x.Value)).Select(x => x.Value);
            foreach (string MatchDate in Dates)
            {
                if (!Data.Times.Any(x => x.Value.Contains(MatchDate)))
                {
                    Data.Dates.Add(new()
                    {
                        Value = MatchDate,
                        StartIndex = RawData.IndexOf(MatchDate),
                        EndIndex = RawData.IndexOf(MatchDate) + MatchDate.Length,
                    });
                }
            }
            foreach (string MatchTime in Times)
            {
                if (!Data.Times.Any(x => x.Value.Contains(MatchTime)))
                {
                    Data.Times.Add(new()
                    {
                        Value = MatchTime,
                        StartIndex = RawData.IndexOf(MatchTime),
                        EndIndex = RawData.IndexOf(MatchTime) + MatchTime.Length,
                    });
                }
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
                    string NoNewLine = item.Value.Split('\n').First().Trim();
                    string TrimmedDoc = RegexPatterns.DocUnusedString().Replace(NoNewLine, string.Empty);
                    if (!Data.Doctors.Any(x => x.Value.Contains(TrimmedDoc)))
                    {
                        if (item.Value.Length is 2)
                        {
                            Data.Doctors.Add(new()
                            {
                                Value = NoNewLine,
                                StartIndex = item.Index,
                                EndIndex = item.Index + NoNewLine.Length,
                            });
                        }
                        else
                        {
                            Data.Doctors.Add(new()
                            {
                                Value = TrimmedDoc,
                                StartIndex = RawData.IndexOf(item.Value),
                                EndIndex = RawData.IndexOf(item.Value) + NoNewLine.Length,
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
                        if (string.IsNullOrEmpty(item.NormalizedValue))
                        {
                            string NormalizedValue = Utils.IsSpecialToken(Item.Text) ? Utils.GetNormalizedString(Item.Text switch
                            {
                                "DATE" => ConvertForm.Date,
                                "TIME" => ConvertForm.Time,
                                "DURATION" => ConvertForm.Duration,
                                "SET" => ConvertForm.Set,
                                _ => throw new NotImplementedException()
                            }, item.Value) : string.Empty;

                            OutputText.Add($"{Filename}_{Item.Text}_{item.StartIndex}_{item.EndIndex}_{item.Value}{NormalizedValue}");
                        }
                        else
                        {
                            OutputText.Add($"{Filename}_{Item.Text}_{item.StartIndex}_{item.EndIndex}_{item.Value}{item.NormalizedValue}");
                        }
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
    private static void ValidateResults(List<string> opt, IEnumerable<string> ValidationSource, bool NoMergeFile = true)
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
            string[] CUR_SplitedString = ValidationSplited[i].Split('\t');
            if (CUR_SplitedString.Length > 4)
            {
                string CUR_Filename = CUR_SplitedString[0].Trim();
                string CUR_PHIType = CUR_SplitedString[1].Trim();
                string CUR_PHIStartIndex = CUR_SplitedString[2].Trim();
                string CUR_PHIEndIndex = CUR_SplitedString[3].Trim();
                string CUR_PHIValue = Utils.IsSpecialToken(CUR_PHIType)
                                        ? $"{CUR_SplitedString[4].Trim()}\t{CUR_SplitedString[5].Trim()}"
                                        : CUR_SplitedString[4].Trim();
                string PHIDataText = $"{CUR_Filename}\t{CUR_PHIType}\t{CUR_PHIStartIndex}\t{CUR_PHIEndIndex}\t{CUR_PHIValue}";
                //For time
                string[] ValidTimeSplited = CUR_PHIValue.Split('\t');
                string CUR_TimeValue = ValidTimeSplited[0];
                string CUR_NormalizedValue = ValidTimeSplited.Length > 1 ? ValidTimeSplited[1] : string.Empty;

                //Display message
                double ProcessPercent = Math.Round((double)i / (double)ValidationSplited.Length * 100, 2);
                Console.Clear();
                Console.WriteLine("Processing validation data... This may take a while...");
                Console.WriteLine("Current filename: {0} ({1}/{2}) | {3}%", CUR_Filename, i, ValidationSplited.Length - 1, ProcessPercent);

                if (OutputSplited.Any(x => x.Contains(PHIDataText)))
                {
                    //Add to validated list
                    ValidatedList.Add(PHIDataText);
                    //Remove the item from to be analyze list
                    ListToBeAnalyze.Remove(PHIDataText);
                }

                //Console.ReadKey();
                if (CUR_PHIType.Equals("TIME"))
                {
                    foreach (string item in OutputSplited)
                    {
                        string[] OutputTimeArray = CUR_PHIValue.Split('\t');
                        //
                        if (OutputTimeArray.Length > 1)
                        {
                            Match RegexOfOutput = RegexPatterns.Answer().Match(item);
                            string OPT_Filename = RegexOfOutput.Groups[1].Value.Trim();
                            string OPT_PHIType = RegexOfOutput.Groups[2].Value.Trim();
                            string OPT_PHIStartIndex = RegexOfOutput.Groups[3].Value.Trim();
                            string OPT_PHIEndIndex = RegexOfOutput.Groups[4].Value.Trim();
                            string OPT_TimeValue = OutputTimeArray[0].Trim();
                            string OPT_NormalizedValue = OutputTimeArray[1].Trim();
                            bool IsFilenameSame = OPT_Filename.Equals(CUR_Filename);
                            bool IsTypeSame = OPT_PHIType.Equals(CUR_PHIType);
                            bool IsStartIndexSame = OPT_PHIStartIndex.Equals(CUR_PHIStartIndex);
                            bool IsEndIndexSame = OPT_PHIEndIndex.Equals(CUR_PHIEndIndex);
                            bool IsTimeValueSame = CUR_TimeValue.Equals(OPT_TimeValue);
                            bool IsNormalizedValueSame = CUR_NormalizedValue.Equals(OPT_NormalizedValue);
                            if (IsFilenameSame && IsTypeSame && IsStartIndexSame
                                && IsEndIndexSame && IsTimeValueSame && IsNormalizedValueSame)
                            {
                                ValidatedList.Add(PHIDataText);
                                ListToBeAnalyze.Remove(PHIDataText);
                            }
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
                    string Left_PHIValue = RegexOfLeftItem.Groups[5].Value.Trim();
                    //Make sure it's same file, eg: 10
                    if (Left_Filename.Equals(CUR_Filename))
                    {
                        Match RegexOfLeftTime = RegexPatterns.ValidationTime().Match(Left_PHIValue);
                        string Left_TimeValue = RegexOfLeftTime.Groups[1].Value.Trim();
                        string Left_NormalizedValue = RegexOfLeftTime.Groups[2].Value.Trim();
                        //Check if it's a date or time
                        if (ValidTimeSplited.Length > 1)
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
        }

        //Generate missing value list
        MissingList = ValidationSplited.Where(x => !ValidatedList.Contains(x.Trim())).ToList();
        for (int i = 0; i < MismatchList.Count; i++)
        {
            Console.Clear();
            //Get filename
            string[] MismatchSplited = MismatchList[i].OriginalValue.Split('\t');
            double ProcessPercent = Math.Round((double)i / (double)MismatchList.Count * 100, 2);
            Console.WriteLine("Generating missing values list... This may take a while...");
            Console.WriteLine("Current filename: {0} ({1}/{2}) | {3}%", MismatchSplited[0], i, MismatchList.Count - 1, ProcessPercent);

            foreach (string item2 in MissingList.ToArray())
            {
                string[] MissingSplited = item2.Split("\t");
                if (MissingSplited.Length > 3)
                {
                    bool IsFilenameSame = MismatchSplited[0].Equals(MissingSplited[0]);
                    bool IsTypeSame = MismatchSplited[1].Equals(MissingSplited[1]);
                    bool IsStartIndexSame = MismatchSplited[2].Equals(MissingSplited[2]);
                    bool IsEndIndexSame = MismatchSplited[3].Equals(MissingSplited[3]);
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
        }

        if (!NoMergeFile)
        {
            Console.Clear();
            List<string> newlist = [.. OutputSplited];
            for (int mi = 0; mi < MissingList.ToArray().Length; mi++)
            {
                //Show progress
                Console.Clear();
                double ProcessPercent = Math.Round((double)mi / (double)MissingList.Count * 100, 2);
                Console.WriteLine("Merging files... This may take a while...");
                Console.WriteLine("{0}/{1} | {2}%", mi, MissingList.Count - 1, ProcessPercent);

                string Filename = MissingList[mi].Split("\t")[0].Trim();
                for (int i = 0; i < OutputSplited.Length; i++)
                {
                    string OUT_Filename = OutputSplited[i].Split("\t")[0].Trim();
                    string ext_spt_fn = (i + 1) < OutputSplited.Length ? OutputSplited[i + 1].Split('\t')[0].Trim() : string.Empty;
                    if (Filename.Equals(OUT_Filename) && ext_spt_fn.Trim() != OUT_Filename)
                    {
                        newlist.Insert(newlist.IndexOf(OutputSplited[i]), MissingList[mi]);
                        MissingList.Remove(MissingList[mi]);
                    }
                }
            }

            using StreamWriter sw = new("answer.txt");
            sw.WriteLine(string.Join('\n', newlist));
            sw.Close();
        }
    }
    #endregion
}
