//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated from a template.
//
//     Manual changes to this file may cause unexpected behavior in your application.
//     Manual changes to this file will be overwritten if the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace DbModel
{
    using System;
    using System.Collections.Generic;
    
    public partial class good
    {
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2214:DoNotCallOverridableMethodsInConstructors")]
        public good()
        {
            this.leftovers = new HashSet<leftover>();
        }
    
        public int id { get; set; }
        public string nom_id { get; set; }
        public string parent_id { get; set; }
        public string name { get; set; }
        public string edizm { get; set; }
        public decimal price { get; set; }
        public string barcode { get; set; }
        public string category { get; set; }
    
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public virtual ICollection<leftover> leftovers { get; set; }
    }
}
