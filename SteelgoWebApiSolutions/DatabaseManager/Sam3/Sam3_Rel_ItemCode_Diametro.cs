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
    
    public partial class Sam3_Rel_ItemCode_Diametro
    {
        public Sam3_Rel_ItemCode_Diametro()
        {
            this.Sam3_Rel_ItemCode_ItemCodeSteelgo = new HashSet<Sam3_Rel_ItemCode_ItemCodeSteelgo>();
            this.Sam3_Rel_Bulto_ItemCode = new HashSet<Sam3_Rel_Bulto_ItemCode>();
            this.Sam3_Rel_FolioCuantificacion_ItemCode = new HashSet<Sam3_Rel_FolioCuantificacion_ItemCode>();
            this.Sam3_Rel_OrdenRecepcion_ItemCode = new HashSet<Sam3_Rel_OrdenRecepcion_ItemCode>();
        }
    
        public int Rel_ItemCode_Diametro_ID { get; set; }
        public int ItemCodeID { get; set; }
        public int Diametro1ID { get; set; }
        public int Diametro2ID { get; set; }
        public bool Activo { get; set; }
        public Nullable<System.DateTime> FechaModificacion { get; set; }
        public Nullable<int> UsuarioModificacion { get; set; }
    
        public virtual Sam3_Diametro Sam3_Diametro { get; set; }
        public virtual Sam3_Diametro Sam3_Diametro1 { get; set; }
        public virtual Sam3_ItemCode Sam3_ItemCode { get; set; }
        public virtual ICollection<Sam3_Rel_ItemCode_ItemCodeSteelgo> Sam3_Rel_ItemCode_ItemCodeSteelgo { get; set; }
        public virtual ICollection<Sam3_Rel_Bulto_ItemCode> Sam3_Rel_Bulto_ItemCode { get; set; }
        public virtual ICollection<Sam3_Rel_FolioCuantificacion_ItemCode> Sam3_Rel_FolioCuantificacion_ItemCode { get; set; }
        public virtual ICollection<Sam3_Rel_OrdenRecepcion_ItemCode> Sam3_Rel_OrdenRecepcion_ItemCode { get; set; }
    }
}
