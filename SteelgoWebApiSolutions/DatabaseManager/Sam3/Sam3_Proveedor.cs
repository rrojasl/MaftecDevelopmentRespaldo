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
    
    public partial class Sam3_Proveedor
    {
        public Sam3_Proveedor()
        {
            this.Sam3_NumeroUnico = new HashSet<Sam3_NumeroUnico>();
            this.Sam3_FolioAvisoEntrada = new HashSet<Sam3_FolioAvisoEntrada>();
        }
    
        public int ProveedorID { get; set; }
        public int ContactoID { get; set; }
        public string Nombre { get; set; }
        public string Descripcion { get; set; }
        public string Direccion { get; set; }
        public string Telefono { get; set; }
        public bool Activo { get; set; }
        public Nullable<int> UsuarioModificacion { get; set; }
        public Nullable<System.DateTime> FechaModificacion { get; set; }
    
        public virtual Sam3_Contacto Sam3_Contacto { get; set; }
        public virtual ICollection<Sam3_NumeroUnico> Sam3_NumeroUnico { get; set; }
        public virtual ICollection<Sam3_FolioAvisoEntrada> Sam3_FolioAvisoEntrada { get; set; }
    }
}
