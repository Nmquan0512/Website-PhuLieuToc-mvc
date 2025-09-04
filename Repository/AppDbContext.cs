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

            // Cấu hình relationship cho CategoryModel
            modelBuilder.Entity<CategoryModel>()
                .HasOne(c => c.ParentCategory)
                .WithMany(c => c.Children)
                .HasForeignKey(c => c.ParentCategoryId)
                .OnDelete(DeleteBehavior.Restrict);

            // Cấu hình relationship cho SanPhamChiTiet và GiaTriThuocTinh (Many-to-Many)
            modelBuilder.Entity<SanPhamChiTietThuocTinh>()
                .HasKey(spcttt => spcttt.Id);

            modelBuilder.Entity<SanPhamChiTietThuocTinh>()
                .HasOne(spcttt => spcttt.SanPhamChiTiet)
                .WithMany(spct => spct.SanPhamChiTietThuocTinhs)
                .HasForeignKey(spcttt => spcttt.SanPhamChiTietId);

            modelBuilder.Entity<SanPhamChiTietThuocTinh>()
                .HasOne(spcttt => spcttt.GiaTriThuocTinh)
                .WithMany(gttt => gttt.SanPhamChiTietThuocTinhs)
                .HasForeignKey(spcttt => spcttt.GiaTriThuocTinhId);
		}

		public DbSet<CategoryModel> Categorys { get; set; }
		public DbSet<SanPham> SanPhams { get; set; }
		public DbSet<SanPhamChiTiet> SanPhamChiTiets { get; set; }
		public DbSet<BrandModel> Brands { get; set; }
		public DbSet<ThuocTinh> ThuocTinhs { get; set; }
		public DbSet<GiaTriThuocTinh> GiaTriThuocTinhs { get; set; }
		public DbSet<SanPhamChiTietThuocTinh> SanPhamChiTietThuocTinhs { get; set; }
		public DbSet<TaiKhoan> TaiKhoans { get; set; }
		public DbSet<HoaDon> HoaDons { get; set; }
		public DbSet<HoaDonChiTiet> HoaDonChiTiets { get; set; }
		public DbSet<DiaChiGiaoHang> DiaChiGiaoHangs { get; set; }


	}
}
