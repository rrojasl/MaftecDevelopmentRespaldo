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
    
    public partial class Sam3_Camion
    {
        public Sam3_Camion()
        {
            this.Sam3_PackingList = new HashSet<Sam3_PackingList>();
            this.Sam3_Plana = new HashSet<Sam3_Plana>();
        }
    
        public int CamionID { get; set; }
        public int TransportistaID { get; set; }
        public int ChoferID { get; set; }
        public string Placas { get; set; }
        public string TarjetaCirulacion { get; set; }
        public string PolizaSeguro { get; set; }
        public bool Activo { get; set; }
        public Nullable<int> UsuarioModificacion { get; set; }
        public Nullable<System.DateTime> FechaModificacion { get; set; }
    
        public virtual Sam3_Chofer Sam3_Chofer { get; set; }
        public virtual ICollection<Sam3_PackingList> Sam3_PackingList { get; set; }
        public virtual Sam3_Transportista Sam3_Transportista { get; set; }
        public virtual ICollection<Sam3_Plana> Sam3_Plana { get; set; }
    }
}
