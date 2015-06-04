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
    
    public partial class Sam3_OrdenTrabajo
    {
        public Sam3_OrdenTrabajo()
        {
            this.Sam3_NumeroUnicoCorte = new HashSet<Sam3_NumeroUnicoCorte>();
            this.Sam3_OrdenTrabajoSpool = new HashSet<Sam3_OrdenTrabajoSpool>();
        }
    
        public int OrdenTrabajoID { get; set; }
        public int EstatusOrdenID { get; set; }
        public int ProyectoID { get; set; }
        public int TallerID { get; set; }
        public string NumeroOrden { get; set; }
        public System.DateTime FechaOrden { get; set; }
        public bool EsAsignado { get; set; }
        public Nullable<int> VersionOrden { get; set; }
        public bool Activo { get; set; }
    
        public virtual Sam3_EstatusOrden Sam3_EstatusOrden { get; set; }
        public virtual ICollection<Sam3_NumeroUnicoCorte> Sam3_NumeroUnicoCorte { get; set; }
        public virtual Sam3_Proyecto Sam3_Proyecto { get; set; }
        public virtual Sam3_Taller Sam3_Taller { get; set; }
        public virtual ICollection<Sam3_OrdenTrabajoSpool> Sam3_OrdenTrabajoSpool { get; set; }
    }
}