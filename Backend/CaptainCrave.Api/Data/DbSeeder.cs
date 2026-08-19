using Api.Models;
using Api.Models.Enums;
using Microsoft.EntityFrameworkCore;

namespace Api.Data;

// Seeder til testdata: fylder databasen med et par kendte fastfood-kæder (restaurant,
// menu, kategorier og retter), så teamet slipper for selv at oprette data manuelt.
// Kører kun én gang — springes helt over hvis der allerede findes en restaurant.
public static class DbSeeder
{
    // Kald denne fra Program.cs efter migrations er kørt.
    public static async Task SeedAsync(AppDbContext db)
    {
        if (await db.Restaurants.AnyAsync())
            return;

        db.Restaurants.AddRange(
            BuildBurgerKing(),
            BuildMcDonalds(),
            BuildKfc(),
            BuildSubway(),
            BuildDominos(),
            BuildPizzaHut());
        await db.SaveChangesAsync();
    }

    private static Restaurant BuildBurgerKing()
    {
        var menu = new Menu { Name = "Menukort" };

        var burgers = new Category { Name = "Burgere" };
        var sides = new Category { Name = "Tilbehør" };
        var drinks = new Category { Name = "Drikkevarer" };
        menu.Categories.Add(burgers);
        menu.Categories.Add(sides);
        menu.Categories.Add(drinks);

        menu.MenuItems.Add(new MenuItem { Category = burgers, Name = "Whopper", Description = "Flamegrillet oksekødsbøf med salat, tomat og løg", Price = 59.00m });
        menu.MenuItems.Add(new MenuItem { Category = burgers, Name = "Chicken Royale", Description = "Sprødt paneret kyllingebryst med mayo", Price = 55.00m });
        menu.MenuItems.Add(new MenuItem { Category = sides, Name = "Pommes Frites", Description = "Sprøde pommes frites", Price = 25.00m });
        menu.MenuItems.Add(new MenuItem { Category = drinks, Name = "Cola", Description = "0,5 L læskedrik", Price = 20.00m });
        menu.MenuItems.Add(new MenuItem { Name = "Dagens Menu", Description = "Whopper, pommes frites og en cola", Price = 79.00m }); // uden kategori

        return new Restaurant
        {
            User = NewOwner("Burger King Ejer", "burgerking@captaincrave.dk"),
            Name = "Burger King",
            Description = "Hjemsted for Whopper'en — flamegrillede burgere",
            Address = "Vesterbrogade 3, 1620 København V",
            Latitude = 55.6720,
            Longitude = 12.5610,
            // Officielt Burger King-logo (Wikimedia Commons).
            ImageUrl = "https://upload.wikimedia.org/wikipedia/commons/c/cc/Burger_King_2020.svg",
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            Menus = { menu }
        };
    }

    private static Restaurant BuildMcDonalds()
    {
        var menu = new Menu { Name = "Menukort" };

        var burgers = new Category { Name = "Burgere" };
        var sides = new Category { Name = "Tilbehør" };
        var drinks = new Category { Name = "Drikkevarer" };
        menu.Categories.Add(burgers);
        menu.Categories.Add(sides);
        menu.Categories.Add(drinks);

        menu.MenuItems.Add(new MenuItem { Category = burgers, Name = "Big Mac", Description = "To bøffer, special sauce, salat, ost, agurker og løg", Price = 55.00m });
        menu.MenuItems.Add(new MenuItem { Category = burgers, Name = "McChicken", Description = "Sprødt kyllingefilet med mayo og salat", Price = 45.00m });
        menu.MenuItems.Add(new MenuItem { Category = sides, Name = "Pommes Frites", Description = "Verdenskendte sprøde pommes frites", Price = 22.00m });
        menu.MenuItems.Add(new MenuItem { Category = drinks, Name = "Milkshake", Description = "Vanilje-milkshake", Price = 30.00m });
        menu.MenuItems.Add(new MenuItem { Name = "Happy Meal", Description = "Burger, pommes frites og legetøj", Price = 49.00m }); // uden kategori

        return new Restaurant
        {
            User = NewOwner("McDonald's Ejer", "mcdonalds@captaincrave.dk"),
            Name = "McDonald's",
            Description = "I'm lovin' it — hurtig mad til hele familien",
            Address = "Strøget 12, 1160 København K",
            Latitude = 55.6786,
            Longitude = 12.5750,
            // Officielt McDonald's-logo (Wikimedia Commons).
            ImageUrl = "https://upload.wikimedia.org/wikipedia/commons/3/36/McDonald%27s_Golden_Arches.svg",
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            Menus = { menu }
        };
    }

    private static Restaurant BuildKfc()
    {
        var menu = new Menu { Name = "Menukort" };

        var chicken = new Category { Name = "Kylling" };
        var sides = new Category { Name = "Tilbehør" };
        var drinks = new Category { Name = "Drikkevarer" };
        menu.Categories.Add(chicken);
        menu.Categories.Add(sides);
        menu.Categories.Add(drinks);

        menu.MenuItems.Add(new MenuItem { Category = chicken, Name = "Original Recipe Bucket", Description = "8 stykker sprødstegt kylling med hemmelig krydderiblanding", Price = 149.00m });
        menu.MenuItems.Add(new MenuItem { Category = chicken, Name = "Zinger Burger", Description = "Krydret sprødt kyllingefilet-burger", Price = 49.00m });
        menu.MenuItems.Add(new MenuItem { Category = sides, Name = "Coleslaw", Description = "Frisk hjemmelavet coleslaw", Price = 18.00m });
        menu.MenuItems.Add(new MenuItem { Category = drinks, Name = "Pepsi", Description = "0,5 L læskedrik", Price = 20.00m });
        menu.MenuItems.Add(new MenuItem { Name = "Familiemenu", Description = "16 stykker kylling til hele familien", Price = 249.00m }); // uden kategori

        return new Restaurant
        {
            User = NewOwner("KFC Ejer", "kfc@captaincrave.dk"),
            Name = "KFC",
            Description = "Finger Lickin' Good — verdens bedste sprødstegte kylling",
            Address = "Nørrebrogade 45, 2200 København N",
            Latitude = 55.6900,
            Longitude = 12.5500,
            // Officielt KFC-logo (Wikimedia Commons).
            ImageUrl = "https://upload.wikimedia.org/wikipedia/commons/b/bf/KFC_logo.svg",
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            Menus = { menu }
        };
    }

    private static Restaurant BuildSubway()
    {
        var menu = new Menu { Name = "Menukort" };

        var sandwiches = new Category { Name = "Sandwich" };
        var sides = new Category { Name = "Tilbehør" };
        var drinks = new Category { Name = "Drikkevarer" };
        menu.Categories.Add(sandwiches);
        menu.Categories.Add(sides);
        menu.Categories.Add(drinks);

        menu.MenuItems.Add(new MenuItem { Category = sandwiches, Name = "Italian B.M.T.", Description = "Pepperoni, skinke og salami med friske grønsager", Price = 59.00m });
        menu.MenuItems.Add(new MenuItem { Category = sandwiches, Name = "Kylling Teriyaki", Description = "Grillet kylling med teriyaki-sauce", Price = 55.00m });
        menu.MenuItems.Add(new MenuItem { Category = sides, Name = "Cookies", Description = "Friskbagte cookies", Price = 15.00m });
        menu.MenuItems.Add(new MenuItem { Category = drinks, Name = "Sprite", Description = "0,5 L læskedrik", Price = 20.00m });
        menu.MenuItems.Add(new MenuItem { Name = "Footlong Menu", Description = "Footlong sandwich, chips og drik", Price = 89.00m }); // uden kategori

        return new Restaurant
        {
            User = NewOwner("Subway Ejer", "subway@captaincrave.dk"),
            Name = "Subway",
            Description = "Eat Fresh — friske sandwich bygget efter dit ønske",
            Address = "Frederiksberg Allé 20, 1820 Frederiksberg",
            Latitude = 55.6740,
            Longitude = 12.5480,
            // Officielt Subway-logo (Wikimedia Commons).
            ImageUrl = "https://upload.wikimedia.org/wikipedia/commons/5/5c/Subway_2016_logo.svg",
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            Menus = { menu }
        };
    }

    private static Restaurant BuildDominos()
    {
        var menu = new Menu { Name = "Menukort" };

        var pizza = new Category { Name = "Pizza" };
        var sides = new Category { Name = "Tilbehør" };
        var drinks = new Category { Name = "Drikkevarer" };
        menu.Categories.Add(pizza);
        menu.Categories.Add(sides);
        menu.Categories.Add(drinks);

        menu.MenuItems.Add(new MenuItem { Category = pizza, Name = "Pepperoni Passion", Description = "Rigelig pepperoni og ekstra ost", Price = 89.00m });
        menu.MenuItems.Add(new MenuItem { Category = pizza, Name = "Vegetar Supreme", Description = "Peberfrugt, champignon, løg og oliven", Price = 85.00m });
        menu.MenuItems.Add(new MenuItem { Category = sides, Name = "Hvidløgsbrød", Description = "Ovnbagt hvidløgsbrød", Price = 29.00m });
        menu.MenuItems.Add(new MenuItem { Category = drinks, Name = "Cola", Description = "1,5 L læskedrik", Price = 25.00m });
        menu.MenuItems.Add(new MenuItem { Name = "Familiemenu", Description = "2 store pizzaer, hvidløgsbrød og cola", Price = 199.00m }); // uden kategori

        return new Restaurant
        {
            User = NewOwner("Domino's Ejer", "dominos@captaincrave.dk"),
            Name = "Domino's Pizza",
            Description = "Frisk pizza leveret hurtigt til døren",
            Address = "Amagerbrogade 100, 2300 København S",
            Latitude = 55.6600,
            Longitude = 12.6000,
            // Officielt Domino's-logo (Wikimedia Commons).
            ImageUrl = "https://upload.wikimedia.org/wikipedia/commons/7/74/Dominos_pizza_logo.svg",
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            Menus = { menu }
        };
    }

    private static Restaurant BuildPizzaHut()
    {
        var menu = new Menu { Name = "Menukort" };

        var pizza = new Category { Name = "Pizza" };
        var sides = new Category { Name = "Tilbehør" };
        var drinks = new Category { Name = "Drikkevarer" };
        menu.Categories.Add(pizza);
        menu.Categories.Add(sides);
        menu.Categories.Add(drinks);

        menu.MenuItems.Add(new MenuItem { Category = pizza, Name = "Pan Pizza Pepperoni", Description = "Tyk bund med pepperoni og ost", Price = 95.00m });
        menu.MenuItems.Add(new MenuItem { Category = pizza, Name = "Meat Lovers", Description = "Pepperoni, skinke, bacon og pølse", Price = 99.00m });
        menu.MenuItems.Add(new MenuItem { Category = sides, Name = "Potato Wedges", Description = "Sprøde kartoffelbåde", Price = 32.00m });
        menu.MenuItems.Add(new MenuItem { Category = drinks, Name = "Fanta", Description = "0,5 L læskedrik", Price = 20.00m });
        menu.MenuItems.Add(new MenuItem { Name = "Duo Menu", Description = "2 mellemstore pizzaer til deling", Price = 159.00m }); // uden kategori

        return new Restaurant
        {
            User = NewOwner("Pizza Hut Ejer", "pizzahut@captaincrave.dk"),
            Name = "Pizza Hut",
            Description = "No one out-pizzas the Hut",
            Address = "Roskildevej 50, 2000 Frederiksberg",
            Latitude = 55.6750,
            Longitude = 12.4980,
            // Officielt Pizza Hut-logo (Wikimedia Commons).
            ImageUrl = "https://upload.wikimedia.org/wikipedia/commons/d/d2/Pizza_Hut_logo.svg",
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            Menus = { menu }
        };
    }

    // Opretter en restaurant-ejer med et fast standardkodeord til lokal test/login.
    private static User NewOwner(string name, string email) => new()
    {
        Name = name,
        Email = email,
        Address = "Danmark",
        PasswordHash = BCrypt.Net.BCrypt.HashPassword("Password123!"),
        Role = UserRole.Restaurant,
        CreatedAt = DateTime.UtcNow
    };
}
