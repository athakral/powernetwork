using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using PowerNetwork.Core.DataModels;
using Jeffijoe.HttpClientGoodies;
using Newtonsoft.Json.Linq;

namespace PowerNetwork.Web.Controllers
{
    public class HomeController : Controller
    {
        private readonly IHostingEnvironment _hostingEnvironment;
        private readonly AppConfig _appConf;
        private readonly ILogger _logger;

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
            ViewBag.AppConf = this._appConf;
            ViewBag.HostingEnvironment = this._hostingEnvironment;
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

            //var anotherResponse = await RequestBuilder.Post("https://cognito-idp.eu-west-1.amazonaws.com/")
            //    .Content(new StringContent(JsonConvert.SerializeObject(new { AccessToken = code })))
            //    .AddHeaders(new List<KeyValuePair<string, object>>()
            //    {
            //        new KeyValuePair<string, object>("Content-Type", "application/x-amz-json-1.1"),
            //        new KeyValuePair<string, object>("X-Amz-Target", "AWSCognitoIdentityProviderService.GetUser"),
            //        new KeyValuePair<string, object>("X-Amz-User-Agent", "aws-sdk-js/2.6.4")
            //    })
            //    .SendAsync()
            //    .AsJson<dynamic>();
            //var clntHand = new HttpClientHandler()
            //{
            //    CookieContainer = new CookieContainer(),
            //    Proxy = new WebProxy(),
            //    UseProxy = true,
            //    UseDefaultCredentials = false
            //};

            _logger.LogInformation("Passed Code " + code);
            var client = new HttpClient();
            //var client = new HttpClient(clntHand);
            // client.DefaultRequestHeaders.Clear();
            // client.DefaultRequestHeaders.Add("ContentType", "application/x-amz-json-1.1");
            // client.DefaultRequestHeaders.Add("X-Amz-Target", "AWSCognitoIdentityProviderService.GetUser");
            // client.DefaultRequestHeaders.Add("X-Amz-User-Agent", "aws-sdk-js/2.6.4");
            var requestContent = new StringContent("{\"AccessToken\":\"" + code + "\"}", System.Text.Encoding.UTF8, "application/json");
            requestContent.Headers.Clear();
            requestContent.Headers.TryAddWithoutValidation("Content-Type", "application/x-amz-json-1.1");
            requestContent.Headers.Add("X-Amz-Target", "AWSCognitoIdentityProviderService.GetUser");
            requestContent.Headers.Add("X-Amz-User-Agent", "aws-sdk-js/2.6.4");


            var response = client.PostAsync("https://cognito-idp.eu-west-1.amazonaws.com/", requestContent).Result;
            _logger.LogInformation("Server's Reponse", response);

            if (response.IsSuccessStatusCode)
            {

                //(awsUser as Newtonsoft.Json.Linq.JObject).GetValue("Username").ToString()

                var awsUser = (await response.Content.ReadAsStringAsync()
                        .ContinueWith<JObject>(postTask => JsonConvert.DeserializeObject<JObject>(postTask.Result)))
                    .GetValue("Username");

                if (string.IsNullOrEmpty(awsUser?.ToString())) return Json("");
                var claims = new List<Claim> {
                    new Claim(ClaimTypes.Name,awsUser.ToString()),
                };

                var claimsIdentity = new ClaimsIdentity(claims, "password");
                var claimsPrinciple = new ClaimsPrincipal(claimsIdentity);
                await HttpContext.Authentication.SignInAsync("Cookies", claimsPrinciple);
                // FormsAuthentication.SetAuthCookie(username, true);
                return Json(awsUser.ToString());
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
            ViewBag.HostingEnvironment = this._hostingEnvironment;
            return View();
        }

        [Authorize]
        public IActionResult PowerOutlet(string l)
        {
            ViewBag.LanguageCode = l;
            ViewBag.HostingEnvironment = this._hostingEnvironment;
            return View();
        }

        [Authorize]
        public IActionResult Fraud(string l)
        {
            ViewBag.LanguageCode = l;
            ViewBag.HostingEnvironment = this._hostingEnvironment;
            return View();
        }

    }
}
