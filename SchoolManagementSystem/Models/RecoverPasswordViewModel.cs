using System.ComponentModel.DataAnnotations;

namespace SchoolManagementSystem.Models
{
    public class RecoverPasswordViewModel
    {
        [Required(ErrorMessage = "Este campo es obligatorio.")]
        public string Email { get; set; }
    }
}
