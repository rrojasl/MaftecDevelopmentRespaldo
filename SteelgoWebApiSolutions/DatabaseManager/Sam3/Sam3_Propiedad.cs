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
    
    public partial class Sam3_Propiedad
    {
        public Sam3_Propiedad()
        {
            this.Sam3_Rel_Perfil_Propiedad_Pagina = new HashSet<Sam3_Rel_Perfil_Propiedad_Pagina>();
        }
    
        public int PropiedadID { get; set; }
        public Nullable<int> EntidadID { get; set; }
        public string Nombre { get; set; }
        public Nullable<bool> Activo { get; set; }
        public Nullable<int> UsuarioModificacion { get; set; }
        public Nullable<System.DateTime> FechaModificacion { get; set; }
    
        public virtual ICollection<Sam3_Rel_Perfil_Propiedad_Pagina> Sam3_Rel_Perfil_Propiedad_Pagina { get; set; }
    }
}
