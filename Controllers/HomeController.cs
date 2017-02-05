using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Microsoft.AspNetCore.Authorization;

namespace GnfSmartMeters.Controllers
{
    public class HomeController : Controller
    {
        public IActionResult Index()
        {
            return View();
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

    //    public IActionResult Index(string l) {
    //         ViewBag.LanguageCode = l;
    //         return View();
    //     }

        // [HttpPost]
        // public IActionResult Login(string code) {
        //     try {
        //         var client = new WebClient();
        //         client.Headers[HttpRequestHeader.ContentType] = "application/x-amz-json-1.1";
        //         client.Headers["X-Amz-Target"] = "AWSCognitoIdentityProviderService.GetUser";
        //         client.Headers["X-Amz-User-Agent"] = "aws-sdk-js/2.6.4";

        //         var response = client.UploadString("https://cognito-idp.eu-west-1.amazonaws.com/", "{\"AccessToken\":\"" + code + "\"}");

        //         var username = JsonConvert.DeserializeObject<dynamic>(response).Username.Value as string;
        //         if (!string.IsNullOrEmpty(username)) {
        //             FormsAuthentication.SetAuthCookie(username, true);
        //             return Json(username);
        //         }

        //     } catch (Exception ex) {
        //     }

        //     return Json("");
        // }

        // public IActionResult Logout() {
        //     FormsAuthentication.SignOut();
        //     return RedirectToAction("Index");
        // }

        [Authorize]
        public IActionResult Main(string l) {
            ViewBag.LanguageCode = l;
            return View();
        }

        [Authorize]
        public IActionResult PowerOutlet(string l) {
            ViewBag.LanguageCode = l;
            return View();
        }

        [Authorize]
        public IActionResult Fraud(string l) {
            ViewBag.LanguageCode = l;
            return View();
        }
 
    }
}
