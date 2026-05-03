namespace GDJ.Service
{
    public class Playlist
    {
        public string Id { get; }
        public string? Name { get; }
        public List<string>? TrackUris { get; set; }
        public Playlist(string id, string? name)
        {
            if (string.IsNullOrEmpty(id))
                throw new ArgumentException("Playlist ID cannot be null or empty");

            Id = id;
            Name = name;
        }
    }
}
