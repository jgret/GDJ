using SpotifyAPI.Web;
using System.Timers;

namespace GDJ.Service
{

    public class GDJService : IGDJService
    {
        public static int API_POLL_INTERVAL = 5000;

        private Dictionary<string, int> playlistStats;
        private List<Playlist> playlists; // Playlists with mixRatios

        private System.Timers.Timer pollPlayback;
        private SpotifyClient client;

        public GDJService(SpotifyClient client)
        {
            playlistStats = new Dictionary<string, int>();

            pollPlayback = new System.Timers.Timer(API_POLL_INTERVAL);
            pollPlayback.Elapsed += TimerCallback;
            pollPlayback.AutoReset = true;
            pollPlayback.Enabled = true; // Start the polling Timer

            this.client = client;
            playlists = new List<Playlist>();
        }

        private async void GetNext()
        {
            // Calc Next Playlist
        }

        public void UpdatePlaylists(List<Playlist> playlists)
        {
            this.playlists.Clear();
            this.playlists.AddRange(playlists);
            
            foreach (var item in playlistStats.Keys)
            {
                if(!playlists.Any(p => p.Id == item))
                {
                    playlistStats.Remove(item);
                }
                else
                {
                    playlistStats[item] = 0;
                }
            }

            foreach (var item in playlists)
            {
                if(!playlistStats.ContainsKey(item.Id))
                {
                    playlistStats[item.Id] = 0;
                }
            }
        }


        private async void TimerCallback(object? sender, ElapsedEventArgs e)
        {
            var currentTrack = await client.Player.GetCurrentlyPlaying(new PlayerCurrentlyPlayingRequest());

            if(currentTrack.Item is FullTrack fullTrack)
            {
                if(currentTrack.ProgressMs >= fullTrack.DurationMs)
                {
                    var q = await client.Player.GetQueue();
                    if (q.Queue.Count <= 1)
                    {
                        GetNext();
                    }
                }
            }
        }
    }
}
