using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using CsvHelper;
using CsvHelper.Configuration;
using PowerNetwork.Core.DataModels;

namespace PowerNetwork.Core.Helpers {
    public class MpgsDataService2 : IMpgsDataService {

        private static MpgsRegion[] _region;
        private static MpgsCity[] _city;
        private static MpgsCenter[] _center;

        private static MpgsCts[] _cts;
        private static MpgsCtsSummary[] _ctsSummary;

        private static List<string> _ctsSummaryConstructs;
        private static MpgsRelevance[] _relevances;
        private static MpgsRelevance[] _typeConstructProbs;
        private static MpgsRoc[] _rocs;
        private static MpgsLift[] _lifts;

        private static MpgsMaintenanceStrategy2[] _maintenanceStrategies2;

        private static string _rootFolder;

        public MpgsDataService2(string rootFolder) {
            _rootFolder = rootFolder;
        }

        public dynamic Common() {
            LoadData();

            var technicalMax = Math.Max(_maintenanceStrategies2.Count(o => o.TechnicalSeverity > 0),
                _maintenanceStrategies2.Length / 2);
            var technicalGraph = new List<dynamic>();

            var i = 0;
            double sum = 0;
            technicalGraph.Add(new { i, sum });

            foreach (var item in _maintenanceStrategies2.Take(technicalMax)) {
                i++;
                sum += item.TechnicalSeverity;
                technicalGraph.Add(new { i, sum });
            }

            return new {
                regions = _region,
                cities = _city,
                centers = _center,
                minClientCount = _ctsSummary.Min(o => o.ClientCount),
                maxClientCount = _ctsSummary.Max(o => o.ClientCount),
                strategy = new {
                    assetCount = technicalMax,
                    technicalGraph,
                    preventive = _maintenanceStrategies2.Sum(o => o.Preventive),
                    corrective = _maintenanceStrategies2.Sum(o => o.Corrective)
                }
            };
        }

        public List<MpgsCts> Cts(double x1, double x2, double y1, double y2) {
            LoadData();
            return _cts.Where(o => o.Lng > x1 && o.Lng < x2 && o.Lat > y1 && o.Lat < y2).ToList();
        }

        public List<MpgsCts> CtsSearch(string code) {
            LoadData();
            return _cts.Where(o => o.Code == code).Take(1).ToList();
        }

        public dynamic SummaryTable(string region, string city, string center,
            string code, int? actionType, int failRate0, int failRate1, int clientCount0, int clientCount1, int page) {

            var pageSize = 50;
            LoadData();

            var cts =
                _cts.Where(
                    o =>
                        (string.IsNullOrEmpty(region) || o.Region == region) &&
                        (string.IsNullOrEmpty(city) || o.City == city) &&
                        (string.IsNullOrEmpty(center) || o.Center == center) &&
                        (string.IsNullOrEmpty(code) || o.Code == code) &&
                        (!actionType.HasValue || o.ActionType == actionType) &&
                        (failRate0 == 0 || o.FailRate >= failRate0) &&
                        (failRate1 == 100 || o.FailRate <= failRate1) &&
                        o.ClientCount >= clientCount0 && o.ClientCount <= clientCount1);

            var count = cts.Count();
            cts = cts.OrderByDescending(o => o.FailRate).Skip(pageSize * page).Take(pageSize);

            var items = from ct in cts
                        join sum in _ctsSummary on ct.Code equals sum.Code
                        where sum != null
                        select new { ct, sum };

            return new { count, items, pageCount = Math.Ceiling(count / (double)pageSize) };
        }

        public string SummaryTableCsv(string region, string city, string center,
            string code, int? actionType, int failRate0, int failRate1, int clientCount0, int clientCount1) {

            LoadData();

            var cts =
                _cts.Where(
                    o =>
                        (string.IsNullOrEmpty(region) || o.Region == region) &&
                        (string.IsNullOrEmpty(city) || o.City == city) &&
                        (string.IsNullOrEmpty(center) || o.Center == center) &&
                        (string.IsNullOrEmpty(code) || o.Code == code) &&
                        (!actionType.HasValue || o.ActionType == actionType) &&
                        (failRate0 == 0 || o.FailRate >= failRate0) &&
                        (failRate1 == 100 || o.FailRate <= failRate1) &&
                        o.ClientCount >= clientCount0 && o.ClientCount <= clientCount1);

            cts = cts.OrderByDescending(o => o.FailRate);

            var items = from ct in cts
                        join sum in _ctsSummary on ct.Code equals sum.Code
                        where sum != null
                        select new { ct, sum };

            var builder = new StringBuilder();
            builder.AppendLine("Code,# clients,Type Constr,# transf,# exits,Box Age,Tp function,Max. I,Min. I,Transf. Age,Transf. F. Rate,Exits F. Rate,Box F. Rate,Constr F. Rate,Last 36m failure,Last 48m failure,Failure Prob,Action required");

            foreach (var item in items) {
                builder.Append(item.ct.Code + ",");
                builder.Append(item.ct.ClientCount + ",");

                builder.Append(item.sum.ConstructType + ",");
                builder.Append(item.sum.TransfCount + ",");
                builder.Append(item.sum.ExitCount + ",");
                builder.Append(item.sum.BoxAge + ",");
                builder.Append(item.sum.CellFunctional + ",");

                builder.Append(item.sum.MaxIma + ",");
                builder.Append(item.sum.MinIma + ",");
                builder.Append(item.sum.TransfAge + ",");

                builder.Append(item.sum.TransfFailRate + ",");
                builder.Append(item.sum.ExitFailRate + ",");
                builder.Append(item.sum.BoxFailRate + ",");
                builder.Append(item.sum.ConstrFailRate + ",");

                builder.Append(item.sum.FailRate36 + ",");
                builder.Append(item.sum.FailRate48 + ",");
                builder.Append(item.sum.FailRate + ",");
                builder.AppendLine(item.sum.Action);
            }

            return builder.ToString();
        }

        public MpgsRelevance[] Relevance() {
            if (_relevances == null) {
                var reader = new CsvReader(File.OpenText(_rootFolder + "mp_gs_relevance.csv"), new CsvConfiguration { HasHeaderRecord = false, WillThrowOnMissingField = false });
                _relevances = reader.GetRecords<MpgsRelevance>().ToArray();

                foreach (var relevance in _relevances) {
                    relevance.Relevance = Math.Round(relevance.Relevance, 1);
                }
            }

            return _relevances;
        }

        public IEnumerable<dynamic> Variables(int x) {
            LoadData();

            if (x == 1) {
                return _typeConstructProbs.Select(o => new { x = o.Variable, y = o.Relevance * 100 });
            }

            var y = 15;

            if (_ctsSummary.First().Variables == null) {
                _ctsSummaryConstructs = _ctsSummary.Select(o => o.ConstructType).Distinct().ToList();

                foreach (var ctsSummary in _ctsSummary) {
                    ctsSummary.Variables = new[] {
                        ctsSummary.TransfFailRate, _ctsSummaryConstructs.IndexOf(ctsSummary.ConstructType), ctsSummary.ClientCount,
                        ctsSummary.CellFunctional, ctsSummary.ExitFailRate, ctsSummary.ConstrFailRate, ctsSummary.MaxIma,
                        ctsSummary.ExitCount, ctsSummary.BoxAge, ctsSummary.MinIma, ctsSummary.FailRate48,
                        ctsSummary.TransfAge, ctsSummary.FailRate36, ctsSummary.BoxFailRate, ctsSummary.TransfCount,
                        ctsSummary.FailRate
                    };
                }
            }

            return _ctsSummary.Select(o => new { x = o.Variables[x], y = o.Variables[y] });
        }

        public MpgsRoc[] Roc() {
            if (_rocs == null) {
                var reader = new CsvReader(File.OpenText(_rootFolder + "roc.csv"), new CsvConfiguration { HasHeaderRecord = false, WillThrowOnMissingField = false });
                _rocs = reader.GetRecords<MpgsRoc>().ToArray();

                foreach (var roc in _rocs) {
                    roc.CustTarget = Math.Round(roc.CustTarget * 100);
                    roc.Sensitivity = Math.Round(roc.Sensitivity * 100, 1);

                    roc.TruePosPercent = Math.Round(roc.TruePosPercent * 100, 1);
                    roc.FalseNegPercent = Math.Round(roc.FalseNegPercent * 100, 1);
                    roc.TrueNegPercent = Math.Round(roc.TrueNegPercent * 100, 1);
                    roc.FalsePosPercent = Math.Round(roc.FalsePosPercent * 100, 1);
                }
            }

            return _rocs;
        }

        public MpgsLift[] Lift() {
            if (_lifts == null) {
                var reader = new CsvReader(File.OpenText(_rootFolder + "lift.csv"), new CsvConfiguration { HasHeaderRecord = false, WillThrowOnMissingField = false });
                _lifts = reader.GetRecords<MpgsLift>().ToArray();

                foreach (var lift in _lifts) {
                    lift.CustTarget = Math.Round(lift.CustTarget * 100);
                    lift.Precision = Math.Round(lift.Precision, 2);

                    lift.TruePosPercent = Math.Round(lift.TruePosPercent * 100, 1);
                    lift.FalseNegPercent = Math.Round(lift.FalseNegPercent * 100, 1);
                    lift.TrueNegPercent = Math.Round(lift.TrueNegPercent * 100, 1);
                    lift.FalsePosPercent = Math.Round(lift.FalsePosPercent * 100, 1);
                }
            }

            return _lifts;
        }

        public dynamic Strategy2(int technical) {
            LoadData();

            var technicalItems = _maintenanceStrategies2.Take(technical);
            var technicalPreventive = technicalItems.Sum(o => o.Preventive);

            var economicItems = _maintenanceStrategies2.Skip(technical).OrderByDescending(o => o.Savings);
            var graphData = new List<dynamic>();

            var index = 0;
            double preventive = economicItems.Sum(o => o.Preventive);
            double corrective = 0;

            graphData.Add(new {
                index,
                preventive = Math.Round(preventive, 1),
                corrective = Math.Round(corrective, 1),
                total = Math.Round(preventive + corrective, 1)
            });

            foreach (var item in economicItems) {
                preventive -= item.Preventive;
                corrective += item.Corrective;

                index++;

                graphData.Add(new {
                    index,
                    preventive = Math.Round(preventive, 1),
                    corrective = Math.Round(corrective, 1),
                    total = Math.Round(preventive + corrective, 1)
                });
            }

            return new {
                technical,
                technicalPreventive,
                index = 0,
                preventive = technicalPreventive,
                corrective = economicItems.Sum(o => o.Corrective),
                graphData
            };
        }

        public dynamic Strategy3(int technical, int filter, double value) {
            LoadData();

            var technicalItems = _maintenanceStrategies2.Take(technical).ToArray();
            var technicalPreventive = technicalItems.Sum(o => o.Preventive);

            var economicItems = _maintenanceStrategies2.Skip(technical).OrderByDescending(o => o.Savings).ToArray();

            var index = 0;
            double preventive = 0;
            double corrective = 0;

            if (filter == 1) {
                double sum = technicalPreventive;
                while (sum < value) {
                    sum += economicItems[index].Preventive;
                    index++;
                }

                index--;
                if (index >= 0) sum -= economicItems[index].Preventive;

                preventive = sum;
                corrective = economicItems.Skip(index + 1).Sum(o => o.Corrective);

            } else if (filter == 2) {
                double sum = 0;
                while (sum < value) {
                    sum += economicItems[index].Corrective;
                    index++;
                }

                index--;
                if (index >= 0) sum -= economicItems[index].Corrective;

                preventive = economicItems.Skip(index + 1).Sum(o => o.Preventive) + technicalPreventive;
                corrective = sum;
            }

            var graphData = new List<dynamic>();

            var joinTechnical = from item in technicalItems
                                join summary in _ctsSummary on item.Code equals summary.Code
                                select new { item, summary };
            foreach (var item in joinTechnical) {
                graphData.Add(new { f = item.summary.FailRate, p = item.item.Preventive, c = item.item.Corrective, selected = true });
            }

            var eIndex = 0;
            var joinEconomic = from item in economicItems
                               join summary in _ctsSummary on item.Code equals summary.Code
                               select new { item, summary };
            foreach (var item in joinEconomic) {
                graphData.Add(new { f = item.summary.FailRate, p = item.item.Preventive, c = item.item.Corrective, selected = eIndex <= index });
                eIndex++;
            }

            return new {
                technical,
                technicalPreventive,
                index,
                corrective,
                preventive,
                graphData
            };
        }

        private static readonly object Lock = new object();

        private static void LoadData() {
            lock (Lock) {
                if (_cts == null) {
                    var reader = new CsvReader(File.OpenText(_rootFolder + "mp_gs_cts_gps_coordinates_2.csv"), new CsvConfiguration { HasHeaderRecord = false, WillThrowOnMissingField = false });
                    _cts = reader.GetRecords<MpgsCts>().ToArray();
                }

                var validCodes = _cts.Select(o => o.Code).ToList();

                if (_ctsSummary == null) {
                    var reader = new CsvReader(File.OpenText(_rootFolder + "mp_gs_summry.csv"), new CsvConfiguration { HasHeaderRecord = false, WillThrowOnMissingField = false });
                    _ctsSummary = reader.GetRecords<MpgsCtsSummary>().Where(o => validCodes.Contains(o.Code)).ToArray();

                    foreach (var ctsSummary in _ctsSummary) {
                        ctsSummary.ActionType = ctsSummary.Action == "Regulatory Check"
                            ? 1
                            : ctsSummary.Action == "Maintenance Required" ? 2 : 3;

                        ctsSummary.TransfAge = Math.Round(ctsSummary.TransfAge, 1);

                        ctsSummary.TransfFailRate = Math.Round(ctsSummary.TransfFailRate * 100, 1);
                        ctsSummary.BoxFailRate = Math.Round(ctsSummary.BoxFailRate * 100, 1);
                        ctsSummary.ExitFailRate = Math.Round(ctsSummary.ExitFailRate * 100, 1);
                        ctsSummary.ConstrFailRate = Math.Round(ctsSummary.ConstrFailRate * 100, 1);

                        ctsSummary.FailRate36 = Math.Round(ctsSummary.FailRate36 * 100, 1);
                        ctsSummary.FailRate48 = Math.Round(ctsSummary.FailRate48 * 100, 1);
                        ctsSummary.FailRate = Math.Round(ctsSummary.FailRate * 100, 1);
                    }

                    var join = from cts in _cts
                               join summary in _ctsSummary on cts.Code equals summary.Code
                               select new { cts, summary };

                    foreach (var joinItem in join) {
                        joinItem.cts.ClientCount = joinItem.summary.ClientCount;
                        joinItem.cts.FailRate = joinItem.summary.FailRate;
                        joinItem.cts.ActionType = joinItem.summary.ActionType;
                    }
                }

                if (_region == null) {
                    var reader = new CsvReader(File.OpenText(_rootFolder + "region_2.csv"), new CsvConfiguration { HasHeaderRecord = false, WillThrowOnMissingField = false });
                    _region = reader.GetRecords<MpgsRegion>().ToArray();

                    foreach (var region in _region) {
                        region.RegularCheckCount = _cts.Count(o => o.Region == region.Name && o.ActionType == 1);
                        region.MaintenanceCount = _cts.Count(o => o.Region == region.Name && o.ActionType == 2);
                    }
                }

                if (_city == null) {
                    var reader = new CsvReader(File.OpenText(_rootFolder + "city_2.csv"), new CsvConfiguration { HasHeaderRecord = false, WillThrowOnMissingField = false });
                    _city = reader.GetRecords<MpgsCity>().ToArray();

                    var group = from cts in _cts
                                where cts.ActionType == 1 || cts.ActionType == 2
                                group cts by new { cts.City, cts.ActionType } into ctsGroup
                                select new { count = ctsGroup.Count(), ctsGroup.Key.City, ctsGroup.Key.ActionType };

                    var groupDict = group.ToDictionary(o => o.City + o.ActionType, o => o.count);

                    foreach (var city in _city) {
                        city.RegularCheckCount = groupDict.ContainsKey(city.Name + "1") ? groupDict[city.Name + "1"] : 0;
                        city.MaintenanceCount = groupDict.ContainsKey(city.Name + "2") ? groupDict[city.Name + "2"] : 0;
                    }
                }

                if (_center == null) {
                    var reader = new CsvReader(File.OpenText(_rootFolder + "center_2.csv"), new CsvConfiguration { HasHeaderRecord = false, WillThrowOnMissingField = false });
                    _center = reader.GetRecords<MpgsCenter>().ToArray();

                    foreach (var center in _center) {
                        center.RegularCheckCount = _cts.Count(o => o.Center == center.Name && o.ActionType == 1);
                        center.MaintenanceCount = _cts.Count(o => o.Center == center.Name && o.ActionType == 2);
                    }
                }

                if (_maintenanceStrategies2 == null) {
                    var reader = new CsvReader(File.OpenText(_rootFolder + "mp_gs_maintenance_strategy_v1.csv"), new CsvConfiguration { HasHeaderRecord = false, WillThrowOnMissingField = false });
                    _maintenanceStrategies2 = reader.GetRecords<MpgsMaintenanceStrategy2>().Where(o => validCodes.Contains(o.Code)).ToArray();

                    _maintenanceStrategies2 = _maintenanceStrategies2
                        .Where(o => o.Savings > -100000).OrderByDescending(o => o.TechnicalSeverity).ThenBy(o => o.Preventive).ToArray();
                }

                if (_typeConstructProbs == null) {
                    var reader = new CsvReader(File.OpenText(_rootFolder + "mp_gs_failure_prob_type_construct.csv"), new CsvConfiguration { HasHeaderRecord = false, WillThrowOnMissingField = false });
                    _typeConstructProbs = reader.GetRecords<MpgsRelevance>().ToArray();
                }
            }
        }

    }
}
