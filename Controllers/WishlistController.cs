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
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Market.Controllers
{
    [Authorize]
    public class WishlistController(MarketDbContext context, UserManager<User> userManager, IWebHostEnvironment environment) : Controller
    {
        public async Task<IActionResult> Index()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var buyer = await context.Buyers
                .Include(b => b.Wishlists)
                .ThenInclude(w => w.Product)
                .ThenInclude(p => p.Photos)
                .FirstOrDefaultAsync(b => b.UserId == userId);

            if (buyer == null)
            {
                return NotFound("User not found.");
            }

            ViewBag.ItemCount = context.Buyers
            .Where(b => b.UserId == User.FindFirstValue(ClaimTypes.NameIdentifier))
            .Include(b => b.ShoppingCart)
            .ThenInclude(c => c.Items)
            .SelectMany(b => b.ShoppingCart.Items)
            .Count();
        
            return View(buyer.Wishlists);
        }

        public async Task<IActionResult> Add(int productId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (userId == null)
            {
                return View("Login", "Account");
            }

            var buyer = await context.Buyers
                .Include(b => b.Wishlists)
                .FirstOrDefaultAsync(b => b.UserId == userId);

            if (buyer == null)
            {
                return BadRequest("User not found.");
            }

            var product = await context.Products.FindAsync(productId);
            if (product == null)
            {
                return NotFound("Product not found.");
            }

            if (buyer.Wishlists.Any(w => w.ProductId == productId))
            {
                return BadRequest("Product already in wishlist.");
            }

            var wishlistItem = new Wishlist
            {
                BuyerId = buyer.Id,
                ProductId = productId
            };

            context.Wishlists.Add(wishlistItem);
            await context.SaveChangesAsync();

            return NoContent();
        }

        [HttpGet]
        public async Task<IActionResult> Delete(int productId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (userId == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var buyer = await context.Buyers
                .Include(b => b.Wishlists)
                .ThenInclude(w => w.Product)
                .FirstOrDefaultAsync(b => b.UserId == userId);

            if (buyer == null)
            {
                return BadRequest("Покупатель не найден.");
            }

            var wishlistItem = buyer.Wishlists.FirstOrDefault(w => w.ProductId == productId);
            if (wishlistItem == null)
            {
                return NotFound("Продукт не найден в списке желаемого.");
            }

            context.Wishlists.Remove(wishlistItem);
            await context.SaveChangesAsync();

            return RedirectToAction("Index");
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View("Error!");
        }
    }
}