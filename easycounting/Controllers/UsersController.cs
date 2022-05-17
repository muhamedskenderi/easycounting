using easycounting.Models;
using PagedList;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Security;

namespace easycounting.Controllers
{
    public class UsersController : Controller
    {
        public int CompanyID()
        {
            var cookie = HttpUtility.UrlDecode(Request.Cookies["companyID"].Value.ToString());

            int companyID = Convert.ToInt32(cookie);
            return companyID;
        }

        DbEnt db = new DbEnt();

        public bool UsernameExists(string username)
        {
            var c = db.Users.Where(x => x.username == username).SingleOrDefault();

            bool check = c != null;
            return check;
        }

        public bool EmailExists(string email)
        {
            var c = db.Users.Where(x => x.email == email).SingleOrDefault();

            bool check = c != null;
            return check;
        }
        // GET: Employees
        public ActionResult Index(int? page)
        {
            int id = CompanyID();

            var items = db.UsersInCompanies.Where(x => x.companyID == id).ToList().OrderByDescending(x => x.userID);
            ViewBag.OnePageOfUsers = items;

            var role = db.Users.Where(x => x.userID == id).SingleOrDefault();

            ViewBag.role = role.roleID;
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
        public ActionResult Create(User model,FormCollection f)
        {
            int cmopanyID = CompanyID();
            Crypto crypto = new Crypto();
            Guid code = Guid.NewGuid();
            if (UsernameExists(model.username))
            {
                ModelState.AddModelError("username", "This username is already taken!");
            }
             if (EmailExists(model.email))
            {
                ModelState.AddModelError("email", "This e-mail address is already taken!");

            }
            if (!UsernameExists(model.username) && !EmailExists(model.email))
            {
                if (model.password != model.confirmPassword)
                {
                    ModelState.AddModelError("confirmPassword", "Password and confirm password does not match! ");

                }
                else
                {
                    User user = new User
                    {
                        ActivationCode = code.ToString(),
                        IsVerified = true,
                        roleID = Convert.ToInt32(f["role"].ToString()),
                        username = model.username,
                        password = crypto.Hash(model.password),
                        email = model.email
                    };
                    var row = db.Users.Add(user);
                    UsersInCompany uc = new UsersInCompany
                    {
                        companyID = cmopanyID,
                        userID = row.userID
                    };
                    db.UsersInCompanies.Add(uc);

                    var check = db.SaveChanges();
                    if (check != 0)
                    {
                        return RedirectToAction("", "users");
                    }
                }
               
            }
            
            return View();
        }

        [HttpGet]
        public ActionResult Edit(int id)
        {
            int companyID = CompanyID();

            var edit = db.Users.Single(x => x.userID == id);
            var company = db.UsersInCompanies.Where(x => x.companyID == companyID && x.userID == id).FirstOrDefault();
            if (company == null)
            {
                return RedirectToAction("notfound", "error");

            }
            else
            {
            

             
                return View(edit);
            }

        }

        [HttpPost]
        public ActionResult Edit(int id, User model, FormCollection f)
        {
            Crypto crypto = new Crypto();
            if (UsernameExists(model.username))
            {
                ModelState.AddModelError("username", "This username is already taken!");
            }
            if (EmailExists(model.email))
            {
                ModelState.AddModelError("email", "This e-mail address is already taken!");

            }
            if (!UsernameExists(model.username) && !EmailExists(model.email))
            {
                var row = db.Users.Find(id);

                row.username = model.username;
                row.email = model.email;
                row.roleID = Convert.ToInt32(f["role"].ToString());
                
            
            }

            var check = db.SaveChanges();
            if (check != 0)
            {
                return RedirectToAction("", "users");
            }

            return View();
        }


        [HttpPost]
        public ActionResult Delete(int id)
        {
            var row = db.Users.Where(x => x.userID == id).SingleOrDefault();
            db.Users.Remove(row);
            var check = db.SaveChanges();
            if (check != 0)
            {
                return RedirectToAction("", "users");
            }
            return View();
        }


        [HttpPost]
        public ActionResult ChangePassword(int id,FormCollection f)
        {
            Crypto c = new Crypto();
            var row = db.Users.Where(x => x.userID == id).SingleOrDefault();
            row.password = c.Hash(f["pass"].ToString());
            var check = db.SaveChanges();
            if (check != 0)
            {
                return RedirectToAction("", "users",new { message = "Password Successfully Updated for " + row.username });
            }
            return View();
        }
    }
}