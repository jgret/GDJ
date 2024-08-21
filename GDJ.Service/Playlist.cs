using System.Runtime.CompilerServices;

namespace GDJ.Service
{
    public class Playlist
    {
        public string Id { get; }               // Spotify Playlist ID
        public double MixRatio { get; set; }    // Mix ratio of the playlist (0.0 - 1.0)
        public int NumPlayed { get; set; }      // Number of times the playlist has been played in current session
        public Playlist(string id, double mixRatio) 
        {
            Id = id;
            if (string.IsNullOrEmpty(id))
                throw new ArgumentException("Playlist ID cannot be null or empty");
            if(id.Length != 22)
                throw new ArgumentException("Playlist ID invalid");

            MixRatio = mixRatio;
            if(mixRatio < 0.0 || mixRatio > 1.0)
                throw new ArgumentException("Mix ratio must be between 0.0 and 1.0");

            NumPlayed = 0;
        }
    }
}
