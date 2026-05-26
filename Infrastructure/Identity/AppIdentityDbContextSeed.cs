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
                {
                    await roleManager.CreateAsync(new IdentityRole(role));
                }
            }

            // Dacă există deja useri, nu mai seed-uim.
            // Dacă ai deja useri @skinet.com în DB și vrei să îi înlocuiești,
            // trebuie să ștergi baza de date / să golești tabelele Identity.
            await SeedSellerProfilesAsync(userManager);
            if (userManager.Users.Any()) return;

            var admin = new AppUser
            {
                DisplayName = "Admin",
                Email = "admin@greenbeauty.com",
                UserName = "admin@greenbeauty.com",
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

            var cosmeticsProducers = new[]
            {
                new AppUser
                {
                    DisplayName = "LuxeSkin Studio",
                    Email = "luxeskin@greenbeauty.com",
                    UserName = "luxeskin@greenbeauty.com",
                    IsVerified = true,
                    CompanyName = "LuxeSkin Studio SRL",
                    Address = new Address
                    {
                        FirstName = "LuxeSkin",
                        LastName = "Studio",
                        Street = "LuxeSkin Street",
                        City = "New York",
                        State = "NY",
                        Zipcode = "15648"
                    }
                },
                new AppUser
                {
                    DisplayName = "PureGlow Lab",
                    Email = "pureglow@greenbeauty.com",
                    UserName = "pureglow@greenbeauty.com",
                    IsVerified = true,
                    CompanyName = "PureGlow Lab SRL",
                    Address = new Address
                    {
                        FirstName = "PureGlow",
                        LastName = "Lab",
                        Street = "PureGlow Street",
                        City = "New York",
                        State = "NY",
                        Zipcode = "30154"
                    }
                },
                new AppUser
                {
                    DisplayName = "BotanicaBeauty",
                    Email = "botanica@greenbeauty.com",
                    UserName = "botanica@greenbeauty.com",
                    IsVerified = true,
                    CompanyName = "BotanicaBeauty SRL",
                    Address = new Address
                    {
                        FirstName = "Botanica",
                        LastName = "Beauty",
                        Street = "BotanicaBeauty Street",
                        City = "New York",
                        State = "NY",
                        Zipcode = "48752"
                    }
                }
            };

            foreach (var producer in cosmeticsProducers)
            {
                await userManager.CreateAsync(producer, "Producer@123");
                await userManager.AddToRoleAsync(producer, "CosmeticsProducer");
            }

            var ingredientProducers = new[]
            {
                new AppUser
                {
                    DisplayName = "NatureSource",
                    Email = "naturesource@greenbeauty.com",
                    UserName = "naturesource@greenbeauty.com",
                    IsVerified = true,
                    CompanyName = "NatureSource SRL",
                    Address = new Address
                    {
                        FirstName = "Nature",
                        LastName = "Source",
                        Street = "NatureSource Street",
                        City = "New York",
                        State = "NY",
                        Zipcode = "15601"
                    }
                },
                new AppUser
                {
                    DisplayName = "RawEssentials",
                    Email = "rawessentials@greenbeauty.com",
                    UserName = "rawessentials@greenbeauty.com",
                    IsVerified = true,
                    CompanyName = "RawEssentials SRL",
                    Address = new Address
                    {
                        FirstName = "Raw",
                        LastName = "Essentials",
                        Street = "RawEssentials Street",
                        City = "New York",
                        State = "NY",
                        Zipcode = "55401"
                    }
                },
                new AppUser
                {
                    DisplayName = "HerbalRoots",
                    Email = "herbalroots@greenbeauty.com",
                    UserName = "herbalroots@greenbeauty.com",
                    IsVerified = true,
                    CompanyName = "HerbalRoots SRL",
                    Address = new Address
                    {
                        FirstName = "Herbal",
                        LastName = "Roots",
                        Street = "HerbalRoots Blvd",
                        City = "New York",
                        State = "NY",
                        Zipcode = "41001"
                    }
                }
            };

            foreach (var producer in ingredientProducers)
            {
                await userManager.CreateAsync(producer, "Producer@123");
                await userManager.AddToRoleAsync(producer, "IngredientsProducer");
            }

            var buyers = new[]
            {
                new AppUser
                {
                    DisplayName = "Alice",
                    Email = "alice@greenbeauty.com",
                    UserName = "alice@greenbeauty.com",
                    IsVerified = true,
                    Address = new Address
                    {
                        FirstName = "Alice",
                        LastName = "Johnson",
                        Street = "14 Rose Street",
                        City = "New York",
                        State = "NY",
                        Zipcode = "010001"
                    }
                },
                new AppUser
                {
                    DisplayName = "Maria",
                    Email = "maria@greenbeauty.com",
                    UserName = "maria@greenbeauty.com",
                    IsVerified = true,
                    Address = new Address
                    {
                        FirstName = "Maria",
                        LastName = "Popescu",
                        Street = "2 Tulips Street",
                        City = "New York",
                        State = "NY",
                        Zipcode = "400010"
                    }
                },
                new AppUser
                {
                    DisplayName = "Elena",
                    Email = "elena@greenbeauty.com",
                    UserName = "elena@greenbeauty.com",
                    IsVerified = true,
                    Address = new Address
                    {
                        FirstName = "Elena",
                        LastName = "Ionescu",
                        Street = "New Street",
                        City = "New York",
                        State = "NY",
                        Zipcode = "900001"
                    }
                }
            };

            foreach (var buyer in buyers)
            {
                await userManager.CreateAsync(buyer, "Buyer@123");
                await userManager.AddToRoleAsync(buyer, "Buyer");
            }

            var bloggers = new[]
            {
                new AppUser
                {
                    DisplayName = "SkincareDiary",
                    Email = "skincarediary@greenbeauty.com",
                    UserName = "skincarediary@greenbeauty.com",
                    IsVerified = true,
                    Address = new Address
                    {
                        FirstName = "Skincare",
                        LastName = "Diary",
                        Street = "1 Beauty Blvd",
                        City = "New York",
                        State = "NY",
                        Zipcode = "02001"
                    }
                },
                new AppUser
                {
                    DisplayName = "GreenBeautyBlog",
                    Email = "greenbeautyblog@greenbeauty.com",
                    UserName = "greenbeautyblog@greenbeauty.com",
                    IsVerified = true,
                    Address = new Address
                    {
                        FirstName = "Green",
                        LastName = "Beauty",
                        Street = "9 Eco Street",
                        City = "New York",
                        State = "NY",
                        Zipcode = "40020"
                    }
                },
                new AppUser
                {
                    DisplayName = "NaturalGlowBlog",
                    Email = "naturalglow@greenbeauty.com",
                    UserName = "naturalglow@greenbeauty.com",
                    IsVerified = true,
                    Address = new Address
                    {
                        FirstName = "Natural",
                        LastName = "Glow",
                        Street = "NaturalGlow Street",
                        City = "New York",
                        State = "NY",
                        Zipcode = "30010"
                    }
                }
            };

            foreach (var blogger in bloggers)
            {
                await userManager.CreateAsync(blogger, "Blogger@123");
                await userManager.AddToRoleAsync(blogger, "Blogger");
            }

            await SeedSellerProfilesAsync(userManager);
        }

        private static async Task SeedSellerProfilesAsync(UserManager<AppUser> userManager)
        {
            var profiles = new Dictionary<string, (string Description, string Story, string History, string Location, string MapUrl)>
            {
                ["luxeskin@greenbeauty.com"] = (
                    "Fresh cosmetics studio with clean formulas for simple and effective routines.",
                    "LuxeSkin Studio works in small batches, focusing on lightweight textures, botanical ingredients and products tested in daily routines.",
                    "The brand started as a small local lab and grew through collaborations with natural suppliers and customers looking for gentle care.",
                    "Bucharest, Romania",
                    "https://www.google.com/maps?q=Bucharest,Romania&output=embed"),
                ["pureglow@greenbeauty.com"] = (
                    "Artisanal cosmetics lab focused on hydration, calming care and formulas for sensitive skin.",
                    "PureGlow Lab creates products in controlled batches, with a focus on stable ingredients and pleasant textures.",
                    "The team started with care formulas for family use and turned that practice into a small lab brand.",
                    "Cluj-Napoca, Romania",
                    "https://www.google.com/maps?q=Cluj-Napoca,Romania&output=embed"),
                ["botanica@greenbeauty.com"] = (
                    "Skincare studio inspired by plant extracts, fine creams and gentle rituals.",
                    "BotanicaBeauty combines plants, oils and modern textures into products that fit easily into daily routines.",
                    "The brand was built around botanical recipes and collaboration with small local producers.",
                    "Brasov, Romania",
                    "https://www.google.com/maps?q=Brasov,Romania&output=embed"),
                ["naturesource@greenbeauty.com"] = (
                    "Supplier of fresh botanical ingredients for handmade cosmetics and studio formulas.",
                    "NatureSource selects plants, leaves and extracts from small farms, with traceability and carefully checked batches.",
                    "The farm started with aromatic crops and expanded into ingredients for natural cosmetics.",
                    "Sibiu, Romania",
                    "https://www.google.com/maps?q=Sibiu,Romania&output=embed"),
                ["rawessentials@greenbeauty.com"] = (
                    "Supplier of butters, bases and raw materials for balms, creams and body products.",
                    "RawEssentials works with carefully stored raw ingredients for producers who want consistent formulas.",
                    "The company grew from a small raw-material warehouse into a specialized supplier for handmade cosmetics.",
                    "Timisoara, Romania",
                    "https://www.google.com/maps?q=Timisoara,Romania&output=embed"),
                ["herbalroots@greenbeauty.com"] = (
                    "Farm for dried plants, botanical flowers and aromatic ingredients used in natural cosmetic recipes.",
                    "HerbalRoots grows and dries plants in small batches, preserving color, aroma and botanical properties.",
                    "Their story began with a family garden and became a farm dedicated to clean ingredients.",
                    "Iasi, Romania",
                    "https://www.google.com/maps?q=Iasi,Romania&output=embed")
            };

            foreach (var profile in profiles)
            {
                var user = await userManager.FindByEmailAsync(profile.Key);
                if (user == null || !string.IsNullOrWhiteSpace(user.SellerDescription)) continue;

                user.SellerDescription = profile.Value.Description;
                user.SellerStory = profile.Value.Story;
                user.SellerHistory = profile.Value.History;
                user.SellerLocation = profile.Value.Location;
                user.SellerMapUrl = profile.Value.MapUrl;

                await userManager.UpdateAsync(user);
            }
        }
    }
}
