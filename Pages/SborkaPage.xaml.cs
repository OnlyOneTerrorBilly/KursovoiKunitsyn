using System;
using System.Collections.Generic;
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
    // Страница "Сборка" - учет процесса сборки изделий из материалов.
    // Позволяет создавать сборки на основе заказов, указывать используемые материалы
    // и проверять их наличие на складе.
    public partial class SborkaPage : Page
    {
        private DataBaseEntities _context; // Контекст БД
        private bool _isNewRecord = true; // Флаг новой записи
        private Sborka _currentSborka; // Текущая выбранная сборка
        private List<Sborka> _allSborki; // Все сборки
        private List<OformlenieZakaza> _allOrders; // Все заказы
        private List<MaterialyIFurnitura> _allMaterials; // Все материалы
        private List<PostuplenieTovara_Soderzhimoe> _availableMaterials; // Доступные материалы

        public SborkaPage()
        {
            InitializeComponent();
            _context = DataBaseEntities.GetContext();
        }

        // Загрузка страницы
        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            LoadData();
            ClearForm();
            LoadAvailableMaterials();
        }

        // Загрузка всех данных
        private void LoadData()
        {
            try
            {
                _context = DataBaseEntities.GetContext();

                // УБИРАЕМ ФИЛЬТР! Загружаем ВСЕ заказы
                _allOrders = _context.OformlenieZakaza
                    .Include("Zakazchiki")
                    .OrderByDescending(o => o.DataOformlenieZakaza)
                    .ToList();

                OrderComboBox.ItemsSource = _allOrders;

                // Загружаем материалы для таблицы
                _allMaterials = _context.MaterialyIFurnitura
                    .OrderBy(m => m.Name_MaterialyIFurnitura)
                    .ToList();

                MaterialColumn.ItemsSource = _allMaterials;

                // Загружаем сборки с заказами и материалами
                _allSborki = _context.Sborka
                    .Include("OformlenieZakaza")
                    .Include("OformlenieZakaza.Zakazchiki")
                    .Include("Sborka_Materialy")
                    .Include("Sborka_Materialy.MaterialyIFurnitura")
                    .OrderByDescending(s => s.DataSozdaniyaSborki)
                    .ToList();

                SborkaGrid.ItemsSource = _allSborki;

                // Настраиваем фильтр
                FilterComboBox.Items.Clear();
                FilterComboBox.Items.Add("Все сборки");
                FilterComboBox.Items.Add("Сегодня");
                FilterComboBox.Items.Add("За неделю");
                FilterComboBox.Items.Add("За месяц");
                FilterComboBox.SelectedIndex = 0;
            }
            catch (Exception ex)
            {
                ShowError($"Ошибка загрузки данных: {ex.Message}");
            }
        }

        // Загрузка доступных материалов со склада
        private void LoadAvailableMaterials()
        {
            try
            {
                // Группируем поступления по материалам, суммируем количество
                var postupleniya = _context.PostuplenieTovara_Soderzhimoe
                    .GroupBy(p => p.Id_Materiala)
                    .Select(g => new
                    {
                        Id_Materiala = g.Key,
                        Kolichestvo = g.Sum(p => p.Kolichestvo_Postuplenie)
                    })
                    .ToList();

                // Группируем использованные в сборках материалы, суммируем
                var sborki = _context.Sborka_Materialy
                    .GroupBy(s => s.Id_Material)
                    .Select(g => new
                    {
                        Id_Materiala = g.Key,
                        Kolichestvo = g.Sum(s => s.Kolichestvo_SborkaMaterialy)
                    })
                    .ToList();

                // Рассчитываем остатки: поступления - использовано
                _availableMaterials = new List<PostuplenieTovara_Soderzhimoe>();

                foreach (var material in _allMaterials)
                {
                    var postuplenie = postupleniya.FirstOrDefault(p => p.Id_Materiala == material.Id_MaterialyIFurnitura);
                    var sborka = sborki.FirstOrDefault(s => s.Id_Materiala == material.Id_MaterialyIFurnitura);

                    decimal postuplenieQty = postuplenie?.Kolichestvo ?? 0;
                    decimal sborkaQty = sborka?.Kolichestvo ?? 0;
                    decimal ostatok = postuplenieQty - sborkaQty;

                    if (ostatok > 0)
                    {
                        _availableMaterials.Add(new PostuplenieTovara_Soderzhimoe
                        {
                            Id_Materiala = material.Id_MaterialyIFurnitura,
                            MaterialyIFurnitura = material,
                            Kolichestvo_Postuplenie = (int)ostatok
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                ShowError($"Ошибка загрузки остатков материалов: {ex.Message}");
            }
        }

        // Очистка формы
        private void ClearForm()
        {
            _isNewRecord = true;
            _currentSborka = null;

            FormTitle.Text = "НОВАЯ СБОРКА";
            NumberBox.Text = "(автоматически)";
            DatePicker.SelectedDate = DateTime.Now;
            OrderComboBox.SelectedIndex = -1;
            ProductBox.Text = "";
            QuantityBox.Text = "1";

            // Очищаем таблицу материалов
            MaterialsGrid.ItemsSource = null;
            MaterialsGrid.ItemsSource = new List<Sborka_Materialy>();
            UpdateMaterialsCount();

            // Скрываем блоки ошибок
            ErrorBorder.Visibility = Visibility.Collapsed;
            CheckBorder.Visibility = Visibility.Collapsed;

            // Активируем кнопки
            EditButton.IsEnabled = false;
            DeleteButton.IsEnabled = false;
            SaveButton.Content = "СОХРАНИТЬ";
        }

        // Обновление счетчика материалов
        private void UpdateMaterialsCount()
        {
            int count = 0;

            if (MaterialsGrid.ItemsSource != null)
            {
                count = MaterialsGrid.Items.OfType<Sborka_Materialy>().Count();
            }

            MaterialsCountText.Text = $"Материалов: {count}";
        }

        // Проверка наличия материалов
        private string CheckMaterialsAvailability()
        {
            var errors = new List<string>();

            if (MaterialsGrid.ItemsSource == null)
            {
                return "Добавьте материалы для сборки";
            }

            foreach (var item in MaterialsGrid.Items)
            {
                if (item is Sborka_Materialy material)
                {
                    if (material.Id_Material == 0)
                    {
                        errors.Add("Выберите материал для всех строк");
                        break;
                    }

                    if (material.Kolichestvo_SborkaMaterialy <= 0)
                    {
                        errors.Add($"Количество материала '{material.MaterialyIFurnitura?.Name_MaterialyIFurnitura}' должно быть больше 0");
                        continue;
                    }

                    // Проверяем наличие на складе
                    var available = _availableMaterials
                        .FirstOrDefault(m => m.Id_Materiala == material.Id_Material);

                    if (available == null)
                    {
                        errors.Add($"Материал '{material.MaterialyIFurnitura?.Name_MaterialyIFurnitura}' отсутствует на складе");
                    }
                    else if (available.Kolichestvo_Postuplenie < material.Kolichestvo_SborkaMaterialy)
                    {
                        errors.Add($"Недостаточно материала '{material.MaterialyIFurnitura?.Name_MaterialyIFurnitura}'. Доступно: {available.Kolichestvo_Postuplenie}, требуется: {material.Kolichestvo_SborkaMaterialy}");
                    }
                }
            }

            return errors.Count > 0 ? string.Join("\n", errors) : null;
        }

        // Показать ошибку
        private void ShowError(string message)
        {
            ErrorText.Text = message;
            ErrorBorder.Visibility = Visibility.Visible;
        }

        // Показать результат проверки
        private void ShowCheckResult(string message, bool isError = true)
        {
            CheckText.Text = message;
            CheckBorder.Background = isError ?
                new SolidColorBrush(Color.FromRgb(253, 232, 232)) :
                new SolidColorBrush(Color.FromRgb(232, 253, 232));
            CheckBorder.BorderBrush = isError ?
                new SolidColorBrush(Color.FromRgb(231, 76, 60)) :
                new SolidColorBrush(Color.FromRgb(46, 204, 113));
            CheckText.Foreground = isError ?
                new SolidColorBrush(Color.FromRgb(192, 57, 43)) :
                new SolidColorBrush(Color.FromRgb(39, 174, 96));
            CheckBorder.Visibility = Visibility.Visible;
        }

        // Скрыть ошибку
        private void HideError()
        {
            ErrorBorder.Visibility = Visibility.Collapsed;
            CheckBorder.Visibility = Visibility.Collapsed;
        }

        // Проверка заполнения формы
        private bool ValidateForm()
        {
            HideError();

            if (OrderComboBox.SelectedItem == null)
            {
                ShowError("Выберите заказ");
                OrderComboBox.Focus();
                return false;
            }

            if (string.IsNullOrWhiteSpace(ProductBox.Text))
            {
                ShowError("Введите наименование изделия");
                ProductBox.Focus();
                return false;
            }

            if (!int.TryParse(QuantityBox.Text, out int quantity) || quantity <= 0)
            {
                ShowError("Введите корректное количество");
                QuantityBox.Focus();
                return false;
            }

            if (MaterialsGrid.ItemsSource == null || MaterialsGrid.Items.Count == 0)
            {
                ShowError("Добавьте хотя бы один материал");
                return false;
            }

            return true;
        }

        // ========== ОБРАБОТЧИКИ СОБЫТИЙ ==========

        // Кнопка "Новая сборка"
        private void AddButton_Click(object sender, RoutedEventArgs e)
        {
            ClearForm();
        }

        // Кнопка "Изменить" - загрузка выбранной сборки
        private void EditButton_Click(object sender, RoutedEventArgs e)
        {
            if (_currentSborka == null) return;

            _isNewRecord = false;
            FormTitle.Text = "РЕДАКТИРОВАНИЕ СБОРКИ";
            SaveButton.Content = "ОБНОВИТЬ";

            try
            {
                // Заполняем форму данными выбранной сборки
                NumberBox.Text = _currentSborka.Id_Sborka.ToString();
                DatePicker.SelectedDate = _currentSborka.DataSozdaniyaSborki;

                // Находим заказ в комбобоксе
                if (OrderComboBox.ItemsSource != null)
                {
                    var selectedOrder = _allOrders
                        .FirstOrDefault(o => o.Id_OformlenieZakaza == _currentSborka.Id_Zakaza);

                    if (selectedOrder != null)
                    {
                        OrderComboBox.SelectedItem = selectedOrder;
                    }
                }

                // Загружаем материалы для этой сборки
                var materials = _context.Sborka_Materialy
                    .Where(m => m.Id_Sborka == _currentSborka.Id_Sborka)
                    .ToList();

                // Связываем материалы
                foreach (var material in materials)
                {
                    material.MaterialyIFurnitura = _allMaterials
                        .FirstOrDefault(m => m.Id_MaterialyIFurnitura == material.Id_Material);
                }

                MaterialsGrid.ItemsSource = materials;
                UpdateMaterialsCount();

                // Заполняем информацию об изделии (можно взять из заказа)
                var order = _currentSborka.OformlenieZakaza;
                if (order != null)
                {
                    ProductBox.Text = order.NaimenovanieZakaza;
                    QuantityBox.Text = "1"; // По умолчанию 1 изделие
                }
            }
            catch (Exception ex)
            {
                ShowError($"Ошибка при загрузке данных сборки: {ex.Message}");
            }
        }

        // Кнопка "Удалить"
        private void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            if (_currentSborka == null) return;

            var result = MessageBox.Show(
                $"Вы уверены, что хотите удалить сборку №{_currentSborka.Id_Sborka}?\nВсе связанные материалы также будут удалены.",
                "Подтверждение удаления",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    // Сначала удаляем связанные материалы
                    var materials = _context.Sborka_Materialy
                        .Where(m => m.Id_Sborka == _currentSborka.Id_Sborka)
                        .ToList();

                    _context.Sborka_Materialy.RemoveRange(materials);

                    // Затем удаляем саму сборку
                    _context.Sborka.Remove(_currentSborka);
                    _context.SaveChanges();

                    LoadData();
                    ClearForm();

                    MessageBox.Show("Сборка успешно удалена!", "Успех",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    ShowError($"Ошибка при удалении: {ex.Message}");
                }
            }
        }

        // Кнопка "Обновить"
        private void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            LoadData();
            LoadAvailableMaterials();
            ClearForm();
            MessageBox.Show("Данные обновлены", "Информация",
                MessageBoxButton.OK, MessageBoxImage.Information);
        }

        // Фильтр по дате
        private void FilterComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_allSborki == null || FilterComboBox.SelectedIndex < 0) return;

            DateTime filterDate = DateTime.MinValue;
            switch (FilterComboBox.SelectedIndex)
            {
                case 1: // Сегодня
                    filterDate = DateTime.Today;
                    break;
                case 2: // За неделю
                    filterDate = DateTime.Today.AddDays(-7);
                    break;
                case 3: // За месяц
                    filterDate = DateTime.Today.AddMonths(-1);
                    break;
            }

            if (FilterComboBox.SelectedIndex == 0)
            {
                SborkaGrid.ItemsSource = _allSborki;
            }
            else
            {
                var filtered = _allSborki
                    .Where(s => s.DataSozdaniyaSborki >= filterDate)
                    .ToList();
                SborkaGrid.ItemsSource = filtered;
            }
        }

        // Поиск по тексту 
        private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(SearchBox.Text))
            {
                // Возвращаем текущий фильтр
                FilterComboBox_SelectionChanged(null, null);
            }
            else
            {
                var searchText = SearchBox.Text.ToLower();
                var currentItems = SborkaGrid.ItemsSource as List<Sborka> ?? _allSborki;

                var filtered = currentItems.Where(s =>
                    (s.OformlenieZakaza?.NaimenovanieZakaza ?? "").ToLower().Contains(searchText) ||
                    (s.OformlenieZakaza?.Zakazchiki?.FamiliyaZakazchika ?? "").ToLower().Contains(searchText) ||
                    (s.OformlenieZakaza?.Zakazchiki?.ImyaZakazchika ?? "").ToLower().Contains(searchText)
                ).ToList();

                SborkaGrid.ItemsSource = filtered;
            }
        }

        // Выбор в списке
        private void SborkaGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            _currentSborka = SborkaGrid.SelectedItem as Sborka;

            if (_currentSborka != null)
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

        // Добавление нового материала
        private void AddMaterialButton_Click(object sender, RoutedEventArgs e)
        {
            if (MaterialsGrid.ItemsSource == null)
                MaterialsGrid.ItemsSource = new List<Sborka_Materialy>();

            var list = MaterialsGrid.ItemsSource as List<Sborka_Materialy>;
            list.Add(new Sborka_Materialy
            {
                Kolichestvo_SborkaMaterialy = 1
            });

            MaterialsGrid.ItemsSource = null;
            MaterialsGrid.ItemsSource = list;
            UpdateMaterialsCount();
        }

        // Удаление материала
        private void RemoveMaterialButton_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            if (button?.DataContext is Sborka_Materialy material)
            {
                var list = MaterialsGrid.ItemsSource as List<Sborka_Materialy>;
                if (list != null)
                {
                    list.Remove(material);
                    MaterialsGrid.ItemsSource = null;
                    MaterialsGrid.ItemsSource = list;
                    UpdateMaterialsCount();
                }
            }
        }

        // Добавление новой строки в таблицу (DataGrid)
        private void MaterialsGrid_AddingNewItem(object sender, AddingNewItemEventArgs e)
        {
            e.NewItem = new Sborka_Materialy
            {
                Kolichestvo_SborkaMaterialy = 1
            };
        }

        // Кнопка "Проверить" - проверка наличия материалов
        private void CheckButton_Click(object sender, RoutedEventArgs e)
        {
            var result = CheckMaterialsAvailability();
            if (string.IsNullOrEmpty(result))
            {
                ShowCheckResult("✓ Все материалы доступны в достаточном количестве", false);
            }
            else
            {
                ShowCheckResult(result, true);
            }
        }

        // Кнопка "Сохранить"
        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            if (!ValidateForm()) return;

            // Проверяем наличие материалов
            var checkResult = CheckMaterialsAvailability();
            if (!string.IsNullOrEmpty(checkResult))
            {
                var result = MessageBox.Show(
                    "Не все материалы доступны в достаточном количестве.\n" +
                    "Продолжить сохранение?",
                    "Предупреждение",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning);

                if (result != MessageBoxResult.Yes)
                {
                    return;
                }
            }

            try
            {
                if (_isNewRecord)
                {
                    // Создание новой сборки
                    var newSborka = new Sborka
                    {
                        DataSozdaniyaSborki = DatePicker.SelectedDate.Value,
                        Id_Zakaza = ((OformlenieZakaza)OrderComboBox.SelectedItem).Id_OformlenieZakaza
                    };

                    _context.Sborka.Add(newSborka);
                    _context.SaveChanges(); // Сохраняем, чтобы получить ID

                    // Добавляем материалы
                    foreach (var item in MaterialsGrid.Items)
                    {
                        if (item is Sborka_Materialy material)
                        {
                            var newMaterial = new Sborka_Materialy
                            {
                                Id_Sborka = newSborka.Id_Sborka,
                                Id_Material = material.Id_Material,
                                Kolichestvo_SborkaMaterialy = material.Kolichestvo_SborkaMaterialy
                            };

                            _context.Sborka_Materialy.Add(newMaterial);
                        }
                    }

                    _context.SaveChanges();

                    MessageBox.Show("Сборка успешно создана!", "Успех",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    // Обновление существующей сборки
                    _currentSborka.DataSozdaniyaSborki = DatePicker.SelectedDate.Value;
                    _currentSborka.Id_Zakaza = ((OformlenieZakaza)OrderComboBox.SelectedItem).Id_OformlenieZakaza;

                    // Удаляем старые материалы
                    var oldMaterials = _context.Sborka_Materialy
                        .Where(m => m.Id_Sborka == _currentSborka.Id_Sborka)
                        .ToList();

                    _context.Sborka_Materialy.RemoveRange(oldMaterials);

                    // Добавляем новые материалы
                    foreach (var item in MaterialsGrid.Items)
                    {
                        if (item is Sborka_Materialy material)
                        {
                            var newMaterial = new Sborka_Materialy
                            {
                                Id_Sborka = _currentSborka.Id_Sborka,
                                Id_Material = material.Id_Material,
                                Kolichestvo_SborkaMaterialy = material.Kolichestvo_SborkaMaterialy
                            };

                            _context.Sborka_Materialy.Add(newMaterial);
                        }
                    }

                    _context.SaveChanges();

                    MessageBox.Show("Сборка успешно обновлена!", "Успех",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }

                // Обновляем данные
                LoadData();
                LoadAvailableMaterials();
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
        // Кнопка "Назад"
        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            NavigationService.Navigate(new ViborDocumentaPage());
        }
        // Выбор заказа
        private void OrderComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (OrderComboBox.SelectedItem is OformlenieZakaza selectedOrder)
            {
                ProductBox.Text = selectedOrder.NaimenovanieZakaza;
            }
        }
    }
}
