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
    
    public partial class Sam3_FamiliaItemCode
    {
        public Sam3_FamiliaItemCode()
        {
            this.Sam3_Recepcion = new HashSet<Sam3_Recepcion>();
        }
    
        public int FamiliaItemCodeID { get; set; }
        public string Nombre { get; set; }
        public bool Activo { get; set; }
    
        public virtual ICollection<Sam3_Recepcion> Sam3_Recepcion { get; set; }
    }
}