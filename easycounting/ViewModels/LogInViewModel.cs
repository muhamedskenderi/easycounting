using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace easycounting.ViewModels
{
    public class LogInViewModel
    {
        public string username { get; set; }

        [DataType(DataType.Password)]
        public string password { get; set; }

       


    }
}