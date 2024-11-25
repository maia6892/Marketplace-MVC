using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Market.Models.ViewModels
{
    public class CartViewModel
    {
        public int Id { get; set; }
        public int BuyerId { get; set; }
        public Buyer Buyer { get; set; }
        public ICollection<ShoppingCartItem> Items { get; set; }
    }
}