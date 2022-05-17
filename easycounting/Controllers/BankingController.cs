using easycounting.Models;
using PagedList;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace easycounting.Controllers
{
    public class BankingController : Controller
    {
        // GET: Banking
        DbEnt db = new DbEnt();
        [CustomAuthorize(Roles = "Super Administrator")]

        public ActionResult Index(int? page)
        {
            int id= CompanyID();
            ViewBag.companyid = id;

            List<Transaction> transactions = db.Transactions.Where(x => x.Account.companyID == id).OrderByDescending(x=>x.date).ToList();
            ViewBag.OnePageOfUsers = transactions;


            if (transactions != null)
                return View(transactions.ToPagedList(page ?? 1, 10));
            else
                return View();

        }
        [CustomAuthorize(Roles = "Super Administrator")]

        public ActionResult Accounts()
        {
            return View();
        }

        [HttpGet]
        [CustomAuthorize(Roles = "Super Administrator")]

        public ActionResult AddAccount()
        {
            return View();
        
        }
        public int CompanyID()
        {
            var cookie = HttpUtility.UrlDecode(Request.Cookies["companyID"].Value.ToString());

            int companyID = Convert.ToInt32(cookie);
            return companyID;
        }


        [HttpPost]
        [CustomAuthorize(Roles = "Super Administrator")]

        public ActionResult AddAccount(FormCollection f)
        {
            int companyID = CompanyID();
            Crypto crypto = new Crypto();
            Account account = new Account();
            account.bankName = f["bankName"].ToString();
            account.cardNo = crypto.Base64Encode(f["cardNo"].ToString());
            account.nameOnCard = f["nameOnCard"].ToString();
            account.cvc = crypto.Base64Encode(f["cvc"].ToString());
            account.companyID = companyID;
            db.Accounts.Add(account);
            if (Convert.ToInt32(f["year"].ToString()) > Convert.ToInt32(DateTime.Now.Year.ToString()))
            {
                string expr = f["month"].ToString() + "/" + f["year"].ToString();

                account.exprDate = expr;
                var check = db.SaveChanges();
                if (check != 0)
                {
                    return RedirectToAction("", "banking");
                }
            }
           
           
            return View();
        }


        [HttpPost]
        [CustomAuthorize(Roles = "Super Administrator")]

        public ActionResult EditAccount(int id,FormCollection f )
        {
            ViewBag.status = false;
            var row = db.Accounts.Find(id);
            Crypto crypto = new Crypto();
            row.bankName = f["bankName"].ToString();
            row.cardNo = crypto.Base64Encode(f["cardNo"].ToString());
            row.nameOnCard = f["nameOnCard"].ToString();
            row.cvc = crypto.Base64Encode(f["cvc"].ToString());
            

                row.exprDate = f["exprDate"].ToString();
                var check = db.SaveChanges();
                if (check != 0)
                {
                    return RedirectToAction("", "banking");
                }
            
            

            return View();
        }


        [HttpPost]
        [CustomAuthorize(Roles = "Super Administrator")]

        public ActionResult DeleteAccount(int id)
        {
           
            
            var row = db.Accounts.Find(id);
            row.hidden = true;
            var check = db.SaveChanges();
            if (check != 0)
            {
                return RedirectToAction("", "banking");
            }



            return View();
        }


        [CustomAuthorize(Roles = "Super Administrator")]

        public ActionResult Transactions(string id)
        {
            return View(db.Transactions.Single(x=>x.transactionID == id));
        }
        [CustomAuthorize(Roles = "Super Administrator")]

        public ActionResult Reconciliations()
        {
            return View();
        }
    }
}