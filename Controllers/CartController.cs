using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Security.Claims;
using System.Text.Json;
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
    public class CartController(MarketDbContext context, UserManager<User> userManager) : Controller
    {
        public async Task<IActionResult> Index()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var buyer = await context.Buyers
                .Include(b => b.ShoppingCart)
                    .ThenInclude(cart => cart.Items)
                    .ThenInclude(item => item.Product)
                    .ThenInclude(p => p.Photos)
                .FirstOrDefaultAsync(b => b.UserId == userId);

            ViewBag.ItemCount = context.Buyers
            .Where(b => b.UserId == User.FindFirstValue(ClaimTypes.NameIdentifier))
            .Include(b => b.ShoppingCart)
            .ThenInclude(c => c.Items)
            .SelectMany(b => b.ShoppingCart.Items)
            .Count();


            return View(buyer?.ShoppingCart);
        }

        public async Task<IActionResult> Add(int productId, int quantity = 1)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (userId == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var buyer = await context.Buyers
                .Include(b => b.ShoppingCart)
                    .ThenInclude(cart => cart.Items)
                    .ThenInclude(item => item.Product)
                .FirstOrDefaultAsync(b => b.UserId == userId);

            if (buyer == null)
            {
                return BadRequest("Buyer not found.");
            }

            var shoppingCart = buyer.ShoppingCart ?? new ShoppingCart { BuyerId = buyer.Id };
            if (buyer.ShoppingCart == null)
            {
                context.ShoppingCarts.Add(shoppingCart);
                buyer.ShoppingCart = shoppingCart;
            }

            var existingItem = shoppingCart.Items?.FirstOrDefault(item => item.ProductId == productId);

            if (existingItem != null)
            {
                existingItem.Quantity += quantity;
            }
            else
            {
                var newItem = new ShoppingCartItem
                {
                    ShoppingCart = shoppingCart,
                    ProductId = productId,
                    Quantity = quantity
                };
                context.ShoppingCartItems.Add(newItem);
                shoppingCart.Items.Add(newItem);
            }

            await context.SaveChangesAsync();

            return NoContent();
        }


        public async Task<IActionResult> Delete(int productId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (userId == null)
            {
                return RedirectToAction("Login", "Account");
            }
            var buyer = await context.Buyers
                .Include(b => b.ShoppingCart)
                    .ThenInclude(cart => cart.Items)
                .FirstOrDefaultAsync(b => b.UserId == userId);

            if (buyer == null || buyer.ShoppingCart == null)
            {
                return BadRequest("Корзина не найдена.");
            }

            var cartItem = buyer.ShoppingCart.Items.FirstOrDefault(item => item.ProductId == productId);

            if (cartItem == null)
            {
                return NotFound("Товар не найден в корзине.");
            }

            context.ShoppingCartItems.Remove(cartItem);
            await context.SaveChangesAsync();

            return RedirectToAction("Index", "Cart");
        }

        [HttpPost]
        public async Task<IActionResult> Update(Dictionary<int, int> quantities)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (userId == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var buyer = await context.Buyers
                .Include(b => b.ShoppingCart)
                    .ThenInclude(cart => cart.Items)
                    .ThenInclude(item => item.Product)
                .FirstOrDefaultAsync(b => b.UserId == userId);

            if (buyer == null || buyer.ShoppingCart == null)
            {
                return BadRequest("Корзина не найдена.");
            }

            foreach (var entry in quantities)
            {
                var cartItem = buyer.ShoppingCart.Items.FirstOrDefault(item => item.ProductId == entry.Key);

                if (cartItem != null)
                {
                    cartItem.Quantity = entry.Value;
                }
            }

            var subtotal = buyer.ShoppingCart.Items.Sum(item => item.Product.Price * item.Quantity);
            ViewBag.Subtotal = subtotal;
            ViewBag.Total = subtotal;

            await context.SaveChangesAsync();

            return RedirectToAction("Index", "Cart");
        }

        public async Task<IActionResult> Checkout()
        {
            // Получаем текущего пользователя
            var user = await userManager.GetUserAsync(User);
            var buyer = await context.Buyers
                .Include(b => b.ShoppingCart)
                    .ThenInclude(cart => cart.Items)
                    .ThenInclude(item => item.Product)
                .FirstOrDefaultAsync(b => b.UserId == user.Id);

            if (buyer?.ShoppingCart == null)
            {
                return NotFound("Cart not found.");
            }

            var cart = buyer.ShoppingCart;

            if (!cart.Items.Any())
            {
                return BadRequest("Shopping cart is empty.");
            }

            // Проверка наличия товара на складе и создание заказа
            var order = new Order
            {
                BuyerId = buyer.Id,
                OrderDate = DateTime.UtcNow,
                Status = "Pending",
                TotalAmount = 0m,
                OrderItems = new List<OrderItem>()
            };

            foreach (var item in cart.Items)
            {
                var product = item.Product;

                if (product.StockQuantity < item.Quantity)
                {
                    return RedirectToAction("Cart", "ShoppingCart", new { message = $"Not enough stock for {product.Name}." });
                }

                product.StockQuantity -= item.Quantity;

                if (product.StockQuantity == 0)
                {
                    context.Products.Remove(product);
                }

                var orderItem = new OrderItem
                {
                    ProductId = product.Id,
                    Product = product,
                    Quantity = item.Quantity,
                    UnitPrice = product.Price
                };

                order.OrderItems.Add(orderItem);
                order.TotalAmount += item.Quantity * product.Price;
            }

            await context.Orders.AddAsync(order);

            context.ShoppingCartItems.RemoveRange(cart.Items);

            context.ShoppingCarts.Remove(cart);

            await context.SaveChangesAsync();

            return RedirectToAction("OrderDetails", "Cart", new { id = order.Id });
        }

        public async Task<IActionResult> OrderDetails(int id)
        {
            var user = await userManager.GetUserAsync(User);
            var buyer = await context.Buyers
                .Include(b => b.Orders)
                    .ThenInclude(o => o.OrderItems)
                    .ThenInclude(oi => oi.Product)
                .ThenInclude(p => p.Photos)
                .FirstOrDefaultAsync(b => b.UserId == user.Id);


            var order = await context.Orders
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.Product)
                .ThenInclude(p => p.Photos)
                .FirstOrDefaultAsync(o => o.Id == id && o.BuyerId == buyer.Id);


            if (order == null)
            {
                return NotFound("Order not found.");
            }

            ViewBag.ItemCount = context.Buyers
            .Where(b => b.UserId == User.FindFirstValue(ClaimTypes.NameIdentifier))
            .Include(b => b.ShoppingCart)
            .ThenInclude(c => c.Items)
            .SelectMany(b => b.ShoppingCart.Items)
            .Count();

            return View(order);
        }


        public async Task<IActionResult> AllOrders()
        {
            var user = await userManager.GetUserAsync(User);
            var buyer = await context.Buyers
                .Include(b => b.Orders)
                    .ThenInclude(o => o.OrderItems)
                    .ThenInclude(oi => oi.Product)
                .ThenInclude(p => p.Photos)
                .FirstOrDefaultAsync(b => b.UserId == user.Id);

            if (buyer == null)
            {
                return NotFound("Buyer not found.");
            }

            var orders = await context.Orders
                .Where(o => o.BuyerId == buyer.Id) 
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.Product)
                .ThenInclude(p => p.Photos)
                .ToListAsync(); 

            if (orders == null || !orders.Any())
            {
                return NotFound("No orders found.");
            }

            ViewBag.ItemCount = context.Buyers
            .Where(b => b.UserId == User.FindFirstValue(ClaimTypes.NameIdentifier))
            .Include(b => b.ShoppingCart)
            .ThenInclude(c => c.Items)
            .SelectMany(b => b.ShoppingCart.Items)
            .Count();

            return View(orders); 
        }




        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View("Error!");
        }
    }
}