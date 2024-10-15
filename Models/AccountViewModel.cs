using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using PagedList;

namespace ELearning.Models
{
    public class AccountViewModel
    {
    }

    public class LoginViewModel
    { 
        [Required]
        public string UserName { get; set; } = "";
        [Required]
        public string Password { get; set; } = ""; 
        public bool IsVerified { get; set; } = false;
        public string FullName { get; set; } = "";
        public string Email { get; set; } = "";
        public string UserType { get; set; } = "";
        public int UserID { get; set; } = 0;
        public string DefaultImageName { get; set; } = "";
        public int ClassID { get; set; } = 0;
        public bool IsActive { get; set; } = false; 
        public int CountNotVeriedStudents { get; set; } = 0;
    }

    public class RegisterViewModel
    {
        [Required]
        public string FullName { get; set; } = "";
        [Required]
        [EmailAddress]
        public string Email { get; set; } = "";
        [Required]
        public string UserName { get; set; } = "";
        [Required]
        public string Password { get; set; } = ""; 
        public bool IsVerified { get; set; } = false;
        public string UserType { get; set; } = "";
        [Required]
        [Display(Name = "Class")]
        public int ClassID { get; set; } = 0; 

        public IEnumerable<SelectListItem> ClassList { get; set; }
    }

     
}