using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace Market.Models
{
    public class Store
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public int SellerId { get; set; }
        public Seller Seller { get; set; }
        public ICollection<Product> Products { get; set; }
        public string LogoFilePath { get; set; }
    }
}