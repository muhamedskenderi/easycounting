using easycounting.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Script.Serialization;
using System.Web.Security;

namespace easycounting.Controllers
{
    public class HomeController : Controller
    {
        DbEnt db = new DbEnt();
        [CustomAuthorize(Roles = "Super Administrator, Administrator,Manager,Employee")]
        public ActionResult Index()
        {
          
            Calculations cal = new Calculations();
            int id = CompanyID();
            
            decimal paidRevenues = db.Revenues.Where(x => x.Customer.companyID == id && x.status == "Paid").Count();
            decimal unpaidRevenues = db.Revenues.Where(x => x.Customer.companyID == id && x.status == "Unpaid").Count();
            decimal overdueRevenues = db.Revenues.Where(x => x.Customer.companyID == id && x.status == "Overdue").Count();
            decimal s = paidRevenues + unpaidRevenues + overdueRevenues;

            decimal paidInvoices = db.Invoices.Where(x => x.Customer.companyID == id && x.status == "Paid").Count();
            decimal unpaidInvoices = db.Invoices.Where(x => x.Customer.companyID == id && x.status == "Unpaid").Count();
            decimal overdueInvoices = db.Invoices.Where(x => x.Customer.companyID == id && x.status == "Overdue").Count();
            decimal S = paidInvoices + unpaidInvoices + overdueInvoices;

            decimal paidPayments = db.Payments.Where(x => x.Vendor.companyID == id && x.status == "Paid").Count();
            decimal unpaidPayments = db.Payments.Where(x => x.Vendor.companyID == id && x.status == "Unpaid").Count();
            decimal overduePayments = db.Payments.Where(x => x.Vendor.companyID == id && x.status == "Overdue").Count();
            decimal sh = paidPayments + unpaidPayments + overduePayments;

            decimal paidBills = db.Bills.Where(x => x.Vendor.companyID == id && x.status == "Paid").Count();
            decimal unpaidBills = db.Bills.Where(x => x.Vendor.companyID == id && x.status == "Unpaid").Count();
            decimal overdueBills = db.Bills.Where(x => x.Vendor.companyID == id && x.status == "Overdue").Count();
            decimal Sh = paidBills + unpaidBills + overdueBills;

            ViewBag.role = GetRole();
            if (GetRole() == "Employee")
            {

            }
            else
            {
                if (sh != 0)
                {
                    ViewBag.paidPayments = paidPayments / sh * 100;
                    ViewBag.overduePayments = (overduePayments / sh) * 100;
                    ViewBag.unpaidPayments = (unpaidPayments / sh) * 100;

                    ViewBag.paidBills = (paidBills / Sh) * 100;
                    ViewBag.overdueBills = (overdueBills / Sh) * 100;
                    ViewBag.unpaidBills = (unpaidBills / Sh) * 100;

                }
                else
                {
                    ViewBag.paidPayments = 0;
                    ViewBag.overduePayments = 0;
                    ViewBag.unpaidPayments = 0;

                    ViewBag.paidBills = 0;
                    ViewBag.overdueBills = 0;
                    ViewBag.unpaidBills = 0;

                }
                if (Sh != 0)
                {
                    ViewBag.paidRevenues = paidRevenues / s * 100;
                    ViewBag.overdueRevenues = (overdueRevenues / s) * 100;
                    ViewBag.unpaidRevenues = (unpaidRevenues / s) * 100;

                    ViewBag.paidInvoices = (paidInvoices / S) * 100;
                    ViewBag.overdueInvoices = (overdueInvoices / S) * 100;
                    ViewBag.unpaidInvoices = (unpaidInvoices / S) * 100;
                }
                else
                {
                    ViewBag.paidRevenues = 0;
                    ViewBag.overdueRevenues = 0;
                    ViewBag.unpaidRevenues = 0;

                    ViewBag.paidInvoices = 0;
                    ViewBag.overdueInvoices = 0;
                    ViewBag.unpaidInvoices = 0;
                }
                ViewBag.expensesDue = cal.ExpensesDue(id);
                ViewBag.incomesDue = cal.IncomesDue(id);
                ViewBag.totalCustomers = cal.CountCustomers(id);
                ViewBag.totalVendors = cal.CountVendors(id);

                ViewBag.expenses = cal.Expenses(id);
                ViewBag.earnings = cal.Earnings(id);
                ViewBag.balance = cal.GetProfit(cal.Earnings(id), cal.Expenses(id));
            }
            
           

           

           
            ViewBag.id = id;
            return View();
        }


        public string GetRole()
        {
            string username = User.Identity.Name;
            string[] role = Roles.GetRolesForUser(username);
            return role[0];
        }
        public int CompanyID()
        {
            var cookie = HttpUtility.UrlDecode(Request.Cookies["companyID"].Value.ToString());

            int companyID = Convert.ToInt32(cookie);
            return companyID;
        }

        public JsonResult InvoiceAnalytics()
        {
            Calculations cal = new Calculations();
            int companyid = CompanyID();

            
               
              
            return Json(cal.InvoicesStats(companyid), JsonRequestBehavior.AllowGet);
        }

        public JsonResult BalanceAnalytics()
        {
            Calculations cal = new Calculations();
            double? balance = 0; double? incomes = 0; double? expenses = 0;
            int companyid = CompanyID();
            List<string[]> items = new List<string[]>();

            for (int i = 1; i <= 12; i++)
            {
                expenses = cal.GetExpenses(companyid, i);
                incomes = cal.GetIncomes(companyid, i);
                balance = cal.GetProfit(incomes, expenses);

                string[] elem = { incomes.ToString(), expenses.ToString(), balance.ToString() };
                items.Add(elem);

            }


            return Json(items, JsonRequestBehavior.AllowGet);
        }



    }
}