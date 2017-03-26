using System;
using System.Collections.Generic;
using PowerNetwork.Core.DataModels;

namespace PowerNetwork.Core.Helpers {
    public interface IDataService {

        #region Map

        List<CtTipoModel> Cts(double x1, double x2, double y1, double y2);
        List<MeterModel> Meters(string groupCode);
        List<MeterGroupModel> MeterGroups(string otherCode);

        #endregion

        #region Utils

        string CtFromMeterCode(string meterCode);
        string MaxExit(string code);
        CtTipoModel CtTipo(string code);

        #endregion

        #region Alarms

        List<AlarmModel> OverloadAlarms(int teleLevel0, int teleLevel1, int tipo);
        List<AlarmModel> UnbalanceAlarms(int teleLevel0, int teleLevel1, int tipo);
        List<AlarmModel> FraudAlarms(int teleLevel0, int teleLevel1, int tipo);

        #endregion

        #region Graphs

        List<OverviewBalanceModel> OverviewBalance(string code, DateTime from, DateTime to);
        List<IntensityModel> Intensity(string code, DateTime from, DateTime to, string exit);
        List<HistogramModel> Histogram(string code, DateTime from, DateTime to, string exit);
        List<FraudModel> Fraud(string code, DateTime from, DateTime to);
        
        #endregion
    }
}
