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
                    "Atelier de cosmetice proaspete, cu formule curate pentru rutine simple si eficiente.",
                    "LuxeSkin Studio lucreaza in loturi mici, cu accent pe texturi usoare, ingrediente botanice si produse testate in rutina zilnica.",
                    "Brandul a pornit ca un mic laborator local si s-a dezvoltat prin colaborari cu furnizori naturali si clienti care cauta ingrijire blanda.",
                    "Bucuresti, Romania",
                    "https://www.google.com/maps?q=Bucharest,Romania&output=embed"),
                ["pureglow@greenbeauty.com"] = (
                    "Laborator de cosmetice artizanale axat pe hidratare, calmare si formule pentru piele sensibila.",
                    "PureGlow Lab creeaza produse in serii controlate, cu focus pe ingrediente stabile si senzorial placut.",
                    "Echipa a inceput cu formule de ingrijire pentru familie si a transformat practica intr-un mic brand de laborator.",
                    "Cluj-Napoca, Romania",
                    "https://www.google.com/maps?q=Cluj-Napoca,Romania&output=embed"),
                ["botanica@greenbeauty.com"] = (
                    "Studio de skincare inspirat de extracte vegetale, creme fine si ritualuri blande.",
                    "BotanicaBeauty combina plante, uleiuri si texturi moderne pentru produse usor de integrat in rutina de zi cu zi.",
                    "Brandul s-a construit in jurul retetelor botanice si al colaborarii cu mici producatori locali.",
                    "Brasov, Romania",
                    "https://www.google.com/maps?q=Brasov,Romania&output=embed"),
                ["naturesource@greenbeauty.com"] = (
                    "Furnizor de ingrediente botanice proaspete pentru cosmetice handmade si formule de atelier.",
                    "NatureSource selecteaza plante, frunze si extracte din ferme mici, cu trasabilitate si loturi atent verificate.",
                    "Ferma a pornit cu culturi aromatice si s-a extins catre ingrediente pentru cosmetica naturala.",
                    "Sibiu, Romania",
                    "https://www.google.com/maps?q=Sibiu,Romania&output=embed"),
                ["rawessentials@greenbeauty.com"] = (
                    "Furnizor de unturi, baze si materii prime pentru balsamuri, creme si produse de corp.",
                    "RawEssentials lucreaza cu ingrediente brute, atent pastrate, pentru producatori care vor formule consistente.",
                    "Compania a crescut dintr-un mic depozit de materii prime catre un furnizor specializat pentru cosmetice handmade.",
                    "Timisoara, Romania",
                    "https://www.google.com/maps?q=Timisoara,Romania&output=embed"),
                ["herbalroots@greenbeauty.com"] = (
                    "Ferma de plante uscate, flori botanice si ingrediente aromatice pentru retete cosmetice naturale.",
                    "HerbalRoots cultiva si usuca plante in loturi mici, pastrand culoarea, aroma si proprietatile botanice.",
                    "Povestea lor a inceput cu o gradina de familie si s-a transformat intr-o ferma dedicata ingredientelor curate.",
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
