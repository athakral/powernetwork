using System;

namespace PowerNetwork.Web.Models {
    public class IntensityCsvModel {
        public DateTime Date { get; set; }
        public int Hour { get; set; }
        public double R { get; set; }
        public double S { get; set; }
        public double T { get; set; }
        public string Exit { get; set; }
    }

    public class IntensityOutModel {
        public string date { get; set; }
        public double r { get; set; }
        public double s { get; set; }
        public double t { get; set; }
    }

    public class HistogramOutModel {
        public int threshold { get; set; }
        public int r { get; set; }
        public int s { get; set; }
        public int t { get; set; }
    }

    public class FraudOutModel {
        public string date { get; set; }
        public double ct { get; set; }
        public double exit { get; set; }
    }

    public class CtsModel {
        public string center { get; set; }
        public string code { get; set; }
        public string othercode { get; set; }
        public double lng { get; set; }
        public double lat { get; set; }
        public string region { get; set; }
        public string city { get; set; }

        public string alarm { get; set; }
    }

    public class CtsRegionModel {
        public string name { get; set; }
        public double lng { get; set; }
        public double lat { get; set; }
        public int count { get; set; }
    }

    public class CtsCityModel {
        public string name { get; set; }
        public string region { get; set; }
        public double lng { get; set; }
        public double lat { get; set; }
        public int count { get; set; }
    }

    public class CtsCenterModel {
        public string name { get; set; }
        public double lng { get; set; }
        public double lat { get; set; }
        public int count { get; set; }
    }
}