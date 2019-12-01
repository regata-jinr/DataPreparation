using System.IO;
using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Diagnostics;
//using System.Data.SqlClient;

namespace DataPreparation
{
    static partial class Worker
    {
        public static void ParseParallel()
        {
            var sw = new Stopwatch();
            sw.Start();
            Console.WriteLine("Start parsing files");

            var elemPattern = new Regex(@"[A-Z]{1,2}-\d{2,3}[m]{0,1}");
            var reqPattern = new Regex(@"\d.\d{3}");
            var actPattern = new Regex(@"\d.\d{6}E[+-]\d{3}");
            var elNumPattern = new Regex(@"\d{2,3}");

            //var enOutputPattern = new Regex(@"\d{1,5}.\d{2}");
            //var mdaLineAndActivPattern = new Regex(@"\d.\d{4}E[+-]\d{3}");
            var mdaNuclPattern = new Regex(@"\d.\d{2}E[+-]\d{3}");

            var finalDict = new ConcurrentDictionary<string, List<double>>();

            var files = Directory.GetFiles(@"D:\GoogleDrive\Job\flnp\data\onlyStandarts");
            var FileList = files.ToList();
            int bcnt = FileList.Count;

            Parallel.ForEach(files, new Action<string, ParallelLoopState>((string file, ParallelLoopState state) =>
            {

                var rLine = "";
                double activity = 0.0, req = 0.0, err = 0.0, mda = 0.0;
                var handler = "";
                var element = "";
                var title = "";
                var mesType = "";
                var height = "";

                double liveTime = 0.0;
                double realTime = 0.0;
                double deadTime = 0.0;
                bool startActivityArea = false;
                bool startMDAArea = false;
                var fdKey = "";

                // if (!file.Contains("Pavlov_7005058")) return;
                handler = file.Split('_')[0];
                var lines = File.ReadAllLines(file, System.Text.Encoding.GetEncoding("windows-1251"));
                foreach (var line in lines)
                {
                    rLine = line;
                    Debug.WriteLine($"<====Current line====>");
                    Debug.WriteLine(rLine);
                    Debug.WriteLine($"<====================>");

                    if (string.IsNullOrEmpty(line)) continue;
                    if (rLine.StartsWith("Имя образца") || rLine.StartsWith("Sample Title")) title = rLine.Replace(" ", "").Split(':')[1];
                    if (rLine.StartsWith("Тип") || rLine.StartsWith("Sample Type")) mesType = rLine.Replace(" ", "").Split(':')[1];
                    if (rLine.StartsWith("Геометрия") || rLine.StartsWith("Sample Geometry")) height = rLine.Replace(" ", "").Split(':')[1];
                    if (rLine.StartsWith("Живое время") || rLine.StartsWith("Live Time"))
                    {
                        rLine = rLine.Replace(" ", "").Split(':')[1].Replace("seconds", "").Replace("секунд", "");
                        if (!double.TryParse(rLine, out liveTime)) liveTime = -1.0;
                    }
                    if (rLine.StartsWith("Реальное время") || rLine.StartsWith("Real Time"))
                    {
                        rLine = rLine.Replace(" ", "").Split(':')[1].Replace("seconds", "").Replace("секунд", "");
                        if (!double.TryParse(rLine, out realTime)) realTime = -1.0;
                    }

                    if (rLine.StartsWith("Мёртвое время") || rLine.StartsWith("Dead Time"))
                    {
                        rLine = rLine.Replace(" ", "").Split(':')[1].Replace("%", "");
                        if (!double.TryParse(rLine, out deadTime)) deadTime = -1.0;
                    }


                    if (string.Equals(rLine.Trim(), "Нуклид Достоверность Средневзвешенная  Погрешность") || string.Equals(rLine.Trim(), "Nuclide       Wt mean         Wt mean"))
                    {
                        Debug.WriteLine($"Parsing of file {file}:");
                        Debug.WriteLine($"title = {title}");
                        Debug.WriteLine($"mesType = {mesType}");
                        Debug.WriteLine($"height = {height}");
                        Debug.WriteLine($"liveTime = {liveTime}");
                        Debug.WriteLine($"realTime = {realTime}");
                        Debug.WriteLine($"deadTime = {deadTime}");
                        Debug.WriteLine("Start interference peaks parsing:");
                        startActivityArea = true;
                    }

                    if (string.Equals(rLine.Replace(" ", "").Trim(), "НуклидЭнергия,Выход,МДАлинии,МДАнуклида,Активность,") || string.Equals(rLine.Replace(" ", "").Trim(), "NuclideEnergyYieldLineMDANuclideMDAActivity"))
                    {
                        Debug.WriteLine("Start mda area parsing:");
                        startMDAArea = true;

                    }

                    if (string.Equals(rLine.Replace("*", "").Trim(), "НЕИДЕНТИФИЦИРОВАННЫЕ ПИКИ") || string.Equals(rLine.Replace("*", "").Trim(), "U N I D E N T I F I E D   P E A K S"))
                    {
                        startActivityArea = false;
                    }

                    if (startActivityArea)
                    {
                        if (line.Length < 40) continue;

                        if (rLine.Contains("СКО") || rLine.Contains("sigma") || rLine.Contains("X")) continue;

                        element = elemPattern.Match(rLine.Substring(0, 20)).Value;
                        if (string.IsNullOrEmpty(element)) continue;
                        if (string.IsNullOrWhiteSpace(element)) continue;
                        fdKey = $"{Path.GetFileNameWithoutExtension(file)}_{element}";
                        if (!double.TryParse(reqPattern.Match(rLine).Value, out req))
                        {
                            Debug.WriteLine($"Problem with pasrsing current line for veracity value {reqPattern.Match(rLine).Value}");
                            req = -1.0;
                        }
                        if (!double.TryParse(actPattern.Match(rLine).Value, out activity))
                        {
                            Debug.WriteLine($"Problem with pasrsing current line for activity value {actPattern.Match(rLine).Value}");
                            activity = -1.0;
                        }

                        if (!double.TryParse(reqPattern.Match(rLine, 44).Value, out err))
                        {
                            Debug.WriteLine($"Problem with pasrsing current line for activity error value {actPattern.Match(rLine, 44).Value}");
                            err = -1.0;
                        }
                        try
                        {
                            finalDict.TryAdd(fdKey, new List<double> { req, activity, err });
                        }
                        catch (ArgumentException) { }
                    }

                    if (startMDAArea)
                    {
                        element = elemPattern.Match(rLine).Value;
                        if (string.IsNullOrEmpty(element)) continue;
                        if (!line.TrimStart().StartsWith("+")) continue;
                        fdKey = $"{Path.GetFileNameWithoutExtension(file)}_{element}";
                        if (!double.TryParse(mdaNuclPattern.Match(rLine, 53).Value, out mda))
                        {
                            Debug.WriteLine($"Problem with pasrsing current line for mda of nuclid value {mdaNuclPattern.Match(rLine).Value}");
                            mda = -1.0;
                        }
                        if (finalDict.Keys.Contains(fdKey)) finalDict[fdKey].Add(mda);
                    }

                }
                FileList.Remove(file);
                Console.WriteLine($"File {file} processed!");
                Console.WriteLine($"{FileList.Count} files left");
                //  if (bcnt - FileList.Count == 10) state.Break();
            }));  // files loop


            using (TextWriter writer = File.CreateText(@"D:\GoogleDrive\Job\flnp\data\combinedStandardsPar.csv"))
            {
                writer.WriteLine("file\thandler\treq\tact\terr\tmda:");
                var wLine = "";
                foreach (var key in finalDict)
                {
                    wLine = $"{key.Key.Split('_')[0]}\t{key.Key.Split('_')[1]}\t";
                    foreach (var el in key.Value)
                    {
                        wLine = ($"{wLine}{el}\t");
                    }
                    writer.WriteLine(wLine.Substring(0, wLine.Length - 2));
                }
            }

            sw.Stop();
            Console.WriteLine($"Elapsed time: {sw.ElapsedMilliseconds / 1000} sec. Press any button...");
            Console.ReadKey();

        } //parse





        //public static void DownloadEthRPT()
        //{

        //    var rLine = "";
        //    var fName = "";
        //    var fileNames = new List<string>();
        //    //using (TextReader reader = File.OpenText(@"D:\GoogleDrive\Job\flnp\dev\DownloadEthalons\AllStandartsList.txt"))
        //    //{
        //    //    while ((rLine = reader.ReadLine()) != null)
        //    //        fileNames.Add(rLine);
        //    //}

        //    var sw = new Stopwatch();
        //    sw.Start();
        //    Console.WriteLine($"Start RPTs!");
        //    using (var sftp = new SftpClient(Properties.Settings.Default.host, Properties.Settings.Default.user, Properties.Settings.Default.psw))
        //    {
        //        sftp.Connect();
        //        //Parallel.ForEach(fileNames, new Action<string, ParallelLoopState>((string fileName, ParallelLoopState state) =>
        //        //{ }));
        //        foreach (var fileName in fileNames)
        //        {
        //            try
        //            {

        //                DirectoryInfo di = Directory.CreateDirectory(@"D:\onlyStandarts1");
        //                fName = $"{di.FullName}\\{fileName.Split('/').First()}_{fileName.Split('/').Last()}";
        //                if (File.Exists(fName)) fName = $"{fName}(1)";
        //                using (var file = File.Create(fName))
        //                {
        //                    sftp.DownloadFile($"DataStorage/{fileName}", file);
        //                }
        //                Console.WriteLine($"DataStorage/{fileName} ==> {fName}");

        //            }
        //            catch (Exception ex)
        //            {
        //                Console.WriteLine($"{ex.ToString()}");

        //                //state.Break();
        //            }
        //        }

        //    }


        //    sw.Stop();
        //    Console.WriteLine($"Elapsed time: {Convert.ToDouble(sw.ElapsedMilliseconds) / 1000} sec. Press any button...");
        //    Console.ReadLine();

        //}


        //public static void DownloadEthSpectra()
        //{

        //    var sw = new Stopwatch();
        //    sw.Start();
        //    Console.WriteLine($"Start downloading!");


        //    var fileNames = new List<string>();

        //    using (SqlConnection con = new SqlConnection(Properties.Settings.Default.ConnectionString))
        //    {
        //        con.Open();
        //        Console.WriteLine($"State connection - {con.State.ToString()}");
        //        using (SqlCommand sCmd = new SqlCommand(Properties.Settings.Default.Query, con))
        //        {
        //            SqlDataReader reader = sCmd.ExecuteReader();
        //            while (reader.Read())
        //            {
        //                fileNames.Add(reader.GetString(6));
        //            }
        //        }
        //    }
        //    using (var sftp = new SftpClient(Properties.Settings.Default.host, Properties.Settings.Default.user, Properties.Settings.Default.psw))
        //    {
        //        sftp.Connect();
        //        using (var ssh = new SshClient(Properties.Settings.Default.host, Properties.Settings.Default.user, Properties.Settings.Default.psw))
        //        {
        //            ssh.Connect();
        //            Parallel.ForEach(fileNames, new Action<string, ParallelLoopState>((string fileName, ParallelLoopState state) =>
        //            {
        //                var cl = ssh.RunCommand($"find Spectra/ -name {fileName}* | wc -l").Result;
        //                var fl = ssh.RunCommand($"find Spectra/ -name {fileName}*").Result;
        //                var fullFileName = "";
        //                if (Convert.ToInt32(cl) > 1)
        //                {
        //                    Console.WriteLine($"Result of searchings: {fileName}");
        //                    Console.WriteLine($"{fl}");
        //                    fullFileName = fl.Split('\n')[1];
        //                }
        //                else if (fl.Length == 0) { Console.WriteLine($"File not found: {fileName}"); return; }
        //                else fullFileName = fl.Substring(0, fl.Length - 1);
        //                Console.WriteLine($"Will be download file: {fullFileName} to:");
        //                try
        //                {
        //                    var dFile = $"D:\\{Path.GetDirectoryName(fullFileName)}\\{Path.GetFileName(fullFileName)}";
        //                    Console.WriteLine($"{dFile}");

        //                    DirectoryInfo di = Directory.CreateDirectory($"D:\\{ Path.GetDirectoryName(fullFileName)}");
        //                    using (var file = File.Create(dFile))
        //                    {
        //                        sftp.DownloadFile(fullFileName, file);
        //                    }
        //                }
        //                catch (Exception ex)
        //                {
        //                    Console.WriteLine($"{ex.ToString()}");
        //                    state.Break();
        //                }
        //            }));
        //        }
        //    }
        //    sw.Stop();
        //    Console.WriteLine($"Elapsed time: {sw.ElapsedMilliseconds * 1000} sec. Press any button...");
        //    Console.ReadLine();
        //}
    }
}
