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
    
    public partial class payment
    {
        public int id { get; set; }
        public int request_row_id { get; set; }
        public decimal amount { get; set; }
    
        public virtual request_rows request_rows { get; set; }
    }
}
