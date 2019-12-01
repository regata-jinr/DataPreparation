
namespace DataPreporation
{
   public class TrainModel
    {
        public int    FileName   { get; set; }
        public string Ethalon    { get; set; }
        public double PeakEnergy { get; set; }
        public double Output     { get; set; }
        public string Nuclid     { get; set; }

        public override string ToString()
        {
            return $"{FileName}-{Ethalon}-{PeakEnergy}-{Output}-{Nuclid}";
        }

    }
}
