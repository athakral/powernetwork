namespace PowerNetwork.Core.DataModels {
    public class CtTipoModel {
        public string Code { get; set; }

        public double TeleLevel { get; set; }
        public int T5 { get; set; }
        public int T4 { get; set; }
        public int To { get; set; }

        public double T5Percent { get; set; }
        public double T4Percent { get; set; }
        public double T3Percent { get; set; }
        public double T2Percent { get; set; }
        public double T1Percent { get; set; }

        public double Other { get; set; }
        public double Assets { get; set; }
    }
}