//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated from a template.
//
//     Manual changes to this file may cause unexpected behavior in your application.
//     Manual changes to this file will be overwritten if the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace DatabaseManager.Sam3
{
    using System;
    using System.Collections.Generic;
    
    public partial class MenuContextual
    {
        public int MenuID { get; set; }
        public string Texto { get; set; }
        public string Liga { get; set; }
        public int EntidadID { get; set; }
    
        public virtual Entidad Entidad { get; set; }
    }
}
