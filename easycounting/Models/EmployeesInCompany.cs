//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated from a template.
//
//     Manual changes to this file may cause unexpected behavior in your application.
//     Manual changes to this file will be overwritten if the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace easycounting.Models
{
    using System;
    using System.Collections.Generic;
    
    public partial class EmployeesInCompany
    {
        public int id { get; set; }
        public int employeeID { get; set; }
        public int companyID { get; set; }
    
        public virtual Company Company { get; set; }
        public virtual Employee Employee { get; set; }
    }
}