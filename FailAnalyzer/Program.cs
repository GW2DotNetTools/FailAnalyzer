using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;

namespace FailAnalyzer
{
    public class Program
    {
        public static void Main(string[] args)
        {
            if (args == null)
            {
                Console.WriteLine("Please enter a folder with evtc files.");
                Console.ReadLine();
                return;
            }

            if (args.Length != 1)
            {
                Console.WriteLine("Incorrect number of input parameters. Try again with a single folder path.");
                Console.ReadLine();
                return;
            }

            if (!Directory.Exists(args[0]))
            {
                Console.WriteLine("The Directory does not exist.");
                Console.ReadLine();
                return;
            }

            Run(args[0]);
        }

        private static void Run(string folderPath)
        {
            foreach (var filePath in Directory.GetFiles(folderPath, "*.evtc"))
            {
                var process = Process.Start("raid_heroes.exe", filePath);
                process.WaitForExit();
            }

            List<Player> totalFails = new List<Player>();
            foreach (var htmlFile in Directory.GetFiles(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "*.html"))
            {
                var fileContent = File.ReadLines(htmlFile);

                var analyzedFails = Analyze(fileContent);
                MergeFails(totalFails, analyzedFails);
            }

            WriteFailBoard(totalFails);
        }

        private static void MergeFails(List<Player> totalFails, List<Player> analyzedFails)
        {
            foreach (var analyzedFail in analyzedFails)
            {
                var result = totalFails.FirstOrDefault(x => x.Name == analyzedFail.Name);
                if (result != null)
                {
                    foreach (var playerFails in analyzedFail.Fails)
                    {
                        if (result.Fails.ContainsKey(playerFails.Key))
                        {
                            result.Fails[playerFails.Key] += playerFails.Value;
                        }
                        else
                        {
                            result.Fails.Add(playerFails.Key, playerFails.Value);
                        }
                    }
                }
                else
                {
                    totalFails.Add(analyzedFail);
                }
            }
        }

        private static void WriteFailBoard(List<Player> totalFails)
        {
            List<string> content = new List<string>
            {
                "<link rel=\"stylesheet\" href=\"https://maxcdn.bootstrapcdn.com/bootstrap/4.0.0-alpha.6/css/bootstrap.min.css\" integrity=\"sha384-rwoIResjU2yc3z8GV/NPeZWAv56rSmLldC3R/AZzGRnGxQQKnKkoFVhFQhNUwEyJ\" crossorigin=\"anonymous\">",
                "<script src=\"https://maxcdn.bootstrapcdn.com/bootstrap/4.0.0-alpha.6/js/bootstrap.min.js\" integrity=\"sha384-vBWWzlZJ8ea9aCX4pEW3rVHjgjt7zpkNpZk+02D9phzyeVkE+jo0ieGizqPLForn\" crossorigin=\"anonymous\"></script>",
                "<body><table id=\"example\" class=\"table table-striped table-bordered\"><thead><tr><th>Name</th><th>Failed Mechanic</th><th>Amount of Fails</th></tr></thead><tbody>"
            };

            foreach (var fails in totalFails)
            {
                foreach (var fail in fails.Fails)
                {
                    content.Add($"<tr><td>{fails.Name}</td><td>{fail.Key}</td><td>{fail.Value}</td></tr>");
                }
            }
            content.Add("</tbody></table></body>");
            File.WriteAllLines("TotalFails.html", content);
        }

        private static List<Player> Analyze(IEnumerable<string> lines)
        {
            List<Player> players = new List<Player>();

            string failedId = string.Empty;
            bool skipLines = false;
            int skipLinesCount = 0;
            SearchTerm detection = SearchTerm.PlayerName;

            foreach (var item in lines)
            {
                if (skipLines && skipLinesCount != 0)
                {
                    if (item.Contains("<li class=\"active\"><a data-toggle=\"tab\" href=") && item.Contains("#mf"))
                    {
                        var b = item.Substring(item.IndexOf("#mf"));
                        failedId = b.Remove(b.IndexOf("\""));
                    }

                    skipLinesCount--;

                    if (skipLinesCount == 0 && !item.Contains("mf"))
                    {
                        detection = SearchTerm.PlayerName;
                    }
                    continue;
                }

                if (item.Contains(failedId.Replace("#mf", "mn")))
                {
                    detection = SearchTerm.PlayerName;

                    if (players.Count == 10)
                    {
                        break;
                    }
                }

                if (detection == SearchTerm.PlayerName && item.Contains("<div class=\"row\"><div class=\"col-sm-8\" style=\"min-width: 730px;\"><ul class=\"nav nav-tabs\"><li class=\"active\"><a data-toggle=\"tab\""))
                {
                    var index = item.IndexOf("height=\"18\" width=\"18\">") + 23;
                    var broken = item.Substring(index, 50);
                    string name = broken.Remove(broken.IndexOf("<")).Trim();
                    players.Add(new Player(name));
                    skipLines = true;
                    skipLinesCount = 6;
                    detection = SearchTerm.NextPlayer;
                }

                if (detection == SearchTerm.NextPlayer && item.Contains("<table class=\"table table-condensed\"><tr><td class=\"text-left\">"))
                {
                    var b = item.Replace("<table class=\"table table-condensed\"><tr><td class=\"text-left\">", "");
                    string failname = b.Remove(b.IndexOf("<"));
                    string failcount = b.Substring(b.IndexOf("</td>"), 20).Replace("tr", "").Replace("td", "").Replace("<", "").Replace(">", "").Replace("/", "");
                    players.Last().Fails.Add(failname, Convert.ToInt32(failcount));
                }
            }

            return players;
        }
    }
}
