
# PHI資料分析器

本隊結合深度學習及正規表達式來分析病例中的 Protected Health Infomation (PHI) 資訊

## 模式 Patterns

本隊使用的模式清單可至: [PATTERNS.md](PATTERNS.md) 了解

## 執行環境

### 模型訓練及推理
* [Python 3.11.7](https://www.python.org/downloads/release/python-3117/)

使用的PIP套件清單：
- numpy
- tqdm
- datasets
- transformers
- islab-opendeid
- torch
- torchvision 
- torchaudio

### 正規表達式

以 Visual Studio 2022 為編寫環境，.NET SDK的版本為 8.0
* [Visual Studio 2022 (17.8 or later)](https://learn.microsoft.com/en-us/visualstudio/releases/2022/release-notes)
* [.NET 8.0 SDK / Runtime](https://dotnet.microsoft.com/en-us/download/dotnet/8.0)

專案預設開啟 NativeAOT 編譯，因此需要於 Visual Studio 安裝程式中勾選**使用 C++ 的桌面開發**
* [Install C and C++ support in Visual Studio](https://learn.microsoft.com/en-us/cpp/build/vscpp-step-0-installation?view=msvc-170)


## 使用方法

### I. 模型訓練及推理

* 程式碼目錄中的 Python_Scripts 皆為 Python 腳本

| 檔案名稱 | 說明 |
| ----------- | ----------- |
| Training_rev2.ipynb | 模型訓練腳本 |
| Prediction_rev3.ipynb | 為資料推理腳本 | 

* Training_rev2.ipynb 內參數說明

| 檔案名稱 | 說明 |
| ----------- | ----------- |
| MODEL_SAVEDIR | 模型儲存目錄 |
| TRAINING_DATASET_PATH | 訓練集目錄路徑 | 
| MODEL_FILENAME | 模型儲存名稱 |
| LANGUAGE_MODEL | 語言模型名稱，預設為：EleutherAI/pythia-70m | 
| DATALOADER_BATCH_SIZE | 資料載入器的 batch 載入大小，預設為：8 |
| TORCH_DEVICE | PyTorch 的處理後端，首選為cuda、次選為mps、最後選為cpu | 
| FORCE_CLEAR_MEMORY_AFTER_EPOCH | 每一期結束後皆清除處理後端的快取，預設為：False |
| EPOCHS | 訓練期數，預設為：10 | 

* Prediction_rev3.ipynb 內參數說明

| 檔案名稱 | 說明 |
| ----------- | ----------- |
| MODEL_DIR | 模型目錄 |
| MODEL_FILENAME | 模型名稱 |
| SAVE_LOCATION_WITH_FILENAME | 推理結果儲存路徑 | 
| TEST_DATASET_PATH | 欲推理的資料集路徑 | 
| VALIDATE_OUT_PATH | 驗證結果輸出路徑|
| LANGUAGE_MODEL | 語言模型名稱，預設為：EleutherAI/pythia-70m | 
| TORCH_DEVICE | PyTorch 的處理後端，首選為cuda、次選為mps、最後選為cpu | 
| PREDICT_BATCH_SIZE | 資料載入器的 batch 載入大小，預設為：32 | 

* 輸出結果

| 檔案名稱 | 說明 |
| ----------- | ----------- |
| Training_rev2.ipynb | 在指定的目錄下，產生<指定的名稱>.pt (例：70m_epoch.pt) |
| Prediction_rev3.ipynb | 在指定的目錄下，產生<指定的名稱>.txt (例：answer_dl.txt) | 

其中推理結果的檔案應為此格式：```1097	MEDICALRECORD	1	11	433475.RDC```

### II. 正規表達式

* 在終端機以下列指令打開編譯完成的 **(需先切換到程式所在的目錄)**
```bash
.\Concept-PHIRegex.exe --dataset <需推理資料集路徑> --result <模型推理完成輸出的文字檔案路徑>
```

* 完成後，可以 **Y** 按鍵來打開 answer.txt
```bash
Merging files... This may take a while...
956/956 | 99.9%
Done!

Total files: 950 | Process Time: 7443ms | Memory Used: 42.664 MB

Final result is saved to D:\Test\answer.txt
Press [Y] to open validation file OR any key to exit.
```

* answer.txt 將在程式所在的目錄中，且格式應為：
```bash
1097	MEDICALRECORD	1	11	433475.RDC
```

## 參考資料

 - [Regex101](https://regex101.com/)
 - [隱私保護與醫學數據標準化競賽：解碼臨床病例、讓數據說故事](https://codalab.lisn.upsaclay.fr/competitions/15425)

