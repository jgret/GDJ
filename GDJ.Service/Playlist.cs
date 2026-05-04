namespace GDJ.Service
{
    public class Playlist
    {
        public string Id { get; }
        public string? Name { get; }
        public int CurrentTrackIndex { get; set; }
        public List<string> TrackUris { get; set; }
        public Playlist(string id, string? name, List<string> trackUris)
        {
            if (string.IsNullOrEmpty(id))
                throw new ArgumentException("Playlist ID cannot be null or empty");

            Id = id;
            Name = name;
            CurrentTrackIndex = 0;
            TrackUris = [.. trackUris];
        }

        public int GetNextTrackIndex()
        {
            var idx = CurrentTrackIndex;
            CurrentTrackIndex++;
            if (CurrentTrackIndex >= TrackUris.Count)
                CurrentTrackIndex %= TrackUris.Count;
            return idx;
        }

        public string GetNextTrackUri()
        {
            return TrackUris.ElementAt(GetNextTrackIndex());
        }
    }
}
