using System.Diagnostics;
using System.Reflection;
using System.Text;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.VisualBasic;

namespace ConceptPHIRegex;

internal partial class Program
{
    static void Main()
    {
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
#else
            IEnumerable<string> FilesArray = new string[] { @"D:\Second_Phase_Text_Dataset\1093.txt" };
            //IEnumerable<string> FilesArray = new string[] { @"D:\First_Phase_Text_Dataset\491.txt" };
#endif
            //Calcuate process time
            Stopwatch ProcessTime = new();
            ProcessTime.Start();
            Console.Title = "Processing data...Please don't close the window";

            //Initialize data class
            List<PHIData> List_PHIData = new();
            List<string> opt = new();
            foreach (string item in FilesArray)
            {
                string RawData = Utils.ReadRawData(item);
                string Filename = Path.GetFileNameWithoutExtension(item);
                PHIData ProcessedData = ProcessRegexFromRawData(RawData);
                List_PHIData.Add(ProcessedData);
                opt.Add(GenerateOutput(Filename, ProcessedData));
            }

            //Display processed time
            ProcessTime.Stop();
            Console.WriteLine(Environment.NewLine);
            Console.WriteLine("Total files: {0} | Process Time: {1}ms", FilesArray.Count(), ProcessTime.ElapsedMilliseconds);
            StreamWriter sw = new(@"D:\answer.txt");
            sw.WriteLine(string.Join(Environment.NewLine, opt));
            sw.Close();
            Console.WriteLine($@"The results are saved to D:\answer.txt, press Y key open the file or any key to close.");
            if (Console.ReadKey().Key == ConsoleKey.Y)
            {
                ProcessStartInfo info = new()
                {
                    FileName = @"C:\Program Files\VSCodium\VSCodium.exe",
                    Arguments = @"D:\answer.txt"
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
        string IntroTexts = Utils.ReadIntroTexts();
        while (true)
        {
            Console.Write(IntroTexts);
            string? Input = Console.ReadLine();
            Console.Clear();
            Input = Input!.Replace("\"", string.Empty);
            if (string.IsNullOrEmpty(Input))
            {
                return AppContext.BaseDirectory;
            }
            if (Path.Exists(Input))
                return Input;
            else
                Console.WriteLine("Error: The folder or file doesn't exist, please try again.");

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
            //Street or Location-Other
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
        if (Regex_DateAndTime.Any())
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
        if (Regex_Organization.Any())
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

        //#region Match: Profession
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
        //#endregion

        return Data;
    }

    internal static string GenerateOutput(string Filename, PHIData item)
    {
        List<string> OutputText = new();
        if (item.IDs.Any())
        {
            foreach (RegexData ID in item.IDs)
            {
                OutputText.Add($"{Filename}_IDNUM_{ID.StartIndex}_{ID.EndIndex}_{ID.Value}");
            }
        }
        if (!string.IsNullOrEmpty(item.MedicalRecord.Value))
        {
            OutputText.Add($"{Filename}_MEDICALRECORD_{item.MedicalRecord.StartIndex}_{item.MedicalRecord.EndIndex}_{item.MedicalRecord.Value}");
        }
        if (!string.IsNullOrEmpty(item.Patient.Value))
        {
            OutputText.Add($"{Filename}_PATIENT_{item.Patient.StartIndex}_{item.Patient.EndIndex}_{item.Patient.Value}");
        }
        if (!string.IsNullOrEmpty(item.Street.Value))
        {
            OutputText.Add($"{Filename}_STREET_{item.Street.StartIndex}_{item.Street.EndIndex}_{item.Street.Value}");
        }
        if (!string.IsNullOrEmpty(item.City.Value))
        {
            OutputText.Add($"{Filename}_CITY_{item.City.StartIndex}_{item.City.EndIndex}_{item.City.Value}");
        }
        if (!string.IsNullOrEmpty(item.State.Value))
        {
            OutputText.Add($"{Filename}_STATE_{item.State.StartIndex}_{item.State.EndIndex}_{item.State.Value}");
        }
        if (!string.IsNullOrEmpty(item.Zip.Value))
        {
            OutputText.Add($"{Filename}_ZIP_{item.Zip.StartIndex}_{item.Zip.EndIndex}_{item.Zip.Value}");
        }
        if (item.Dates.Any())
        {
            foreach (RegexData Date in item.Dates)
            {
                OutputText.Add($"{Filename}_DATE_{Date.StartIndex}_{Date.EndIndex}_{Date.Value}_{Utils.ConvertToISO8601(ConvertForm.Date, Date.Value)}");
            }
        }
        if (item.Times.Any())
        {
            foreach (RegexData Time in item.Times)
            {
                OutputText.Add($"{Filename}_TIME_{Time.StartIndex}_{Time.EndIndex}_{Time.Value}_{Utils.ConvertToISO8601(ConvertForm.Time, Time.Value)}");
            }
        }
        if (!string.IsNullOrEmpty(item.Department.Value))
        {
            OutputText.Add($"{Filename}_DEPARTMENT_{item.Department.StartIndex}_{item.Department.EndIndex}_{item.Department.Value}");
        }
        if (!string.IsNullOrEmpty(item.Hospital.Value))
        {
            OutputText.Add($"{Filename}_HOSPITAL_{item.Hospital.StartIndex}_{item.Hospital.EndIndex}_{item.Hospital.Value}");
        }
        if (item.Doctors.Any())
        {
            foreach (RegexData item3 in item.Doctors)
            {
                OutputText.Add($"{Filename}_DOCTOR_{item3.StartIndex}_{item3.EndIndex}_{item3.Value}");
            }
        }
        if (!string.IsNullOrEmpty(item.Phone.Value))
        {
            OutputText.Add($"{Filename}_PHONE_{item.Phone.StartIndex}_{item.Phone.EndIndex}_{item.Phone.Value}");
        }
        if (item.Orgainzations.Any())
        {
            foreach (RegexData Org in item.Orgainzations)
            {
                OutputText.Add($"{Filename}_ORGANIZATION_{Org.StartIndex}_{Org.EndIndex}_{Org.Value}");
            }
        }
        if (!string.IsNullOrEmpty(item.Age.Value))
        {
            OutputText.Add($"{Filename}_AGE_{item.Age.StartIndex}_{item.Age.EndIndex}_{item.Age.Value}");
        }
        if (!string.IsNullOrEmpty(item.LocationOther.Value))
        {
            OutputText.Add($"{Filename}_LOCATION-OTHER_{item.LocationOther.StartIndex}_{item.LocationOther.EndIndex}_{item.LocationOther.Value}");
        }
        if (!string.IsNullOrEmpty(item.URL.Value))
        {
            OutputText.Add($"{Filename}_URL_{item.URL.StartIndex}_{item.URL.EndIndex}_{item.URL.Value}");
        }
        if (!string.IsNullOrEmpty(item.IPAddr.Value))
        {
            OutputText.Add($"{Filename}_IPADDRESS_{item.IPAddr.StartIndex}_{item.IPAddr.EndIndex}_{item.IPAddr.Value}");
        }
        if (!string.IsNullOrEmpty(item.Fax.Value))
        {
            OutputText.Add($"{Filename}_FAX_{item.Fax.StartIndex}_{item.Fax.EndIndex}_{item.Fax.Value}");
        }
        if (!string.IsNullOrEmpty(item.Username.Value))
        {
            OutputText.Add($"{Filename}_USERNAME_{item.Username.StartIndex}_{item.Username.EndIndex}_{item.Username.Value}");
        }
        if (!string.IsNullOrEmpty(item.Profession.Value))
        {
            OutputText.Add($"{Filename}_PROFESSION_{item.Profession.StartIndex}_{item.Profession.EndIndex}_{item.Profession.Value}");
        }
        string gens = string.Join("\n", OutputText).Replace("_", "\t");
        Console.WriteLine(gens);
        return gens;
    }
}