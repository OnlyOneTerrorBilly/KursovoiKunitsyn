using System;
using Kursovoi.Pages;
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
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Kursovoi.Pages
{
    // Страница "Спецификация на изделие" - определяет состав изделия
    // (какие материалы и фурнитура нужны для его изготовления).
    public partial class SpecifikaciyaNaIzdeliePage : Page, INotifyPropertyChanged
    {
        private DataBaseEntities _context; // Контекст БД
        private bool _isNewRecord = true; // Флаг новой записи
        private SpecifikaciyaNaIzdelie _currentSpecifikaciya; // Текущая выбранная спецификация
        private List<SpecifikaciyaNaIzdelie> _allSpecifikacii; // Все спецификации
        private List<OformlenieZakaza> _allOrders; // Все заказы
        private List<MaterialyIFurnitura> _allMaterials; // Все материалы/фурнитура

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        public SpecifikaciyaNaIzdeliePage()
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

                // Загружаем заказы
                _allOrders = _context.OformlenieZakaza
                    .Include("Zakazchiki")
                    .OrderByDescending(o => o.DataOformlenieZakaza)
                    .ToList();

                OrderComboBox.ItemsSource = _allOrders;

                // Загружаем материалы и фурнитуру
                _allMaterials = _context.MaterialyIFurnitura
                    .Include("TipyMaterialov")
                    .OrderBy(m => m.Name_MaterialyIFurnitura)
                    .ToList();

                MaterialColumn.ItemsSource = _allMaterials;     
                OnPropertyChanged(nameof(MaterialColumn)); // Обновляем привязку

                // Загружаем все спецификации с заказами, клиентами и материалами
                _allSpecifikacii = _context.SpecifikaciyaNaIzdelie
                    .Include("OformlenieZakaza")
                    .Include("Zakazchiki")
                    .Include("Specifikaciya_Materialy.MaterialyIFurnitura")
                    .OrderByDescending(s => s.DataSozdaniyaSpecifikacii)
                    .ToList();

                SpecifikaciiGrid.ItemsSource = _allSpecifikacii;
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
            _currentSpecifikaciya = null;

            FormTitle.Text = "НОВАЯ СПЕЦИФИКАЦИЯ";
            NumberBox.Text = "(автоматически)";
            DatePicker.SelectedDate = DateTime.Now;
            OrderComboBox.SelectedIndex = -1;
            ClientBox.Text = "";
            ProductBox.Text = "";
            DimensionsBox.Text = "";

            // Очищаем таблицу материалов
            MaterialsGrid.ItemsSource = null;
            MaterialsGrid.ItemsSource = new List<Specifikaciya_Materialy>();
            UpdateMaterialsCount();

            // Активируем кнопки
            EditButton.IsEnabled = false;
            DeleteButton.IsEnabled = false;
            SaveButton.Content = "СОХРАНИТЬ";

            HideError();
        }

        // Обновление счетчика материалов
        private void UpdateMaterialsCount()
        {
            int count = 0;
            int materialCount = 0;
            int furnituraCount = 0;

            if (MaterialsGrid.ItemsSource != null)
            {
                foreach (var item in MaterialsGrid.Items)
                {
                    if (item is Specifikaciya_Materialy specMaterial)
                    {
                        count++;
                        // Определяем тип по имени типа материала
                        var material = _allMaterials.FirstOrDefault(m => m.Id_MaterialyIFurnitura == specMaterial.Id_Materiala);
                        if (material?.TipyMaterialov?.NameTipa?.ToLower().Contains("фурнитур") == true)
                        {
                            furnituraCount++;
                        }
                        else
                        {
                            materialCount++;
                        }
                    }
                }
            }

            MaterialsCountText.Text = $"Материалов: {materialCount}   Фурнитуры: {furnituraCount}   Всего: {count}";
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

            if (MaterialsGrid.ItemsSource == null || MaterialsGrid.Items.Count == 0)
            {
                ShowError("Добавьте хотя бы один материал или фурнитуру");
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
            if (_currentSpecifikaciya == null) return;

            _isNewRecord = false;
            FormTitle.Text = "РЕДАКТИРОВАНИЕ СПЕЦИФИКАЦИИ";
            SaveButton.Content = "ОБНОВИТЬ";

            try
            {
                // Заполняем форму данными выбранной спецификации
                NumberBox.Text = _currentSpecifikaciya.Id_SpecifikaciyaNaIzdelie.ToString();
                DatePicker.SelectedDate = _currentSpecifikaciya.DataSozdaniyaSpecifikacii;

                // Находим заказ в комбобоксе
                if (OrderComboBox.ItemsSource != null)
                {
                    var selectedOrder = _allOrders
                        .FirstOrDefault(o => o.Id_OformlenieZakaza == _currentSpecifikaciya.Id_Zakaza);

                    if (selectedOrder != null)
                    {
                        OrderComboBox.SelectedItem = selectedOrder;
                    }
                }

                ClientBox.Text = _currentSpecifikaciya.Zakazchiki?.FullName ?? "";
                ProductBox.Text = _currentSpecifikaciya.OformlenieZakaza?.NaimenovanieZakaza ?? "";
                DimensionsBox.Text = _currentSpecifikaciya.OformlenieZakaza?.RazmeriGotovogoIzdeliya ?? "";

                // Загружаем материалы для этой спецификации
                var materials = _context.Specifikaciya_Materialy
                    .Where(m => m.Id_Specifikaciya == _currentSpecifikaciya.Id_SpecifikaciyaNaIzdelie)
                    .ToList();

                // Связываем материалы
                foreach (var material in materials)
                {
                    material.MaterialyIFurnitura = _allMaterials
                        .FirstOrDefault(m => m.Id_MaterialyIFurnitura == material.Id_Materiala);
                }

                MaterialsGrid.ItemsSource = materials;
                UpdateMaterialsCount();
            }
            catch (Exception ex)
            {
                ShowError($"Ошибка при загрузке данных спецификации: {ex.Message}");
            }
        }

        // Кнопка "Удалить"
        private void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            if (_currentSpecifikaciya == null) return;

            var result = MessageBox.Show(
                $"Вы уверены, что хотите удалить спецификацию №{_currentSpecifikaciya.Id_SpecifikaciyaNaIzdelie}?",
                "Подтверждение удаления",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    // Сначала удаляем связанные материалы
                    var materials = _context.Specifikaciya_Materialy
                        .Where(m => m.Id_Specifikaciya == _currentSpecifikaciya.Id_SpecifikaciyaNaIzdelie)
                        .ToList();

                    _context.Specifikaciya_Materialy.RemoveRange(materials);

                    // Затем удаляем саму спецификацию
                    _context.SpecifikaciyaNaIzdelie.Remove(_currentSpecifikaciya);
                    _context.SaveChanges();

                    LoadData();
                    ClearForm();

                    MessageBox.Show("Спецификация успешно удалена!", "Успех",
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
                SpecifikaciiGrid.ItemsSource = _allSpecifikacii;
            }
            else
            {
                var searchText = SearchBox.Text.ToLower();
                var filtered = _allSpecifikacii.Where(s =>
                    (s.OformlenieZakaza?.NaimenovanieZakaza ?? "").ToLower().Contains(searchText) ||
                    (s.Zakazchiki?.FamiliyaZakazchika ?? "").ToLower().Contains(searchText) ||
                    (s.Zakazchiki?.ImyaZakazchika ?? "").ToLower().Contains(searchText) ||
                    s.Id_SpecifikaciyaNaIzdelie.ToString().Contains(searchText)
                ).ToList();

                SpecifikaciiGrid.ItemsSource = filtered;
            }
        }

        // Выбор в списке
        private void SpecifikaciiGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            _currentSpecifikaciya = SpecifikaciiGrid.SelectedItem as SpecifikaciyaNaIzdelie;

            if (_currentSpecifikaciya != null)
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

        // Выбор заказа
        private void OrderComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (OrderComboBox.SelectedItem is OformlenieZakaza selectedOrder)
            {
                ClientBox.Text = selectedOrder.Zakazchiki?.FullName ?? "";
                ProductBox.Text = selectedOrder.NaimenovanieZakaza;
                DimensionsBox.Text = selectedOrder.RazmeriGotovogoIzdeliya ?? "";
            }
        }

        // Добавление материала
        private void AddMaterialButton_Click(object sender, RoutedEventArgs e)
        {
            if (MaterialsGrid.ItemsSource == null)
                MaterialsGrid.ItemsSource = new List<Specifikaciya_Materialy>();

            var list = MaterialsGrid.ItemsSource as List<Specifikaciya_Materialy>;
            list.Add(new Specifikaciya_Materialy
            {
                Kolichestvo_SpecifikaciyaMaterialy = 1
            });

            MaterialsGrid.ItemsSource = null;
            MaterialsGrid.ItemsSource = list;
            UpdateMaterialsCount();
        }

        // Удаление материала
        private void RemoveMaterialButton_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            if (button?.DataContext is Specifikaciya_Materialy material)
            {
                var list = MaterialsGrid.ItemsSource as List<Specifikaciya_Materialy>;
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
            e.NewItem = new Specifikaciya_Materialy
            {
                Kolichestvo_SpecifikaciyaMaterialy = 1
            };
        }

        // Кнопка "Сохранить"
        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            if (!ValidateForm()) return;

            try
            {
                if (_isNewRecord)
                {
                    // Создание новой спецификации
                    var newSpecifikaciya = new SpecifikaciyaNaIzdelie
                    {
                        DataSozdaniyaSpecifikacii = DatePicker.SelectedDate.Value,
                        Id_Zakaza = ((OformlenieZakaza)OrderComboBox.SelectedItem).Id_OformlenieZakaza,
                        Zakazchik = ((OformlenieZakaza)OrderComboBox.SelectedItem).Zakazchik_OformlenieZakaza
                    };

                    _context.SpecifikaciyaNaIzdelie.Add(newSpecifikaciya);
                    _context.SaveChanges();

                    // Добавляем материалы
                    foreach (var item in MaterialsGrid.Items)
                    {
                        if (item is Specifikaciya_Materialy material)
                        {
                            var newMaterial = new Specifikaciya_Materialy
                            {
                                Id_Specifikaciya = newSpecifikaciya.Id_SpecifikaciyaNaIzdelie,
                                Id_Materiala = material.Id_Materiala,
                                Kolichestvo_SpecifikaciyaMaterialy = material.Kolichestvo_SpecifikaciyaMaterialy
                            };

                            _context.Specifikaciya_Materialy.Add(newMaterial);
                        }
                    }

                    _context.SaveChanges();

                    MessageBox.Show("Спецификация успешно создана!", "Успех",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    // Обновление существующей спецификации
                    _currentSpecifikaciya.DataSozdaniyaSpecifikacii = DatePicker.SelectedDate.Value;
                    _currentSpecifikaciya.Id_Zakaza = ((OformlenieZakaza)OrderComboBox.SelectedItem).Id_OformlenieZakaza;
                    _currentSpecifikaciya.Zakazchik = ((OformlenieZakaza)OrderComboBox.SelectedItem).Zakazchik_OformlenieZakaza;

                    // Удаляем старые материалы
                    var oldMaterials = _context.Specifikaciya_Materialy
                        .Where(m => m.Id_Specifikaciya == _currentSpecifikaciya.Id_SpecifikaciyaNaIzdelie)
                        .ToList();

                    _context.Specifikaciya_Materialy.RemoveRange(oldMaterials);

                    // Добавляем новые материалы
                    foreach (var item in MaterialsGrid.Items)
                    {
                        if (item is Specifikaciya_Materialy material)
                        {
                            var newMaterial = new Specifikaciya_Materialy
                            {
                                Id_Specifikaciya = _currentSpecifikaciya.Id_SpecifikaciyaNaIzdelie,
                                Id_Materiala = material.Id_Materiala,
                                Kolichestvo_SpecifikaciyaMaterialy = material.Kolichestvo_SpecifikaciyaMaterialy
                            };

                            _context.Specifikaciya_Materialy.Add(newMaterial);
                        }
                    }

                    _context.SaveChanges();

                    MessageBox.Show("Спецификация успешно обновлена!", "Успех",
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
