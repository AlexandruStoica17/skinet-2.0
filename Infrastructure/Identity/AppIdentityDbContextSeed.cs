using Core.Entities.Identity;
using Microsoft.AspNetCore.Identity;

namespace Infrastructure.Identity
{
    public class AppIdentityDbContextSeed
    {
        // COD VECHI COMENTAT
        // public static async Task SeedUSersAsync(UserManager<AppUser> userManager)
        
        // COD NOU
        public static async Task SeedUsersAsync(UserManager<AppUser> userManager, RoleManager<IdentityRole> roleManager)
        {
            // 1. COD NOU: Definim și creăm rolurile necesare platformei
            var roles = new List<IdentityRole>
            {
                new IdentityRole { Name = "Admin" },
                new IdentityRole { Name = "CosmeticsProducer" },
                new IdentityRole { Name = "IngredientsProducer" },
                new IdentityRole { Name = "Buyer" },
                new IdentityRole { Name = "Blogger" }
            };

            foreach (var role in roles)
            {
                if (!await roleManager.RoleExistsAsync(role.Name))
                {
                    await roleManager.CreateAsync(role);
                }
            }

            if (!userManager.Users.Any())
            {
                /* --- COD VECHI COMENTAT PENTRU BOB ---
                var user = new AppUser
                {
                     DisplayName = "Bob",
                    Email = "bob@test.com",
                    UserName = "bob@test.com",
                    Address = new Address
                    {
                        FirstName = "Bob",
                        LastName = "Bobbity",
                        Street = "10 The Street",
                        City = "New York",
                        State = "NY",
                        Zipcode = "90210"
                    } 
                };
                 await userManager.CreateAsync(user, "Pa$$w0rd");
                ---------------------------------------- */

                // 2. COD NOU: Creăm Adminul
                var admin = new AppUser
                {
                    DisplayName = "Super Admin",
                    Email = "admin@test.com",
                    UserName = "admin@test.com",
                    IsVerified = true, // Contul adminului e mereu verificat
                    Address = new Address
                    {
                        FirstName = "Super",
                        LastName = "Admin",
                        Street = "Strada Adminilor 1",
                        City = "București",
                        State = "B",
                        Zipcode = "123456" // Observă că la tine e Zipcode, nu ZipCode. Am păstrat cum era.
                    }
                };

                await userManager.CreateAsync(admin, "Pa$$w0rd");
                await userManager.AddToRoleAsync(admin, "Admin"); // Îi dăm rolul!

                // 3. COD NOU (BOB MODIFICAT): Recreăm utilizatorul Bob, dar îi dăm rol și status
                var bob = new AppUser
                {
                    DisplayName = "Bob",
                    Email = "bob@test.com",
                    UserName = "bob@test.com",
                    IsVerified = true, // Bob e cumpărător, nu are nevoie de aprobarea firmei
                    Address = new Address
                    {
                        FirstName = "Bob",
                        LastName = "Bobbity",
                        Street = "10 The Street",
                        City = "New York",
                        State = "NY",
                        Zipcode = "90210"
                    }
                };

                await userManager.CreateAsync(bob, "Pa$$w0rd");
                await userManager.AddToRoleAsync(bob, "Buyer"); // Bob e doar cumpărător
            }
        }
    }
}