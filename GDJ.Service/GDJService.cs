using SpotifyAPI.Web;
using System.Reflection.Metadata.Ecma335;
using System.Timers;

namespace GDJ.Service
{

    public class GDJService : IGDJService
    {
        private static int API_POLL_INTERVAL_MS = 5000;
        private static int BUFFER_TIME_MS = 2500;
        private static int Q_MAX_TRACKS = 1;

        // Dictionary of playlist id and counter of how many times it has been played
        private Dictionary<string, int> playlistStats;

        // Playlists with mixRatios (Updated by the GDJ.CLI)
        private List<Playlist> playlists; 

        private System.Timers.Timer service;
        private SpotifyClient client;

        public GDJService(SpotifyClient client)
        {
            playlistStats = new Dictionary<string, int>();

            service = new System.Timers.Timer(API_POLL_INTERVAL_MS);
            service.Elapsed += ServiceCallback;
            service.AutoReset = true;

            this.client = client;
            playlists = new List<Playlist>();
        }

        public void UpdatePlaylists(List<Playlist> playlists)
        {
            this.playlists.Clear();
            this.playlists.AddRange(playlists ?? new List<Playlist>());

            // the service is disabled if all playlists are disabled or provided playlist List is null
            serivce.Enabled = (this.playlists.Count != 0);

            foreach (var id in playlistStats.Keys.ToList())
            {
                // Disabled playlists are removed from the dictionary
                if(playlists.All(p => p.Id != id))
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
            foreach (var playlist in playlists)
            {
                if (!playlistStats.ContainsKey(playlist.Id!))
                {
                    playlistStats.Add(playlist.Id!, 0); // playlistStats[playlist.Id] = 0; also works
                }
            }
        }

        private async void ServiceCallback(object? sender, ElapsedEventArgs e)
        {
            var currPlaying = await client.Player.GetCurrentlyPlaying(new PlayerCurrentlyPlayingRequest());

            if (currPlaying.Item is FullTrack currTrack)
            {
                if (currPlaying.ProgressMs >= currTrack.DurationMs - BUFFER_TIME_MS) // Track is about to end
                {
                    var q = (await client.Player.GetQueue()).Queue;
                    if (q.Count <= Q_MAX_TRACKS)
                    {
                        GetNext();
                    }
                }
            }
        }

        private async void GetNext()
        {
            var nextPlaylistId = GetNextPlaylistId();
            FullTrack? randTrack = await GetRandTrack(nextPlaylistId);
            if (randTrack is null)
            {
                // Remove empty playlist from the list
                UpdatePlaylists(playlists.Where(p => p.Id != nextPlaylistId).ToList());
                return;
            }

            // Check if the random track is already in the queue
            var q = (await client.Player.GetQueue()).Queue;
            if (q.OfType<FullTrack>().Any(t => t.Id.Equals(randTrack.Id)))
            {
                GetNext(); // Try again
                return;
            }

            var isAdded = await client.Player.AddToQueue(new PlayerAddToQueueRequest(randTrack.Uri));
            if (isAdded)
            {
                playlistStats[nextPlaylistId]++;
            }
        }

        private async Task<FullTrack?> GetRandTrack(string playlistId)
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
            return playlists // playlists is never empty-> Checked in UpdatePlaylists() 
                .Select(p => new KeyValuePair<string, double>(p.Id!, playlistStats[p.Id!] * (1 - p.MixRatio)))
                .OrderBy(kvp => kvp.Value)
                .FirstOrDefault().Key;
        }
    }
}
