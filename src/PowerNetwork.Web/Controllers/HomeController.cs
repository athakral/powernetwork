using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http.Authentication;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using PowerNetwork.Core.DataModels;
using Newtonsoft.Json.Linq;
using PowerNetwork.Web.Models;

namespace PowerNetwork.Web.Controllers {
    public class HomeController : Controller {

        private readonly IHostingEnvironment _hostingEnvironment;
        private readonly AppConfig _appConf;
        private readonly ILogger _logger;

        public HomeController(IHostingEnvironment hostingEnvironment, IOptions<AppConfig> appConfig, ILogger<HomeController> logger) {
            _hostingEnvironment = hostingEnvironment;
            _appConf = appConfig.Value;
            _logger = logger;
        }

        public IActionResult Error() {
            return View();
        }

        public IActionResult Index(string l, string code) {
            if (User.Identity.IsAuthenticated) {
                return RedirectToAction("Main");
            }

            if (!string.IsNullOrEmpty(code)) {
                _logger.LogInformation("Passed Code" + code);

                var requestContent = new StringContent("{\"AccessToken\":\"" + code + "\"}", System.Text.Encoding.UTF8, "application/json");
                requestContent.Headers.Clear();
                requestContent.Headers.TryAddWithoutValidation("Content-Type", "application/x-amz-json-1.1");
                requestContent.Headers.Add("X-Amz-Target", "AWSCognitoIdentityProviderService.GetUser");
                requestContent.Headers.Add("X-Amz-User-Agent", "aws-sdk-js/2.6.4");

                var client = new HttpClient();
                var response = client.PostAsync("https://cognito-idp.eu-west-1.amazonaws.com/", requestContent).Result;

                _logger.LogInformation("Server's Reponse", response);

                if (response.IsSuccessStatusCode) {
                    var awsUser = JsonConvert.DeserializeObject<JObject>(response.Content.ReadAsStringAsync().Result).GetValue("Username");

                    if (!string.IsNullOrEmpty(awsUser?.ToString())) {
                        var claims = new List<Claim> { new Claim(ClaimTypes.Name, awsUser.ToString()), new Claim("Read", "true") };

                        var claimsIdentity = new ClaimsIdentity(claims, "password");
                        var claimsPrinciple = new ClaimsPrincipal(claimsIdentity);

                        HttpContext.Authentication.SignInAsync("Cookies", claimsPrinciple, new AuthenticationProperties { IsPersistent = true });
                        return RedirectToAction("Main");
                    }
                }
            }

            ViewBag.AppConf = _appConf;
            _logger.LogInformation("Application Configuration", _appConf);
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Login([FromBody] LoginModel loginModel) {
            if (!ModelState.IsValid) {
                var errors = ModelState.SelectMany(x => x.Value.Errors, (y, z) => z.Exception.Message);
                return BadRequest(errors);
            }

            _logger.LogInformation("Passed Code " + loginModel.Code);

            var requestContent = new StringContent("{\"AccessToken\":\"" + loginModel.Code + "\"}", System.Text.Encoding.UTF8, "application/json");
            requestContent.Headers.Clear();
            requestContent.Headers.TryAddWithoutValidation("Content-Type", "application/x-amz-json-1.1");
            requestContent.Headers.Add("X-Amz-Target", "AWSCognitoIdentityProviderService.GetUser");
            requestContent.Headers.Add("X-Amz-User-Agent", "aws-sdk-js/2.6.4");

            var client = new HttpClient();
            var response = client.PostAsync("https://cognito-idp.eu-west-1.amazonaws.com/", requestContent).Result;

            _logger.LogInformation("Server's Reponse", response);

            if (response.IsSuccessStatusCode) {
                var awsUser =
                    (await response.Content.ReadAsStringAsync().ContinueWith(postTask => JsonConvert.DeserializeObject<JObject>(postTask.Result)))
                        .GetValue("Username");

                if (string.IsNullOrEmpty(awsUser?.ToString())) return Json("");

                var claims = new List<Claim> {
                    new Claim(ClaimTypes.Name,awsUser.ToString()),
                    new Claim("Read","true")
                };

                var claimsIdentity = new ClaimsIdentity(claims, "password");
                var claimsPrinciple = new ClaimsPrincipal(claimsIdentity);

                await HttpContext.Authentication.SignInAsync("Cookies", claimsPrinciple, new AuthenticationProperties {
                    IsPersistent = true
                });

                return Json(awsUser.ToString());
            }

            return Json("");
        }

        public async Task<IActionResult> Logout() {
            await HttpContext.Authentication.SignOutAsync("Cookies");
            return RedirectToAction("Index");
        }

        [Authorize(Policy = "ReadPolicy")]
        [Route("main")]
        public IActionResult Main() {
            //if (_hostingEnvironment.EnvironmentName == "Demo" || _hostingEnvironment.EnvironmentName == "DemoProduction") {
            //    return Redirect("/power-outlet");
            //}

            return View();
        }

        [Authorize(Policy = "ReadPolicy")]
        [Route("power-outlet")]
        public IActionResult PowerOutlet() {
            ViewBag.AppConf = _appConf;
            ViewBag.CurrentTab = "power";
            return View();
        }

        [Authorize(Policy = "ReadPolicy")]
        [Route("fraud")]
        public IActionResult Fraud() {
            ViewBag.AppConf = _appConf;
            ViewBag.CurrentTab = "fraud";
            return View();
        }

        [Authorize(Policy = "ReadPolicy")]
        [Route("mpgs")]
        public IActionResult Mpgs() {
            ViewBag.AppConf = _appConf;
            return View();
        }

    }
}
