// Copyright (c) Adrian Sims 2018
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in all
// copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
// SOFTWARE.
namespace Gclusters
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Globalization;
    using System.IO;
    internal class ClusterProcessor
    {
        private readonly string filename;
        private readonly Dictionary<string, List<string>> stationToClusters = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, List<string>> clusterToStations = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
        public ClusterProcessor(string filename)
        {
            this.filename = filename;
            int linenumber = 0;
            foreach (var line in File.ReadLines(filename).Where(x => x.Length > 0 && x[0] != '/'))
            {
                if (!DateTime.TryParseExact(line.Substring(9, 8), "ddMMyyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out var endDate))
                {
                    throw new Exception($"invalid date {line.Substring(9, 8)} in cluster file {filename} at line {linenumber + 1}");
                }
                if (!DateTime.TryParseExact(line.Substring(17, 8), "ddMMyyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out var startDate))
                {
                    throw new Exception($"invalid date {line.Substring(17, 8)} in cluster file {filename} at line {linenumber + 1}");
                }
                if (endDate < startDate)
                {
                    throw new Exception($"End date before start date in cluster file {filename} at line {linenumber + 1}");
                }
                var clusterId = line.Substring(1, 4);
                var stationNlc = line.Substring(5, 4);

                if (endDate >= DateTime.Today)
                {
                    DictUtils.AddEntryToList(stationToClusters, stationNlc, clusterId);
                    DictUtils.AddEntryToList(clusterToStations, clusterId, stationNlc);
                }

                linenumber++;
            }
        }

        public (List<string> list, bool isCluster) GetInfo(string code)
        {
            if (stationToClusters.TryGetValue(code, out var clusters))
            {
                return (clusters, false);
            }
            if (clusterToStations.TryGetValue(code, out var stations))
            {
                return (stations, true);
            }
            return (null, false);
        }

        public Dictionary<string, List<string>> GetAllClusters => clusterToStations;
    }
}