using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace AreebTechnologyTask.Services.Tickets
{
    /// <summary>
    /// Creates and checks ticket codes such as "TKT-000123-K7PQ2MXA4Z".
    /// The last part is a signature (HMAC-SHA256) of the booking ID made with a secret key, so a ticket
    /// cannot be faked or changed to another booking: only this server can produce a valid code.
    /// </summary>
    public partial class TicketCodeService
    {
        private const string Base32Alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ234567";
        private const int SignatureLength = 10; // 50 bits

        private readonly byte[] _key;

        public TicketCodeService(IConfiguration configuration)
        {
            // "Tickets:SigningKey" can be set separately; otherwise a ticket-only key is derived from the JWT key,
            // so the same secret is never used directly for two different purposes.
            var secret = configuration["Tickets:SigningKey"];
            if (string.IsNullOrWhiteSpace(secret))
            {
                secret = configuration["Jwt:Key"];
            }
            if (string.IsNullOrWhiteSpace(secret))
            {
                throw new InvalidOperationException("No key for signing tickets: set Tickets:SigningKey or Jwt:Key.");
            }
            _key = HMACSHA256.HashData(Encoding.UTF8.GetBytes(secret), Encoding.UTF8.GetBytes("event-booking/ticket-code/v1"));
        }

        /// <summary>The ticket code for a booking, e.g. "TKT-000123-K7PQ2MXA4Z".</summary>
        public string CreateCode(int bookingId) => $"TKT-{bookingId:D6}-{Signature(bookingId)}";

        /// <summary>Checks a ticket code and returns the booking ID it belongs to, if the signature is valid.</summary>
        public bool TryReadCode(string? code, out int bookingId)
        {
            bookingId = 0;
            var match = CodePattern().Match(code?.Trim().ToUpperInvariant() ?? string.Empty);
            if (!match.Success || !int.TryParse(match.Groups["id"].Value, out var id))
            {
                return false;
            }

            var expected = Encoding.ASCII.GetBytes(Signature(id));
            var actual = Encoding.ASCII.GetBytes(match.Groups["sig"].Value);
            if (!CryptographicOperations.FixedTimeEquals(expected, actual)) // constant time: no timing hints
            {
                return false;
            }

            bookingId = id;
            return true;
        }

        private string Signature(int bookingId)
        {
            var mac = HMACSHA256.HashData(_key, Encoding.UTF8.GetBytes($"booking:{bookingId}"));
            return ToBase32(mac)[..SignatureLength];
        }

        private static string ToBase32(byte[] bytes)
        {
            var result = new StringBuilder();
            int buffer = 0, bits = 0;
            foreach (var b in bytes)
            {
                buffer = (buffer << 8) | b;
                bits += 8;
                while (bits >= 5)
                {
                    result.Append(Base32Alphabet[(buffer >> (bits - 5)) & 31]);
                    bits -= 5;
                }
            }
            return result.ToString();
        }

        [GeneratedRegex(@"^TKT-(?<id>\d{1,9})-(?<sig>[A-Z2-7]{10})$")]
        private static partial Regex CodePattern();
    }
}
