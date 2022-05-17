using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace easycounting.ViewModels
{
    public class RegisterViewModel
    {
        public string companyName { get; set; }

        public string taxNo { get; set; }

        public string address { get; set; }

        public string phone { get; set; }

        public string username { get; set; }

        [DataType(DataType.Password)]
        public string password { get; set; }
        public string email { get; set; }
        [DataType(DataType.Password)]
        public string confirmedPassword { get; set; }


    }
}