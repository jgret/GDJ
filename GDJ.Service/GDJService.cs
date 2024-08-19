using Newtonsoft.Json;
using SpotifyAPI.Web;
using SpotifyAPI.Web.Http;
using System.Reflection.Metadata.Ecma335;
using System.Timers;

namespace GDJ.Service
{

    public class GDJService : IGDJService
    {
        private static int API_POLL_INTERVAL_MS = 180000;
        private static int BUFFER_TIME_MS = 6000;
        private static int Q_MAX_TRACKS = 1;

        // Dictionary of playlist id and counter of how many times it has been played
        private Dictionary<string, int> playlistStats;
        private int totalSongsPlayed;

        // Playlists with mixRatios (Updated by the GDJ.CLI)
        private List<Playlist> playlists; 

        private System.Timers.Timer service;
        private SpotifyClient client;

        private string deviceId;

        public GDJService(SpotifyClient client)
        {
            this.client = client;
            
            playlistStats = new Dictionary<string, int>();
            playlists = new List<Playlist>();

            service = new System.Timers.Timer(API_POLL_INTERVAL_MS);
            service.Elapsed += ServiceCallback;
            service.AutoReset = true;
            deviceId = string.Empty;
            totalSongsPlayed = 0;
        }

        public async Task InitDevice()
        {
             deviceId = (await client.Player.GetAvailableDevices()).Devices[0].Id;
        }

        public void UpdatePlaylists(List<Playlist> pl)
        {
            playlists.Clear();
            playlists.AddRange(pl ?? new List<Playlist>());

            foreach (var id in playlistStats.Keys.ToList())
            {
                // Disabled playlists are removed from the dictionary
                if(!playlists.Any(p => p.Id == id))
                {
                    playlistStats.Remove(id);
                }
                // New playlist is available: Reset counters to 0
                else
                {
                    playlistStats[id] = 0;
                }
            }

            // Add new playlists to the dictionary
            foreach (Playlist playlist in playlists)
            {
                if (!playlistStats.ContainsKey(playlist.Id))
                {
                    playlistStats[playlist.Id] = 0;
                }
            }

            // the service is disabled if all playlists are disabled or provided playlist List is null
            service.Enabled = playlists.Count != 0;
        }

        // Timer Callback
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
                    await Task.Delay(ex.RetryAfter);
                    retry = true;
                }
            }
            while (retry);
        }

        private async Task GetNextAsync()
        {
            var nextPlaylistId = GetNextPlaylistId();
            FullTrack? randTrack = await GetRandTrackAsync(nextPlaylistId);
            if (randTrack is null)
            {
                // Remove empty playlist from the list (Or if the playlist contained an Episode -> don't use Playists with episodes)
                UpdatePlaylists(playlists.Where(p => p.Id != nextPlaylistId).ToList());
                return;
            }

            // Check if the random track is already in the queue
            var q = (await client.Player.GetQueue()).Queue;
            if (q.OfType<FullTrack>().Any(t => t.Id.Equals(randTrack.Id)))
            {
                await GetNextAsync(); // Try again
                // TODO: test with edge cases -> potential recursive loop if a playlist with only one track is enabled
                return;
            }

            try
            {
                var p = new PlayerAddToQueueRequest(randTrack.Uri);
                p.DeviceId = deviceId;

                await client.Player.AddToQueue(p);
            }
            catch (JsonReaderException e)
            {
                Console.WriteLine(e.Message);
            }
            playlistStats[nextPlaylistId]++;
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
            // Get the playlist id with the lowest score: (score = counter * (1 - mixRatio))
            return playlists // playlists is never empty -> Checked in UpdatePlaylists() 
                .Select(p => new KeyValuePair<string, double>(p.Id!, (double) (playlistStats[p.Id!]) * (1.0 - p.MixRatio)))
                .OrderBy(kvp => kvp.Value)
                .FirstOrDefault().Key;
        }
    }
}
