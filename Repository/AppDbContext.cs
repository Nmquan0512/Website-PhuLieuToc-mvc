using Microsoft.EntityFrameworkCore;
using PhuLieuToc.Models;

namespace PhuLieuToc.Repository
{
	public class AppDbContext : DbContext
	{



		public AppDbContext(DbContextOptions options) : base(options)
		{
		}

		protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
		{
			optionsBuilder.UseSqlServer("Data Source=WINDOWS-PC\\SQLEXPRESS02;Initial Catalog=PhuLieuToc;Trusted_Connection=True;TrustServerCertificate=True");
		}

		protected override void OnModelCreating(ModelBuilder modelBuilder)
		{
			base.OnModelCreating(modelBuilder);


            modelBuilder.Entity<BrandModel>()
			.HasMany(b => b.Products)
			.WithOne(p => p.Brand)
			.HasForeignKey(p => p.BrandId);
		}

		public DbSet<CategoryModel> Categorys { get; set; }
		public DbSet<ProductModel> Products { get; set; }

		public DbSet<BrandModel> Brands { get; set; }


	}
}
