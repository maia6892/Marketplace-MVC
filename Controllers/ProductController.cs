using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Market.Data;
using Market.Models;
using Market.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Market.Controllers
{
    public class ProductController(MarketDbContext context, UserManager<User> userManager, IWebHostEnvironment environment) : Controller
    {
        public IActionResult AddProduct(int storeId)
        {
            var model = new ProductViewModel
            {
                StoreId = storeId,
                AvailableCategories = context.Categories
                    .Select(c => new SelectListItem { Value = c.Id.ToString(), Text = c.Name })
                    .ToList()
            };

            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> AddProduct(ProductViewModel productViewModel)
        {
            if (!ModelState.IsValid)
            {
                await PopulateAvailableCategories(productViewModel);
                return View(productViewModel);
            }

            var user = await userManager.GetUserAsync(User);
            var seller = await context.Sellers
                .Include(s => s.Stores)
                .FirstOrDefaultAsync(s => s.UserId == user.Id);

            if (seller == null)
            {
                ModelState.AddModelError("", "User is not registered as a seller.");
                await PopulateAvailableCategories(productViewModel);
                return View(productViewModel);
            }

            var store = seller.Stores.FirstOrDefault(s => s.Id == productViewModel.StoreId);
            if (store == null)
            {
                ModelState.AddModelError("", "Store not found.");
                await PopulateAvailableCategories(productViewModel);
                return View(productViewModel);
            }

            var product = new Product
            {
                Name = productViewModel.Name,
                Description = productViewModel.Description,
                Price = productViewModel.Price,
                StockQuantity = productViewModel.StockQuantity,
                StoreId = productViewModel.StoreId,
                Categories = await context.Categories
                    .Where(c => productViewModel.SelectedCategoryIds.Contains(c.Id))
                    .ToListAsync(),
                Photos = new List<ProductPhoto>()
            };

            await SaveProductPhotos(productViewModel.Photos, product);

            context.Products.Add(product);
            await context.SaveChangesAsync();

            return RedirectToAction("StoreDetails", "Store", new { storeId = productViewModel.StoreId });
        }

        public async Task<IActionResult> ProductDetails(int productId)
        {
            var product = await context.Products
                .Include(p => p.Store)//
                .Include(p => p.Photos)
                .Include(p => p.Categories)
                .FirstOrDefaultAsync(p => p.Id == productId);

            if (product == null)
            {
                return NotFound("Product not found.");
            }

            var model = new ProductViewModel
            {
                Id = product.Id,
                Name = product.Name,
                Description = product.Description,
                Price = product.Price,
                StockQuantity = product.StockQuantity,
                StoreId = product.StoreId,
                StoreName = product.Store.Name,
                Categories = product.Categories?.Select(c => new CategoryViewModel { Id = c.Id, Name = c.Name }).ToList() ?? new List<CategoryViewModel>(),
                Photos = product.Photos.Select(photo => new FormFile(null, 0, 0, null, photo.FilePath)).ToList(),
                ExistingPhotos = product.Photos.Select(photo => new ProductPhotoViewModel { Id = photo.Id, FilePath = photo.FilePath }).ToList()
            };

            return View(model);
        }



        public async Task<IActionResult> ViewProduct(int productId)
        {
            var product = await context.Products
                .Include(p => p.Photos)
                .Include(p => p.Categories)
                .Include(p => p.Reviews)
                .FirstOrDefaultAsync(p => p.Id == productId);

            if (product == null)
            {
                return NotFound("Product not found.");
            }

            var model = new ProductViewModel
            {
                Id = product.Id,
                Name = product.Name,
                Description = product.Description,
                Price = product.Price,
                StockQuantity = product.StockQuantity,
                StoreId = product.StoreId,
                Categories = product.Categories?.Select(c => new CategoryViewModel { Id = c.Id, Name = c.Name }).ToList() ?? new List<CategoryViewModel>(),
                Photos = product.Photos.Select(photo => new FormFile(null, 0, 0, null, photo.FilePath)).ToList()
            };

            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> EditProduct(int productId)
        {
            var product = await context.Products
                .Include(p => p.Categories)
                .Include(p => p.Photos)
                .FirstOrDefaultAsync(p => p.Id == productId);

            if (product == null)
            {
                return NotFound("Product not found.");
            }

            var viewModel = new ProductViewModel
            {
                Id = product.Id,
                StoreId = product.StoreId,
                Name = product.Name,
                Description = product.Description,
                Price = product.Price,
                StockQuantity = product.StockQuantity,
                ExistingPhotos = product.Photos.Select(photo => new ProductPhotoViewModel
                {
                    Id = photo.Id,
                    FilePath = photo.FilePath
                }).ToList(),
                SelectedCategoryIds = product.Categories.Select(c => c.Id).ToList(),
                AvailableCategories = await context.Categories
                    .Select(c => new SelectListItem { Value = c.Id.ToString(), Text = c.Name })
                    .ToListAsync()
            };

            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditProduct(ProductViewModel productViewModel)
        {

            if (!ModelState.IsValid)
            {
                productViewModel.AvailableCategories = await context.Categories
                    .Select(c => new SelectListItem { Value = c.Id.ToString(), Text = c.Name })
                    .ToListAsync();
                return View(productViewModel);
            }

            var product = await context.Products
                .Include(p => p.Categories)
                .Include(p => p.Photos)
                .FirstOrDefaultAsync(p => p.Id == productViewModel.Id);

            if (product == null)
            {
                return NotFound("Product not found.");
            }

            product.Name = productViewModel.Name;
            product.Description = productViewModel.Description;
            product.Price = productViewModel.Price;
            product.StockQuantity = productViewModel.StockQuantity;

            var selectedCategories = await context.Categories
                .Where(c => productViewModel.SelectedCategoryIds.Contains(c.Id))
                .ToListAsync();

            product.Categories.Clear();
            foreach (var category in selectedCategories)
            {
                product.Categories.Add(category);
            }

            if (productViewModel.Photos != null && productViewModel.Photos.Any())
            {
                await SaveProductPhotos(productViewModel.Photos, product);
            }

            context.Products.Update(product);
            await context.SaveChangesAsync();

            return RedirectToAction("ViewProduct", new { productId = productViewModel.Id });
        }

        [HttpGet]
        public async Task<IActionResult> EditProductPhoto(int productId)
        {
            var product = await context.Products
                .Include(p => p.Photos)
                .FirstOrDefaultAsync(p => p.Id == productId);

            if (product == null)
            {
                return NotFound("Product not found.");
            }

            var viewModel = new EditProductPhotoViewModel
            {
                ProductId = productId,
                ExistingPhotos = product.Photos.Select(photo => new ProductPhotoViewModel
                {
                    Id = photo.Id,
                    FilePath = photo.FilePath
                }).ToList()
            };

            return View(viewModel);
        }

        [HttpPost]
        public async Task<IActionResult> EditProductPhoto(int productId, List<int> photoIds)
        {
            var product = await context.Products
                .Include(p => p.Photos)
                .FirstOrDefaultAsync(p => p.Id == productId);

            if (product == null)
            {
                return NotFound("Product not found.");
            }

            foreach (var photoId in photoIds)
            {
                var photo = product.Photos.FirstOrDefault(p => p.Id == photoId);
                if (photo != null)
                {
                    var photoPath = Path.Combine(environment.WebRootPath, photo.FilePath.TrimStart('/'));
                    if (System.IO.File.Exists(photoPath))
                    {
                        System.IO.File.Delete(photoPath);
                    }
                    context.ProductPhotos.Remove(photo);
                }
            }

            await context.SaveChangesAsync();

            return RedirectToAction("EditProduct", new { productId });
        }

        [HttpPost]
        public async Task<IActionResult> DeletePhoto(int productId, int photoId)
        {
            var product = await context.Products
                .Include(p => p.Photos)
                .FirstOrDefaultAsync(p => p.Id == productId);

            if (product == null)
            {
                return NotFound("Product not found.");
            }

            var photo = product.Photos.FirstOrDefault(p => p.Id == photoId);
            if (photo != null)
            {
                var photoPath = Path.Combine(environment.WebRootPath, photo.FilePath.TrimStart('/'));
                if (System.IO.File.Exists(photoPath))
                {
                    System.IO.File.Delete(photoPath);
                }
                context.ProductPhotos.Remove(photo);
            }

            await context.SaveChangesAsync();

            return RedirectToAction("EditProductPhoto", new { productId });
        }

        public async Task<IActionResult> DeleteProduct(int productId)
        {
            var user = await userManager.GetUserAsync(User);
            var seller = await context.Sellers
                .Include(s => s.Stores)
                    .ThenInclude(store => store.Products)
                        .ThenInclude(product => product.Photos)
                .FirstOrDefaultAsync(s => s.UserId == user.Id);

            if (seller == null)
            {
                return Unauthorized("User is not registered as a seller.");
            }

            var product = await context.Products
                .Include(p => p.Photos) 
                .FirstOrDefaultAsync(p => p.Id == productId);

            if (product == null || !seller.Stores.Any(store => store.Id == product.StoreId))
            {
                return NotFound("Product not found or you do not have permission to delete it.");
            }

            if (product.Photos != null && product.Photos.Any())
            {
                foreach (var photo in product.Photos)
                {
                    var photoPath = Path.Combine(environment.WebRootPath, photo.FilePath.TrimStart('/'));
                    if (System.IO.File.Exists(photoPath))
                    {
                        System.IO.File.Delete(photoPath);
                    }
                }
            }

            context.Products.Remove(product);
            await context.SaveChangesAsync();

            return RedirectToAction("StoreDetails", "Store", new { storeId = product.StoreId });
        }

        private async Task PopulateAvailableCategories(ProductViewModel productViewModel)
        {
            productViewModel.AvailableCategories = await context.Categories
                .Select(c => new SelectListItem
                {
                    Value = c.Id.ToString(),
                    Text = c.Name
                }).ToListAsync();

            productViewModel.Categories = productViewModel.AvailableCategories
                .Where(c => productViewModel.SelectedCategoryIds.Contains(int.Parse(c.Value)))
                .Select(c => new CategoryViewModel { Id = int.Parse(c.Value), Name = c.Text })
                .ToList();
        }

        private async Task SaveProductPhotos(IEnumerable<IFormFile> photos, Product product)
        {
            var uploadsDir = Path.Combine(environment.WebRootPath, "product_photos");
            if (!Directory.Exists(uploadsDir))
            {
                Directory.CreateDirectory(uploadsDir);
            }

            foreach (var photo in photos)
            {
                var photoFileName = Guid.NewGuid() + Path.GetExtension(photo.FileName);
                var photoFilePath = Path.Combine(uploadsDir, photoFileName);

                using (var stream = new FileStream(photoFilePath, FileMode.Create))
                {
                    await photo.CopyToAsync(stream);
                }

                product.Photos.Add(new ProductPhoto { FilePath = $"/product_photos/{photoFileName}" });
            }
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View("Error!");
        }
    }
}