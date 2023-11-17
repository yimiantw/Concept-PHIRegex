## Configuration
This program generates a JSON-based config called ```config.json``` at launch at the first time.

The default config shows as following:
```
{
  "PreviousLocation": "",
  "SaveLocation": "",
  "SaveFilename": "answer.txt",
  "EditorLocation": "notepad.exe"
}
```
* **PreviousLocation** (Type: String)\
The location of previous used dataset folder or file.

* **SaveLocation** (Type: String)\
The location of the result file where .

* **SaveFilename** (Type: String, Default: answer.txt)\
The name of the result file ***(File extension must be included. e.g. .txt)***.

* **EditorLocation** (Type: String, Default: notepad.exe)\
The editor will be opened when the result file generated.

Using backslash(```\```) to escape in the path strings is unnecessary because the .NET is smart enough to identify the string.