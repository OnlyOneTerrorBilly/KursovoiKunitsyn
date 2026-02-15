using Kursovoi.Classes;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data.Entity;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Kursovoi.Pages
{
    // Страница "Список заказов" - учет заказов клиентов с отслеживанием статусов.
    // Доступна всем пользователям.
    public partial class SpisokZakazovPage : Page
    {
        private DataBaseEntities _context; // Контекст БД
        private bool _isNewRecord = true; // Флаг новой записи
        private SpisokZakazov _currentZakaz; // Текущий выбранный заказ
        private List<SpisokZakazov> _allZakazy; // Все заказы
        private List<StatusyZakazov> _allStatuses; // Все статусы
        private List<Zakazchiki> _allClients; // Все клиенты

        public SpisokZakazovPage()
        {
            InitializeComponent();
            _context = DataBaseEntities.GetContext();
        }

        // Загрузка страницы
        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            LoadData();
            ClearForm();
        }

        // Загрузка всех данных
        private void LoadData()
        {
            try
            {
                _context = DataBaseEntities.GetContext();

                // Загружаем статусы
                _allStatuses = _context.StatusyZakazov
                    .OrderBy(s => s.NameStatusyZakazov)
                    .ToList();

                StatusComboBox.ItemsSource = _allStatuses;

                // Загружаем клиентов
                _allClients = _context.Zakazchiki
                    .OrderBy(c => c.FamiliyaZakazchika)
                    .ThenBy(c => c.ImyaZakazchika)
                    .ToList();

                ClientComboBox.ItemsSource = _allClients;

                // Загружаем заказы с связанными данными
                _allZakazy = _context.SpisokZakazov
                    .Include("StatusyZakazov")
                    .Include("Zakazchiki")
                    .Include("OformlenieZakaza")
                    .OrderByDescending(z => z.DataZakaza)
                    .ToList();

                // Вручную связываем заказы со статусами и клиентами
                foreach (var zakaz in _allZakazy)
                {
                    zakaz.StatusyZakazov = _allStatuses
                        .FirstOrDefault(s => s.Id_StatusyZakazov == zakaz.Status);

                    zakaz.Zakazchiki = _allClients
                        .FirstOrDefault(c => c.Id_Zakazchika == zakaz.Zakazchik_SpisokZakazov);
                }

                ZakazyGrid.ItemsSource = _allZakazy;
            }
            catch (Exception ex)
            {
                ShowError($"Ошибка загрузки данных: {ex.Message}");
            }
        }

        // Очистка формы
        private void ClearForm()
        {
            _isNewRecord = true;
            _currentZakaz = null;

            FormTitle.Text = "НОВЫЙ ЗАКАЗ";
            IdBox.Text = "(автоматически)";
            DatePicker.SelectedDate = DateTime.Now;
            OrderNumberBox.Text = "";
            StatusComboBox.SelectedIndex = -1;
            ClientComboBox.SelectedIndex = -1;
            ProductBox.Text = "";

            // Активируем кнопки
            EditButton.IsEnabled = false;
            DeleteButton.IsEnabled = false;
            SaveButton.Content = "СОХРАНИТЬ";

            HideError();
        }

        // Показать ошибку
        private void ShowError(string message)
        {
            ErrorText.Text = message;
            ErrorBorder.Visibility = Visibility.Visible;
        }

        // Скрыть ошибку
        private void HideError()
        {
            ErrorBorder.Visibility = Visibility.Collapsed;
        }

        // Проверка заполнения формы
        private bool ValidateForm()
        {
            HideError();

            if (string.IsNullOrWhiteSpace(OrderNumberBox.Text))
            {
                ShowError("Введите номер заказа");
                OrderNumberBox.Focus();
                return false;
            }

            if (!int.TryParse(OrderNumberBox.Text, out _))
            {
                ShowError("Номер заказа должен быть числом");
                OrderNumberBox.Focus();
                return false;
            }

            if (StatusComboBox.SelectedItem == null)
            {
                ShowError("Выберите статус");
                StatusComboBox.Focus();
                return false;
            }

            if (ClientComboBox.SelectedItem == null)
            {
                ShowError("Выберите заказчика");
                ClientComboBox.Focus();
                return false;
            }

            if (DatePicker.SelectedDate == null)
            {
                ShowError("Укажите дату заказа");
                DatePicker.Focus();
                return false;
            }

            return true;
        }

        // ========== ОБРАБОТЧИКИ СОБЫТИЙ ==========

        // Кнопка "Назад"
        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            NavigationService.Navigate(new ViborDocumentaPage());
        }

        // Кнопка "Новый"
        private void AddButton_Click(object sender, RoutedEventArgs e)
        {
            ClearForm();
        }

        // Кнопка "Изменить"
        private void EditButton_Click(object sender, RoutedEventArgs e)
        {
            if (_currentZakaz == null) return;

            _isNewRecord = false;
            FormTitle.Text = "РЕДАКТИРОВАНИЕ ЗАКАЗА";
            SaveButton.Content = "ОБНОВИТЬ";

            try
            {
                // Заполняем форму данными выбранного заказа
                IdBox.Text = _currentZakaz.Id_SpiskaZakazov.ToString();
                DatePicker.SelectedDate = _currentZakaz.DataZakaza;
                OrderNumberBox.Text = _currentZakaz.NomerZakaza.ToString();

                // Находим статус в комбобоксе
                if (StatusComboBox.ItemsSource != null && _currentZakaz.Status > 0)
                {
                    var selectedStatus = _allStatuses
                        .FirstOrDefault(s => s.Id_StatusyZakazov == _currentZakaz.Status);

                    if (selectedStatus != null)
                    {
                        StatusComboBox.SelectedItem = selectedStatus;
                    }
                }

                // Находим клиента в комбобоксе
                if (ClientComboBox.ItemsSource != null && _currentZakaz.Zakazchik_SpisokZakazov > 0)
                {
                    var selectedClient = _allClients
                        .FirstOrDefault(c => c.Id_Zakazchika == _currentZakaz.Zakazchik_SpisokZakazov);

                    if (selectedClient != null)
                    {
                        ClientComboBox.SelectedItem = selectedClient;
                    }
                }

                // Заполняем информацию об изделии
                ProductBox.Text = _currentZakaz.OformlenieZakaza?.NaimenovanieZakaza ?? "";
            }
            catch (Exception ex)
            {
                ShowError($"Ошибка при загрузке данных заказа: {ex.Message}");
            }
        }

        // Кнопка "Удалить"
        private void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            if (_currentZakaz == null) return;

            var result = MessageBox.Show(
                $"Вы уверены, что хотите удалить заказ №{_currentZakaz.Id_SpiskaZakazov}?",
                "Подтверждение удаления",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    _context.SpisokZakazov.Remove(_currentZakaz);
                    _context.SaveChanges();

                    LoadData();
                    ClearForm();

                    MessageBox.Show("Заказ успешно удален!", "Успех",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    ShowError($"Ошибка при удалении: {ex.Message}");
                }
            }
        }

        // Поиск
        private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(SearchBox.Text))
            {
                ZakazyGrid.ItemsSource = _allZakazy;
            }
            else
            {
                var searchText = SearchBox.Text.ToLower();
                var filtered = _allZakazy.Where(z =>
                    z.NomerZakaza.ToString().Contains(searchText) ||
                    (z.StatusyZakazov?.NameStatusyZakazov ?? "").ToLower().Contains(searchText) ||
                    (z.Zakazchiki?.FamiliyaZakazchika ?? "").ToLower().Contains(searchText) ||
                    (z.Zakazchiki?.ImyaZakazchika ?? "").ToLower().Contains(searchText) ||
                    (z.OformlenieZakaza?.NaimenovanieZakaza ?? "").ToLower().Contains(searchText)
                ).ToList();

                ZakazyGrid.ItemsSource = filtered;
            }
        }

        // Выбор в списке
        private void ZakazyGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            _currentZakaz = ZakazyGrid.SelectedItem as SpisokZakazov;

            if (_currentZakaz != null)
            {
                EditButton.IsEnabled = true;
                DeleteButton.IsEnabled = true;
            }
            else
            {
                EditButton.IsEnabled = false;
                DeleteButton.IsEnabled = false;
            }
        }

        // Кнопка "Сохранить"
        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            if (!ValidateForm()) return;

            try
            {
                if (_isNewRecord)
                {
                    // Создание нового заказа
                    var newZakaz = new SpisokZakazov
                    {
                        DataZakaza = DatePicker.SelectedDate.Value,
                        NomerZakaza = int.Parse(OrderNumberBox.Text),
                        Status = ((StatusyZakazov)StatusComboBox.SelectedItem).Id_StatusyZakazov,
                        Zakazchik_SpisokZakazov = ((Zakazchiki)ClientComboBox.SelectedItem).Id_Zakazchika
                    };

                    _context.SpisokZakazov.Add(newZakaz);
                    _context.SaveChanges();

                    MessageBox.Show("Заказ успешно создан!", "Успех",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    // Обновление существующего заказа
                    _currentZakaz.DataZakaza = DatePicker.SelectedDate.Value;
                    _currentZakaz.NomerZakaza = int.Parse(OrderNumberBox.Text);
                    _currentZakaz.Status = ((StatusyZakazov)StatusComboBox.SelectedItem).Id_StatusyZakazov;
                    _currentZakaz.Zakazchik_SpisokZakazov = ((Zakazchiki)ClientComboBox.SelectedItem).Id_Zakazchika;

                    _context.SaveChanges();

                    MessageBox.Show("Заказ успешно обновлен!", "Успех",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }

                // Обновляем данные
                LoadData();
                ClearForm();
            }
            catch (Exception ex)
            {
                ShowError($"Ошибка при сохранении: {ex.Message}");
            }
        }

        // Кнопка "Отмена"
        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            ClearForm();
        }
    }
}
