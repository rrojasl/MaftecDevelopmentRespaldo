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
    
    public partial class Sam3_Rel_FolioCuantificacion_ItemCode
    {
        public int Rel_FolioCuantificacion_ItemCode_ID { get; set; }
        public int FolioCuantificacionID { get; set; }
        public int ItemCodeID { get; set; }
        public bool TieneNumerosUnicos { get; set; }
        public Nullable<System.DateTime> FechaModificacion { get; set; }
        public Nullable<int> UsuarioModificacion { get; set; }
        public bool Activo { get; set; }
    
        public virtual Sam3_FolioCuantificacion Sam3_FolioCuantificacion { get; set; }
        public virtual Sam3_ItemCode Sam3_ItemCode { get; set; }
    }
}