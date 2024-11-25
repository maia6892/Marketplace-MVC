using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;
using System.Threading.Tasks;

namespace Market.Models
{
    public class Buyer
    {
        public int Id { get; set; }
        public string UserId { get; set; }
        public User User { get; set; }
        public ICollection<Order> Orders { get; set; }
        public ICollection<Wishlist> Wishlists { get; set; }
        public ShoppingCart ShoppingCart { get; set; }
    }
}