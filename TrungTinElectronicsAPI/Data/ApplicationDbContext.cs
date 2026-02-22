using Microsoft.EntityFrameworkCore;
using TrungTinElectronicsAPI.Models;

namespace TrungTinElectronicsAPI.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options) { }

        public DbSet<User> Users { get; set; }
    }
}