using easycounting.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace easycounting.Controllers
{
    public class TaxController : Controller
    {
        DbEnt db = new DbEnt();
        public int CompanyID()
        {
            var cookie = HttpUtility.UrlDecode(Request.Cookies["companyID"].Value.ToString());

            int companyID = Convert.ToInt32(cookie);
            return companyID;
        }
        [HttpPost]
        [CustomAuthorize(Roles = "Super Administrator")]
        public ActionResult Create(FormCollection f)
        {
            string username = User.Identity.Name;
            using (var db = new DbEnt())
            {
                int compID = CompanyID();
                var company = db.Users.Where(x => x.username == username).SingleOrDefault();
                if (company != null)
                {

                    Tax tax = new Tax();
                    tax.name = f["taxName"].ToString();
                    tax.description = f["taxDescription"].ToString();
                    tax.value = Convert.ToInt32(f["taxValue"].ToString());
                    tax.companyID = Convert.ToInt32(compID);
                    db.Taxes.Add(tax);
                    var check = db.SaveChanges();
                    if (check != 0)
                    {
                        
                            return RedirectToAction("taxes", "settings");

                    }
                }



            }
            return View();
        }


        [HttpPost]
        public ActionResult Delete(int id)
        {
            Tax i = db.Taxes.Find(id);
            db.Taxes.Remove(i);
            var check = db.SaveChanges();
            if (check != 0)
            {
                return RedirectToAction("taxes", "settings");
            }

            return View();
        }

        [HttpPost]
        public ActionResult Edit(int id, FormCollection f)
        {
            string name = f["taxName"].ToString();
            int value = Convert.ToInt32(f["taxValue"].ToString());
            string description = f["taxDescription"].ToString();
            var tax = db.Taxes.Find(id);
            tax.name = name;
            tax.description = description;
            tax.value = value;
            var check = db.SaveChanges();
            if (check != 0)
            {
                return RedirectToAction("taxes", "settings");
            }
            return View();
        }
    }
}