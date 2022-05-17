using System;
using System.Collections.Generic;
using System.Data.Entity.Validation;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Web;
using System.Web.Mvc;
using easycounting.Models;
using easycounting.ViewModels;

namespace easycounting.Controllers
{
    public class RegisterController : Controller
    {
        DbEnt db = new DbEnt();
        // GET: Register
        [HttpGet]
        [AllowAnonymous]
        public ActionResult Index()
        {
            
            return View();
        }


        
        public bool EmailExists(string email)
        {
            var c = db.Users.Where(x => x.email == email).SingleOrDefault();

            bool check = c != null;
            return check;
        }

        public bool CompanyExists(string companyName, string taxID)
        {
            var c = db.Companies.Where(x => x.name == companyName || x.taxNo == taxID).SingleOrDefault();

            bool check = c != null;
            return check;
        }

        public bool UsernameExists(string username)
        {
            var c = db.Users.Where(x => x.username == username).SingleOrDefault();

            bool check = c != null;
            return check;
        }

        [HttpPost]
        public ActionResult Index(RegisterViewModel u, FormCollection f)
        {
           

            
                    Crypto crypto = new Crypto();
                    string compName = u.companyName.ToLower();
                    string taxNo = f["taxNo"].ToString();
                    var email = EmailExists(u.email);
                    var company = CompanyExists(u.companyName,u.taxNo);
                      var username = UsernameExists(u.username);

                if (ModelState.IsValid)
                { 
                        if (email == false  && username == false && company == false)
                        {
                                Company c = new Company();
                                c.email = u.email;
                                c.name = u.companyName;
                                c.taxNo = u.taxNo;
                                c.phone = u.phone;
                                c.address = u.phone;
                                c.dateCreated = DateTime.Now;
                                c.currency = "MKD";
                                c.hidden = false;
                                var companyIDrow = db.Companies.Add(c);

                        


                                Guid code = Guid.NewGuid();

                                if (u.password != u.confirmedPassword)
                                {
                                    ModelState.AddModelError("password", "Confirm password doesn't match, Try again");
                                 return View();
                                }
                                User user = new User();
                                user.email = u.email;
                                user.username = u.username;
                                user.IsVerified = false;
                                user.ActivationCode = code.ToString();
                                user.roleID = 1;
                                user.password = crypto.Hash(u.password);
                                var g = db.Users.Add(user);

                                UsersInCompany uc = new UsersInCompany { 
                                userID = g.userID,
                                companyID = companyIDrow.companyID
                                };
                                db.UsersInCompanies.Add(uc);

                                var check = db.SaveChanges();
                                if (check != 0)
                                {
                                    SendVerificationLinkEmail(g.email, code.ToString());

                                    return RedirectToAction("created", "account", new { email = g.email });
                                }
                                else
                                {
                                    return View(u);
                                }
                        }

                
                        if (email == true)
                            {
                                ModelState.AddModelError("email", "Email address already exists. Please enter a different email address.");

                            return View(u);
                        }

                    if (username == true)
                        {
                            ModelState.AddModelError("username", "Username already exists. Please enter a different username.");
                        return View(u);   
                    }
                    

                    if (company == true)
                        {
                            ModelState.AddModelError("companyName", "The company name or tax number already exists. If you are already registred please log in.");
                        return View(u);

                    }
                

            }

           
            return View(u);
        }

        public void SendVerificationLinkEmail(string email, string activationCode)
        {
            var verifyUrl = "/easycounting/account/verify?ActivationCode=" + activationCode;
            var link = Request.Url.AbsoluteUri.Replace(Request.Url.PathAndQuery, verifyUrl);

            #region
            var fromEmail = new MailAddress("yourEmail@example.com", "easycounting");
            var toEmail = new MailAddress(email);
            var fromEmailPassword = "yourPassword";
            #endregion

            string subject = "Your account is successfully created!";

            string body = "<br/><br/>We are excited to tell you that your easycounting account is" +
                " successfully created. Please click on the below link to verify your account" +
                " <br/><br/><a class='btn btn-primary' href='" + link + "'>" + link + "</a> ";

            var smtp = new SmtpClient
            {
                Host = "smtp.gmail.com",
                Port = 587,
                EnableSsl = true,
                DeliveryMethod = SmtpDeliveryMethod.Network,
                UseDefaultCredentials = true,
                Credentials = new NetworkCredential(fromEmail.Address, fromEmailPassword)
            };

            using (var message = new MailMessage(fromEmail, toEmail)
            {
                Subject = subject,
                Body = body,
                IsBodyHtml = true
            })
                smtp.Send(message);
        }
    }
}