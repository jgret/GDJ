namespace GDJ.Service
{
    public class PlaylistMix : Playlist
    {
        public double MixRatio { get; set; }    // Mix ratio of the playlist (0.0 - 1.0)
        public int NumPlayed { get; set; }      // Number of times the playlist has been played in current session
        public PlaylistMix(string id, double mixRatio, string? name = null) : base(id)
        {
            MixRatio = mixRatio;
            if(mixRatio < 0.0 || mixRatio > 1.0)
                throw new ArgumentException("Mix ratio must be between 0.0 and 1.0");

            NumPlayed = 0;
        }
    }
}
