using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Hosting;
using CsvHelper;
using System.IO;
using CsvHelper.Configuration;
using PowerNetwork.Core.Helpers;
using PowerNetwork.Web.Models;
using Microsoft.Extensions.Options;
using PowerNetwork.Core.DataModels;

namespace PowerNetwork.Web.Controllers
{
    public class DataController : Controller
    {

        private readonly IHostingEnvironment _hostingEnvironment;
        private readonly DataService _dataService;

        public DataController(IHostingEnvironment hostingEnvironment, IOptions<AppConfig> appConfig)
        {
            this._hostingEnvironment = hostingEnvironment;
            this._dataService = DataService.Instance(appConfig.Value.ConnectionString);

        }

        private static CtsRegionModel[] _ctsRegions;
        private static CtsCityModel[] _ctsCities;
        private static CtsCenterModel[] _ctsCenters;

        public IActionResult Common()
        {
            if (_ctsRegions == null)
            {
                var csvReaderRegion = new CsvReader(System.IO.File.OpenText(
                    Path.Combine(this._hostingEnvironment.WebRootPath, "data/region_v1.2.csv")), new CsvConfiguration()
                    {
                        HasHeaderRecord = false
                    });

                //csvReaderRegion.Configuration.RegisterClassMap<CtsRegionModelMap>();
                _ctsRegions = csvReaderRegion.GetRecords<CtsRegionModel>().ToArray();
            }

            if (_ctsCities == null)
            {
                var csvReaderCity = new CsvReader(System.IO.File.OpenText(
                   Path.Combine(this._hostingEnvironment.WebRootPath, "data/city_v1.2.csv")), new CsvConfiguration()
                   {
                       HasHeaderRecord = false
                   });

                _ctsCities = csvReaderCity.GetRecords<CtsCityModel>().ToArray();
            }

            if (_ctsCenters == null)
            {
                var csvReaderCenter = new CsvReader(System.IO.File.OpenText(
                  Path.Combine(this._hostingEnvironment.WebRootPath, "data/center_v1.2.csv")), new CsvConfiguration()
                  {
                      HasHeaderRecord = false
                  });

                _ctsCenters = csvReaderCenter.GetRecords<CtsCenterModel>().ToArray();
            }

            return
                Json(
                    new
                    {
                        regions = _ctsRegions,
                        cities = _ctsCities,
                        centers = _ctsCenters
                    });
        }

        private static CtsModel[] _ctsItems;

        public IActionResult Cts(double x1, double x2, double y1, double y2)
        {
            if (_ctsItems == null)
            {
                var csvReaderCts = new CsvReader(System.IO.File.OpenText(
                 Path.Combine(this._hostingEnvironment.WebRootPath, "data/cts_v1.2.csv")), new CsvConfiguration()
                 {
                     HasHeaderRecord = false
                 });

                _ctsItems = csvReaderCts.GetRecords<CtsModel>().ToArray();
            }

            return Json(_ctsItems.Where(o => o.lng > x1 && o.lng < x2 && o.lat > y1 && o.lat < y2).ToList());
        }

        public IActionResult CtsSearch(string code)
        {
            if (_ctsItems == null)
            {
                var csvReaderCts = new CsvReader(System.IO.File.OpenText(
                 Path.Combine(this._hostingEnvironment.WebRootPath, "data/cts_v1.2.csv")), new CsvConfiguration()
                 {
                     HasHeaderRecord = false
                 });

                _ctsItems = csvReaderCts.GetRecords<CtsModel>().ToArray();
            }

            return Json(_ctsItems.Where(o => o.code == code || o.othercode == code).Take(1).ToList());
        }

        public IActionResult MeterSearch(string code)
        {
            if (_ctsItems == null)
            {
                var csvReaderCts = new CsvReader(System.IO.File.OpenText(
                 Path.Combine(this._hostingEnvironment.WebRootPath, "data/cts_v1.2.csv")), new CsvConfiguration()
                 {
                     HasHeaderRecord = false
                 });

                _ctsItems = csvReaderCts.GetRecords<CtsModel>().ToArray();
            }


            var ctCode = this._dataService.CtFromMeterCode(code);
            return Json(_ctsItems.Where(o => o.othercode == ctCode).Take(1).ToList());
        }

        public IActionResult MeterGroups(string otherCode, bool exitCalc)
        {
            var groups = this._dataService.MeterGroups(otherCode);

            var exitCodes = new List<int>();
            var exitMax = string.Empty;

            if (exitCalc)
            {
                var exits = groups.Select(o => o.Exit).Distinct().ToList();

                foreach (var exit in exits)
                {
                    int exitCode;
                    if (int.TryParse(exit, out exitCode))
                    {
                        if (exitCode > 0 && exitCode <= 24) exitCodes.Add(exitCode);
                    }
                }

                exitCodes.Sort();

                exitMax = this._dataService.MaxExit(otherCode);
            }

            return Json(new { groups, exits = exitCodes, exitMax });
        }

        public IActionResult Meters(string groupCode)
        {
            return Json(this._dataService.Meters(groupCode));
        }

        public IActionResult Alarms(string region, string city, string center, string code)
        {
            var overloadAlarms = this._dataService.OverloadAlarms();
            var unbalanceAlarms = this._dataService.UnbalanceAlarms();

            if (_ctsItems == null)
            {
                var csvReaderCts = new CsvReader(System.IO.File.OpenText(
                 Path.Combine(this._hostingEnvironment.WebRootPath, "data/cts_v1.2.csv")), new CsvConfiguration()
                 {
                     HasHeaderRecord = false,
                     WillThrowOnMissingField = false
                 });

                _ctsItems = csvReaderCts.GetRecords<CtsModel>().ToArray();
            }

            var overload = new List<dynamic>();
            foreach (var overloadAlarm in overloadAlarms)
            {
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
            foreach (var unbalanceAlarm in unbalanceAlarms)
            {
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

        public IActionResult FraudAlarms(string region, string city, string center, string code)
        {
            var alarms = this._dataService.FraudAlarms();

            if (_ctsItems == null)
            {
                var csvReaderCts = new CsvReader(System.IO.File.OpenText(
                 Path.Combine(this._hostingEnvironment.WebRootPath, "data/cts_v1.2.csv")), new CsvConfiguration()
                 {
                     HasHeaderRecord = false,
                     WillThrowOnMissingField = false
                 });

                _ctsItems = csvReaderCts.GetRecords<CtsModel>().ToArray();
            }

            var joinedAlarms = from a in alarms join c in _ctsItems on a.Code equals c.othercode select new { alarm = a, cts = c };

            if (!string.IsNullOrEmpty(center)) joinedAlarms = joinedAlarms.Where(o => o.cts.center == center);
            if (!string.IsNullOrEmpty(city)) joinedAlarms = joinedAlarms.Where(o => o.cts.city == city);
            if (string.IsNullOrEmpty(city) && !string.IsNullOrEmpty(region)) joinedAlarms = joinedAlarms.Where(o => o.cts.region == region);
            if (string.IsNullOrEmpty(center) && string.IsNullOrEmpty(region) && !string.IsNullOrEmpty(code)) joinedAlarms = joinedAlarms.Where(o => o.cts.othercode == code);

            var unbalance = new List<dynamic>();
            foreach (var unbalanceAlarm in joinedAlarms)
            {
                unbalance.Add(new { code = unbalanceAlarm.alarm.Code, ratio = unbalanceAlarm.alarm.Ratio, unbalanceAlarm.cts.lat, unbalanceAlarm.cts.lng });
            }

            return Json(new { Data = unbalance });
        }

        private static IntensityCsvModel[] _intensityCsvItems;

        public IActionResult Intensity(string code, int mode, DateTime from, DateTime to, string exit)
        {
            if (_intensityCsvItems == null)
            {
                var csvReaderIntensity = new CsvReader(System.IO.File.OpenText(
                 Path.Combine(this._hostingEnvironment.WebRootPath, "data/intensity.txt")), new CsvConfiguration()
                 {
                     HasHeaderRecord = false
                 });

                _intensityCsvItems = csvReaderIntensity.GetRecords<IntensityCsvModel>().ToArray();
            }

            var items = _intensityCsvItems.Where(o => o.Exit == exit && o.Date >= from && o.Date <= to).ToList();

            var result = new List<IntensityOutModel>();

            foreach (var item in items)
            {
                result.Add(new IntensityOutModel { date = item.Date.ToString("yyyy-MM-dd") + " " + item.Hour + ":00:00", r = item.R, s = item.S, t = item.T });
            }

            return Json(new { items = result });
        }

        public IActionResult Intensity2(string code, int mode, DateTime from, DateTime to, string exit)
        {
            var items = this._dataService.Intensity(code, from, to, exit).OrderBy(o => o.Date).ThenBy(o => o.Hour).ToList();

            var result = new List<IntensityOutModel>();

            foreach (var item in items)
            {
                result.Add(new IntensityOutModel { date = item.Date.ToString("yyyy-MM-dd") + " " + item.Hour + ":00:00", r = item.R, s = item.S, t = item.T });
            }

            return Json(new { items = result });
        }

        public IActionResult Histogram(string code, DateTime from, DateTime to, string exit)
        {
            var items = this._dataService.Histogram(code, from, to, exit);

            var result = new List<HistogramOutModel>();

            if (items.Count > 0)
            {
                var maxThreshold = items.Max(o => o.Threshold);
                var minThreshold = items.Min(o => o.Threshold);

                for (var i = minThreshold; i < maxThreshold; i += 1)
                {
                    var resultItem = new HistogramOutModel
                    {
                        threshold = (int)i,
                        r = items.Where(o => o.Fase == "FASE_R" && (int)o.Threshold == i).Sum(o => (int)o.Count),
                        s = items.Where(o => o.Fase == "FASE_S" && (int)o.Threshold == i).Sum(o => (int)o.Count),
                        t = items.Where(o => o.Fase == "FASE_T" && (int)o.Threshold == i).Sum(o => (int)o.Count)
                    };

                    result.Add(resultItem);
                }
            }

            return Json(new { items = result });
        }

        public IActionResult Fraud(string code, DateTime from, DateTime to)
        {
            var items = this._dataService.Fraud(code, from, to);

            var result = new List<FraudOutModel>();

            foreach (var item in items)
            {
                result.Add(new FraudOutModel { date = item.Date.ToString("yyyy-MM-dd"), ct = item.Ct, exit = item.Exit });
            }

            return Json(new { items = result });
        }

    }
}