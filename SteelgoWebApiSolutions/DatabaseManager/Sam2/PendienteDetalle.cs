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
    
    public partial class PendienteDetalle
    {
        public int PendienteDetalleID { get; set; }
        public int PendienteID { get; set; }
        public int CategoriaPendienteID { get; set; }
        public bool EsAlta { get; set; }
        public System.Guid Responsable { get; set; }
        public string Estatus { get; set; }
        public string Observaciones { get; set; }
        public Nullable<System.Guid> UsuarioModifica { get; set; }
        public Nullable<System.DateTime> FechaModificacion { get; set; }
        public byte[] VersionRegistro { get; set; }
    
        public virtual aspnet_Users aspnet_Users { get; set; }
        public virtual CategoriaPendiente CategoriaPendiente { get; set; }
        public virtual Pendiente Pendiente { get; set; }
        public virtual Usuario Usuario { get; set; }
    }
}