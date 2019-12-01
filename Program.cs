using System.IO;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Diagnostics;
using System.Data;
using System.Text;
using Microsoft.EntityFrameworkCore;

namespace DataPreparation
{
    class Program
    {
        static void Main(string[] args)
        {
            if (!args.Any()) { Console.WriteLine("Specify the name of directory with RPT files!"); return; }

            var sw = new Stopwatch();
            sw.Start();

            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

                if (!Directory.Exists(args[0]))
                {
                    Console.WriteLine($"Directory'{args[0]}' doesn't exist! Check the file!");
                    return;
                }

            Console.WriteLine($"Start parsing files in directory - '{args[0]}'");
            foreach (var file in Directory.GetFiles(args[0], "*.rpt"))
            {
                Worker.Parse(file);

                if (Worker.dataList.Any())
                {
                        //Worker.dataList.ForEach(d => Console.WriteLine(d));
                        using (var uc = new TrainContext())
                        {
                            try
                            {
                                uc.TrainData.AddRange(Worker.dataList);
                                uc.SaveChanges();
                            }
                            catch (DbUpdateException db)
                            {
                                Console.WriteLine($"Error occured during saving data from the file '{file}' to DB:");
                                Console.WriteLine(db.Message);
                                Console.WriteLine(db.InnerException.Message);
                            }
                        }
                }
                Worker.dataList.Clear();
            }

            sw.Stop();
            Console.WriteLine($"Parsing is finished. Elapsed time: {sw.ElapsedMilliseconds} ms");
        }
    }

    public static partial class Worker
    {

        static Regex elemPattern         = new Regex(@"[A-Z]{1,2}-\d{2,3}[m]{0,1}");
        static Regex energyPattern       = new Regex(@"\d{2,4}.\d{2}");
        static Regex energyOutputPattern = new Regex(@"\d{1,3}.\d{2}");
        static Regex fileIDPattern       = new Regex(@"\d{7}");
        static Regex datePattern         = new Regex(@"\d{2}.\d{2}.\d{2}");

        public static List<TrainModel> dataList = new List<TrainModel>();

        public static void Parse(string file)
        {
            try
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

                    if (rLine.StartsWith("Имя образца", StringComparison.OrdinalIgnoreCase) || rLine.StartsWith("Sample Title", StringComparison.OrdinalIgnoreCase)) ethalon = rLine.Replace(" ", "", StringComparison.OrdinalIgnoreCase).Split(':')[1];

                    if (string.Equals(rLine.Trim(), "Нуклид Достоверность Энергия, Выход,   Активность,  Погрешность", StringComparison.OrdinalIgnoreCase) || string.Equals(rLine.Trim(), "Nuclide    Id       Energy    Yield     Activity     Activity", StringComparison.OrdinalIgnoreCase))
                    {
                        Debug.WriteLine($"Parsing of file {file}:");
                        Debug.WriteLine("Start identified peaks parsing:");
                        startEnergyArea = true;
                    }


                    if (string.Equals(rLine.Trim(), "Нуклид Достоверность Средневзвешенная  Погрешность", StringComparison.OrdinalIgnoreCase) || string.Equals(rLine.Trim(), "Nuclide       Wt mean         Wt mean", StringComparison.OrdinalIgnoreCase))
                    {
                        startEnergyArea = false;
                        break;
                    }

                    if (startEnergyArea)
                    {
                        if (rLine.Contains("СКО", StringComparison.OrdinalIgnoreCase) || rLine.Contains("sigma", StringComparison.OrdinalIgnoreCase) || rLine.Contains("X", StringComparison.OrdinalIgnoreCase)) continue;

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

                            tr = new TrainModel() { Ethalon = ethalon, FileName = fileNameTmp, Nuclid = NuclidTmp, PeakEnergy = PeakEnergyTmp, Output = OutPutEnergyTmp };

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
            }
            catch (Exception e)
            {
                Console.WriteLine($"Error occured during parsing the file '{file}'");
                Console.WriteLine(e.Message);
            }
        } 
    } //Parse()
} // Worker


