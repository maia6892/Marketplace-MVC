using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Market.Models.ViewModels
{
    public class SoldItemViewModel
    {
        public string ProductName { get; set; }
        public int QuantitySold { get; set; }
        public decimal TotalEarnings { get; set; }
    }
}