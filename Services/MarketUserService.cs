using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Market.Data;
using Market.Models;
using Microsoft.AspNetCore.Identity;

namespace Market.Services
{
    public class MarketUserService(MarketDbContext marketDbContext, UserManager<User> userManager)
    {
        public async Task AddUserToMarketDbContextAsync(User user)
        {
            if (await IsUserInRoleAsync(user, "Seller"))
            {
                var seller = new Seller
                {
                    UserId = user.Id,
                    User = user
                };
                marketDbContext.Sellers.Add(seller);
            }
            else if (await IsUserInRoleAsync(user, "Buyer"))
            {
                var buyer = new Buyer
                {
                    UserId = user.Id,
                    User = user
                };
                marketDbContext.Buyers.Add(buyer);
            }

            await marketDbContext.SaveChangesAsync();
        }

        private async Task<bool> IsUserInRoleAsync(User user, string role)
        {
            return await userManager.IsInRoleAsync(user, role);
        }
    }
}