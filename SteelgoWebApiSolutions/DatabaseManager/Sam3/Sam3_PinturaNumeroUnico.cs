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
    
    public partial class Sam3_PinturaNumeroUnico
    {
        public int PinturaNumeroUnicoID { get; set; }
        public int ProyectoID { get; set; }
        public int NumeroUnicoID { get; set; }
        public int RequisicionNumeroUnicoDetalleID { get; set; }
        public Nullable<System.DateTime> FechaPrimarios { get; set; }
        public string ReportePrimarios { get; set; }
        public Nullable<System.DateTime> FechaIntermedio { get; set; }
        public string ReporteIntermedio { get; set; }
        public bool Liberado { get; set; }
        public bool Activo { get; set; }
        public Nullable<int> UsuarioModificacion { get; set; }
        public Nullable<System.DateTime> FechaModificacion { get; set; }
    
        public virtual Sam3_NumeroUnico Sam3_NumeroUnico { get; set; }
        public virtual Sam3_Proyecto Sam3_Proyecto { get; set; }
        public virtual Sam3_RequisicionNumeroUnicoDetalle Sam3_RequisicionNumeroUnicoDetalle { get; set; }
    }
}
