using System;

namespace PowerNetwork.Core.DataModels {
    public class OverviewBalanceModel {
        public DateTime Date;
        public int Hour;

        public decimal IntensityR;
        public decimal IntensityS;
        public decimal IntensityT;
        
        public long TensionR;
        public long TensionS;
        public long TensionT;
    }
}