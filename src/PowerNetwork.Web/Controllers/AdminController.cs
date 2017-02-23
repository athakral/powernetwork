using Microsoft.AspNetCore.Mvc;
using PowerNetwork.Core.DataModels;
using Microsoft.Extensions.Options;

namespace PowerNetwork.Web.Controllers {
    public class AdminController : Controller {

        private readonly AppConfig _appConf;

        public AdminController(IOptions<AppConfig> appConfig) {
            _appConf = appConfig.Value;
        }

        // TODO (Hoa): still in working with AWS credentials
        public IActionResult Index() {
            return View();
        }

    }
}
