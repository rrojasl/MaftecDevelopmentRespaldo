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
    
    public partial class ItemCodeEquivalente
    {
        public int ItemCodeEquivalenteID { get; set; }
        public int ItemCodeID { get; set; }
        public decimal Diametro1 { get; set; }
        public decimal Diametro2 { get; set; }
        public int ItemEquivalenteID { get; set; }
        public decimal DiametroEquivalente1 { get; set; }
        public decimal DiametroEquivalente2 { get; set; }
        public Nullable<System.Guid> UsuarioModifica { get; set; }
        public Nullable<System.DateTime> FechaModificacion { get; set; }
        public byte[] VersionRegistro { get; set; }
    
        public virtual aspnet_Users aspnet_Users { get; set; }
        public virtual ItemCode ItemCode { get; set; }
        public virtual ItemCode ItemCode1 { get; set; }
    }
}