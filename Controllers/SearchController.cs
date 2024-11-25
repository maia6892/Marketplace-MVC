using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Market.Data;
using Market.Models;
using Market.Models.ViewModels;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Market.Controllers
{
    public class SearchController(MarketDbContext context, UserManager<User> userManager, IWebHostEnvironment environment) : Controller
    {
        public IActionResult Filter(List<string> storeNames, List<string> categoryNames, decimal? minPrice, decimal? maxPrice)
        {
            var productsQuery = context.Products
                .Include(p => p.Photos)
                .Include(p => p.Categories)
                .Include(p => p.Store)
                .AsQueryable();

            if (storeNames != null && storeNames.Any())
            {
                productsQuery = productsQuery.Where(p => storeNames.Contains(p.Store.Name));
            }

            if (categoryNames != null && categoryNames.Any())
            {
                productsQuery = productsQuery.Where(p => p.Categories.Any(c => categoryNames.Contains(c.Name)));
            }

            if (minPrice.HasValue)
            {
                productsQuery = productsQuery.Where(p => p.Price >= minPrice.Value);
            }
            if (maxPrice.HasValue)
            {
                productsQuery = productsQuery.Where(p => p.Price <= maxPrice.Value);
            }

            var products = productsQuery.Select(p => new ProductViewModel
            {
                Id = p.Id,
                Name = p.Name,
                StoreId = p.StoreId,
                StoreName = p.Store.Name,
                Description = p.Description,
                StockQuantity = p.StockQuantity,
                Price = p.Price,
                Categories = p.Categories.Select(c => new CategoryViewModel
                {
                    Id = c.Id,
                    Name = c.Name
                }).ToList(),
                ExistingPhotos = p.Photos.Select(photo => new ProductPhotoViewModel
                {
                    Id = photo.Id,
                    FilePath = photo.FilePath
                }).ToList()
            }).ToList();

            return PartialView("_ProductListPartial", products);
        }

        public IActionResult Search(string keyword)
        {
            var productsQuery = context.Products
                .Include(p => p.Photos)
                .Include(p => p.Categories)
                .Include(p => p.Store)
                .AsQueryable();

            if (!string.IsNullOrEmpty(keyword))
            {
                productsQuery = productsQuery.Where(p => p.Name.Contains(keyword));
            }

            var products = productsQuery.Select(p => new ProductViewModel
            {
                Id = p.Id,
                Name = p.Name,
                StoreId = p.StoreId,
                StoreName = p.Store.Name,
                Description = p.Description,
                StockQuantity = p.StockQuantity,
                Price = p.Price,
                Categories = p.Categories.Select(c => new CategoryViewModel
                {
                    Id = c.Id,
                    Name = c.Name
                }).ToList(),
                ExistingPhotos = p.Photos.Select(photo => new ProductPhotoViewModel
                {
                    Id = photo.Id,
                    FilePath = photo.FilePath
                }).ToList()
            }).ToList();

            return PartialView("_ProductListPartial", products);
        }

        public IActionResult SearchView()
        {
            var products = context.Products
            .Include(p => p.Photos)
            .Select(p => new ProductViewModel
            {
                Id = p.Id,
                Name = p.Name,
                StoreId = p.StoreId,
                StoreName = p.Store.Name,
                Description = p.Description,
                StockQuantity = p.StockQuantity,
                Price = p.Price,
                Categories = p.Categories.Select(c => new CategoryViewModel
                {
                    Id = c.Id,
                    Name = c.Name
                }).ToList(),
                ExistingPhotos = p.Photos.Select(photo => new ProductPhotoViewModel
                {
                    Id = photo.Id,
                    FilePath = photo.FilePath
                }).ToList()
            })
            .ToList();

            ViewBag.ItemCount = context.Buyers
            .Where(b => b.UserId == User.FindFirstValue(ClaimTypes.NameIdentifier))
            .Include(b => b.ShoppingCart)
            .ThenInclude(c => c.Items)
            .SelectMany(b => b.ShoppingCart.Items)
            .Count();

            return View(products);
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View("Error!");
        }
    }
}