using easycounting.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace easycounting
{
    public class Calculations
    {
        DbEnt db = new DbEnt();
        public double? VendorBalance(int id,int vendorid)
        {
            var bills = db.Bills.Where(x => x.companyID == id && x.status != "Paid" && x.vendorID == vendorid).ToList();
            var payments = db.Payments.Where(x => x.Vendor.companyID == id && x.status != "Paid" && x.vendorID == vendorid).ToList();

            double? paymentSum = 0; double billSum = 0;
            foreach (var i in bills)
            {
                billSum += billSum + i.total;

            }
            foreach (var k in payments)
            {
                paymentSum += paymentSum + k.total;

            }

            if (paymentSum == null)
            {
                return billSum;
            }
            else
            {
                return paymentSum + billSum;
            }
       

        }

        public double? CustomerBalance(int id, int customerid)
        {
            var invoices = db.Invoices.Where(x => x.companyID == id && x.status != "Paid" && x.customerID == customerid).ToList();
            var revenues = db.Revenues.Where(x => x.Customer.companyID == id && x.status != "Paid" && x.customerID == customerid).ToList();

            double? revenueSum = 0; double invoiceSum = 0;
            foreach (var i in invoices)
            {
                invoiceSum += invoiceSum + i.total;

            }
            foreach (var k in revenues)
            {
                revenueSum += revenueSum + k.total;

            }

            if (revenueSum == null)
            {
                return invoiceSum;
            }
            else
            {
                return revenueSum + invoiceSum;
            }


        }

        public List<string> InvoicesStats(int companyid)
        {
            int totalPaid, totalUnpaid, totalOverdue;
            double p, u, o;
            p = u = o = 0;
            totalPaid = totalOverdue = totalUnpaid = 0;
            List<string> items = new List<string>();
            
                var paid = db.Invoices.Where(x => x.invoiceDate.Year == DateTime.Now.Year && x.invoiceDate.Month == DateTime.Now.Month && x.companyID == companyid && x.status == "Paid").OrderBy(x => x.invoiceDate).ToList();
                var unpaid = db.Invoices.Where(x => x.invoiceDate.Year == DateTime.Now.Year && x.invoiceDate.Month == DateTime.Now.Month && x.companyID == companyid && x.status == "Unpaid").OrderBy(x => x.invoiceDate).ToList();
                var overdue = db.Invoices.Where(x => x.invoiceDate.Year == DateTime.Now.Year && x.invoiceDate.Month == DateTime.Now.Month && x.companyID == companyid && x.status == "Overdue").OrderBy(x => x.invoiceDate).ToList();

                 totalPaid = paid.Count();
                 totalUnpaid = unpaid.Count();
                 totalOverdue = overdue.Count();

               
                foreach (var i in paid) p = p + i.total;
                foreach (var i in unpaid) u = u + i.total;
                foreach (var i in overdue) o = o + i.total;

                items.Add(totalPaid.ToString());
                items.Add(totalUnpaid.ToString());
                items.Add(totalOverdue.ToString());
                
                items.Add(p.ToString());
                items.Add(u.ToString());
                items.Add(o.ToString());



            
         



            return items;
        }


        public double? GetIncomes(int companyid, int month)
        {
            if (month <= DateTime.Now.Year)
            {
                var invoices = db.Invoices.Where(x => x.invoiceDate.Month == month && x.invoiceDate.Year == DateTime.Now.Year && x.companyID == companyid && x.status == "Paid").ToList();
                var revenues = db.Revenues.Where(x => x.date.Month == month && x.date.Year == DateTime.Now.Year && x.Customer.companyID == companyid && x.status == "Paid").ToList();

                double? incomes = 0;
                foreach (var i in invoices)
                {
                    incomes = incomes + i.total;
                }
                foreach (var i in revenues)
                {
                    incomes = incomes + i.total;
                }


                return incomes;
            }
            else
            {
                return 0;
            }

            
        }

        public double? GetExpenses(int companyid, int month)
        {

            if (month <= DateTime.Now.Year)
            {
                var bills = db.Bills.Where(x => x.billDate.Month == month && x.billDate.Year == DateTime.Now.Year && x.companyID == companyid && x.status == "Paid").ToList();
                var payments = db.Payments.Where(x => x.date.Month == month && x.date.Year == DateTime.Now.Year && x.Vendor.companyID == companyid && x.status == "Paid").ToList();

                double? expenses = 0;


                foreach (var i in bills)
                {
                    expenses = expenses + i.total;
                }
                foreach (var i in payments)
                {
                    expenses = expenses + i.total;
                }

                return expenses;

            }
            else
            {
                return 0;
            }




        }

        public double Earnings(int companyID)
        {
            var invoices = db.Invoices.Where(x => x.invoiceDate.Year == DateTime.Now.Year && x.companyID == companyID && x.status == "Paid").ToList();
            var revenues = db.Revenues.Where(x => x.date.Year == DateTime.Now.Year && x.Customer.companyID == companyID && x.status == "Paid").ToList();
            double incomes = 0;
            foreach (var i in invoices)
            {
                incomes = incomes + i.total;
            }
            foreach (var i in revenues)
            {
                incomes = incomes + i.total;
            }


            return incomes;

        }

        public double? Expenses(int companyID)
        {
            var bills = db.Bills.Where(x =>  x.billDate.Year == DateTime.Now.Year && x.companyID == companyID && x.status == "Paid").ToList();
            var payments = db.Payments.Where(x => x.date.Year == DateTime.Now.Year && x.Vendor.companyID == companyID && x.status == "Paid").ToList();

            double? expenses = 0;


            foreach (var i in bills)
            {
                expenses = expenses + i.total;
            }
            foreach (var i in payments)
            {
                expenses = expenses + i.total;
            }

            return expenses;


        }


        public double? ExpensesDue(int companyID)
        {
            var bills = db.Bills.Where(x => x.billDate.Year == DateTime.Now.Year && x.companyID == companyID && x.status != "Paid").ToList();
            var payments = db.Payments.Where(x => x.date.Year == DateTime.Now.Year && x.Vendor.companyID == companyID && x.status != "Paid").ToList();

            double? expenses = 0;


            foreach (var i in bills)
            {
                expenses = expenses + i.total;
            }
            foreach (var i in payments)
            {
                expenses = expenses + i.total;
            }

            return expenses;


        }

        public double IncomesDue(int companyID)
        {
            var invoices = db.Invoices.Where(x => x.invoiceDate.Year == DateTime.Now.Year && x.companyID == companyID && x.status != "Paid").ToList();
            var revenues = db.Revenues.Where(x => x.date.Year == DateTime.Now.Year && x.Customer.companyID == companyID && x.status != "Paid").ToList();
            double incomes = 0;
            foreach (var i in invoices)
            {
                incomes = incomes + i.total;
            }
            foreach (var i in revenues)
            {
                incomes = incomes + i.total;
            }


            return incomes;

        }

        public int CountVendors(int companyID)
        {
            return db.Vendors.Where(x => x.companyID == companyID).Count();
        }

        public int CountCustomers(int companyID)
        {
            return db.Customers.Where(x => x.companyID == companyID).Count();
        }


        public double? GetProfit(double? incomes, double? expenses)
        {


            return incomes - expenses;
        }

        public double? CalculateActive(int id, string from, string to)
        {
            if (from != null && to != null)
            {
                
                DateTime f = Convert.ToDateTime(from);
                DateTime t = Convert.ToDateTime(to);

                var row = db.Actives.Where(x => x.companyID == id && x.datePerfomerd >= f && x.datePerfomerd <= t).ToList();

                double? s = 0;
                foreach (var i in row)
                {
                    if (i.amortizationID != null)
                    {
                        s = s + (i.amount - i.Amortization.amount);
                    }
                    else
                    {
                        s = s + i.amount;
                    }
                }

                return s;
            }
            else

            {
                var row = db.Actives.Where(x => x.companyID == id).ToList();

                double? s = 0;
                foreach (var i in row)
                {
                    if (i.amortizationID != null)
                    {
                        s = s + (i.amount - i.Amortization.amount);
                    }
                    else
                    {
                        s = s + i.amount;
                    }
                }
                return s;
            }
            

          
        }


        public double CalculatePassive(int id, string from, string to)
        {
            if (from != null && to != null)
            {
                DateTime f = Convert.ToDateTime(from);
                DateTime t = Convert.ToDateTime(to);
                var row = db.Passives.Where(x => x.companyID == id && x.datePerformed >= f && x.datePerformed <= t).ToList();

                double s = 0;
                foreach (var i in row)
                {


                    s = s + i.amount;

                }

                return s;
            }
            else
            {
                var row = db.Passives.Where(x => x.companyID == id).ToList();

                double s = 0;
                foreach (var i in row)
                {


                    s = s + i.amount;

                }

                return s;

            }
          
        }




    }
}