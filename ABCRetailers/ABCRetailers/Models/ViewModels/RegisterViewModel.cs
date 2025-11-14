using System.ComponentModel.DataAnnotations;

namespace ABCRetailers.Models.ViewModels
{
    public class RegisterViewModel
    {
        // Login fields
        [Required]
        public string Username { get; set; } = string.Empty;

        [Required, DataType(DataType.Password)]
        public string PasswordHash { get; set; } = string.Empty;

        [Required]
        public string Role { get; set; } = "Customer";

        // Customer profile fields - make these nullable
        [Display(Name = "First Name")]
        public string? Name { get; set; }

        [Display(Name = "Last Name")]
        public string? Surname { get; set; }

        [EmailAddress]
        public string? Email { get; set; }

        [Display(Name = "Shipping Address")]
        public string? ShippingAddress { get; set; }
    }
}