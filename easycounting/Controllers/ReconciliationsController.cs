using easycounting.Models;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace easycounting.Controllers
{
    public class ReconciliationsController : Controller
    {
        public int CompanyID()
        {
            var cookie = HttpUtility.UrlDecode(Request.Cookies["companyID"].Value.ToString());

            int companyID = Convert.ToInt32(cookie);
            return companyID;
        }
        DbEnt db = new DbEnt();
        // GET: Reconciliations
        [CustomAuthorize(Roles = "Super Administrator")]
        public ActionResult Index()
        {
            int companyID = CompanyID();
            ViewBag.id = companyID;
            return View();
        }

        [HttpPost]
        public ActionResult AddActive(FormCollection f)
        {
            int companyID = CompanyID();

            Active active = new Active();
            active.accNo = f["accNo"].ToString();
            active.description = f["description"].ToString();
            active.category = f["category"].ToString();
            active.amount = Convert.ToDouble(f["amount"].ToString());
            active.companyID = companyID;
            active.datePerfomerd = DateTime.Now;

            if (f["category"].ToString() == "Fundamental Assets")
            {
                Amortization am = new Amortization();
                if (f["saleDate"].ToString() != "") am.saleDate = Convert.ToDateTime(f["saleDate"].ToString());
                am.amortizationRateYearly = Convert.ToDecimal(f["amortizationRate"].ToString());
                am.amount = Convert.ToInt32(f["amountAfterCalc"].ToString());
                am.purchaseDate = Convert.ToDateTime(f["purchaseDate"].ToString());

                var row = db.Amortizations.Add(am);
                active.amortizationID = row.amortizationID;
            }
            
            
            db.Actives.Add(active);
            var check = db.SaveChanges();
            if (check != 0)
            {
                return RedirectToAction("", "reconciliations");
            }

            return View();
        }

        public ActionResult EditActive(int id,FormCollection f)
        {
            var active = db.Actives.Find(id);

            active.accNo = f["accNo"].ToString();
            active.description = f["description"].ToString();
            active.category = f["category"].ToString();
            active.amount = Convert.ToDouble(f["amount"].ToString());
            if (active.amortizationID != null)
            {
                int? amortizationid = active.amortizationID;

                if (f["category"].ToString() == "Fundamental Assets")
                { 
                var am = db.Amortizations.Find(amortizationid);
                if (f["saleDate"].ToString() != "") am.saleDate = Convert.ToDateTime(f["saleDate"].ToString());
                am.amortizationRateYearly = Convert.ToDecimal(f["amortizationRate"].ToString());
                am.amount = Convert.ToInt32(f["amountAfterCalcEdit"].ToString());
                    am.purchaseDate = Convert.ToDateTime(f["purchaseDate"].ToString());

                }
                else
                {
                    var row = db.Amortizations.Find(amortizationid);
                    db.Amortizations.Remove(row);
                }
            }
            else
            {

                if (f["category"].ToString() == "Fundamental Assets")
                {
                    Amortization am = new Amortization();
                    if (f["saleDate"].ToString() != "") am.saleDate = Convert.ToDateTime(f["saleDate"].ToString());
                    am.amortizationRateYearly = Convert.ToDecimal(f["amortizationRate"].ToString());
                    am.amount = Convert.ToInt32(f["amountAfterCalcEdit"].ToString());
                    am.purchaseDate = Convert.ToDateTime(f["purchaseDate"].ToString());
                    var row = db.Amortizations.Add(am);
                    active.amortizationID = row.amortizationID;
                }
            }
            db.Entry(active).State = EntityState.Modified;
            var check = db.SaveChanges();
            if (check != 0)
            {
                return RedirectToAction("","reconciliations");
            }
            return View();
        }
       
        public JsonResult CalculateAmortization(double vf, double norma, DateTime blerja, string shitja)
        {
            List<string> items = new List<string>();
            double s = 0;
            if (shitja != "empty")
            {
                DateTime data = Convert.ToDateTime(shitja);
               
                int days = Convert.ToInt32((data - blerja).TotalDays);

                if (data.Year != blerja.Year)
                s = (vf * norma / 365) * (days-365);
                else
                    s = (vf * norma / 365) * days;

            }
            else
            {
               
                if (blerja.Month == 1)
                {
                    s = (vf * norma / 365) * 365;
                }
                else
                {
                    int days = blerja.DayOfYear;
                    s = (vf * norma / 365) * (365 - days);
                }
              
               

            }

            items.Add(s.ToString());
           

            return Json(items, JsonRequestBehavior.AllowGet);
        }

       

        [HttpPost]
        public ActionResult DeleteActive(int id)
        {
            var row = db.Actives.Find(id);
            db.Actives.Remove(row);
            var check = db.SaveChanges();

            if (check != 0)
            {
                return RedirectToAction("", "reconciliations");
            }
            return View();
        }


        [HttpPost]
        public ActionResult AddPassive(FormCollection f)
        {
            int companyID = CompanyID();

            Passive passive = new Passive();
            passive.accNo = f["accNo"].ToString();
            passive.description = f["description"].ToString();
            passive.category = f["categoryPassive"].ToString();
            passive.amount = Convert.ToDouble(f["amount"].ToString());
            passive.companyID = companyID;
            passive.datePerformed = DateTime.Now;
            db.Passives.Add(passive);

            var check = db.SaveChanges();

            if (check != 0)
            {
                return RedirectToAction("", "reconciliations");
            }
            return View();
        }
        [HttpPost]
        public ActionResult EditPassive(int id,FormCollection f)
        {
            var passive = db.Passives.Find(id);
            passive.accNo = f["accNo"].ToString();
            passive.description = f["description"].ToString();
            passive.category = f["categoryPassive"].ToString();
            passive.amount = Convert.ToDouble(f["amount"].ToString());

            var check = db.SaveChanges();

            if (check != 0)
            {
                return RedirectToAction("", "reconciliations");
            }
            return View();
        }
        [HttpPost]
        public ActionResult DeletePassive(int id)
        {
            var row = db.Passives.Find(id);
            db.Passives.Remove(row);
            var check = db.SaveChanges();

            if (check != 0)
            {
                return RedirectToAction("", "reconciliations");
            }
            return View();
        }

        [HttpGet]
        public ActionResult Balance(string from, string to)
        {
            int id = CompanyID();
            ViewBag.id = id;
            double? active = 0;
            double passive = 0;
            Calculations cal = new Calculations();
            if (from != null && to != null)
            {
                
                 active = cal.CalculateActive(id, from, to);
                passive = cal.CalculatePassive(id,from,to);


            }
            else
            {
                 active = cal.CalculateActive(id,null,null);
                passive = cal.CalculatePassive(id,null,null);


            }

            ViewBag.active = active;
            ViewBag.passive = passive;
            double? balance = active - passive;
            if (balance > 0)
            {
                ViewBag.balance = 0;
                ViewBag.shareholder = balance;
            }
            else if (balance < 0)
            {
                ViewBag.balance = balance;
                ViewBag.shareholder = 0;
            }
            else
            {
                ViewBag.balance = 0;
                ViewBag.shareholder = 0;
            }
           
            return View();
        }
    }
}