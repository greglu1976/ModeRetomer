using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.IO;
using Newtonsoft.Json;

namespace ModeRetomer
{
    class GenParams
    {

        public Dictionary<string, string> InputsJson { get; private set; }
        public Dictionary<string, string> OutputsJson { get; private set; }
        public Dictionary<string, string> FullInputsJson { get; private set; }
        public Dictionary<string, string> AnalsJson { get; private set; }


        public GenParams(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath))
                throw new ArgumentException("File path cannot be null or empty.", nameof(filePath));
            // Инициализация словарей
            InputsJson = new Dictionary<string, string>();
            OutputsJson = new Dictionary<string, string>();
            AnalsJson = new Dictionary<string, string>();

            string inputsFilePath = filePath + @"\inputs.json";
            string inputsJson = File.ReadAllText(inputsFilePath);
            FullInputsJson = JsonConvert.DeserializeObject<Dictionary<string, string>>(inputsJson);

            string outputsFilePath = filePath + @"\outputs.json";
            string outputsJson = File.ReadAllText(outputsFilePath);
            OutputsJson = JsonConvert.DeserializeObject<Dictionary<string, string>>(outputsJson);

            Divider(); // разделяем дискретны и аналоги по патерну
        }

        private void Divider() 
        {
            // Паттерны для исключения аналоговых сигналов
            var analogPatterns = new HashSet<string> { "IA", "IB", "IC", "UA1", "UB1", "UC1",
                                              "dIA", "dIB", "dIC", "dUA1", "dUB1", "dUC1",
                                              "IA1", "IB1", "IC1", "UA2", "UB2", "UC2",
                                              "dIA1", "dIB1", "dIC1", "dUA2", "dUB2", "dUC2",
                                              "IA2harm", "IB2harm", "IC2harm", "IA5harm", "IB5harm", "IC5harm", 
                                              "diffA", "restA", "diffB", "restB", "diffC", "restC" };
            var analogItems = FullInputsJson
                .Where(item => analogPatterns.Any(pattern => item.Key.Contains(pattern)))
                .ToDictionary(item => item.Key, item => item.Value);

            var digitalItems = FullInputsJson
                .Where(item => !analogPatterns.Any(pattern => item.Key.Contains(pattern)))
                .ToDictionary(item => item.Key, item => item.Value);

            // Process OutputsJson to remove analog patterns
            var digitalOutputs = OutputsJson
                .Where(item => !analogPatterns.Any(pattern => item.Key.Contains(pattern)))
                .ToDictionary(item => item.Key, item => item.Value);

            AnalsJson = analogItems;
            InputsJson = digitalItems;
            OutputsJson = digitalOutputs; // Update OutputsJson to exclude analog patterns

        }
    }
}
