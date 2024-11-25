using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Market.Models.ViewModels
{
    public class StoreViewModel
    {
        public int StoreId { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public IFormFile? Logo { get; set; }
        public string? LogoFilePath { get; set; }
    }
}