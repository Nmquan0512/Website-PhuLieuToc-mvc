using Microsoft.EntityFrameworkCore;
using PhuLieuToc.Models;
using System.Linq;

namespace PhuLieuToc.Repository
{
    public class SeedData
    {
        public static void SeedingData(AppDbContext _context)
        {
            // Tự động apply migration nếu chưa có
            _context.Database.Migrate();

            // Nếu chưa có sản phẩm nào, tiến hành tạo dữ liệu mẫu
            if (!_context.Products.Any())
            {
                // Tạo các danh mục cha
                var thietBi = new CategoryModel
                {
                    TenDanhMuc = "Thiết bị",
                    Slug = "thiet-bi",
                    MoTa = "Thiết bị làm đẹp, tạo kiểu tóc",
                    TrangThai = 1
                };

                var hoaChat = new CategoryModel
                {
                    TenDanhMuc = "Hoá chất",
                    Slug = "hoa-chat",
                    MoTa = "Các loại hoá chất dành cho tóc",
                    TrangThai = 1
                };

                var dungCuSalon = new CategoryModel
                {
                    TenDanhMuc = "Dụng cụ salon",
                    Slug = "dung-cu-salon",
                    MoTa = "Dụng cụ và phụ kiện salon",
                    TrangThai = 1
                };

                var chamSocToc = new CategoryModel
                {
                    TenDanhMuc = "Sản phẩm chăm sóc tóc",
                    Slug = "cham-soc-toc",
                    MoTa = "Sản phẩm chăm sóc và nuôi dưỡng tóc",
                    TrangThai = 1
                };

                var nail = new CategoryModel
                {
                    TenDanhMuc = "Nail",
                    Slug = "nail",
                    MoTa = "Phụ liệu và dụng cụ làm nail",
                    TrangThai = 1
                };

                var noiThat = new CategoryModel
                {
                    TenDanhMuc = "Nội thất",
                    Slug = "noi-that",
                    MoTa = "Phụ kiện nội thất salon",
                    TrangThai = 1
                };

                var makeup = new CategoryModel
                {
                    TenDanhMuc = "Makeup",
                    Slug = "makeup",
                    MoTa = "Phụ kiện và mỹ phẩm trang điểm",
                    TrangThai = 1
                };

                // Tạo danh mục con cho "Thiết bị"
                var mayDuoiToc = new CategoryModel
                {
                    TenDanhMuc = "Máy duỗi tóc",
                    Slug = "may-duoi-toc",
                    MoTa = "Máy duỗi tóc cao cấp, công nghệ hiện đại",
                    TrangThai = 1,
                    ParentCategory = thietBi
                };

                var maySayToc = new CategoryModel
                {
                    TenDanhMuc = "Máy sấy tóc",
                    Slug = "may-say-toc",
                    MoTa = "Máy sấy tóc với nhiều tính năng hiện đại",
                    TrangThai = 1,
                    ParentCategory = thietBi
                };

                var mayUonToc = new CategoryModel
                {
                    TenDanhMuc = "Máy uốn tóc",
                    Slug = "may-uon-toc",
                    MoTa = "Máy uốn tóc đa năng, chuyên dụng cho salon",
                    TrangThai = 1,
                    ParentCategory = thietBi
                };

                thietBi.Children.Add(mayDuoiToc);
                thietBi.Children.Add(maySayToc);
                thietBi.Children.Add(mayUonToc);

                // Tạo danh mục con cho "Hoá chất"
                var thuocNhuomToc = new CategoryModel
                {
                    TenDanhMuc = "Thuốc nhuộm tóc",
                    Slug = "thuoc-nhuom-toc",
                    MoTa = "Thuốc nhuộm tóc đa dạng màu sắc",
                    TrangThai = 1,
                    ParentCategory = hoaChat
                };

                var thuocDuongToc = new CategoryModel
                {
                    TenDanhMuc = "Thuốc dưỡng tóc",
                    Slug = "thuoc-duong-toc",
                    MoTa = "Thuốc dưỡng tóc giúp phục hồi và bảo vệ tóc",
                    TrangThai = 1,
                    ParentCategory = hoaChat
                };

                hoaChat.Children.Add(thuocNhuomToc);
                hoaChat.Children.Add(thuocDuongToc);

                // Thêm các danh mục cha (có chứa danh mục con nếu có) vào context
                _context.Categorys.AddRange(thietBi, hoaChat, dungCuSalon, chamSocToc, nail, noiThat, makeup);

                // Seed dữ liệu cho thương hiệu
                var brandA = new BrandModel
                {
                    TenThuongHieu = "Thương hiệu A",
                    Slug = "thuong-hieu-a",
                    MoTa = "Mô tả thương hiệu A",
                    TrangThai = 1
                };

                var brandB = new BrandModel
                {
                    TenThuongHieu = "Thương hiệu B",
                    Slug = "thuong-hieu-b",
                    MoTa = "Mô tả thương hiệu B",
                    TrangThai = 1
                };

                _context.Brands.AddRange(brandA, brandB);

                // Lưu lại để EF gán Id cho danh mục và thương hiệu
                _context.SaveChanges();

                // Tạo sản phẩm (tham chiếu danh mục con – leaf categories)
                var product1 = new SanPhamChiTiet
                {
                    TenSanPham = "Máy duỗi tóc X1",
                    Slug = "may-duoi-toc-x1",
                    MoTa = "Máy duỗi tóc cao cấp, công nghệ mới.",
                    GiaBan = 1200000m,
                    Anh = "/images/home/may-duoi-toc-x1.png",
                    BrandId = brandA.Id,
                    CategoryId = mayDuoiToc.Id
                };

                var product2 = new SanPhamChiTiet
                {
                    TenSanPham = "Thuốc nhuộm tóc Đỏ Ruby",
                    Slug = "thuoc-nhuom-toc-do-ruby",
                    MoTa = "Thuốc nhuộm màu đỏ ruby bền lâu.",
                    GiaBan = 200000m,
                    Anh = "/images/home/thuoc-nhuom-do-ruby.jpg",
                    BrandId = brandB.Id,
                    CategoryId = thuocNhuomToc.Id
                };

                var product3 = new SanPhamChiTiet
                {
                    TenSanPham = "Thuốc dưỡng tóc VIP",
                    Slug = "thuoc-duong-toc-vip",
                    MoTa = "Thuốc dưỡng tóc cho mái tóc mượt mà và chắc khỏe.",
                    GiaBan = 20000m,
                    Anh = "/images/home/sanpham1.jpg",
                    BrandId = brandB.Id,
                    CategoryId = thuocDuongToc.Id
                };

                _context.Products.AddRange(product1, product2, product3);

                _context.SaveChanges();
            }
        }
    }
}
