﻿//------------------------------------------------------------------------------
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
    using System.Data.Entity;
    using System.Data.Entity.Infrastructure;
    using System.Data.Entity.Core.Objects;
    using System.Linq;
    
    public partial class SamContext : DbContext
    {
        public SamContext()
            : base("name=SamContext")
        {
        }
    
        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            throw new UnintentionalCodeFirstException();
        }
    
        public virtual DbSet<Acero> Acero { get; set; }
        public virtual DbSet<Camion> Camion { get; set; }
        public virtual DbSet<Chofer> Chofer { get; set; }
        public virtual DbSet<Cliente> Cliente { get; set; }
        public virtual DbSet<Colada> Colada { get; set; }
        public virtual DbSet<Color> Color { get; set; }
        public virtual DbSet<Contacto> Contacto { get; set; }
        public virtual DbSet<Corte> Corte { get; set; }
        public virtual DbSet<CorteDetalle> CorteDetalle { get; set; }
        public virtual DbSet<Diametro> Diametro { get; set; }
        public virtual DbSet<Entidad> Entidad { get; set; }
        public virtual DbSet<EstatusOrden> EstatusOrden { get; set; }
        public virtual DbSet<ExtensionDocumento> ExtensionDocumento { get; set; }
        public virtual DbSet<Fabricante> Fabricante { get; set; }
        public virtual DbSet<FamiliaAcero> FamiliaAcero { get; set; }
        public virtual DbSet<FamiliaItemCode> FamiliaItemCode { get; set; }
        public virtual DbSet<FamiliaMaterial> FamiliaMaterial { get; set; }
        public virtual DbSet<FolioAvisoLlegada> FolioAvisoLlegada { get; set; }
        public virtual DbSet<FolioLlegada> FolioLlegada { get; set; }
        public virtual DbSet<FolioPackingList> FolioPackingList { get; set; }
        public virtual DbSet<Incidencia> Incidencia { get; set; }
        public virtual DbSet<ItemCode> ItemCode { get; set; }
        public virtual DbSet<Maquina> Maquina { get; set; }
        public virtual DbSet<MaterialSpool> MaterialSpool { get; set; }
        public virtual DbSet<MenuContextual> MenuContextual { get; set; }
        public virtual DbSet<MenuGeneral> MenuGeneral { get; set; }
        public virtual DbSet<Notificacion> Notificacion { get; set; }
        public virtual DbSet<NumeroUnico> NumeroUnico { get; set; }
        public virtual DbSet<NumeroUnicoCorte> NumeroUnicoCorte { get; set; }
        public virtual DbSet<NumeroUnicoInventario> NumeroUnicoInventario { get; set; }
        public virtual DbSet<NumeroUnicoMovimiento> NumeroUnicoMovimiento { get; set; }
        public virtual DbSet<NumeroUnicoSegmento> NumeroUnicoSegmento { get; set; }
        public virtual DbSet<OrdenTrabajo> OrdenTrabajo { get; set; }
        public virtual DbSet<OrdenTrabajoSpool> OrdenTrabajoSpool { get; set; }
        public virtual DbSet<PackingList> PackingList { get; set; }
        public virtual DbSet<Pagina> Pagina { get; set; }
        public virtual DbSet<Patio> Patio { get; set; }
        public virtual DbSet<Perfil> Perfil { get; set; }
        public virtual DbSet<PermisoAduana> PermisoAduana { get; set; }
        public virtual DbSet<PinturaNumeroUnico> PinturaNumeroUnico { get; set; }
        public virtual DbSet<Plana> Plana { get; set; }
        public virtual DbSet<Preferencia> Preferencia { get; set; }
        public virtual DbSet<Propiedad> Propiedad { get; set; }
        public virtual DbSet<Proveedor> Proveedor { get; set; }
        public virtual DbSet<Proyecto> Proyecto { get; set; }
        public virtual DbSet<Recepcion> Recepcion { get; set; }
        public virtual DbSet<Rel_Documento_Entidad> Rel_Documento_Entidad { get; set; }
        public virtual DbSet<Rel_FolioAvisoLlegada_Proyecto> Rel_FolioAvisoLlegada_Proyecto { get; set; }
        public virtual DbSet<Rel_Incidencia_Entidad> Rel_Incidencia_Entidad { get; set; }
        public virtual DbSet<Rel_Perfil_MenuContextual> Rel_Perfil_MenuContextual { get; set; }
        public virtual DbSet<Rel_Perfil_MenuGeneral> Rel_Perfil_MenuGeneral { get; set; }
        public virtual DbSet<Rel_Perfil_Propiedad_Pagina> Rel_Perfil_Propiedad_Pagina { get; set; }
        public virtual DbSet<Rel_Usuario_Preferencia> Rel_Usuario_Preferencia { get; set; }
        public virtual DbSet<Rel_Usuario_Proyecto> Rel_Usuario_Proyecto { get; set; }
        public virtual DbSet<Repositorio> Repositorio { get; set; }
        public virtual DbSet<RequisicionNumeroUnico> RequisicionNumeroUnico { get; set; }
        public virtual DbSet<RequisicionNumeroUnicoDetalle> RequisicionNumeroUnicoDetalle { get; set; }
        public virtual DbSet<Sesion> Sesion { get; set; }
        public virtual DbSet<Spool> Spool { get; set; }
        public virtual DbSet<sysdiagrams> sysdiagrams { get; set; }
        public virtual DbSet<Taller> Taller { get; set; }
        public virtual DbSet<TipoCorte> TipoCorte { get; set; }
        public virtual DbSet<TipoDocumento> TipoDocumento { get; set; }
        public virtual DbSet<TipoMaterial> TipoMaterial { get; set; }
        public virtual DbSet<TipoMovimiento> TipoMovimiento { get; set; }
        public virtual DbSet<TipoNotificacion> TipoNotificacion { get; set; }
        public virtual DbSet<Transportista> Transportista { get; set; }
        public virtual DbSet<UbicacionFisica> UbicacionFisica { get; set; }
        public virtual DbSet<Usuario> Usuario { get; set; }
    
        public virtual int sp_alterdiagram(string diagramname, Nullable<int> owner_id, Nullable<int> version, byte[] definition)
        {
            var diagramnameParameter = diagramname != null ?
                new ObjectParameter("diagramname", diagramname) :
                new ObjectParameter("diagramname", typeof(string));
    
            var owner_idParameter = owner_id.HasValue ?
                new ObjectParameter("owner_id", owner_id) :
                new ObjectParameter("owner_id", typeof(int));
    
            var versionParameter = version.HasValue ?
                new ObjectParameter("version", version) :
                new ObjectParameter("version", typeof(int));
    
            var definitionParameter = definition != null ?
                new ObjectParameter("definition", definition) :
                new ObjectParameter("definition", typeof(byte[]));
    
            return ((IObjectContextAdapter)this).ObjectContext.ExecuteFunction("sp_alterdiagram", diagramnameParameter, owner_idParameter, versionParameter, definitionParameter);
        }
    
        public virtual int sp_creatediagram(string diagramname, Nullable<int> owner_id, Nullable<int> version, byte[] definition)
        {
            var diagramnameParameter = diagramname != null ?
                new ObjectParameter("diagramname", diagramname) :
                new ObjectParameter("diagramname", typeof(string));
    
            var owner_idParameter = owner_id.HasValue ?
                new ObjectParameter("owner_id", owner_id) :
                new ObjectParameter("owner_id", typeof(int));
    
            var versionParameter = version.HasValue ?
                new ObjectParameter("version", version) :
                new ObjectParameter("version", typeof(int));
    
            var definitionParameter = definition != null ?
                new ObjectParameter("definition", definition) :
                new ObjectParameter("definition", typeof(byte[]));
    
            return ((IObjectContextAdapter)this).ObjectContext.ExecuteFunction("sp_creatediagram", diagramnameParameter, owner_idParameter, versionParameter, definitionParameter);
        }
    
        public virtual int sp_dropdiagram(string diagramname, Nullable<int> owner_id)
        {
            var diagramnameParameter = diagramname != null ?
                new ObjectParameter("diagramname", diagramname) :
                new ObjectParameter("diagramname", typeof(string));
    
            var owner_idParameter = owner_id.HasValue ?
                new ObjectParameter("owner_id", owner_id) :
                new ObjectParameter("owner_id", typeof(int));
    
            return ((IObjectContextAdapter)this).ObjectContext.ExecuteFunction("sp_dropdiagram", diagramnameParameter, owner_idParameter);
        }
    
        public virtual ObjectResult<sp_helpdiagramdefinition_Result> sp_helpdiagramdefinition(string diagramname, Nullable<int> owner_id)
        {
            var diagramnameParameter = diagramname != null ?
                new ObjectParameter("diagramname", diagramname) :
                new ObjectParameter("diagramname", typeof(string));
    
            var owner_idParameter = owner_id.HasValue ?
                new ObjectParameter("owner_id", owner_id) :
                new ObjectParameter("owner_id", typeof(int));
    
            return ((IObjectContextAdapter)this).ObjectContext.ExecuteFunction<sp_helpdiagramdefinition_Result>("sp_helpdiagramdefinition", diagramnameParameter, owner_idParameter);
        }
    
        public virtual ObjectResult<sp_helpdiagrams_Result> sp_helpdiagrams(string diagramname, Nullable<int> owner_id)
        {
            var diagramnameParameter = diagramname != null ?
                new ObjectParameter("diagramname", diagramname) :
                new ObjectParameter("diagramname", typeof(string));
    
            var owner_idParameter = owner_id.HasValue ?
                new ObjectParameter("owner_id", owner_id) :
                new ObjectParameter("owner_id", typeof(int));
    
            return ((IObjectContextAdapter)this).ObjectContext.ExecuteFunction<sp_helpdiagrams_Result>("sp_helpdiagrams", diagramnameParameter, owner_idParameter);
        }
    
        public virtual int sp_renamediagram(string diagramname, Nullable<int> owner_id, string new_diagramname)
        {
            var diagramnameParameter = diagramname != null ?
                new ObjectParameter("diagramname", diagramname) :
                new ObjectParameter("diagramname", typeof(string));
    
            var owner_idParameter = owner_id.HasValue ?
                new ObjectParameter("owner_id", owner_id) :
                new ObjectParameter("owner_id", typeof(int));
    
            var new_diagramnameParameter = new_diagramname != null ?
                new ObjectParameter("new_diagramname", new_diagramname) :
                new ObjectParameter("new_diagramname", typeof(string));
    
            return ((IObjectContextAdapter)this).ObjectContext.ExecuteFunction("sp_renamediagram", diagramnameParameter, owner_idParameter, new_diagramnameParameter);
        }
    
        public virtual int sp_upgraddiagrams()
        {
            return ((IObjectContextAdapter)this).ObjectContext.ExecuteFunction("sp_upgraddiagrams");
        }
    }
}