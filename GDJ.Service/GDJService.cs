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
            activePlaylists = (pl ?? []).ToDictionary(p => p.Id, p => p);
            service.Enabled = activePlaylists.Count != 0; // the service is disabled if all playlists are disabled or List is null
        }

        public async Task<List<PlaylistMix>> RefetchLibrary(CancellationToken cancel = default)
        {
            var playlistPage = await client.Playlists.CurrentUsers(cancel);
            var playlists = await client.PaginateAll(playlistPage, cancellationToken: cancel);

            foreach (FullPlaylist fp in playlists)
            {
                var itemsPage = fp.Tracks!;
                var items = await client.PaginateAll(itemsPage, cancellationToken: cancel);

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
            return library.Values.Select(p => new PlaylistMix(p.Id, 0.0, p.Name)).ToList();
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

        // -----------------------------
        // --- Service Logic Methods ---
        // -----------------------------

        private async Task GetNextAsync(CancellationToken cancellationToken = default)
        {
            var nextPlaylistId = GetNextPlaylistId();
            var randTrackUri = GetRandTrackUri(nextPlaylistId, cancellationToken);

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
                await client.Player.AddToQueue(new PlayerAddToQueueRequest(randTrackUri), cancellationToken); // <-- TODO: This always throws an exception. See Issue #2
            }
            catch (JsonReaderException e)
            {
                Console.WriteLine(e.Message);
            }

            activePlaylists[nextPlaylistId].NumPlayed++;
            totalSongsPlayed++;
        }

        private string GetRandTrackUri(string playlistId, CancellationToken cancellationToken = default)
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
