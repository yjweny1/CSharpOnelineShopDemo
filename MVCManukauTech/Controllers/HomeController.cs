using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MVCManukauTech.Models;
using MVCManukauTech.Models.DB;
using Newtonsoft.Json;

namespace MVCManukauTech.Controllers
{
    public class HomeController : Controller
    {
        private readonly F191_tron01_XSpyContext _context;

        public HomeController(F191_tron01_XSpyContext context)
        {
            _context = context;
        }

        public IActionResult Index()
        {
            return View();
        }

        public string AjaxIndex()
        {
            string sql = @"SELECT TOP 6 * FROM Product ORDER BY RAND();";
            var result = _context.Product.FromSql(sql);
            string json = JsonConvert.SerializeObject(result);
            return json;
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
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
