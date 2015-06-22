﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using DatabaseManager.Sam3;
using DatabaseManager.EntidadesPersonalizadas;
using BackEndSAM.Utilities;
using System.Web.Script.Serialization;
using BackEndSAM.Models;
using SecurityManager.Api.Models;
using BackEndSAM.Utilities;

namespace BackEndSAM.DataAcces
{
    public class AvisoLlegadaBd
    {
        private static readonly object _mutex = new object();
        private static AvisoLlegadaBd _instance;

        /// <summary>
        /// constructor privado para implementar el patron Singleton
        /// </summary>
        private AvisoLlegadaBd()
        {
        }

        /// <summary>
        /// crea una instancia de la clase
        /// </summary>
        public static AvisoLlegadaBd Instance
        {
            get
            {
                lock (_mutex)
                {
                    if (_instance == null)
                    {
                        _instance = new AvisoLlegadaBd();
                    }
                }
                return _instance;
            }
        }

        public object GenerarAvisoLlegada(AvisoLlegadaJson avisoJson, Sam3_Usuario usuario)
        {
            try
            {
                using (SamContext ctx = new SamContext())
                {
                    //Buscamos el folio maximo en los avisos de Legada
                    int? nuevoFolio;

                    if (ctx.Sam3_FolioAvisoLlegada.Any())
                    {
                        nuevoFolio = ctx.Sam3_FolioAvisoLlegada.Select(x => x.Consecutivo).Max();
                    }
                    else
                    {
                        nuevoFolio = 0;
                    }

                    if (nuevoFolio > 0)
                    {
                        nuevoFolio = nuevoFolio + 1;
                    }
                    else
                    {
                        nuevoFolio = 1;
                    }

                    DateTime temp = new DateTime();
                    DateTime.TryParse(avisoJson.FechaRecepcion, out temp);

                    //asignamos campos al nueva aviso de llegada
                    Sam3_FolioAvisoLlegada nuevoAvisoLlegada = new Sam3_FolioAvisoLlegada();
                    nuevoAvisoLlegada.Activo = true;
                    nuevoAvisoLlegada.ChoferID = avisoJson.Chofer[0].ChoferID;
                    nuevoAvisoLlegada.Consecutivo = nuevoFolio;
                    nuevoAvisoLlegada.Estatus = "Creado";
                    nuevoAvisoLlegada.EsVirtual = false;
                    nuevoAvisoLlegada.PaseSalidaEnviado = false;
                    nuevoAvisoLlegada.PatioID = avisoJson.Patio[0].PatioID;
                    nuevoAvisoLlegada.ProveedorID = avisoJson.Proveedor[0].ProveedorID;
                    nuevoAvisoLlegada.TransportistaID = avisoJson.Transportista[0].TransportistaID;
                    nuevoAvisoLlegada.FechaRecepcion = Convert.ToDateTime(avisoJson.FechaRecepcion);
                    nuevoAvisoLlegada.UsuarioModificacion = usuario.UsuarioID;
                    nuevoAvisoLlegada.FechaModificacion = DateTime.Now;
                    nuevoAvisoLlegada.VehiculoID = Convert.ToInt32(avisoJson.Tracto.VehiculoID);
                    //Guardamos los cambios
                    ctx.Sam3_FolioAvisoLlegada.Add(nuevoAvisoLlegada);
                    ctx.SaveChanges();

                    int nuevoID = nuevoAvisoLlegada.FolioAvisoLlegadaID;

                    //Guardamos el permisos aduana
                    foreach (PermisoAduanaAV permisoAv in avisoJson.PermisoAduana)
                    {
                        if (permisoAv.NumeroPermiso != null)
                        {
                            Sam3_PermisoAduana nuevoPermiso = new Sam3_PermisoAduana();
                            nuevoPermiso.Activo = true;
                            nuevoPermiso.Estatus = "Creado";
                            nuevoPermiso.FolioAvisoLlegadaID = nuevoID;
                            nuevoPermiso.NumeroPermiso = Convert.ToInt32(permisoAv.NumeroPermiso);
                            nuevoPermiso.PermisoAutorizado = permisoAv.PermisoAutorizado;
                            nuevoPermiso.PermisoTramite = permisoAv.PermisoTramite;
                            nuevoPermiso.UsuarioModificacion = usuario.UsuarioID;
                            nuevoPermiso.FechaModificacion = DateTime.Now;
                            nuevoPermiso.FechaGeneracion = DateTime.Now;
                            ctx.Sam3_PermisoAduana.Add(nuevoPermiso);
                            ctx.SaveChanges();
                            //guardamos en la relacion de Permiso de aduana y documentos
                            foreach (ArchivoAutorizadoAV archivosPermiso in permisoAv.ArchivoAutorizado)
                            {
                                Sam3_Rel_PermisoAduana_Documento permisoDocumento = new Sam3_Rel_PermisoAduana_Documento();
                                permisoDocumento.Activo = true;
                                permisoDocumento.PermisoAduanaID = nuevoPermiso.PermisoAduanaID;
                                permisoDocumento.DocumentoID = archivosPermiso.ArchivoID;
                                permisoDocumento.Extencion = archivosPermiso.Extension;
                                permisoDocumento.Nombre = archivosPermiso.Nombre;
                                permisoDocumento.UsuarioModificacion = usuario.UsuarioID;
                                permisoDocumento.FechaModificacion = DateTime.Now;
                                ctx.Sam3_Rel_PermisoAduana_Documento.Add(permisoDocumento);
                            }
                        }
                    }


                    //guardamos en la relacion entre folios y proyectos
                    foreach (ProyectosAV p in avisoJson.Proyectos)
                    {
                        Sam3_Rel_FolioAvisoLlegada_Proyecto avisoProyecto = new Sam3_Rel_FolioAvisoLlegada_Proyecto();
                        avisoProyecto.Activo = true;
                        avisoProyecto.FolioAvisoLlegadaID = nuevoID;
                        avisoProyecto.ProyectoID = p.ProyectoID;
                        avisoProyecto.UsuarioModificacion = usuario.UsuarioID;
                        avisoProyecto.FechaModificacion = DateTime.Now;

                        ctx.Sam3_Rel_FolioAvisoLlegada_Proyecto.Add(avisoProyecto);
                    }

                    //Guardamos en la relacion de Avisos y planas
                    foreach (PlanaAV plana in avisoJson.Plana)
                    {
                        if (plana.PlanaID > 0)
                        {
                            Sam3_Rel_FolioAvisoLlegada_Vehiculo nuevaPlana = new Sam3_Rel_FolioAvisoLlegada_Vehiculo();
                            nuevaPlana.Activo = true;
                            nuevaPlana.FolioAvisoLlegadaID = nuevoID;
                            nuevaPlana.VehiculoID = plana.PlanaID;
                            nuevaPlana.UsuarioModificacion = usuario.UsuarioID;
                            nuevaPlana.FechaModificacion = DateTime.Now;

                            ctx.Sam3_Rel_FolioAvisoLlegada_Vehiculo.Add(nuevaPlana);
                        }
                    }


                    //Guardamos los archivos del pase de salida
                    //foreach (ArchivosAV archivosAvisollegada in avisoJson.Archivos)
                    //{
                    //    Sam3_Rel_FolioAvisoLlegada_Documento documentoLlegada = new Sam3_Rel_FolioAvisoLlegada_Documento();
                    //    documentoLlegada.Activo = true;
                    //    documentoLlegada.DocumentoID = archivosAvisollegada.ArchivoID;
                    //    documentoLlegada.Extencion = archivosAvisollegada.Extension;
                    //    documentoLlegada.Nombre = archivosAvisollegada.Nombre;
                    //    documentoLlegada.FolioAvisoLlegadaID = nuevoID;
                    //    documentoLlegada.UsuarioModificacion = usuario.UsuarioID;
                    //    documentoLlegada.FechaModificacion = DateTime.Now;
                    //    ctx.Sam3_Rel_FolioAvisoLlegada_Documento.Add(documentoLlegada);
                    //}

                    //guardamos todos los cambios pendientes
                    ctx.SaveChanges();

                    TransactionalInformation result = new TransactionalInformation();
                    result.ReturnMessage.Add("Ok");
                    result.ReturnMessage.Add(nuevoAvisoLlegada.FolioAvisoLlegadaID.ToString());
                    result.ReturnCode = 200;
                    result.ReturnStatus = true;
                    result.IsAuthenicated = true;

                    return result;
                }
            }
            catch (Exception ex)
            {
                TransactionalInformation result = new TransactionalInformation();
                result.ReturnMessage.Add(ex.Message);
                result.ReturnCode = 500;
                result.ReturnStatus = false;
                result.IsAuthenicated = true;

                return result;
            }
        }

        public object ObtenerListadoAvisoLlegada(FiltrosJson filtros)
        {
            try
            {
                using (SamContext ctx = new SamContext())
                {
                    List<int> lstFoliosAvisoLlegada = new List<int>();
                    DateTime fechaInicial = new DateTime();
                    DateTime fechaFinal = new DateTime();
                    DateTime.TryParse(filtros.FechaInicial, out fechaInicial);
                    DateTime.TryParse(filtros.FechaFinal, out fechaFinal);

                    int folioLlegadaID = Convert.ToInt32(filtros.FolioLlegadaID);
                    int folioAvisoLlegadaID = Convert.ToInt32(filtros.FolioAvisoLlegadaID);

                    List<int> proyectos = filtros.Proyectos.Select(x => x.ProyectoID).ToList();
                    List<int> proveedores = filtros.Proveedor.Select(x => x.ProveedorID).ToList();
                    List<int> patios = filtros.Patio.Select(x => x.PatioID).ToList();

                    if (folioLlegadaID > 0)
                    {
                        lstFoliosAvisoLlegada = (from r in ctx.Sam3_FolioAvisoEntrada
                                                 join a in ctx.Sam3_FolioAvisoLlegada on r.FolioAvisoLlegadaID equals a.FolioAvisoLlegadaID
                                                 where r.FolioAvisoEntradaID == folioLlegadaID
                                                 && r.Activo.Value == true
                                                 && (a.FechaRecepcion >= fechaInicial && a.FechaRecepcion <= fechaFinal)
                                                 select a.FolioAvisoLlegadaID).AsParallel().ToList();
                    }
                    else
                    {
                        lstFoliosAvisoLlegada = (from a in ctx.Sam3_FolioAvisoLlegada
                                                 join apr in ctx.Sam3_Rel_FolioAvisoLlegada_Proyecto on
                                                 a.FolioAvisoLlegadaID equals apr.FolioAvisoLlegadaID
                                                 where a.Activo.Value == true
                                                 && patios.Contains(a.PatioID)
                                                 && proveedores.Contains(a.ProveedorID)
                                                 && proyectos.Contains(apr.ProyectoID)
                                                 && (a.FechaRecepcion >= fechaInicial && a.FechaRecepcion <= fechaFinal)
                                                 select a.FolioAvisoLlegadaID).AsParallel().ToList();
                    }

                    List<AvisoLlegadaJson> resultados = new List<AvisoLlegadaJson>();

                    foreach (int folio in lstFoliosAvisoLlegada)
                    {
                        AvisoLlegadaJson aviso = new AvisoLlegadaJson();
                        Sam3_FolioAvisoLlegada registroBd = ctx.Sam3_FolioAvisoLlegada.Where(x => x.FolioAvisoLlegadaID == folio && x.Activo.Value)
                            .AsParallel().SingleOrDefault();
                        //agregamos el listado de archivos del aviso de llegada
                        aviso.Archivos = (from r in ctx.Sam3_Rel_FolioAvisoLlegada_Documento
                                          where r.Activo && r.FolioAvisoLlegadaID == registroBd.FolioAvisoLlegadaID
                                          select new ArchivosAV
                                          {
                                              ArchivoID = r.DocumentoID,
                                              Extension = r.Extencion,
                                              Nombre = r.Nombre,
                                              TipoArchivo = ""
                                          }).ToList();

                        //agregamog los choferes
                        aviso.Chofer = (from r in ctx.Sam3_Chofer
                                        where r.ChoferID == registroBd.ChoferID && r.Activo
                                        select new ChoferAV { ChoferID = r.ChoferID, Nombre = r.Nombre }).AsParallel().ToList();
                        
                        aviso.FechaRecepcion = registroBd.FechaRecepcion.ToString();
                        aviso.FolioAvisoLlegadaID = registroBd.FolioAvisoLlegadaID;

                        TractoAV tractoBd = (from r in ctx.Sam3_Vehiculo
                                             where r.Activo
                                             && r.VehiculoID == registroBd.VehiculoID
                                             select new TractoAV
                                             {
                                                 VehiculoID = r.VehiculoID.ToString(),
                                                 Placas = r.Placas
                                             }).AsParallel().SingleOrDefault();

                        aviso.Tracto = tractoBd != null ? tractoBd : new TractoAV { VehiculoID = "0", Placas = string.Empty };

                        //Obtenemos el listado de archivos de pase de salida
                        List<ArchivosPaseSalida> archivosPaseSalida = (from r in ctx.Sam3_Rel_FolioAvisoLlegada_PaseSalida_Archivo
                                                                       where r.FolioAvisoLlegadaID == registroBd.FolioAvisoLlegadaID
                                                                       && r.Activo
                                                                       select new ArchivosPaseSalida
                                                                       {
                                                                           Nombre = r.Nombre,
                                                                           Extension = r.Extencion,
                                                                           ArchivoID = r.DocumentoID.ToString()
                                                                       }).AsParallel().ToList();
                        aviso.PaseSalida.Add(new PaseSalidaAV
                        {
                            PaseSalidaEnviado = registroBd.PaseSalidaEnviado.Value,
                            Archivos = archivosPaseSalida
                        });

                        //agregamos los patios
                        aviso.Patio = (from r in ctx.Sam3_Patio
                                       where r.PatioID == registroBd.PatioID && r.Activo
                                       select new PatioAV
                                       {
                                           Nombre = r.Nombre,
                                           PatioID = r.PatioID
                                       }).AsParallel().ToList();

                        //agregar permisos de aduana
                        //primero obtenemos los archivos de permisos de aduana
                        List<Sam3_PermisoAduana> lstpermisosAduana = ctx.Sam3_PermisoAduana
                            .Where(x => x.FolioAvisoLlegadaID == registroBd.FolioAvisoLlegadaID && x.Activo)
                            .AsParallel().ToList();
                        foreach (Sam3_PermisoAduana p in lstpermisosAduana)
                        {
                            List<ArchivoAutorizadoAV> lstarchivosPermisoAduana = (from r in ctx.Sam3_Rel_PermisoAduana_Documento
                                                                                  where r.PermisoAduanaID == p.PermisoAduanaID && r.Activo
                                                                                  select new ArchivoAutorizadoAV
                                                                                  {
                                                                                      ArchivoID = r.DocumentoID,
                                                                                      Extension = r.Extencion,
                                                                                      Nombre = r.Nombre
                                                                                  }).AsParallel().ToList();
                            aviso.PermisoAduana.Add(new PermisoAduanaAV
                            {
                                ArchivoAutorizado = lstarchivosPermisoAduana,
                                NumeroPermiso = p.NumeroPermiso.ToString(),
                                PermisoAutorizado = p.PermisoAutorizado.Value,
                                PermisoTramite = p.PermisoTramite.Value,
                                FechaGeneracion = p.FechaGeneracion.ToString(),
                                FechaAutorizacion = p.FechaAutorización.ToString()
                            });
                        }

                        if (aviso.PermisoAduana.Count <= 0)
                        {
                            aviso.PermisoAduana.Add(new PermisoAduanaAV
                            {
                                NumeroPermiso = string.Empty,
                                PermisoAutorizado = false,
                                PermisoTramite = false,
                                FechaAutorizacion = string.Empty,
                                FechaGeneracion = string.Empty
                            });
                        }

                        aviso.Plana = (from r in ctx.Sam3_Vehiculo
                                       join p in ctx.Sam3_Rel_FolioAvisoLlegada_Vehiculo on r.VehiculoID equals p.VehiculoID
                                       where (p.FolioAvisoLlegadaID == registroBd.FolioAvisoLlegadaID)
                                       select new PlanaAV
                                       {
                                           PlanaID = r.VehiculoID
                                       }).AsParallel().ToList();

                        aviso.Proveedor = (from r in ctx.Sam3_Proveedor
                                           where r.ProveedorID == registroBd.ProveedorID && r.Activo
                                           select new ProveedorAV
                                           {
                                               Nombre = r.Nombre,
                                               ProveedorID = r.ProveedorID
                                           }).AsParallel().ToList();

                        aviso.Proyectos = (from r in ctx.Sam3_Rel_FolioAvisoLlegada_Proyecto
                                           where r.FolioAvisoLlegadaID == registroBd.FolioAvisoLlegadaID && r.Activo
                                           select new ProyectosAV
                                           {
                                               ProyectoID = r.ProyectoID
                                           }).AsParallel().ToList();

                        aviso.Transportista = (from r in ctx.Sam3_Transportista
                                               where r.TransportistaID == registroBd.TransportistaID && r.Activo
                                               select new TransportistaAV
                                               {
                                                   Nombre = r.Nombre,
                                                   TransportistaID = r.TransportistaID
                                               }).AsParallel().ToList();

                        resultados.Add(aviso);


                    }

                    return resultados;
                }
            }
            catch (Exception ex)
            {
                TransactionalInformation result = new TransactionalInformation();
                result.ReturnMessage.Add(ex.Message);
                result.ReturnCode = 500;
                result.ReturnStatus = false;
                result.IsAuthenicated = true;

                return result;
            }
        }

        public object ObtenerAvisoLlegadaPorID(int avisoLlegadaID)
        {
            try
            {
                using (SamContext ctx = new SamContext())
                {

                    AvisoLlegadaJson aviso = new AvisoLlegadaJson();
                    Sam3_FolioAvisoLlegada registroBd = ctx.Sam3_FolioAvisoLlegada.Where(x =>
                        x.FolioAvisoLlegadaID == avisoLlegadaID && x.Activo.Value)
                        .AsParallel().SingleOrDefault();
                    //agregamos el listado de archivos del aviso de llegada
                    aviso.Archivos = (from r in ctx.Sam3_Rel_FolioAvisoLlegada_Documento
                                      where r.Activo && r.FolioAvisoLlegadaID == registroBd.FolioAvisoLlegadaID
                                      select new ArchivosAV
                                      {
                                          ArchivoID = r.DocumentoID,
                                          Extension = r.Extencion,
                                          Nombre = r.Nombre,
                                          TipoArchivo = ""
                                      }).ToList();

                    //agregamog los choferes
                    aviso.Chofer = (from r in ctx.Sam3_Chofer
                                    where r.ChoferID == registroBd.ChoferID && r.Activo
                                    select new ChoferAV { ChoferID = r.ChoferID, Nombre = r.Nombre }).AsParallel().ToList();
                    
                    aviso.FechaRecepcion = registroBd.FechaRecepcion.ToString();
                    aviso.FolioAvisoLlegadaID = registroBd.FolioAvisoLlegadaID;

                    TractoAV tractoBd = (from r in ctx.Sam3_Vehiculo
                                         where r.Activo
                                         && r.VehiculoID == registroBd.VehiculoID
                                         select new TractoAV
                                         {
                                             VehiculoID = r.VehiculoID.ToString(),
                                             Placas = r.Placas
                                         }).AsParallel().SingleOrDefault();

                    aviso.Tracto = tractoBd != null ? tractoBd : new TractoAV { VehiculoID = "0", Placas = string.Empty };

                    //Obtenemos el listado de archivos de pase de salida
                    List<ArchivosPaseSalida> archivosPaseSalida = (from r in ctx.Sam3_Rel_FolioAvisoLlegada_PaseSalida_Archivo
                                                                   where r.FolioAvisoLlegadaID == registroBd.FolioAvisoLlegadaID
                                                                   && r.Activo
                                                                   select new ArchivosPaseSalida
                                                                   {
                                                                       Nombre = r.Nombre,
                                                                       Extension = r.Extencion,
                                                                       ArchivoID = r.DocumentoID.ToString()
                                                                   }).AsParallel().ToList();
                    aviso.PaseSalida.Add(new PaseSalidaAV
                    {
                        PaseSalidaEnviado = registroBd.PaseSalidaEnviado.Value,
                        Archivos = archivosPaseSalida
                    });

                    //agregamos los patios
                    aviso.Patio = (from r in ctx.Sam3_Patio
                                   where r.PatioID == registroBd.PatioID && r.Activo
                                   select new PatioAV
                                   {
                                       Nombre = r.Nombre,
                                       PatioID = r.PatioID
                                   }).AsParallel().ToList();

                    //agregar permisos de aduana
                    //primero obtenemos los archivos de permisos de aduana
                    List<Sam3_PermisoAduana> lstpermisosAduana = ctx.Sam3_PermisoAduana
                        .Where(x => x.FolioAvisoLlegadaID == registroBd.FolioAvisoLlegadaID && x.Activo)
                        .AsParallel().ToList();

                    foreach (Sam3_PermisoAduana p in lstpermisosAduana)
                    {
                        List<ArchivoAutorizadoAV> lstarchivosPermisoAduana = (from r in ctx.Sam3_Rel_PermisoAduana_Documento
                                                                              where r.PermisoAduanaID == p.PermisoAduanaID && r.Activo
                                                                              select new ArchivoAutorizadoAV
                                                                              {
                                                                                  ArchivoID = r.DocumentoID,
                                                                                  Extension = r.Extencion,
                                                                                  Nombre = r.Nombre
                                                                              }).AsParallel().ToList();
                        aviso.PermisoAduana.Add(new PermisoAduanaAV
                        {
                            NumeroPermiso = p.NumeroPermiso.ToString(),
                            PermisoAutorizado = p.PermisoAutorizado.Value,
                            PermisoTramite = p.PermisoTramite.Value,
                            FechaAutorizacion = p.FechaAutorización.ToString(),
                            FechaGeneracion = p.FechaGeneracion.ToString()
                        });
                    }

                    if (aviso.PermisoAduana.Count <= 0)
                    {
                        aviso.PermisoAduana.Add(new PermisoAduanaAV
                        {
                            NumeroPermiso = string.Empty,
                            PermisoAutorizado = false,
                            PermisoTramite = false,
                            FechaAutorizacion = string.Empty,
                            FechaGeneracion = string.Empty
                        });
                    }

                    aviso.Plana = (from r in ctx.Sam3_Vehiculo
                                   join p in ctx.Sam3_Rel_FolioAvisoLlegada_Vehiculo on r.VehiculoID equals p.VehiculoID
                                   where (p.FolioAvisoLlegadaID == registroBd.FolioAvisoLlegadaID)
                                   select new PlanaAV
                                   {
                                       PlanaID = r.VehiculoID
                                   }).AsParallel().ToList();

                    aviso.Proveedor = (from r in ctx.Sam3_Proveedor
                                       where r.ProveedorID == registroBd.ProveedorID && r.Activo
                                       select new ProveedorAV
                                       {
                                           Nombre = r.Nombre,
                                           ProveedorID = r.ProveedorID
                                       }).AsParallel().ToList();

                    aviso.Proyectos = (from r in ctx.Sam3_Rel_FolioAvisoLlegada_Proyecto
                                       where r.FolioAvisoLlegadaID == registroBd.FolioAvisoLlegadaID && r.Activo
                                       select new ProyectosAV
                                       {
                                           ProyectoID = r.ProyectoID
                                       }).AsParallel().ToList();

                    aviso.Transportista = (from r in ctx.Sam3_Transportista
                                           where r.TransportistaID == registroBd.TransportistaID && r.Activo
                                           select new TransportistaAV
                                           {
                                               Nombre = r.Nombre,
                                               TransportistaID = r.TransportistaID
                                           }).AsParallel().ToList();

                    JavaScriptSerializer serializer = new JavaScriptSerializer();
                    string json = serializer.Serialize(aviso);

                    return aviso;
                }
            }
            catch (Exception ex)
            {
                TransactionalInformation result = new TransactionalInformation();
                result.ReturnMessage.Add(ex.Message);
                result.ReturnCode = 500;
                result.ReturnStatus = false;
                result.IsAuthenicated = true;

                return result;
            }
        }

        public object EliminarAvisoLlegada(int avisoLlegadaID, Sam3_Usuario usuario)
        {
            try
            {
                using (SamContext ctx = new SamContext())
                {
                    Sam3_FolioAvisoLlegada aviso = ctx.Sam3_FolioAvisoLlegada.Where(x => x.FolioAvisoLlegadaID == avisoLlegadaID)
                        .AsParallel().SingleOrDefault();

                    aviso.Activo = false;
                    aviso.UsuarioModificacion = usuario.UsuarioID;
                    aviso.FechaModificacion = DateTime.Now;

                    ctx.SaveChanges();

                    TransactionalInformation result = new TransactionalInformation();
                    result.ReturnMessage.Add("Ok");
                    result.ReturnCode = 200;
                    result.ReturnStatus = false;
                    result.IsAuthenicated = true;

                    return result;
                }
            }
            catch (Exception ex)
            {
                TransactionalInformation result = new TransactionalInformation();
                result.ReturnMessage.Add(ex.Message);
                result.ReturnCode = 500;
                result.ReturnStatus = false;
                result.IsAuthenicated = true;

                return result;
            }
        }

        public object ActualizarAvisoLlegada(AvisoLlegadaJson cambios, Sam3_Usuario usuario)
        {
            try
            {
                using (SamContext ctx = new SamContext())
                {
                    Sam3_FolioAvisoLlegada avisoBd = ctx.Sam3_FolioAvisoLlegada.Where(x => x.FolioAvisoLlegadaID == cambios.FolioAvisoLlegadaID)
                        .AsParallel().SingleOrDefault();
                    if (avisoBd != null)
                    {
                        //modificamos los parametros generales
                        avisoBd.Activo = true;
                        avisoBd.ChoferID = cambios.Chofer[0].ChoferID;
                        avisoBd.FechaModificacion = DateTime.Now;
                        avisoBd.FechaRecepcion = Convert.ToDateTime(cambios.FechaRecepcion);
                        avisoBd.PatioID = cambios.Patio[0].PatioID;
                        avisoBd.ProveedorID = cambios.Proveedor[0].ProveedorID;
                        avisoBd.TransportistaID = cambios.Transportista[0].TransportistaID;
                        avisoBd.UsuarioModificacion = usuario.UsuarioID;
                        avisoBd.VehiculoID = Convert.ToInt32(cambios.Tracto.VehiculoID);


                        //Actualizar informacion de las planas
                        foreach (PlanaAV plana in cambios.Plana)
                        {
                            if (!avisoBd.Sam3_Rel_FolioAvisoLlegada_Vehiculo
                                .Where(x => x.VehiculoID == plana.PlanaID && x.FolioAvisoLlegadaID == cambios.FolioAvisoLlegadaID).Any()) // varificamos si existe la plana
                            {
                                //agregamos una nuevo registro a la relacion de aviso y planas
                                Sam3_Rel_FolioAvisoLlegada_Vehiculo nuevoRegistro = new Sam3_Rel_FolioAvisoLlegada_Vehiculo();
                                nuevoRegistro.Activo = true;
                                nuevoRegistro.FechaModificacion = DateTime.Now;
                                nuevoRegistro.FolioAvisoLlegadaID = avisoBd.FolioAvisoLlegadaID;
                                nuevoRegistro.VehiculoID = plana.PlanaID;
                                nuevoRegistro.UsuarioModificacion = usuario.UsuarioID;

                                ctx.Sam3_Rel_FolioAvisoLlegada_Vehiculo.Add(nuevoRegistro);
                            }
                        }

                        //actualizar la información de los documentos de Aviso de llegada
                        foreach (ArchivosAV archivo in cambios.Archivos)
                        {
                            //verificamos si ya existe el archivo actual
                            if (!ctx.Sam3_Rel_FolioAvisoLlegada_Documento
                                .Where(x => x.DocumentoID == archivo.ArchivoID && x.FolioAvisoLlegadaID == avisoBd.FolioAvisoLlegadaID).Any())
                            {
                                //si el archivo no existe, agregamos uno nuevo
                                Sam3_Rel_FolioAvisoLlegada_Documento nuenoDoc = new Sam3_Rel_FolioAvisoLlegada_Documento();
                                nuenoDoc.Activo = true;
                                nuenoDoc.DocumentoID = archivo.ArchivoID;
                                nuenoDoc.Extencion = archivo.Extension;
                                nuenoDoc.FechaModificacion = DateTime.Now;
                                nuenoDoc.FolioAvisoLlegadaID = avisoBd.FolioAvisoLlegadaID;
                                nuenoDoc.Nombre = archivo.Nombre;
                                nuenoDoc.UsuarioModificacion = usuario.UsuarioID;

                                ctx.Sam3_Rel_FolioAvisoLlegada_Documento.Add(nuenoDoc);
                            }
                        }

                        foreach (ProyectosAV proyecto in cambios.Proyectos)
                        {
                            //verificamos si existe el registro
                            if (!ctx.Sam3_Rel_FolioAvisoLlegada_Proyecto.Where(x => x.ProyectoID == proyecto.ProyectoID
                                && x.FolioAvisoLlegadaID == cambios.FolioAvisoLlegadaID).Any())
                            {
                                Sam3_Rel_FolioAvisoLlegada_Proyecto nuevoProyecto = new Sam3_Rel_FolioAvisoLlegada_Proyecto();
                                nuevoProyecto.Activo = true;
                                nuevoProyecto.FechaModificacion = DateTime.Now;
                                nuevoProyecto.FolioAvisoLlegadaID = cambios.FolioAvisoLlegadaID;
                                nuevoProyecto.ProyectoID = proyecto.ProyectoID;
                                nuevoProyecto.UsuarioModificacion = usuario.UsuarioID;

                                ctx.Sam3_Rel_FolioAvisoLlegada_Proyecto.Add(nuevoProyecto);
                            }
                        }

                        ctx.SaveChanges();

                    }

                    TransactionalInformation result = new TransactionalInformation();
                    result.ReturnMessage.Add("Ok");
                    result.ReturnCode = 200;
                    result.ReturnStatus = false;
                    result.IsAuthenicated = true;

                    return result;
                }
            }
            catch (Exception ex)
            {
                TransactionalInformation result = new TransactionalInformation();
                result.ReturnMessage.Add(ex.Message);
                result.ReturnCode = 500;
                result.ReturnStatus = false;
                result.IsAuthenicated = true;

                return result;
            }
        }

        public object ObtenerListadoFoliosParaFiltro()
        {
            try
            {
                using (SamContext ctx = new SamContext())
                {
                    List<ListaCombos> lstFolios = (from r in ctx.Sam3_FolioAvisoLlegada
                                                   where r.Activo.Value
                                                   select new ListaCombos
                                                  {
                                                      id = r.FolioAvisoLlegadaID.ToString(),
                                                      value = r.Consecutivo.ToString()
                                                  }).AsParallel().ToList();

                    return lstFolios;
                }
            }
            catch (Exception ex)
            {
                TransactionalInformation result = new TransactionalInformation();
                result.ReturnMessage.Add(ex.Message);
                result.ReturnCode = 500;
                result.ReturnStatus = false;
                result.IsAuthenicated = true;

                return result;
            }
        }


    }//Fin Clase
}