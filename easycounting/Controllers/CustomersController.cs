using easycounting.Models;
using PagedList;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace easycounting.Controllers
{
    public class CustomersController : Controller
    {
        DbEnt db = new DbEnt();

        // GET: Customers
        [CustomAuthorize(Roles = "Super Administrator,Administrator,Manager,Employee")]

        public ActionResult Index(int? page)
        {
            Calculations calc = new Calculations();
            int id = CompanyID();
            List<Customer> items = db.Customers.Where(x => x.companyID == id).OrderByDescending(x=>x.dateCreated).ToList();
            ViewBag.OnePageOfUsers = items;
            foreach (var item in items)
            {
                item.balance = calc.CustomerBalance(id, item.customerID);

            }


            if (items != null)
                return View(items.ToPagedList(page ?? 1, 7));
            else
                return View();
        }

        public int CompanyID()
        {
            var cookie = HttpUtility.UrlDecode(Request.Cookies["companyID"].Value.ToString());

            int companyID = Convert.ToInt32(cookie);
            return companyID;
        }
        [HttpGet]
        [CustomAuthorize(Roles = "Super Administrator,Administrator,Manager,Employee")]
        public ActionResult Create()
        {
            string username = User.Identity.Name;
            var comp = db.Users.Where(x => x.username == username).SingleOrDefault();
            int id = CompanyID();
            return View();
        }

        [HttpPost]
        public ActionResult Create(Customer c)
        {
                int id = CompanyID();       
                c.companyID = id;
                c.dateCreated = DateTime.Now;
                db.Customers.Add(c);
                var check = db.SaveChanges();
                if (check != 0)
                {
                    return RedirectToAction("", "customers");
                }

            
            return View();
        }



        [HttpGet]
        [CustomAuthorize(Roles = "Super Administrator,Administrator,Manager,Employee")]
        public ActionResult Edit(int id)
        {
            int companyID = CompanyID();
           ;
            return View(db.Customers.Single(x => x.customerID == id));
        }


        [HttpPost]
        [CustomAuthorize(Roles = "Super Administrator,Administrator,Manager,Employee")]
        public ActionResult Edit(int id, Customer c)
        {
                int companyID = CompanyID();
          

            
                var item = db.Customers.Find(id);
                item.name = c.name;
                item.phone = c.phone;
                item.email = c.email;
                item.address = c.address;
                item.taxNo = c.taxNo;
                db.Entry(item).State = EntityState.Modified;
                var check = db.SaveChanges();
                if (check != 0)
                {

                    return RedirectToAction("", "customers");
                }


            return View();
        }


        [HttpPost]
        [CustomAuthorize(Roles = "Super Administrator,Administrator,Manager,Employee")]

        public ActionResult Delete(int id)
        {
            Customer i = db.Customers.Find(id);
            db.Customers.Remove(i);
            var check = db.SaveChanges();
            if (check != 0)
            {
                return RedirectToAction("", "customers");
            }

            return View();
        }
    }
}