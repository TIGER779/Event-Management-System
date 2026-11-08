using System.ComponentModel.DataAnnotations.Schema;

namespace EventHub.Models
{
    public class Event
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string Category { get; set; }
        public DateTime Date { get; set; }
        public string Venue { get; set; }
        public int Price { get; set; }
        public string ImageUrl { get; set; }
        public ICollection<Booking>? Bookings { get; set; }

        // Resolves ImageUrl to a usable <img src>:
        //  - full URLs (http/https) are used as-is
        //  - paths starting with "/" are used as-is
        //  - plain file names are served from wwwroot/Images
        //  - empty values fall back to a placeholder
        [NotMapped]
        public string DisplayImageUrl
        {
            get
            {
                if (string.IsNullOrWhiteSpace(ImageUrl))
                    return "/Images/placeholder.svg";

                var url = ImageUrl.Trim();
                if (url.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
                    url.StartsWith("https://", StringComparison.OrdinalIgnoreCase) ||
                    url.StartsWith("/"))
                    return url;

                if (url.StartsWith("~/"))
                    return url.Substring(1);

                if (url.StartsWith("Images/", StringComparison.OrdinalIgnoreCase))
                    return "/" + url;

                return "/Images/" + url;
            }
        }
    }
}
