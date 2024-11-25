using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Market.Models
{
    public class Wishlist
    {
        public int Id { get; set; }
        public int BuyerId { get; set; }
        public Buyer Buyer { get; set; }
        public int ProductId { get; set; }
        public Product Product { get; set; }
    }
}