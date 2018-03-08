using System.ComponentModel.DataAnnotations;

namespace ASP.NETCoreAPIAuthSchemeUsingJWTs.Models.ApiDtos
{
    public class AccountApiDto
    {
        [Required]
        public string Username { get; set; }

        [Required]
        public string Password { get; set; }
    }
}