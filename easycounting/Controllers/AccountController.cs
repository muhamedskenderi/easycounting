using easycounting.Models;
using easycounting.ViewModels;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using System.Web.Security;
using static easycounting.ViewModels.AccountViewModel;

namespace easycounting.Controllers
{
    public class AccountController : Controller
    {
        DbEnt db = new DbEnt();
        // GET: Account
        [CustomAuthorize(Roles = "Super Administrator")]
        [HttpGet]
        public ActionResult Index()
        {
            return View();
        }

     

        public bool EmailExists(string email)
        {
            var c = db.Companies.Where(x => x.email == email).SingleOrDefault();

            bool check = c != null;
            return check;
        }

        public bool CompanyExists(string companyName, string taxID)
        {
            var c = db.Companies.Where(x => x.name == companyName || x.taxNo == taxID).SingleOrDefault();

            bool check = c != null;
            return check;
        }

        [HttpPost]
        public ActionResult Auth(int id)
        {
            if (HttpContext.Request.Cookies["companyID"] != null)
            {
                HttpCookie cookie = new HttpCookie("companyID", id.ToString());
                cookie.Expires = DateTime.Now.AddDays(1);
                Response.Cookies.Add(cookie);
            }
            else
            {
                Response.Cookies["companyID"].Value = id.ToString();


            }

            return RedirectToAction("","home");
        }

        public int CompanyID()
        {
            string username = User.Identity.Name;

            var comp = db.UsersInCompanies.Where(x => x.User.username == username).SingleOrDefault();
            int id = Convert.ToInt32(comp.companyID);
            return id;
        }
        [AllowAnonymous]
        public ActionResult Verify(string ActivationCode)
        {


            string message = "";

            var row = db.Users.Where(x => x.ActivationCode == ActivationCode).SingleOrDefault();
            var id = row.userID;
            ViewBag.User = row.username;
            if (!User.Identity.IsAuthenticated)
            {



                if (row != null)
                {
                    if (row.IsVerified)
                    {
                        return RedirectToAction("", "home");
                    }
                    else
                    {
                        var rowToUpdate = db.Users.Find(id);
                        /* rowToUpdate.IsVerified = true;
                     db.Entry(rowToUpdate).State = EntityState.Modified;*/


                        var updatedUser = new User() { userID = id, IsVerified = true };
                        using (var db = new DbEnt())
                        {
                            db.Configuration.ValidateOnSaveEnabled = false;
                            db.Users.Attach(updatedUser);
                            db.Entry(updatedUser).Property(x => x.IsVerified).IsModified = true;
                            var check = db.SaveChanges();
                            if (check != 0)
                            {
                                message = "Not Verified";
                            }
                        }



                    }

                }
            }
            else
            {
                return RedirectToAction("", "home");
            }

            ViewBag.Status = message;
            return View();
        }


        [HttpGet]
        [AllowAnonymous]

        public ActionResult ForgotPassword()
        { 
            if (User.Identity.IsAuthenticated)
            {
                return RedirectToAction("", "home");
            }
            return View();
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public ActionResult ForgotPassword(ForgotPasswordViewModel model)
        {
            if (!User.Identity.IsAuthenticated)
            {
                var user = db.Users.Where(x => x.email == model.Email).SingleOrDefault();
                if (user == null)
                {
                    ModelState.AddModelError("Email", "This email is not in our system");
                    return View();
                }
                bool isConfirmed = user.IsVerified;

                if (!isConfirmed)
                {
                    ModelState.AddModelError("Email", "Email address not confirmed yet");

                    return View();
                }


                var code = db.Users.Where(x => x.email == model.Email).SingleOrDefault();
              

                var callbackUrl = "localhost/easycounting/account/resetpassword?code=" + code.ActivationCode;


                System.Net.Mail.MailMessage m = new System.Net.Mail.MailMessage(
                new System.Net.Mail.MailAddress("easycountingapp@gmail.com", "Reset Password - easycounting"),
                new System.Net.Mail.MailAddress(user.email));
                m.Subject = "Email confirmation";
                m.Body = string.Format("Please reset your password by clicking here: <a href=\"" + callbackUrl + "\">link</a>");
                m.IsBodyHtml = true;
                System.Net.Mail.SmtpClient smtp = new System.Net.Mail.SmtpClient("smtp.gmail.com");
                smtp.Credentials = new System.Net.NetworkCredential("easycountingapp@gmail.com", "muhamed98");
                smtp.EnableSsl = true;
                smtp.Send(m);
                return View("ForgotPasswordConfirmation");


            }
            else
            {
                return RedirectToAction("", "home");
            }
            
               

        }

        [HttpGet]
        public ActionResult AddCompany()
        {
          
            return View();
        }

        [HttpPost]
        public ActionResult AddCompany(Company model)
        {
            if (EmailExists(model.email) != true)
            {
                if (CompanyExists(model.name, model.taxNo) != true)
                {
                    string username = User.Identity.Name;

                    var comp = db.Users.Where(x => x.username == username).SingleOrDefault();
                    Company c = new Company()
                    {
                        name = model.name,
                        taxNo = model.taxNo,
                        phone = model.phone,
                        address = model.address,
                        email = model.phone,
                        dateCreated = DateTime.Now,
                        hidden = false,
                        currency = "MKD"
                    };
                    var compRow = db.Companies.Add(c);
                    UsersInCompany uc = new UsersInCompany { 
                    userID = comp.userID,
                    companyID = compRow.companyID
                    };
                    db.UsersInCompanies.Add(uc);
                    var check = db.SaveChanges();
                    if (check != 0)
                    {
                        return RedirectToAction("", "account");
                    }
                }
                {
                    ModelState.AddModelError("company", "A company with this Name or Tax Number already exists!");
                }
            }
            else
            {
                ModelState.AddModelError("company", "A company with this email already exists!");

            }

            return View();
        }

        [HttpPost]
        public ActionResult DeleteCompany(int id)
        {
            Response.Cookies["companyID"].Value = id.ToString();
            return RedirectToAction("deleteaccount", "settings");

        }

        // GET: /Account/ResetPassword
        [AllowAnonymous]
        [HttpGet]
        public ActionResult ResetPassword(string code)
        {
            if (code == null)
            {
                return RedirectToAction("notfound", "error");
            }
            else
            {
                var r = db.Users.Where(x => x.ActivationCode == code).SingleOrDefault();
                if (r == null)
                {

                    return RedirectToAction("notfound", "error");

                }
                Session["id"] = r.userID.ToString();
                return View();
            }
        }
        [AllowAnonymous]
        [HttpPost]
        public ActionResult ResetPassword(ResetPasswordViewModel model)
        {
            string idCheck = Session["id"].ToString();

            if (idCheck != null)
            {
                int id = Convert.ToInt32(Session["id"].ToString());
                var row = db.Users.Where(x => x.userID == id).SingleOrDefault();
                string currentPassword = row.password;
                Crypto enc = new Crypto();
                string newPassword = enc.Hash(model.Password);
                if (model.Password != model.ConfirmPassword)
                {
                    Session["id"] = idCheck;
                    ModelState.AddModelError("Password", "The password and confirmation password do not match. ");

                }
                else
                {
                    if (newPassword == currentPassword)
                    {
                        TempData["id"] = idCheck;
                        ModelState.AddModelError("Password", "You can not use a password you have used before.");
                    }
                    else
                    {
                        var updatedUser = new User() { userID = id, password = newPassword };

                        using (var db = new DbEnt())
                        {
                            db.Configuration.ValidateOnSaveEnabled = false;
                            db.Users.Attach(updatedUser);
                            db.Entry(updatedUser).Property(x => x.password).IsModified = true;
                            var check = db.SaveChanges();
                            if (check != 0)
                            {
                                return View("ResetPasswordConfirmation");
                            }
                        }
                    }
                    

                }
            }
            else
            {
                return RedirectToAction("notfound", "error");
            }
            
           
            return View();
        }




        public ActionResult Created(string email)
        {
            ViewBag.email = email;

            return View();
        }


        public ActionResult Logout()
        {
            if (Request.Cookies["companyID"] != null)
            {
                var c = new HttpCookie("MyCookie");
                c.Expires = DateTime.Now.AddDays(-1);
                Response.Cookies.Add(c);
            }
            FormsAuthentication.SignOut();
            return RedirectToAction("", "login");
        }
    }
}