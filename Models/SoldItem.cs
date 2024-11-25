using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Market.Models
{
    public class SoldItem
    {
        public int Id { get; set; }
        public string ProductName { get; set; }
        public int QuantitySold { get; set; }
        public decimal TotalEarnings { get; set; }
        public DateTime SoldAt { get; set; } // Добавляем дату продажи
    }
}