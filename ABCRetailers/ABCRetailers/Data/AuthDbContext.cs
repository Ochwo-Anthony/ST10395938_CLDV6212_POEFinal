using Microsoft.EntityFrameworkCore;
using ABCRetailers.Models;


namespace ABCRetailers.Data
{
    public class AuthDbContext : DbContext
    {
        public AuthDbContext(DbContextOptions<AuthDbContext> options) : base(options) { }

        public DbSet<User> Users { get; set; }
        public DbSet<Cart> Cart {  get; set; }
    }
}
