//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated from a template.
//
//     Manual changes to this file may cause unexpected behavior in your application.
//     Manual changes to this file will be overwritten if the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace DatabaseManager.Sam2
{
    using System;
    using System.Collections.Generic;
    
    public partial class PreList
    {
        public PreList()
        {
            this.OrdenTrabajoSpool = new HashSet<OrdenTrabajoSpool>();
        }
    
        public int PrelistID { get; set; }
        public Nullable<int> ProyectoID { get; set; }
        public string NumeroPrelist { get; set; }
        public Nullable<System.DateTime> FechaPrelist { get; set; }
    
        public virtual Proyecto Proyecto { get; set; }
        public virtual ICollection<OrdenTrabajoSpool> OrdenTrabajoSpool { get; set; }
    }
}