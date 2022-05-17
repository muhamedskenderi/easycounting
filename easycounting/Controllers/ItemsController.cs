using easycounting.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using PagedList;
using System.Web.Mvc;
using System.Data.Entity;

namespace easycounting.Controllers
{
    public class ItemsController : Controller
    {
        DbEnt db = new DbEnt();

        // GET: Items
        [CustomAuthorize(Roles = "Super Administrator,Administrator,Manager,Employee")]
        public ActionResult Index(int? page)
        {
            int id = CompanyID();
            List<Item> items = db.Items.Where(x => x.companyID == id).ToList();
            ViewBag.OnePageOfUsers = items;


            if (items != null)
                return View(items.ToPagedList(page ?? 1, 5));
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
            var getcategories = db.ItemsCategories.Where(x=>x.companyID == id).ToList();
            SelectList list = new SelectList(getcategories, "categoryID", "name");
            ViewBag.category = list;
            return View();
        }

        [HttpPost]
        [CustomAuthorize(Roles = "Super Administrator,Administrator,Manager,Employee")]

        public ActionResult Create(Item i)
        {
            int id = CompanyID();
            var getcategories = db.ItemsCategories.Where(x => x.companyID == id).ToList();
            SelectList list = new SelectList(getcategories, "categoryID", "name");
            ViewBag.category = list;

            if (i.salePrice < i.purchasePrice)
            {
                ModelState.AddModelError("salePrice", "Sale price cannot be smaller than purchase price");
            }
            else
            {
               
                i.companyID = id;
                db.Items.Add(i);
                var check = db.SaveChanges();
                if (check != 0)
                {
                    return RedirectToAction("", "items");
                }

            }
            return View();
        }

        [HttpGet]
        [CustomAuthorize(Roles = "Super Administrator,Administrator,Manager,Employee")]
        public ActionResult Edit(int id)
        {
            int companyID = CompanyID();
            var getcategories = db.ItemsCategories.Where(x=>x.companyID == companyID).ToList();
            SelectList list = new SelectList(getcategories, "categoryID", "name");
            ViewBag.category = list;
            return View(db.Items.Single(x=>x.itemID == id));
        }


        [HttpPost]
        [CustomAuthorize(Roles = "Super Administrator,Administrator,Manager,Employee")]
        public ActionResult Edit(int id, Item i)
        {
            int companyID = CompanyID();
            var getcategories = db.ItemsCategories.Where(x => x.companyID == companyID).ToList();
            SelectList list = new SelectList(getcategories, "categoryID", "name");
            ViewBag.category = list;
            
            if (i.salePrice < i.purchasePrice)
            {
                ModelState.AddModelError("salePrice", "Sale price cannot be smaller than purchase price");
            }
            else
            {
                var item = db.Items.Find(id);
                item.categoryID = i.categoryID;
                item.name = i.name;
                item.description = i.description;
                item.salePrice = i.salePrice;
                item.purchasePrice = i.purchasePrice;
                db.Entry(item).State = EntityState.Modified;
                var check = db.SaveChanges();
                if (check != 0)
                {
                    
                    return RedirectToAction("", "items");
                }

            }

            

            return View();
        }

        [HttpPost]
        [CustomAuthorize(Roles = "Super Administrator,Administrator,Manager,Employee")]

        public ActionResult Delete(int id)
        {
            Item i = db.Items.Find(id);
            db.Items.Remove(i);
            var check = db.SaveChanges();
            if (check != 0 )
            {
                return RedirectToAction("", "items");
            }

            return View();
        }
    }
}