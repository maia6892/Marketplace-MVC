using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Market.Models;
using Market.Models.ViewModels;
using Market.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;

namespace Market.Controllers;

public class HomeController(MarketDbContext context, UserManager<User> userManager) : Controller
{
    public IActionResult Index()
    {
        var products = context.Products
            .Include(p => p.Photos)
            .Select(p => new ProductViewModel
            {
                Id = p.Id,
                Name = p.Name,
                StoreId = p.StoreId,
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
        ViewBag.Categories = context.Products
            .SelectMany(p => p.Categories)
            .Where(c => c != null)
            .Select(c => c.Name)
            .Distinct()
            .ToList();

        ViewBag.ItemCount = context.Buyers
            .Where(b => b.UserId == User.FindFirstValue(ClaimTypes.NameIdentifier))
            .Include(b => b.ShoppingCart)
            .ThenInclude(c => c.Items)
            .SelectMany(b => b.ShoppingCart.Items)
            .Count();
            

        return View(products);
    }

    public IActionResult About()
    {
        ViewBag.ItemCount = context.Buyers
            .Where(b => b.UserId == User.FindFirstValue(ClaimTypes.NameIdentifier))
            .Include(b => b.ShoppingCart)
            .ThenInclude(c => c.Items)
            .SelectMany(b => b.ShoppingCart.Items)
            .Count();
        
        return View();
    }


    public IActionResult Privacy()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
