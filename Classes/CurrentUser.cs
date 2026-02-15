using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Kursovoi.Classes
{
    // Статический класс для хранения данных о текущем авторизованном пользователе.
    // Доступен из любой точки приложения без создания экземпляра.
    public static class CurrentUser
    {
        //Уникальный идентификатор пользователя из БД
        public static int UserId { get; set; }

        // Полное имя пользователя (Фамилия + Имя + Отчество)
        public static string FullName { get; set; }

        // ID роли пользователя (числовой код из БД)
        public static int RoleId { get; set; }

        // Название роли пользователя (например "Менеджер")
        public static string RoleName { get; set; }

        // Константы для ID ролей (должны соответствовать базе данных)
        public const int ROLE_MANAGER = 1;      // Менеджер
        public const int ROLE_ZAMERSHIK = 2;    // Замерщик
        public const int ROLE_MASTER = 3;        // Мастер
        public const int ROLE_DIRECTOR = 4;      // Директор
        public const int ROLE_ADMIN = 5;         // Администратор
    }
}
