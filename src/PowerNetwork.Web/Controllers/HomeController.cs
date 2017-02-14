using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using PowerNetwork.Core.DataModels;
using Newtonsoft.Json.Linq;
using Microsoft.AspNetCore.Http;
using PowerNetwork.Core.Helpers;
using PowerNetwork.Web.Models;
// Needed for the SetString and GetString extension methods

namespace PowerNetwork.Web.Controllers
{
    public class HomeController : Controller
    {
        private readonly AppConfig _appConf;
        private readonly ILogger _logger;

        public HomeController(IOptions<AppConfig> appConfig, ILogger<HomeController> logger)
        {
            this._appConf = appConfig.Value;
            this._logger = logger;
        }


        public IActionResult Error()
        {
            return View();
        }

        public IActionResult Index(string l)
        {
            if (User.Identity.IsAuthenticated)
            {
                return RedirectToAction("Main");
            }
            ViewBag.AppConf = this._appConf;
            _logger.LogInformation("Application Configuration", this._appConf);
            return View(Request.GetSubDomain());
        }

        [HttpPost]
        public async Task<IActionResult> Login([FromBody] LoginModel loginModel)
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState
                    .SelectMany(x => x.Value.Errors, (y, z) => z.Exception.Message);

                return BadRequest(errors);
            }



            _logger.LogInformation("Passed Code " + loginModel.Code);
            var client = new HttpClient();
            var requestContent = new StringContent("{\"AccessToken\":\"" + loginModel.Code + "\"}", System.Text.Encoding.UTF8, "application/json");
            requestContent.Headers.Clear();
            requestContent.Headers.TryAddWithoutValidation("Content-Type", "application/x-amz-json-1.1");
            requestContent.Headers.Add("X-Amz-Target", "AWSCognitoIdentityProviderService.GetUser");
            requestContent.Headers.Add("X-Amz-User-Agent", "aws-sdk-js/2.6.4");


            var response = client.PostAsync("https://cognito-idp.eu-west-1.amazonaws.com/", requestContent).Result;
            _logger.LogInformation("Server's Reponse", response);

            if (response.IsSuccessStatusCode)
            {

                var awsUser = (await response.Content.ReadAsStringAsync()
                        .ContinueWith<JObject>(postTask => JsonConvert.DeserializeObject<JObject>(postTask.Result)))
                    .GetValue("Username");

                if (string.IsNullOrEmpty(awsUser?.ToString())) return Json("");
                var claims = new List<Claim> {
                    new Claim(ClaimTypes.Name,awsUser.ToString()),
                    new Claim("Read","true")
                };

                var claimsIdentity = new ClaimsIdentity(claims, "password");
                var claimsPrinciple = new ClaimsPrincipal(claimsIdentity);
                await HttpContext.Authentication.SignInAsync("Cookies", claimsPrinciple, new AuthenticationProperties
                {
                    IsPersistent = true
                });
                return Json(awsUser.ToString());
            }

            return Json("");
        }

        public async Task<IActionResult> Logout()
        {
            await HttpContext.Authentication.SignOutAsync("Cookies");
            return RedirectToAction("Index");
        }

        [Authorize(Policy = "ReadPolicy")]
        [Route("main")]
        public IActionResult Main()
        {
            return View();
        }

        [Authorize(Policy = "ReadPolicy")]
        [Route("power-outlet")]
        // [Route("Home/Index")]
        public IActionResult PowerOutlet()
        {
            return View();
        }

        [Authorize(Policy = "ReadPolicy")]
        [Route("fraud")]
        public IActionResult Fraud()
        {
            return View();
        }

    }
}
