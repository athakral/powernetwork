using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using CsvHelper;
using CsvHelper.Configuration;
using MailKit.Net.Smtp;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using PowerNetwork.Core.DataModels;
using Microsoft.Extensions.Options;
using MimeKit;
using PowerNetwork.Core.Helpers;
using PowerNetwork.Web.Models;
using System.Linq;
using System.Text;
using MailKit.Security;

namespace PowerNetwork.Web.Controllers {
    public class AdminController : Controller {

        private readonly AppConfig _appConf;
        private readonly IHostingEnvironment _hostingEnvironment;

        private readonly IDataService _dataService;
        private readonly string _rootDataFolder;

        public AdminController(IOptions<AppConfig> appConfig, IHostingEnvironment hostingEnvironment, IDataService dataService) {
            _appConf = appConfig.Value;
            _hostingEnvironment = hostingEnvironment;

            _dataService = dataService;
            _rootDataFolder = _hostingEnvironment.EnvironmentName == "Demo" || _hostingEnvironment.EnvironmentName == "DemoProduction" ? "data/sample/" : "data/";
        }

        // TODO (Hoa): still in working with AWS credentials
        public IActionResult Index() {
            ViewBag.AppConf = _appConf;
            return View();
        }

        [Authorize(Policy = "ReadPolicy")]
        public IActionResult SendMail() {
            return View();
        }

        private static CtsModel[] _ctsItems;

        public IActionResult SendTestMail(string email, string center) {
            var result = "";

            try {
                // prepare data
                var date2 = DateTime.Now.Date;
                var date1 = date2.AddDays(-30);

                var overloadAlarms = _dataService.OverloadAlarms(0, 100, 0);
                var unbalanceAlarms = _dataService.UnbalanceAlarms(0, 100, 0);

                if (_ctsItems == null) {
                    var csvReaderCts = new CsvReader(System.IO.File.OpenText(Path.Combine(_hostingEnvironment.WebRootPath, _rootDataFolder + "cts_v1.2.csv")),
                        new CsvConfiguration { HasHeaderRecord = false, WillThrowOnMissingField = false });

                    _ctsItems = csvReaderCts.GetRecords<CtsModel>().ToArray();
                }

                var rowsHtml = new StringBuilder();
                var overloadCount = 0;
                var unbalanceCount = 0;

                foreach (var overloadAlarm in overloadAlarms) {
                    var cts = _ctsItems.FirstOrDefault(o => o.othercode == overloadAlarm.Code);
                    if (cts == null || cts.center != center) continue;

                    rowsHtml.Append("<tr>");

                    rowsHtml.Append("<td>" + date1.ToString("dd-MM-yyyy") + "</td><td>" + date2.ToString("dd-MM-yyyy") + "</td>");
                    rowsHtml.Append("<td>" + cts.othercode + "</td><td>" + cts.city + "</td>");

                    overloadCount++;
                    rowsHtml.Append("<td>" + overloadAlarm.Ratio + "</td>");
                    rowsHtml.Append("<td>" + (overloadAlarm.Data[0] ?? "") + "</td>");
                    rowsHtml.Append("<td>" + (overloadAlarm.Data[1] ?? "") + "</td>");

                    var unbalanceAlarm = unbalanceAlarms.FirstOrDefault(o => o.Code == cts.othercode);
                    if (unbalanceAlarm != null) {
                        unbalanceCount++;
                        rowsHtml.Append("<td>" + unbalanceAlarm.Ratio + "</td>");
                    } else {
                        rowsHtml.Append("<td></td>");
                    }

                    rowsHtml.Append("<td>" + _dataService.MaxExit(cts.othercode) + "</td>");

                    rowsHtml.Append("</tr>");
                }

                foreach (var unbalanceAlarm in unbalanceAlarms) {
                    if (overloadAlarms.Exists(o => o.Code == unbalanceAlarm.Code)) continue;

                    var cts = _ctsItems.FirstOrDefault(o => o.othercode == unbalanceAlarm.Code);
                    if (cts == null || cts.center != center) continue;

                    rowsHtml.Append("<tr>");

                    rowsHtml.Append("<td>" + date1.ToString("dd-MM-yyyy") + "</td><td>" + date2.ToString("dd-MM-yyyy") + "</td>");
                    rowsHtml.Append("<td>" + cts.othercode + "</td><td>" + cts.city + "</td>");
                    rowsHtml.Append("<td></td><td></td><td></td>");

                    unbalanceCount++;
                    rowsHtml.Append("<td>" + unbalanceAlarm.Ratio + "</td>");
                    rowsHtml.Append("<td>" + _dataService.MaxExit(cts.othercode) + "</td>");

                    rowsHtml.Append("</tr>");
                }

                // build email
                var subject = "[TCE] Informe periódico de anomalías " + date2.ToString("dd-MM-yyyy");

                var body = System.IO.File.ReadAllText(Path.Combine(_hostingEnvironment.WebRootPath, "emails/monthly-report.html"));
                body = body.Replace("${date}", date2.ToString("dd-MM-yyyy"));
                body = body.Replace("${center}", center.Replace("MTTO.", "").Trim().ToTitleCase());

                body = body.Replace("${day1}", date1.Day.ToString());
                body = body.Replace("${month1}", date1.Month.ToSpanishMonth());
                body = body.Replace("${day2}", date2.Day.ToString());
                body = body.Replace("${month2}", date2.Month.ToSpanishMonth());

                body = body.Replace("${overloadCount}", overloadCount.ToString());
                body = body.Replace("${unbalanceCount}", unbalanceCount.ToString());

                body = body.Replace("${dataRows}", rowsHtml.ToString());

                // start sending
                var emailMessage = new MimeMessage { Body = new BodyBuilder { HtmlBody = body }.ToMessageBody() };

                //// sending via Gmail
                //emailMessage.From.Add(new MailboxAddress("", "tmhdev.01@gmail.com"));
                //emailMessage.To.Add(new MailboxAddress("", email));

                //using (var client = new SmtpClient()) {
                //    client.Connect("smtp.gmail.com", 587, false);
                //    client.AuthenticationMechanisms.Remove("XOAUTH2");
                //    client.Authenticate("tmhdev.01@gmail.com", "minhhoa123");

                //    client.Send(emailMessage);
                //    client.Disconnect(true);
                //}

                // sending via SES
                emailMessage.From.Add(new MailboxAddress("", "minhhoa.work@gmail.com"));
                emailMessage.To.Add(new MailboxAddress("", email));
                
                using (var client = new SmtpClient()) {
                    client.Connect("email-smtp.eu-west-1.amazonaws.com", 587, SecureSocketOptions.StartTls);
                    client.Authenticate("AKIAI3OCKSODKATIYELA", "Ap+n1TbW2lW5Vvx237wYj8T7bSikUD/UB+Pod7RNXeNi");

                    client.Send(emailMessage);
                    client.Disconnect(true);
                }

                // return
                result = "Email has been sent successfully";

            } catch (Exception ex) {
                result = ex.Message;
            }

            return Json(new { result });
        }

    }

}
