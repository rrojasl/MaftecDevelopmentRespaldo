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
    
    public partial class Sam3_MenuContextual
    {
        public Sam3_MenuContextual()
        {
            this.Sam3_Rel_Perfil_MenuContextual = new HashSet<Sam3_Rel_Perfil_MenuContextual>();
        }
    
        public int MenuID { get; set; }
        public string Texto { get; set; }
        public string Liga { get; set; }
        public bool Activo { get; set; }
        public Nullable<int> UsuarioModificacion { get; set; }
        public Nullable<System.DateTime> FechaModificacion { get; set; }
    
        public virtual ICollection<Sam3_Rel_Perfil_MenuContextual> Sam3_Rel_Perfil_MenuContextual { get; set; }
    }
}
