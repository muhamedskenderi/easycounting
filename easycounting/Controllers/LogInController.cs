using easycounting.Models;
using easycounting.ViewModels;
using System;
using System.Collections.Generic;
using System.Data.Entity.Validation;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;
using System.Web.Security;

namespace easycounting.Controllers
{
    public class LogInController : Controller
    {
        // GET: LogIn
        [HttpGet]
        [AllowAnonymous]

        public ActionResult Index(string ReturnUrl)
        {
            ViewBag.ReturnUrl = ReturnUrl;
            return View();
        }

        [HttpPost]
        public ActionResult Index(LogInViewModel login, string ReturnUrl)
        {
            using (DbEnt db = new DbEnt())
            {
                Crypto c = new Crypto();
                string password = c.Hash(login.password);
              
                    var row = db.UsersInCompanies.Where(x => x.User.username == login.username && x.User.password == password).FirstOrDefault();

                    if (row != null)
                    {
                    if (row.User.IsVerified == false)
                    {
                        ModelState.AddModelError("username", "Your email address is still not verified. Please verify your email to get access in the dashboard!");

                    }
                    else if (row.Company.hidden == true)
                    {
                        ModelState.AddModelError("username", "Your account is deactivated. Please contact the administrator to check if you can still recover your account!");

                    }
                    else
                    {
                        FormsAuthentication.SetAuthCookie(row.User.username, false);
                        if (row.User.Role.role1 == "Administrator" || row.User.Role.role1 == "Super Administrator")
                        {
                            return RedirectToAction("", "account");
                        }
                        else
                        {
                            return RedirectToAction("", "home");

                        }
                    } 

                    



                }
                    else
                    {
                        ModelState.AddModelError("username", "Username or Password do not match. Please try again!");

                    }
              

            }
            return View();
        }

        private ActionResult RedirectToLocal(string returnUrl)
        {
            if (Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }
            else
            {
                return RedirectToAction("", "home");
            }
        }
    }
}