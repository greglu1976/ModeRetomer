using System;
using System.Windows.Forms;
using System.Drawing;
using System.Threading;
using System.Collections.Generic;
using System.Linq;
using System.Collections.Concurrent;
using System.Diagnostics;

namespace ModeRetomer
{
    public partial class Form1 : Form
    {

        private Thread _workerThread;
        private readonly ConcurrentQueue<Action> _taskQueue = new ConcurrentQueue<Action>();
        private readonly AutoResetEvent _taskSignal = new AutoResetEvent(false);
        private volatile bool _isWorkerRunning = false;



        public RetomDriver m_retomDrv = null;
        //public Thread m_Thread = null;


        private ModeManager modeManager;
        public int currMode = 0; // Номер текущего режим = 1
        public Form1()
        {
            InitializeComponent();
        }
        private void button1_Click(object sender, EventArgs e)
        {
            // Создаем и настраиваем диалог выбора папки
            using (var folderDialog = new FolderBrowserDialog())
            {
                folderDialog.Description = "Выберите папку с режимами";
                folderDialog.SelectedPath = @"C:\Users\g.lubov.UNI-ENG\Desktop\М300-Т\01 ПМИ ГЗ\gzrpn_modes\"; // Начальный путь

                // Показываем диалог и проверяем результат
                if (folderDialog.ShowDialog() == DialogResult.OK)
                {
                    try
                    {
                        // Создаем менеджер режимов с выбранной папкой
                        Console.WriteLine(folderDialog.SelectedPath);
                        modeManager = new ModeManager(folderDialog.SelectedPath);

                        // Выводим информацию о загруженных режимах
                        MessageBox.Show($"Успешно загружено {modeManager.modesCollection.Count} режимов из папки:\n{folderDialog.SelectedPath}",
                                      "Успех", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Ошибка при загрузке режимов:\n{ex.Message}",
                                      "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
                else
                {
                    MessageBox.Show("Выбор папки отменен", "Информация",
                                  MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
        }

        private void BtnProcess_Click(object sender, EventArgs e)
        {
            // Проверяем, что modeManager был инициализирован
            if (modeManager == null || modeManager.modesCollection == null || modeManager.modesCollection.Count == 0)
            {
                MessageBox.Show("Режимы не инициализированы. Сначала загрузите режимы с помощью кнопки 'Инициализировать режимы из папки'.",
                              "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }


            // Если уже есть драйвер — удаляем старый
            if (m_retomDrv != null)
            {
                m_retomDrv.m_retom.BinaryInputsEvent -= m_retom_BinaryInputsEvent; // Отписываемся
                m_retomDrv.RemoveRetom(); // Освобождаем ресурсы
            }


            // Создаем РЕТОМ
            m_retomDrv = new RetomDriver();
            m_retomDrv.CreateRetom();

            m_retomDrv.m_retom.BinaryInputsEvent += m_retom_BinaryInputsEvent;

            DisplayCurrMode();

        }

        private void BtnInitRetom_Click(object sender, EventArgs e)
        {

            m_retomDrv.m_stFunction = "Open";
            m_retomDrv.RunRetom();
            //RunFunction();
            LblError.Text = m_retomDrv.m_nIsOpen.ToString();
        }

        private void BtnCloseRetom_Click(object sender, EventArgs e)
        {
            m_retomDrv.m_stFunction = "Close";
            m_retomDrv.RunRetom();
            //RunFunction();
            LblError.Text = m_retomDrv.m_nIsOpen.ToString();
        }

        public void RunFunction()
        {
            // Добавляем задачу в очередь
            _taskQueue.Enqueue(() => m_retomDrv.RunRetom());

            // Если поток не запущен — создаем его
            if (_workerThread == null || !_workerThread.IsAlive)
            {
                _isWorkerRunning = true;
                _workerThread = new Thread(WorkerLoop)
                {
                    IsBackground = true
                };
                _workerThread.Start();
            }

            // Сигнализируем потоку, что есть задача
            _taskSignal.Set();
        }

        private void WorkerLoop()
        {
            while (_isWorkerRunning)
            {
                // Ждем сигнала о новой задаче
                _taskSignal.WaitOne();

                // Обрабатываем все задачи в очереди
                while (_taskQueue.TryDequeue(out var task))
                {
                    try
                    {
                        task.Invoke(); // Выполняем RunRetom()
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"Ошибка в потоке: {ex.Message}");
                    }
                }
            }
        }


        private void BtnStopMode_Click(object sender, EventArgs e)
        {
            StopThread();
        }
        public void StopThread()
        {
            _taskSignal.Set(); // Разблокируем поток для завершения

            if (_workerThread != null && _workerThread.IsAlive)
            {
                _workerThread.Join(1000); // Даем время на завершение
                if (_workerThread.IsAlive)
                    _workerThread.Abort(); // На крайний случай
            }

            _workerThread = null;
            while (_taskQueue.TryDequeue(out _)) { }
        }

        private void button5_Click(object sender, EventArgs e)
        {
            if (currMode < modeManager.modesCollection.Count - 1)
            {
                button6.Enabled = true;
                currMode++;
                DisplayCurrMode();
            }
            else
            {
                button5.Enabled = false;
            }
        }

        private void button6_Click(object sender, EventArgs e)
        {
            if (currMode > 0)
            {
                button5.Enabled = true;
                currMode--;
                DisplayCurrMode();
            }
            else
            {
                button6.Enabled = false;
            }
        }

        public void DisplayCurrMode()
        {
            var mode = modeManager.modesCollection[currMode];

            //foreach (var kvp in mode.NormOutputs) { Console.WriteLine(kvp); }

            TextBoxIA1.Text = mode.AnalRetomGr1[0].ToString();
            TextBoxIB1.Text = mode.AnalRetomGr1[2].ToString();
            TextBoxIC1.Text = mode.AnalRetomGr1[4].ToString();
            TextBoxdIA1.Text = mode.AnalRetomGr1[1].ToString();
            TextBoxdIB1.Text = mode.AnalRetomGr1[3].ToString();
            TextBoxdIC1.Text = mode.AnalRetomGr1[5].ToString();

            TextBoxUA1.Text = mode.AnalRetomGr1[6].ToString();
            TextBoxUB1.Text = mode.AnalRetomGr1[8].ToString();
            TextBoxUC1.Text = mode.AnalRetomGr1[10].ToString();
            TextBoxdUA1.Text = mode.AnalRetomGr1[7].ToString();
            TextBoxdUB1.Text = mode.AnalRetomGr1[9].ToString();
            TextBoxdUC1.Text = mode.AnalRetomGr1[11].ToString();

            TextBoxNameMode.Text = mode.ModeName;
            TextBoxIA2h.Text = mode.AnalRetomGr3[0].ToString();
            TextBoxIB2h.Text = mode.AnalRetomGr3[1].ToString();
            TextBoxIC2h.Text = mode.AnalRetomGr3[2].ToString();

            TextBoxIA5h.Text = mode.AnalRetomGr4[0].ToString();
            TextBoxIB5h.Text = mode.AnalRetomGr4[1].ToString();
            TextBoxIC5h.Text = mode.AnalRetomGr4[2].ToString();

            TextBoxIA2.Text = mode.AnalRetomGr2[0].ToString();
            TextBoxIB2.Text = mode.AnalRetomGr2[2].ToString();
            TextBoxIC2.Text = mode.AnalRetomGr2[4].ToString();
            TextBoxdIA2.Text = mode.AnalRetomGr2[1].ToString();
            TextBoxdIB2.Text = mode.AnalRetomGr2[3].ToString();
            TextBoxdIC2.Text = mode.AnalRetomGr2[5].ToString();

            TextBoxUA2.Text = mode.AnalRetomGr2[6].ToString();
            TextBoxUB2.Text = mode.AnalRetomGr2[8].ToString();
            TextBoxUC2.Text = mode.AnalRetomGr2[10].ToString();
            TextBoxdUA2.Text = mode.AnalRetomGr2[7].ToString();
            TextBoxdUB2.Text = mode.AnalRetomGr2[9].ToString();
            TextBoxdUC2.Text = mode.AnalRetomGr2[11].ToString();

            Label[] Outlabels = { LblOut1, LblOut2, LblOut3, LblOut4, LblOut5, LblOut6, LblOut7, LblOut8, LblOut9, LblOut10, LblOut11, LblOut12, LblOut13, LblOut14, LblOut15, LblOut16 };
            Label[] OutSlabels = { LblOutS1, LblOutS2, LblOutS3, LblOutS4, LblOutS5, LblOutS6, LblOutS7, LblOutS8, LblOutS9, LblOutS10, LblOutS11, LblOutS12, LblOutS13, LblOutS14, LblOutS15, LblOutS16 };
            short ix = 0;
            foreach (var kvp in mode.NormInputs)
            {
                Outlabels[ix].Text = kvp.Key.ToString();
                int intValue = Convert.ToInt32(kvp.Value);
                bool boolValue = Convert.ToBoolean(intValue);
                OutSlabels[ix].BackColor = boolValue ? Color.Red : Color.Green;
                ix++;
            }

            Label[] Inlabels = { LblIn1, LblIn2, LblIn3, LblIn4, LblIn5, LblIn6, LblIn7, LblIn8, LblIn9, LblIn10, LblIn11, LblIn12, LblIn13, LblIn14, LblIn15, LblIn16 };
            Label[] InSlabels = { LblInS1, LblInS2, LblInS3, LblInS4, LblInS5, LblInS6, LblInS7, LblInS8, LblInS9, LblInS10, LblInS11, LblInS12, LblInS13, LblInS14, LblInS15, LblInS16 };
            short iy = 0;
            foreach (var kvp in mode.NormOutputs)
            {
                Inlabels[iy].Text = kvp.Key.ToString();
                int intValue = Convert.ToInt32(kvp.Value);
                bool boolValue = Convert.ToBoolean(intValue);
                InSlabels[iy].BackColor = boolValue ? Color.Red : Color.Green;
                iy++;
            }
        }

        private void BtnStartMode_Click(object sender, EventArgs e)
        {
            m_retomDrv.AnalGr1 = modeManager.modesCollection[currMode].AnalRetomGr1; // Передаем аналоги 1 группы
            m_retomDrv.AnalGr2 = modeManager.modesCollection[currMode].AnalRetomGr2;
            m_retomDrv.AnalGr3 = modeManager.modesCollection[currMode].AnalRetomGr3;
            m_retomDrv.AnalGr4 = modeManager.modesCollection[currMode].AnalRetomGr4;
            m_retomDrv.Contacts = modeManager.modesCollection[currMode].OutputsRetom;
            m_retomDrv.m_stFunction = "Out61";
            m_retomDrv.RunRetom(); // !!! ТАК РАБОТАЕТ
            //RunFunction(); // ТАК НЕ РАБОТАЕТ
        }

        private void button2_Click(object sender, EventArgs e)
        {
            m_retomDrv.m_stFunction = "Disable";
            m_retomDrv.RunRetom();
            //RunFunction();
        }

        void m_retom_BinaryInputsEvent(short nGroup, int dwBinaryInput)
        {
            //MessageBox.Show("111");
            // Обеспечим выполнение в UI-потоке
            if (panel1.InvokeRequired)
            {
                panel1.Invoke(new Action<short, int>(m_retom_BinaryInputsEvent), nGroup, dwBinaryInput);
                return;
            }
            Label[] Currlabels = { LblInScurr1, LblInScurr2, LblInScurr3, LblInScurr4, LblInScurr5, LblInScurr6, LblInScurr7, LblInScurr8, LblInScurr9, LblInScurr10, LblInScurr11, LblInScurr12, LblInScurr13, LblInScurr14, LblInScurr15, LblInScurr16 };
            // Преобразуем dwBinaryInput в биты (младший бит - вход 1)
            for (int i = 0; i < 8; i++)
            {
                bool isActive = (dwBinaryInput & (1 << i)) != 0;
                Currlabels[i].BackColor = isActive ? Color.Red : Color.Green;
            }

        }

        private void BtnResetConts_Click(object sender, EventArgs e)
        {
            m_retomDrv.Contacts = Enumerable.Repeat(false, 16).ToList();
            m_retomDrv.m_stFunction = "SetOutContact";
            m_retomDrv.RunRetom();
            //RunFunction();
        }

        private void button3_Click(object sender, EventArgs e)
        {
            m_retomDrv.Contacts = modeManager.modesCollection[currMode].OutputsRetom;
            m_retomDrv.m_stFunction = "SetOutContact";
            m_retomDrv.RunRetom();
            //RunFunction();
        }

        private void removeRetomBtn_Click(object sender, EventArgs e)
        {
            m_retomDrv.RemoveRetom();
        }
    }
}
