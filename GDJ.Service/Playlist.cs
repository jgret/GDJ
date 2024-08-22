namespace GDJ.Service
{
    public class Playlist
    {
        public string? Name { get; set; }
        public bool Enabled { get; set; }
        public List<string>? TrackIds { get; set; }
        public string Id { get; }
        public Playlist(string id, string? name = null)
        {
            if (string.IsNullOrEmpty(id))
                throw new ArgumentException("Playlist ID cannot be null or empty");
            if (id.Length != 22)
                throw new ArgumentException("Playlist ID invalid");

            Id = id;
            Name = name;
            Enabled = false;
        }
    }
}
