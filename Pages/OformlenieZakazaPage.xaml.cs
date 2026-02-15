using System;
using System.Collections.Generic;
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
    // Страница для оформления заказов.
    // Позволяет создавать, редактировать, удалять и просматривать заказы.
    public partial class OformlenieZakazaPage : Page
    {
        private DataBaseEntities _context; // Контекст БД
        private bool _isNewRecord = true; // Флаг: true - новая запись, false - редактирование
        private OformlenieZakaza _currentOrder; // Текущий выбранный заказ
        private List<OformlenieZakaza> _allOrders; // Все заказы из БД
        private List<Zakazchiki> _allClients; // Все клиенты из БД

        public OformlenieZakazaPage()
        {
            InitializeComponent();
            _context = DataBaseEntities.GetContext();
        }

        
        // Загрузка страницы - вызывается при открытии
        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            LoadData();
            ClearForm();
        }

        // Загрузка всех данных из БД: клиентов и заказов
        private void LoadData()
        {
            try
            {
                _context = DataBaseEntities.GetContext();

                // Загружаем клиентов, сортируем по фамилии и имени
                _allClients = _context.Zakazchiki
                    .OrderBy(c => c.FamiliyaZakazchika)
                    .ThenBy(c => c.ImyaZakazchika)
                    .ToList();

                ClientComboBox.ItemsSource = _allClients;

                // Загружаем заказы, сортируем по дате (сначала новые)
                _allOrders = _context.OformlenieZakaza
                    .OrderByDescending(o => o.DataOformlenieZakaza)
                    .ToList();

                // Вручную связываем заказы с клиентами
                foreach (var order in _allOrders)
                {
                    order.Zakazchiki = _allClients
                        .FirstOrDefault(c => c.Id_Zakazchika == order.Zakazchik_OformlenieZakaza);
                }

                OrdersGrid.ItemsSource = _allOrders; // Привязываем список к DataGrid
            }
            catch (Exception ex)
            {
                ShowError($"Ошибка загрузки данных: {ex.Message}");
            }
        }

        // Очистка формы для создания новой записи
        private void ClearForm()
        {
            _isNewRecord = true;
            _currentOrder = null;

            FormTitle.Text = "НОВЫЙ ЗАКАЗ";
            OrderNumberBox.Text = "(автоматически)";
            OrderDatePicker.SelectedDate = DateTime.Now; // Текущая дата по умолчанию
            OrderNameBox.Text = "";
            ClientComboBox.SelectedIndex = -1;
            DescriptionBox.Text = "";
            DimensionsBox.Text = "";
            DocumentationBox.Text = "";

            // Кнопки редактирования/удаления недоступны, т.к. ничего не выбрано
            EditButton.IsEnabled = false;
            DeleteButton.IsEnabled = false;
            SaveButton.Content = "СОХРАНИТЬ";

            HideError();
        }

        // Показать сообщение об ошибке
        private void ShowError(string message)
        {
            ErrorText.Text = message;
            ErrorBorder.Visibility = Visibility.Visible;
        }

        // Скрыть сообщение об ошибке
        private void HideError()
        {
            ErrorBorder.Visibility = Visibility.Collapsed;
        }

        // Проверка заполнения формы
        private bool ValidateForm()
        {
            HideError();

            if (string.IsNullOrWhiteSpace(OrderNameBox.Text))
            {
                ShowError("Введите наименование заказа");
                OrderNameBox.Focus();
                return false;
            }

            if (ClientComboBox.SelectedItem == null)
            {
                ShowError("Выберите клиента");
                ClientComboBox.Focus();
                return false;
            }

            if (OrderDatePicker.SelectedDate == null)
            {
                ShowError("Укажите дату заказа");
                OrderDatePicker.Focus();
                return false;
            }

            return true;
        }

        // ========== ОБРАБОТЧИКИ СОБЫТИЙ ==========

        // Кнопка "Новый"
        private void AddButton_Click(object sender, RoutedEventArgs e)
        {
            ClearForm();
        }

        // Кнопка "Изменить"
        private void EditButton_Click(object sender, RoutedEventArgs e)
        {
            if (_currentOrder == null) return;

            _isNewRecord = false;
            FormTitle.Text = "РЕДАКТИРОВАНИЕ ЗАКАЗА";
            SaveButton.Content = "ОБНОВИТЬ";

            try
            {
                // Заполняем форму данными выбранного заказа
                OrderNumberBox.Text = _currentOrder.Id_OformlenieZakaza.ToString();
                OrderDatePicker.SelectedDate = _currentOrder.DataOformlenieZakaza;
                OrderNameBox.Text = _currentOrder.NaimenovanieZakaza;

                // Находим и выбираем клиента в комбобоксе
                if (ClientComboBox.ItemsSource != null && _currentOrder.Zakazchik_OformlenieZakaza > 0)
                {
                    var selectedClient = _allClients
                        .FirstOrDefault(c => c.Id_Zakazchika == _currentOrder.Zakazchik_OformlenieZakaza);

                    if (selectedClient != null)
                    {
                        ClientComboBox.SelectedItem = selectedClient;
                    }
                }

                DescriptionBox.Text = _currentOrder.OpisanieIzdeliya ?? "";
                DimensionsBox.Text = _currentOrder.RazmeriGotovogoIzdeliya ?? "";
                DocumentationBox.Text = _currentOrder.Dokumentaciya ?? "";
            }
            catch (Exception ex)
            {
                ShowError($"Ошибка при загрузке данных заказа: {ex.Message}");
            }
        }

        // Кнопка "Удалить" - удаление выбранного заказа
        private void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            if (_currentOrder == null) return;

            var result = MessageBox.Show(
                $"Вы уверены, что хотите удалить заказ №{_currentOrder.Id_OformlenieZakaza}?",
                "Подтверждение удаления",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    _context.OformlenieZakaza.Remove(_currentOrder);
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

        // Поиск по заказам (фильтрация списка)
        private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(SearchBox.Text))
            {
                OrdersGrid.ItemsSource = _allOrders; // Показываем все
            }
            else
            {
                var searchText = SearchBox.Text.ToLower();
                var filtered = _allOrders.Where(o =>
                    (o.NaimenovanieZakaza ?? "").ToLower().Contains(searchText) ||
                    (o.Zakazchiki?.FamiliyaZakazchika ?? "").ToLower().Contains(searchText) ||
                    (o.Zakazchiki?.ImyaZakazchika ?? "").ToLower().Contains(searchText) ||
                    (o.OpisanieIzdeliya ?? "").ToLower().Contains(searchText)
                ).ToList();

                OrdersGrid.ItemsSource = filtered; // Показываем отфильтрованные
            }
        }

        // Выбор заказа в списке - активирует кнопки редактирования/удаления
        private void OrdersGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            _currentOrder = OrdersGrid.SelectedItem as OformlenieZakaza;

            if (_currentOrder != null)
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
                    var newOrder = new OformlenieZakaza
                    {
                        DataOformlenieZakaza = OrderDatePicker.SelectedDate.Value,
                        NaimenovanieZakaza = OrderNameBox.Text.Trim(),
                        Zakazchik_OformlenieZakaza = ((Zakazchiki)ClientComboBox.SelectedItem).Id_Zakazchika,
                        OpisanieIzdeliya = DescriptionBox.Text.Trim(),
                        RazmeriGotovogoIzdeliya = DimensionsBox.Text.Trim(),
                        Dokumentaciya = DocumentationBox.Text.Trim()
                    };

                    _context.OformlenieZakaza.Add(newOrder);
                    _context.SaveChanges();

                    MessageBox.Show("Заказ успешно создан!", "Успех",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    // Обновление существующего заказа
                    _currentOrder.DataOformlenieZakaza = OrderDatePicker.SelectedDate.Value;
                    _currentOrder.NaimenovanieZakaza = OrderNameBox.Text.Trim();
                    _currentOrder.Zakazchik_OformlenieZakaza = ((Zakazchiki)ClientComboBox.SelectedItem).Id_Zakazchika;
                    _currentOrder.OpisanieIzdeliya = DescriptionBox.Text.Trim();
                    _currentOrder.RazmeriGotovogoIzdeliya = DimensionsBox.Text.Trim();
                    _currentOrder.Dokumentaciya = DocumentationBox.Text.Trim();

                    _context.SaveChanges();

                    MessageBox.Show("Заказ успешно обновлен!", "Успех",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }

                // Обновляем данные и очищаем форму
                LoadData();
                ClearForm();
            }
            catch (Exception ex)
            {
                ShowError($"Ошибка при сохранении: {ex.Message}");
            }
        }

        // Кнопка "Отмена" - сброс формы
        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            ClearForm();
        }

        // Кнопка "Назад" - возврат к выбору документов
        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            NavigationService.Navigate(new ViborDocumentaPage());
        }
    }
}
