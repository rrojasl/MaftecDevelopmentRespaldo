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
    
    public partial class Soldador
    {
        public Soldador()
        {
            this.DestajoSoldador = new HashSet<DestajoSoldador>();
            this.JuntaCampoSoldaduraDetalle = new HashSet<JuntaCampoSoldaduraDetalle>();
            this.JuntaReportePndCuadrante = new HashSet<JuntaReportePndCuadrante>();
            this.JuntaReportePndSector = new HashSet<JuntaReportePndSector>();
            this.JuntaSoldaduraDetalle = new HashSet<JuntaSoldaduraDetalle>();
            this.Wpq = new HashSet<Wpq>();
        }
    
        public int SoldadorID { get; set; }
        public int PatioID { get; set; }
        public string Codigo { get; set; }
        public string NumeroEmpleado { get; set; }
        public string Nombre { get; set; }
        public string ApPaterno { get; set; }
        public string ApMaterno { get; set; }
        public bool Activo { get; set; }
        public Nullable<System.Guid> UsuarioModifica { get; set; }
        public Nullable<System.DateTime> FechaModificacion { get; set; }
        public byte[] VersionRegistro { get; set; }
        public Nullable<System.DateTime> FechaVigencia { get; set; }
        public string AreaTrabajo { get; set; }
    
        public virtual aspnet_Users aspnet_Users { get; set; }
        public virtual ICollection<DestajoSoldador> DestajoSoldador { get; set; }
        public virtual ICollection<JuntaCampoSoldaduraDetalle> JuntaCampoSoldaduraDetalle { get; set; }
        public virtual ICollection<JuntaReportePndCuadrante> JuntaReportePndCuadrante { get; set; }
        public virtual ICollection<JuntaReportePndSector> JuntaReportePndSector { get; set; }
        public virtual ICollection<JuntaSoldaduraDetalle> JuntaSoldaduraDetalle { get; set; }
        public virtual Patio Patio { get; set; }
        public virtual ICollection<Wpq> Wpq { get; set; }
    }
}