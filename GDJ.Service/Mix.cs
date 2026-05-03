namespace GDJ.Service
{
    public class Mix
    {
        public string Id { get; set; }
        public string? Name { get; set; }
        public double MixRatio { get; set; }
        public int NumPlayed { get; set; }
        public Mix(string id, double mixRatio, string? name = null)
        {
            MixRatio = mixRatio;
            if(mixRatio < 0.0 || mixRatio > 1.0)
                throw new ArgumentException("Mix ratio must be between 0.0 and 1.0");

            NumPlayed = 0;
            Name = name;
            Id = id;
        }
    }
}
