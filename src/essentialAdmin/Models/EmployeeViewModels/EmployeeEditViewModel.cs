﻿using essentialAdmin.Data.Models;
using Microsoft.AspNetCore.Mvc.Rendering;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace essentialAdmin.Models.EmployeeViewModels
{
    public class EmployeeEditViewModel
    {
        [Display(Name = "Vorname")]
        public string FirstName { get; set; }

        [Display(Name = "Nachname")]
        public string LastName { get; set; }

        [Required]
        [EmailAddress]
        [Display(Name = "E-Mail Adresse")]
        public string Email { get; set; }

        public string currentEmail { get; set; }

        [Required]
        [Display(Name = "Rolle")]
        public string Role { get; set; }

        [Display(Name = "Rolle")]
        public virtual List<EmployeeRolesViewModel> EmployeeRoles { get; set; }

    }
}
