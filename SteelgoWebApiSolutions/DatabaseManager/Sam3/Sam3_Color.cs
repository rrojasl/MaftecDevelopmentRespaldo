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
    
    public partial class Sam3_Color
    {
        public Sam3_Color()
        {
            this.Sam3_Proyecto = new HashSet<Sam3_Proyecto>();
        }
    
        public int ColorID { get; set; }
        public string Nombre { get; set; }
        public string NombreIngles { get; set; }
        public string CodigoHexadecimal { get; set; }
        public bool Activo { get; set; }
        public Nullable<int> UsuarioModificacion { get; set; }
        public Nullable<System.DateTime> FechaModificacion { get; set; }
    
        public virtual ICollection<Sam3_Proyecto> Sam3_Proyecto { get; set; }
    }
}
