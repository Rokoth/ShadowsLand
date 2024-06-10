using System.ComponentModel.DataAnnotations;
using System.Web.Mvc;

namespace Shadows.Contract
{
    public class User : Entity
    {
        [Display(Name = "Имя")]
        [Required(ErrorMessage = "Поле должно быть установлено")]
        [Remote("CheckName", "User", ErrorMessage = "Имя уже используется")]
        public string Name { get; set; }

        [Display(Name = "Описание")]
        public string Description { get; set; }

        [Display(Name = "Логин")]
        [Required(ErrorMessage = "Поле должно быть установлено")]
        [Remote("CheckLogin", "User", ErrorMessage = "Логин уже используется")]
        public string Login { get; set; }
        [Display(Name = "Email")]
        public string Email { get; set; }
        [Display(Name = "Только листовые элементы")]
        public bool LeafOnly { get; set; }
        [Display(Name = "ИД формулы")]
        public Guid FormulaId { get; set; }
        [Display(Name = "Формула")]
        public string Formula { get; set; }
        [Display(Name = "Дата последнего добавления элемента в резервы")]
        public DateTimeOffset? LastAddedDate { get; set; }
        [Display(Name = "Величина резерва по умолчанию")]
        [Required(ErrorMessage = "Поле должно быть установлено")]
        public decimal DefaultReserveValue { get; set; }

        [Display(Name = "Период добавления элементов в резервы")]
        [Required(ErrorMessage = "Поле должно быть установлено")]
        public int AddPeriod { get; set; }

    }
}