using System.Threading.Channels;

namespace AreebTechnologyTask.Services.Tickets
{
    /// <summary>
    /// In-memory queue of bookings whose ticket email still has to be sent.
    /// Booking only adds the booking ID here, so the page responds immediately; <see cref="TicketEmailWorker"/>
    /// builds and sends the email in the background.
    /// </summary>
    public class TicketEmailQueue
    {
        private readonly Channel<int> _channel = Channel.CreateUnbounded<int>(new UnboundedChannelOptions { SingleReader = true });

        public void Enqueue(int bookingId) => _channel.Writer.TryWrite(bookingId);

        public IAsyncEnumerable<int> ReadAllAsync(CancellationToken cancellationToken) => _channel.Reader.ReadAllAsync(cancellationToken);
    }

    /// <summary>Background service that sends the queued ticket emails one by one.</summary>
    public class TicketEmailWorker : BackgroundService
    {
        private readonly TicketEmailQueue _queue;
        private readonly IServiceScopeFactory _scopes;
        private readonly ILogger<TicketEmailWorker> _logger;

        public TicketEmailWorker(TicketEmailQueue queue, IServiceScopeFactory scopes, ILogger<TicketEmailWorker> logger)
        {
            _queue = queue;
            _scopes = scopes;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            await foreach (var bookingId in _queue.ReadAllAsync(stoppingToken))
            {
                try
                {
                    // Each email gets its own scope, so it gets a fresh database context.
                    using var scope = _scopes.CreateScope();
                    var emails = scope.ServiceProvider.GetRequiredService<TicketEmailService>();
                    await emails.SendTicketAsync(bookingId, stoppingToken);
                }
                catch (Exception ex) when (!stoppingToken.IsCancellationRequested)
                {
                    // The booking itself is already saved; the user can use "Email my ticket" to try again.
                    _logger.LogError(ex, "Could not send the ticket email for booking {BookingId}.", bookingId);
                }
            }
        }
    }
}
