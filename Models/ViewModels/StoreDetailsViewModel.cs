using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Market.Models.ViewModels
{
    public class StoreDetailsViewModel
    {
        public int StoreId { get; set; }
        public string StoreName { get; set; }
        public string StoreDescription { get; set; }
        public string LogoFilePath { get; set; }
        public IEnumerable<Product>? Products { get; set; }
        public int CurrentPage { get; set; }
        public int TotalPages { get; set; }
    }
}