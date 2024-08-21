using Newtonsoft.Json;
using SpotifyAPI.Web;
using System.Timers;

namespace GDJ.Service
{

    public class GDJService : IGDJService
    {
        private static int API_POLL_INTERVAL_MS = 7000;


        private Dictionary<string, PlaylistMix> activePlaylists; 
        private int totalSongsPlayed;

        private System.Timers.Timer service;
        private SpotifyClient client;

        // ------------------------------
        // --- Service Initialization ---
        // ------------------------------

        public GDJService(SpotifyClient client)
        {
            this.client = client;
            
            activePlaylists = new Dictionary<string, PlaylistMix>();

            service = new System.Timers.Timer(API_POLL_INTERVAL_MS);
            service.Elapsed += ServiceCallback;
            service.AutoReset = true;

            totalSongsPlayed = 0;
        }

        // -------------------------------
        // --- Service Control Methods ---
        // -------------------------------

        public void UpdatePlaylists(List<PlaylistMix> pl)
        {
            activePlaylists.Clear();
            activePlaylists = (pl ?? new List<PlaylistMix>()).ToDictionary(p => p.Id, p => p);
            
            // the service is disabled if all playlists are disabled or List is null
            service.Enabled = activePlaylists.Count != 0;
        }

        // ------------------------------
        // --- Service Timer Callback ---
        // ------------------------------

        private async void ServiceCallback(object? sender, ElapsedEventArgs e)
        {
            var retry = false;
            do
            {
                try
                {
                    await GetNextAsync();
                }
                catch (APITooManyRequestsException ex)
                {
                    Console.WriteLine($"Too many Requests. Retrying after {ex.RetryAfter}");
                    await Task.Delay(ex.RetryAfter);
                    retry = true;
                }
            }
            while (retry);
        }

        // ------------------------------
        // --- Service Logic Methods ---
        // ------------------------------
        private async Task GetNextAsync()
        {
            var nextPlaylistId = GetNextPlaylistId();
            FullTrack? randTrack = await GetRandTrackAsync(nextPlaylistId);
            if (randTrack is null)
            {
                // Remove empty playlist from the list (Or if the playlist contained an Episode -> don't use Playists with episodes)
                UpdatePlaylists(activePlaylists.Values.Where(p => p.Id != nextPlaylistId).ToList());
                return;
            }

            // Check if the random track is already in the queue
            var q = (await client.Player.GetQueue()).Queue;
            if (q.OfType<FullTrack>().Any(t => t.Id == randTrack.Id))
            {
                await GetNextAsync(); // Try again
                // TODO: test with edge cases -> potential recursive loop if a playlist with only one track is enabled
                return;
            }

            try
            {
                await client.Player.AddToQueue(new PlayerAddToQueueRequest(randTrack.Uri)); // <-- This always throws an exception. See Issue #2
            }
            catch (JsonReaderException e)
            {
                Console.WriteLine(e.Message);
            }

            activePlaylists[nextPlaylistId].NumPlayed++;
            totalSongsPlayed++;
        }

        private async Task<FullTrack?> GetRandTrackAsync(string playlistId, CancellationToken cancellationToken = default)
        {
            var items = (await client.Playlists.GetItems(playlistId)).Items;
            if (items == null || items.Count == 0)
            {
                return null;
            }

            Random rand = new Random();
            var randIdx = rand.Next(0, items.Count);
            var randTrack = items[randIdx].Track;

            return randTrack as FullTrack;
        }

        private string GetNextPlaylistId()
        {
            return activePlaylists // Sort by the difference between mix and actual ratio
                .OrderByDescending(p => (p.Value.MixRatio * totalSongsPlayed) - p.Value.NumPlayed)
                .First().Key;
        }
    }
}
