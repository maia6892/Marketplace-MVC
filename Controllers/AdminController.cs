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
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Market.Controllers
{
    public class AdminController(MarketDbContext context, UserManager<User> userManager) : Controller
    {
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> CheckoutSummary()
        {
            var soldItems = await context.SoldItems.ToListAsync();

            var viewModel = new CheckoutViewModel
            {
                CartItems = soldItems.Select(s => new ShoppingCartItem
                {
                    Product = new Product { Name = s.ProductName, Price = s.TotalEarnings / s.QuantitySold },
                    Quantity = s.QuantitySold
                }).ToList(),
                Total = soldItems.Sum(s => s.TotalEarnings)
            };

            return View(viewModel);
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View("Error!");
        }
    }
}