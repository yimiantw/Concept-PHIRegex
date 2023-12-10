## 模式 Patterns

本隊使用下方列出的的正規表達式模式進行 PHI 資訊的擷取：

* **類型: ID**\
IDNUM: ```\d{2}[A-Z]\d{5,7}[A-Z0-9]?```\
MEDICALRECORD: ```\d{5,7}.[A-Z]{3}```

* **類型: Name**\
PATIENT: ```^[A-Za-z]{2,}\s?[A-Za-z]{2,},\s?[A-Za-z]{2,}\s?[A-Za-z]+```\
DOCTOR (變體一): ```(?:Dr|DR|PRO)[\s?|.]\s?([A-Z]+\s?.?\s?\w+?\s?\w+(?:\s\w{3,})?(?:-[A-Z]+)?)```\
DOCTOR (變體二): ```^\w+:\s+\(([A-Z]+\s+\w+)\)```\
DOCTOR (變體三): ```(?:Result)\s?\w+\.?\s?(\w+\s?\.?\s?\w+\.?\w+)```\
DOCTOR (變體四): ```TO: ([A-Z]{2})(?:;[\s{2}]?|:[\s{2}]?|\s{2})\s?([A-Z]{2})```\
DOCTOR (變體五): ```\(([A-Z]{2})\/```\
USERNAME: ```\(([A-Z]{2})\/```

* **類型: Profession**\
PROFESSION: ```(?:is\sa\s(\w+)|(\w+)\sjob)```

* **類型: Location**\
DEPARTMENT & HOSPITAL: ```^Location:\s{2}((?:\d\/\d\s)?\w+\s?\w+\s?\w+\s?)\-\s?(\w+\s?\w+.?\w+)```\
ORGANIZATION: ```(?:\((\w+(?:\W|\s)?\w+\s(?:Corporation|Company))|(?:Performed\sat\s)(\w+\s?(?:&|\s)\s?\w+\s?\w+)|(?:Department\sof\s\w+\s?\w?)\,\s(\w+)\,\s\w)```\
STREET, CITY, STATE, ZIP, LOCATION-OTHER: ```^(?:(\w+(?:\s?\w)+)|((?:PO|P.O.)\s(?:BOX)\s\d{2,4}))\n(\w+(?:\s?\w)+)\s{2}(?:((?:[A-Za-z]+\s){1,})\s{1,2}(\d{4})|([A-Za-z]+\s[A-Za-z]+)(\d{4})|\s{2}(\d{4}))```\
COUNTRY: ```USA|AUS```

* **類型: Age**\
AGE (變體一): ```(?:in\s)?(\d{1,3})\s?(?:yo|yr|years\sold)```\
AGE (變體二): ```(?:age)\s?(\d{1,3})```

* **類型: Date**\
DATE & TIME: ```(?:(\d{3,4})Hrs\s{1}on\s)?(\d{1,2}[/|\.]\d{1,2}[/|\.]\d{2,4})(?:\s{1}at\s{1}(\d{1,2}:\d{2}))?```\
TIME (變體一): ```([0-9]{1,2}:?[0-9]{1,2})(?:[H|h]rs)\s?(?:on|at)\s?([0-9]{1,2}[.|/][0-9]{1,2}[.|/][0-9]{1,2})```\
TIME (變體二): ```(?:at\s)?([0-9]{1,2}[\.|:][0-9]{1,2})(?:am|pm)?\s?(?:on)?\s?([0-9]{1,2}[.|/][0-9]{1,2}[.|/][0-9]{1,2})```\
TIME (變體三): ```([0-9]{1,2}[.|/][0-9]{1,2}[.|/][0-9]{2,4})\s?(?:at|on)\s?([0-9]{1,2}[:|.][0-9]{1,2})(?:am|pm)?```\
DATE (變體一): ```([0-9]{2})-([A-Za-z]{3})-([0-9]{2,4})```\
DURATION: ```(?:((?:\d{1,3}|\w{3,}))\s?(?i)((?:(?:week|wk)|(?:year|yr)|month|day|time|(?:hour|hr)|(?:minute|min)|(?:second|sec))(?:s)?))```\
SET: ```(?i)(?:once|twice|thrice)```

* **類型: Contract**\
PHONE: ```\((\d{4}\s?\d{4})\)```\
FAX: ```\d{2}-\d{4}-\d{4}```\
EMAIL: ```\w.+@\w.+\.\w+```\
URL: ```([-a-zA-Z0-9@:%._\+~#=]{1,256}\.[a-zA-Z0-9()]{1,6})\b[-a-zA-Z0-9()@:%_\+.~#?&//=]*```\
IPADDR: ```\d{1,3}\.\d{1,3}\.\d{1,3}\.\d{1,3}```

## 參考資料

 - [What is a good regular expression to match a URL?](https://stackoverflow.com/questions/3809401/what-is-a-good-regular-expression-to-match-a-url)