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
    
    public partial class Rel_Perfil_Propiedad_Pagina
    {
        public int PerfilID { get; set; }
        public int PropiedadID { get; set; }
        public int PaginaID { get; set; }
        public bool PermisoEdicion { get; set; }
        public bool PermisoLectura { get; set; }
        public bool Requerido { get; set; }
        public int EntidadID { get; set; }
        public bool Activo { get; set; }
    
        public virtual Entidad Entidad { get; set; }
        public virtual Perfil Perfil { get; set; }
        public virtual Propiedad Propiedad { get; set; }
    }
}
