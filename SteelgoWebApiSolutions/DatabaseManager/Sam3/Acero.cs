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
    
    public partial class Acero
    {
        public Acero()
        {
            this.Colada = new HashSet<Colada>();
        }
    
        public int AceroID { get; set; }
        public int FamiliaAceroID { get; set; }
        public string Nomenclatura { get; set; }
        public bool VerificadoPorCalidad { get; set; }
    
        public virtual FamiliaAcero FamiliaAcero { get; set; }
        public virtual ICollection<Colada> Colada { get; set; }
    }
}
