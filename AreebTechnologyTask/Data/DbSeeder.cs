using AreebTechnologyTask.Enums;
using AreebTechnologyTask.Models;
using Microsoft.EntityFrameworkCore;

namespace AreebTechnologyTask.Data
{
    /// <summary>
    /// Creates/updates the database schema and fills an empty database with sample data,
    /// so the app works right after cloning (no manual SQL scripts needed).
    /// </summary>
    public static class DbSeeder
    {
        public static async Task MigrateAndSeedAsync(IServiceProvider services, CancellationToken cancellationToken = default)
        {
            using var scope = services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger(typeof(DbSeeder));

            // Creates the database if needed and applies any migrations that have not run yet.
            // On an existing, up-to-date database this does nothing.
            await db.Database.MigrateAsync(cancellationToken);

            // Sample data is only added to an empty database, so existing data is never changed.
            if (await db.Users.AnyAsync(cancellationToken) || await db.Events.AnyAsync(cancellationToken))
            {
                return;
            }

            logger.LogInformation("Empty database: adding sample users, events and bookings.");

            // Sample accounts use example.com, a domain reserved for examples,
            // so emails sent by the app can never reach a real person.
            var admin = NewUser("Admin", "admin@example.com", "admin123", UserRole.Admin, "Maadi st 9, Cairo", "01114778562");
            var john = NewUser("John Doe", "john@example.com", "john123", UserRole.User, "New Cairo", "01005635487");
            var jane = NewUser("Jane Smith", "jane@example.com", "jane123", UserRole.User, "Kozzika", "01117884756");
            db.Users.AddRange(admin, john, jane);

            // Event dates are relative to today, so a new database always shows upcoming events.
            var today = DateTime.Today;
            var tech = new Event
            {
                Name = $"Tech Conference {today.AddDays(45).Year}",
                Description = "Join us for the most anticipated technology conference of the year! Featuring keynote speeches from industry leaders, hands-on workshops on AI and Machine Learning, networking sessions, and exclusive product demonstrations. Perfect for tech enthusiasts, developers, and business leaders looking to stay ahead of the curve.",
                Category = "Technology",
                Date = today.AddDays(45).AddHours(9),
                Venue = "Convention Center",
                Price = 299,
                ImageUrl = "tech-conference.jpg"
            };
            var music = new Event
            {
                Name = "Summer Music Festival",
                Description = "Experience three days of non-stop music featuring over 50 artists across 5 stages! From rock to electronic, jazz to hip-hop, this festival has something for everyone. Enjoy gourmet food vendors, art installations, and camping facilities. Early bird tickets include access to exclusive artist meet-and-greets.",
                Category = "Music",
                Date = today.AddDays(30).AddHours(14),
                Venue = "City Park",
                Price = 199,
                ImageUrl = "music-festival.jpg"
            };
            var food = new Event
            {
                Name = "International Food Expo",
                Description = "A culinary journey around the world! Sample dishes from 30+ countries, attend cooking demonstrations by celebrity chefs, participate in wine tastings, and learn new recipes in interactive workshops. Perfect for food lovers, home cooks, and culinary professionals. Includes a souvenir recipe book and cooking apron.",
                Category = "Food",
                Date = today.AddDays(40).AddHours(10),
                Venue = "Exhibition Hall",
                Price = 99,
                ImageUrl = "food-expo.jpg"
            };
            var sports = new Event
            {
                Name = "Championship Sports Tournament",
                Description = "Witness the ultimate showdown of athletic excellence! Featuring top athletes competing in basketball, soccer, and volleyball. Enjoy live entertainment, food vendors, and interactive sports activities for all ages. Premium tickets include VIP seating, meet-and-greet with athletes, and exclusive merchandise.",
                Category = "Sports",
                Date = today.AddDays(120).AddHours(8),
                Venue = "Sports Complex",
                Price = 149,
                ImageUrl = "sports-tournament.jpg"
            };
            var art = new Event
            {
                Name = "Art & Design Exhibition",
                Description = "Immerse yourself in a world of creativity! Showcasing contemporary art, digital design, and interactive installations from emerging and established artists. Features workshops, artist talks, and a marketplace for unique art pieces. Perfect for art enthusiasts, collectors, and creative professionals.",
                Category = "Arts",
                Date = today.AddDays(95).AddHours(11),
                Venue = "Modern Art Gallery",
                Price = 79,
                ImageUrl = "art-exhibition.jpg"
            };
            var business = new Event
            {
                Name = "Business Innovation Summit",
                Description = "Connect with industry leaders and innovators! A two-day event featuring panel discussions on emerging business trends, startup pitch competitions, and networking opportunities. Includes access to exclusive business resources and mentorship sessions with successful entrepreneurs.",
                Category = "Business",
                Date = today.AddDays(70).AddHours(9),
                Venue = "Business Center",
                Price = 349,
                ImageUrl = "business-summit.jpg"
            };
            db.Events.AddRange(tech, music, food, sports, art, business);

            var now = DateTime.UtcNow;
            db.Bookings.AddRange(
                new Booking { User = john, Event = tech, BookingDate = now },
                new Booking { User = john, Event = food, BookingDate = now },
                new Booking { User = jane, Event = music, BookingDate = now },
                new Booking { User = jane, Event = sports, BookingDate = now });

            await db.SaveChangesAsync(cancellationToken);
        }

        private static User NewUser(string name, string email, string password, UserRole role, string address, string phone) => new()
        {
            Name = name,
            Email = email,
            HashedPassword = BCrypt.Net.BCrypt.HashPassword(password),
            Role = role,
            Address = address,
            Phone = phone
        };
    }
}
