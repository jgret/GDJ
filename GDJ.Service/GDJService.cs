using Newtonsoft.Json;
using SpotifyAPI.Web;
using System.Globalization;
using System.Timers;

namespace GDJ.Service
{

    public class GDJService : IGDJService
    {
        private static readonly int API_POLL_INTERVAL_MS = 7000;

        private readonly System.Timers.Timer service;
        private readonly SpotifyClient client;

        private Dictionary<string, Playlist> library;
        private MixDistribution mixDistribution;

        // -------------------------------------------------------------------------------- 
        // --- Service Initialization

        public GDJService(SpotifyClient client)
        {
            this.client = client;

            library = new Dictionary<string, Playlist>();
            mixDistribution = new MixDistribution();

            service = new System.Timers.Timer(API_POLL_INTERVAL_MS);
            service.Elapsed += ServiceCallbackAsync;
            service.AutoReset = true;
        }

        // -------------------------------------------------------------------------------- 
        // --- API

        public void UpdateMixById(string id, double mix)
        {
            mixDistribution.UpdateMixById(id, mix);
        }

        // -------------------------------------------------------------------------------- 
        // --- Service Control Methods

        public async void FetchLibraryAsync(CancellationToken cancel = default)
        {
            var playlistPage = await client.Playlists.CurrentUsers(cancel);
            var playlists = await client.PaginateAll(playlistPage, cancellationToken: cancel);

            foreach (FullPlaylist fp in playlists)
            {
                var fpPages = await client.Playlists.GetItems(fp.Id!, cancel);
                var items = await client.PaginateAll(fpPages, cancellationToken: cancel);

                if (items.Count == 0) continue;

                var trackList = items
                        .Select(t => t.Track)
                        .OfType<FullTrack>()
                        .Distinct()
                        .Select(t => t.Uri)
                        .ToList();

                library.TryAdd(fp.Id!, new Playlist(fp.Id!, fp.Name, trackList));
            }

            double initMix = 1.0 / library.Count;
            var mixList = library
                .Values
                .Select(p => new Mix(p.Id, initMix, p.Name))
                .ToDictionary(m => m.Id);

            mixDistribution = new MixDistribution(mixList);
            service.Enabled = true;
        }


        // -------------------------------------------------------------------------------- 
        // --- Service Timer Callback

        private async void ServiceCallbackAsync(object? sender, ElapsedEventArgs e) {
            var retry = false;
            var nextPlaylistId = mixDistribution.GetNextPlaylistId();
            var nextTrackUri = library[nextPlaylistId!].GetNextTrackUri();

            do
            {
                try
                {
                    await client.Player.AddToQueue(new PlayerAddToQueueRequest(nextTrackUri));
                }
                catch (APITooManyRequestsException ex)
                {
                    Console.WriteLine($"Too many Requests. Retrying after {ex.RetryAfter}");
                    await Task.Delay(ex.RetryAfter);
                    retry = true;
                }
                catch (APIException ex)
                {
                    Console.WriteLine($"{ex.Message}\nGDJ needs an active Spotify Player. Pausing the Queueing service.");
                    service.Enabled = false;
                }
            }
            while (retry);
        }
    }
}
