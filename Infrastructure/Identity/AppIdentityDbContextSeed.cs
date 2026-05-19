using Core.Entities.Identity;
using Microsoft.AspNetCore.Identity;

namespace Infrastructure.Identity
{
    public class AppIdentityDbContextSeed
    {
        public static async Task SeedUsersAsync(
            UserManager<AppUser> userManager,
            RoleManager<IdentityRole> roleManager)
        {
            // ── Roles ────────────────────────────────────────────────────────
            var roles = new[]
            {
                "Admin",
                "CosmeticsProducer",
                "IngredientsProducer",
                "Buyer",
                "Blogger"
            };

            foreach (var role in roles)
            {
                if (!await roleManager.RoleExistsAsync(role))
                    await roleManager.CreateAsync(new IdentityRole(role));
            }

            // ── Skip if users already exist ──────────────────────────────────
            if (userManager.Users.Any()) return;

            // ── Admin ────────────────────────────────────────────────────────
            var admin = new AppUser
            {
                DisplayName = "Admin",
                Email = "admin@skinet.com",
                UserName = "admin@skinet.com",
                IsVerified = true,
                Address = new Address
                {
                    FirstName = "Site",
                    LastName = "Admin",
                    Street = "1 Admin Street",
                    City = "New York",
                    State = "NY",
                    Zipcode = "15795"
                }
            };
            await userManager.CreateAsync(admin, "Admin@123");
            await userManager.AddToRoleAsync(admin, "Admin");

            // ── Cosmetics Producers (3) ───────────────────────────────────────
            var cosmeticsProducers = new[]
            {
                new AppUser
                {
                    DisplayName = "LuxeSkin Studio",
                    Email       = "luxeskin@skinet.com",
                    UserName    = "luxeskin@skinet.com",
                    IsVerified  = true,
                    CompanyName = "LuxeSkin Studio SRL",
                    Address = new Address
                    {
                        FirstName = "LuxeSkin",
                        LastName  = "Studio",
                        Street    = "LuxeSkin Street",
                        City      = "New York",
                        State     = "NY",
                        Zipcode   = "15648"
                    }
                },
                new AppUser
                {
                    DisplayName = "PureGlow Lab",
                    Email       = "pureglow@skinet.com",
                    UserName    = "pureglow@skinet.com",
                    IsVerified  = true,
                    CompanyName = "PureGlow Lab SRL",
                    Address = new Address
                    {
                        FirstName = "PureGlow",
                        LastName  = "Lab",
                        Street    = "PureGlow Street",
                        City      = "New York",
                        State     = "NY",
                        Zipcode   = "30154"
                    }
                },
                new AppUser
                {
                    DisplayName = "BotanicaBeauty",
                    Email       = "botanica@skinet.com",
                    UserName    = "botanica@skinet.com",
                    IsVerified  = true,
                    CompanyName = "BotanicaBeauty SRL",
                    Address = new Address
                    {
                        FirstName = "Botanica",
                        LastName  = "Beauty",
                        Street    = "BotanicaBeauty Street",
                        City      = "New York",
                        State     = "NY",
                        Zipcode   = "48752"
                    }
                }
            };

            foreach (var producer in cosmeticsProducers)
            {
                await userManager.CreateAsync(producer, "Producer@123");
                await userManager.AddToRoleAsync(producer, "CosmeticsProducer");
            }

            // ── Ingredients Producers (3) ─────────────────────────────────────
            var ingredientProducers = new[]
            {
                new AppUser
                {
                    DisplayName = "NatureSource",
                    Email       = "naturesource@skinet.com",
                    UserName    = "naturesource@skinet.com",
                    IsVerified  = true,
                    CompanyName = "NatureSource SRL",
                    Address = new Address
                    {
                        FirstName = "Nature",
                        LastName  = "Source",
                        Street    = "NatureSource Street",
                        City      = "New York",
                        State     = "NY",
                        Zipcode   = "15601"
                    }
                },
                new AppUser
                {
                    DisplayName = "RawEssentials",
                    Email       = "rawessentials@skinet.com",
                    UserName    = "rawessentials@skinet.com",
                    IsVerified  = true,
                    CompanyName = "RawEssentials SRL",
                    Address = new Address
                    {
                        FirstName = "Raw",
                        LastName  = "Essentials",
                        Street    = "RawEssentials Street",
                        City      = "New York",
                        State     = "NY",
                        Zipcode   = "55401"
                    }
                },
                new AppUser
                {
                    DisplayName = "HerbalRoots",
                    Email       = "herbalroots@skinet.com",
                    UserName    = "herbalroots@skinet.com",
                    IsVerified  = true,
                    CompanyName = "HerbalRoots SRL",
                    Address = new Address
                    {
                        FirstName = "Herbal",
                        LastName  = "Roots",
                        Street    = "HerbalRoots Blvd",
                        City      = "New York",
                        State     = "NY",
                        Zipcode   = "41001"
                    }
                }
            };

            foreach (var producer in ingredientProducers)
            {
                await userManager.CreateAsync(producer, "Producer@123");
                await userManager.AddToRoleAsync(producer, "IngredientsProducer");
            }

            // ── Buyers (3) ───────────────────────────────────────────────────
            var buyers = new[]
            {
                new AppUser
                {
                    DisplayName = "Alice",
                    Email       = "alice@skinet.com",
                    UserName    = "alice@skinet.com",
                    IsVerified  = true,
                    Address = new Address
                    {
                        FirstName = "Alice",
                        LastName  = "Johnson",
                        Street    = "14 Rose Street",
                          City      = "New York",
                        State     = "NY",
                        Zipcode   = "010001"
                    }
                },
                new AppUser
                {
                    DisplayName = "Maria",
                    Email       = "maria@skinet.com",
                    UserName    = "maria@skinet.com",
                    IsVerified  = true,
                    Address = new Address
                    {
                        FirstName = "Maria",
                        LastName  = "Popescu",
                        Street    = "2 Tulips Street",
                         City      = "New York",
                        State     = "NY",
                        Zipcode   = "400010"
                    }
                },
                new AppUser
                {
                    DisplayName = "Elena",
                    Email       = "elena@skinet.com",
                    UserName    = "elena@skinet.com",
                    IsVerified  = true,
                    Address = new Address
                    {
                        FirstName = "Elena",
                        LastName  = "Ionescu",
                        Street    = "New Street",
                          City      = "New York",
                        State     = "NY",
                        Zipcode   = "900001"
                    }
                }
            };

            foreach (var buyer in buyers)
            {
                await userManager.CreateAsync(buyer, "Buyer@123");
                await userManager.AddToRoleAsync(buyer, "Buyer");
            }

            // ── Bloggers (3) ─────────────────────────────────────────────────
            var bloggers = new[]
            {
                new AppUser
                {
                    DisplayName = "SkincareDiary",
                    Email       = "skincarediary@skinet.com",
                    UserName    = "skincarediary@skinet.com",
                    IsVerified  = true,
                    Address = new Address
                    {
                        FirstName = "Skincare",
                        LastName  = "Diary",
                        Street    = "1 Beauty Blvd",
                         City      = "New York",
                        State     = "NY",
                        Zipcode   = "02001"
                    }
                },
                new AppUser
                {
                    DisplayName = "GreenBeautyBlog",
                    Email       = "greenbeauty@skinet.com",
                    UserName    = "greenbeauty@skinet.com",
                    IsVerified  = true,
                    Address = new Address
                    {
                        FirstName = "Green",
                        LastName  = "Beauty",
                        Street    = "9 Eco Street",
                          City      = "New York",
                        State     = "NY",
                        Zipcode   = "40020"
                    }
                },
                new AppUser
                {
                    DisplayName = "NaturalGlowBlog",
                    Email       = "naturalglow@skinet.com",
                    UserName    = "naturalglow@skinet.com",
                    IsVerified  = true,
                    Address = new Address
                    {
                        FirstName = "Natural",
                        LastName  = "Glow",
                        Street    = "NaturalGlow Street",
                          City      = "New York",
                        State     = "NY",
                        Zipcode   = "30010"
                    }
                }
            };

            foreach (var blogger in bloggers)
            {
                await userManager.CreateAsync(blogger, "Blogger@123");
                await userManager.AddToRoleAsync(blogger, "Blogger");
            }
        }
    }
}