using Kursovoi.Classes;
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
    
    // Главная страница после авторизации.
    // Отображает доступные документы в зависимости от роли пользователя.
    public partial class ViborDocumentaPage : Page
    {
        public ViborDocumentaPage()
        {
            InitializeComponent();
            Loaded += ViborDocumentaPage_Loaded;
            UserNameText.Text = CurrentUser.FullName; // Отображаем имя пользователя
        }

        private void ViborDocumentaPage_Loaded(object sender, RoutedEventArgs e)
        {
            // Блокируем кнопки в зависимости от роли пользователя
            SetAccessByRole();
        }

        // Установка прав доступа к кнопкам в зависимости от роли
        private void SetAccessByRole()
        {
            // По умолчанию все кнопки активны
            // Блокируем в зависимости от роли

            // Менеджер (1) - только Оформление заказа и Список заказов
            // Замерщик (2) - Поступление товара, Спецификация, Список заказов
            // Мастер (3) - Сборка, Список заказов
            // Директор (4) - все документы
            // Администратор (5) - все документы

            switch (CurrentUser.RoleId)
            {
                case CurrentUser.ROLE_MANAGER:
                    BtnPostuplenieTovara.IsEnabled = false;
                    BtnSpecifikaciya.IsEnabled = false;
                    BtnSborka.IsEnabled = false;
                break;

                case CurrentUser.ROLE_ZAMERSHIK:
                    BtnOformlenieZakaza.IsEnabled = false;
                    BtnSborka.IsEnabled = false;
                break;

                case CurrentUser.ROLE_MASTER:
                    BtnOformlenieZakaza.IsEnabled = false;
                    BtnPostuplenieTovara.IsEnabled = false;
                    BtnSpecifikaciya.IsEnabled = false;
                break;

                case CurrentUser.ROLE_DIRECTOR:
                case CurrentUser.ROLE_ADMIN:
                    // Все кнопки активны
                break;
            }
        }

        // Показать сообщение о запрете доступа
        private void ShowMessageNoAccess()
        {
            MessageBox.Show("У вас нет доступа к этому документу!", "Доступ запрещен",
                MessageBoxButton.OK, MessageBoxImage.Warning);
        }

        // Переход к оформлению заказа(с проверкой прав)
        private void BtnOformlenieZakaza_Click(object sender, RoutedEventArgs e)
        {
            // Проверяем доступ
            if (!BtnOformlenieZakaza.IsEnabled)
            {
                ShowMessageNoAccess();
                return;
            }
            NavigationService.Navigate(new OformlenieZakazaPage());
        }

        // Переход к поступлению товара (с проверкой прав)
        private void BtnPostuplenieTovara_Click(object sender, RoutedEventArgs e)
        {
            if (!BtnPostuplenieTovara.IsEnabled)
            {
                ShowMessageNoAccess();
                return;
            }
            NavigationService.Navigate(new PostuplenieTovaraPage());
        }

        // Переход к спецификации (с проверкой прав)
        private void BtnSpecifikaciya_Click(object sender, RoutedEventArgs e)
        {
            if (!BtnSpecifikaciya.IsEnabled)
            {
                ShowMessageNoAccess();
                return;
            }
            NavigationService.Navigate(new SpecifikaciyaNaIzdeliePage());
        }

        // Переход к сборке (с проверкой прав)
        private void BtnSborka_Click(object sender, RoutedEventArgs e)
        {
            if (!BtnSborka.IsEnabled)
            {
                ShowMessageNoAccess();
                return;
            }
            NavigationService.Navigate(new SborkaPage());
        }

        // Переход к списку заказов (доступен всем)
        private void BtnSpisokZakazov_Click(object sender, RoutedEventArgs e)
        {
            NavigationService.Navigate(new SpisokZakazovPage());
        }

        // Выход - возврат на страницу авторизации
        private void BtnExit_Click(object sender, RoutedEventArgs e)
        {
            // Возвращаемся на страницу авторизации
            NavigationService.Navigate(new AvtorizationPage());
        }
    }
}
