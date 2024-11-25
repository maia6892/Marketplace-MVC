using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Market.Models
{
    public class Product
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public decimal Price { get; set; }
        public int StockQuantity { get; set; }
        public int StoreId { get; set; }
        public Store Store { get; set; }
        public Wishlist? Wishlist { get; set; }
        public ICollection<Category> Categories { get; set; } // Связь многие ко многим с категориями
        public ICollection<ProductPhoto> Photos { get; set; }
        public ICollection<Review>? Reviews { get; set; }
    }
}