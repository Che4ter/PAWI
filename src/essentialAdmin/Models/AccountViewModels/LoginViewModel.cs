﻿using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace essentialAdmin.Models.AccountViewModels
{
    public class LoginViewModel
    {
        [Required]
        [EmailAddress]
        [DisplayName("E-Mail Adresse")]
        public string Email { get; set; }

        [Required]
        [DataType(DataType.Password)]
        [DisplayName("Passwort")]
        public string Password { get; set; }

        [Display(Name = "Merken?")]
        public bool RememberMe { get; set; }
    }
}
