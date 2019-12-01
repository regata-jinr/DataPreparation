using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DataPreparation
{
    [Table("TrainData")]
    public class TrainModel
    {
        [Key]
        public int    Id   { get; set; }
        public int    FileName   { get; set; }
        public string Ethalon    { get; set; }
        public double PeakEnergy { get; set; }
        public double Output     { get; set; }
        public string Nuclid     { get; set; }

        public override string ToString()
        {
            return $"{FileName};{Ethalon};{PeakEnergy};{Output};{Nuclid}";
        }

    }
}
