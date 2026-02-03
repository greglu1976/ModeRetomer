using System;
using System.Collections.Generic;
using OfficeOpenXml;
using System.Linq;

namespace ModeRetomer
{
    class Mode
    {
        public Dictionary<string, string> SgfParameters { get; private set; }
        public Dictionary<string, string> Settings { get; private set; }
        public Dictionary<string, string> Inputs { get; private set; }
        public Dictionary<string, string> Outputs { get; private set; }

        public Dictionary<string, string> NormInputs { get; private set; } // Словарь для создания входов на GUI
        public Dictionary<string, string> NormOutputs { get; private set; } // Словарь для создания выходов на GUI
        public List<double> AnalRetomGr1 { get; private set; }
        public List<double> AnalRetomGr2 { get; private set; }
        public List<double> AnalRetomGr3 { get; private set; }
        public List<double> AnalRetomGr4 { get; private set; }
        public bool isActiveGr1 { get; private set; }
        public bool isActiveGr2 { get; private set; }
        public bool isActiveGr3 { get; private set; }
        public bool isActiveGr4 { get; private set; }
        public List<bool> OutputsRetom { get; private set; }
        public string ModeName { get; private set; }


        // Конструктор для работы с List<KeyValuePair> (сохраняет порядок)
        public Mode(string filePath, List<KeyValuePair<string, string>> inputsJSON,
                   List<KeyValuePair<string, string>> outputsJSON, string ModeName_)
        {
            if (string.IsNullOrWhiteSpace(filePath))
                throw new ArgumentException("File path cannot be null or empty.", nameof(filePath));

            // Инициализация словарей
            SgfParameters = new Dictionary<string, string>();
            Settings = new Dictionary<string, string>();
            Inputs = new Dictionary<string, string>();
            Outputs = new Dictionary<string, string>();
            ModeName = ModeName_;

            // Загрузка данных из файла
            LoadDataFromExcel(filePath);
            NormDicts(inputsJSON, outputsJSON);
            CreateSignalLists();
        }

        // Перегрузка конструктора для работы с Dictionary (сохраняет порядок в .NET Core 3.0+)
        public Mode(string filePath, Dictionary<string, string> inputsJSON,
                   Dictionary<string, string> outputsJSON, string ModeName_)
        {
            if (string.IsNullOrWhiteSpace(filePath))
                throw new ArgumentException("File path cannot be null or empty.", nameof(filePath));

            // Инициализация словарей
            SgfParameters = new Dictionary<string, string>();
            Settings = new Dictionary<string, string>();
            Inputs = new Dictionary<string, string>();
            Outputs = new Dictionary<string, string>();
            ModeName = ModeName_;

            // Загрузка данных из файла
            LoadDataFromExcel(filePath);
            NormDicts(inputsJSON, outputsJSON);
            CreateSignalLists();
        }

        private void LoadDataFromExcel(string filePath)
        {
            ExcelPackage.License.SetNonCommercialOrganization("My Noncommercial organization");

            using (var package = new ExcelPackage(new System.IO.FileInfo(filePath)))
            {
                // Обработка листа 'SGF_Parameters'
                ProcessWorksheet(package, "SGF_Parameters", SgfParameters);

                // Обработка листа 'Settings'
                ProcessWorksheet(package, "Settings", Settings);

                // Обработка листа 'Inputs'
                ProcessWorksheet(package, "Inputs", Inputs);

                // Обработка листа 'Outputs'
                ProcessWorksheet(package, "Outputs", Outputs);
            }
        }

        private void ProcessWorksheet(ExcelPackage package, string sheetName, Dictionary<string, string> dictionary)
        {
            var worksheet = package.Workbook.Worksheets[sheetName];
            if (worksheet == null)
                throw new InvalidOperationException($"Worksheet '{sheetName}' not found in the Excel file.");

            int rowCount = worksheet.Dimension.Rows;
            int colCount = worksheet.Dimension.Columns;

            // Предполагаем, что ключи находятся в первой строке, значения - во второй
            for (int col = 1; col <= colCount; col++)
            {
                var key = worksheet.Cells[1, col].Value?.ToString();
                var value = worksheet.Cells[2, col].Value?.ToString();

                if (!string.IsNullOrWhiteSpace(key))
                {
                    dictionary[key] = value ?? string.Empty;
                }
            }
        }

        // Метод для работы с List<KeyValuePair> (гарантированное сохранение порядка)
        private void NormDicts(List<KeyValuePair<string, string>> inputsJSON, List<KeyValuePair<string, string>> outputsJSON)
        {
            const int requiredCount = 16;
            NormInputs = new Dictionary<string, string>();
            NormOutputs = new Dictionary<string, string>();

            // Обработка входов (Inputs) - порядок из inputsJSON сохраняется
            int inputProcessed = 0;
            foreach (var kvp in inputsJSON) // ← порядок гарантирован, так как это List
            {
                if (inputProcessed >= requiredCount) break;

                var excelKey = kvp.Key;
                var jsonValue = kvp.Value;

                if (Inputs.ContainsKey(excelKey))
                {
                    NormInputs[jsonValue] = Inputs[excelKey];
                    inputProcessed++;
                }
            }

            // Заполнение резервных входов
            for (int i = inputProcessed; i < requiredCount; i++)
            {
                NormInputs[$"Вход {i + 1}"] = "0";
            }

            // Обработка выходов (Outputs) - порядок из outputsJSON сохраняется
            int outputProcessed = 0;
            foreach (var kvp in outputsJSON)
            {
                if (outputProcessed >= requiredCount) break;

                var excelKey = kvp.Key;          // ключ из Excel (например, "Relay1")
                var guiLabel = kvp.Value;        // метка для GUI (например, "Отключение")

                if (Outputs.ContainsKey(excelKey))
                {
                    NormOutputs[guiLabel] = Outputs[excelKey];
                    outputProcessed++;
                }
            }

            // Заполнение резервных выходов
            for (int i = outputProcessed; i < requiredCount; i++)
            {
                NormOutputs[$"Выход {i + 1}"] = "0";
            }
        }

        // Метод для работы с Dictionary (для .NET Core 3.0+ порядок сохраняется)
        private void NormDicts(Dictionary<string, string> inputsJSON, Dictionary<string, string> outputsJSON)
        {
            const int requiredCount = 16;
            NormInputs = new Dictionary<string, string>();
            NormOutputs = new Dictionary<string, string>();

            // Обработка входов (Inputs)
            int inputProcessed = 0;
            foreach (var kvp in inputsJSON) // В .NET Core 3.0+ Dictionary сохраняет порядок вставки
            {
                if (inputProcessed >= requiredCount) break;

                var excelKey = kvp.Key;
                var jsonValue = kvp.Value;

                if (Inputs.ContainsKey(excelKey))
                {
                    NormInputs[jsonValue] = Inputs[excelKey];
                    inputProcessed++;
                }
            }

            // Заполнение резервных входов
            for (int i = inputProcessed; i < requiredCount; i++)
            {
                NormInputs[$"Вход {i + 1}"] = "0";
            }

            // Обработка выходов (Outputs)
            int outputProcessed = 0;
            foreach (var kvp in outputsJSON)
            {
                if (outputProcessed >= requiredCount) break;

                var excelKey = kvp.Key;
                var guiLabel = kvp.Value;

                if (Outputs.ContainsKey(excelKey))
                {
                    NormOutputs[guiLabel] = Outputs[excelKey];
                    outputProcessed++;
                }
            }

            // Заполнение резервных выходов
            for (int i = outputProcessed; i < requiredCount; i++)
            {
                NormOutputs[$"Выход {i + 1}"] = "0";
            }
        }

        // Создание списков аналоговых сигналов
        public void CreateSignalLists()
        {
            // Инициализация списков
            AnalRetomGr1 = new List<double>();
            AnalRetomGr2 = new List<double>();
            AnalRetomGr3 = new List<double>();
            AnalRetomGr4 = new List<double>();

            // Инициализация флагов активности
            isActiveGr1 = false;
            isActiveGr2 = false;
            isActiveGr3 = false;
            isActiveGr4 = false;

            // Задаем порядок ключей для каждого списка
            var firstListKeys = new List<string>
            {
                "IA", "dIA", "IB", "dIB", "IC", "dIC",
                "UA1", "dUA1", "UB1", "dUB1", "UC1", "dUC1"
            };

            var secondListKeys = new List<string>
            {
                "IA1", "dIA1", "IB1", "dIB1", "IC1", "dIC1",
                "UA2", "dUA2", "UB2", "dUB2", "UC2", "dUC2"
            };

            var thirdListKeys = new List<string>
            {
                "IA2harm", "IB2harm", "IC2harm"
            };

            var fourthListKeys = new List<string>
            {
                "IA5harm", "IB5harm", "IC5harm"
            };


            // Значения по умолчанию для производных величин
            var defaultValues = new Dictionary<string, double>
            {
                {"dIA", 0.0}, {"dIB", 240.0}, {"dIC", 120.0},
                {"dUA1", 0.0}, {"dUB1", 240.0}, {"dUC1", 120.0},
                {"dIA1", 0.0}, {"dIB1", 240.0}, {"dIC1", 120.0},
                {"dUA2", 0.0}, {"dUB2", 240.0}, {"dUC2", 120.0}
            };

            // Проверяем активность групп перед заполнением
            isActiveGr1 = Inputs.ContainsKey("IA") || Inputs.ContainsKey("UA1");
            isActiveGr2 = Inputs.ContainsKey("IA1") || Inputs.ContainsKey("UA2");
            isActiveGr3 = Inputs.ContainsKey("IA2harm");
            isActiveGr4 = Inputs.ContainsKey("IA5harm");

            // Заполняем первый список
            foreach (var key in firstListKeys)
            {
                if (Inputs.TryGetValue(key, out string strValue) && double.TryParse(strValue, out double value))
                {
                    AnalRetomGr1.Add(value);
                }
                else if (defaultValues.TryGetValue(key, out double defaultValue))
                {
                    AnalRetomGr1.Add(defaultValue);
                }
                else
                {
                    AnalRetomGr1.Add(0.0);
                }
            }

            // Заполняем второй список
            foreach (var key in secondListKeys)
            {
                if (Inputs.TryGetValue(key, out string strValue) && double.TryParse(strValue, out double value))
                {
                    AnalRetomGr2.Add(value);
                }
                else if (defaultValues.TryGetValue(key, out double defaultValue))
                {
                    AnalRetomGr2.Add(defaultValue);
                }
                else
                {
                    AnalRetomGr2.Add(0.0);
                }
            }

            // Заполняем третий список
            foreach (var key in thirdListKeys)
            {
                if (Inputs.TryGetValue(key, out string strValue) && double.TryParse(strValue, out double value))
                {
                    AnalRetomGr3.Add(value);
                }
                else if (defaultValues.TryGetValue(key, out double defaultValue))
                {
                    AnalRetomGr3.Add(defaultValue);
                }
                else
                {
                    AnalRetomGr3.Add(0.0);
                }
            }

            // Заполняем четвертый список
            foreach (var key in fourthListKeys)
            {
                if (Inputs.TryGetValue(key, out string strValue) && double.TryParse(strValue, out double value))
                {
                    AnalRetomGr4.Add(value);
                }
                else if (defaultValues.TryGetValue(key, out double defaultValue))
                {
                    AnalRetomGr4.Add(defaultValue);
                }
                else
                {
                    AnalRetomGr4.Add(0.0);
                }
            }


            var trueValues = new HashSet<string> { "true", "1", "yes", "да" };

            // Получаем значения в порядке, в котором они были добавлены в NormInputs
            OutputsRetom = NormInputs.Values
                .Select(v => trueValues.Contains(v?.ToLower()?.Trim() ?? ""))
                .ToList();

            // Обрезаем список до 16 элементов, если он больше
            if (OutputsRetom.Count > 16)
            {
                OutputsRetom = OutputsRetom.Take(16).ToList();
            }
            // Дополняем список значениями false, если он меньше 16
            else if (OutputsRetom.Count < 16)
            {
                OutputsRetom.AddRange(Enumerable.Repeat(false, 16 - OutputsRetom.Count));
            }
        }
    }
}