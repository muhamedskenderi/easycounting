using easycounting.Models;
using PagedList;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace easycounting.Controllers
{
    public class PayrollController : Controller
    {
        DbEnt db = new DbEnt();
        public int CompanyID()
        {
            var cookie = HttpUtility.UrlDecode(Request.Cookies["companyID"].Value.ToString());

            int companyID = Convert.ToInt32(cookie);
            return companyID;
        }
        [CustomAuthorize(Roles = "Super Administrator,Administrator,Manager")]

        public ActionResult Index(int?page)
        {
            int id = CompanyID();

            var items = db.Transactions.Where(x => x.Payroll.companyID == id).ToList().OrderByDescending(x => x.date);


            ViewBag.OnePageOfUsers = items;

            var role = db.Users.Where(x => x.userID == id).SingleOrDefault();

            ViewBag.role = role.roleID;
            if (items != null)
                return View(items.ToPagedList(page ?? 1, 10));
            else
                return View();
        }

        [HttpGet]
        [CustomAuthorize(Roles = "Super Administrator,Administrator,Manager")]

        public ActionResult Create()
        {
            int companyID = CompanyID();
            Crypto decrypt = new Crypto();
            SelectList getaccounts = new SelectList((from x in db.Accounts.Where(x => x.companyID == companyID).ToList()
                                                     select new
                                                     {
                                                         ID = x.accountID,
                                                         Data = x.bankName + " (" + String.Format("************{0}", decrypt.Base64Decode(x.cardNo).Trim().Substring(12, 4)) + " )"
                                                     }), "ID", "Data");
            ViewBag.accounts = getaccounts;

            SelectList list4 = new SelectList((from x in db.EmployeesInCompanies.Where(x => x.companyID == companyID).ToList()
                                               select new
                                               {
                                                   ID = x.employeeID,
                                                   Data = x.Employee.name + " ( Bruto:" + decrypt.Base64Decode(x.Employee.bruto) + " MKD | Bonus:"+ decrypt.Base64Decode(x.Employee.bonus) +  " MKD)"
                                               }), "ID", "Data");
            ViewBag.employees = list4;

            return View();
        }



        [HttpPost]
        [CustomAuthorize(Roles = "Super Administrator,Administrator,Manager")]

        public ActionResult Create(Payroll p,FormCollection f)
        {
            int companyID = CompanyID();

            Payroll payroll = new Payroll {
                employeeID = p.employeeID,
                description = p.description,
                companyID = companyID,
                month = f["monthSalary"].ToString() + " " + DateTime.Now.Year.ToString()
            };
            var row = db.Payrolls.Add(payroll);
            var user = db.UsersInCompanies.Where(x => x.companyID == companyID).SingleOrDefault();

            Transaction transaction = new Transaction
            {
                transactionID = DateTime.Now.ToString("ssmmddHHMMyyyyff"),
                accountID = Convert.ToInt32(f["accountID"].ToString()),
                userID = user.User.userID,
                date = DateTime.Now,
                status = "Paid",
                payrollID = row.payrollID,
                paymentID = null,
                billID = null
            };
            db.Transactions.Add(transaction);
            var checker = db.SaveChanges();

            if (checker != 0)
            {
                return RedirectToAction("payslip", "payroll", new { id = row.payrollID });
            }

          

            return View();
        }

        [CustomAuthorize(Roles = "Super Administrator, Administrator, Manager")]
        public ActionResult Payslip(string id)
        {
            
            return View(db.Transactions.Single(x=>x.transactionID == id));
        }
    }
}