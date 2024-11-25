using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Market.Models.ViewModels
{
    public class CheckoutViewModel
    {
        public List<ShoppingCartItem> CartItems { get; set; }
        public decimal Total { get; set; }
    }
}