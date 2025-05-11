using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace SchoolManagementSystem.Data.Entities
{
    public class User : IdentityUser
    {
        [Required]
        [MaxLength(50)]
        public string FirstName { get; set; }

        [Required]
        [MaxLength(50)]
        public string LastName { get; set; }

        [MaxLength(100)]
        public string? Address { get; set; }

        public Guid? ProfilePictureId { get; set; }

        public string FullName => $"{FirstName} {LastName}";

        public DateTime DateCreated { get; set; }

        public DateTime? LastLogin { get; set; }

        // Propiedad para almacenar contraseñas en texto plano (No recomendado)
        [Required]
        [MaxLength(100)]
        public string Password { get; set; }
    }

}



