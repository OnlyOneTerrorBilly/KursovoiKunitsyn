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
    // Страница учета поступления товаров (материалов и фурнитуры) от поставщиков.
    // Реализует документ "Поступление товара" с шапкой и табличной частью.
    public partial class PostuplenieTovaraPage : Page
    {
        private DataBaseEntities _context;                      // Контекст БД
        private bool _isNewRecord = true;                       // Флаг: новая запись
        private PostuplenieTovara _currentPostuplenie;          // Текущее выбранное поступление
        private List<PostuplenieTovara> _allPostupleniya;       // Все поступления
        private List<CompanyPostavshiki> _allSuppliers;         // Все поставщики
        private List<MaterialyIFurnitura> _allMaterials;        // Все материалы/фурнитура

        public PostuplenieTovaraPage()
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

        // Загрузка всех справочников и списка поступлений из БД
        private void LoadData()
        {
            try
            {
                _context = DataBaseEntities.GetContext();

                // Загружаем поставщиков
                _allSuppliers = _context.CompanyPostavshiki
                    .OrderBy(s => s.NamePostavshiki)
                    .ToList();

                SupplierComboBox.ItemsSource = _allSuppliers;

                // Загружаем материалы и фурнитуру
                _allMaterials = _context.MaterialyIFurnitura
                    .OrderBy(m => m.Name_MaterialyIFurnitura)
                    .ToList();

                // Настраиваем колонку товаров
                ItemColumn.ItemsSource = _allMaterials;

                // Загружаем поступления с поставщиками и товарами
                _allPostupleniya = _context.PostuplenieTovara
                    .Include("CompanyPostavshiki")
                    .Include("PostuplenieTovara_Soderzhimoe")
                    .Include("PostuplenieTovara_Soderzhimoe.MaterialyIFurnitura")
                    .OrderByDescending(p => p.DataPostupleniya)
                    .ToList();

                // Вручную связываем поступления с поставщиками
                foreach (var post in _allPostupleniya)
                {
                    post.CompanyPostavshiki = _allSuppliers
                        .FirstOrDefault(s => s.Id_Postavshiki == post.CompaniyaPostavshika);
                }

                PostuplenieGrid.ItemsSource = _allPostupleniya;
            }
            catch (Exception ex)
            {
                ShowError($"Ошибка загрузки данных: {ex.Message}");
            }
        }

        // Очистка формы для нового поступления
        private void ClearForm()
        {
            _isNewRecord = true;
            _currentPostuplenie = null;

            FormTitle.Text = "НОВОЕ ПОСТУПЛЕНИЕ";
            NumberBox.Text = "(автоматически)";
            DatePicker.SelectedDate = DateTime.Now;
            SupplierComboBox.SelectedIndex = -1;
            ContractBox.Text = "";

            // Очищаем таблицу товаров
            ItemsGrid.ItemsSource = null;
            ItemsGrid.ItemsSource = new List<PostuplenieTovara_Soderzhimoe>();
            UpdateTotal();

            // Активируем кнопки
            EditButton.IsEnabled = false;
            DeleteButton.IsEnabled = false;
            SaveButton.Content = "СОХРАНИТЬ";

            HideError();
        }

        // Пересчет итоговой суммы по всем товарам в таблице
        private void UpdateTotal()
        {
            try
            {
                decimal total = 0;

                if (ItemsGrid.ItemsSource != null)
                {
                    foreach (var item in ItemsGrid.ItemsSource)
                    {
                        if (item is PostuplenieTovara_Soderzhimoe lineItem)
                        {
                            lineItem.Summa = lineItem.Kolichestvo_Postuplenie * lineItem.Cena;
                            total += lineItem.Summa;
                        }
                    }
                }

                TotalText.Text = total.ToString("N2");
            }
            catch
            {
                TotalText.Text = "0.00";
            }
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

            if (SupplierComboBox.SelectedItem == null)
            {
                ShowError("Выберите поставщика");
                SupplierComboBox.Focus();
                return false;
            }

            if (string.IsNullOrWhiteSpace(ContractBox.Text))
            {
                ShowError("Введите номер договора");
                ContractBox.Focus();
                return false;
            }

            if (DatePicker.SelectedDate == null)
            {
                ShowError("Укажите дату поступления");
                DatePicker.Focus();
                return false;
            }

            // Проверка табличной части
            if (ItemsGrid.ItemsSource == null || ItemsGrid.Items.Count == 0)
            {
                ShowError("Добавьте хотя бы один товар");
                return false;
            }

            foreach (var item in ItemsGrid.Items)
            {
                if (item is PostuplenieTovara_Soderzhimoe lineItem)
                {
                    if (lineItem.Id_Materiala == 0)
                    {
                        ShowError("Выберите товар для всех строк");
                        return false;
                    }

                    if (lineItem.Kolichestvo_Postuplenie <= 0)
                    {
                        ShowError("Количество должно быть больше 0");
                        return false;
                    }

                    if (lineItem.Cena <= 0)
                    {
                        ShowError("Цена должна быть больше 0");
                        return false;
                    }
                }
            }

            return true;
        }

        // ========== ОБРАБОТЧИКИ СОБЫТИЙ ==========

        // Кнопка "Новый"
        private void AddButton_Click(object sender, RoutedEventArgs e)
        {
            ClearForm();
        }

        // Кнопка "Изменить" - загрузка данных выбранного поступления в форму
        private void EditButton_Click(object sender, RoutedEventArgs e)
        {
            if (_currentPostuplenie == null) return;

            _isNewRecord = false;
            FormTitle.Text = "РЕДАКТИРОВАНИЕ ПОСТУПЛЕНИЯ";
            SaveButton.Content = "ОБНОВИТЬ";

            try
            {
                // Заполняем форму данными выбранного поступления
                NumberBox.Text = _currentPostuplenie.Id_PostuplenieTovara.ToString();
                DatePicker.SelectedDate = _currentPostuplenie.DataPostupleniya;
                ContractBox.Text = _currentPostuplenie.Dogovor ?? "";

                // Находим поставщика в комбобоксе
                if (SupplierComboBox.ItemsSource != null && _currentPostuplenie.CompaniyaPostavshika > 0)
                {
                    var selectedSupplier = _allSuppliers
                        .FirstOrDefault(s => s.Id_Postavshiki == _currentPostuplenie.CompaniyaPostavshika);

                    if (selectedSupplier != null)
                    {
                        SupplierComboBox.SelectedItem = selectedSupplier;
                    }
                }

                // Загружаем товары для этого поступления
                var items = _context.PostuplenieTovara_Soderzhimoe
                    .Where(i => i.Id_Postupleniya == _currentPostuplenie.Id_PostuplenieTovara)
                    .ToList();

                // Связываем товары с материалами
                foreach (var item in items)
                {
                    item.MaterialyIFurnitura = _allMaterials
                        .FirstOrDefault(m => m.Id_MaterialyIFurnitura == item.Id_Materiala);
                }

                ItemsGrid.ItemsSource = items;
                UpdateTotal();
            }
            catch (Exception ex)
            {
                ShowError($"Ошибка при загрузке данных поступления: {ex.Message}");
            }
        }

        // Кнопка "Удалить"
        private void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            if (_currentPostuplenie == null) return;

            var result = MessageBox.Show(
                $"Вы уверены, что хотите удалить поступление №{_currentPostuplenie.Id_PostuplenieTovara}?",
                "Подтверждение удаления",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    // Сначала удаляем связанные товары (табличная часть)
                    var items = _context.PostuplenieTovara_Soderzhimoe
                        .Where(i => i.Id_Postupleniya == _currentPostuplenie.Id_PostuplenieTovara)
                        .ToList();

                    _context.PostuplenieTovara_Soderzhimoe.RemoveRange(items);

                    // Затем удаляем само поступление
                    _context.PostuplenieTovara.Remove(_currentPostuplenie);
                    _context.SaveChanges();

                    LoadData();
                    ClearForm();

                    MessageBox.Show("Поступление успешно удалено!", "Успех",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    ShowError($"Ошибка при удалении: {ex.Message}");
                }
            }
        }

        // Поиск по списку поступлений
        private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(SearchBox.Text))
            {
                PostuplenieGrid.ItemsSource = _allPostupleniya;
            }
            else
            {
                var searchText = SearchBox.Text.ToLower();
                var filtered = _allPostupleniya.Where(p =>
                    (p.CompanyPostavshiki?.NamePostavshiki ?? "").ToLower().Contains(searchText) ||
                    (p.Dogovor ?? "").ToLower().Contains(searchText)
                ).ToList();

                PostuplenieGrid.ItemsSource = filtered;
            }
        }

        // Выбор в списке
        private void PostuplenieGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            _currentPostuplenie = PostuplenieGrid.SelectedItem as PostuplenieTovara;

            if (_currentPostuplenie != null)
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

        // Добавление новой строки в таблицу
        private void ItemsGrid_AddingNewItem(object sender, AddingNewItemEventArgs e)
        {
            e.NewItem = new PostuplenieTovara_Soderzhimoe
            {
                Kolichestvo_Postuplenie = 1,
                Cena = 0,
                Summa = 0
            };
        }

        // Завершение редактирования ячейки
        private void ItemsGrid_CellEditEnding(object sender, DataGridCellEditEndingEventArgs e)
        {
            UpdateTotal();
        }

        // Кнопка "Сохранить"
        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            if (!ValidateForm()) return;

            try
            {
                if (_isNewRecord)
                {
                    // Создание нового поступления
                    var newPostuplenie = new PostuplenieTovara
                    {
                        DataPostupleniya = DatePicker.SelectedDate.Value,
                        CompaniyaPostavshika = ((CompanyPostavshiki)SupplierComboBox.SelectedItem).Id_Postavshiki,
                        Dogovor = ContractBox.Text.Trim()
                    };

                    _context.PostuplenieTovara.Add(newPostuplenie);
                    _context.SaveChanges();

                    // Добавляем товары
                    foreach (var item in ItemsGrid.Items)
                    {
                        if (item is PostuplenieTovara_Soderzhimoe lineItem)
                        {
                            var newItem = new PostuplenieTovara_Soderzhimoe
                            {
                                Id_Postupleniya = newPostuplenie.Id_PostuplenieTovara,
                                Id_Materiala = lineItem.Id_Materiala,
                                Kolichestvo_Postuplenie = lineItem.Kolichestvo_Postuplenie,
                                Cena = lineItem.Cena,
                                Summa = lineItem.Kolichestvo_Postuplenie * lineItem.Cena
                            };

                            _context.PostuplenieTovara_Soderzhimoe.Add(newItem);
                        }
                    }

                    _context.SaveChanges();

                    MessageBox.Show("Поступление успешно создано!", "Успех",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    // Обновление существующего поступления
                    _currentPostuplenie.DataPostupleniya = DatePicker.SelectedDate.Value;
                    _currentPostuplenie.CompaniyaPostavshika = ((CompanyPostavshiki)SupplierComboBox.SelectedItem).Id_Postavshiki;
                    _currentPostuplenie.Dogovor = ContractBox.Text.Trim();

                    // Удаляем старые товары
                    var oldItems = _context.PostuplenieTovara_Soderzhimoe
                        .Where(i => i.Id_Postupleniya == _currentPostuplenie.Id_PostuplenieTovara)
                        .ToList();

                    _context.PostuplenieTovara_Soderzhimoe.RemoveRange(oldItems);

                    // Добавляем новые товары
                    foreach (var item in ItemsGrid.Items)
                    {
                        if (item is PostuplenieTovara_Soderzhimoe lineItem)
                        {
                            var newItem = new PostuplenieTovara_Soderzhimoe
                            {
                                Id_Postupleniya = _currentPostuplenie.Id_PostuplenieTovara,
                                Id_Materiala = lineItem.Id_Materiala,
                                Kolichestvo_Postuplenie = lineItem.Kolichestvo_Postuplenie,
                                Cena = lineItem.Cena,
                                Summa = lineItem.Kolichestvo_Postuplenie * lineItem.Cena
                            };

                            _context.PostuplenieTovara_Soderzhimoe.Add(newItem);
                        }
                    }

                    _context.SaveChanges();

                    MessageBox.Show("Поступление успешно обновлено!", "Успех",
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

        // Кнопка "Назад"
        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            NavigationService.Navigate(new ViborDocumentaPage());
        }
    }
}
