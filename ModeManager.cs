using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Text.RegularExpressions;
using System.Windows.Forms;

namespace ModeRetomer
{
    class ModeManager
    {
        public List<Mode> modesCollection { get; } = new List<Mode>();
        public GenParams CommonParameters { get; }

        public ModeManager(string modesFolderPath)
        {
            CommonParameters = new GenParams(modesFolderPath);
            LoadModes(modesFolderPath);
        }

        private void LoadModes(string modesFolderPath)
        {
            try
            {
                var genParams = new GenParams(modesFolderPath);

                // Получаем все XLSX файлы в папке с более гибким шаблоном имени
                var xlsxFiles = Directory.GetFiles(modesFolderPath, "*.xlsx")
                                        .Where(f => Regex.IsMatch(Path.GetFileName(f), @"^.+_\d+\.xlsx$")) // Более общее выражение
                                        .OrderBy(f => {
                                            var match = Regex.Match(Path.GetFileName(f), @"_(\d+)\.xlsx$");
                                            return match.Success ? int.Parse(match.Groups[1].Value) : 0;
                                        });

                // Обрабатываем каждый файл
                foreach (var filePath in xlsxFiles)
                {
                    try
                    {
                        var fileName = Path.GetFileName(filePath);
                        var mode = new Mode(filePath, genParams.InputsJson, genParams.OutputsJson, fileName);
                        modesCollection.Add(mode);

                        Console.WriteLine($"Загружен режим из файла: {fileName}");
                        Console.WriteLine($"Активные группы: Gr1={mode.isActiveGr1}, Gr2={mode.isActiveGr2}, Gr3={mode.isActiveGr3}");
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Ошибка при загрузке файла {Path.GetFileName(filePath)}: {ex.Message}",
                                        "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }

                if (!modesCollection.Any())
                {
                    MessageBox.Show("Не найдено ни одного файла режима в указанной папке",
                                    "Информация", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при инициализации: {ex.Message}",
                                "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }


    }
}
