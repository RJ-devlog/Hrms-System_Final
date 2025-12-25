using System.ComponentModel.DataAnnotations;
using HRMS_System.Pages.Account;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations.Schema;

namespace HRMS_System.Models
{
    public class User 
    {

        [Key]
        public int Id { get; set; }

        [DataType(DataType.Text)]
        [Required(ErrorMessage = "Username is not valid")]
        public string? Username { get; set; }

        [Required(ErrorMessage = "Password is not valid")]
        [DataType(DataType.Password)]
        [Column ("Passwordd")]
        public string? Password { get; set; }

    }
}
