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
    
    public partial class Sam3_Colada
    {
        public Sam3_Colada()
        {
            this.Sam3_NumeroUnico = new HashSet<Sam3_NumeroUnico>();
        }
    
        public int ColadaID { get; set; }
        public Nullable<int> FabricanteID { get; set; }
        public int AceroID { get; set; }
        public int ProyectoID { get; set; }
        public string NumeroColada { get; set; }
        public string NumeroCertificado { get; set; }
        public bool HoldCalidad { get; set; }
        public bool Activo { get; set; }
        public Nullable<int> UsuarioModificacion { get; set; }
        public Nullable<System.DateTime> FechaModificacion { get; set; }
    
        public virtual Sam3_Acero Sam3_Acero { get; set; }
        public virtual Sam3_Fabricante Sam3_Fabricante { get; set; }
        public virtual Sam3_Proyecto Sam3_Proyecto { get; set; }
        public virtual ICollection<Sam3_NumeroUnico> Sam3_NumeroUnico { get; set; }
    }
}
