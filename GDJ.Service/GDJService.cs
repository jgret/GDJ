using Newtonsoft.Json;
using SpotifyAPI.Web;
using System.Diagnostics;
using System.Threading.Channels;
using System.Timers;

namespace GDJ.Service
{

    public class GDJService : IGDJService
    {
        private static readonly int API_POLL_INTERVAL_MS = 7000;

        private readonly System.Timers.Timer service;
        private readonly SpotifyClient client;

        private Dictionary<string, Playlist> library; // playlist id -> playlist with tracklist
        private Dictionary<string, PlaylistMix> activePlaylists; // playlist id -> playlist with mix ratio
        private int totalSongsPlayed;

        // ------------------------------
        // --- Service Initialization ---
        // ------------------------------

        public GDJService(SpotifyClient client)
        {
            this.client = client;

            library = [];
            activePlaylists = [];

            service = new System.Timers.Timer(API_POLL_INTERVAL_MS);
            service.Elapsed += ServiceCallbackAsync;
            service.AutoReset = true;

            totalSongsPlayed = 0;
        }

        // -------------------------------
        // --- Service Control Methods ---
        // -------------------------------

        public void UpdatePlaylists(List<PlaylistMix> pl)
        {
            activePlaylists.Clear();
            activePlaylists = (pl ?? []).ToDictionary(p => p.Id, p => p);
            service.Enabled = activePlaylists.Count != 0; // the service is disabled if all playlists are disabled or List is null
        }

        public async Task<List<PlaylistMix>> RefetchLibraryAsync(CancellationToken cancel = default)
        {
            var playlistPage = await client.Playlists.CurrentUsers(cancel);
            var playlists = await client.PaginateAll(playlistPage, cancellationToken: cancel);

            foreach (FullPlaylist fp in playlists)
            {
                var fp2 = await client.Playlists.GetItems(fp.Id!, cancel);
                var items = await client.PaginateAll(fp2, cancellationToken: cancel);

                if (items.Count == 0) continue;

                var pl = new Playlist(fp.Id!, fp.Name)
                {
                    TrackUris = items
                    .Select(t => t.Track)
                    .OfType<FullTrack>()
                    .Distinct()
                    .Select(t => t.Uri)
                    .ToList()
                };
                library.TryAdd(pl.Id, pl);
                
            }
            return library.Values.Select(p => new PlaylistMix(p.Id, 0.0, p.Name)).ToList(); // important to return a NEW list
        }

        // ------------------------------
        // --- Service Timer Callback ---
        // ------------------------------

        private async void ServiceCallbackAsync(object? sender, ElapsedEventArgs e)
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

        // -----------------------------
        // --- Service Logic Methods ---
        // -----------------------------

        private async Task GetNextAsync(CancellationToken cancellationToken = default)
        {
            var nextPlaylistId = GetNextPlaylistId();
            var randTrackUri = GetRandTrackUri(nextPlaylistId);

            // Check if the random track is already in the queue
            var q = (await client.Player.GetQueue(cancellationToken)).Queue; // TODO: reduce API calls by caching the queue
            if (q.OfType<FullTrack>().Any(t => t.Uri == randTrackUri))
            {
                await GetNextAsync(cancellationToken); // Try again
                // TODO: test with edge cases -> potential recursive loop if a playlist with only one track is enabled
                return;
            }

            try
            {
                await client.Player.AddToQueue(new PlayerAddToQueueRequest(randTrackUri), cancellationToken); 
            }
            catch (JsonReaderException e)
            {
                Console.WriteLine(e.Message); // <-- TODO: This is always caught. See Issue #2
            }

            activePlaylists[nextPlaylistId].NumPlayed++;
            totalSongsPlayed++;
        }

        private string GetRandTrackUri(string playlistId)
        {
            var items = library[playlistId].TrackUris;

            Random rand = new();
            var randIdx = rand.Next(0, items!.Count);

            return items[randIdx];
        }

        private string GetNextPlaylistId()
        {
            return activePlaylists // Sort by the difference between mix and actual ratio
                .OrderByDescending(p => (p.Value.MixRatio * totalSongsPlayed) - p.Value.NumPlayed)
                .First().Key;
        }
    }
}
