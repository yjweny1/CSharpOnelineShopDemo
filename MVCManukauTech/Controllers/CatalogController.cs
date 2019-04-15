using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using MVCManukauTech.Models.DB;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;

namespace MVCManukauTech.Controllers
{
    public class CatalogController : Controller
    {
        private readonly F191_tron01_XSpyContext _context;

        public CatalogController(F191_tron01_XSpyContext context)
        {
            _context = context;
        }

        public IActionResult Index(string categoryName, string searchString)
        {
            if (categoryName != null)
            {
                ViewData["categoryName"] = categoryName;
            }
            else if (searchString != null)
            {
                ViewData["searchString"] = searchString;
            }
            return View();
        }

        // GET: Catalog?CategoryName=Travel
        public string AjaxIndex()
        {
            string json = "";
            string search = "";
            //140903 JPC add CategoryName to SELECT list of fields
            string SQL = "SELECT ProductId, Product.CategoryId AS CategoryId, Name, ImageFileName, UnitCost"
                + ", SUBSTRING(Description, 1, 100) + '...' AS Description, CategoryName "
                + "FROM Product INNER JOIN Category ON Product.CategoryId = Category.CategoryId ";
            string categoryName = Request.Query["CategoryName"];
            string searchString = Request.Query["searchString"];

            if (categoryName != null)
            {
                if (categoryName.Length > 20 || categoryName.IndexOf("'") > -1 || categoryName.IndexOf("#") > -1)
                {
                    return json;
                }

                SQL += " WHERE CategoryName = @p0";
                search = categoryName;
                ViewBag.CategoryName = categoryName;
            }
            else if (searchString != null)
            {
                if (searchString.Length > 50 || searchString.IndexOf("'") > -1 || searchString.IndexOf("#") > -1)
                {
                    return json;
                }
                search = "%" + searchString + "%";
                SQL += " WHERE Name LIKE @p0";
            }

            //150807 JPC Security improvement implementation of @p0
            var products = _context.CatalogViewModel.FromSql(SQL, search);

            json = JsonConvert.SerializeObject(products);
            return json;
        }

        // GET: Catalog/Details?ProductId=1MORE4ME
        public IActionResult Details(string ProductId)
        {
            if (ProductId == null)
            {
                return BadRequest(); // Http status code 400
            }
            //140903 JPC security check - if ProductId is dodgy then return bad request and log the fact 
            //  of a possible hacker attack.  Excessive length or containing possible control characters
            //  are cause for concern!  TODO move this into a separate reusable code method with more sophistication.
            if (ProductId.Length > 20 || ProductId.IndexOf("'") > -1 || ProductId.IndexOf("#") > -1)
            {
                //TODO Code to log this event and send alert email to admin
                return BadRequest(); // Http status code 400
            }

            //150807 JPC Security improvement implementation of @p0
            //20180312 JPC change to query based on class CatalogViewModel
            //  Change above code to give all of the description rather than summarising with the first 100 characters
            string SQL = "SELECT ProductId, Product.CategoryId AS CategoryId, Name, ImageFileName, UnitCost"
            + ", Description, CategoryName "
            + "FROM Product INNER JOIN Category ON Product.CategoryId = Category.CategoryId "
            + " WHERE ProductId = @p0";

            //140904 JPC case of one product to look at the details.
            //  SQL gives some kind of collection where we need to clean that up with ToList() then take element [0]
            //150807 JPC Security improvement implementation of @p0 substitute ProductId
            var product = _context.CatalogViewModel.FromSql(SQL, ProductId).ToList()[0];
            if (product == null)
            {
                return NotFound(); //Http status code 404
            }
            return View(product);
        }

    }
}
