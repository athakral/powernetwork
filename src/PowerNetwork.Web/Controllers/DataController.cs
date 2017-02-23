using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Hosting;
using CsvHelper;
using System.IO;
using CsvHelper.Configuration;
using PowerNetwork.Core.Helpers;
using PowerNetwork.Web.Models;
using Microsoft.Extensions.Options;
using PowerNetwork.Core.DataModels;
//using HiQPdf;

namespace PowerNetwork.Web.Controllers {
    public class DataController : Controller {

        private readonly IHostingEnvironment _hostingEnvironment;
        private readonly DataService _dataService;

        public DataController(IHostingEnvironment hostingEnvironment, IOptions<AppConfig> appConfig) {
            _hostingEnvironment = hostingEnvironment;
            _dataService = DataService.Instance(appConfig.Value.ConnectionString);
        }

        private string MapPath(string path) {
            return Path.Combine(_hostingEnvironment.WebRootPath, path);
        }

        private static CtsRegionModel[] _ctsRegions;
        private static CtsCityModel[] _ctsCities;
        private static CtsCenterModel[] _ctsCenters;

        public IActionResult Common() {
            var config = new CsvConfiguration { HasHeaderRecord = false };

            if (_ctsRegions == null) {
                var csvReaderRegion = new CsvReader(System.IO.File.OpenText(MapPath("data/region_v1.2.csv")), config);
                _ctsRegions = csvReaderRegion.GetRecords<CtsRegionModel>().ToArray();
            }

            if (_ctsCities == null) {
                var csvReaderCity = new CsvReader(System.IO.File.OpenText(MapPath("data/city_v1.2.csv")), config);
                _ctsCities = csvReaderCity.GetRecords<CtsCityModel>().ToArray();
            }

            if (_ctsCenters == null) {
                var csvReaderCenter = new CsvReader(System.IO.File.OpenText(MapPath("data/center_v1.2.csv")), config);
                _ctsCenters = csvReaderCenter.GetRecords<CtsCenterModel>().ToArray();
            }

            return Json(new { regions = _ctsRegions, cities = _ctsCities, centers = _ctsCenters });
        }

        private static CtsModel[] _ctsItems;

        public IActionResult Cts(double x1, double x2, double y1, double y2) {
            if (_ctsItems == null) {
                var csvReaderCts = new CsvReader(System.IO.File.OpenText(MapPath("data/cts_v1.2.csv")),
                    new CsvConfiguration() { HasHeaderRecord = false, WillThrowOnMissingField = false });

                _ctsItems = csvReaderCts.GetRecords<CtsModel>().ToArray();
            }

            var result = _ctsItems.Where(o => o.lng > x1 && o.lng < x2 && o.lat > y1 && o.lat < y2).ToList();

            // tipo data
            var tipos = _dataService.Cts(x1, x2, y1, y2);
            foreach (var tipo in tipos) {
                var cts = result.FirstOrDefault(o => o.othercode == tipo.Code);

                if (cts == null) continue;
                cts.teleLevel = tipo.TeleLevel;
                cts.tp4 = tipo.T4;
                cts.tp5 = tipo.T5;
                cts.to = tipo.To;
            }

            return Json(result);
        }

        public IActionResult CtsSearch(string code) {
            if (_ctsItems == null) {
                var csvReaderCts = new CsvReader(System.IO.File.OpenText(MapPath("data/cts_v1.2.csv")),
                    new CsvConfiguration() { HasHeaderRecord = false, WillThrowOnMissingField = false });

                _ctsItems = csvReaderCts.GetRecords<CtsModel>().ToArray();
            }

            return Json(_ctsItems.Where(o => o.code == code || o.othercode == code).Take(1).ToList());
        }

        public IActionResult MeterSearch(string code) {
            if (_ctsItems == null) {
                var csvReaderCts = new CsvReader(System.IO.File.OpenText(MapPath("data/cts_v1.2.csv")),
                    new CsvConfiguration() { HasHeaderRecord = false, WillThrowOnMissingField = false });

                _ctsItems = csvReaderCts.GetRecords<CtsModel>().ToArray();
            }

            var ctCode = _dataService.CtFromMeterCode(code);
            return Json(_ctsItems.Where(o => o.othercode == ctCode).Take(1).ToList());
        }

        public IActionResult MeterGroups(string otherCode, bool exitCalc) {
            var groups = _dataService.MeterGroups(otherCode);

            var exitCodes = new List<int>();
            var exitMax = string.Empty;

            if (exitCalc) {
                var exits = groups.Select(o => o.Exit).Distinct().ToList();

                foreach (var exit in exits) {
                    int exitCode;
                    if (int.TryParse(exit, out exitCode)) {
                        if (exitCode > 0 && exitCode <= 24) exitCodes.Add(exitCode);
                    }
                }

                exitCodes.Sort();

                exitMax = _dataService.MaxExit(otherCode);
            }

            return Json(new { groups, exits = exitCodes, exitMax, tipo = _dataService.CtTipo(otherCode) });
        }

        public IActionResult Meters(string groupCode) {
            return Json(_dataService.Meters(groupCode));
        }

        public IActionResult Alarms(string region, string city, string center, string code, int teleLevel0, int teleLevel1, int tipo) {
            var overloadAlarms = _dataService.OverloadAlarms(teleLevel0, teleLevel1, tipo);
            var unbalanceAlarms = _dataService.UnbalanceAlarms(teleLevel0, teleLevel1, tipo);

            if (_ctsItems == null) {
                var csvReaderCts = new CsvReader(System.IO.File.OpenText(MapPath("data/cts_v1.2.csv")),
                    new CsvConfiguration { HasHeaderRecord = false, WillThrowOnMissingField = false });

                _ctsItems = csvReaderCts.GetRecords<CtsModel>().ToArray();
            }

            var overload = new List<dynamic>();
            foreach (var overloadAlarm in overloadAlarms) {
                var cts = _ctsItems.FirstOrDefault(o => o.othercode == overloadAlarm.Code);

                if (cts == null) continue;
                if (!string.IsNullOrEmpty(center) && cts.center != center) continue;
                if (!string.IsNullOrEmpty(city) && cts.city != city) continue;
                if (string.IsNullOrEmpty(city) && !string.IsNullOrEmpty(region) && cts.region != region) continue;
                if (string.IsNullOrEmpty(center) && string.IsNullOrEmpty(region) && !string.IsNullOrEmpty(code) && cts.othercode != code) continue;

                cts.alarm = "o";
                overload.Add(new { code = overloadAlarm.Code, ratio = overloadAlarm.Ratio, data = overloadAlarm.Data, cts });
            }

            var unbalance = new List<dynamic>();
            foreach (var unbalanceAlarm in unbalanceAlarms) {
                var cts = _ctsItems.FirstOrDefault(o => o.othercode == unbalanceAlarm.Code);

                if (cts == null) continue;
                if (!string.IsNullOrEmpty(center) && cts.center != center) continue;
                if (!string.IsNullOrEmpty(city) && cts.city != city) continue;
                if (string.IsNullOrEmpty(city) && !string.IsNullOrEmpty(region) && cts.region != region) continue;
                if (string.IsNullOrEmpty(center) && string.IsNullOrEmpty(region) && !string.IsNullOrEmpty(code) && cts.othercode != code) continue;

                cts.alarm = cts.alarm == "o" ? "ob" : "b";
                unbalance.Add(new { code = unbalanceAlarm.Code, ratio = unbalanceAlarm.Ratio, cts });
            }

            return Json(new { overload, unbalance });
        }

        public IActionResult FraudAlarms(string region, string city, string center, string code, int teleLevel0, int teleLevel1, int tipo) {
            var alarms = _dataService.FraudAlarms(teleLevel0, teleLevel1, tipo);

            if (_ctsItems == null) {
                var csvReaderCts = new CsvReader(System.IO.File.OpenText(MapPath("data/cts_v1.2.csv")),
                    new CsvConfiguration { HasHeaderRecord = false, WillThrowOnMissingField = false });

                _ctsItems = csvReaderCts.GetRecords<CtsModel>().ToArray();
            }

            var joinedAlarms = from a in alarms join c in _ctsItems on a.Code equals c.othercode select new { alarm = a, cts = c };

            if (!string.IsNullOrEmpty(center)) joinedAlarms = joinedAlarms.Where(o => o.cts.center == center);
            if (!string.IsNullOrEmpty(city)) joinedAlarms = joinedAlarms.Where(o => o.cts.city == city);
            if (string.IsNullOrEmpty(city) && !string.IsNullOrEmpty(region)) joinedAlarms = joinedAlarms.Where(o => o.cts.region == region);
            if (string.IsNullOrEmpty(center) && string.IsNullOrEmpty(region) && !string.IsNullOrEmpty(code)) joinedAlarms = joinedAlarms.Where(o => o.cts.othercode == code);

            var unbalance = new List<dynamic>();
            foreach (var unbalanceAlarm in joinedAlarms) {
                unbalance.Add(new { code = unbalanceAlarm.alarm.Code, ratio = unbalanceAlarm.alarm.Ratio, unbalanceAlarm.cts.lat, unbalanceAlarm.cts.lng });
            }

            return Json(new { Data = unbalance });
        }

        public IActionResult OverviewBalance(string code, DateTime from, DateTime to) {
            var items = _dataService.OverviewBalance(code, from, to).OrderBy(o => o.Date).ThenBy(o => o.Hour).ToList();

            var result = new List<OverviewBalanceOutModel>();

            foreach (var item in items) {
                result.Add(new OverviewBalanceOutModel {
                    date = item.Date.ToString("yyyy-MM-dd") + " " + item.Hour.ToString("D2") + ":00:00",
                    i = new[] { item.IntensityR, item.IntensityS, item.IntensityT },
                    t = new[] { item.TensionR, item.TensionS, item.TensionT }
                });
            }

            return Json(new { items = result });
        }

        private static IntensityCsvModel[] _intensityCsvItems;

        public IActionResult Intensity(string code, int mode, DateTime from, DateTime to, string exit) {
            if (_intensityCsvItems == null) {
                var csvReaderIntensity = new CsvReader(System.IO.File.OpenText(MapPath("data/intensity.txt")), new CsvConfiguration { HasHeaderRecord = false });
                _intensityCsvItems = csvReaderIntensity.GetRecords<IntensityCsvModel>().ToArray();
            }

            var items = _intensityCsvItems.Where(o => o.Exit == exit && o.Date >= from && o.Date <= to).ToList();

            var result = new List<IntensityOutModel>();

            foreach (var item in items) {
                result.Add(new IntensityOutModel {
                    date = item.Date.ToString("yyyy-MM-dd") + " " + item.Hour.ToString("D2") + ":00:00",
                    r = item.R,
                    s = item.S,
                    t = item.T
                });
            }

            return Json(new { items = result });
        }

        public IActionResult Intensity2(string code, int mode, DateTime from, DateTime to, string exit) {
            var items = _dataService.Intensity(code, from, to, exit).OrderBy(o => o.Date).ThenBy(o => o.Hour).ToList();

            var result = new List<IntensityOutModel>();

            foreach (var item in items) {
                result.Add(new IntensityOutModel {
                    date = item.Date.ToString("yyyy-MM-dd") + " " + item.Hour.ToString("D2") + ":00:00",
                    r = item.R,
                    s = item.S,
                    t = item.T
                });
            }

            return Json(new { items = result });
        }

        public IActionResult Histogram(string code, DateTime from, DateTime to, string exit) {
            var items = _dataService.Histogram(code, from, to, exit);

            var result = new List<HistogramOutModel>();

            if (items.Count > 0) {
                var maxThreshold = items.Max(o => o.Threshold);
                var minThreshold = items.Min(o => o.Threshold);

                for (var i = minThreshold; i < maxThreshold; i += 1) {
                    var resultItem = new HistogramOutModel {
                        threshold = (int) i,
                        r = items.Where(o => o.Fase == "FASE_R" && (int) o.Threshold == i).Sum(o => (int) o.Count),
                        s = items.Where(o => o.Fase == "FASE_S" && (int) o.Threshold == i).Sum(o => (int) o.Count),
                        t = items.Where(o => o.Fase == "FASE_T" && (int) o.Threshold == i).Sum(o => (int) o.Count)
                    };

                    result.Add(resultItem);
                }
            }

            return Json(new { items = result });
        }

        public IActionResult Fraud(string code, DateTime from, DateTime to) {
            var items = _dataService.Fraud(code, from, to);

            var result = new List<FraudOutModel>();

            foreach (var item in items) {
                result.Add(new FraudOutModel { date = item.Date.ToString("yyyy-MM-dd"), ct = item.Ct, exit = item.Exit });
            }

            return Json(new { items = result, sum = new { ct = items.Sum(o => o.Ct), exit = items.Sum(o => o.Exit) } });
        }


        // TODO (Hoa): conversion library is in evaluation
        public ActionResult Export() {
            //var html = new StreamReader(Request.InputStream).ReadToEnd();

            //var htmlToPdfConverter = new HtmlToPdf();
            //var pdfBuffer = htmlToPdfConverter.ConvertHtmlToMemory(html, "http://localhost:3922/");

            var file = Guid.NewGuid() + ".pdf";
            //System.IO.File.WriteAllBytes(Server.MapPath("~/temp/") + file, pdfBuffer);

            return Json("/temp/" + file);
        }

    }
}