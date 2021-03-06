﻿// Copyright (c) Adrian Sims 2018
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
using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Gclusters
{
    using System.Collections.Generic;
    using Newtonsoft.Json;

    internal class Program
    {
        private static void WriteAllStations()
        {
            var allStationsFile = Path.Combine(Directory.GetCurrentDirectory(), "allstations.js");
            var allStations = Naptan.AllGeoCrs;

            var stationInfoList = from station in allStations
                let nlc = StationCodeConverter.GetNlcFromCrs(station.crs)
                let gridPosition = GridLocation.CreateFromOsGrid(station.eastings, station.northings)
                select new StationInfo
                {
                    Crs = station.crs,
                    Eastings = station.eastings,
                    Northings = station.northings,
                    Nlc = StationCodeConverter.GetNlcFromCrs(station.crs),
                    Name = StationCodeConverter.GetNameFromNlc(nlc),
                    Latitude = gridPosition.Latitude * 180 / Math.PI,
                    Longitude = gridPosition.Longitude * 180 / Math.PI,
                };
            var json = JsonConvert.SerializeObject(stationInfoList, Formatting.Indented);
            using (var strw = new StreamWriter(allStationsFile))
            {
                strw.WriteLine($"allstations= {json}");
            }
        }
        private static void Main(string[] args)
        {
            try
            {
                if (args.Length == 0)
                {
                    throw new Exception("Please supply a cluster ID or a station NLC and optionally -r <directory> to set the fares feed directory.");
                }
                var optionIndex = args.ToList().FindIndex(x => x == "-r");
                string rjisDirectory = @"s:\";
                if (optionIndex >= 0)
                {
                    if (optionIndex == args.Length - 1)
                    {
                        throw new Exception("the -r option must be followed by a directory name.");
                    }
                    rjisDirectory = args[optionIndex + 1];
                }
                var clusterFilenames = Directory.GetFiles(rjisDirectory, "RJFAF*.FSC").ToList();
                clusterFilenames.RemoveAll(x => !Regex.Match(x, @"RJFAF\d\d\d.FSC").Success);
                if (!clusterFilenames.Any())
                {
                    throw new Exception($"No cluster (.FSC) files in the directory {rjisDirectory}");
                }
                if (clusterFilenames.Count() > 1)
                {
                    throw new Exception($"More than one cluster (.FSC) files in the directory {rjisDirectory}");
                }

                WriteAllStations();

                var clusterInfo = new ClusterProcessor(clusterFilenames.First());
                var clusterToGridPoints = new Dictionary<string, List<(int, int)>>();
                var clusterToStationList = new Dictionary<string, List<StationInfo>>();

                for (var i = 0; i < args.Length; i++)
                {
                    if (optionIndex == -1 || (i != optionIndex && i != optionIndex + 1))
                    {
                        var (list, isCluster) = clusterInfo.GetInfo(args[i]);
                        if (list == null)
                        {
                            Console.Error.WriteLine($"{args[i]} nothing found.");
                        }
                        else if (isCluster)
                        {
                            foreach (var station in list)
                            {
                                var name = StationCodeConverter.GetNameFromNlc(station) ?? "name of station unknown.";
                                Console.WriteLine($"{station} {name}");
                                var crs = StationCodeConverter.GetCrsFromNlc(station);
                                if (!string.IsNullOrEmpty(crs))
                                {
                                    var grid = Naptan.GetGridPoint(crs);
                                    if (grid.eastings != int.MaxValue)
                                    {
                                        DictUtils.AddEntryToList(clusterToGridPoints, args[i], grid);
                                        DictUtils.AddEntryToList(clusterToStationList, args[i], new StationInfo
                                        {
                                            Nlc = station,
                                            Crs = StationCodeConverter.GetCrsFromNlc(station),
                                            Eastings = grid.eastings,
                                            Northings = grid.northings,
                                            Name = StationCodeConverter.GetNameFromNlc(station),
                                        });
                                    }
                                }
                            }
                            var directory = Directory.GetCurrentDirectory();
                            var outfile = Path.Combine(directory, "clusterpoints.js");
                            var json1 = JsonConvert.SerializeObject(clusterToGridPoints, Formatting.Indented);
                            var json2 = JsonConvert.SerializeObject(clusterToStationList, Formatting.Indented);
                            using (var strw = new StreamWriter(outfile))
                            {
                                strw.WriteLine("clusterInfo=");
                                strw.WriteLine(json1);
                                strw.WriteLine("stationInfo=");
                                strw.WriteLine(json2);
                            }

                            var outFilename = Path.Combine(directory, "clustersizes.txt");
                            var sortedClusters = clusterInfo.GetAllClusters.OrderByDescending(x => x.Value.Count);
                            using (var str = new StreamWriter(outFilename))
                            {
                                foreach (var cluster in sortedClusters)
                                {
                                    str.WriteLine($"{cluster.Key}, {cluster.Value.Count}");
                                }
                            }
                        }
                        else
                        {
                            foreach (var cluster in list)
                            {
                                Console.WriteLine($"{cluster}");
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                var codeBase = System.Reflection.Assembly.GetExecutingAssembly().CodeBase;
                var progname = Path.GetFileNameWithoutExtension(codeBase);
                Console.Error.WriteLine(progname + ": Error: " + ex.Message);
            }

        }
    }
}
