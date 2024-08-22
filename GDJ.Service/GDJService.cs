using Newtonsoft.Json;
using SpotifyAPI.Web;
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

            // the service is disabled if all playlists are disabled or List is null
            service.Enabled = activePlaylists.Count != 0;
        }

        public async Task<List<PlaylistMix>> RefetchLibrary(CancellationToken cancel = default)
        {
            var playlistPage = await client.Playlists.CurrentUsers(cancel);

            var playlists = await client.PaginateAll(playlistPage, cancellationToken: cancel);

            foreach (var fp in playlists)
            {
                var items = await client.PaginateAll(fp.Tracks!, cancellationToken: cancel);
                
                var pl = new Playlist(fp.Id!, fp.Name);
                pl.TrackIds = items.Select(i => ((FullTrack)i.Track).Id!).ToList();

                library.Add(pl.Id, pl);
            }

            return library.Values.Select(p => new PlaylistMix(p.Id, 0.0)).ToList();

            //await foreach (var playlist in client.Paginate(playlistPage, cancel: cancel))
            //{
            //    library.Add(playlist.Id!, new Playlist(playlist.Id!, playlist.Name));
            //    var items = await client.Playlists.GetItems(playlist.Id!, cancel);
            //    await foreach (var item in client.Paginate(items, cancel: cancel))
            //    {
            //        if (item.Track is FullTrack track)
            //        {
            //            library[playlist.Id!].TrackIds!.Add(track.Id!);
            //        }
            //        // await Task.Delay(10, cancel); // Avoid rate limiting
            //    }
            //    // await Task.Delay(10, cancel); // Avoid rate limiting
            //}
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
            FullTrack? randTrack = await GetRandTrackAsync(nextPlaylistId, cancellationToken);
            if (randTrack is null)
            {
                // Remove empty playlist from the list (Or if the playlist contained an Episode -> don't use Playists with episodes)
                UpdatePlaylists(activePlaylists.Values.Where(p => p.Id != nextPlaylistId).ToList());
                return;
            }

            // Check if the random track is already in the queue
            var q = (await client.Player.GetQueue(cancellationToken)).Queue;
            if (q.OfType<FullTrack>().Any(t => t.Id == randTrack.Id))
            {
                await GetNextAsync(cancellationToken); // Try again
                // TODO: test with edge cases -> potential recursive loop if a playlist with only one track is enabled
                return;
            }

            try
            {
                await client.Player.AddToQueue(new PlayerAddToQueueRequest(randTrack.Uri), cancellationToken); // <-- This always throws an exception. See Issue #2
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
            var items = (await client.Playlists.GetItems(playlistId, cancellationToken)).Items;
            if (items == null || items.Count == 0)
            {
                return null;
            }

            Random rand = new();
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
