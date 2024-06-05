using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;

namespace GliderGunBackup
{
    class Program
    {
        Dictionary<int, int> ParseCosts(string costsS)
        {
            var costs = new Dictionary<int, int>();
            if (costsS != null)
            {
                var lines = costsS.Split('\n');
                for (int i = 1; i < lines.Length - 1; i++)
                {
                    var spl = lines[i].Split(',');
                    int period = int.Parse(spl[0].Trim('"').Split('_')[1]);
                    int area = int.Parse(spl[1].Trim('"'));
                    costs.Add(period, area);
                }
            }
            return costs;
        }

        Program()
        {
            const string branch = "main";
            const string remote = "origin";
            const string userName = "Vort";
            const string repositoryName = "glider_guns";
            string token = File.ReadAllText("token.txt");
            string repositoryURL = $"https://{userName}:{token}@github.com/{userName}/{repositoryName}.git";

            const string catagolueURL = "https://catagolue.hatsya.com/";

            DateTime now = DateTime.UtcNow;
            Console.WriteLine($"Update process started {now}");
            if (!Directory.Exists(repositoryName))
            {
                Console.Write($"Initializing repository...");
                Directory.CreateDirectory(repositoryName);
                Process.Start("git", $"init {repositoryName} -b {branch}").WaitForExit();
                Process.Start("git", $"-C {repositoryName} remote add {remote} {repositoryURL}").WaitForExit();
                Console.WriteLine(" Done");
            }
            WebClient wc = new WebClient();
            int gunsDownloaded = 0;
            foreach (string tab in new string[] { "gun", "guntrue" })
            {
                Console.Write($"Downloading costs for '{tab}'...");
                string tabPath = Path.Combine(repositoryName, tab);
                if (!Directory.Exists(tabPath))
                    Directory.CreateDirectory(tabPath);

                string costsPath = Path.Combine(tabPath, $"costs.txt");
                string costsS1 = null;
                if (File.Exists(costsPath))
                    costsS1 = File.ReadAllText(costsPath);
                string costsS2 = wc.DownloadString(
                    $"{catagolueURL}textcensus/b3s23/synthesis-costs/{tab}");
                Console.WriteLine(" Done");
                if (costsS1 == costsS2)
                    continue;

                Dictionary<int, int> costs1 = ParseCosts(costsS1);
                Dictionary<int, int> costs2 = ParseCosts(costsS2);

                Console.Write($"Downloading patterns from '{tab}'...");
                foreach (int period in costs2.Keys)
                {
                    if (!costs1.ContainsKey(period) ||
                        costs2[period] < costs1[period])
                    {
                        string rle = wc.DownloadString(
                            $"{catagolueURL}textsamples/{tab}_{period}/b3s23/synthesis");
                        File.WriteAllText(Path.Combine(tabPath, $"{tab}_{period}.rle"), rle);
                        Console.Write('.');
                        gunsDownloaded++;
                    }
                }
                Console.WriteLine(" Done");

                File.WriteAllText(costsPath, costsS2);
            }
            Console.WriteLine($"Guns downloaded: {gunsDownloaded}");
            if (gunsDownloaded != 0)
            {
                Console.Write($"Updating repository...");
                Process.Start("git", $"-C {repositoryName} add -A").WaitForExit();
                Process.Start("git", $"-C {repositoryName} commit -m \"{gunsDownloaded} guns modified\"").WaitForExit();
                Process.Start("git", $"-C {repositoryName} push {remote} {branch}").WaitForExit();
                Console.WriteLine(" Done");
            }
            Console.WriteLine($"Update process finished {DateTime.UtcNow}");
        }

        static void Main(string[] args)
        {
            System.Threading.Thread.CurrentThread.CurrentCulture =
                System.Globalization.CultureInfo.InvariantCulture;
            new Program();
        }
    }
}
