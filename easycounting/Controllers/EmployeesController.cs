using easycounting.Models;
using PagedList;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace easycounting.Controllers
{
    public class EmployeesController : Controller
    {
        public int CompanyID()
        {
            var cookie = HttpUtility.UrlDecode(Request.Cookies["companyID"].Value.ToString());

            int companyID = Convert.ToInt32(cookie);
            return companyID;
        }
        
        DbEnt db = new DbEnt();

        // GET: Employees
        public ActionResult Index(int?page)
        {
            string username = User.Identity.Name;
            int id = CompanyID();
            ViewBag.aa = username;


            var items = db.EmployeesInCompanies.Where(x => x.Company.companyID == id).ToList().OrderByDescending(x => x.id);
            ViewBag.OnePageOfUsers = items;

            if (items != null)
                return View(items.ToPagedList(page ?? 1, 10));
            else
                return View();
        }

        [HttpGet]
        public ActionResult Create()
        {
            return View();
        }

        [HttpPost]
        public ActionResult Create(Employee e)
        {
            int cmopanyID = CompanyID();
            Crypto crypto = new Crypto();
            Employee employee = new Employee { 
            name = e.name,
            email = e.email,
            neto = crypto.Base64Encode(e.neto),
            bruto = crypto.Base64Encode(e.bruto),
            bonus = crypto.Base64Encode(e.bonus),
            department = e.department,
            position = e.position
            };
            var row = db.Employees.Add(employee);
            EmployeesInCompany ec = new EmployeesInCompany {
                companyID = cmopanyID,
                employeeID = row.employeeID
            };
            db.EmployeesInCompanies.Add(ec);

            var check = db.SaveChanges();
            if (check != 0)
            {
                return RedirectToAction("", "employees");
            }
            return View();
        }

        [HttpGet]
        public ActionResult Edit(int id)
        {
            int companyID = CompanyID();
        
            var edit = db.Employees.Single(x => x.employeeID == id);
            var company = db.EmployeesInCompanies.Where(x => x.companyID == companyID && x.employeeID == id).FirstOrDefault();
            if (company == null)
            {
                return RedirectToAction("notfound", "error");

            }
            else
            {
                Crypto decrypt = new Crypto();

                ViewBag.neto = decrypt.Base64Decode(edit.neto);
                ViewBag.bruto = decrypt.Base64Decode(edit.bruto);
                ViewBag.bonus = decrypt.Base64Decode(edit.bonus);
                return View(edit);
            }
            
        }

        [HttpPost]
        public ActionResult Edit(int id,EmployeesInCompany e)
        {
            var row = db.Employees.Find(id);
            Crypto crypto = new Crypto();
            row.name = e.Employee.name;
            row.email = e.Employee.email;
            row.neto = crypto.Base64Encode(e.Employee.neto);
            row.bruto = crypto.Base64Encode(e.Employee.bruto);
            row.bonus = crypto.Base64Encode(e.Employee.bonus);
            row.department = e.Employee.department;
            row.position = e.Employee.position;
            var check = db.SaveChanges();
            if (check != 0)
            {
                return RedirectToAction("", "employees");
            }

            return View();
        }


        [HttpPost]
        public ActionResult Delete(int id)
        {
            var row = db.Employees.Where(x => x.employeeID == id).SingleOrDefault();
            db.Employees.Remove(row);
            var check = db.SaveChanges();
            if (check != 0)
            {
                return RedirectToAction("", "employees");
            }
            return View();
        }
    }
}