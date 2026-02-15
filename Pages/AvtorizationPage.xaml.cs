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

    // Страница авторизации пользователя.
    // Проверяет логин и пароль, загружает данные пользователя.
    public partial class AvtorizationPage : Page
    {
        public AvtorizationPage()
        {
            InitializeComponent();
        }

        // Обработчик нажатия кнопки "Войти"
        // Проверяет введенные данные и выполняет авторизацию
        private void LoginButton_Click(object sender, RoutedEventArgs e)
        {
            // Получаем введенные логин и пароль, удаляем лишние пробелы
            string login = LoginTextBox.Text.Trim();
            string password = PasswordBox.Password;

            // Проверка на пустые поля
            if (string.IsNullOrEmpty(login) || string.IsNullOrEmpty(password))
            {
                ErrorTextBlock.Text = "Введите логин и пароль";
                return;
            }

            try
            {
                // Получаем контекст базы данных
                var context = DataBaseEntities.GetContext();

                // Ищем пользователя с указанным логином и паролем
                var user = context.Polzovateli
                    .FirstOrDefault(u => u.Login == login && u.Password == password);

                // Если пользователь не найден
                if (user == null)
                {
                    ErrorTextBlock.Text = "Неверный логин или пароль";
                    return;
                }

                // Сохраняем текущего пользователя в статическом классе
                Classes.CurrentUser.UserId = user.Id_Polzovatel;
                Classes.CurrentUser.FullName = $"{user.FamiliyaPolzovatelia} {user.ImyaPolzovatelya} {user.OtchestvoPolzovatelia}";
                Classes.CurrentUser.RoleId = user.Id_Roli;
                Classes.CurrentUser.RoleName = user.Roli.NameRoli;

                // Переходим на страницу выбора документа
                NavigationService.Navigate(new ViborDocumentaPage());
            }
            catch (Exception ex)
            {
                // Обработка ошибок подключения к БД
                ErrorTextBlock.Text = $"Ошибка подключения к базе данных: {ex.Message}";
            }
        }
    }
}
