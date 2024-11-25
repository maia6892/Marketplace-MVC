using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Market.Data;
using Market.Models;
using Market.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Microsoft.Extensions.Logging;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Processing;

namespace Market.Controllers
{
    public class StoreController(MarketDbContext context, UserManager<User> userManager, IWebHostEnvironment environment) : Controller
    {
        public async Task<IActionResult> Index()
        {
            var user = await userManager.GetUserAsync(User);
            var seller = await context.Sellers
                .Include(s => s.Stores)
                .FirstOrDefaultAsync(s => s.UserId == user.Id);

            if (seller == null || seller.Stores == null)
            {
                return View(Enumerable.Empty<Store>());
            }

            ViewBag.ItemCount = context.Buyers
            .Where(b => b.UserId == User.FindFirstValue(ClaimTypes.NameIdentifier))
            .Include(b => b.ShoppingCart)
            .ThenInclude(c => c.Items)
            .SelectMany(b => b.ShoppingCart.Items)
            .Count();
        

            return View(seller.Stores);
        }

        public IActionResult AddStore()
        {
            return View(new StoreViewModel());
        }

        [HttpPost]
        public async Task<IActionResult> AddStore(StoreViewModel storeViewModel)
        {
            if (!ModelState.IsValid)
            {
                foreach (var modelStateError in ModelState.Values.SelectMany(v => v.Errors))
                {
                    Console.WriteLine(modelStateError.ErrorMessage);  
                }

                return View(storeViewModel);
            }
            if (ModelState.IsValid)
            {
                var user = await userManager.GetUserAsync(User);
                var seller = await context.Sellers.FirstOrDefaultAsync(s => s.UserId == user.Id);
                if (seller == null)
                {
                    ModelState.AddModelError("", "User is not registered as a seller.");
                    return View(storeViewModel);
                }

                var store = new Store
                {
                    Name = storeViewModel.Name,
                    Description = storeViewModel.Description,
                    SellerId = seller.Id,
                    Seller = seller,
                    Products = new List<Product>(),
                };

                var uploadsDir = Path.Combine(environment.WebRootPath, "uploads");
                if (!Directory.Exists(uploadsDir))
                {
                    Directory.CreateDirectory(uploadsDir);
                }

                if (storeViewModel.Logo != null)
                {
                    var fileName = Guid.NewGuid() + Path.GetExtension(storeViewModel.Logo.FileName);
                    var filePath = Path.Combine(uploadsDir, fileName);

                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await storeViewModel.Logo.CopyToAsync(stream);
                    }

                    store.LogoFilePath = $"/uploads/{fileName}";
                }

                context.Stores.Add(store);
                await context.SaveChangesAsync();

                return RedirectToAction("Index", "Store");
            }
            return View(storeViewModel);
        }


        public async Task<IActionResult> StoreDetails(int storeId, int page = 1, int pageSize = 6)
        {
            var user = await userManager.GetUserAsync(User);
            if (user == null)
            {
                return View("Error");
            }

            var store = await context.Stores
                .Include(s => s.Products.OrderBy(p => p.Id).Skip((page - 1) * pageSize).Take(pageSize))
                    .ThenInclude(p => p.Photos)
                .Include(s => s.Products)
                    .ThenInclude(p => p.Categories)
                .FirstOrDefaultAsync(s => s.Id == storeId && s.Seller.UserId == user.Id);

            if (store == null)
            {
                return View("Error");
            }

            int totalProducts = await context.Products.CountAsync(p => p.StoreId == storeId);
            var totalPages = (int)Math.Ceiling(totalProducts / (double)pageSize);

            var model = new StoreDetailsViewModel
            {
                StoreId = store.Id,
                StoreName = store.Name,
                StoreDescription = store.Description,
                LogoFilePath = store.LogoFilePath,
                Products = store.Products,
                CurrentPage = page,
                TotalPages = totalPages
            };

            return View(model);
        }


        
        public async Task<IActionResult> EditStore(int storeId)
        {
            var user = await userManager.GetUserAsync(User);
            var seller = await context.Sellers
                .Include(s => s.Stores)
                .FirstOrDefaultAsync(s => s.UserId == user.Id);

            if (seller == null)
            {
                return Unauthorized("User is not registered as a seller.");
            }

            var store = seller.Stores.FirstOrDefault(s => s.Id == storeId);
            if (store == null)
            {
                return NotFound("Store not found.");
            }

            var storeViewModel = new StoreViewModel
            {
                StoreId = store.Id,
                Name = store.Name,
                Description = store.Description,
                LogoFilePath = store.LogoFilePath
            };

            return View(storeViewModel);
        }

        [HttpPost]
        public async Task<IActionResult> EditStore(int storeId, StoreViewModel storeViewModel)
        {
            if (!ModelState.IsValid)
            {
                // Убедитесь, что сохраняется путь к старому логотипу, если форма невалидна
                var storeFromDb = await context.Stores.FindAsync(storeId);
                if (storeFromDb != null)
                {
                    storeViewModel.LogoFilePath = storeFromDb.LogoFilePath;
                }
                return View(storeViewModel);
            }

            var user = await userManager.GetUserAsync(User);
            var seller = await context.Sellers
                .Include(s => s.Stores)
                .FirstOrDefaultAsync(s => s.UserId == user.Id);

            if (seller == null)
            {
                ModelState.AddModelError("", "Пользователь не зарегистрирован как продавец.");
                return View(storeViewModel);
            }

            var store = seller.Stores.FirstOrDefault(s => s.Id == storeId);
            if (store == null)
            {
                ModelState.AddModelError("", "Магазин не найден.");
                return View(storeViewModel);
            }

            store.Name = storeViewModel.Name;
            store.Description = storeViewModel.Description;

            if (storeViewModel.Logo != null)
            {
                var uploadsDir = Path.Combine(environment.WebRootPath, "uploads");
                if (!Directory.Exists(uploadsDir))
                {
                    Directory.CreateDirectory(uploadsDir);
                }

                var fileName = Guid.NewGuid() + Path.GetExtension(storeViewModel.Logo.FileName);
                var filePath = Path.Combine(uploadsDir, fileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await storeViewModel.Logo.CopyToAsync(stream);
                }

                store.LogoFilePath = $"/uploads/{fileName}";
            }
            else
            {
                storeViewModel.LogoFilePath = store.LogoFilePath;
            }

            context.Stores.Update(store);
            await context.SaveChangesAsync();

            return RedirectToAction("Index", "Store");
        }


        public async Task<IActionResult> DeleteStore(int storeId)
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

            var store = seller.Stores.FirstOrDefault(s => s.Id == storeId);

            if (store == null)
            {
                return NotFound("Store not found or you do not have permission to delete it.");
            }

            if (!string.IsNullOrEmpty(store.LogoFilePath))
            {
                var logoPath = Path.Combine(environment.WebRootPath, store.LogoFilePath.TrimStart('/'));
                if (System.IO.File.Exists(logoPath))
                {
                    System.IO.File.Delete(logoPath);
                }
            }

            foreach (var product in store.Products)
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

            context.Products.RemoveRange(store.Products);
            context.Stores.Remove(store);
            await context.SaveChangesAsync();

            return RedirectToAction("Index", "Store");
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View("Error!");
        }
    }
}