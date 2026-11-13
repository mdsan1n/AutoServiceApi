using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;




namespace WorkOrdersDesk
{
    public partial class Form1 : Form
    {
        private readonly HttpClient _http = new HttpClient
        {
            BaseAddress = new Uri("http://localhost:5291/"),
            Timeout = TimeSpan.FromSeconds(10),
        };

        private CancellationTokenSource _loadCts;

        public Form1()
        {

            InitializeComponent();
            SetBusy(false);
            SetupHelp();

        }

        private static string FixHelpText(string text)
        {
            byte[] win1251Bytes = Encoding.GetEncoding(1251).GetBytes(text);
            return Encoding.GetEncoding(1252).GetString(win1251Bytes);
        }


        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern Int32 SendMessage(IntPtr hWnd, int msg, int wParam, [MarshalAs(UnmanagedType.LPWStr)] string lParam);
        private const int EM_SETCUEBANNER = 0x1501;
        private async Task<List<WorkOrder>> LoadWorkOrdersAsync(
            CancellationToken cancellationToken)
        {
            await Task.Delay(2000, cancellationToken);
            List<WorkOrder> items = await _http.GetFromJsonAsync<List<WorkOrder>>("api/workorders", cancellationToken);

            return items ?? new List<WorkOrder>();
        }

        private void ShowFieldErrors(ValidationProblem problem)
        {
            if (problem == null || problem.Errors == null)
                return;

            var fields = new Dictionary<string, Control>(StringComparer.OrdinalIgnoreCase)
            {
                ["CarNumber"] = carNumberTextBox,
                ["ClientName"] = clientNameTextBox,
                ["Description"] = descriptionTextBox,
                ["Cost"] = costNumeric
            };

            foreach (var pair in problem.Errors)
            {
                Control control;
                if (fields.TryGetValue(pair.Key, out control))
                {
                    errorProvider.SetError(control, string.Join(" ", pair.Value));
                }
            }
        }

        private static string FormatValidationErrors(ValidationProblem problem)
        {
            if (problem == null || problem.Errors == null || problem.Errors.Count == 0)
                return "Данные не прошли проверку на сервере.";

            IEnumerable<string> messages = problem.Errors.SelectMany(pair => pair.Value);
            return "Исправьте ошибки (поля отмечены красным значком):" +
                Environment.NewLine + string.Join(Environment.NewLine, messages);
        }


        private void SetBusy(bool busy, string status = "")
        {
            loadButton.Enabled = !busy;
            addButton.Enabled = !busy;
            deleteButton.Enabled = !busy; // Добавили блокировку кнопки удаления
            cancelButton.Enabled = busy && (_loadCts != null);
            progressBar.Visible = busy;
            if (status.Length > 0)
                statusLabel.Text = status;
        }

        private void SetupGridHeaders()
        {
            SetHeader(nameof(WorkOrder.Id), "№ Заказа");
            SetHeader(nameof(WorkOrder.CarNumber), "Госномер");
            SetHeader(nameof(WorkOrder.ClientName), "ФИО клиента");
            SetHeader(nameof(WorkOrder.Description), "Описание работ");
            SetHeader(nameof(WorkOrder.Cost), "Стоимость, руб.");
            SetHeader(nameof(WorkOrder.Status), "Статус");
            SetHeader(nameof(WorkOrder.CreatedAt), "Дата создания");
        }

        private void SetHeader(string propertyName, string headerText)
        {
            DataGridViewColumn column = workOrdersGrid.Columns[propertyName];
            if (column != null)
            {
                column.HeaderText = headerText;
            }
        }

        private void ClearInputFields()
        {
            carNumberTextBox.Clear();
            clientNameTextBox.Clear();
            descriptionTextBox.Clear();
            costNumeric.Value = costNumeric.Minimum;
        }

        private static void ShowWarning(string message)
        {
            MessageBox.Show(message, "Автосервис — Заказы",
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }

        private async void LoadButton_Click_1(object sender, EventArgs e)
        {
            using (var cts = new CancellationTokenSource())
            {
                _loadCts = cts;
                SetBusy(true, "Загрузка заказов...");

                try
                {
                    List<WorkOrder> items = await LoadWorkOrdersAsync(cts.Token);
                    workOrdersGrid.DataSource = items;
                    SetupGridHeaders();
                    statusLabel.Text = "Загружено заказов: " + items.Count;
                }
                catch (OperationCanceledException) when (cts.IsCancellationRequested)
                {
                    statusLabel.Text = "Загрузка отменена пользователем";
                }
                catch (TaskCanceledException)
                {
                    statusLabel.Text = "Сервер не ответил вовремя";
                    ShowWarning("Сервер автосервиса не ответил за 10 секунд. Попробуйте позже.");
                }
                catch (HttpRequestException ex)
                {
                    statusLabel.Text = "Нет связи с сервером";
                    ShowWarning("Нет связи с сервером автосервиса. Проверьте, запущен ли API." + Environment.NewLine + ex.Message);
                }
                finally
                {
                    _loadCts = null;
                    SetBusy(false);
                }
            }
        }

        private void cancelButton_Click(object sender, EventArgs e)
        {
            if (_loadCts != null)
            {
                _loadCts.Cancel();
            }
        }

        private void syncLoadButton_Click(object sender, EventArgs e)
        {
            statusLabel.Text = "Синхронная загрузка...";
            List<WorkOrder> items = LoadWorkOrdersAsync(CancellationToken.None).Result;
            workOrdersGrid.DataSource = items;
        }

        private async void AddButton_Click(object sender, EventArgs e)
        {
            errorProvider.Clear(); 

            if (string.IsNullOrWhiteSpace(carNumberTextBox.Text))
            {
                errorProvider.SetError(carNumberTextBox, "Укажите государственный номер автомобиля");
                carNumberTextBox.Focus();
                return;
            }
            if (string.IsNullOrWhiteSpace(clientNameTextBox.Text))
            {
                errorProvider.SetError(clientNameTextBox, "Укажите ФИО клиента");
                clientNameTextBox.Focus();
                return;
            }
            if (string.IsNullOrWhiteSpace(descriptionTextBox.Text))
            {
                errorProvider.SetError(descriptionTextBox, "Укажите описание работ");
                descriptionTextBox.Focus();
                return;
            }

            var request = new
            {
                CarNumber = carNumberTextBox.Text.Trim(),
                ClientName = clientNameTextBox.Text.Trim(),
                Description = descriptionTextBox.Text.Trim(),
                Cost = costNumeric.Value
            };

            bool added = false;
            SetBusy(true, "Сохранение заказа...");

            try
            {
                HttpResponseMessage response =
                    await _http.PostAsJsonAsync("api/workorders", request);

                if (response.IsSuccessStatusCode)
                {
                    WorkOrder created =
                        await response.Content.ReadFromJsonAsync<WorkOrder>();

                    string createdId = created != null ? created.Id.ToString() : "";
                    statusLabel.Text = "Создан заказ № " + createdId;
                    ClearInputFields();
                    added = true;
                }
                else if (response.StatusCode == HttpStatusCode.BadRequest)
                {
                    statusLabel.Text = "Данные не прошли проверку";
                    ValidationProblem problem =
                        await response.Content.ReadFromJsonAsync<ValidationProblem>();

                    ShowFieldErrors(problem);
                    ShowWarning(FormatValidationErrors(problem));
                }
                else if (response.StatusCode == HttpStatusCode.Conflict)
                {
                    statusLabel.Text = "Операция отклонена сервером";
                    ShowWarning(await response.Content.ReadAsStringAsync());
                }
                else
                {
                    int code = (int)response.StatusCode;
                    statusLabel.Text = "Ошибка сервера: " + code;
                    ShowWarning("Сервер не смог сохранить заказ. Попробуйте позже.");
                }
            }
            catch (TaskCanceledException)
            {
                statusLabel.Text = "Сервер не ответил вовремя";
                ShowWarning("Сервер не ответил за 10 секунд.");
            }
            catch (HttpRequestException ex)
            {
                statusLabel.Text = "Нет связи с сервером";
                ShowWarning("Нет связи с сервером автосервиса." +
                    Environment.NewLine + ex.Message);
            }
            finally
            {
                SetBusy(false);
            }

            if (added)
            {
                loadButton.PerformClick();
            }
        }

        private void Form1_Load(object sender, EventArgs e)
        {

        }

        private void costNumeric_ValueChanged(object sender, EventArgs e)
        {

        }

        private async void deleteButton_Click(object sender, EventArgs e)
        {

            if (workOrdersGrid.CurrentRow == null || !(workOrdersGrid.CurrentRow.DataBoundItem is WorkOrder selectedOrder))
            {
                ShowWarning("Выберите заказ в таблице для удаления.");
                return;
            }

            DialogResult confirm = MessageBox.Show(
                "Вы действительно хотите удалить заказ № " + selectedOrder.Id +
                " (автомобиль " + selectedOrder.CarNumber + ")?",
                "Подтверждение удаления",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);

            if (confirm != DialogResult.Yes)
            {
                return;
            }

 
            bool deleted = false;
            SetBusy(true, "Удаление заказа...");

            try
            {
                HttpResponseMessage response =
                    await _http.DeleteAsync("api/workorders/" + selectedOrder.Id);

                if (response.StatusCode == HttpStatusCode.NoContent) 
                {
                    statusLabel.Text = "Заказ № " + selectedOrder.Id + " удалён";
                    deleted = true;
                }
                else if (response.StatusCode == HttpStatusCode.NotFound) 
                {
                    statusLabel.Text = "Заказ не найден";
                    ShowWarning("Заказ № " + selectedOrder.Id + " не найден на сервере. Возможно, он уже был удалён.");
                    deleted = true; 
                }
                else if (response.StatusCode == HttpStatusCode.Conflict) 
                {
                    statusLabel.Text = "Удаление запрещено";
                    ShowWarning(await response.Content.ReadAsStringAsync());
                }
                else
                {
                    int code = (int)response.StatusCode;
                    statusLabel.Text = "Ошибка сервера: " + code;
                    ShowWarning("Сервер не смог удалить заказ.");
                }
            }
            catch (TaskCanceledException)
            {
                statusLabel.Text = "Сервер не ответил вовремя";
                ShowWarning("Сервер не ответил за 10 секунд.");
            }
            catch (HttpRequestException ex)
            {
                statusLabel.Text = "Нет связи с сервером";
                ShowWarning("Нет связи с сервером автосервиса." +
                    Environment.NewLine + ex.Message);
            }
            finally
            {
                SetBusy(false);
            }

            if (deleted)
            {
                loadButton.PerformClick();
            }
        }
        private void SetupHelp()
        {
            toolTip.SetToolTip(loadButton,
                "Получить с сервера актуальный список заказов");
            toolTip.SetToolTip(cancelButton,
                "Прервать загрузку списка, если сервер долго не отвечает");
            toolTip.SetToolTip(syncLoadButton,
                "Учебный эксперимент: окно зависнет. Не нажимайте при работе!");
            toolTip.SetToolTip(deleteButton,
                "Удалить выделенный в таблице заказ");
            toolTip.SetToolTip(addButton,
                "Сохранить новый заказ на сервере");

            toolTip.SetToolTip(carNumberTextBox,
                "Государственный номер автомобиля (от 5 до 15 символов)");
            toolTip.SetToolTip(clientNameTextBox,
                "Фамилия, имя и отчество клиента (от 3 до 100 символов)");
            toolTip.SetToolTip(descriptionTextBox,
                "Подробное описание выполняемых работ (от 10 до 500 символов)");
            toolTip.SetToolTip(costNumeric,
                "Предварительная стоимость ремонта в рублях (от 0 до 1 000 000)");

            SendMessage(carNumberTextBox.Handle, EM_SETCUEBANNER, 0, "Например: В555ВВ777");
            SendMessage(clientNameTextBox.Handle, EM_SETCUEBANNER, 0, "Например: Иванов Иван Иванович");
            SendMessage(descriptionTextBox.Handle, EM_SETCUEBANNER, 0, "Например: Замена масла и фильтров");


            helpProvider.SetHelpString(carNumberTextBox,
                "Государственный регистрационный знак автомобиля. От 5 до 15 символов. Например: А123ВС777.");
            helpProvider.SetHelpString(clientNameTextBox,
                "ФИО владельца автомобиля или заказчика. От 3 до 100 символов. Например: Петров Сергей Иванович.");
            helpProvider.SetHelpString(descriptionTextBox,
                "Подробное описание неисправности и планируемых работ. От 10 до 500 символов. Например: Диагностика ходовой.");
            helpProvider.SetHelpString(costNumeric,
                "Стоимость ремонта в рублях: от 0 до 1 000 000 (например: 4500).");
            helpProvider.SetHelpString(workOrdersGrid,
                "Таблица заказов. Выделите строку для удаления или нажмите «Загрузить» для обновления.");


            // Справка по клавише F1 
            helpProvider.SetHelpString(carNumberTextBox, FixHelpText(
               "Введите государственный регистрационный знак автомобиля. " +
               "Допустимая длина — от 5 до 15 символов. Например: В555ВВ777."));

            helpProvider.SetHelpString(clientNameTextBox, FixHelpText(
                "Укажите фамилию, имя и отчество владельца автомобиля или заказчика. " +
                "Длина строки должна составлять от 3 до 100 символов. Например: Иванов Иван Иванович."));

            helpProvider.SetHelpString(descriptionTextBox, FixHelpText(
                "Опишите выявленную неисправность и перечень планируемых ремонтных работ. " +
                "Описание должно содержать от 10 до 500 символов. Например: Замена масла и фильтров."));

            helpProvider.SetHelpString(costNumeric, FixHelpText(
                "Укажите предварительную стоимость ремонта в рублях. " +
                "Допустимое значение — от 0 до 1 000 000. Например: 4500."));

            helpProvider.SetHelpString(workOrdersGrid, FixHelpText(
                "Таблица содержит список всех заказов автосервиса. " +
                "Чтобы обновить данные с сервера, нажмите кнопку «Загрузить», а для удаления выделите строку и нажмите «Удалить выбранное»."));


        }

        private void оПрограммеToolStripMenuItem_Click(object sender, EventArgs e)
        {
            MessageBox.Show(
        "Автосервис - Заказы" + Environment.NewLine +
        "Версия " + Application.ProductVersion + Environment.NewLine +
        "Автор: Алейникова А. О., группа: О-24-ИСП-1-СПО",
        "О программе",
        MessageBoxButtons.OK,
        MessageBoxIcon.Information);
        }

        private void руководствоПользователяToolStripMenuItem_Click(object sender, EventArgs e)
        {
            string path = Path.Combine(AppContext.BaseDirectory, "Help", "index.html");
            if (!File.Exists(path))
            {
                ShowWarning("Файл справки не найден:" + Environment.NewLine + path);
                return;
            }
            Process.Start(new ProcessStartInfo(path) { UseShellExecute = true });
        }

    }
}



