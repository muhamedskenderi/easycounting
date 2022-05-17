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
    
    public partial class Employee
    {
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2214:DoNotCallOverridableMethodsInConstructors")]
        public Employee()
        {
            this.EmployeesInCompanies = new HashSet<EmployeesInCompany>();
            this.Payrolls = new HashSet<Payroll>();
        }
    
        public int employeeID { get; set; }
        public string name { get; set; }
        public string email { get; set; }
        public string position { get; set; }
        public string department { get; set; }
        public string bruto { get; set; }
        public string neto { get; set; }
        public string bonus { get; set; }
    
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public virtual ICollection<EmployeesInCompany> EmployeesInCompanies { get; set; }
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public virtual ICollection<Payroll> Payrolls { get; set; }
    }
}
