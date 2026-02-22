using Microsoft.EntityFrameworkCore;
using TrungTinElectronicsAPI.Models;

namespace TrungTinElectronicsAPI.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<Product> Products { get; set; }
        public DbSet<Category> Categories { get; set; }
        public DbSet<ProductTag> ProductTags { get; set; }
        // thêm DbSet khác ở đây

        public DbSet<User> Users { get; set; }
    }
}
