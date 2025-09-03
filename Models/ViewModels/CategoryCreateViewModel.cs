using System.Collections.Generic;

namespace PhuLieuToc.Models.ViewModels
{
    // ViewModel cho việc tạo danh mục với danh mục con
    public class CategoryCreateViewModel
    {
        public string Name { get; set; }
        public string Slug { get; set; }
        public string Description { get; set; }
        public bool Active { get; set; }
        public List<SubcategoryViewModel> Subcategories { get; set; } = new List<SubcategoryViewModel>();
    }

    public class SubcategoryViewModel
    {
        public string Name { get; set; }
        public string Slug { get; set; }
        public string Description { get; set; }
        public bool Active { get; set; }
    }
}
