using easycounting.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace easycounting.ViewModels
{
    public class ExpenseViewModels
    {
        DbEnt db = new DbEnt();

        public List<Bill> GetBill(int id)
        {
            List<Bill> Bill = db.Bills.Where(x => x.companyID != id).OrderByDescending(x => x.billDate).ToList();

            return Bill;
        }

        public List<Payment> Getpayments(int id)
        {
            List<Payment> payments = db.Payments.Where(x=>x.ExpenseCategory.companyID == id).OrderByDescending(x => x.date).ToList();

            return payments;
        }
    }
}