using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Market.Models;
using Microsoft.EntityFrameworkCore;

namespace Market.Data
{
    public static class DbInitializer
    {
        public static void Seed(IServiceProvider serviceProvider)
        {
            using (var context = new MarketDbContext(serviceProvider.GetRequiredService<DbContextOptions<MarketDbContext>>()))
            {
                if (!context.Categories.Any())
                {
                    context.Categories.AddRange(
                        new Category { Name = "Electronics" },
                        new Category { Name = "Books" },
                        new Category { Name = "Clothing" },
                        new Category { Name = "Toys" },
                        new Category { Name = "Home & Garden" },
                        new Category { Name = "Sports" },
                        new Category { Name = "Other" }
                    );

                    context.SaveChanges();
                }
            }
        }
    }
}