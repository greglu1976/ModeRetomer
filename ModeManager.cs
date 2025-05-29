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

        private void LoadModesOld(string modesFolderPath)
        {
            //string modesFolderPath = @"H:\www_cs\ModeRetomer\Modes\";

            try
            {
                // Инициализация GenParams (один для всех режимов)
                var genParams = new GenParams(modesFolderPath);

                // Создаем коллекцию для хранения режимов
                //List<Mode> modesCollection = new List<Mode>();

                // Получаем все XLSX файлы в папке, соответствующие шаблону
                var xlsxFiles = Directory.GetFiles(modesFolderPath, "*_*.xlsx")
                                        .Where(f => Regex.IsMatch(Path.GetFileName(f), @"^[A-Za-zА-Яа-я]+_\d+\.xlsx$"))
                                        .OrderBy(f => {
                                            var match = Regex.Match(Path.GetFileName(f), @"_(\d+)\.xlsx$");
                                            return match.Success ? int.Parse(match.Groups[1].Value) : 0;
                                        });

                // Обрабатываем каждый файл
                foreach (var filePath in xlsxFiles)
                {
                    try
                    {
                        var mode = new Mode(filePath, genParams.InputsJson, genParams.OutputsJson, Path.GetFileName(filePath));
                        modesCollection.Add(mode);

                        // Для отладки - выводим информацию о загруженном режиме
                        Console.WriteLine($"Загружен режим из файла: {Path.GetFileName(filePath)}");
                        Console.WriteLine($"Активные группы: Gr1={mode.isActiveGr1}, Gr2={mode.isActiveGr2}, Gr3={mode.isActiveGr3}");
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Ошибка при загрузке файла {Path.GetFileName(filePath)}: {ex.Message}",
                                        "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }

                // Проверяем, что коллекция не пуста
                if (!modesCollection.Any())
                {
                    MessageBox.Show("Не найдено ни одного файла режима в указанной папке",
                                    "Информация", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                // Здесь можно работать с коллекцией modesCollection
               // MessageBox.Show($"Успешно загружено {modesCollection.Count} режимов",
                               // "Успех", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при инициализации: {ex.Message}",
                                "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
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
