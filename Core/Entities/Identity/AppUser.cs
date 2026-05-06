using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;

namespace Core.Entities.Identity
{
    public class AppUser : IdentityUser
    {
        public string DisplayName { get; set; }
        public Address Address { get; set; }
        public ICollection<Photo> Photos { get; set; } 
        
        // Funcționalități noi pentru Blog și E-commerce
        public ICollection<Post> Posts { get; set; } 
        public ICollection<FavoriteProduct> FavoriteProducts { get; set; }
        public ICollection<Message> MessagesSent { get; set; }
        public ICollection<Message> MessagesReceived { get; set; }
        public bool IsVerified { get; set; } = false; 
        
        public string CompanyName { get; set; } // Pentru facturare/vitrină
        public string DocumentUrl { get; set; } // Aici vom salva calea către PDF/Poza cu actele firmei
    }
}