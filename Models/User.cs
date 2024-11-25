using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;

namespace Market.Models
{
    public class User : IdentityUser
    {
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public bool IsSeller { get; set; } = false;
        public DateTime DateOfBirth { get; set; }
        public ICollection<ShippingAddress> ShippingAddresses { get; set; }
    }
}