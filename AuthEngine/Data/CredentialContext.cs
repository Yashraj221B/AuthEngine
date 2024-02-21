using Microsoft.EntityFrameworkCore;
using AuthEngine.Models;

namespace AuthEngine.Data
{
    public class CredentialContext : DbContext
    {
        public CredentialContext(DbContextOptions<CredentialContext> options) : base(options)
        {
        }
        public DbSet<UserInfo> UserInfo { get; set; }
        public DbSet<Credentials> Credentials { get; set; }
    }
}