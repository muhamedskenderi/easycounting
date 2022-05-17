using easycounting.Models;


using PagedList;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.IO;
using System.Linq;
using System.Text;
using System.Web;
using System.Web.Mvc;
using iTextSharp.text;
using iTextSharp.text.pdf;
using iTextSharp.tool.xml;
using iTextSharp.text.html.simpleparser;
using iTextSharp.text.html;
using easycounting.ViewModels;
using System.Data;
using System.Text.RegularExpressions;

namespace easycounting.Controllers
{
    public class ExpensesController : Controller
    {
        DbEnt db = new DbEnt();

        [CustomAuthorize(Roles = "Super Administrator,Administrator,Manager,Employee")]

        public ActionResult Index()
        {

            int id = CompanyID();
            ViewBag.id = id;
            
            decimal paidPayments = db.Payments.Where(x => x.Vendor.companyID == id && x.status == "Paid").Count();
            decimal unpaidPayments = db.Payments.Where(x => x.Vendor.companyID == id && x.status == "Unpaid").Count();
            decimal overduePayments = db.Payments.Where(x => x.Vendor.companyID == id && x.status == "Overdue").Count();
            decimal s = paidPayments + unpaidPayments + overduePayments;

            decimal paidBills = db.Bills.Where(x => x.Vendor.companyID == id && x.status == "Paid").Count();
            decimal unpaidBills = db.Bills.Where(x => x.Vendor.companyID == id && x.status == "Unpaid").Count();
            decimal overdueBills = db.Bills.Where(x => x.Vendor.companyID == id && x.status == "Overdue").Count();
            decimal S = paidBills + unpaidBills + overdueBills;


            if (s != 0)
            {
                ViewBag.paidPayments = paidPayments / s * 100;
                ViewBag.overduePayments = (overduePayments / s) * 100;
                ViewBag.unpaidPayments = (unpaidPayments / s) * 100;

            }
            else
            {
                ViewBag.paidPayments = 0;
                ViewBag.overduePayments = 0;
                ViewBag.unpaidPayments = 0;

            }

            if (S != 0)
            {
                ViewBag.paidBills = (paidBills / S) * 100;
                ViewBag.overdueBills = (overdueBills / S) * 100;
                ViewBag.unpaidBills = (unpaidBills / S) * 100;
            }
            else
            {
                ViewBag.paidBills = 0;
                ViewBag.overdueBills = 0;
                ViewBag.unpaidBills = 0;
            }

            
            

            return View();

        }

        public int CompanyID()
        {
            var cookie = HttpUtility.UrlDecode(Request.Cookies["companyID"].Value.ToString());

            int companyID = Convert.ToInt32(cookie);
            return companyID;
        }


        [CustomAuthorize(Roles = "Super Administrator,Administrator,Manager,Employee")]
        [HttpGet]
        public ActionResult Create()
        {
            int id = CompanyID();
            Crypto decrypt = new Crypto();
            var getitems = db.Items.Where(x => x.companyID == id).ToList();
            SelectList list = new SelectList(getitems, "itemID", "name");
            ViewBag.items = list;
            
            var getexpenses = db.ExpenseCategories.Where(x => x.companyID == id).ToList();
            SelectList list2 = new SelectList(getexpenses, "expenseID", "name");
            ViewBag.expenses = list2;

            var getvendors = db.Vendors.Where(x => x.companyID == id).ToList();
            SelectList list3 = new SelectList(getvendors, "vendorID", "name");
            ViewBag.vendors = list3;

            var gettaxes = db.Taxes.Where(x => x.companyID == id).ToList();
            SelectList list4 = new SelectList((from x in db.Taxes.Where(x=>x.companyID == id).ToList()
                                               select new
                                               {
                                                   ID = x.taxID,
                                                   Data = x.name + " (" + x.value + " %)"
                                               }), "ID", "Data");
            ViewBag.taxes = list4;
            int companyID = CompanyID();
            SelectList getaccounts = new SelectList((from x in db.Accounts.Where(x => x.companyID == companyID).ToList()
                                                     select new
                                                     {
                                                         ID = x.accountID,
                                                         Data = x.bankName + " (" + String.Format("************{0}", decrypt.Base64Decode(x.cardNo).Trim().Substring(12, 4)) + " )"
                                                     }), "ID", "Data");
            ViewBag.accounts = getaccounts;

            return View();
        }


        [CustomAuthorize(Roles = "Super Administrator,Administrator,Manager,Employee")]
        [HttpPost]
        public ActionResult Create(FormCollection f, Bill model)
        {
            int companyID = CompanyID();
            Bill bill = new Bill();
            bill.billNo = f["billNo"].ToString();
            bill.companyID = companyID;
            bill.expenseID = Convert.ToInt32(f["expenseID"].ToString());
            bill.billDate = Convert.ToDateTime(f["billDate"].ToString());
            bill.dueDate = Convert.ToDateTime(f["dueDate"].ToString());
            if (f["discountValue"].ToString() != "")
            {
                bill.discount = Convert.ToInt32(f["discountValue"].ToString());

            }
            else
            {
                bill.discount = 0;
            }
            if (f["taxID"].ToString() != "0")
            {
                bill.taxID = Convert.ToInt32(f["taxID"].ToString());

            }
            bill.total = model.total;
            bill.status = f["billStatus"].ToString();
            bill.notes = f["notes"].ToString();
            bill.vendorID = Convert.ToInt32(f["vendorsList"].ToString());
            var billRow = db.Bills.Add(bill);
            int counter = model.ItemsCount;

            if (counter == 0) counter = 1;
            for (var i = 1; i <= counter; i++)
            {
                ItemsInBill item = new ItemsInBill();

                item.itemID = Convert.ToInt32(f["itemID" + i]);
                item.qt = Convert.ToInt32(f["qt" + i]);
                item.billID = billRow.billID;

                db.ItemsInBills.Add(item);
            }

            if (Convert.ToInt32(f["chosenAccount"].ToString()) != 0 && f["chosenAccount"].ToString() != null)
            {
                var user = db.UsersInCompanies.Where(x => x.companyID == companyID).SingleOrDefault();
                Transaction transaction = new Transaction();

                transaction.transactionID = DateTime.Now.ToString("ssmmddHHMMyyyyff");
                transaction.accountID = Convert.ToInt32(Convert.ToInt32(f["chosenAccount"].ToString()));
                transaction.status = "Paid";
                transaction.userID = user.userID;
                transaction.billID = bill.billID;
                transaction.payrollID = null;
                transaction.date = Convert.ToDateTime(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
                bill.status = "Paid";

                db.Transactions.Add(transaction);
            }

            var checker = db.SaveChanges();

            if (checker != 0)
            {
                return RedirectToAction("bill", "expenses", new { id= billRow.billID });
            }



            return View();
        }


        public string RemoveSpecialCharacters(string str)
        {
            StringBuilder sb = new StringBuilder();
            foreach (char c in str)
            {
                if ((c >= '0' && c <= '9') || (c >= 'A' && c <= 'Z') || (c >= 'a' && c <= 'z') || c == '.' || c == '_')
                {
                    sb.Append(c);
                }
            }
            return sb.ToString();
        }

        [CustomAuthorize(Roles = "Super Administrator,Administrator,Manager,Employee")]
        public ActionResult Bill(int id)
        {
            Crypto decrypt = new Crypto();
            int companyid = CompanyID();
            SelectList getaccounts = new SelectList((from x in db.Accounts.Where(x => x.companyID == companyid).ToList()
                                                     select new
                                                     {
                                                         ID = x.accountID,
                                                         Data = x.bankName + " (" + String.Format("************{0}", decrypt.Base64Decode(x.cardNo).Trim().Substring(12, 4)) + " )"
                                                     }), "ID", "Data");
         
            var d = db.Companies.Where(x => x.companyID == companyid).Single();
            ViewBag.CompanyName = d.name;
            ViewBag.accounts = getaccounts;

            return View(db.Bills.Single(x => x.billID == id));

        }

        [HttpPost]
        [CustomAuthorize(Roles = "Super Administrator,Administrator,Manager,Employee")]

        public ActionResult PayBill(int billID, FormCollection f)
        {
            var row = db.Bills.Where(x => x.billID == billID).SingleOrDefault();

            int companyID = CompanyID();
            Transaction transaction = new Transaction();
            var user = db.UsersInCompanies.Where(x => x.companyID == companyID).SingleOrDefault();
            transaction.accountID = Convert.ToInt32(f["accountID"].ToString());
            transaction.transactionID = DateTime.Now.ToString("ssmmddHHMMyyyyff");
            transaction.paymentID =null;
            transaction.billID = billID;
            transaction.status = "Paid";
            transaction.userID = user.userID;
            transaction.date = DateTime.Now;
            transaction.payrollID = null;


            row.status = "Paid";
            db.Transactions.Add(transaction);
            var check = db.SaveChanges();
            if (check != 0)
            {
                return RedirectToAction("bill","expenses", new { id= billID });
            
            }
            return View();
        
        }


        [CustomAuthorize(Roles = "Super Administrator,Administrator,Manager,Employee")]
        [HttpGet]
        public ActionResult EditBill(int id)
        {
            Crypto decrypt = new Crypto();
            int companyID = CompanyID();
            var row = db.Bills.Find(id);
            string chosenVendor = row.vendorID.ToString();
            ViewBag.companyID = companyID;
            var getitems = db.Items.Where(x => x.companyID == companyID).ToList();
            SelectList list = new SelectList(getitems, "itemID", "name");
            ViewBag.items = list;

            var getexpenses = db.ExpenseCategories.Where(x => x.companyID == companyID).ToList();
            SelectList list2 = new SelectList(getexpenses, "expenseID", "name");
            ViewBag.expenses = list2;

            var getvendors = db.Vendors.Where(x => x.companyID == companyID).ToList();
            SelectList list3 = new SelectList(getvendors, "vendorID", "name");
            ViewBag.vendors = list3;

            var gettaxes = db.Taxes.Where(x => x.companyID == companyID).ToList();
            SelectList list4 = new SelectList((from x in db.Taxes.Where(x => x.companyID == id).ToList()
                                               select new
                                               {
                                                   ID = x.taxID,
                                                   Data = x.name + " (" + x.value + " %)"
                                               }), "ID", "Data");
            ViewBag.taxes = list4;


            SelectList getaccounts = new SelectList((from x in db.Accounts.Where(x => x.companyID == companyID).ToList()
                                                     select new
                                                     {
                                                         ID = x.accountID,
                                                         Data = x.bankName + " (" + String.Format("************{0}", decrypt.Base64Decode(x.cardNo).Trim().Substring(12, 4)) + " )"
                                                     }), "ID", "Data");
            ViewBag.accounts = getaccounts;
            ViewBag.chosenVendor = chosenVendor;

            var check = db.Bills.Where(x => x.billID == id).Single();
            if (check.status == "Paid") return RedirectToAction("bill", "expenses", new { id = check.billID });
            else
                return View(check);

        }


        [HttpPost]
        [CustomAuthorize(Roles = "Super Administrator,Administrator,Manager,Employee")]

        public ActionResult EditBill(int id, Bill model, FormCollection f)
        {
            int companyid = CompanyID();
            var bill = db.Bills.Find(id);
            var fieldsToRemove = db.ItemsInBills.Where(x => x.billID == id).ToList();

            if (model.fieldChanged)
            {
                foreach (var i in fieldsToRemove)
                {
                    ItemsInBill ib = db.ItemsInBills.Find(i.id);
                    db.ItemsInBills.Remove(ib);
                }
                int counter = Convert.ToInt32(f["ItemsCount"].ToString());
                if (counter == 0) counter = 1;
                for (var i = 1; i <= counter; i++)
                {
                    ItemsInBill item = new ItemsInBill();

                    item.itemID = Convert.ToInt32(f["itemID" + i]);
                    item.qt = Convert.ToInt32(f["qt" + i]);
                    item.billID = id;
                    db.ItemsInBills.Add(item);
                }

            }

            bill.billNo = f["billNo"].ToString();
            bill.expenseID = Convert.ToInt32(f["expenseID"].ToString());
            bill.billDate = Convert.ToDateTime(f["billDate"].ToString());
            bill.dueDate = Convert.ToDateTime(f["dueDate"].ToString());
            bill.total = Convert.ToDouble(Regex.Replace(f["total"].ToString(), @"[^0-9a-zA-Z]+", "."));
            bill.status = f["billStatus"].ToString();
            bill.notes = f["notes"].ToString();
            bill.vendorID = Convert.ToInt32(f["vendorsList"].ToString());
            if (f["discountValue"].ToString() != "")
            {
                bill.discount = Convert.ToInt32(f["discountValue"].ToString());

            }
            else
            {
                bill.discount = 0;
            }
          

            if (Convert.ToInt32(Regex.Replace(f["taxID"].ToString(), @"[^0-9a-zA-Z]+", "")) != 0)
            {
                bill.taxID = Convert.ToInt32(Regex.Replace(f["taxID"].ToString(), @"[^0-9a-zA-Z]+", ""));

            }

            if (Convert.ToInt32(f["chosenAccount"].ToString()) != 0 && f["chosenAccount"].ToString() != null)
            {
                var user = db.UsersInCompanies.Where(x => x.companyID == companyid).SingleOrDefault();
                Transaction transaction = new Transaction();

                transaction.transactionID = DateTime.Now.ToString("ssmmddHHMMyyyyff");
                    transaction.accountID = Convert.ToInt32(Convert.ToInt32(f["chosenAccount"].ToString()));
                transaction.status = "Paid";
                transaction.userID = user.userID;
                transaction.billID = bill.billID;
                transaction.payrollID = null;


                transaction.date = Convert.ToDateTime(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
                bill.status = "Paid";

                db.Transactions.Add(transaction);
            }


            var check = db.SaveChanges();
            if (check != 0)
            {

                return RedirectToAction("", "expenses");
            }
            return View();
        }



        [HttpGet]
        [CustomAuthorize(Roles = "Super Administrator,Administrator,Manager,Employee")]
        public ActionResult AllBills(int? page)
        {
            int id = CompanyID();


            var items = db.Bills.Where(x => x.Vendor.companyID == id).ToList().OrderByDescending(x=>x.billID);
            ViewBag.OnePageOfUsers = items;

            if (items != null)
                return View(items.ToPagedList(page ?? 1, 10));
            else
                return View();

        }

        [HttpGet]
        [CustomAuthorize(Roles = "Super Administrator,Administrator,Manager,Employee")]
        public ActionResult AllPayments(int? page)
        {
            int id = CompanyID();


            var items = db.Payments.Where(x => x.Vendor.companyID == id).ToList().OrderByDescending(x => x.date);
            ViewBag.OnePageOfUsers = items;

            if (items != null)
                return View(items.ToPagedList(page ?? 1, 10));
            else
                return View();

        }



        [HttpPost]
        [CustomAuthorize(Roles = "Super Administrator,Administrator,Manager,Employee")]
        public ActionResult CreateCategory(FormCollection f, string from, int? id)
        {
            string username = User.Identity.Name;
            int compID = CompanyID();
            var company = db.Users.Where(x => x.username == username).SingleOrDefault();
            if (company != null)
            {

                ExpenseCategory expense = new ExpenseCategory
                {
                    name = f["expenseName"].ToString(),
                    description = f["expenseDescription"].ToString(),
                    companyID = compID
                };
                db.ExpenseCategories.Add(expense);
                var check = db.SaveChanges();
                if (check != 0)
                {
                    if (from == "bills")
                        return RedirectToAction("create", "expenses");
                    if (from == "settings")
                        return RedirectToAction("expenses", "settings");

                }

            }
            return View();
        }

        [HttpPost]
        [CustomAuthorize(Roles = "Super Administrator,Administrator,Manager,Employee")]

        public ActionResult DeleteBill(int id)
        {
            Bill i = db.Bills.Find(id);
            db.Bills.Remove(i);
            var check = db.SaveChanges();
            if (check != 0)
            {
                return RedirectToAction("", "expenses");
            }

            return View();
        }


        [HttpPost]
        [CustomAuthorize(Roles = "Super Administrator,Administrator,Manager,Employee")]

        public ActionResult DeleteCategory(int id)
        {
            ExpenseCategory i = db.ExpenseCategories.Find(id);
            db.ExpenseCategories.Remove(i);
            var check = db.SaveChanges();
            if (check != 0)
            {
                return RedirectToAction("expenses", "settings");
            }

            return View();
        }

        [HttpPost]
        [CustomAuthorize(Roles = "Super Administrator,Administrator,Manager,Employee")]

        public ActionResult EditCategory(int id, FormCollection f)
        {
            string name = f["expenseName"].ToString();
            int expenseID = CompanyID();
            string description = f["expenseDescription"].ToString();
            var expense = db.ExpenseCategories.Find(id);
            expense.name = name;
            expense.description = description;
            expense.companyID = expenseID;
            var check = db.SaveChanges();
            if (check != 0)
            {
                return RedirectToAction("expenses", "settings");
            }
            return View();
        }


        public JsonResult GetVendorDetails(int id)
        {
            int companyid = CompanyID();

            var row = db.Vendors.Where(x => x.vendorID == id && x.companyID == companyid).SingleOrDefault();

            List<string> vendors = new List<string>();
            vendors.Add(row.name);
            vendors.Add(row.taxNo);
            vendors.Add(row.address);
            vendors.Add(row.phone);
            vendors.Add(row.email);
            return Json(vendors, JsonRequestBehavior.AllowGet);
        }

        public JsonResult GetItemDetails(int id)
        {
            int companyid = CompanyID();

            var row = db.Items.Where(x => x.itemID == id && x.companyID == companyid).SingleOrDefault();

            List<string> items = new List<string>();
            items.Add(row.itemID.ToString());
            items.Add(row.purchasePrice.ToString());
            items.Add(row.description.ToString());
            items.Add(row.ItemsCategory.name.ToString());

            return Json(items, JsonRequestBehavior.AllowGet);
        }

        
     
        public JsonResult GetAllItems(int billid)
        {
            int companyid = CompanyID();
            var row = db.ItemsInBills.Where(x => x.billID == billid && x.Bill.companyID== companyid).ToList();
            var dict = new Dictionary<string, string[]>();
            List<string[]> items = new List<string[]>();
            foreach (var i in row)
            {
                string[] elem = {  i.itemID.ToString(), i.Item.purchasePrice.ToString(), i.Item.description.ToString(), i.Item.ItemsCategory.name.ToString(),   i.qt.ToString(),  };
                items.Add(elem);
            }

            return Json(items, JsonRequestBehavior.AllowGet);
        }

      

        public JsonResult GetTaxDetails(int id)
        {

            var row = db.Taxes.Where(x => x.taxID == id).FirstOrDefault();
            List<string> tax = new List<string>();
            tax.Add(row.value.ToString());
            tax.Add(row.taxID.ToString());

            return Json(tax, JsonRequestBehavior.AllowGet);
        }

        public JsonResult GetBillDetails(int id)
        {
            int companyid = CompanyID();

            var row = db.Bills.Where(x => x.billID == id && x.companyID == companyid).SingleOrDefault();

            List<string> items = new List<string>();
            items.Add(row.Vendor.name.ToString());
            items.Add(row.Vendor.address.ToString());
            items.Add(row.Vendor.taxNo.ToString());
            items.Add(row.Vendor.phone.ToString());
            items.Add(row.Vendor.email.ToString());
            items.Add(row.status.ToString());
            items.Add(row.expenseID.ToString());
            items.Add(row.billNo.ToString());
            items.Add(row.billDate.ToString("yyyy-MM-dd"));
            items.Add(row.dueDate.ToString("yyyy-MM-dd"));
            items.Add(row.total.ToString());
            items.Add(row.notes.ToString());




            return Json(items, JsonRequestBehavior.AllowGet);
        }



     
        public ActionResult DownloadBill(int id)
        {
            GenerateBill(id);
            return View();
        
        }

        [CustomAuthorize(Roles = "Super Administrator,Administrator,Manager,Employee")]

        public ActionResult PrintBill(int id)
        {
            var row = db.Bills.Where(x => x.billID == id).SingleOrDefault();
            if (row != null)
                return View(row);
            else
                return RedirectToAction("notofund", "error");
        }

        #region--Generate Bill PDF--
        public void GenerateBill(int id)
        {
            double subtotal = 0;
            int k = 1;
            int tax = 0;
            int companyid = CompanyID();
            var company = db.Companies.Where(x => x.companyID == companyid).SingleOrDefault();
            var bill = db.Bills.Where(x => x.billID == id).SingleOrDefault();
            if (bill.taxID != null)
            {
                var taxFind = db.Taxes.Where(x => x.taxID == bill.taxID).Single();
                tax = taxFind.value;
            }
            int discount = bill.discount;
            var items = db.ItemsInBills.Where(x => x.billID == id).ToList();
            Document pdfDoc = new Document(PageSize.A4, 10, 10, 10, 10);
            MemoryStream PDFData = new MemoryStream();
            PdfWriter writer = PdfWriter.GetInstance(pdfDoc, PDFData);

            var titleFont = FontFactory.GetFont("Arial", 12, Font.BOLD);
            var titleFontBlue = FontFactory.GetFont("Arial", 14, Font.NORMAL, BaseColor.BLUE);
            var boldTableFont = FontFactory.GetFont("Arial", 8, Font.BOLD);
            var bodyFont = FontFactory.GetFont("Arial", 8, Font.NORMAL);
            var EmailFont = FontFactory.GetFont("Arial", 8, Font.NORMAL, BaseColor.BLUE);
            BaseColor TabelHeaderBackGroundColor = WebColors.GetRGBColor("#EEEEEE");

            Rectangle pageSize = writer.PageSize;
            // Open the Document for writing
            pdfDoc.Open();
            //Add elements to the document here

            #region Top table
            // Create the header table 
            PdfPTable headertable = new PdfPTable(3);
            headertable.HorizontalAlignment = 0;
            headertable.WidthPercentage = 100;
            headertable.SetWidths(new float[] { 100f, 320f, 100f });  // then set the column's __relative__ widths
            headertable.DefaultCell.Border = Rectangle.NO_BORDER;
            //headertable.DefaultCell.Border = Rectangle.BOX; //for testing           

            iTextSharp.text.Image logo = iTextSharp.text.Image.GetInstance(HttpContext.Server.MapPath("~/assets/img/bill.png"));
            logo.ScaleToFit(300, 45);

            {
                PdfPCell pdfCelllogo = new PdfPCell(logo);
                pdfCelllogo.Border = Rectangle.NO_BORDER;
                pdfCelllogo.BorderColorBottom = new BaseColor(System.Drawing.Color.Black);
                pdfCelllogo.BorderWidthBottom = 1f;
                headertable.AddCell(pdfCelllogo);
            }

            {
                PdfPCell middlecell = new PdfPCell();
                middlecell.Border = Rectangle.NO_BORDER;
                middlecell.BorderColorBottom = new BaseColor(System.Drawing.Color.Black);
                middlecell.BorderWidthBottom = 1f;
                headertable.AddCell(middlecell);
            }

            {
                PdfPTable nested = new PdfPTable(1);
                nested.DefaultCell.Border = Rectangle.NO_BORDER;
                PdfPCell nextPostCell1 = new PdfPCell(new Phrase(company.name, titleFont));
                nextPostCell1.Border = Rectangle.NO_BORDER;
                nested.AddCell(nextPostCell1);
                PdfPCell nextPostCell2 = new PdfPCell(new Phrase(company.address, bodyFont));
                nextPostCell2.Border = Rectangle.NO_BORDER;
                nested.AddCell(nextPostCell2);
                PdfPCell nextPostCell3 = new PdfPCell(new Phrase(company.phone, bodyFont));
                nextPostCell3.Border = Rectangle.NO_BORDER;
                nested.AddCell(nextPostCell3);
                PdfPCell nextPostCell4 = new PdfPCell(new Phrase(company.email, EmailFont));
                nextPostCell4.Border = Rectangle.NO_BORDER;
                nested.AddCell(nextPostCell4);
                nested.AddCell("");
                PdfPCell nesthousing = new PdfPCell(nested);
                nesthousing.Border = Rectangle.NO_BORDER;
                nesthousing.BorderColorBottom = new BaseColor(System.Drawing.Color.Black);
                nesthousing.BorderWidthBottom = 1f;
                nesthousing.Rowspan = 5;
                nesthousing.PaddingBottom = 10f;
                headertable.AddCell(nesthousing);
            }


            PdfPTable Invoicetable = new PdfPTable(3);
            Invoicetable.HorizontalAlignment = 0;
            Invoicetable.WidthPercentage = 100;
            Invoicetable.SetWidths(new float[] { 100f, 320f, 100f });  // then set the column's __relative__ widths
            Invoicetable.DefaultCell.Border = Rectangle.NO_BORDER;
            Invoicetable.SpacingAfter = 40;
            {
                PdfPTable nested = new PdfPTable(1);
                nested.DefaultCell.Border = Rectangle.NO_BORDER;
                PdfPCell nextPostCell1 = new PdfPCell(new Phrase("Bill To:", bodyFont));
                nextPostCell1.Border = Rectangle.NO_BORDER;
                nested.AddCell(nextPostCell1);
                PdfPCell nextPostCell2 = new PdfPCell(new Phrase(bill.Vendor.name, titleFont));
                nextPostCell2.Border = Rectangle.NO_BORDER;
                nested.AddCell(nextPostCell2);
                PdfPCell nextPostCell3 = new PdfPCell(new Phrase(bill.Vendor.address, bodyFont));
                nextPostCell3.Border = Rectangle.NO_BORDER;
                nested.AddCell(nextPostCell3);
                PdfPCell nextPostCell4 = new PdfPCell(new Phrase(bill.Vendor.email, EmailFont));
                nextPostCell4.Border = Rectangle.NO_BORDER;
                nested.AddCell(nextPostCell4);
                nested.AddCell("");
                PdfPCell nesthousing = new PdfPCell(nested);
                nesthousing.Border = Rectangle.NO_BORDER;
                nesthousing.BorderColorBottom = new BaseColor(System.Drawing.Color.Black);
                nesthousing.BorderWidthBottom = 1f;
                nesthousing.Rowspan = 5;
                nesthousing.PaddingBottom = 10f;
                Invoicetable.AddCell(nesthousing);
                
            }

            {
                PdfPCell middlecell = new PdfPCell();
                middlecell.Border = Rectangle.NO_BORDER;
                //middlecell.BorderColorBottom = new BaseColor(System.Drawing.Color.Black);
                //middlecell.BorderWidthBottom = 1f;
                Invoicetable.AddCell(middlecell);
            }


            {
                PdfPTable nested = new PdfPTable(1);
                nested.DefaultCell.Border = Rectangle.NO_BORDER;
                PdfPCell nextPostCell1 = new PdfPCell(new Phrase("Bill No. " + bill.billNo, titleFontBlue));
                nextPostCell1.Border = Rectangle.NO_BORDER;
                nested.AddCell(nextPostCell1);
                PdfPCell nextPostCell2 = new PdfPCell(new Phrase("Date of Bill: " + bill.billDate.ToString("dd/MM/yyyy"), bodyFont));
                nextPostCell2.Border = Rectangle.NO_BORDER;
                nested.AddCell(nextPostCell2);
                PdfPCell nextPostCell3 = new PdfPCell(new Phrase("Due Date: " + bill.dueDate.ToString("dd/MM/yyyy"), bodyFont));
                nextPostCell3.Border = Rectangle.NO_BORDER;
                nested.AddCell(nextPostCell3);
                nested.AddCell("");
                PdfPCell nesthousing = new PdfPCell(nested);
                nesthousing.Border = Rectangle.NO_BORDER;
                //nesthousing.BorderColorBottom = new BaseColor(System.Drawing.Color.Black);
                //nesthousing.BorderWidthBottom = 1f;
                nesthousing.Rowspan = 5;
                nesthousing.PaddingBottom = 10f;
                Invoicetable.AddCell(nesthousing);
            }


            pdfDoc.Add(headertable);
            Invoicetable.PaddingTop = 10f;

            pdfDoc.Add(Invoicetable);
            #endregion

            #region Items Table
            //Create body table
            PdfPTable itemTable = new PdfPTable(5);

            itemTable.HorizontalAlignment = 0;
            itemTable.WidthPercentage = 100;
            itemTable.SetWidths(new float[] { 5, 20, 30, 20, 25 });  // then set the column's __relative__ widths
            itemTable.SpacingAfter = 75;
            itemTable.DefaultCell.Border = Rectangle.BOX;
            PdfPCell cell1 = new PdfPCell(new Phrase("NO", boldTableFont));
            cell1.BackgroundColor = TabelHeaderBackGroundColor;
            cell1.HorizontalAlignment = Element.ALIGN_CENTER;
            itemTable.AddCell(cell1);

            PdfPCell cell2 = new PdfPCell(new Phrase("ITEMS", boldTableFont));
            cell2.BackgroundColor = TabelHeaderBackGroundColor;
            cell2.HorizontalAlignment = Element.ALIGN_CENTER;
            itemTable.AddCell(cell2);

            PdfPCell cell3 = new PdfPCell(new Phrase("DESCRIPTION", boldTableFont));
            cell3.BackgroundColor = TabelHeaderBackGroundColor;
            cell3.HorizontalAlignment = 1;
            itemTable.AddCell(cell3);
            PdfPCell cell4 = new PdfPCell(new Phrase("QUANTITY", boldTableFont));
            cell4.BackgroundColor = TabelHeaderBackGroundColor;
            cell4.HorizontalAlignment = Element.ALIGN_CENTER;
            itemTable.AddCell(cell4);
            
            PdfPCell cell5 = new PdfPCell(new Phrase("PRICE", boldTableFont));
            cell5.BackgroundColor = TabelHeaderBackGroundColor;
            cell5.HorizontalAlignment = Element.ALIGN_CENTER;
            itemTable.AddCell(cell5);
           
            //foreach (DataRow row in dt.Rows)
            foreach (var i in items)
            {
                PdfPCell numberCell = new PdfPCell(new Phrase(k.ToString(), bodyFont));
                numberCell.HorizontalAlignment = 1;
                numberCell.PaddingLeft = 10f;
                numberCell.Border = Rectangle.LEFT_BORDER | Rectangle.RIGHT_BORDER;
                itemTable.AddCell(numberCell);

                var _phrase = new Phrase();
                _phrase.Add(new Chunk(i.Item.name, EmailFont));
                PdfPCell descCell = new PdfPCell(_phrase);
                descCell.HorizontalAlignment = 0;
                descCell.PaddingLeft = 10f;
                descCell.Border = Rectangle.LEFT_BORDER | Rectangle.RIGHT_BORDER;
                itemTable.AddCell(descCell);

                PdfPCell descriptionCelll = new PdfPCell(new Phrase(i.Item.description, bodyFont));
                descriptionCelll.HorizontalAlignment = 1;
                descriptionCelll.PaddingLeft = 10f;
                descriptionCelll.Border = Rectangle.LEFT_BORDER | Rectangle.RIGHT_BORDER;
                itemTable.AddCell(descriptionCelll);

                PdfPCell qtyCell = new PdfPCell(new Phrase(i.qt.ToString(), bodyFont));
                qtyCell.HorizontalAlignment = 1;
                qtyCell.PaddingLeft = 10f;
                qtyCell.Border = Rectangle.LEFT_BORDER | Rectangle.RIGHT_BORDER;
                itemTable.AddCell(qtyCell);

                PdfPCell amountCell = new PdfPCell(new Phrase(i.Item.purchasePrice.ToString(), bodyFont));
                amountCell.HorizontalAlignment = 1;
                amountCell.PaddingLeft = 10f;
                amountCell.Border = Rectangle.LEFT_BORDER | Rectangle.RIGHT_BORDER;
                itemTable.AddCell(amountCell);


                subtotal += i.Item.purchasePrice * i.qt; 

                k++;
            }
            // Table footer
            PdfPCell subtotalAmtCell1 = new PdfPCell(new Phrase(""));
            subtotalAmtCell1.Border = Rectangle.LEFT_BORDER | Rectangle.TOP_BORDER;
            itemTable.AddCell(subtotalAmtCell1);
            PdfPCell subtotalAmtCell2 = new PdfPCell(new Phrase(""));
            subtotalAmtCell2.Border = Rectangle.TOP_BORDER; //Rectangle.NO_BORDER; //Rectangle.TOP_BORDER;
            itemTable.AddCell(subtotalAmtCell2);
            PdfPCell subtotalAmtCell3 = new PdfPCell(new Phrase(""));
            subtotalAmtCell3.Border = Rectangle.TOP_BORDER; //Rectangle.NO_BORDER; //Rectangle.TOP_BORDER;
            itemTable.AddCell(subtotalAmtCell3);

            PdfPCell subtotalAmtStrCell = new PdfPCell(new Phrase("Subtotal", boldTableFont));
            subtotalAmtStrCell.Border = Rectangle.TOP_BORDER;   //Rectangle.NO_BORDER; //Rectangle.TOP_BORDER;
            subtotalAmtStrCell.HorizontalAlignment = 1;
            itemTable.AddCell(subtotalAmtStrCell);

            PdfPCell subtotalAmtCell = new PdfPCell(new Phrase(String.Format("{0:0.00}", subtotal) + " MKD", boldTableFont));
            subtotalAmtCell.HorizontalAlignment = 1;
            itemTable.AddCell(subtotalAmtCell);


            PdfPCell discountAmtCell1 = new PdfPCell(new Phrase(""));
            discountAmtCell1.Border = Rectangle.LEFT_BORDER | Rectangle.TOP_BORDER;
            itemTable.AddCell(discountAmtCell1);
            PdfPCell discountAmtCell2 = new PdfPCell(new Phrase(""));
            discountAmtCell2.Border = Rectangle.TOP_BORDER; //Rectangle.NO_BORDER; //Rectangle.TOP_BORDER;
            itemTable.AddCell(discountAmtCell2);
            PdfPCell discountAmtCell3 = new PdfPCell(new Phrase(""));
            discountAmtCell3.Border = Rectangle.TOP_BORDER; //Rectangle.NO_BORDER; //Rectangle.TOP_BORDER;
            itemTable.AddCell(discountAmtCell3);

            PdfPCell discountAmtStrCell = new PdfPCell(new Phrase("Discount", boldTableFont));
            discountAmtStrCell.Border = Rectangle.TOP_BORDER;   //Rectangle.NO_BORDER; //Rectangle.TOP_BORDER;
            discountAmtStrCell.HorizontalAlignment = 1;
            itemTable.AddCell(discountAmtStrCell);

            PdfPCell discountAmtCell = new PdfPCell(new Phrase(discount.ToString() + " %", boldTableFont));
            discountAmtCell.HorizontalAlignment = 1;
            itemTable.AddCell(discountAmtCell);



            //tax 

            PdfPCell taxAmtCell1 = new PdfPCell(new Phrase(""));
            taxAmtCell1.Border = Rectangle.LEFT_BORDER | Rectangle.TOP_BORDER;
            itemTable.AddCell(taxAmtCell1);
            PdfPCell taxAmtCell2 = new PdfPCell(new Phrase(""));
            taxAmtCell2.Border = Rectangle.TOP_BORDER; //Rectangle.NO_BORDER; //Rectangle.TOP_BORDER;
            itemTable.AddCell(taxAmtCell2);
            PdfPCell taxAmtCell3 = new PdfPCell(new Phrase(""));
            taxAmtCell3.Border = Rectangle.TOP_BORDER; //Rectangle.NO_BORDER; //Rectangle.TOP_BORDER;
            itemTable.AddCell(taxAmtCell3);

            PdfPCell taxAmtStrCell = new PdfPCell(new Phrase("Tax", boldTableFont));
            subtotalAmtStrCell.Border = Rectangle.TOP_BORDER;   //Rectangle.NO_BORDER; //Rectangle.TOP_BORDER;
            subtotalAmtStrCell.HorizontalAlignment = 1;
            itemTable.AddCell(subtotalAmtStrCell);

            PdfPCell taxAmtCell = new PdfPCell(new Phrase(tax + " %", boldTableFont));
            taxAmtCell.HorizontalAlignment = 1;
            itemTable.AddCell(taxAmtCell);

            // tax end


            PdfPCell totalAmtCell1 = new PdfPCell(new Phrase(""));
            totalAmtCell1.Border = Rectangle.LEFT_BORDER | Rectangle.TOP_BORDER;
            itemTable.AddCell(totalAmtCell1);
            PdfPCell totalAmtCell2 = new PdfPCell(new Phrase(""));
            totalAmtCell2.Border = Rectangle.TOP_BORDER; //Rectangle.NO_BORDER; //Rectangle.TOP_BORDER;
            itemTable.AddCell(totalAmtCell2);
            PdfPCell totalAmtCell3 = new PdfPCell(new Phrase(""));
            totalAmtCell3.Border = Rectangle.TOP_BORDER; //Rectangle.NO_BORDER; //Rectangle.TOP_BORDER;
            itemTable.AddCell(totalAmtCell3);


            PdfPCell totalAmtStrCell = new PdfPCell(new Phrase("Total Amount", boldTableFont));
            totalAmtStrCell.Border = Rectangle.TOP_BORDER;   //Rectangle.NO_BORDER; //Rectangle.TOP_BORDER;
            totalAmtStrCell.HorizontalAlignment = 1;
            itemTable.AddCell(totalAmtStrCell);
            
            PdfPCell totalAmtCell = new PdfPCell(new Phrase(String.Format("{0:0.00}", bill.total) + "MKD", boldTableFont));
            totalAmtCell.HorizontalAlignment = 1;
            itemTable.AddCell(totalAmtCell);



           



            PdfPCell cell = new PdfPCell(new Phrase("***NOTES: " + bill.notes + " ***", bodyFont));
            cell.Colspan = 5;
            cell.HorizontalAlignment = 1;
            itemTable.AddCell(cell);
            pdfDoc.Add(itemTable);


            #endregion

            PdfContentByte cb = new PdfContentByte(writer);


            BaseFont bf = BaseFont.CreateFont(BaseFont.HELVETICA, BaseFont.CP1250, true);
            cb = new PdfContentByte(writer);
            cb = writer.DirectContent;
            cb.BeginText();
            cb.SetFontAndSize(bf, 8);
            cb.SetTextMatrix(pageSize.GetLeft(120), 20);
            cb.EndText();

            //Move the pointer and draw line to separate footer section from rest of page
            cb.MoveTo(40, pdfDoc.PageSize.GetBottom(50));
            cb.LineTo(pdfDoc.PageSize.Width - 40, pdfDoc.PageSize.GetBottom(50));
            cb.Stroke();

            pdfDoc.Close();
            DownloadPDF(PDFData,bill.billNo);


        }
        #endregion

        #region--Download PDF
        protected void DownloadPDF(System.IO.MemoryStream PDFData, string billNo)
        {
            // Clear response content & headers
            HttpContext.Response.Clear();
            HttpContext.Response.ClearContent();
            HttpContext.Response.ClearHeaders();
            HttpContext.Response.ContentType = "application/pdf";
            HttpContext.Response.Charset = string.Empty;
            HttpContext.Response.Cache.SetCacheability(System.Web.HttpCacheability.Public);
            HttpContext.Response.AddHeader("Content-Disposition", string.Format("attachment;filename=Bill-{0}.pdf", billNo));
            HttpContext.Response.OutputStream.Write(PDFData.GetBuffer(), 0, PDFData.GetBuffer().Length);
            HttpContext.Response.OutputStream.Flush();
            HttpContext.Response.OutputStream.Close();
            HttpContext.Response.End();
        }
        #endregion

        [HttpGet]
        [CustomAuthorize(Roles = "Super Administrator,Administrator,Manager,Employee")]

        public ActionResult AddPayment()
        {
            Crypto decrypt = new Crypto();
            int companyID = CompanyID();
        
            SelectList getaccounts = new SelectList((from x in db.Accounts.Where(x => x.companyID == companyID).ToList()
                                               select new
                                               {
                                                   ID = x.accountID,
                                                   Data = x.bankName + " (" + String.Format("************{0}", decrypt.Base64Decode(x.cardNo).Trim().Substring(12, 4)) + " )"
                                               }), "ID", "Data");

            SelectList getvendors = new SelectList((from x in db.Vendors.Where(x => x.companyID == companyID).ToList()
                                                     select new
                                                     {
                                                         ID = x.vendorID,
                                                         Data = x.name + " (" + x.bankNo + " )"
                                                     }), "ID", "Data");
            SelectList getcategories = new SelectList((from x in db.ExpenseCategories.Where(x=>x.companyID == companyID).ToList()
                                                    select new
                                                    {
                                                        ID = x.expenseID,
                                                        Data = x.name
                                                    }), "ID", "Data");

            var dict = new Dictionary<string, string>
                {                    
                    { "Unpaid", "Unpaid" },
                    { "Paid", "Paid" },
                    { "Overdue", "Overdue" }
                };
            SelectList setstatus = new SelectList(dict, "Key", "Value");
            ViewBag.accounts = getaccounts;
            ViewBag.vendors = getvendors;
            ViewBag.status = setstatus;
            ViewBag.categories = getcategories;
            return View();
        }

        


        [HttpPost]
        [CustomAuthorize(Roles = "Super Administrator,Administrator,Manager,Employee")]

        public ActionResult AddPayment(Payment model,FormCollection f)
        {
            Crypto decrypt = new Crypto();
            int companyID = CompanyID();

            SelectList getaccounts = new SelectList((from x in db.Accounts.Where(x => x.companyID == companyID).ToList()
                                                     select new
                                                     {
                                                         ID = x.accountID,
                                                         Data = x.bankName + " (" + String.Format("************{0}", decrypt.Base64Decode(x.cardNo).Trim().Substring(12, 4)) + " )"
                                                     }), "ID", "Data");

            SelectList getvendors = new SelectList((from x in db.Vendors.Where(x => x.companyID == companyID).ToList()
                                                    select new
                                                    {
                                                        ID = x.vendorID,
                                                        Data = x.name + " (" + x.bankNo + " )"
                                                    }), "ID", "Data");
            SelectList getcategories = new SelectList((from x in db.ExpenseCategories.Where(x => x.companyID == companyID).ToList()
                                                       select new
                                                       {
                                                           ID = x.expenseID,
                                                           Data = x.name
                                                       }), "ID", "Data");


            var dict = new Dictionary<string, string>
                {
                    { "Unpaid", "Unpaid" },
                    { "Paid", "Paid" },
                    { "Overdue", "Overdue" }
                };
            SelectList setstatus =  new SelectList(dict, "Key", "Value");
            ViewBag.accounts = getaccounts;
            ViewBag.vendors = getvendors;
            ViewBag.status = setstatus;
            ViewBag.categories = getcategories;

           
                var user = db.UsersInCompanies.Where(x => x.companyID == companyID).SingleOrDefault();
                Payment payment = new Payment
                {
                    vendorID = model.vendorID,
                    accountID = model.accountID,
                    expenseID = model.expenseID,
                    total = model.total,
                    status = model.status,
                    description = model.description,
                    date = DateTime.Now
                };
            var row = db.Payments.Add(payment);

            if (Convert.ToInt32(f["chosenAccount"].ToString()) != 0 && f["chosenAccount"].ToString() != null)
                {

                    Transaction transaction = new Transaction();

                    transaction.transactionID = DateTime.Now.ToString("ssmmddHHMMyyyyff");
                    transaction.accountID = Convert.ToInt32(Convert.ToInt32(f["chosenAccount"].ToString()));
                    transaction.status = "Paid";
                    transaction.userID = user.userID;
                    transaction.paymentID = row.paymentID;
                    payment.accountID = transaction.accountID;
                    transaction.date = Convert.ToDateTime(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
                    payment.status = "Paid";


                    db.Transactions.Add(transaction);
                }
               
                var check = db.SaveChanges();
                if (check != 0)
                {
                    return RedirectToAction("","expenses");
                }


            

            return View();
        }

        [HttpGet]
        [CustomAuthorize(Roles = "Super Administrator,Administrator,Manager,Employee")]

        public ActionResult EditPayment(int id)
        {
            Crypto decrypt = new Crypto();
            int companyID = CompanyID();

            SelectList getaccounts = new SelectList((from x in db.Accounts.Where(x => x.companyID == companyID).ToList()
                                                     select new
                                                     {
                                                         ID = x.accountID,
                                                         Data = x.bankName + " (" + String.Format("************{0}", decrypt.Base64Decode(x.cardNo).Trim().Substring(12, 4)) + " )"
                                                     }), "ID", "Data");

            SelectList getvendors = new SelectList((from x in db.Vendors.Where(x => x.companyID == companyID).ToList()
                                                    select new
                                                    {
                                                        ID = x.vendorID,
                                                        Data = x.name + " (" + x.bankNo + " )"
                                                    }), "ID", "Data");
            SelectList getcategories = new SelectList((from x in db.ExpenseCategories.Where(x => x.companyID == companyID).ToList()
                                                       select new
                                                       {
                                                           ID = x.expenseID,
                                                           Data = x.name
                                                       }), "ID", "Data");

            var dict = new Dictionary<string, string>
                {
                    { "Unpaid", "Unpaid" },
                                        { "Paid", "Paid" },

                    { "Overdue", "Overdue" }
                };
            SelectList setstatus = new SelectList(dict, "Key", "Value");
            ViewBag.accounts = getaccounts;
            ViewBag.vendors = getvendors;
            ViewBag.status = setstatus;
            ViewBag.categories = getcategories;
            return View(db.Payments.Where(x => x.paymentID == id && x.ExpenseCategory.companyID == companyID).SingleOrDefault()) ;
        }


        [HttpPost]
        [CustomAuthorize(Roles = "Super Administrator,Administrator,Manager,Employee")]

        public ActionResult EditPayment(int id,Payment model,FormCollection f)
        {
            Crypto decrypt = new Crypto();
            int companyID = CompanyID();
            var row = db.Payments.Find(id);
            SelectList getaccounts = new SelectList((from x in db.Accounts.Where(x => x.companyID == companyID).ToList()
                                                     select new
                                                     {
                                                         ID = x.accountID,
                                                         Data = x.bankName + " (" + String.Format("************{0}", decrypt.Base64Decode(x.cardNo).Trim().Substring(12, 4)) + " )"
                                                     }), "ID", "Data");

            SelectList getvendors = new SelectList((from x in db.Vendors.Where(x => x.companyID == companyID).ToList()
                                                    select new
                                                    {
                                                        ID = x.vendorID,
                                                        Data = x.name + " (" + x.bankNo + " )"
                                                    }), "ID", "Data");
            SelectList getcategories = new SelectList((from x in db.ExpenseCategories.Where(x => x.companyID == companyID).ToList()
                                                       select new
                                                       {
                                                           ID = x.expenseID,
                                                           Data = x.name
                                                       }), "ID", "Data");


            var dict = new Dictionary<string, string>
                {                    
                { "Unpaid", "Unpaid" },
                    { "Paid", "Paid" },
                    { "Overdue", "Overdue" }
                };
            SelectList setstatus = new SelectList(dict, "Key", "Value");
            ViewBag.accounts = getaccounts;
            ViewBag.vendors = getvendors;
            ViewBag.status = setstatus;
            ViewBag.categories = getcategories;

            if (ModelState.IsValid)
            {
                var user = db.UsersInCompanies.Where(x => x.companyID == companyID).SingleOrDefault();

                row.accountID = model.accountID;
                row.vendorID = model.vendorID;
                row.expenseID = model.expenseID;
                row.total = model.total;
                row.status = model.status;
                row.description = model.description;



                if (Convert.ToInt32(f["chosenAccount"].ToString()) != 0 && f["chosenAccount"].ToString() != null)
                {

                    Transaction transaction = new Transaction();

                    transaction.transactionID = DateTime.Now.ToString("ssmmddHHMMyyyyff");
                    transaction.accountID = Convert.ToInt32(Convert.ToInt32(f["chosenAccount"].ToString()));
                    transaction.status = "Paid";
                    transaction.userID = user.userID;
                    transaction.paymentID = row.paymentID;

                    transaction.date = Convert.ToDateTime(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
                    row.status = "Paid";

                    db.Transactions.Add(transaction);
                }
               
                    
                var check = db.SaveChanges();
                if (check != 0)
                {
                    return RedirectToAction("", "expenses");
                }


            }

            return View();
        }
        [CustomAuthorize(Roles = "Super Administrator,Administrator,Manager,Employee")]

        public ActionResult ViewPayment(int id)
        {
            return View(db.Payments.Where(x => x.paymentID == id).SingleOrDefault());
        }

        [HttpPost]
        [CustomAuthorize(Roles = "Super Administrator,Administrator,Manager,Employee")]

        public ActionResult DeletePayment(int id)
        {
            var row = db.Payments.Where(x => x.paymentID == id && x.status != "Paid").SingleOrDefault();
            db.Payments.Remove(row);
            var check = db.SaveChanges();
            if (check != 0)
            {
                return RedirectToAction("","expenses");
            }
            return View();
        }


    }


}

    
