using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PhuLieuToc.Models
{
    public class GioHang
    {
        [Key]
        public int GioHangId { get; set; }

        [Required]
        [ForeignKey(nameof(TaiKhoan))]
        public int TaiKhoanId { get; set; }
        public TaiKhoan TaiKhoan { get; set; }

        public ICollection<GioHangChiTiet> GioHangChiTiets { get; set; } = new List<GioHangChiTiet>();

        [Required]
        public DateTime NgayTao { get; set; } = DateTime.Now;

        public DateTime? NgayCapNhat { get; set; }
    }
}
