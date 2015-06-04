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
    
    public partial class JuntaSpoolPendiente
    {
        public int JuntaSpoolPendienteID { get; set; }
        public int SpoolPendienteID { get; set; }
        public int TipoJuntaID { get; set; }
        public int FabAreaID { get; set; }
        public string Etiqueta { get; set; }
        public string EtiquetaMaterial1 { get; set; }
        public string EtiquetaMaterial2 { get; set; }
        public string Cedula { get; set; }
        public int FamiliaAceroMaterial1ID { get; set; }
        public Nullable<int> FamiliaAceroMaterial2ID { get; set; }
        public decimal Diametro { get; set; }
        public Nullable<decimal> Espesor { get; set; }
        public Nullable<decimal> KgTeoricos { get; set; }
        public Nullable<decimal> Peqs { get; set; }
        public Nullable<System.Guid> UsuarioModifica { get; set; }
        public Nullable<System.DateTime> FechaModificacion { get; set; }
        public byte[] VersionRegistro { get; set; }
        public bool RequierePwht { get; set; }
    
        public virtual aspnet_Users aspnet_Users { get; set; }
        public virtual FabArea FabArea { get; set; }
        public virtual FamiliaAcero FamiliaAcero { get; set; }
        public virtual FamiliaAcero FamiliaAcero1 { get; set; }
        public virtual SpoolPendiente SpoolPendiente { get; set; }
        public virtual TipoJunta TipoJunta { get; set; }
    }
}