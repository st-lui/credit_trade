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
    
    public partial class leftover
    {
        public int id { get; set; }
        public int warehouse_id { get; set; }
        public int good_id { get; set; }
        public decimal amount { get; set; }
        public Nullable<decimal> expenditure { get; set; }
    
        public virtual good good { get; set; }
        public virtual warehouse warehouse { get; set; }
    }
}
