namespace PowerNetwork.Core.DataModels {

    public class MpgsRegion {
        public string Name { get; set; }
        public double Lng { get; set; }
        public double Lat { get; set; }

        public int Count { get; set; }
        public int RegularCheckCount { get; set; }
        public int MaintenanceCount { get; set; }
    }

    public class MpgsCity {
        public string Name { get; set; }
        public string Region { get; set; }
        public double Lng { get; set; }
        public double Lat { get; set; }

        public int Count { get; set; }
        public int RegularCheckCount { get; set; }
        public int MaintenanceCount { get; set; }
    }

    public class MpgsCenter {
        public string Name { get; set; }
        public double Lng { get; set; }
        public double Lat { get; set; }

        public int Count { get; set; }
        public int RegularCheckCount { get; set; }
        public int MaintenanceCount { get; set; }
    }

    public class MpgsCts {
        public string Center { get; set; }
        public string Code { get; set; }
        public double Lng { get; set; }
        public double Lat { get; set; }
        public string Region { get; set; }
        public string City { get; set; }

        public int? ClientCount { get; set; }
        public double? FailRate { get; set; }
        public int? ActionType { get; set; }
    }

    public class MpgsCtsSummary {
        public string Code { get; set; }

        public int ClientCount { get; set; }
        public string ConstructType { get; set; }
        public int TransfCount { get; set; }
        public int ExitCount { get; set; }
        public double BoxAge { get; set; }
        public int CellFunctional { get; set; }

        public double MaxIma { get; set; }
        public double MinIma { get; set; }
        public double TransfAge { get; set; }

        public double TransfFailRate { get; set; }
        public double BoxFailRate { get; set; }
        public double ExitFailRate { get; set; }
        public double ConstrFailRate { get; set; }

        public double FailRate36 { get; set; }
        public double FailRate48 { get; set; }
        public double FailRate { get; set; }

        public string Action { get; set; }

        public int? ActionType { get; set; }
        public double[] Variables { get; set; }
    }

    public class MpgsRelevance {
        public string Variable { get; set; }
        public double Relevance { get; set; }
    }

    public class MpgsRoc {
        public double Sensitivity { get; set; }
        public double CustTarget { get; set; }

        public double TruePos { get; set; }
        public double FalsePos { get; set; }
        public double FalseNeg { get; set; }
        public double TrueNeg { get; set; }

        public double TruePosPercent { get; set; }
        public double FalseNegPercent { get; set; }
        public double TrueNegPercent { get; set; }
        public double FalsePosPercent { get; set; }
    }

    public class MpgsLift {
        public double CustTarget { get; set; }
        public double Precision { get; set; }

        public double TruePos { get; set; }
        public double FalsePos { get; set; }
        public double FalseNeg { get; set; }
        public double TrueNeg { get; set; }

        public double TruePosPercent { get; set; }
        public double FalseNegPercent { get; set; }
        public double TrueNegPercent { get; set; }
        public double FalsePosPercent { get; set; }
    }

    public class MpgsMaintenanceStrategy {
        public string Code { get; set; }
        public double Probability { get; set; }

        public double EconomicSeverity { get; set; }
        public string MaintenanceSeverity { get; set; }
        public double TechnicalSeverity { get; set; }
        public string MaintenanceCritically { get; set; }
        public string Maintenance { get; set; }

        public double PreventiveMaintenanceCost { get; set; }
        public double Rango { get; set; }
        public double CorrectiveMaintenanceSavings { get; set; }

        public double AcumCost { get; set; }
        public double AcumRango { get; set; }
        public double AcumSavings { get; set; }
    }

    public class MpgsMaintenanceStrategy2 {
        public string Code { get; set; }
        public double TechnicalSeverity { get; set; }

        public double Preventive { get; set; }
        public double Corrective { get; set; }
        public double Savings { get; set; }
    }

}
