using SpotifyAPI.Web;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GDJ.Service
{
    public class Playlist // -> Record?
    {
        public string? Id { get; set; } // Spotify Playlist ID
        public double MixRatio { get; set; } // Mix ratio of the playlist (0.0 - 1.0)
        public Playlist(string id, double mixRatio)
        {
            Id = id;
            MixRatio = mixRatio;
        }
    }
}
