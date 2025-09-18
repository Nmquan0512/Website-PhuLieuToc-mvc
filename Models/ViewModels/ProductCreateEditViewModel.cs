using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace PhuLieuToc.Models.ViewModels
{
    public class VariantInputViewModel
    {
        public int? SanPhamChiTietId { get; set; }
        public List<int> GiaTriThuocTinhIds { get; set; } = new List<int>();

        [Required]
        [Range(0, double.MaxValue)]
        public decimal Gia { get; set; }

        [Required]
        [Range(0, int.MaxValue)]
        public int SoLuongTon { get; set; }

        public string? Anh { get; set; }
        public IFormFile? AnhFile { get; set; }

        [Range(0, 1)]
        public int TrangThai { get; set; } = 1;
    }

    public class ProductCreateEditViewModel
    {
        public int? SanPhamId { get; set; }

        [Required]
        [StringLength(200)]
        public string TenSanPham { get; set; }

        [Required]
        [StringLength(200)]
        public string Slug { get; set; }

        [StringLength(2000)]
        public string? MoTa { get; set; }

        [Range(0, 1)]
        public int TrangThai { get; set; } = 1;

        [Required]
        public int CategoryId { get; set; }

        [Required]
        public int BrandId { get; set; }

        // UI Selections
        public List<int> SelectedThuocTinhIds { get; set; } = new List<int>();

        public List<VariantInputViewModel> Variants { get; set; } = new List<VariantInputViewModel>();

        // Dropdown sources
        public List<CategoryModel> Categories { get; set; } = new List<CategoryModel>();
        public List<BrandModel> Brands { get; set; } = new List<BrandModel>();
    }
}


