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
    public class VendorsController : Controller
    {
        DbEnt db = new DbEnt();

        // GET: Vendors
        [CustomAuthorize(Roles = "Super Administrator,Administrator,Manager,Employee")]

        public ActionResult Index(int? page)
        {
            int id = CompanyID();
            List<Vendor> items = db.Vendors.Where(x => x.companyID == id).ToList();
            Calculations calc = new Calculations();
            foreach (var item in items)
            {
                item.balance = calc.VendorBalance(id,item.vendorID);
            
            }
            ViewBag.OnePageOfUsers = items;
           
         
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
        [CustomAuthorize(Roles = "Super Administrator,Administrator,Manager,Employee")]

        public ActionResult Create(Vendor c)
        {
            int id = CompanyID();
            c.companyID = id;
            c.dateCreated = DateTime.Now;
            db.Vendors.Add(c);
            var check = db.SaveChanges();
            if (check != 0)
            {
                return RedirectToAction("", "Vendors");
            }


            return View();
        }



        [HttpGet]
        [CustomAuthorize(Roles = "Super Administrator,Administrator,Manager,Employee")]
        public ActionResult Edit(int id)
        {
            int companyID = CompanyID();
            ;
            return View(db.Vendors.Single(x => x.vendorID == id));
        }


        [HttpPost]
        [CustomAuthorize(Roles = "Super Administrator,Administrator,Manager,Employee")]
        public ActionResult Edit(int id, Vendor c)
        {
            int companyID = CompanyID();



            var item = db.Vendors.Find(id);
            item.name = c.name;
            item.phone = c.phone;
            item.email = c.email;
            item.address = c.address;
            item.taxNo = c.taxNo;
            db.Entry(item).State = EntityState.Modified;
            var check = db.SaveChanges();
            if (check != 0)
            {

                return RedirectToAction("", "vendors");
            }


            return View();
        }


        [HttpPost]
        [CustomAuthorize(Roles = "Super Administrator,Administrator,Manager,Employee")]
        public ActionResult Delete(int id)
        {
            Vendor i = db.Vendors.Find(id);
            db.Vendors.Remove(i);
            var check = db.SaveChanges();
            if (check != 0)
            {
                return RedirectToAction("", "vendors");
            }

            return View();
        }
    }
}