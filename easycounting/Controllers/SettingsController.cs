using easycounting.Models;
using easycounting.ViewModels;
using Microsoft.AspNet.Identity;
using Microsoft.AspNetCore.Identity;
using PagedList;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using System.Web.Security;

namespace easycounting.Controllers
{
    public class SettingsController : Controller
    {
        DbEnt db = new DbEnt();
        public int CompanyID()
        {
            var cookie = HttpUtility.UrlDecode(Request.Cookies["companyID"].Value.ToString());

            int companyID = Convert.ToInt32(cookie);
            return companyID;
        }



        public bool EmailExists(string email)
        {
            var c = db.Users.Where(x => x.email == email).SingleOrDefault();

            bool check = c != null;
            return check;
        }

        public bool CompanyExists(string companyName)
        {
            var c = db.Companies.Where(x => x.name == companyName).SingleOrDefault();

            bool check = c != null;
            return check;
        }

        public bool TaxNoExists(string taxID)
        {
            var c = db.Companies.Where(x =>  x.taxNo == taxID).SingleOrDefault();

            bool check = c != null;
            return check;
        }

        public bool UsernameExists(string username)
        {
            var c = db.Users.Where(x => x.username == username).SingleOrDefault();

            bool check = c != null;
            return check;
        }

        // GET: Settings
        [CustomAuthorize(Roles = "Super Administrator")]
        [HttpGet]
        public ActionResult Index()
        {
            ViewBag.Message = "";
            ViewBag.Status = false;
            int id = CompanyID();
            return View(db.Companies.Single(x=>x.companyID==id));
        }

        [CustomAuthorize(Roles = "Super Administrator")]
        [HttpPost]
        public ActionResult Index(Company company,FormCollection f)
        {
            bool usernameChanged;
            usernameChanged  =  false;
            int id = CompanyID();
            var comp = db.Companies.Find(id);

            if (f["name"].ToString() != comp.name )
            {
             
                   
                

                    if (CompanyExists(f["name"].ToString()))
                {
                    ModelState.AddModelError("name", "Company name already exists!");
                }
               
                else
                {
                    comp.name = f["name"].ToString();
           
                }
            }


            if (f["taxNo"].ToString() != comp.taxNo)
            {
               

                if (TaxNoExists(f["taxNo"].ToString()))
                {
                    ModelState.AddModelError("name", "Tax No belongs to another existing company!");
                }

                else 
                    comp.taxNo = f["taxNo"].ToString();
            }

            comp.phone = company.phone;
            comp.address = company.address;



            if (f["username"].ToString() != User.Identity.Name.ToString()) // if username is changed
            {
                if (UsernameExists(f["username"].ToString()))
                    ModelState.AddModelError("username", "This username is already taken");
                else
                {
                string username = User.Identity.Name;
                var userRow = db.Users.Where(x => x.username == username).SingleOrDefault();
                    int userid = userRow.userID;
                    var user = db.Users.Find(userid);
                    user.username = f["username"].ToString();
                    db.Entry(user).State = EntityState.Modified;
                    usernameChanged = true;

                }

            }
            db.Entry(comp).State = EntityState.Modified;

            var check = db.SaveChanges();
            if (check != 0)
            {
                ViewBag.Message = "Profile details updated successfully";
                ViewBag.Status = true;
                if (usernameChanged)
                {
                    FormsAuthentication.SignOut();
                    FormsAuthentication.SetAuthCookie(f["username"].ToString(), false);
                }
            }
            else
            {
                ViewBag.Message = "Something went wrong. Please try again!";
                ViewBag.Status = false;

            }
            return View();
        }

        [CustomAuthorize(Roles = "Super Administrator")]
        public ActionResult ItemCategories(int? page)
        {
            int id = CompanyID();
            List<ItemsCategory> items = db.ItemsCategories.Where(x => x.companyID == id).ToList();
            ViewBag.OnePageOfUsers = items;


            if (items != null)
                return View(items.ToPagedList(page ?? 1, 5));
            else
                return View();
        }
        [CustomAuthorize(Roles = "Super Administrator")]
        public ActionResult Taxes(int? page)
        {
            int id = CompanyID();
            List<Tax> items = db.Taxes.Where(x => x.companyID == id).ToList();
            ViewBag.OnePageOfUsers = items;


            if (items != null)
                return View(items.ToPagedList(page ?? 1, 5));
            else
                return View();
        }


        [CustomAuthorize(Roles = "Super Administrator")]
        public ActionResult Expenses(int? page)
        {
            int id = CompanyID();
            List<ExpenseCategory> expense = db.ExpenseCategories.Where(x => x.companyID == id).ToList();
            ViewBag.OnePageOfUsers = expense;


            if (expense != null)
                return View(expense.ToPagedList(page ?? 1, 5));
            else
                return View();
        }

        [CustomAuthorize(Roles = "Super Administrator")]
        public ActionResult Sales(int? page)
        {
            int id = CompanyID();
            List<SaleCategory> saleCategories = db.SaleCategories.Where(x => x.companyID == id).ToList();
            ViewBag.OnePageOfUsers = saleCategories;


            if (saleCategories != null)
                return View(saleCategories.ToPagedList(page ?? 1, 5));
            else
                return View();
        }


        [CustomAuthorize(Roles = "Super Administrator")]
        [HttpGet]
        public ActionResult ChangePassword()
        {
            ViewBag.Message = "";
            ViewBag.Status = false;
            return View();
        }

        [HttpPost]
        public ActionResult ChangePassword(ChangePasswordViewModel model)
        {
            string message = "";
            bool status = false;
            Crypto crypto = new Crypto();
            int id = CompanyID();
            var user = db.UsersInCompanies.Where(x => x.companyID == id).SingleOrDefault();
            string pass = user.User.password.ToString();

            if (pass == crypto.Hash(model.OldPassword))
            {
                if (crypto.Hash(model.NewPassword) == crypto.Hash(model.ConfirmPassword))
                {
                    if (crypto.Hash(model.NewPassword) == pass)
                    {
                        ModelState.AddModelError("confirmPassword", "New Password can not be same as Current Password");

                    }
                    var row = db.Users.Find(user.userID);
                    row.password = crypto.Hash(model.NewPassword);
                    var check = db.SaveChanges();
                    if (check != 0)
                    {
                        status = true;
                        message = "Password changed succesfully";

                    }                
                }

                else
                {
                    ModelState.AddModelError("confirmPassword", "New Password and Confirm Password does not match");

                }

            }
            else
            {
                ModelState.AddModelError("oldPassword", "Wrong current password");
            }
            ViewBag.Status = status;
            ViewBag.Message = message;
            return View();
        }


        [CustomAuthorize(Roles = "Super Administrator")]
        [HttpGet]
        public ActionResult DeleteAccount()
        {
            return View();
        }


        [CustomAuthorize(Roles = "Super Administrator")]
        [HttpPost]
        public ActionResult DeactivateAccount()
        {
            int id = CompanyID();
            var row = db.Companies.Find(id);
            row.hidden = true;
            var check = db.SaveChanges();
            if (check != 0)
            {
                return RedirectToAction("", "account");
            }
            return View();
        }
    }
}