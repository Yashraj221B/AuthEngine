using System.ComponentModel.DataAnnotations;
namespace AuthEngine.Models
{
    public class UserInfo
    {
        [Key]
        required public string UserId { get; set; }
        required public string FirstName { get; set; }
        required public string LastName { get; set; }
        public string? Email { get; set; }
        public string? PhoneNumber { get; set; }
    }

    public class Credentials
    {
        [Key]
        required public string UserId { get; set; }
        required public string Username { get; set; }
        required public string Password { get; set; }
        required public DateTime CreatedAt { get; set; }
        public DateTime? LastLogin { get; set; }
        public DateTime? LastPasswordChange { get; set; }
        public string? Token { get; set; }
        public DateTime? TokenExpires { get; set; }
        public bool? IsAdmin { get; set; }
        public bool? IsDisabled { get; set; }
    }
}