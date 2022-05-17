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
    public class SalesController : Controller
    {
        DbEnt db = new DbEnt();
        // GET: Sales
        [CustomAuthorize(Roles = "Super Administrator,Administrator,Manager,Employee")]

        public ActionResult Index()
        {
            int id = CompanyID();
            ViewBag.id = id;

            decimal paidRevenues = db.Revenues.Where(x => x.Customer.companyID == id && x.status == "Paid").Count();
            decimal unpaidRevenues = db.Revenues.Where(x => x.Customer.companyID == id && x.status == "Unpaid").Count();
            decimal overdueRevenues = db.Revenues.Where(x => x.Customer.companyID == id && x.status == "Overdue").Count();
            decimal s = paidRevenues + unpaidRevenues + overdueRevenues;

            decimal paidInvoices = db.Invoices.Where(x => x.Customer.companyID == id && x.status == "Paid").Count();
            decimal unpaidInvoices = db.Invoices.Where(x => x.Customer.companyID == id && x.status == "Unpaid").Count();
            decimal overdueInvoices = db.Invoices.Where(x => x.Customer.companyID == id && x.status == "Overdue").Count();
            decimal S = paidInvoices + unpaidInvoices + overdueInvoices;


            if (s != 0)
            {
                ViewBag.paidRevenues = paidRevenues / s * 100;
                ViewBag.overdueRevenues = (overdueRevenues / s) * 100;
                ViewBag.unpaidRevenues = (unpaidRevenues / s) * 100;
            }
            else
            {
                ViewBag.paidRevenues = 0;
                ViewBag.overdueRevenues = 0;
                ViewBag.unpaidRevenues = 0;
            }

            if (S != 0)
            {
                ViewBag.paidInvoices = (paidInvoices / S) * 100;
                ViewBag.overdueInvoices = (overdueInvoices / S) * 100;
                ViewBag.unpaidInvoices = (unpaidInvoices / S) * 100;

            }
            else
            {
                ViewBag.paidInvoices = 0;
                ViewBag.overdueInvoices = 0;
                ViewBag.unpaidInvoices = 0;

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
        public ActionResult AddInvoice()
        {
            int id = CompanyID();
            Crypto decrypt = new Crypto();
            var getitems = db.Items.Where(x => x.companyID == id).ToList();
            SelectList list = new SelectList(getitems, "itemID", "name");
            ViewBag.items = list;

            var getsales = db.SaleCategories.Where(x => x.companyID == id).ToList();
            SelectList list2 = new SelectList(getsales, "saleID", "name");
            ViewBag.sales = list2;

            var getcustomer = db.Customers.Where(x => x.companyID == id).ToList();
            SelectList list3 = new SelectList(getcustomer, "customerID", "name");
            ViewBag.customers = list3;

            var gettaxes = db.Taxes.Where(x => x.companyID == id).ToList();
            SelectList list4 = new SelectList((from x in db.Taxes.Where(x => x.companyID == id).ToList()
                                               select new
                                               {
                                                   ID = x.taxID,
                                                   Data = x.name + " (" + x.value + " %)"
                                               }), "ID", "Data");
            ViewBag.taxes = list4;
          

            return View();
        }



        [HttpPost]
        [CustomAuthorize(Roles = "Super Administrator,Administrator,Manager,Employee")]

        public ActionResult AddInvoice(FormCollection f, Invoice model)
        {
            int companyID = CompanyID();
            Invoice invoice = new Invoice();
            invoice.invoiceNo = f["invoiceNo"].ToString();
            invoice.companyID = companyID;
            invoice.saleID = Convert.ToInt32(f["saleID"].ToString());
            invoice.invoiceDate = Convert.ToDateTime(f["invoiceDate"].ToString());
            invoice.dueDate = Convert.ToDateTime(f["dueDate"].ToString());
            if (f["discountValue"].ToString() != "")
            {
                invoice.discount = Convert.ToInt32(f["discountValue"].ToString());

            }
            else
            {
                invoice.discount = 0;
            }
            if (f["taxID"].ToString() != "0")
            {
                invoice.taxID = Convert.ToInt32(f["taxID"].ToString());

            }
            invoice.total = model.total;
            invoice.status = f["invoiceStatus"].ToString();
            invoice.notes = f["notes"].ToString();
            invoice.customerID = Convert.ToInt32(f["customersList"].ToString());
            var invoiceRow = db.Invoices.Add(invoice);
            int counter = model.ItemsCount;

            if (counter == 0) counter = 1;
            for (var i = 1; i <= counter; i++)
            {
                ItemsInInvoice item = new ItemsInInvoice();

                item.itemID = Convert.ToInt32(f["itemID" + i]);
                item.qt = Convert.ToInt32(f["qt" + i]);
                item.invoiceID = invoiceRow.invoiceID;

                db.ItemsInInvoices.Add(item);
            }

          
            var checker = db.SaveChanges();

            if (checker != 0)
            {
                return RedirectToAction("invoice", "sales", new { id= invoiceRow.invoiceID});
            }



            return View();
        }

        [CustomAuthorize(Roles = "Super Administrator,Administrator,Manager,Employee")]

        public ActionResult Invoice(int id)
        {
            Crypto decrypt = new Crypto();
            int companyid = CompanyID();
            

            var d = db.Companies.Where(x => x.companyID == companyid).Single();
            ViewBag.CompanyName = d.name;

            return View(db.Invoices.Single(x => x.invoiceID == id));

        }



        [CustomAuthorize(Roles = "Super Administrator,Administrator,Manager,Employee")]
        [HttpGet]
        public ActionResult EditInvoice(int id)
        {
            Crypto decrypt = new Crypto();
            var row = db.Invoices.Find(id);
            int companyID = CompanyID();
            ViewBag.companyID = companyID;
            string chosenCustomer = row.customerID.ToString();
            var getitems = db.Items.Where(x => x.companyID == companyID).ToList();
            SelectList list = new SelectList(getitems, "itemID", "name");
            ViewBag.items = list;

            var getsales = db.SaleCategories.Where(x => x.companyID == companyID).ToList();
            SelectList list2 = new SelectList(getsales, "saleID", "name");
            ViewBag.sales = list2;

            var getcustomers = db.Customers.Where(x => x.companyID == companyID).ToList();
            SelectList list3 = new SelectList(getcustomers, "customerID", "name");
            ViewBag.customers = list3;
         
            ViewBag.chosenCustomer = chosenCustomer;

            var gettaxes = db.Taxes.Where(x => x.companyID == companyID).ToList();
            SelectList list4 = new SelectList((from x in db.Taxes.Where(x => x.companyID == id).ToList()
                                               select new
                                               {
                                                   ID = x.taxID,
                                                   Data = x.name + " (" + x.value + " %)"
                                               }), "ID", "Data");
            ViewBag.taxes = list4;


          

            var check = db.Invoices.Where(x => x.invoiceID == id).Single();
            if (check.status == "Paid") return RedirectToAction("invoice", "sales", new { id = check.invoiceID });
            else
                return View(check);

        }


        [HttpPost]
        [CustomAuthorize(Roles = "Super Administrator,Administrator,Manager,Employee")]

        public ActionResult EditInvoice(int id, Invoice model, FormCollection f)
        {
            int companyid = CompanyID();
            var invoice = db.Invoices.Find(id);
            var fieldsToRemove = db.ItemsInInvoices.Where(x => x.invoiceID == id).ToList();

            if (model.fieldChanged)
            {
                foreach (var i in fieldsToRemove)
                {
                    ItemsInInvoice ii = db.ItemsInInvoices.Find(i.id);
                    db.ItemsInInvoices.Remove(ii);
                }
                int counter = Convert.ToInt32(f["ItemsCount"].ToString());
                if (counter == 0) counter = 1;
                for (var i = 1; i <= counter; i++)
                {
                    ItemsInInvoice item = new ItemsInInvoice();

                    item.itemID = Convert.ToInt32(f["itemID" + i]);
                    item.qt = Convert.ToInt32(f["qt" + i]);
                    item.invoiceID = id;
                    db.ItemsInInvoices.Add(item);
                }

            }

            invoice.invoiceNo = f["invoiceNo"].ToString();
            invoice.saleID = Convert.ToInt32(f["saleID"].ToString());
            invoice.invoiceDate = Convert.ToDateTime(f["invoiceDate"].ToString());
            invoice.dueDate = Convert.ToDateTime(f["dueDate"].ToString());
            invoice.total = Convert.ToDouble(Regex.Replace(f["total"].ToString(), @"[^0-9a-zA-Z]+", "."));
            invoice.status = f["invoiceStatus"].ToString();
            invoice.notes = f["notes"].ToString();
            invoice.customerID = Convert.ToInt32(f["customersList"].ToString());
            if (f["discountValue"].ToString() != "")
            {
                invoice.discount = Convert.ToInt32(f["discountValue"].ToString());

            }
            else
            {
                invoice.discount = 0;
            }


            if (Convert.ToInt32(Regex.Replace(f["taxID"].ToString(), @"[^0-9a-zA-Z]+", "")) != 0)
            {
                invoice.taxID = Convert.ToInt32(Regex.Replace(f["taxID"].ToString(), @"[^0-9a-zA-Z]+", ""));

            }

          

            var check = db.SaveChanges();
            if (check != 0)
            {

                return RedirectToAction("invoice", "sales", new { id=invoice.invoiceID});
            }
            return View();
        }

        [CustomAuthorize(Roles = "Super Administrator,Administrator,Manager,Employee")]

        public ActionResult PrintInvoice(int id)
        {
            int companyID = CompanyID();
            var row = db.Invoices.Where(x => x.invoiceID == id && x.companyID == companyID).SingleOrDefault();
            if (row != null)
                return View(row);
            else
                return RedirectToAction("notofund", "error");
        }
        public JsonResult GetCustomerDetails(int id)
        {
            int companyid = CompanyID();

            var row = db.Customers.Where(x => x.customerID == id && x.companyID == companyid).SingleOrDefault();

            List<string> customers = new List<string>();
            customers.Add(row.name);
            customers.Add(row.taxNo);
            customers.Add(row.address);
            customers.Add(row.phone);
            customers.Add(row.email);
            return Json(customers, JsonRequestBehavior.AllowGet);
        }

        public JsonResult GetItemDetails(int id)
        {
            int companyid = CompanyID();

            var row = db.Items.Where(x => x.itemID == id && x.companyID == companyid).SingleOrDefault();

            List<string> items = new List<string>();
            items.Add(row.itemID.ToString());
            items.Add(row.salePrice.ToString());
            items.Add(row.description.ToString());
            items.Add(row.ItemsCategory.name.ToString());

            return Json(items, JsonRequestBehavior.AllowGet);
        }



        public JsonResult GetAllItems(int invoiceid)
        {
            int companyid = CompanyID();
            var row = db.ItemsInInvoices.Where(x => x.invoiceID == invoiceid && x.Invoice.companyID == companyid).ToList();
            var dict = new Dictionary<string, string[]>();
            List<string[]> items = new List<string[]>();
            foreach (var i in row)
            {
                string[] elem = { i.itemID.ToString(), i.Item.salePrice.ToString(), i.Item.description.ToString(), i.Item.ItemsCategory.name.ToString(), i.qt.ToString(), };
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

        public JsonResult GetInvoiceDetails(int id)
        {
            int companyid = CompanyID();

            var row = db.Invoices.Where(x => x.invoiceID == id && x.companyID == companyid).SingleOrDefault();

            List<string> items = new List<string>();
            items.Add(row.Customer.name.ToString());
            items.Add(row.Customer.address.ToString());
            items.Add(row.Customer.taxNo.ToString());
            items.Add(row.Customer.phone.ToString());
            items.Add(row.Customer.email.ToString());
            items.Add(row.status.ToString());
            items.Add(row.saleID.ToString());
            items.Add(row.invoiceNo.ToString());
            items.Add(row.invoiceDate.ToString("yyyy-MM-dd"));
            items.Add(row.dueDate.ToString("yyyy-MM-dd"));
            items.Add(row.total.ToString());
            items.Add(row.notes.ToString());




            return Json(items, JsonRequestBehavior.AllowGet);
        }

        [CustomAuthorize(Roles = "Super Administrator,Administrator,Manager,Employee")]

        public ActionResult DownloadInvoice(int id)
        {
            GenerateInvoice(id);
            return View();

        }
        #region--Generate Invoice PDF--
        public void GenerateInvoice(int id)
        {
            double subtotal = 0;
            int k = 1;
            int tax = 0;
            int companyid = CompanyID();
            var company = db.Companies.Where(x => x.companyID == companyid).SingleOrDefault();
            var invoice = db.Invoices.Where(x => x.invoiceID == id).SingleOrDefault();
            if (invoice.taxID != null)
            {
                var taxFind = db.Taxes.Where(x => x.taxID == invoice.taxID).Single();
                tax = taxFind.value;
            }
            int? discount = invoice.discount;

            var items = db.ItemsInInvoices.Where(x => x.invoiceID == id).ToList();
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

            iTextSharp.text.Image logo = iTextSharp.text.Image.GetInstance(HttpContext.Server.MapPath("~/assets/img/invoice.png"));
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
                PdfPCell nextPostCell1 = new PdfPCell(new Phrase("Invoice To:", bodyFont));
                nextPostCell1.Border = Rectangle.NO_BORDER;
                nested.AddCell(nextPostCell1);
                PdfPCell nextPostCell2 = new PdfPCell(new Phrase(invoice.Customer.name, titleFont));
                nextPostCell2.Border = Rectangle.NO_BORDER;
                nested.AddCell(nextPostCell2);
                PdfPCell nextPostCell3 = new PdfPCell(new Phrase(invoice.Customer.address, bodyFont));
                nextPostCell3.Border = Rectangle.NO_BORDER;
                nested.AddCell(nextPostCell3);
                PdfPCell nextPostCell4 = new PdfPCell(new Phrase(invoice.Customer.email, EmailFont));
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
                PdfPCell nextPostCell1 = new PdfPCell(new Phrase("Invoice No. " + invoice.invoiceNo, titleFontBlue));
                nextPostCell1.Border = Rectangle.NO_BORDER;
                nested.AddCell(nextPostCell1);
                PdfPCell nextPostCell2 = new PdfPCell(new Phrase("Date of Invoice: " + invoice.invoiceDate.ToString("dd/MM/yyyy"), bodyFont));
                nextPostCell2.Border = Rectangle.NO_BORDER;
                nested.AddCell(nextPostCell2);
                PdfPCell nextPostCell3 = new PdfPCell(new Phrase("Due Date: " + invoice.dueDate.ToString("dd/MM/yyyy"), bodyFont));
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

            PdfPCell totalAmtCell = new PdfPCell(new Phrase(String.Format("{0:0.00}", invoice.total) + "MKD", boldTableFont));
            totalAmtCell.HorizontalAlignment = 1;
            itemTable.AddCell(totalAmtCell);







            PdfPCell cell = new PdfPCell(new Phrase("***NOTES: " + invoice.notes + " ***", bodyFont));
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
            DownloadPDF(PDFData, invoice.invoiceNo);


        }
        #endregion

        #region--Download PDF
        protected void DownloadPDF(System.IO.MemoryStream PDFData, string InvoiceNo)
        {
            // Clear response content & headers
            HttpContext.Response.Clear();
            HttpContext.Response.ClearContent();
            HttpContext.Response.ClearHeaders();
            HttpContext.Response.ContentType = "application/pdf";
            HttpContext.Response.Charset = string.Empty;
            HttpContext.Response.Cache.SetCacheability(System.Web.HttpCacheability.Public);
            HttpContext.Response.AddHeader("Content-Disposition", string.Format("attachment;filename=Invoice-{0}.pdf", InvoiceNo));
            HttpContext.Response.OutputStream.Write(PDFData.GetBuffer(), 0, PDFData.GetBuffer().Length);
            HttpContext.Response.OutputStream.Flush();
            HttpContext.Response.OutputStream.Close();
            HttpContext.Response.End();
        }
        #endregion








        [HttpPost]
        [CustomAuthorize(Roles = "Super Administrator,Administrator,Manager,Employee")]
        public ActionResult AddCategory(FormCollection f, string from, int? id)
        {
            string username = User.Identity.Name;
            int compID = CompanyID();
            var company = db.Users.Where(x => x.username == username).SingleOrDefault();
            if (company != null)
            {

                SaleCategory sales = new SaleCategory
                {
                    name = f["saleName"].ToString(),
                    description = f["saleDescription"].ToString(),
                    companyID = Convert.ToInt32(compID)
                };
                db.SaleCategories.Add(sales);
                var check = db.SaveChanges();
                if (check != 0)
                {
                    if (from == "sales")
                        return RedirectToAction("invoices", "sales");
                    if (from == "settings")
                        return RedirectToAction("sales", "settings");

                }

            }
            return View();
        }



        [HttpPost]
        [CustomAuthorize(Roles = "Super Administrator,Administrator,Manager,Employee")]

        public ActionResult EditCategory(int id, FormCollection f)
        {
            string name = f["saleName"].ToString();
            int compID = CompanyID();
            string description = f["saleDescription"].ToString();
            var sale = db.SaleCategories.Find(id);
            sale.name = name;
            sale.description = description;
            sale.companyID = compID;
            var check = db.SaveChanges();
            if (check != 0)
            {
                return RedirectToAction("sales", "settings");
            }
            return View();
        }

        [HttpPost]
        [CustomAuthorize(Roles = "Super Administrator,Administrator,Manager,Employee")]

        public ActionResult DeleteCategory(int id)
        {
            var row = db.SaleCategories.Find(id);
            db.SaleCategories.Remove(row);
            var check = db.SaveChanges();
            if (check != 0)
            {
                return RedirectToAction("sales", "settings");
            }
            return View();
        }


        [HttpGet]
        [CustomAuthorize(Roles = "Super Administrator,Administrator,Manager,Employee")]

        public ActionResult AddRevenue()
        {
            Crypto decrypt = new Crypto();
            int companyID = CompanyID();

            SelectList getaccounts = new SelectList((from x in db.Accounts.Where(x => x.companyID == companyID).ToList()
                                                     select new
                                                     {
                                                         ID = x.accountID,
                                                         Data = x.bankName + " (" + String.Format("************{0}", decrypt.Base64Decode(x.cardNo).Trim().Substring(12, 4)) + " )"
                                                     }), "ID", "Data");

            SelectList getCustomers = new SelectList((from x in db.Customers.Where(x => x.companyID == companyID).ToList()
                                                    select new
                                                    {
                                                        ID = x.customerID,
                                                        Data = x.name + " (" + x.taxNo + " )"
                                                    }), "ID", "Data");
            SelectList getcategories = new SelectList((from x in db.SaleCategories.Where(x => x.companyID == companyID).ToList()
                                                       select new
                                                       {
                                                           ID = x.saleID,
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
            ViewBag.customers = getCustomers;
            ViewBag.status = setstatus;
            ViewBag.categories = getcategories;
            return View();
        }




        [HttpPost]
        [CustomAuthorize(Roles = "Super Administrator,Administrator,Manager,Employee")]

        public ActionResult AddRevenue(Revenue model, FormCollection f)
        {
            Crypto decrypt = new Crypto();
            int companyID = CompanyID();

            SelectList getaccounts = new SelectList((from x in db.Accounts.Where(x => x.companyID == companyID).ToList()
                                                     select new
                                                     {
                                                         ID = x.accountID,
                                                         Data = x.bankName + " (" + String.Format("************{0}", decrypt.Base64Decode(x.cardNo).Trim().Substring(12, 4)) + " )"
                                                     }), "ID", "Data");

            SelectList getCustomers = new SelectList((from x in db.Customers.Where(x => x.companyID == companyID).ToList()
                                                    select new
                                                    {
                                                        ID = x.customerID,
                                                        Data = x.name + " (" + x.taxNo + " )"
                                                    }), "ID", "Data");
            SelectList getcategories = new SelectList((from x in db.SaleCategories.Where(x => x.companyID == companyID).ToList()
                                                       select new
                                                       {
                                                           ID = x.saleID,
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
            ViewBag.customers = getCustomers;
            ViewBag.status = setstatus;
            ViewBag.categories = getcategories;


            var user = db.UsersInCompanies.Where(x => x.companyID == companyID).SingleOrDefault();
            Revenue revenue = new Revenue
            {
                customerID = model.customerID,
                salesD = model.salesD,
                total = model.total,
                status = model.status,
                description = model.description,
                date = DateTime.Now
            }; 
            db.Revenues.Add(revenue);

        
            var check = db.SaveChanges();
            if (check != 0)
            {
                return RedirectToAction("", "sales");
            }




            return View();
        }

        [HttpGet]
        [CustomAuthorize(Roles = "Super Administrator,Administrator,Manager,Employee")]

        public ActionResult EditRevenue(int id)
        {
            Crypto decrypt = new Crypto();
            int companyID = CompanyID();
            var row = db.Revenues.Find(id);
            string selected = row.status;
            SelectList getaccounts = new SelectList((from x in db.Accounts.Where(x => x.companyID == companyID).ToList()
                                                     select new
                                                     {
                                                         ID = x.accountID,
                                                         Data = x.bankName + " (" + String.Format("************{0}", decrypt.Base64Decode(x.cardNo).Trim().Substring(12, 4)) + " )"
                                                     }), "ID", "Data");

            SelectList getCustomers = new SelectList((from x in db.Customers.Where(x => x.companyID == companyID).ToList()
                                                    select new
                                                    {
                                                        ID = x.customerID,
                                                        Data = x.name + " (" + x.taxNo + " )"
                                                    }), "ID", "Data");
            SelectList getcategories = new SelectList((from x in db.SaleCategories.Where(x => x.companyID == companyID).ToList()
                                                       select new
                                                       {
                                                           ID = x.saleID,
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
            ViewBag.Customers = getCustomers;
            ViewBag.status = setstatus;
            ViewBag.categories = getcategories;
            ViewBag.selected = selected;
           var elem = db.Revenues.Where(x => x.revenueID == id && x.SaleCategory.companyID == companyID).SingleOrDefault();
            if (elem.status == "Paid") return RedirectToAction("", "sales");
            return View(elem);
        }


        [HttpPost]
        [CustomAuthorize(Roles = "Super Administrator,Administrator,Manager,Employee")]

        public ActionResult EditRevenue(int id, Revenue model, FormCollection f)
        {
            Crypto decrypt = new Crypto();
            int companyID = CompanyID();
            var row = db.Revenues.Find(id);
            SelectList getaccounts = new SelectList((from x in db.Accounts.Where(x => x.companyID == companyID).ToList()
                                                     select new
                                                     {
                                                         ID = x.accountID,
                                                         Data = x.bankName + " (" + String.Format("************{0}", decrypt.Base64Decode(x.cardNo).Trim().Substring(12, 4)) + " )"
                                                     }), "ID", "Data");

            SelectList getCustomers = new SelectList((from x in db.Customers.Where(x => x.companyID == companyID).ToList()
                                                    select new
                                                    {
                                                        ID = x.customerID,
                                                        Data = x.name + " (" + x.taxNo + " )"
                                                    }), "ID", "Data");
            SelectList getcategories = new SelectList((from x in db.SaleCategories.Where(x => x.companyID == companyID).ToList()
                                                       select new
                                                       {
                                                           ID = x.saleID,
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
            ViewBag.Customers = getCustomers;
            ViewBag.status = setstatus;
            ViewBag.categories = getcategories;

            if (ModelState.IsValid)
            {
                var user = db.UsersInCompanies.Where(x => x.companyID == companyID).SingleOrDefault();

                row.customerID = model.customerID;
                row.salesD = model.salesD;
                row.total = model.total;
                row.status = f["status"].ToString();
                row.description = model.description;




                var check = db.SaveChanges();
                if (check != 0)
                {
                    return RedirectToAction("", "sales");
                }


            }

            return View();
        }

        [CustomAuthorize(Roles = "Super Administrator,Administrator,Manager,Employee")]

        public ActionResult ViewRevenue(int id)
        {
            int companyID = CompanyID();
            return View(db.Revenues.Where(x => x.revenueID == id && x.SaleCategory.companyID == companyID).SingleOrDefault());
        }

        [HttpGet]
        [CustomAuthorize(Roles = "Super Administrator,Administrator,Manager,Employee")]
        public ActionResult AllRevenues(int? page)
        {
            int id = CompanyID();


            var items = db.Revenues.Where(x => x.Customer.companyID == id).ToList().OrderByDescending(x => x.date);
            ViewBag.OnePageOfUsers = items;

            if (items != null)
                return View(items.ToPagedList(page ?? 1, 10));
            else
                return View();

        }


        [HttpPost]
        [CustomAuthorize(Roles = "Super Administrator,Administrator,Manager,Employee")]

        public ActionResult DeleteRevenue(int id)
        {
            int companyID = CompanyID();
            var row = db.Revenues.Where(x => x.revenueID == id && x.status != "Paid" && x.Customer.companyID == companyID).SingleOrDefault();
            db.Revenues.Remove(row);
            var check = db.SaveChanges();
            if (check != 0)
            {
                return RedirectToAction("", "sales");
            }
            return View();
        }

        [HttpPost]
        [CustomAuthorize(Roles = "Super Administrator,Administrator,Manager,Employee")]

        public ActionResult DeleteInvoice(int id)
        {
            var i = db.Invoices.Find(id);
            db.Invoices.Remove(i);
            var check = db.SaveChanges();
            if (check != 0)
            {
                return RedirectToAction("", "sales");
            }

            return View();
        }

        [HttpGet]
        [CustomAuthorize(Roles = "Super Administrator,Administrator,Manager,Employee")]
        public ActionResult AllInvoices(int? page)
        {
            int id = CompanyID();


            var items = db.Invoices.Where(x => x.Customer.companyID == id).ToList().OrderByDescending(x => x.invoiceID);
            ViewBag.OnePageOfUsers = items;

            if (items != null)
                return View(items.ToPagedList(page ?? 1, 10));
            else
                return View();

        }




    }
}