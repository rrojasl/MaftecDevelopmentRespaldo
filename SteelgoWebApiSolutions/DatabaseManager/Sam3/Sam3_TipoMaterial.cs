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
    
    public partial class Sam3_TipoMaterial
    {
        public Sam3_TipoMaterial()
        {
            this.Sam3_ItemCode = new HashSet<Sam3_ItemCode>();
        }
    
        public int TipoMaterialID { get; set; }
        public string Nombre { get; set; }
        public string NombreIngles { get; set; }
        public bool Activo { get; set; }
        public Nullable<int> UsuarioModificacion { get; set; }
        public Nullable<System.DateTime> FechaModificacion { get; set; }
    
        public virtual ICollection<Sam3_ItemCode> Sam3_ItemCode { get; set; }
    }
}
