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
    
    public partial class TipoDocumento
    {
        public int TipoDocumentoID { get; set; }
        public string Nombre { get; set; }
        public int EntidadID { get; set; }
        public bool Activo { get; set; }
    
        public virtual Entidad Entidad { get; set; }
        public virtual Rel_Documento_Entidad Rel_Documento_Entidad { get; set; }
    }
}