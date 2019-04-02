using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using MVCManukauTech.Models.DB;
using MVCManukauTech.ViewModels;

namespace MVCManukauTech.Controllers
{
    public class QueryActorsController : Controller
    {
        private readonly NorthwindContext _context;

        public QueryActorsController(NorthwindContext context)
        {
            _context = context;
        }

        //GET /QueryActors/CustomersAndSuppliersByCity
        public IActionResult CustomersAndSuppliersByCity()
        {
            string sql =
                "SELECT CustomerId as QueryKey, City, CompanyName, ContactName, 'Customers' AS Relationship "
                 + "FROM Customers "
                 + "UNION "
                 + "SELECT CONVERT(nchar(5), SupplierId) AS QueryKey, City, CompanyName, ContactName, 'Suppliers' "
                 + "FROM Suppliers";
            var actors = _context.ActorsByCityViewModel.FromSql(sql).ToList();
            return View(actors);
        }
    }
}