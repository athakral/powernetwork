using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using CsvHelper;
using CsvHelper.Configuration;
using PowerNetwork.Core.DataModels;
using Npgsql;
using NpgsqlTypes;

namespace PowerNetwork.Core.Helpers {
    public class MpgsDataService : IMpgsDataService {

        private static MpgsRegion[] _region;
        private static MpgsCity[] _city;
        private static MpgsCenter[] _center;

        private static MpgsCts[] _cts;

        private static MpgsRoc[] _rocs;
        private static MpgsLift[] _lifts;

        private static MpgsMaintenanceStrategy2[] _maintenanceStrategies2;

        private static string _rootFolder;
        private static string _connectionString;

        public MpgsDataService(string rootFolder, string connectionString) {
            _rootFolder = rootFolder;
            _connectionString = connectionString;
        }

        public dynamic Common() {
            LoadData();

            int minClientCount = 0, maxClientCount = 0;

            using (var connection = new NpgsqlConnection(_connectionString)) {
                connection.Open();

                var command = new NpgsqlCommand("select top 1 num_clients from mp_gs_summry order by num_clients asc", connection);
                var reader = command.ExecuteReader();
                while (reader.Read()) {
                    minClientCount = (int)(double)reader["num_clients"];
                }
                reader.Close();

                command = new NpgsqlCommand("select top 1 num_clients from mp_gs_summry order by num_clients desc", connection);
                reader = command.ExecuteReader();
                while (reader.Read()) {
                    maxClientCount = (int)(double)reader["num_clients"];
                }
                reader.Close();
            }

            var technicalMax = Math.Max(_maintenanceStrategies2.Count(o => o.TechnicalSeverity > 1400), _maintenanceStrategies2.Length / 10);
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
                minClientCount,
                maxClientCount,
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

            var result = new List<MpgsCts>();

            using (var connection = new NpgsqlConnection(_connectionString)) {
                var text =
                    @"select ct.code, sum.num_clients, sum.failure_probability, sum.action_required
                    from mp_gs_cts_gps_coordinates ct
                    join mp_gs_summry sum on sum.code = ct.code
                    where ct.log_sexadecimal > ? and ct.log_sexadecimal < ? and ct.lat_sexadecimal > ? and ct.lat_sexadecimal < ?";

                var command = new NpgsqlCommand(text, connection);

                command.Parameters.Add("x1", NpgsqlDbType.Double).Value = x1;
                command.Parameters.Add("x2", NpgsqlDbType.Double).Value = x2;
                command.Parameters.Add("y1", NpgsqlDbType.Double).Value = y1;
                command.Parameters.Add("y2", NpgsqlDbType.Double).Value = y2;

                connection.Open();

                var reader = command.ExecuteReader();
                while (reader.Read()) {
                    var code = reader["code"] as string;
                    var ct = _cts.FirstOrDefault(o => o.Code == code);

                    if (ct != null) {
                        ct.ClientCount = (int)(double)reader["num_clients"];
                        ct.FailRate = Math.Round((double)reader["failure_probability"] * 100, 1);

                        var action = reader["action_required"] as string;
                        ct.ActionType = action == "Regulatory Check" ? 1 : action == "Maintenance Required" ? 2 : 3;

                        result.Add(ct);
                    }
                }

                reader.Close();
            }

            return result;
        }

        public List<MpgsCts> CtsSearch(string code) {
            LoadData();
            return _cts.Where(o => o.Code == code).Take(1).ToList();
        }

        public dynamic SummaryTable(string region, string city, string center,
            string code, int? actionType, int failRate0, int failRate1, int clientCount0, int clientCount1, int page) {

            LoadData();

            var pageSize = 50;
            var items = new List<dynamic>();
            long count = 0;

            using (var connection = new NpgsqlConnection(_connectionString)) {
                var whereItems = new List<string>();

                if (!string.IsNullOrEmpty(region)) whereItems.Add("ct.provincia = '" + region + "'");
                if (!string.IsNullOrEmpty(city)) whereItems.Add("ct.municipio = '" + city + "'");
                if (!string.IsNullOrEmpty(center)) whereItems.Add("ct.centroresponsable = '" + center + "'");

                if (!string.IsNullOrEmpty(code)) whereItems.Add("ct.code = '" + code + "'");
                if (actionType.HasValue && actionType.Value > 0) whereItems.Add("sum.action_required = '" + (actionType.Value == 1 ? "Regulatory Check" : actionType.Value == 2 ? "Maintenance Required" : "No action Required") + "'");

                if (failRate0 > 0) whereItems.Add("sum.failure_probability >= " + failRate0 / 100.0);
                if (failRate1 < 100) whereItems.Add("sum.failure_probability <= " + failRate1 / 100.0);

                whereItems.Add("sum.num_clients >= " + clientCount0);
                whereItems.Add("sum.num_clients <= " + clientCount1);

                connection.Open();

                var countText =
                    @"select count(1) as count
                    from mp_gs_cts_gps_coordinates ct
                    join mp_gs_summry sum on sum.code = ct.code
                    where ";

                var countCommand = new NpgsqlCommand(countText + string.Join(" and ", whereItems), connection);

                var reader = countCommand.ExecuteReader();
                while (reader.Read()) {
                    count = (long)reader["count"];
                }
                reader.Close();

                var text =
                    @"select ct.code,
                        sum.num_clients, sum.type_construct, sum.num_transf, sum.num_exits, sum.box_age, sum.cell_functional_type,
                        sum.max_ima, sum.min_ima_s, sum.transf_age,
                        sum.transf_fail_rate, sum.box_fail_rate, sum.exist_fail_rate, sum.constr_fail_rate,
                        sum.last_36m_failure, sum.last_48m_failure, sum.failure_probability, sum.action_required
                    from mp_gs_cts_gps_coordinates ct
                    join mp_gs_summry sum on sum.code = ct.code
                    where ";

                var command = new NpgsqlCommand(text + string.Join(" and ", whereItems) + " order by sum.failure_probability desc limit " + pageSize + " offset " + (pageSize * page), connection);

                reader = command.ExecuteReader();
                while (reader.Read()) {
                    var ctCode = reader["code"] as string;
                    var ct = _cts.FirstOrDefault(o => o.Code == ctCode);

                    if (ct != null) {
                        var sum = new MpgsCtsSummary {
                            Code = ctCode,

                            ClientCount = (int)(double)reader["num_clients"],
                            ConstructType = reader["type_construct"] as string,
                            TransfCount = (int)(double)reader["num_transf"],
                            ExitCount = (int)(double)reader["num_exits"],
                            BoxAge = (double)reader["box_age"],
                            CellFunctional = (int)(double)reader["cell_functional_type"],

                            MaxIma = (double)reader["max_ima"],
                            MinIma = (double)reader["min_ima_s"],
                            TransfAge = Math.Round((double)reader["transf_age"], 1),

                            TransfFailRate = Math.Round((double)reader["transf_fail_rate"] * 100, 1),
                            BoxFailRate = Math.Round((double)reader["box_fail_rate"] * 100, 1),
                            ExitFailRate = Math.Round((double)reader["exist_fail_rate"] * 100, 1),
                            ConstrFailRate = Math.Round((double)reader["constr_fail_rate"] * 100, 1),

                            FailRate36 = Math.Round((double)reader["last_36m_failure"] * 100, 1),
                            FailRate48 = Math.Round((double)reader["last_48m_failure"] * 100, 1),
                            FailRate = Math.Round((double)reader["failure_probability"] * 100, 1),
                            Action = reader["action_required"] as string
                        };

                        sum.ActionType = sum.Action == "Regulatory Check" ? 1 : sum.Action == "Maintenance Required" ? 2 : 3;

                        ct.ClientCount = sum.ClientCount;
                        ct.FailRate = sum.FailRate;
                        ct.ActionType = sum.ActionType;

                        items.Add(new { ct, sum });
                    }
                }

                reader.Close();
            }

            return new { count, items, pageCount = Math.Ceiling(count / (double)pageSize) };
        }

        public string SummaryTableCsv(string region, string city, string center,
            string code, int? actionType, int failRate0, int failRate1, int clientCount0, int clientCount1) {

            LoadData();

            var items = new List<dynamic>();

            using (var connection = new NpgsqlConnection(_connectionString)) {
                var whereItems = new List<string>();

                if (!string.IsNullOrEmpty(region)) whereItems.Add("ct.provincia = '" + region + "'");
                if (!string.IsNullOrEmpty(city)) whereItems.Add("ct.municipio = '" + city + "'");
                if (!string.IsNullOrEmpty(center)) whereItems.Add("ct.centroresponsable = '" + center + "'");

                if (!string.IsNullOrEmpty(code)) whereItems.Add("ct.code = '" + code + "'");
                if (actionType.HasValue && actionType.Value > 0) whereItems.Add("sum.action_required = '" + (actionType.Value == 1 ? "Regulatory Check" : actionType.Value == 2 ? "Maintenance Required" : "No action Required") + "'");

                if (failRate0 > 0) whereItems.Add("sum.failure_probability >= " + failRate0 / 100.0);
                if (failRate1 < 100) whereItems.Add("sum.failure_probability <= " + failRate1 / 100.0);

                whereItems.Add("sum.num_clients >= " + clientCount0);
                whereItems.Add("sum.num_clients <= " + clientCount1);

                connection.Open();

                var text =
                    @"select ct.code,
                        sum.num_clients, sum.type_construct, sum.num_transf, sum.num_exits, sum.box_age, sum.cell_functional_type,
                        sum.max_ima, sum.min_ima_s, sum.transf_age,
                        sum.transf_fail_rate, sum.box_fail_rate, sum.exist_fail_rate, sum.constr_fail_rate,
                        sum.last_36m_failure, sum.last_48m_failure, sum.failure_probability, sum.action_required
                    from mp_gs_cts_gps_coordinates ct
                    join mp_gs_summry sum on sum.code = ct.code
                    where ";

                var command = new NpgsqlCommand(text + string.Join(" and ", whereItems) + " order by sum.failure_probability desc", connection);

                var reader = command.ExecuteReader();
                while (reader.Read()) {
                    var ctCode = reader["code"] as string;
                    var ct = _cts.FirstOrDefault(o => o.Code == ctCode);

                    if (ct != null) {
                        var sum = new MpgsCtsSummary {
                            Code = ctCode,

                            ClientCount = (int)(double)reader["num_clients"],
                            ConstructType = reader["type_construct"] as string,
                            TransfCount = (int)(double)reader["num_transf"],
                            ExitCount = (int)(double)reader["num_exits"],
                            BoxAge = (double)reader["box_age"],
                            CellFunctional = (int)(double)reader["cell_functional_type"],

                            MaxIma = (double)reader["max_ima"],
                            MinIma = (double)reader["min_ima_s"],
                            TransfAge = Math.Round((double)reader["transf_age"], 1),

                            TransfFailRate = Math.Round((double)reader["transf_fail_rate"] * 100, 1),
                            BoxFailRate = Math.Round((double)reader["box_fail_rate"] * 100, 1),
                            ExitFailRate = Math.Round((double)reader["exist_fail_rate"] * 100, 1),
                            ConstrFailRate = Math.Round((double)reader["constr_fail_rate"] * 100, 1),

                            FailRate36 = Math.Round((double)reader["last_36m_failure"] * 100, 1),
                            FailRate48 = Math.Round((double)reader["last_48m_failure"] * 100, 1),
                            FailRate = Math.Round((double)reader["failure_probability"] * 100, 1),
                            Action = reader["action_required"] as string
                        };

                        sum.ActionType = sum.Action == "Regulatory Check" ? 1 : sum.Action == "Maintenance Required" ? 2 : 3;

                        ct.ClientCount = sum.ClientCount;
                        ct.FailRate = sum.FailRate;
                        ct.ActionType = sum.ActionType;

                        items.Add(new { ct, sum });
                    }
                }

                reader.Close();
            }

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
            var result = new List<MpgsRelevance>();

            using (var connection = new NpgsqlConnection(_connectionString)) {
                var command = new NpgsqlCommand("select variables, \"case\" from mp_gs_relevance order by \"case\" desc", connection);
                connection.Open();

                var reader = command.ExecuteReader();
                while (reader.Read()) {
                    result.Add(new MpgsRelevance {
                        Variable = reader["variables"] as string,
                        Relevance = (double)Math.Round((decimal)reader["case"], 1)
                    });
                }

                reader.Close();
            }

            return result.ToArray();
        }

        public IEnumerable<dynamic> Variables(int x) {
            LoadData();

            var result = new List<dynamic>();

            if (x == 1) {
                using (var connection = new NpgsqlConnection(_connectionString)) {
                    var command = new NpgsqlCommand("select type_construct, failure_probability from mp_gs_failure_prob_type_construct order by failure_probability desc", connection);
                    connection.Open();

                    var reader = command.ExecuteReader();
                    while (reader.Read()) {
                        result.Add(new {
                            x = reader["type_construct"] as string,
                            y = (decimal)reader["failure_probability"] * 100
                        });
                    }

                    reader.Close();
                }

                return result;
            }

            var columns = new[] {
                "transf_fail_rate", "type_construct", "num_clients", "cell_functional_type", "exist_fail_rate",
                "constr_fail_rate", "max_ima", "num_exits", "box_age", "min_ima_s", "last_48m_failure", "transf_age",
                "last_36m_failure", "box_fail_rate", "num_transf", "failure_probability"
            };

            using (var connection = new NpgsqlConnection(_connectionString)) {
                var command = new NpgsqlCommand("select " + columns[x] + ", failure_probability from mp_gs_summry", connection);
                connection.Open();

                var reader = command.ExecuteReader();
                while (reader.Read()) {
                    result.Add(new {
                        x = (double)reader[columns[x]],
                        y = (double)reader["failure_probability"] * 100
                    });
                }

                reader.Close();
            }

            return result;
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

            var failRateData = new Dictionary<string, double>();

            using (var connection = new NpgsqlConnection(_connectionString)) {
                var command = new NpgsqlCommand("select code, failure_probability from mp_gs_summry", connection);
                connection.Open();

                var reader = command.ExecuteReader();
                while (reader.Read()) {
                    var code = reader["code"] as string;
                    if (code != null && !failRateData.ContainsKey(code)) failRateData.Add(code, (double)reader["failure_probability"] * 100);
                }

                reader.Close();
            }

            var graphData = new List<dynamic>();

            foreach (var item in technicalItems) {
                if (!failRateData.ContainsKey(item.Code)) continue;
                graphData.Add(new { f = failRateData[item.Code], p = item.Preventive, c = item.Corrective, selected = true });
            }

            var eIndex = 0;
            foreach (var item in economicItems) {
                if (!failRateData.ContainsKey(item.Code)) continue;
                graphData.Add(new { f = failRateData[item.Code], p = item.Preventive, c = item.Corrective, selected = eIndex <= index });
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
                    var reader = new CsvReader(File.OpenText(_rootFolder + "mp_gs_cts_gps_coordinates.csv"), new CsvConfiguration { HasHeaderRecord = false, WillThrowOnMissingField = false });
                    _cts = reader.GetRecords<MpgsCts>().ToArray();
                }

                if (_region == null) {
                    var reader = new CsvReader(File.OpenText(_rootFolder + "region.csv"), new CsvConfiguration { HasHeaderRecord = false, WillThrowOnMissingField = false });
                    _region = reader.GetRecords<MpgsRegion>().ToArray();

                    foreach (var region in _region) {
                        region.RegularCheckCount = _cts.Count(o => o.Region == region.Name && o.ActionType == 1);
                        region.MaintenanceCount = _cts.Count(o => o.Region == region.Name && o.ActionType == 2);
                    }
                }

                if (_city == null) {
                    var reader = new CsvReader(File.OpenText(_rootFolder + "city.csv"), new CsvConfiguration { HasHeaderRecord = false, WillThrowOnMissingField = false });
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
                    var reader = new CsvReader(File.OpenText(_rootFolder + "center.csv"), new CsvConfiguration { HasHeaderRecord = false, WillThrowOnMissingField = false });
                    _center = reader.GetRecords<MpgsCenter>().ToArray();

                    foreach (var center in _center) {
                        center.RegularCheckCount = _cts.Count(o => o.Center == center.Name && o.ActionType == 1);
                        center.MaintenanceCount = _cts.Count(o => o.Center == center.Name && o.ActionType == 2);
                    }
                }

                if (_maintenanceStrategies2 == null) {
                    var strategyItems = new List<MpgsMaintenanceStrategy2>();

                    using (var connection = new NpgsqlConnection(_connectionString)) {
                        connection.Open();

                        var command = new NpgsqlCommand(
                            @"select code, technical_severity, preventive_maintenance_cost, corrective_maintenance_cost, corrective_maintenance_savings
                            from mp_gs_maintenance_strategy_v1 where corrective_maintenance_savings > -100000
                            order by technical_severity desc, preventive_maintenance_cost", connection);

                        var reader = command.ExecuteReader();
                        while (reader.Read()) {
                            strategyItems.Add(new MpgsMaintenanceStrategy2 {
                                Code = reader["code"] as string,
                                TechnicalSeverity = (double)reader["technical_severity"],
                                Preventive = (double)reader["preventive_maintenance_cost"],
                                Corrective = (double)reader["corrective_maintenance_cost"],
                                Savings = (double)reader["corrective_maintenance_savings"]
                            });
                        }
                        reader.Close();
                    }

                    _maintenanceStrategies2 = strategyItems.ToArray();
                }
            }
        }

    }
}
