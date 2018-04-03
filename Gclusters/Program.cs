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
                var clusterNames = Directory.GetFiles(rjisDirectory, "RJFAF*.FSC").ToList();
                clusterNames.RemoveAll(x => !Regex.Match(x, @"RJFAF\d\d\d.FSC").Success);
                if (!clusterNames.Any())
                {
                    throw new Exception($"No cluster (.FSC) files in the directory {rjisDirectory}");
                }
                if (clusterNames.Count() > 1)
                {
                    throw new Exception($"More than one cluster (.FSC) files in the directory {rjisDirectory}");
                }

                var clusterInfo = new ClusterProcessor(clusterNames.First());

                var clusterToGridPoints = new Dictionary<string, List<(int, int)>>();

                for (var i = 0; i < args.Length; i++)
                {
                    if (optionIndex == -1 || (i != optionIndex && i != optionIndex + 1))
                    {
                        var (list, isCluster) = clusterInfo.GetInfo(args[i]);
                        if (list == null)
                        {
                            Console.Error.WriteLine($"{args[i]} nothing found.");
                        }
                        if (isCluster)
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
                                    }
                                }
                            }
                            //var outfile = @"s:\points.js";
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
                var result = JsonConvert.SerializeObject(clusterToGridPoints, Formatting.Indented);
                Console.WriteLine(result);
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
