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
    
        public virtual DbSet<Sam3_Acero> Sam3_Acero { get; set; }
        public virtual DbSet<Sam3_Chofer> Sam3_Chofer { get; set; }
        public virtual DbSet<Sam3_Cliente> Sam3_Cliente { get; set; }
        public virtual DbSet<Sam3_Colada> Sam3_Colada { get; set; }
        public virtual DbSet<Sam3_Color> Sam3_Color { get; set; }
        public virtual DbSet<Sam3_Contacto> Sam3_Contacto { get; set; }
        public virtual DbSet<Sam3_Corte> Sam3_Corte { get; set; }
        public virtual DbSet<Sam3_CorteDetalle> Sam3_CorteDetalle { get; set; }
        public virtual DbSet<Sam3_Diametro> Sam3_Diametro { get; set; }
        public virtual DbSet<Sam3_Entidad> Sam3_Entidad { get; set; }
        public virtual DbSet<Sam3_EstatusOrden> Sam3_EstatusOrden { get; set; }
        public virtual DbSet<Sam3_ExtensionDocumento> Sam3_ExtensionDocumento { get; set; }
        public virtual DbSet<Sam3_Fabricante> Sam3_Fabricante { get; set; }
        public virtual DbSet<Sam3_FamiliaAcero> Sam3_FamiliaAcero { get; set; }
        public virtual DbSet<Sam3_FamiliaItemCode> Sam3_FamiliaItemCode { get; set; }
        public virtual DbSet<Sam3_FamiliaMaterial> Sam3_FamiliaMaterial { get; set; }
        public virtual DbSet<Sam3_FolioAvisoLlegada> Sam3_FolioAvisoLlegada { get; set; }
        public virtual DbSet<Sam3_Incidencia> Sam3_Incidencia { get; set; }
        public virtual DbSet<Sam3_ItemCode> Sam3_ItemCode { get; set; }
        public virtual DbSet<Sam3_Maquina> Sam3_Maquina { get; set; }
        public virtual DbSet<Sam3_MaterialSpool> Sam3_MaterialSpool { get; set; }
        public virtual DbSet<Sam3_MenuContextual> Sam3_MenuContextual { get; set; }
        public virtual DbSet<Sam3_MenuGeneral> Sam3_MenuGeneral { get; set; }
        public virtual DbSet<Sam3_Notificacion> Sam3_Notificacion { get; set; }
        public virtual DbSet<Sam3_NumeroUnico> Sam3_NumeroUnico { get; set; }
        public virtual DbSet<Sam3_NumeroUnicoCorte> Sam3_NumeroUnicoCorte { get; set; }
        public virtual DbSet<Sam3_NumeroUnicoInventario> Sam3_NumeroUnicoInventario { get; set; }
        public virtual DbSet<Sam3_NumeroUnicoMovimiento> Sam3_NumeroUnicoMovimiento { get; set; }
        public virtual DbSet<Sam3_NumeroUnicoSegmento> Sam3_NumeroUnicoSegmento { get; set; }
        public virtual DbSet<Sam3_OrdenTrabajo> Sam3_OrdenTrabajo { get; set; }
        public virtual DbSet<Sam3_OrdenTrabajoSpool> Sam3_OrdenTrabajoSpool { get; set; }
        public virtual DbSet<Sam3_Pagina> Sam3_Pagina { get; set; }
        public virtual DbSet<Sam3_Patio> Sam3_Patio { get; set; }
        public virtual DbSet<Sam3_Perfil> Sam3_Perfil { get; set; }
        public virtual DbSet<Sam3_PermisoAduana> Sam3_PermisoAduana { get; set; }
        public virtual DbSet<Sam3_PinturaNumeroUnico> Sam3_PinturaNumeroUnico { get; set; }
        public virtual DbSet<Sam3_Preferencia> Sam3_Preferencia { get; set; }
        public virtual DbSet<Sam3_Propiedad> Sam3_Propiedad { get; set; }
        public virtual DbSet<Sam3_Proveedor> Sam3_Proveedor { get; set; }
        public virtual DbSet<Sam3_Proyecto> Sam3_Proyecto { get; set; }
        public virtual DbSet<Sam3_Recepcion> Sam3_Recepcion { get; set; }
        public virtual DbSet<Sam3_Rel_Documento_Entidad> Sam3_Rel_Documento_Entidad { get; set; }
        public virtual DbSet<Sam3_Rel_FolioAvisoLlegada_Proyecto> Sam3_Rel_FolioAvisoLlegada_Proyecto { get; set; }
        public virtual DbSet<Sam3_Rel_Incidencia_Entidad> Sam3_Rel_Incidencia_Entidad { get; set; }
        public virtual DbSet<Sam3_Rel_Perfil_MenuContextual> Sam3_Rel_Perfil_MenuContextual { get; set; }
        public virtual DbSet<Sam3_Rel_Perfil_MenuGeneral> Sam3_Rel_Perfil_MenuGeneral { get; set; }
        public virtual DbSet<Sam3_Rel_Perfil_Propiedad_Pagina> Sam3_Rel_Perfil_Propiedad_Pagina { get; set; }
        public virtual DbSet<Sam3_Rel_Usuario_Preferencia> Sam3_Rel_Usuario_Preferencia { get; set; }
        public virtual DbSet<Sam3_Rel_Usuario_Proyecto> Sam3_Rel_Usuario_Proyecto { get; set; }
        public virtual DbSet<Sam3_Repositorio> Sam3_Repositorio { get; set; }
        public virtual DbSet<Sam3_RequisicionNumeroUnico> Sam3_RequisicionNumeroUnico { get; set; }
        public virtual DbSet<Sam3_RequisicionNumeroUnicoDetalle> Sam3_RequisicionNumeroUnicoDetalle { get; set; }
        public virtual DbSet<Sam3_Sesion> Sam3_Sesion { get; set; }
        public virtual DbSet<Sam3_Spool> Sam3_Spool { get; set; }
        public virtual DbSet<Sam3_Taller> Sam3_Taller { get; set; }
        public virtual DbSet<Sam3_TipoCorte> Sam3_TipoCorte { get; set; }
        public virtual DbSet<Sam3_TipoDocumento> Sam3_TipoDocumento { get; set; }
        public virtual DbSet<Sam3_TipoMaterial> Sam3_TipoMaterial { get; set; }
        public virtual DbSet<Sam3_TipoMovimiento> Sam3_TipoMovimiento { get; set; }
        public virtual DbSet<Sam3_TipoNotificacion> Sam3_TipoNotificacion { get; set; }
        public virtual DbSet<Sam3_Transportista> Sam3_Transportista { get; set; }
        public virtual DbSet<Sam3_UbicacionFisica> Sam3_UbicacionFisica { get; set; }
        public virtual DbSet<Sam3_Usuario> Sam3_Usuario { get; set; }
        public virtual DbSet<Sam3_Rel_Perfil_Entidad_Pagina> Sam3_Rel_Perfil_Entidad_Pagina { get; set; }
        public virtual DbSet<Sam3_Rel_TiposDocumentos_ExtencionesDocumentos> Sam3_Rel_TiposDocumentos_ExtencionesDocumentos { get; set; }
        public virtual DbSet<Sam3_Rel_PermisoAduana_Documento> Sam3_Rel_PermisoAduana_Documento { get; set; }
        public virtual DbSet<Sam3_Rel_FolioAvisoLlegada_Documento> Sam3_Rel_FolioAvisoLlegada_Documento { get; set; }
        public virtual DbSet<Sam3_Rel_FolioAvisoLlegada_PaseSalida_Archivo> Sam3_Rel_FolioAvisoLlegada_PaseSalida_Archivo { get; set; }
        public virtual DbSet<Sam3_Rel_FolioAvisoLlegada_Vehiculo> Sam3_Rel_FolioAvisoLlegada_Vehiculo { get; set; }
        public virtual DbSet<Sam3_Rel_Vehiculo_Chofer> Sam3_Rel_Vehiculo_Chofer { get; set; }
        public virtual DbSet<Sam3_Rel_Vehiculo_Transportista> Sam3_Rel_Vehiculo_Transportista { get; set; }
        public virtual DbSet<Sam3_TipoArchivo> Sam3_TipoArchivo { get; set; }
        public virtual DbSet<Sam3_TipoAviso> Sam3_TipoAviso { get; set; }
        public virtual DbSet<Sam3_TipoVehiculo> Sam3_TipoVehiculo { get; set; }
        public virtual DbSet<Sam3_Vehiculo> Sam3_Vehiculo { get; set; }
        public virtual DbSet<Sam3_FolioAvisoEntrada> Sam3_FolioAvisoEntrada { get; set; }
        public virtual DbSet<Sam3_ProyectoConsecutivo> Sam3_ProyectoConsecutivo { get; set; }
        public virtual DbSet<Sam3_Rel_FolioAvisoEntrada_Documento> Sam3_Rel_FolioAvisoEntrada_Documento { get; set; }
        public virtual DbSet<Sam3_UsuariosNotificaciones> Sam3_UsuariosNotificaciones { get; set; }
        public virtual DbSet<Sam3_Bulto> Sam3_Bulto { get; set; }
        public virtual DbSet<Sam3_FolioCuantificacion> Sam3_FolioCuantificacion { get; set; }
        public virtual DbSet<Sam3_Rel_Bulto_ItemCode> Sam3_Rel_Bulto_ItemCode { get; set; }
        public virtual DbSet<Sam3_Rel_FolioCuantificacion_ItemCode> Sam3_Rel_FolioCuantificacion_ItemCode { get; set; }
        public virtual DbSet<Sam3_TipoUso> Sam3_TipoUso { get; set; }
        public virtual DbSet<Sam3_ItemCodeSteelgo> Sam3_ItemCodeSteelgo { get; set; }
        public virtual DbSet<Sam3_Rel_ItemCode_ItemCodeSteelgo> Sam3_Rel_ItemCode_ItemCodeSteelgo { get; set; }
        public virtual DbSet<Sam3_OrdenRecepcion> Sam3_OrdenRecepcion { get; set; }
        public virtual DbSet<Sam3_Rel_OrdenRecepcion_ItemCode> Sam3_Rel_OrdenRecepcion_ItemCode { get; set; }
        public virtual DbSet<Sam3_Rel_FolioAvisoEntrada_OrdenRecepcion> Sam3_Rel_FolioAvisoEntrada_OrdenRecepcion { get; set; }
        public virtual DbSet<Sam3_ProyectoConfiguracion> Sam3_ProyectoConfiguracion { get; set; }
        public virtual DbSet<Sam3_OrdenAlmacenaje> Sam3_OrdenAlmacenaje { get; set; }
        public virtual DbSet<Sam3_Rel_OrdenAlmacenaje_NumeroUnico> Sam3_Rel_OrdenAlmacenaje_NumeroUnico { get; set; }
        public virtual DbSet<Sam3_Rel_Incidencia_NumeroUnico> Sam3_Rel_Incidencia_NumeroUnico { get; set; }
        public virtual DbSet<Sam3_EquivalenciaAcero> Sam3_EquivalenciaAcero { get; set; }
        public virtual DbSet<Sam3_EquivalenciaColada> Sam3_EquivalenciaColada { get; set; }
        public virtual DbSet<Sam3_EquivalenciaDespacho> Sam3_EquivalenciaDespacho { get; set; }
        public virtual DbSet<Sam3_EquivalenciaFabricante> Sam3_EquivalenciaFabricante { get; set; }
        public virtual DbSet<Sam3_EquivalenciaFamiliaAcero> Sam3_EquivalenciaFamiliaAcero { get; set; }
        public virtual DbSet<Sam3_EquivalenciaFamiliaMaterial> Sam3_EquivalenciaFamiliaMaterial { get; set; }
        public virtual DbSet<Sam3_EquivalenciaItemCode> Sam3_EquivalenciaItemCode { get; set; }
        public virtual DbSet<Sam3_EquivalenciaNumeroUnico> Sam3_EquivalenciaNumeroUnico { get; set; }
        public virtual DbSet<Sam3_EquivalenciaPatio> Sam3_EquivalenciaPatio { get; set; }
        public virtual DbSet<Sam3_EquivalenciaProveedor> Sam3_EquivalenciaProveedor { get; set; }
        public virtual DbSet<Sam3_EquivalenciaProyecto> Sam3_EquivalenciaProyecto { get; set; }
        public virtual DbSet<Sam3_ColaCreacionNumerosUnicos> Sam3_ColaCreacionNumerosUnicos { get; set; }
        public virtual DbSet<Sam3_Despacho> Sam3_Despacho { get; set; }
        public virtual DbSet<Sam3_EquivalenciaCorte> Sam3_EquivalenciaCorte { get; set; }
        public virtual DbSet<Sam3_EquivalenciaNumeroUnicoMovimiento> Sam3_EquivalenciaNumeroUnicoMovimiento { get; set; }
        public virtual DbSet<Sam3_Grupo> Sam3_Grupo { get; set; }
        public virtual DbSet<Sam3_ClasificacionIncidencia> Sam3_ClasificacionIncidencia { get; set; }
        public virtual DbSet<Sam3_Rel_Incidencia_ComplementoRecepcion> Sam3_Rel_Incidencia_ComplementoRecepcion { get; set; }
        public virtual DbSet<Sam3_Rel_Incidencia_Corte> Sam3_Rel_Incidencia_Corte { get; set; }
        public virtual DbSet<Sam3_Rel_Incidencia_Despacho> Sam3_Rel_Incidencia_Despacho { get; set; }
        public virtual DbSet<Sam3_Rel_Incidencia_FolioAvisoEntrada> Sam3_Rel_Incidencia_FolioAvisoEntrada { get; set; }
        public virtual DbSet<Sam3_Rel_Incidencia_FolioAvisoLlegada> Sam3_Rel_Incidencia_FolioAvisoLlegada { get; set; }
        public virtual DbSet<Sam3_Rel_Incidencia_FolioCuantificacion> Sam3_Rel_Incidencia_FolioCuantificacion { get; set; }
        public virtual DbSet<Sam3_Rel_Incidencia_ItemCode> Sam3_Rel_Incidencia_ItemCode { get; set; }
        public virtual DbSet<Sam3_Rel_Incidencia_OrdenAlmacenaje> Sam3_Rel_Incidencia_OrdenAlmacenaje { get; set; }
        public virtual DbSet<Sam3_Rel_Incidencia_OrdenRecepcion> Sam3_Rel_Incidencia_OrdenRecepcion { get; set; }
        public virtual DbSet<Sam3_Rel_Incidencia_PaseSalida> Sam3_Rel_Incidencia_PaseSalida { get; set; }
        public virtual DbSet<Sam3_TipoIncidencia> Sam3_TipoIncidencia { get; set; }
        public virtual DbSet<Sam3_Rel_Incidencia_Documento> Sam3_Rel_Incidencia_Documento { get; set; }
        public virtual DbSet<Sam3_Catalogos> Sam3_Catalogos { get; set; }
        public virtual DbSet<Sam3_Rel_Catalogos_Documento> Sam3_Rel_Catalogos_Documento { get; set; }
        public virtual DbSet<Sam3_TipoArchivo_Catalogo> Sam3_TipoArchivo_Catalogo { get; set; }
        public virtual DbSet<Sam3_DeficitMateriales> Sam3_DeficitMateriales { get; set; }
        public virtual DbSet<Sam3_Entrega> Sam3_Entrega { get; set; }
        public virtual DbSet<Sam3_FolioImpresionDocumental> Sam3_FolioImpresionDocumental { get; set; }
        public virtual DbSet<Sam3_FolioPickingTicket> Sam3_FolioPickingTicket { get; set; }
        public virtual DbSet<Sam3_PreDespacho> Sam3_PreDespacho { get; set; }
        public virtual DbSet<Sam3_EquivalenciaDiametro> Sam3_EquivalenciaDiametro { get; set; }
        public virtual DbSet<Sam3_Rel_ItemCode_Diametro> Sam3_Rel_ItemCode_Diametro { get; set; }
        public virtual DbSet<Sam3_Rel_ItemCodeSteelgo_Diametro> Sam3_Rel_ItemCodeSteelgo_Diametro { get; set; }
        public virtual DbSet<Sam3_Rel_Proyecto_Entidad_Configuracion> Sam3_Rel_Proyecto_Entidad_Configuracion { get; set; }
        public virtual DbSet<Sam3_Rel_NumeroUnico_RelFC_RelB> Sam3_Rel_NumeroUnico_RelFC_RelB { get; set; }
        public virtual DbSet<Sam3_Rel_Itemcode_Colada> Sam3_Rel_Itemcode_Colada { get; set; }
        public virtual DbSet<Sam3_CorteSpool> Sam3_CorteSpool { get; set; }
        public virtual DbSet<Sam3_EquivalenciaCorteSpool> Sam3_EquivalenciaCorteSpool { get; set; }
        public virtual DbSet<Sam3_EquivalenciaEstacion> Sam3_EquivalenciaEstacion { get; set; }
        public virtual DbSet<Sam3_EquivalenciaEstatusOrden> Sam3_EquivalenciaEstatusOrden { get; set; }
        public virtual DbSet<Sam3_EquivalenciaFabArea> Sam3_EquivalenciaFabArea { get; set; }
        public virtual DbSet<Sam3_EquivalenciaJuntaSpool> Sam3_EquivalenciaJuntaSpool { get; set; }
        public virtual DbSet<Sam3_EquivalenciaMaterialSpool> Sam3_EquivalenciaMaterialSpool { get; set; }
        public virtual DbSet<Sam3_EquivalenciaODT> Sam3_EquivalenciaODT { get; set; }
        public virtual DbSet<Sam3_EquivalenciaODTJ> Sam3_EquivalenciaODTJ { get; set; }
        public virtual DbSet<Sam3_EquivalenciaODTM> Sam3_EquivalenciaODTM { get; set; }
        public virtual DbSet<Sam3_EquivalenciaODTS> Sam3_EquivalenciaODTS { get; set; }
        public virtual DbSet<Sam3_EquivalenciaSpool> Sam3_EquivalenciaSpool { get; set; }
        public virtual DbSet<Sam3_EquivalenciaTaller> Sam3_EquivalenciaTaller { get; set; }
        public virtual DbSet<Sam3_EquivalenciaTipoCorte> Sam3_EquivalenciaTipoCorte { get; set; }
        public virtual DbSet<Sam3_EquivalenciaTipoJunta> Sam3_EquivalenciaTipoJunta { get; set; }
        public virtual DbSet<Sam3_Estacion> Sam3_Estacion { get; set; }
        public virtual DbSet<Sam3_FabArea> Sam3_FabArea { get; set; }
        public virtual DbSet<Sam3_JuntaSpool> Sam3_JuntaSpool { get; set; }
        public virtual DbSet<Sam3_OrdenTrabajoJunta> Sam3_OrdenTrabajoJunta { get; set; }
        public virtual DbSet<Sam3_OrdenTrabajoMaterial> Sam3_OrdenTrabajoMaterial { get; set; }
        public virtual DbSet<Sam3_TipoJunta> Sam3_TipoJunta { get; set; }
        public virtual DbSet<Sam3_Rel_Proyecto_Correo> Sam3_Rel_Proyecto_Correo { get; set; }
        public virtual DbSet<Sam3_EquivalenciaEspesor> Sam3_EquivalenciaEspesor { get; set; }
        public virtual DbSet<Sam3_Espesor> Sam3_Espesor { get; set; }
        public virtual DbSet<Sam3_Cedula> Sam3_Cedula { get; set; }
        public virtual DbSet<Sam3_EquivalenciaCedula> Sam3_EquivalenciaCedula { get; set; }
        public virtual DbSet<Sam3_CatalogoCedulas> Sam3_CatalogoCedulas { get; set; }
    }
}
