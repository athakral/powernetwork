using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Options;
using PowerNetwork.Core.DataModels;
using System.Net.Http;
using Microsoft.AspNetCore.Authentication.Cookies;
using System.Security.Claims;
using Microsoft.Extensions.Logging;

namespace PowerNetwor.Controllers
{
    public class HomeController : Controller
    {
        private IHostingEnvironment _hostingEnvironment;
        private AppConfig _appConf;
        private ILogger _logger;
        public HomeController(IHostingEnvironment hostingEnvironment, IOptions<AppConfig> appConfig, ILogger<HomeController> logger)
        {
            this._hostingEnvironment = hostingEnvironment;
            this._appConf = appConfig.Value;
            this._logger = logger;
        }
        public IActionResult About()
        {
            ViewData["Message"] = "Your application description page.";

            return View();
        }

        public IActionResult Contact()
        {
            ViewData["Message"] = "Your contact page.";

            return View();
        }

        public IActionResult Error()
        {
            return View();
        }

        public IActionResult Index(string l)
        {
            ViewBag.LanguageCode = l;
            ViewBag.HostingEnvironment = this._hostingEnvironment;
            ViewBag.AppConf = this._appConf;
            _logger.LogInformation("Application COnfiguration", this._appConf);
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Login([FromBody] string code)
        {

            if (!ModelState.IsValid)
            {
                var errors = ModelState
                    .SelectMany(x => x.Value.Errors, (y, z) => z.Exception.Message);

                return BadRequest(errors);
            }

            try
            {
                _logger.LogInformation("Passed Code " + code);
                var client = new HttpClient();
                // client.DefaultRequestHeaders.Clear();
                // client.DefaultRequestHeaders.Add("ContentType", "application/x-amz-json-1.1");
                // client.DefaultRequestHeaders.Add("X-Amz-Target", "AWSCognitoIdentityProviderService.GetUser");
                // client.DefaultRequestHeaders.Add("X-Amz-User-Agent", "aws-sdk-js/2.6.4");
                var requestContent = new StringContent("{\"AccessToken\":\"" + code + "\"}", System.Text.Encoding.UTF8, "application/json");
                requestContent.Headers.Remove("Content-Type");
                requestContent.Headers.Add("ContentType", "application/x-amz-json-1.1");
                requestContent.Headers.Add("X-Amz-Target", "AWSCognitoIdentityProviderService.GetUser");
                requestContent.Headers.Add("X-Amz-User-Agent", "aws-sdk-js/2.6.4");


                var response = client.PostAsync("https://cognito-idp.eu-west-1.amazonaws.com/", requestContent).Result;
                _logger.LogInformation("Server's Reponse", response);

                if (response.IsSuccessStatusCode)
                {
                    var awsUser = await response.Content.ReadAsStringAsync().ContinueWith<dynamic>(postTask =>
                    {
                        return JsonConvert.DeserializeObject<dynamic>(postTask.Result);
                    });

                    if (!string.IsNullOrEmpty(awsUser.Username))
                    {
                        var claims = new List<Claim> {
                                new Claim(ClaimTypes.Name,awsUser.Username),
                                };

                        var claimsIdentity = new ClaimsIdentity(claims, "password");
                        var claimsPrinciple = new ClaimsPrincipal(claimsIdentity);
                        await HttpContext.Authentication.SignInAsync("Cookies", claimsPrinciple);
                        // FormsAuthentication.SetAuthCookie(username, true);
                        return Json(awsUser.Username);
                    }
                }


            }
            catch (Exception ex)
            {
                throw;
            }

            return Json("");
        }

        public async Task<IActionResult> Logout()
        {
            await HttpContext.Authentication.SignOutAsync("Cookies");
            return RedirectToAction("Index");
        }

        [Authorize]
        public IActionResult Main(string l)
        {
            ViewBag.LanguageCode = l;
            return View();
        }

        [Authorize]
        public IActionResult PowerOutlet(string l)
        {
            ViewBag.LanguageCode = l;
            return View();
        }

        [Authorize]
        public IActionResult Fraud(string l)
        {
            ViewBag.LanguageCode = l;
            return View();
        }

    }
}
