-- OPTIONAL / REFERENCE ONLY.
-- The app now creates the database, applies migrations and adds sample data automatically
-- on first start (see Data/DbSeeder.cs and the README). Run this script only for a manual setup.

use Event_Booking_System;
GO

-- Insert Users
-- Email                : password
-- admin@gmail.com      : admin123
-- john@gmail.com       : john123
-- jane@gmail.com       : jane123
-- AbdElwahed@gmail.com : abdelwahed123
INSERT INTO dbo.Users (Name, Email, HashedPassword, Role, Address, Phone) VALUES
('Admin', 'admin@gmail.com', '$2a$11$jM1Ig59CqxcVgXu5H1nqhetQqQ493xZYGlRW.yH0eyS5aaEeZKpOi', 'Admin', 'Maadi st 9 cairo', '01114778562'),
('John Doe', 'john@gmail.com', '$2a$11$WLpF.sQB9s2f0veZhfXdSe6iw.8dyYPdM6GLDQB0sUhVw4lo5k3z2', 'User', 'New cairo', '01005635487'),
('Jane Smith', 'jane@gmail.com', '$2a$11$8Rs5mxBktEraI4Y5aBWD9up3mFHeRjqRLxg4/Ck1.rN3fTKLhAkEK', 'User', 'kozzika', '01117884756'),
('AbdElwahed Ragab', 'AbdElwahed@gmail.com', '$2a$11$LQv3c1yqBWVHxkd0LHAkCOYz6TtxMQJqhN8/LewYpR0NxKxJ9qK8y', 'User', 'Maadi ahmed zaky st', '01114668742');
GO

-- Insert Events
INSERT INTO dbo.Events (Name, Description, Category, Date, Venue, Price, ImageUrl) VALUES
('Tech Conference 2024', 'Join us for the most anticipated technology conference of the year! Featuring keynote speeches from industry leaders, hands-on workshops on AI and Machine Learning, networking sessions, and exclusive product demonstrations. Perfect for tech enthusiasts, developers, and business leaders looking to stay ahead of the curve.', 'Technology', '2025-06-15 09:00:00', 'Convention Center', 299, 'tech-conference.jpg'),
('Summer Music Festival', 'Experience three days of non-stop music featuring over 50 artists across 5 stages! From rock to electronic, jazz to hip-hop, this festival has something for everyone. Enjoy gourmet food vendors, art installations, and camping facilities. Early bird tickets include access to exclusive artist meet-and-greets.', 'Music', '2025-05-20 14:00:00', 'City Park', 199, 'music-festival.jpg'),
('International Food Expo', 'A culinary journey around the world! Sample dishes from 30+ countries, attend cooking demonstrations by celebrity chefs, participate in wine tastings, and learn new recipes in interactive workshops. Perfect for food lovers, home cooks, and culinary professionals. Includes a souvenir recipe book and cooking apron.', 'Food', '2025-06-10 10:00:00', 'Exhibition Hall', 99, 'food-expo.jpg'),
('Championship Sports Tournament', 'Witness the ultimate showdown of athletic excellence! Featuring top athletes competing in basketball, soccer, and volleyball. Enjoy live entertainment, food vendors, and interactive sports activities for all ages. Premium tickets include VIP seating, meet-and-greet with athletes, and exclusive merchandise.', 'Sports', '2025-09-05 08:00:00', 'Sports Complex', 149, 'sports-tournament.jpg'),
('Art & Design Exhibition', 'Immerse yourself in a world of creativity! Showcasing contemporary art, digital design, and interactive installations from emerging and established artists. Features workshops, artist talks, and a marketplace for unique art pieces. Perfect for art enthusiasts, collectors, and creative professionals.', 'Arts', '2025-08-12 11:00:00', 'Modern Art Gallery', 79, 'art-exhibition.jpg'),
('Business Innovation Summit', 'Connect with industry leaders and innovators! A two-day event featuring panel discussions on emerging business trends, startup pitch competitions, and networking opportunities. Includes access to exclusive business resources and mentorship sessions with successful entrepreneurs.', 'Business', '2025-07-20 09:00:00', 'Business Center', 349, 'business-summit.jpg');
GO

-- Insert Sample Bookings
INSERT INTO dbo.Bookings (UserId, EventId, BookingDate) VALUES
(2, 1, GETDATE()),
(2, 3, GETDATE()),
(3, 2, GETDATE()),
(3, 4, GETDATE());
GO