using easycounting.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace easycounting.Controllers
{
    public class CategoryController : Controller
    {
        DbEnt db = new DbEnt();
        [HttpPost]
        [CustomAuthorize(Roles = "Super Administrator")]
        public ActionResult Create(FormCollection f, string from, int? id)
        {
            string username = User.Identity.Name;
            using (var db = new DbEnt())
            {
                var company = db.Users.Where(x => x.username == username).SingleOrDefault();
                if (company != null)
                {
                    int compID = CompanyID();
                    ItemsCategory category = new ItemsCategory();
                    category.name = f["category"].ToString();
                    category.description = f["descriptionCategory"].ToString();
                        category.companyID = Convert.ToInt32(compID);
                    db.ItemsCategories.Add(category);
                    var check = db.SaveChanges();
                    if (check != 0)
                    {
                        if (from == "create")
                        return RedirectToAction("create","items");
                        if (from == "edit")
                            return RedirectToAction("edit", "items", new { id = id }) ;
                        if (from == "settings")
                            return RedirectToAction("itemcategories", "settings");

                    }
                }
               
                
            
            }
            return View();
        }

        [HttpPost]
        public ActionResult Delete(int id)
        {
            ItemsCategory i = db.ItemsCategories.Find(id);
            db.ItemsCategories.Remove(i);
            var check = db.SaveChanges();
            if (check != 0)
            {
                return RedirectToAction("itemcategories", "settings");
            }

            return View();
        }

        [HttpPost]
        public ActionResult Edit(int id,FormCollection f)
        {
            string name = f["category"].ToString();
            string description = f["descriptionCategory"].ToString();
            var category = db.ItemsCategories.Find(id);
            category.name = name;
            category.description = description;
            var check = db.SaveChanges();
            if (check != 0)
            {
                return RedirectToAction("itemcategories", "settings");
            }
            return View();
        }
        public int CompanyID()
        {
            var cookie = HttpUtility.UrlDecode(Request.Cookies["companyID"].Value.ToString());

            int companyID = Convert.ToInt32(cookie);
            return companyID;
        }

    }
}