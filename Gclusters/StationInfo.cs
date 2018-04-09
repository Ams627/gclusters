namespace Gclusters
{
    using System.Collections.Generic;

    internal class StationInfo
    {
        public string Name { get; set; }
        public string Crs { get; set; }
        public string Nlc { get; set; }
        public List<string> Clusters { get; set; }
        public int Eastings { get; set; }
        public int Northings { get; set; }
        public double Latitude { get; set; }
        public double Longitude { get; set; }
    }
}