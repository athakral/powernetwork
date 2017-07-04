using System.Text;
using Microsoft.AspNetCore.Mvc;
using PowerNetwork.Core.Helpers;

namespace PowerNetwork.Web.Controllers {
    public class MpgsDataController : Controller {

        private readonly IMpgsDataService _mpgsDataService;

        public MpgsDataController(IMpgsDataService mpgsDataService) {
            _mpgsDataService = mpgsDataService;
        }

        public IActionResult Common() {
            return Json(_mpgsDataService.Common());
        }

        public IActionResult Cts(double x1, double x2, double y1, double y2) {
            return Json(_mpgsDataService.Cts(x1, x2, y1, y2));
        }

        public IActionResult CtsSearch(string code) {
            return Json(_mpgsDataService.CtsSearch(code));
        }

        public IActionResult SummaryTable(string region, string city, string center,
            string code, int? actionType, int failRate0, int failRate1, int clientCount0, int clientCount1, int page) {

            return Json(_mpgsDataService.SummaryTable(region, city, center, code, actionType, failRate0, failRate1, clientCount0, clientCount1, page));
        }

        public IActionResult SummaryTableCsv(string region, string city, string center,
            string code, int? actionType, int failRate0, int failRate1, int clientCount0, int clientCount1) {

            var csv = _mpgsDataService.SummaryTableCsv(region, city, center, code, actionType, failRate0, failRate1, clientCount0, clientCount1);

            return File(new UTF8Encoding().GetBytes(csv), "text/csv", "AssetSummary.csv");
        }

        public IActionResult Relevance() {
            return Json(_mpgsDataService.Relevance());
        }

        public IActionResult Variables(int x) {
            return Json(_mpgsDataService.Variables(x));
        }

        public IActionResult Roc() {
            return Json(_mpgsDataService.Roc());
        }

        public IActionResult Lift() {
            return Json(_mpgsDataService.Lift());
        }

        public IActionResult Strategy2(int technical) {
            return Json(_mpgsDataService.Strategy2(technical));
        }

        public IActionResult Strategy3(int technical, int filter, double value) {
            return Json(_mpgsDataService.Strategy3(technical, filter, value));
        }

    }

}
