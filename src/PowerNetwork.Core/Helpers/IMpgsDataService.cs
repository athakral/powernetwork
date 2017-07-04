using System.Collections.Generic;
using PowerNetwork.Core.DataModels;

namespace PowerNetwork.Core.Helpers {
    public interface IMpgsDataService {
        
        dynamic Common();

        List<MpgsCts> Cts(double x1, double x2, double y1, double y2);
        List<MpgsCts> CtsSearch(string code);

        dynamic SummaryTable(string region, string city, string center,
            string code, int? actionType, int failRate0, int failRate1, int clientCount0, int clientCount1, int page);

        string SummaryTableCsv(string region, string city, string center,
            string code, int? actionType, int failRate0, int failRate1, int clientCount0, int clientCount1);

        MpgsRelevance[] Relevance();
        IEnumerable<dynamic> Variables(int x);

        MpgsRoc[] Roc();
        MpgsLift[] Lift();

        dynamic Strategy2(int technical);
        dynamic Strategy3(int technical, int filter, double value);
    }
}
