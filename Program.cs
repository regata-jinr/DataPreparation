using System.IO;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Diagnostics;
using System.Data.SqlClient;


namespace DataPreporation
{
    class Program
    {
        static void Main(string[] args)
        {
        }
    }

    static partial class Worker
    {

        static Regex elemPattern = new Regex(@"[A-Z]{1,2}-\d{2,3}[m]{0,1}");
        static Regex elNumPattern = new Regex(@"\d{2,3}");
        static Regex energyPattern = new Regex(@"\d{2,4}.\d{2}");

        static List<TrainModel> dataList = new List<TrainModel>();


        static readonly string Path = @"D:\GoogleDrive\Job\flnp\data\onlyStandarts";

        static List<string> files = Directory.GetFiles(Path).ToList();

        public static void Parse(string file)
        {
            var sw = new Stopwatch();
            sw.Start();
            Console.WriteLine("Start parsing files");

            var rLine = "";
            var ethalon = "";
            double energy = 0.0;
            string element;
            bool startEnergyArea = false;

            var lines = File.ReadAllLines(file, System.Text.Encoding.GetEncoding("windows-1251"));
            foreach (var line in lines)
            {
                element = "";
                if (string.IsNullOrEmpty(line)) continue;
                if (line.Contains("@")) continue;

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
                    Debug.WriteLine($"Parsing of file {file}:");
                    Debug.WriteLine("Start interference peaks parsing:");
                    startEnergyArea = false;
                    break;
                }

                if (startEnergyArea)
                {
                    if (line.Length < 50) continue;
                    if (rLine.Contains("СКО") || rLine.Contains("sigma") || rLine.Contains("X")) continue;

                    TrainModel tr = null;

                    if (elemPattern.IsMatch(rLine.Substring(0, 20)))
                    {
                        element = elemPattern.Match(rLine.Substring(0, 20)).Value;
                        tr = new TrainModel() { Ethalon = ethalon, FileName = int.Parse(System.IO.Path.GetFileName(file)), Nuclid = element, PeakEnergy = Double.Parse(energyPattern.Match(rLine.Substring(0, 32)).Value) };
                    }


                }

            }


            Console.WriteLine($"File {file} processed!");

        } // files loop


    } //Parse()
} // Worker


