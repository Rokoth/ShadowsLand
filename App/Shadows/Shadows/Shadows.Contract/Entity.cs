using System.ComponentModel.DataAnnotations;
using System.Xml.Linq;

namespace Shadows.Contract
{

    /// <summary>
    /// Базовый класс
    /// </summary>
    public class Entity : IEntity
    {
        /// <summary>
        /// Идентификатор
        /// </summary>
        [Display(Name = "Идентификатор")]
        public Guid Id { get; set; }
    }
}