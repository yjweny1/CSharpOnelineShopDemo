using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MVCManukauTech.Models.DB;

using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using Microsoft.AspNetCore.Mvc.Rendering;
using MVCManukauTech.ViewModels;

namespace MVCManukauTech.Controllers
{
    [Authorize(Roles = "Admin")]
    public class UserController : Controller
    {
        private readonly F191_tron01_XSpyContext _context;

        public UserController(F191_tron01_XSpyContext context)
        {
            _context = context;
        }
        public IActionResult Index()
        {
            ViewBag.isButton = true;
            string sql = @"SELECT u.Id AS UserId, u.UserName, u.Email, u.EmailConfirmed, u.PhoneNumber, l.Id AS RoleId, l.Name As RoleName
                FROM AspNetUsers u 
                LEFT JOIN AspNetUserRoles ul ON u.Id = ul.UserId
                LEFT JOIN AspNetRoles l ON ul.RoleId = l.Id";
            var user = _context.UserViewModel.FromSql(sql).ToList();
            return View(user);
        }

        // GET: User/Edit/5
        public IActionResult Edit(string id)
        {
            ViewBag.isButton = true;
            if (id == null)
            {
                return NotFound();
            }

            string sql = @"SELECT u.Id AS UserId, u.UserName, u.Email, u.EmailConfirmed, u.PhoneNumber, l.Id AS RoleId, l.Name As RoleName
                FROM AspNetUsers u 
                LEFT JOIN AspNetUserRoles ul ON u.Id = ul.UserId
                LEFT JOIN AspNetRoles l ON ul.RoleId = l.Id
                WHERE u.Id = @p0";
            var user = _context.UserViewModel.FromSql(sql, id).ToList()[0];
            sql = @"SELECT * FROM AspNetRoles";
            var roles = _context.AspNetRoles.FromSql(sql).ToList();
            ViewData["Roles"] = new SelectList(roles, "Id", "Name");
            return View(user);
        }

        // POST: Products/Edit/5
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        public IActionResult EditRole()
        {
            string id = Request.Form["UserId"];
            string RoleName = Request.Form["RoleName"];
            if (id == null || RoleName == null)
            {
                return NotFound();
            }

            string sql = "SELECT * FROM AspNetUserRoles WHERE UserId = @p0";
            var userRole = _context.AspNetUserRoles.FromSql(sql, id).ToList();
            if (userRole.Count != 1)
            {
                sql = "INSERT INTO AspNetUserRoles (UserId, RoleId) VALUES (@p0, @p1)";
                _context.Database.ExecuteSqlCommand(sql, id, RoleName);
            }
            else
            {
                sql = "UPDATE AspNetUserRoles SET RoleId = @p0 WHERE UserId = @p1";
                _context.Database.ExecuteSqlCommand(sql, RoleName, id);
            }
            return RedirectToAction(nameof(Index)); 
        }
    }
}