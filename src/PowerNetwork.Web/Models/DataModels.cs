using System;

namespace PowerNetwork.Web.Models {
    public class IntensityCsvModel {
        public DateTime Date;
        public int Hour;
        public double R;
        public double S;
        public double T;
        public string Exit;
    }

    public class IntensityOutModel {
        public string date;
        public double r;
        public double s;
        public double t;
    }

    public class HistogramOutModel {
        public int threshold;
        public int r;
        public int s;
        public int t;
    }

    public class FraudOutModel {
        public string date;
        public double ct;
        public double exit;
    }

    public class CtsModel {
        public string center;
        public string code;
        public string othercode;
        public double lng;
        public double lat;
        public string region;
        public string city;

        public string alarm;
    }

    public class CtsRegionModel {
        public string name;
        public double lng;
        public double lat;
        public int count;
    }

    public class CtsCityModel {
        public string name;
        public string region;
        public double lng;
        public double lat;
        public int count;
    }

    public class CtsCenterModel {
        public string name;
        public double lng;
        public double lat;
        public int count;
    }
}