using System.IO;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Diagnostics;
using System.Data.SqlClient;
using System.Text;

namespace DataPreporation
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine(args[0]);
            return;
            if (!args.Any()) { Console.WriteLine("Specify the name of RPT file!"); return; } 
            
            if (!File.Exists(args[0]))
            {
                Console.WriteLine($"File '{args[0]}' doesn't exist! Check the file!");
                return;
            }

            if (Path.GetExtension(args[0]).ToLower() != ".rpt")
            {
                Console.WriteLine($"File should be a report file from GENIE2K and has '.rpt' extension.");
                return;
            }

            var sw = new Stopwatch();
            sw.Start();
            Console.WriteLine($"Start parsing file - '{args[0]}'");
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            Worker.Parse(args[0]);

            if (Worker.dataList.Any())
                Worker.dataList.ForEach(d => Console.WriteLine(d));

            sw.Stop();

            Console.WriteLine($"Parsing is finished. Elapsed time: {sw.ElapsedMilliseconds / 1000} sec");
        }
    }

    static partial class Worker
    {

        static Regex elemPattern         = new Regex(@"[A-Z]{1,2}-\d{2,3}[m]{0,1}");
        static Regex energyPattern       = new Regex(@"\d{2,4}.\d{2}");
        static Regex energyOutputPattern = new Regex(@"\d{1,3}.\d{2}");
        static Regex fileIDPattern       = new Regex(@"\d{7}");
        static Regex datePattern         = new Regex(@"\d{2}.\d{2}.\d{2}");

        public static List<TrainModel> dataList = new List<TrainModel>();

        public static void Parse(string file)
        {
            string rLine;
            string ethalon = "";
            bool startEnergyArea = false;
            TrainModel tr = null;

            var lines = File.ReadAllLines(file, System.Text.Encoding.GetEncoding("windows-1251"));
            foreach (var line in lines)
            {
                if (string.IsNullOrEmpty(line)) continue;

                rLine = line;
                Debug.WriteLine($"<====Current line====>");
                Debug.WriteLine(rLine);
                Debug.WriteLine($"<====================>");

                if (rLine.StartsWith("Имя образца") || rLine.StartsWith("Sample Title")) ethalon = rLine.Replace(" ", "").Split(':')[1];

                if (string.Equals(rLine.Trim(), "Нуклид Достоверность Энергия, Выход,   Активность,  Погрешность") || string.Equals(rLine.Trim(), "Nuclide    Id       Energy    Yield     Activity     Activity"))
                {
                    Debug.WriteLine($"Parsing of file {file}:");
                    Debug.WriteLine("Start identified peaks parsing:");
                    startEnergyArea = true;
                }


                if (string.Equals(rLine.Trim(), "Нуклид Достоверность Средневзвешенная  Погрешность") || string.Equals(rLine.Trim(), "Nuclide       Wt mean         Wt mean"))
                {
                    startEnergyArea = false;
                    break;
                }

                if (startEnergyArea)
                {
                    if (rLine.Contains("СКО") || rLine.Contains("sigma") || rLine.Contains("X")) continue;

                    if (datePattern.IsMatch(line)) continue;

                    if (!elemPattern.IsMatch(line) && !energyPattern.IsMatch(line)) continue;

                    if (elemPattern.IsMatch(rLine.Substring(0, 20)) && !dataList.Where(d => d.Nuclid == elemPattern.Match(rLine.Substring(0, 20)).Value).Any())
                    {
                        var fileNameTmp = int.Parse(System.IO.Path.GetFileName(fileIDPattern.Match(file).Value));
                        var NuclidTmp = elemPattern.Match(rLine.Substring(0, 20)).Value;
                        var PeakEnergyTmp = 0.0;
                        var OutPutEnergyTmp = 0.0;
                        if (line.Length > 50)
                        {
                            PeakEnergyTmp = double.Parse(energyPattern.Match(rLine.Substring(0, 32)).Value);
                            if (!line.Contains("@"))
                                OutPutEnergyTmp = double.Parse(energyOutputPattern.Match(rLine.Substring(32, 10)).Value);
                        }

                        tr = new TrainModel() { Ethalon = ethalon, FileName = fileNameTmp , Nuclid = NuclidTmp, PeakEnergy = PeakEnergyTmp, Output = OutPutEnergyTmp };

                            dataList.Add(tr);
                    }
                    else
                    {
                        if (line.Length < 50 || line.Contains("@")) continue;
                        var PeakEnergyTmp = double.Parse(energyPattern.Match(rLine.Substring(0, 32)).Value);
                        var OutPutEnergyTmp = double.Parse(energyOutputPattern.Match(rLine.Substring(32, 10)).Value);
                        if (dataList.Last().Output < OutPutEnergyTmp)
                        {
                            dataList.Last().Output = OutPutEnergyTmp;
                            dataList.Last().PeakEnergy = PeakEnergyTmp;
                        }
                    }
                }
            } // files loop

            Console.WriteLine($"File {file} processed!");

        } 


    } //Parse()
} // Worker


