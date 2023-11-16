
# PHI Data Finder (Concept)

This program is designed to find any PHI with Regular Expression from medical records.


## Patterns

This concept uses following regex patterns to identify PHI data:

* Category: ID
Identify Number: ```\d{2}[A-Z]\d{5,7}[A-Z0-9]?```\
Medical Record: ```"\d{5,7}.[A-Z]{3}"```

* Category: Name
Patient Name: ```^[A-Za-z]{2,}\s?[A-Za-z]{2,},\s?[A-Za-z]{2,}\s?[A-Za-z]+```\
Doctor (Variant I.): ```(?:Dr|DR|PRO)[\s?|.]\s?([A-Z]+\s?.?\s?\w+?\s?\w+(?:\s\w{3,})?)```\
Doctor (Variant II.): ```^\w+:\s+\(([A-Z]+\s+\w+)\)```\
Doctor (Variant III.): ```(?:Result)\s?\w+\.?\s?(\w+\s?\.?\s?\w+\.?\w+)```\
Username: ```[A-Za-z]{2,}\w?\d{3,}```

* Category: Profession
Profession: ```(?:is\sa\s(\w+)|(\w+)\sjob)```

* Category: Location
Location (Department & Hospital): ```^Location:\s{2}((?:\d\/\d\s)?\w+\s?\w+\s?\w+\s?)\-\s?(\w+\s?\w+.?\w+)```\
Organization: ```(?:\((\w+(?:\W|\s)?\w+\s(?:Corporation|Company))|(?:Performed\sat\s)(\w+\s?(?:&|\s)\s?\w+\s?\w+)|(?:Department\sof\s\w+\s?\w?)\,\s(\w+)\,\s\w)```\
Address (Street, City, State, Zip, Location-Other): ```^(?:(\w+(?:\s?\w)+)|((?:PO|P.O.)\s(?:BOX)\s\d{2,4}))\n(\w+(?:\s?\w)+)\s{2}((?:[A-Za-z]+\s){1,})\s{1,2}(\d{4})```

* Category: Age
Age (Variant I.): ```(?:in\s)?(\d{1,3})\s?(?:yo|yr|years\sold)```\
Age (Variant II.): ```(?:age)\s?(\d{1,3})```

* Category: Date
Date & Time: ```(?:(\d{3,4})Hrs\s{1}on\s)?(\d{1,2}[/|\.]\d{1,2}[/|\.]\d{2,4})(?:\s{1}at\s{1}(\d{1,2}:\d{2}))?```

* Category: Contract
Phone: ```\((\d{4}\s?\d{4})\)```\
Fax: ```\d{2}-\d{4}-\d{4}```\
Email: ```\w.+@\w.+\.\w+```\
URL: ```([-a-zA-Z0-9@:%._\+~#=]{1,256}\.[a-zA-Z0-9()]{1,6})\b[-a-zA-Z0-9()@:%_\+.~#?&//=]*```\
IPAddress (IPv4): ```\d{1,3}\.\d{1,3}\.\d{1,3}\.\d{1,3}```

## Performance
Tested with 1734 files where the data from first phase dataset and second phase dataset
* Compiled w/ Native AOT (Binary size: 3.41MB)\
```Total files: 1734 | Process Time: 2198ms | Memory Used: 35.793 MB```

* Compiled w/o Native AOT (Binary size: 73.1 MB)\
```Total files: 1734 | Process Time: 2760ms | Memory Used: 65.32 MB```

Compiled w/ Native AOT provides 2143.6% smaller binary size, 20.3% lower processing time and 54.8% lower memory usage.

## Prerequisites

Since this project is developed in .NET 8.0, you can use Visual Studio 2022 to build and debug 

* [Visual Studio 2022 (17.8 or later)](https://learn.microsoft.com/en-us/visualstudio/releases/2022/release-notes)

OR, you can use editors with runtime installed

* [Visual Studio Code](https://code.visualstudio.com/)
* [VSCodium](https://vscodium.com/)
* [.NET 8.0 SDK / Runtime](https://dotnet.microsoft.com/en-us/download/dotnet/8.0)

You can run the project with following commands if you choose second method:
```
cd <project_folder>
dotnet run 
```
OR build
```
cd <project_folder>
dotnet build 
```
It's recommended to install SDK instead of runtime since this repository doesn't release any executables. You'll need to compile the whole project on your own.


## Acknowledgements

 - [Regex101](https://regex101.com/)
 - [What is a good regular expression to match a URL?](https://stackoverflow.com/questions/3809401/what-is-a-good-regular-expression-to-match-a-url)
 - [隱私保護與醫學數據標準化競賽：解碼臨床病例、讓數據說故事](https://codalab.lisn.upsaclay.fr/competitions/15425)

