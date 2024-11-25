using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Market.Models.ViewModels;
public class ProductViewModel
{
    public int Id { get; set; }
    public int StoreId { get; set; }
    public string? StoreName { get; set; }

    [Required(ErrorMessage = "Product Name is required.")]
    public string Name { get; set; }

    [Required(ErrorMessage = "Product Description is required.")]
    public string Description { get; set; }

    [Required(ErrorMessage = "Price is required.")]
    [Range(0.01, double.MaxValue, ErrorMessage = "Price must be greater than zero.")]
    public decimal Price { get; set; }

    [Required(ErrorMessage = "Stock Quantity is required.")]
    [Range(0, int.MaxValue, ErrorMessage = "Stock Quantity must be non-negative.")]
    public int StockQuantity { get; set; }

    public IEnumerable<IFormFile> Photos { get; set; } = new List<IFormFile>();
    public IEnumerable<CategoryViewModel> Categories { get; set; } = new List<CategoryViewModel>();
    public IEnumerable<SelectListItem> AvailableCategories { get; set; } = new List<SelectListItem>();

    public List<int> SelectedCategoryIds { get; set; } = new List<int>();
    public IEnumerable<ProductPhotoViewModel> ExistingPhotos { get; set; } = new List<ProductPhotoViewModel>();
}

public class ProductPhotoViewModel
{
    public int Id { get; set; }
    public string FilePath { get; set; }
    public bool PhotoToDelete { get; set; } // Флаг для удаления фото
}

public class EditProductPhotoViewModel
{
    public int ProductId { get; set; }
    public List<ProductPhotoViewModel> ExistingPhotos { get; set; } = new List<ProductPhotoViewModel>();
}

