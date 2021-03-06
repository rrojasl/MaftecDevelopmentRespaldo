﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using DatabaseManager.Sam3;
using DatabaseManager.Sam2;
using DatabaseManager.EntidadesPersonalizadas;
using BackEndSAM.Utilities;
using System.Web.Script.Serialization;
using BackEndSAM.Models;
using SecurityManager.Api.Models;
using System.Web.Mvc;
using System.Net.Http;
using System.Net;
using System.IO;
using System.Net.Http.Headers;
using System.Configuration;

namespace BackEndSAM.DataAcces
{
    public class ListadoBd
    {
        private static readonly object _mutex = new object();
        private static ListadoBd _instance;

        /// <summary>
        /// constructor privado para implementar el patron Singleton
        /// </summary>
        private ListadoBd()
        {
        }

        /// <summary>
        /// crea una instancia de la clase
        /// </summary>
        public static ListadoBd Instance
        {
            get
            {
                lock (_mutex)
                {
                    if (_instance == null)
                    {
                        _instance = new ListadoBd();
                    }
                }
                return _instance;
            }
        }

        /// <summary>
        /// Devuelve los conteos para el dashboard de avisos de llegada
        /// </summary>
        /// <param name="filtros"></param>
        /// <param name="usuario"></param>
        /// <returns></returns>
        public object ObtenerCantidadesDashboard(FiltrosJson filtros, Sam3_Usuario usuario)
        {
            try
            {
                using (SamContext ctx = new SamContext())
                {
                    //traemos la informacion de los patios y proyectos del usuario

                    CantidadesDashboard result = new CantidadesDashboard();
                    DateTime fechaInicial = new DateTime();
                    DateTime fechaFinal = new DateTime();
                    DateTime.TryParse(filtros.FechaInicial, out fechaInicial);
                    DateTime.TryParse(filtros.FechaFinal, out fechaFinal);

                    if (fechaFinal.ToShortDateString() == "1/1/0001")
                    {
                        fechaFinal = DateTime.Now;
                    }

                    if (fechaInicial.ToShortDateString() == "1/1/0001")
                    {
                        int mes = DateTime.Now.Month != 1 ? DateTime.Now.Month - 1 : 12;
                        int year = DateTime.Now.Month == 1 ? DateTime.Now.Year - 1 : DateTime.Now.Year;
                        fechaInicial = new DateTime(year, mes, DateTime.Now.Day);
                    }

                    int patioID = filtros.PatioID != "" ? Convert.ToInt32(filtros.PatioID) : 0;
                    int clienteID = filtros.ClienteID != "" ? Convert.ToInt32(filtros.ClienteID) : 0;

                    List<Sam3_Rel_Usuario_Proyecto> lst = ctx.Sam3_Rel_Usuario_Proyecto.AsParallel().ToList();


                    List<int> proyectos = lst.Where(x => x.UsuarioID == usuario.UsuarioID).Select(x => x.ProyectoID).AsParallel().ToList();

                    List<int> patios = (from r in ctx.Sam3_Proyecto
                                        join p in ctx.Sam3_Patio on r.PatioID equals p.PatioID
                                        where r.Activo && proyectos.Contains(r.ProyectoID)
                                        select p.PatioID).AsParallel().ToList();


                    List<Sam3_FolioAvisoLlegada> registrosBd = new List<Sam3_FolioAvisoLlegada>();

                    //----------------------------------------------------------------------------------------------------------------------
                    //Creados
                    // Filtrado por fechas, patios y proyectos del usuario

                    registrosBd = (from r in ctx.Sam3_FolioAvisoLlegada
                                   join p in ctx.Sam3_Rel_FolioAvisoLlegada_Proyecto on r.FolioAvisoLlegadaID equals p.FolioAvisoLlegadaID
                                   where r.Activo == true && p.Activo
                                   && patios.Contains(r.PatioID)
                                   && proyectos.Contains(p.ProyectoID)
                                   && r.FechaRecepcion >= fechaInicial && r.FechaRecepcion <= fechaFinal
                                   select r).AsParallel().ToList();

                    registrosBd.AddRange((from r in ctx.Sam3_FolioAvisoLlegada
                                          where r.Activo
                                          && !(from rfp in ctx.Sam3_Rel_FolioAvisoLlegada_Proyecto
                                               where rfp.Activo
                                               select rfp.FolioAvisoLlegadaID).Contains(r.FolioAvisoLlegadaID)
                                               && (r.FechaRecepcion >= fechaInicial && r.FechaRecepcion <= fechaFinal)
                                          select r).AsParallel().Distinct().ToList());


                    if (patioID > 0)
                    {
                        registrosBd = registrosBd.Where(x => x.PatioID == patioID).ToList();
                    }

                    if (clienteID > 0)
                    {
                        int sam3Cliente = (from c in ctx.Sam3_Cliente
                                           where c.Activo && c.Sam2ClienteID == clienteID
                                           select c.ClienteID).AsParallel().SingleOrDefault();
                        registrosBd = registrosBd.Where(x => x.ClienteID == sam3Cliente).ToList();
                    }

                    registrosBd = registrosBd.GroupBy(x => x.FolioAvisoLlegadaID).Select(x => x.First()).ToList();

                    //----------------------------------------------------------------------------------------------------------------------

                    result.Creados = registrosBd.Select(x => x.FolioAvisoLlegadaID).Count();

                    List<Sam3_FolioAvisoLlegada> cuentas = (from r in registrosBd
                                                            join pa in ctx.Sam3_Patio on r.PatioID equals pa.PatioID
                                                            join p in ctx.Sam3_PermisoAduana on r.FolioAvisoLlegadaID equals p.FolioAvisoLlegadaID
                                                            where r.Activo && p.Activo
                                                            && p.PermisoAutorizado == true
                                                            && pa.RequierePermisoAduana
                                                            select r).AsParallel().ToList();
                    cuentas.AddRange((from r in registrosBd
                                      join pa in ctx.Sam3_Patio on r.PatioID equals pa.PatioID
                                      where r.Activo && pa.Activo
                                      && !pa.RequierePermisoAduana
                                      select r).AsParallel().ToList());

                    cuentas = cuentas.GroupBy(x => x.FolioAvisoLlegadaID).Select(x => x.First()).ToList();

                    result.Completos = cuentas.Count;

                    result.SinAutorizacion = (from r in registrosBd
                                              join pa in ctx.Sam3_Patio on r.PatioID equals pa.PatioID
                                              join p in ctx.Sam3_PermisoAduana on r.FolioAvisoLlegadaID equals p.FolioAvisoLlegadaID
                                              where r.Activo == true && p.Activo
                                              && p.PermisoAutorizado == false
                                              && pa.RequierePermisoAduana
                                              select r.FolioAvisoLlegadaID).AsParallel().Count();

                    List<Sam3_FolioAvisoLlegada> temp = (from r in registrosBd
                                                         join pa in ctx.Sam3_Patio on r.PatioID equals pa.PatioID
                                                         where r.Activo == true
                                                         && !(from x in ctx.Sam3_PermisoAduana
                                                              where x.Activo
                                                              select x.FolioAvisoLlegadaID).Contains(r.FolioAvisoLlegadaID)
                                                         && pa.RequierePermisoAduana
                                                         select r).AsParallel().ToList();

                    temp = temp.GroupBy(x => x.FolioAvisoLlegadaID).Select(x => x.First()).ToList();

                    result.SinPermiso = temp.Count();


                    result.PorcentajeSinPermiso = result.Creados > 0 ? (result.SinPermiso * 100) / result.Creados : 0;
                    result.ProcentajeSinAutorizacion = result.Creados > 0 ? (result.SinAutorizacion * 100) / result.Creados : 0;
                    result.PorcentajeCompletos = result.Creados > 0 ? (result.Completos * 100) / result.Creados : 0;

                    return result;
                }
            }
            catch (Exception ex)
            {
                //-----------------Agregar mensaje al Log -----------------------------------------------
                LoggerBd.Instance.EscribirLog(ex);
                //-----------------Agregar mensaje al Log -----------------------------------------------
                TransactionalInformation result = new TransactionalInformation();
                result.ReturnMessage.Add(ex.Message);
                result.ReturnCode = 500;
                result.ReturnStatus = false;
                result.IsAuthenicated = true;

                return result;
            }
        }

        /// <summary>
        /// Devuelve los conteos para el dashboard de avisos de entrada de material
        /// </summary>
        /// <param name="filtros"></param>
        /// <param name="usuario"></param>
        /// <returns></returns>
        public object ObtenerCantidadesDashboardAvisoEntrada(FiltrosJson filtros, Sam3_Usuario usuario)
        {
            try
            {
                using (SamContext ctx = new SamContext())
                {
                    //traemos la informacion de los patios y proyectos del usuario

                    CantidadesDashboardAvisoEntrada result = new CantidadesDashboardAvisoEntrada();
                    DateTime fechaInicial = new DateTime();
                    DateTime fechaFinal = new DateTime();
                    DateTime.TryParse(filtros.FechaInicial, out fechaInicial);
                    DateTime.TryParse(filtros.FechaFinal, out fechaFinal);

                    if (fechaFinal.ToShortDateString() == "1/1/0001")
                    {
                        fechaFinal = DateTime.Now;
                    }

                    if (fechaInicial.ToShortDateString() == "1/1/0001")
                    {
                        int mes = DateTime.Now.Month != 1 ? DateTime.Now.Month - 1 : 12;
                        int year = DateTime.Now.Month == 1 ? DateTime.Now.Year - 1 : DateTime.Now.Year;
                        fechaInicial = new DateTime(year, mes, DateTime.Now.Day);
                    }

                    int patioID = filtros.PatioID != "" ? Convert.ToInt32(filtros.PatioID) : 0;
                    int clienteID = filtros.ClienteID != "" ? Convert.ToInt32(filtros.ClienteID) : 0;

                    //Para aviso de llegada no se usan estos parametros de filtrado, segun el spec.
                    //int folioLlegadaID = filtros.FolioLlegadaID != null ? Convert.ToInt32(filtros.FolioLlegadaID) : 0;
                    //int folioAvisoLlegadaID = filtros.FolioAvisoLlegadaID != null ? Convert.ToInt32(filtros.FolioAvisoLlegadaID) : 0;

                    List<Sam3_Rel_Usuario_Proyecto> lst = ctx.Sam3_Rel_Usuario_Proyecto.AsParallel().ToList();


                    List<int> proyectos = lst.Where(x => x.UsuarioID == usuario.UsuarioID).Select(x => x.ProyectoID).AsParallel().ToList();

                    List<int> patios = (from r in ctx.Sam3_Proyecto
                                        join p in ctx.Sam3_Patio on r.PatioID equals p.PatioID
                                        where r.Activo && proyectos.Contains(r.ProyectoID)
                                        select p.PatioID).AsParallel().ToList();


                    List<Sam3_FolioAvisoEntrada> registrosBd = new List<Sam3_FolioAvisoEntrada>();

                    //Traer todos de acuerdo a las fechas, proyectos y patios del usuario

                    registrosBd = (from r in ctx.Sam3_FolioAvisoEntrada
                                   join p in ctx.Sam3_Rel_FolioAvisoLlegada_Proyecto on r.FolioAvisoLlegadaID equals p.FolioAvisoLlegadaID
                                   join pry in ctx.Sam3_Proyecto on p.ProyectoID equals pry.ProyectoID
                                   join pa in ctx.Sam3_Patio on pry.PatioID equals pa.PatioID
                                   where r.Activo && p.Activo && pry.Activo && pa.Activo
                                   && proyectos.Contains(pry.ProyectoID)
                                   && patios.Contains(pa.PatioID)
                                   && (r.FechaModificacion >= fechaInicial && r.FechaModificacion <= fechaFinal)
                                   select r).AsParallel().ToList();


                    //eliminar duplicados
                    registrosBd = registrosBd.GroupBy(x => x.FolioAvisoEntradaID).Select(x => x.First()).ToList();

                    if (patioID > 0 && registrosBd.Count > 0)
                    {
                        registrosBd = (from r in registrosBd
                                       where r.Activo
                                       && r.PatioID == patioID
                                       select r).AsParallel().ToList();
                    }

                    if (clienteID > 0 && registrosBd.Count > 0)
                    {
                        registrosBd = registrosBd.Where(x => x.ClienteID == clienteID).ToList();
                    }

                    result.TotalCreados = registrosBd.Count();

                    result.SinEstaus = (from fa in ctx.Sam3_FolioAvisoLlegada
                                        where fa.Activo
                                        && !(from fe in ctx.Sam3_FolioAvisoEntrada
                                             where fe.Activo
                                             select fe.FolioAvisoLlegadaID).Contains(fa.FolioAvisoLlegadaID)
                                        && (fa.FechaModificacion >= fechaInicial && fa.FechaModificacion <= fechaFinal)
                                        select fa).AsParallel().Distinct().Count();

                    result.SinOrdenDescarga = (from r in registrosBd
                                               where r.FolioDescarga <= 0
                                               && r.Estatus == "En Patio"
                                               select r).Count();

                    result.SinPaseSalida = (from r in registrosBd
                                            join f in ctx.Sam3_FolioAvisoLlegada on r.FolioAvisoLlegadaID equals f.FolioAvisoLlegadaID
                                            where r.Activo && f.Activo
                                            && (f.PaseSalidaEnviado == false || r.Estatus == "Cierre de Folio Por Devolución")
                                            && r.Estatus != "En Patio"
                                            select r).AsParallel().Count();

                    result.PorcentajeSinDescarga = result.TotalCreados > 0 ? (result.SinOrdenDescarga * 100) / result.TotalCreados : 0;
                    result.PorcentajeSinEstatus = result.TotalCreados > 0 ? (result.SinEstaus * 100) / result.TotalCreados : 0;
                    result.PorcentajeSinPaseSalida = result.TotalCreados > 0 ? (result.SinPaseSalida * 100) / result.TotalCreados : 0;

                    return result;
                }
            }
            catch (Exception ex)
            {
                //-----------------Agregar mensaje al Log -----------------------------------------------
                LoggerBd.Instance.EscribirLog(ex);
                //-----------------Agregar mensaje al Log -----------------------------------------------
                TransactionalInformation result = new TransactionalInformation();
                result.ReturnMessage.Add(ex.Message);
                result.ReturnCode = 500;
                result.ReturnStatus = false;
                result.IsAuthenicated = true;

                return result;
            }
        }

        /// <summary>
        /// Devuelve los conteos para el dashboard de cuantificacion
        /// </summary>
        /// <param name="filtros"></param>
        /// <param name="usuario"></param>
        /// <returns></returns>
        public object ObtenerCantidadesDashboardCuantificacion(FiltrosJson filtros, Sam3_Usuario usuario)
        {
            try
            {
                using (SamContext ctx = new SamContext())
                {
                    CantidadesDashboardCuantificacion result = new CantidadesDashboardCuantificacion();
                    //DateTime fechaInicial = new DateTime();
                    //DateTime fechaFinal = new DateTime();
                    //DateTime.TryParse(filtros.FechaInicial, out fechaInicial);
                    //DateTime.TryParse(filtros.FechaFinal, out fechaFinal);

                    //if (fechaFinal.ToShortDateString() == "1/1/0001")
                    //{
                    //    fechaFinal = DateTime.Now;
                    //}

                    //if (fechaInicial.ToShortDateString() == "1/1/0001")
                    //{
                    //    int mes = DateTime.Now.Month != 1 ? DateTime.Now.Month - 1 : 12;
                    //    int year = DateTime.Now.Month == 1 ? DateTime.Now.Year - 1 : DateTime.Now.Year;
                    //    fechaInicial = new DateTime(year, mes, DateTime.Now.Day);
                    //}

                    //int proyectoID = filtros.ProyectoID != "" ? Convert.ToInt32(filtros.ProyectoID) : 0;
                    //int clienteID = filtros.ClienteID != "" ? Convert.ToInt32(filtros.ClienteID) : 0;
                    //int folioAvisoLlegadaID = filtros.FolioAvisoLlegadaID != null ? Convert.ToInt32(filtros.FolioAvisoLlegadaID) : 0;
                    //int packingListID = filtros.PackingListID != "" ? Convert.ToInt32(filtros.PackingListID) : 0;
                    int tipoMaterialID = filtros.TipoMaterialID != "" ? Convert.ToInt32(filtros.TipoMaterialID) : 0;

                    //List<int> proyectos = ctx.Sam3_Rel_Usuario_Proyecto.Where(x => x.UsuarioID == usuario.UsuarioID).Select(x => x.ProyectoID).AsParallel().ToList();

                    //List<int> patios = (from r in ctx.Sam3_Proyecto
                    //                    join p in ctx.Sam3_Patio on r.PatioID equals p.PatioID
                    //                    where r.Activo && proyectos.Contains(r.ProyectoID)
                    //                    select p.PatioID).AsParallel().ToList();

                    //////Traemos todos los folios que esten dentro del periodo de fechas y que correspondan a los proyectos y patios de los usuarios.
                    //List<Sam3_FolioAvisoEntrada> registros = (from fae in ctx.Sam3_FolioAvisoEntrada
                    //                                          join fal in ctx.Sam3_FolioAvisoLlegada on fae.FolioAvisoLlegadaID equals fal.FolioAvisoLlegadaID
                    //                                          join rfal in ctx.Sam3_Rel_FolioAvisoLlegada_Proyecto on fal.FolioAvisoLlegadaID equals rfal.FolioAvisoLlegadaID
                    //                                          join p in ctx.Sam3_Proyecto on rfal.ProyectoID equals p.ProyectoID
                    //                                          join pa in ctx.Sam3_Patio on p.PatioID equals pa.PatioID
                    //                                          where fae.Activo && fal.Activo && rfal.Activo && p.Activo && pa.Activo
                    //                                          && proyectos.Contains(rfal.ProyectoID)
                    //                                          && patios.Contains(pa.PatioID)
                    //                                          && (fae.FechaCreacion >= fechaInicial && fae.FechaCreacion <= fechaFinal)
                    //                                          && fae.FolioDescarga > 0
                    //                                          select fae).AsParallel().ToList();

                    //if (proyectoID > 0)
                    //{
                    //    registros = (from r in registros
                    //                 join re in ctx.Sam3_Rel_FolioAvisoLlegada_Proyecto on r.FolioAvisoLlegadaID equals re.FolioAvisoLlegadaID
                    //                 where re.ProyectoID == proyectoID
                    //                 select r).AsParallel().ToList();
                    //}

                    //if (clienteID > 0)
                    //{
                    //    int sam3Cliente = (from c in ctx.Sam3_Cliente
                    //                       where c.Activo && c.Sam2ClienteID == clienteID
                    //                       select c.ClienteID).AsParallel().SingleOrDefault();
                    //    registros = registros.Where(x => x.ClienteID == sam3Cliente).ToList();
                    //}

                    //if (folioAvisoLlegadaID > 0)
                    //{
                    //    registros = registros.Where(x => x.FolioAvisoLlegadaID == folioAvisoLlegadaID).ToList();
                    //}

                    //if (packingListID > 0)
                    //{
                    //    registros = (from r in registros
                    //                 join f in ctx.Sam3_FolioCuantificacion on r.FolioAvisoEntradaID equals f.FolioAvisoEntradaID
                    //                 where f.Activo && f.FolioCuantificacionID == packingListID
                    //                 select r).AsParallel().ToList();
                    //}

                    //registros = registros.GroupBy(x => x.FolioAvisoEntradaID).Select(x => x.First()).ToList();

                    result.EntradaPorCuantificar = (int)ListadoMaterialesSinCuantificar(filtros, usuario, true);

                    result.PLPorCuantificar = (int)ListadoPacknglistPorCuantificar(filtros, usuario, true);

                    result.MTLSinICS = (int)ListadoMTLSinICS(filtros, usuario, true);

                    //itemcodes sin orde de recepcion
                    result.MTLSinOrdenRecepcion = (int)OrdenRecepcionBd.Instance.ObtenerListadoGenerarOrdenRecepcion(filtros, usuario, true);

                    result.NUPorRecepcionar = (int)ListadoNUConRecepcionSinComplemento(filtros, usuario, true);

                    //numeros unicos sin orden de almacenaje, que ya cuentan con recepcion y complemento de recepcion
                    if (tipoMaterialID == 3) //todos
                    {
                        result.NUSinOrdenAlmacenaje = (from rnu in ctx.Sam3_Rel_NumeroUnico_RelFC_RelB
                                                       join nu in ctx.Sam3_NumeroUnico on rnu.NumeroUnicoID equals nu.NumeroUnicoID
                                                       where rnu.Activo && nu.Activo
                                                       && !(from ronu in ctx.Sam3_Rel_OrdenAlmacenaje_NumeroUnico
                                                            where ronu.Activo
                                                            select ronu.NumeroUnicoID).Contains(nu.NumeroUnicoID)
                                                       select nu).Distinct().Count();

                    }
                    else
                    {
                        result.NUSinOrdenAlmacenaje = (from rnu in ctx.Sam3_Rel_NumeroUnico_RelFC_RelB
                                                       join nu in ctx.Sam3_NumeroUnico on rnu.NumeroUnicoID equals nu.NumeroUnicoID
                                                       join it in ctx.Sam3_ItemCode on nu.ItemCodeID equals it.ItemCodeID
                                                       where rnu.Activo && nu.Activo && it.Activo
                                                       && !(from ronu in ctx.Sam3_Rel_OrdenAlmacenaje_NumeroUnico
                                                            where ronu.Activo
                                                            select ronu.NumeroUnicoID).Contains(nu.NumeroUnicoID)
                                                       && it.TipoMaterialID == tipoMaterialID
                                                       select nu).Distinct().Count();
                    }


                    result.NUSinAlmacenar = (int)ListadoNUSinAlmacenaje(filtros, usuario, true);


                    result.TipoMaterial = tipoMaterialID.ToString();


                    result.IncidenciasAbiertas = (int)ListadoIncidenciasActivas(filtros, usuario, true);

                    return result;
                }
            }
            catch (Exception ex)
            {
                //-----------------Agregar mensaje al Log -----------------------------------------------
                LoggerBd.Instance.EscribirLog(ex);
                //-----------------Agregar mensaje al Log -----------------------------------------------
                TransactionalInformation result = new TransactionalInformation();
                result.ReturnMessage.Add(ex.Message);
                result.ReturnCode = 500;
                result.ReturnStatus = false;
                result.IsAuthenicated = true;

                return result;
            }
        }

        /// <summary>
        /// Genera el listado para el grid de Entradas por cuantificar
        /// </summary>
        /// <param name="filtros"></param>
        /// <param name="usuario"></param>
        /// <returns></returns>
        public object ListadoMaterialesSinCuantificar(FiltrosJson filtros, Sam3_Usuario usuario, bool conteo = false)
        {
            try
            {
                using (SamContext ctx = new SamContext())
                {
                    Boolean activarFolioConfiguracion = !string.IsNullOrEmpty(ConfigurationManager.AppSettings["ActivarFolioConfiguracion"]) ? (ConfigurationManager.AppSettings["ActivarFolioConfiguracion"].Equals("1") ? true : false) : false;
                    DateTime fechaInicial = new DateTime();
                    DateTime fechaFinal = new DateTime();
                    DateTime.TryParse(filtros.FechaInicial, out fechaInicial);
                    DateTime.TryParse(filtros.FechaFinal, out fechaFinal);

                    if (fechaFinal.ToShortDateString() == "1/1/0001")
                    {
                        fechaFinal = DateTime.Now;
                    }

                    if (fechaInicial.ToShortDateString() == "1/1/0001")
                    {
                        int mes = DateTime.Now.Month != 1 ? DateTime.Now.Month - 1 : 12;
                        int year = DateTime.Now.Month == 1 ? DateTime.Now.Year - 1 : DateTime.Now.Year;
                        fechaInicial = new DateTime(year, mes, DateTime.Now.Day);
                    }

                    int proyectoID = filtros.ProyectoID != "" ? Convert.ToInt32(filtros.ProyectoID) : 0;
                    int clienteID = filtros.ClienteID != "" ? Convert.ToInt32(filtros.ClienteID) : 0;
                    int folioAvisoLlegadaID = filtros.FolioAvisoLlegadaID != null ? Convert.ToInt32(filtros.FolioAvisoLlegadaID) : 0;
                    //int packingListID = filtros.PackingListID != "" ? Convert.ToInt32(filtros.PackingListID) : 0;
                    //int tipoMaterialID = filtros.TipoMaterialID != "" ? Convert.ToInt32(filtros.TipoMaterialID) : 0;

                    List<int> proyectos = ctx.Sam3_Rel_Usuario_Proyecto.Where(x => x.UsuarioID == usuario.UsuarioID).Select(x => x.ProyectoID).AsParallel().ToList();

                    List<int> patios = (from r in ctx.Sam3_Proyecto
                                        join p in ctx.Sam3_Patio on r.PatioID equals p.PatioID
                                        where r.Activo && proyectos.Contains(r.ProyectoID)
                                        select p.PatioID).AsParallel().ToList();

                    patios = patios.Distinct().ToList();

                    //Traemos los folios de entrada de material que no tienen cuantificacion, filtrados por proyectos y patio de usuario
                    List<Sam3_FolioAvisoEntrada> registros = (from fe in ctx.Sam3_FolioAvisoEntrada
                                                              join rfp in ctx.Sam3_Rel_FolioAvisoLlegada_Proyecto on fe.FolioAvisoLlegadaID equals rfp.FolioAvisoLlegadaID
                                                              join p in ctx.Sam3_Proyecto on rfp.ProyectoID equals p.ProyectoID
                                                              where fe.Activo && rfp.Activo && fe.FolioDescarga > 0
                                                              && !(from fc in ctx.Sam3_FolioCuantificacion
                                                                   where fc.Activo
                                                                   select fc.FolioAvisoEntradaID).Contains(fe.FolioAvisoEntradaID)
                                                              && (fe.FechaCreacion >= fechaInicial && fe.FechaCreacion <= fechaFinal)
                                                              && proyectos.Contains(p.ProyectoID)
                                                              && patios.Contains(p.PatioID)
                                                              && fe.ComboEstatus != "Cerrado"
                                                              select fe).AsParallel().ToList();
                    if (proyectoID > 0)
                    {
                        registros = (from r in registros
                                     join rfp in ctx.Sam3_Rel_FolioAvisoLlegada_Proyecto on r.FolioAvisoLlegadaID equals rfp.FolioAvisoLlegadaID
                                     where rfp.ProyectoID == proyectoID
                                     select r).AsParallel().ToList();
                    }

                    if (clienteID > 0)
                    {
                        int sam3Cliente = (from c in ctx.Sam3_Cliente
                                           where c.Activo && c.Sam2ClienteID == clienteID
                                           select c.ClienteID).AsParallel().SingleOrDefault();
                        registros = registros.Where(x => x.ClienteID == sam3Cliente).AsParallel().ToList();
                    }

                    if (folioAvisoLlegadaID > 0)
                    {
                        registros = registros.Where(x => x.FolioAvisoLlegadaID == folioAvisoLlegadaID).AsParallel().ToList();
                    }

                    registros = registros.GroupBy(x => x.FolioAvisoEntradaID).Select(x => x.First()).ToList();

                    List<ListadoMaterialesSinCuantificar> listado = (from r in registros
                                                                     join c in ctx.Sam3_Cliente on r.ClienteID equals c.ClienteID
                                                                     where c.Activo
                                                                     select new ListadoMaterialesSinCuantificar
                                                                     {
                                                                         FolioAvisoEntrada = r.FolioAvisoLlegadaID.ToString(),
                                                                         FechaDescarga = r.FechaFolioDescarga != null ? r.FechaFolioDescarga.Value.ToString() : "",
                                                                         Cliente = c.Nombre
                                                                     }).AsParallel().ToList();
                    if (conteo)
                    {
                        return listado.Count();
                    }
                    else
                    {
                        foreach (ListadoMaterialesSinCuantificar item in listado)
                        {
                            DateTime temp = Convert.ToDateTime(item.FechaDescarga);
                            item.FechaDescarga = temp.ToString("dd/MM/yyyy");

                            int folioavisollegadaid = Convert.ToInt32(item.FolioAvisoEntrada);
                            Sam3_FolioAvisoLlegada FolioAvisoLlegada = ctx.Sam3_FolioAvisoLlegada.Where(x => x.FolioAvisoLlegadaID == folioavisollegadaid).FirstOrDefault();
                            item.FolioConfiguracion = activarFolioConfiguracion ? (from pc in ctx.Sam3_Rel_Proyecto_Entidad_Configuracion
                                                                                   where pc.Entidad == FolioAvisoLlegada.Entidad && pc.Proyecto == FolioAvisoLlegada.ProyectoNombrado
                                                                                   select pc.PreFijoFolioAvisoLlegada + ","
                                                                                    + pc.CantidadCerosFolioAvisoLlegada.ToString() + ","
                                                                                    + FolioAvisoLlegada.Consecutivo.ToString() + ","
                                                                                    + pc.PostFijoFolioAvisoLlegada).FirstOrDefault() : item.FolioAvisoEntrada;

                            if (activarFolioConfiguracion)
                            {
                                string[] elemntos = item.FolioConfiguracion.Split(',').ToArray();
                                int digitos = Convert.ToInt32(elemntos[1]);
                                int consecutivo = Convert.ToInt32(elemntos[2]);
                                string formato = "D" + digitos.ToString();

                                item.FolioConfiguracion = elemntos[0].Trim() + consecutivo.ToString(formato).Trim() + elemntos[3].Trim();
                            }

                        }
                        return listado;
                    }
                }
            }
            catch (Exception ex)
            {
                //-----------------Agregar mensaje al Log -----------------------------------------------
                LoggerBd.Instance.EscribirLog(ex);
                //-----------------Agregar mensaje al Log -----------------------------------------------
                TransactionalInformation result = new TransactionalInformation();
                result.ReturnMessage.Add(ex.Message);
                result.ReturnCode = 500;
                result.ReturnStatus = false;
                result.IsAuthenicated = true;

                return result;
            }
        }

        /// <summary>
        /// Genera el set de datos para el grid de Packing list sin cuantificar del dahsboard de cuantificacion
        /// </summary>
        /// <param name="filtros"></param>
        /// <param name="usuario"></param>
        /// <returns></returns>
        public object ListadoPacknglistPorCuantificar(FiltrosJson filtros, Sam3_Usuario usuario, bool conteo = false)
        {
            try
            {
                using (SamContext ctx = new SamContext())
                {
                    Boolean activarFolioConfiguracion = !string.IsNullOrEmpty(ConfigurationManager.AppSettings["ActivarFolioConfiguracion"]) 
                        ? (ConfigurationManager.AppSettings["ActivarFolioConfiguracion"].Equals("1") ? true : false) : false;

                    bool activaConfiguracionPackinglist = !string.IsNullOrEmpty(ConfigurationManager.AppSettings["ActivarFolioConfiguracionCuantificacion"])
                        ? (ConfigurationManager.AppSettings["ActivarFolioConfiguracionCuantificacion"].Equals("1") ? true : false) : false;

                    DateTime fechaInicial = new DateTime();
                    DateTime fechaFinal = new DateTime();
                    DateTime.TryParse(filtros.FechaInicial, out fechaInicial);
                    DateTime.TryParse(filtros.FechaFinal, out fechaFinal);

                    if (fechaFinal.ToShortDateString() == "1/1/0001")
                    {
                        fechaFinal = DateTime.Now;
                    }

                    if (fechaInicial.ToShortDateString() == "1/1/0001")
                    {
                        int mes = DateTime.Now.Month != 1 ? DateTime.Now.Month - 1 : 12;
                        int year = DateTime.Now.Month == 1 ? DateTime.Now.Year - 1 : DateTime.Now.Year;
                        fechaInicial = new DateTime(year, mes, DateTime.Now.Day);
                    }

                    int proyectoID = filtros.ProyectoID != "" ? Convert.ToInt32(filtros.ProyectoID) : 0;
                    int clienteID = filtros.ClienteID != "" ? Convert.ToInt32(filtros.ClienteID) : 0;
                    int folioAvisoLlegadaID = filtros.FolioAvisoLlegadaID != null ? Convert.ToInt32(filtros.FolioAvisoLlegadaID) : 0;
                    int packingListID = filtros.PackingListID != "" ? Convert.ToInt32(filtros.PackingListID) : 0;
                    int tipoMaterialID = filtros.TipoMaterialID != "" ? Convert.ToInt32(filtros.TipoMaterialID) : 0;

                    List<int> proyectos = ctx.Sam3_Rel_Usuario_Proyecto.Where(x => x.UsuarioID == usuario.UsuarioID).Select(x => x.ProyectoID).AsParallel().ToList();

                    List<int> patios = (from r in ctx.Sam3_Proyecto
                                        join p in ctx.Sam3_Patio on r.PatioID equals p.PatioID
                                        where r.Activo && proyectos.Contains(r.ProyectoID)
                                        select p.PatioID).AsParallel().Distinct().ToList();

                    List<Sam3_FolioAvisoEntrada> registros;
                    if (tipoMaterialID == 3) // todos
                    {
                        registros = (from fe in ctx.Sam3_FolioAvisoEntrada
                                     join rfp in ctx.Sam3_Rel_FolioAvisoLlegada_Proyecto on fe.FolioAvisoLlegadaID equals rfp.FolioAvisoLlegadaID
                                     join p in ctx.Sam3_Proyecto on rfp.ProyectoID equals p.ProyectoID
                                     join fc in ctx.Sam3_FolioCuantificacion on fe.FolioAvisoEntradaID equals fc.FolioAvisoEntradaID
                                     join rfi in ctx.Sam3_Rel_FolioCuantificacion_ItemCode on fc.FolioCuantificacionID equals rfi.FolioCuantificacionID
                                     join rid in ctx.Sam3_Rel_ItemCode_Diametro on rfi.Rel_ItemCode_Diametro_ID equals rid.Rel_ItemCode_Diametro_ID
                                     join i in ctx.Sam3_ItemCode on rid.ItemCodeID equals i.ItemCodeID
                                     where fe.Activo && rfp.Activo && p.Activo && fc.Activo && rfi.Activo && i.Activo && rid.Activo
                                     && fe.FolioDescarga > 0
                                     && fc.Estatus != "Cerrado"
                                     && proyectos.Contains(p.ProyectoID)
                                     && patios.Contains(p.PatioID)
                                     && (fe.FechaCreacion >= fechaInicial && fe.FechaCreacion <= fechaFinal)
                                     select fe).AsParallel().ToList();
                    }
                    else
                    {
                        registros = (from fe in ctx.Sam3_FolioAvisoEntrada
                                     join rfp in ctx.Sam3_Rel_FolioAvisoLlegada_Proyecto on fe.FolioAvisoLlegadaID equals rfp.FolioAvisoLlegadaID
                                     join p in ctx.Sam3_Proyecto on rfp.ProyectoID equals p.ProyectoID
                                     join fc in ctx.Sam3_FolioCuantificacion on fe.FolioAvisoEntradaID equals fc.FolioAvisoEntradaID
                                     join rfi in ctx.Sam3_Rel_FolioCuantificacion_ItemCode on fc.FolioCuantificacionID equals rfi.FolioCuantificacionID
                                     join rid in ctx.Sam3_Rel_ItemCode_Diametro on rfi.Rel_ItemCode_Diametro_ID equals rid.Rel_ItemCode_Diametro_ID
                                     join i in ctx.Sam3_ItemCode on rid.ItemCodeID equals i.ItemCodeID
                                     where fe.Activo && rfp.Activo && p.Activo && fc.Activo && rfi.Activo && i.Activo && rid.Activo
                                     && fe.FolioDescarga > 0
                                     && i.TipoMaterialID == tipoMaterialID
                                     && fc.Estatus != "Cerrado"
                                     && proyectos.Contains(p.ProyectoID)
                                     && patios.Contains(p.PatioID)
                                     && (fe.FechaCreacion >= fechaInicial && fe.FechaCreacion <= fechaFinal)
                                     select fe).AsParallel().ToList();
                    }

                    if (proyectoID > 0)
                    {
                        registros = (from r in registros
                                     join rfp in ctx.Sam3_Rel_FolioAvisoLlegada_Proyecto on r.FolioAvisoLlegadaID equals rfp.FolioAvisoLlegadaID
                                     where rfp.ProyectoID == proyectoID
                                     select r).AsParallel().ToList();

                    }

                    if (clienteID > 0)
                    {
                        int sam3Cliente = (from c in ctx.Sam3_Cliente
                                           where c.Activo && c.Sam2ClienteID == clienteID
                                           select c.ClienteID).AsParallel().SingleOrDefault();
                        registros = registros.Where(x => x.ClienteID == sam3Cliente).AsParallel().ToList();
                    }

                    if (folioAvisoLlegadaID > 0)
                    {
                        registros = registros.Where(x => x.FolioAvisoLlegadaID == folioAvisoLlegadaID).AsParallel().ToList();
                    }

                    if (packingListID > 0)
                    {
                        registros = (from r in registros
                                     join fc in ctx.Sam3_FolioCuantificacion on r.FolioAvisoEntradaID equals fc.FolioAvisoEntradaID
                                     where fc.FolioCuantificacionID == packingListID
                                     select r).AsParallel().ToList();
                    }

                    registros = registros.GroupBy(x => x.FolioAvisoEntradaID).Select(x => x.First()).ToList();

                    List<ListadoPLporCuantificar> listado = (from r in registros
                                                             join rfp in ctx.Sam3_Rel_FolioAvisoLlegada_Proyecto on r.FolioAvisoLlegadaID equals rfp.FolioAvisoLlegadaID
                                                             join fc in ctx.Sam3_FolioCuantificacion on r.FolioAvisoEntradaID equals fc.FolioAvisoEntradaID
                                                             join fa in ctx.Sam3_FolioAvisoLlegada on rfp.FolioAvisoLlegadaID equals fa.FolioAvisoLlegadaID
                                                             join p in ctx.Sam3_Proyecto on fc.ProyectoID equals p.ProyectoID
                                                             where r.Activo && rfp.Activo && fc.Activo && fa.Activo && p.Activo
                                                             select new ListadoPLporCuantificar
                                                             {
                                                                 Proyecto = p.Nombre,
                                                                 FolioAvisoEntrada = r.FolioAvisoLlegadaID.ToString(),
                                                                 FechaDescarga = r.FechaFolioDescarga != null ? r.FechaFolioDescarga.Value.ToString() : "",
                                                                 FechaCreacionPackingList = fc.FechaCreacion != null ? fc.FechaCreacion.Value.ToString() : "",
                                                                 PackingList = activaConfiguracionPackinglist && (fc.PackingList == "" || fc.PackingList == null) ?
                                                                                            (from pc in ctx.Sam3_Rel_Proyecto_Entidad_Configuracion
                                                                                             where pc.Rel_Proyecto_Entidad_Configuracion_ID == fc.Rel_Proyecto_Entidad_Configuracion_ID
                                                                                             && pc.Activo == 1
                                                                                             select pc.PreFijoFolioPackingList + ","
                                                                                             + pc.CantidadCerosFolioPackingList.ToString() + ","
                                                                                             + fc.Consecutivo.ToString() + ","
                                                                                             + pc.PostFijoFolioPackingList).FirstOrDefault() 
                                                                                             : fc.PackingList != string.Empty ? fc.PackingList : fc.FolioCuantificacionID.ToString(),
                                                                 FolioCuantificacionID = fc.FolioCuantificacionID.ToString(),
                                                                 FolioConfiguracion = activarFolioConfiguracion ? 
                                                                                            (from pc in ctx.Sam3_Rel_Proyecto_Entidad_Configuracion
                                                                                             where pc.Entidad == fa.Entidad && pc.Proyecto == fa.ProyectoNombrado
                                                                                             select pc.PreFijoFolioAvisoLlegada + ","
                                                                                             + pc.CantidadCerosFolioAvisoLlegada.ToString() + ","
                                                                                             + fa.Consecutivo.ToString() + ","
                                                                                             + pc.PostFijoFolioAvisoLlegada).FirstOrDefault() : r.FolioAvisoLlegadaID.ToString()
                                                             }).Distinct().AsParallel().ToList();

                    listado = listado.GroupBy(x => x.FolioCuantificacionID).Select(x => x.First()).ToList();

                    if (activarFolioConfiguracion && !conteo)
                    {
                        foreach (ListadoPLporCuantificar item in listado)
                        {
                            if (activarFolioConfiguracion)
                            {
                                string[] elemntos = item.FolioConfiguracion.Split(',').ToArray();
                                int digitos = Convert.ToInt32(elemntos[1]);
                                int consecutivo = Convert.ToInt32(elemntos[2]);
                                string formato = "D" + digitos.ToString();

                                item.FolioConfiguracion = elemntos[0].Trim() + consecutivo.ToString(formato).Trim() + elemntos[3].Trim();
                            }
                        }
                    }

                    if (activaConfiguracionPackinglist && !conteo)
                    {
                        foreach (ListadoPLporCuantificar item in listado)
                        {
                            if (activaConfiguracionPackinglist && item.PackingList.Contains(','))
                            {
                                string[] elementos = item.PackingList.Split(',').ToArray();
                                int digitos = Convert.ToInt32(elementos[1]);
                                int consecutivo = Convert.ToInt32(elementos[2]);
                                string formato = "D" + digitos.ToString();

                                item.PackingList = elementos[0].Trim() + consecutivo.ToString(formato).Trim() + elementos[3].Trim();
                            }
                        }
                    }


                    if (conteo)
                    {
                        return listado.Count();
                    }
                    else
                    {
                        foreach (ListadoPLporCuantificar lst in listado)
                        {
                            DateTime temp = Convert.ToDateTime(lst.FechaDescarga);
                            lst.FechaDescarga = temp.ToString("dd/MM/yyyy");

                            DateTime fechaPL = Convert.ToDateTime(lst.FechaCreacionPackingList);
                            lst.FechaCreacionPackingList = fechaPL.ToString("dd/MM/yyyy");
                        }

                        return listado;
                    }
                }
            }
            catch (Exception ex)
            {
                //-----------------Agregar mensaje al Log -----------------------------------------------
                LoggerBd.Instance.EscribirLog(ex);
                //-----------------Agregar mensaje al Log -----------------------------------------------
                TransactionalInformation result = new TransactionalInformation();
                result.ReturnMessage.Add(ex.Message);
                result.ReturnCode = 500;
                result.ReturnStatus = false;
                result.IsAuthenicated = true;

                return result;
            }
        }

        /// <summary>
        /// Genera el set de datos para el gri de Materiales sin ItemCodeSteelgo
        /// </summary>
        /// <param name="filtros"></param>
        /// <param name="usuario"></param>
        /// <returns></returns>
        public object ListadoMTLSinICS(FiltrosJson filtros, Sam3_Usuario usuario, bool conteo = false)
        {
            try
            {
                using (SamContext ctx = new SamContext())
                {
                    DateTime fechaInicial = new DateTime();
                    DateTime fechaFinal = new DateTime();
                    DateTime.TryParse(filtros.FechaInicial, out fechaInicial);
                    DateTime.TryParse(filtros.FechaFinal, out fechaFinal);
                    int totalSinICS = 0;

                    if (fechaFinal.ToShortDateString() == "1/1/0001")
                    {
                        fechaFinal = DateTime.Now;
                    }

                    if (fechaInicial.ToShortDateString() == "1/1/0001")
                    {
                        int mes = DateTime.Now.Month != 1 ? DateTime.Now.Month - 1 : 12;
                        int year = DateTime.Now.Month == 1 ? DateTime.Now.Year - 1 : DateTime.Now.Year;
                        fechaInicial = new DateTime(year, mes, DateTime.Now.Day);
                    }

                    int proyectoID = filtros.ProyectoID != "" ? Convert.ToInt32(filtros.ProyectoID) : 0;
                    int clienteID = filtros.ClienteID != "" ? Convert.ToInt32(filtros.ClienteID) : 0;
                    int folioAvisoLlegadaID = filtros.FolioAvisoLlegadaID != null ? Convert.ToInt32(filtros.FolioAvisoLlegadaID) : 0;
                    int packingListID = filtros.PackingListID != "" ? Convert.ToInt32(filtros.PackingListID) : 0;
                    int tipoMaterialID = filtros.TipoMaterialID != "" ? Convert.ToInt32(filtros.TipoMaterialID) : 0;

                    List<int> proyectos = ctx.Sam3_Rel_Usuario_Proyecto.Where(x => x.UsuarioID == usuario.UsuarioID).Select(x => x.ProyectoID).AsParallel().ToList();

                    List<int> patios = (from r in ctx.Sam3_Proyecto
                                        join p in ctx.Sam3_Patio on r.PatioID equals p.PatioID
                                        where r.Activo && proyectos.Contains(r.ProyectoID)
                                        select p.PatioID).AsParallel().Distinct().ToList();

                    List<Sam3_FolioAvisoEntrada> registos;

                    if (proyectoID > 0)
                    {
                        registos = (from fe in ctx.Sam3_FolioAvisoEntrada
                                    join rfp in ctx.Sam3_Rel_FolioAvisoLlegada_Proyecto on fe.FolioAvisoLlegadaID equals rfp.FolioAvisoLlegadaID
                                    join p in ctx.Sam3_Proyecto on rfp.ProyectoID equals p.ProyectoID
                                    join fc in ctx.Sam3_FolioCuantificacion on fe.FolioAvisoEntradaID equals fc.FolioAvisoEntradaID
                                    join rfi in ctx.Sam3_Rel_FolioCuantificacion_ItemCode on fc.FolioCuantificacionID equals rfi.FolioCuantificacionID
                                    join rid in ctx.Sam3_Rel_ItemCode_Diametro on rfi.Rel_ItemCode_Diametro_ID equals rid.Rel_ItemCode_Diametro_ID
                                    join i in ctx.Sam3_ItemCode on rid.ItemCodeID equals i.ItemCodeID
                                    where fe.Activo && rfp.Activo && p.Activo && fc.Activo && rfi.Activo && i.Activo && rid.Activo
                                    && (!(from its in ctx.Sam3_Rel_ItemCode_ItemCodeSteelgo
                                          where its.Activo
                                          select its.Rel_ItemCode_Diametro_ID).Contains(rid.Rel_ItemCode_Diametro_ID)
                                        ||
                                        (from its in ctx.Sam3_Rel_ItemCode_ItemCodeSteelgo
                                         join ics in ctx.Sam3_ItemCodeSteelgo on its.ItemCodeSteelgoID equals ics.ItemCodeSteelgoID
                                         where its.Activo && ics.ItemCodeSteelgoID == 1
                                         select its.Rel_ItemCode_Diametro_ID).Contains(rid.Rel_ItemCode_Diametro_ID)
                                        )
                                    && proyectos.Contains(p.ProyectoID)
                                    && patios.Contains(p.PatioID)
                                    && (fe.FechaCreacion >= fechaInicial && fe.FechaCreacion <= fechaFinal)
                                    && p.ProyectoID == proyectoID
                                    select fe).AsParallel().ToList();

                        registos.AddRange((from fe in ctx.Sam3_FolioAvisoEntrada
                                           join rfp in ctx.Sam3_Rel_FolioAvisoLlegada_Proyecto on fe.FolioAvisoLlegadaID equals rfp.FolioAvisoLlegadaID
                                           join p in ctx.Sam3_Proyecto on rfp.ProyectoID equals p.ProyectoID
                                           join fc in ctx.Sam3_FolioCuantificacion on fe.FolioAvisoEntradaID equals fc.FolioAvisoEntradaID
                                           join b in ctx.Sam3_Bulto on fc.FolioCuantificacionID equals b.FolioCuantificacionID
                                           join rbi in ctx.Sam3_Rel_Bulto_ItemCode on b.BultoID equals rbi.BultoID
                                           join rid in ctx.Sam3_Rel_ItemCode_Diametro on rbi.Rel_ItemCode_Diametro_ID equals rid.Rel_ItemCode_Diametro_ID
                                           join i in ctx.Sam3_ItemCode on rid.ItemCodeID equals i.ItemCodeID
                                           where fe.Activo && rfp.Activo && p.Activo && fc.Activo && rbi.Activo && i.Activo
                                           && (!(from its in ctx.Sam3_Rel_ItemCode_ItemCodeSteelgo
                                                 where its.Activo
                                                 select its.Rel_ItemCode_Diametro_ID).Contains(rid.Rel_ItemCode_Diametro_ID)
                                               ||
                                               (from its in ctx.Sam3_Rel_ItemCode_ItemCodeSteelgo
                                                join ics in ctx.Sam3_ItemCodeSteelgo on its.ItemCodeSteelgoID equals ics.ItemCodeSteelgoID
                                                where its.Activo && ics.ItemCodeSteelgoID == 1
                                                select its.Rel_ItemCode_Diametro_ID).Contains(rid.Rel_ItemCode_Diametro_ID)
                                               )
                                           && proyectos.Contains(p.ProyectoID)
                                           && patios.Contains(p.PatioID)
                                           && (fe.FechaCreacion >= fechaInicial && fe.FechaCreacion <= fechaFinal)
                                           && p.ProyectoID == proyectoID
                                           select fe).AsParallel().ToList());
                    }
                    else
                    {
                        registos = (from fe in ctx.Sam3_FolioAvisoEntrada
                                    join rfp in ctx.Sam3_Rel_FolioAvisoLlegada_Proyecto on fe.FolioAvisoLlegadaID equals rfp.FolioAvisoLlegadaID
                                    join p in ctx.Sam3_Proyecto on rfp.ProyectoID equals p.ProyectoID
                                    join fc in ctx.Sam3_FolioCuantificacion on fe.FolioAvisoEntradaID equals fc.FolioAvisoEntradaID
                                    join rfi in ctx.Sam3_Rel_FolioCuantificacion_ItemCode on fc.FolioCuantificacionID equals rfi.FolioCuantificacionID
                                    join rid in ctx.Sam3_Rel_ItemCode_Diametro on rfi.Rel_ItemCode_Diametro_ID equals rid.Rel_ItemCode_Diametro_ID
                                    join i in ctx.Sam3_ItemCode on rid.ItemCodeID equals i.ItemCodeID
                                    where fe.Activo && rfp.Activo && p.Activo && fc.Activo && rfi.Activo && i.Activo && rid.Activo
                                    && (!(from its in ctx.Sam3_Rel_ItemCode_ItemCodeSteelgo
                                          where its.Activo
                                          select its.Rel_ItemCode_Diametro_ID).Contains(rid.Rel_ItemCode_Diametro_ID)
                                        ||
                                        (from its in ctx.Sam3_Rel_ItemCode_ItemCodeSteelgo
                                         join ics in ctx.Sam3_ItemCodeSteelgo on its.ItemCodeSteelgoID equals ics.ItemCodeSteelgoID
                                         where its.Activo && ics.ItemCodeSteelgoID == 1
                                         select its.Rel_ItemCode_Diametro_ID).Contains(rid.Rel_ItemCode_Diametro_ID)
                                        )
                                    && proyectos.Contains(p.ProyectoID)
                                    && patios.Contains(p.PatioID)
                                    && (fe.FechaCreacion >= fechaInicial && fe.FechaCreacion <= fechaFinal)
                                    select fe).AsParallel().ToList();

                        registos.AddRange((from fe in ctx.Sam3_FolioAvisoEntrada
                                           join rfp in ctx.Sam3_Rel_FolioAvisoLlegada_Proyecto on fe.FolioAvisoLlegadaID equals rfp.FolioAvisoLlegadaID
                                           join p in ctx.Sam3_Proyecto on rfp.ProyectoID equals p.ProyectoID
                                           join fc in ctx.Sam3_FolioCuantificacion on fe.FolioAvisoEntradaID equals fc.FolioAvisoEntradaID
                                           join b in ctx.Sam3_Bulto on fc.FolioCuantificacionID equals b.FolioCuantificacionID
                                           join rbi in ctx.Sam3_Rel_Bulto_ItemCode on b.BultoID equals rbi.BultoID
                                           join rid in ctx.Sam3_Rel_ItemCode_Diametro on rbi.Rel_ItemCode_Diametro_ID equals rid.Rel_ItemCode_Diametro_ID
                                           join i in ctx.Sam3_ItemCode on rid.ItemCodeID equals i.ItemCodeID
                                           where fe.Activo && rfp.Activo && p.Activo && fc.Activo && rbi.Activo && i.Activo
                                           && (!(from its in ctx.Sam3_Rel_ItemCode_ItemCodeSteelgo
                                                 where its.Activo
                                                 select its.Rel_ItemCode_Diametro_ID).Contains(rid.Rel_ItemCode_Diametro_ID)
                                               ||
                                               (from its in ctx.Sam3_Rel_ItemCode_ItemCodeSteelgo
                                                join ics in ctx.Sam3_ItemCodeSteelgo on its.ItemCodeSteelgoID equals ics.ItemCodeSteelgoID
                                                where its.Activo && ics.ItemCodeSteelgoID == 1
                                                select its.Rel_ItemCode_Diametro_ID).Contains(rid.Rel_ItemCode_Diametro_ID)
                                               )
                                           && proyectos.Contains(p.ProyectoID)
                                           && patios.Contains(p.PatioID)
                                           && (fe.FechaCreacion >= fechaInicial && fe.FechaCreacion <= fechaFinal)
                                           select fe).AsParallel().ToList());
                    }

                    if (clienteID > 0)
                    {
                        int sam3Cliente = (from c in ctx.Sam3_Cliente
                                           where c.Activo && c.Sam2ClienteID == clienteID
                                           select c.ClienteID).AsParallel().SingleOrDefault();
                        registos = registos.Where(x => x.ClienteID == sam3Cliente).ToList();
                    }

                    if (folioAvisoLlegadaID > 0)
                    {
                        registos = registos.Where(x => x.FolioAvisoLlegadaID == folioAvisoLlegadaID).ToList();
                    }

                    //Quitar duplicados
                    registos = registos.GroupBy(x => x.FolioAvisoLlegadaID).Select(x => x.First()).ToList();

                    List<ListadoMTLSinICS> listado = new List<ListadoMTLSinICS>();

                    foreach (Sam3_FolioAvisoEntrada fae in registos)
                    {
                        List<Sam3_FolioCuantificacion> foliosCuantificacion;
                        if (packingListID > 0)
                        {
                            foliosCuantificacion = (from fc in ctx.Sam3_FolioCuantificacion
                                                    join fe in ctx.Sam3_FolioAvisoEntrada on fc.FolioAvisoEntradaID equals fe.FolioAvisoEntradaID
                                                    where fe.FolioAvisoEntradaID == fae.FolioAvisoEntradaID
                                                    && fc.FolioCuantificacionID == packingListID
                                                    select fc).ToList();
                        }
                        else
                        {
                            foliosCuantificacion = (from fc in ctx.Sam3_FolioCuantificacion
                                                    join fe in ctx.Sam3_FolioAvisoEntrada on fc.FolioAvisoEntradaID equals fe.FolioAvisoEntradaID
                                                    where fe.FolioAvisoEntradaID == fae.FolioAvisoEntradaID
                                                    select fc).ToList();
                        }

                        foliosCuantificacion = foliosCuantificacion.GroupBy(x => x.FolioCuantificacionID).Select(x => x.First()).ToList();

                        foreach (Sam3_FolioCuantificacion fc in foliosCuantificacion)
                        {
                            ListadoMTLSinICS elemento = new ListadoMTLSinICS();
                            elemento.FechaCreacionPackingList = fc.FechaCreacion != null ? fc.FechaCreacion.Value.ToString("dd/MM/yyyy") : "";
                            elemento.PackingList = fc.PackingList + "(" + fc.FolioCuantificacionID.ToString() + ")";
                            elemento.Proyecto = ctx.Sam3_Proyecto.Where(x => x.ProyectoID == fc.ProyectoID).Select(x => x.Nombre).SingleOrDefault();
                            elemento.FolioCuantificacionID = fc.FolioCuantificacionID.ToString();

                            int cantidadsinICS = 0;
                            int totalItems = 0;

                            if (tipoMaterialID == 3) //todos
                            {
                                #region todos los materiales
                                cantidadsinICS = (from f in ctx.Sam3_FolioCuantificacion
                                                  join rfi in ctx.Sam3_Rel_FolioCuantificacion_ItemCode on f.FolioCuantificacionID equals rfi.FolioCuantificacionID
                                                  join rid in ctx.Sam3_Rel_ItemCode_Diametro on rfi.Rel_ItemCode_Diametro_ID equals rid.Rel_ItemCode_Diametro_ID
                                                  join it in ctx.Sam3_ItemCode on rid.ItemCodeID equals it.ItemCodeID
                                                  where f.Activo && rfi.Activo && it.Activo && rid.Activo
                                                  && f.FolioCuantificacionID == fc.FolioCuantificacionID
                                                  && (!(from its in ctx.Sam3_Rel_ItemCode_ItemCodeSteelgo
                                                        where its.Activo
                                                        select its.Rel_ItemCode_Diametro_ID).Contains(rid.Rel_ItemCode_Diametro_ID)
                                                   ||
                                                       (from its in ctx.Sam3_Rel_ItemCode_ItemCodeSteelgo
                                                        join ics in ctx.Sam3_ItemCodeSteelgo on its.ItemCodeSteelgoID equals ics.ItemCodeSteelgoID
                                                        where its.Activo && ics.ItemCodeSteelgoID == 1
                                                        select its.Rel_ItemCode_Diametro_ID).Contains(rid.Rel_ItemCode_Diametro_ID))
                                                  select it.ItemCodeID).AsParallel().Count();

                                cantidadsinICS += (from f in ctx.Sam3_FolioCuantificacion
                                                   join b in ctx.Sam3_Bulto on f.FolioCuantificacionID equals b.FolioCuantificacionID
                                                   join rbi in ctx.Sam3_Rel_Bulto_ItemCode on b.BultoID equals rbi.BultoID
                                                   join rid in ctx.Sam3_Rel_ItemCode_Diametro on rbi.Rel_ItemCode_Diametro_ID equals rid.Rel_ItemCode_Diametro_ID
                                                   join it in ctx.Sam3_ItemCode on rid.ItemCodeID equals it.ItemCodeID
                                                   where f.Activo && rbi.Activo && it.Activo && rid.Activo
                                                   && f.FolioCuantificacionID == fc.FolioCuantificacionID
                                                   && (!(from its in ctx.Sam3_Rel_ItemCode_ItemCodeSteelgo
                                                         where its.Activo
                                                         select its.Rel_ItemCode_Diametro_ID).Contains(rid.Rel_ItemCode_Diametro_ID)
                                                       ||
                                                       (from its in ctx.Sam3_Rel_ItemCode_ItemCodeSteelgo
                                                        join ics in ctx.Sam3_ItemCodeSteelgo on its.ItemCodeSteelgoID equals ics.ItemCodeSteelgoID
                                                        where its.Activo && ics.ItemCodeSteelgoID == 1
                                                        select its.Rel_ItemCode_Diametro_ID).Contains(rid.Rel_ItemCode_Diametro_ID))
                                                   select it.ItemCodeID).AsParallel().Count();

                                totalItems = (from f in ctx.Sam3_FolioCuantificacion
                                              join rfi in ctx.Sam3_Rel_FolioCuantificacion_ItemCode on f.FolioCuantificacionID equals rfi.FolioCuantificacionID
                                              join rid in ctx.Sam3_Rel_ItemCode_Diametro on rfi.Rel_ItemCode_Diametro_ID equals rid.Rel_ItemCode_Diametro_ID
                                              join it in ctx.Sam3_ItemCode on rid.ItemCodeID equals it.ItemCodeID
                                              where f.Activo && rfi.Activo && it.Activo && rid.Activo
                                              && f.FolioCuantificacionID == fc.FolioCuantificacionID
                                              select it.ItemCodeID).AsParallel().Count();

                                totalItems += (from f in ctx.Sam3_FolioCuantificacion
                                               join b in ctx.Sam3_Bulto on f.FolioCuantificacionID equals b.FolioCuantificacionID
                                               join rbi in ctx.Sam3_Rel_Bulto_ItemCode on b.BultoID equals rbi.BultoID
                                               join rid in ctx.Sam3_Rel_ItemCode_Diametro on rbi.Rel_ItemCode_Diametro_ID equals rid.Rel_ItemCode_Diametro_ID
                                               join it in ctx.Sam3_ItemCode on rid.ItemCodeID equals it.ItemCodeID
                                               where f.Activo && rbi.Activo && it.Activo && rid.Activo && b.Activo
                                               && f.FolioCuantificacionID == fc.FolioCuantificacionID
                                               select it.ItemCodeID).AsParallel().Count();

                                //return totalItems;
                                #endregion
                            }
                            else
                            {
                                #region por tipo de material
                                cantidadsinICS = (from f in ctx.Sam3_FolioCuantificacion
                                                  join rfi in ctx.Sam3_Rel_FolioCuantificacion_ItemCode on f.FolioCuantificacionID equals rfi.FolioCuantificacionID
                                                  join rid in ctx.Sam3_Rel_ItemCode_Diametro on rfi.Rel_ItemCode_Diametro_ID equals rid.Rel_ItemCode_Diametro_ID
                                                  join it in ctx.Sam3_ItemCode on rid.ItemCodeID equals it.ItemCodeID
                                                  where f.Activo && rfi.Activo && it.Activo && rid.Activo
                                                  && it.TipoMaterialID == tipoMaterialID
                                                  && f.FolioCuantificacionID == fc.FolioCuantificacionID
                                                  && (!(from its in ctx.Sam3_Rel_ItemCode_ItemCodeSteelgo
                                                        where its.Activo
                                                        select its.Rel_ItemCode_Diametro_ID).Contains(rid.Rel_ItemCode_Diametro_ID)
                                                   ||
                                                       (from its in ctx.Sam3_Rel_ItemCode_ItemCodeSteelgo
                                                        join ics in ctx.Sam3_ItemCodeSteelgo on its.ItemCodeSteelgoID equals ics.ItemCodeSteelgoID
                                                        where its.Activo && ics.ItemCodeSteelgoID == 1
                                                        select its.Rel_ItemCode_Diametro_ID).Contains(rid.Rel_ItemCode_Diametro_ID))
                                                  select it.ItemCodeID).AsParallel().Count();

                                cantidadsinICS += (from f in ctx.Sam3_FolioCuantificacion
                                                   join b in ctx.Sam3_Bulto on f.FolioCuantificacionID equals b.FolioCuantificacionID
                                                   join rbi in ctx.Sam3_Rel_Bulto_ItemCode on b.BultoID equals rbi.BultoID
                                                   join rid in ctx.Sam3_Rel_ItemCode_Diametro on rbi.Rel_ItemCode_Diametro_ID equals rid.Rel_ItemCode_Diametro_ID
                                                   join it in ctx.Sam3_ItemCode on rid.ItemCodeID equals it.ItemCodeID
                                                   where f.Activo && rbi.Activo && it.Activo && rid.Activo
                                                   && it.TipoMaterialID == tipoMaterialID
                                                   && f.FolioCuantificacionID == fc.FolioCuantificacionID
                                                   && (!(from its in ctx.Sam3_Rel_ItemCode_ItemCodeSteelgo
                                                         where its.Activo
                                                         select its.Rel_ItemCode_Diametro_ID).Contains(rid.Rel_ItemCode_Diametro_ID)
                                                       ||
                                                       (from its in ctx.Sam3_Rel_ItemCode_ItemCodeSteelgo
                                                        join ics in ctx.Sam3_ItemCodeSteelgo on its.ItemCodeSteelgoID equals ics.ItemCodeSteelgoID
                                                        where its.Activo && ics.ItemCodeSteelgoID == 1
                                                        select its.Rel_ItemCode_Diametro_ID).Contains(rid.Rel_ItemCode_Diametro_ID))
                                                   select it.ItemCodeID).AsParallel().Count();

                                totalItems = (from f in ctx.Sam3_FolioCuantificacion
                                              join rfi in ctx.Sam3_Rel_FolioCuantificacion_ItemCode on f.FolioCuantificacionID equals rfi.FolioCuantificacionID
                                              join rid in ctx.Sam3_Rel_ItemCode_Diametro on rfi.Rel_ItemCode_Diametro_ID equals rid.Rel_ItemCode_Diametro_ID
                                              join it in ctx.Sam3_ItemCode on rid.ItemCodeID equals it.ItemCodeID
                                              where f.Activo && rfi.Activo && it.Activo && rid.Activo
                                              && it.TipoMaterialID == tipoMaterialID
                                              && f.FolioCuantificacionID == fc.FolioCuantificacionID
                                              select it.ItemCodeID).AsParallel().Count();

                                totalItems += (from f in ctx.Sam3_FolioCuantificacion
                                               join b in ctx.Sam3_Bulto on f.FolioCuantificacionID equals b.FolioCuantificacionID
                                               join rbi in ctx.Sam3_Rel_Bulto_ItemCode on b.BultoID equals rbi.BultoID
                                               join rid in ctx.Sam3_Rel_ItemCode_Diametro on rbi.Rel_ItemCode_Diametro_ID equals rid.Rel_ItemCode_Diametro_ID
                                               join it in ctx.Sam3_ItemCode on rid.ItemCodeID equals it.ItemCodeID
                                               where f.Activo && rbi.Activo && it.Activo && rid.Activo && b.Activo
                                               && it.TipoMaterialID == tipoMaterialID
                                               && f.FolioCuantificacionID == fc.FolioCuantificacionID
                                               select it.ItemCodeID).AsParallel().Count();
                                #endregion
                            }

                            totalSinICS += cantidadsinICS;
                            elemento.CantidadSinICS = cantidadsinICS.ToString();
                            elemento.CantidadTotalItems = totalItems.ToString();

                            if (Convert.ToInt32(elemento.CantidadSinICS) > 0)
                            {
                                listado.Add(elemento);
                            }

                        }
                    }

                    if (conteo)
                    {
                        return totalSinICS; //listado.Count();
                    }
                    else
                    {
                        return listado;
                    }
                }
            }
            catch (Exception ex)
            {
                //-----------------Agregar mensaje al Log -----------------------------------------------
                LoggerBd.Instance.EscribirLog(ex);
                //-----------------Agregar mensaje al Log -----------------------------------------------
                TransactionalInformation result = new TransactionalInformation();
                result.ReturnMessage.Add(ex.Message);
                result.ReturnCode = 500;
                result.ReturnStatus = false;
                result.IsAuthenicated = true;

                return result;
            }
        }

        /// <summary>
        /// Numeros unicos con recepcion pero sin complemento de recepcion
        /// </summary>
        /// <param name="filtros"></param>
        /// <param name="usuario"></param>
        /// <returns></returns>
        public object ListadoNUConRecepcionSinComplemento(FiltrosJson filtros, Sam3_Usuario usuario, bool conteo = false)
        {
            try
            {
                using (SamContext ctx = new SamContext())
                {
                    DateTime fechaInicial = new DateTime();
                    DateTime fechaFinal = new DateTime();
                    DateTime.TryParse(filtros.FechaInicial, out fechaInicial);
                    DateTime.TryParse(filtros.FechaFinal, out fechaFinal);
                    Boolean activarFolioConfiguracionOR = !string.IsNullOrEmpty(ConfigurationManager.AppSettings["ActivarFolioConfiguracionOrdenRecepcion"]) 
                        ? (ConfigurationManager.AppSettings["ActivarFolioConfiguracionOrdenRecepcion"].Equals("1") ? true : false) : false;


                    if (fechaFinal.ToShortDateString() == "1/1/0001")
                    {
                        fechaFinal = DateTime.Now;
                    }

                    if (fechaInicial.ToShortDateString() == "1/1/0001")
                    {
                        int mes = DateTime.Now.Month != 1 ? DateTime.Now.Month - 1 : 12;
                        int year = DateTime.Now.Month == 1 ? DateTime.Now.Year - 1 : DateTime.Now.Year;
                        fechaInicial = new DateTime(year, mes, DateTime.Now.Day);
                    }

                    int proyectoID = filtros.ProyectoID != "" ? Convert.ToInt32(filtros.ProyectoID) : 0;
                    int clienteID = filtros.ClienteID != "" ? Convert.ToInt32(filtros.ClienteID) : 0;
                    int folioAvisoLlegadaID = filtros.FolioAvisoLlegadaID != null ? Convert.ToInt32(filtros.FolioAvisoLlegadaID) : 0;
                    int packingListID = filtros.PackingListID != "" ? Convert.ToInt32(filtros.PackingListID) : 0;
                    int tipoMaterialID = filtros.TipoMaterialID != "" ? Convert.ToInt32(filtros.TipoMaterialID) : 0;

                    List<int> proyectos = ctx.Sam3_Rel_Usuario_Proyecto.Where(x => x.UsuarioID == usuario.UsuarioID).Select(x => x.ProyectoID).AsParallel().ToList();

                    List<int> patios = (from r in ctx.Sam3_Proyecto
                                        join p in ctx.Sam3_Patio on r.PatioID equals p.PatioID
                                        where r.Activo && proyectos.Contains(r.ProyectoID)
                                        select p.PatioID).AsParallel().Distinct().ToList();

                    List<Sam3_FolioAvisoEntrada> registros;
                    if (proyectoID > 0)
                    {
                        registros = (from fe in ctx.Sam3_FolioAvisoEntrada
                                     join rfp in ctx.Sam3_Rel_FolioAvisoLlegada_Proyecto on fe.FolioAvisoLlegadaID equals rfp.FolioAvisoLlegadaID
                                     join p in ctx.Sam3_Proyecto on rfp.ProyectoID equals p.ProyectoID
                                     where fe.Activo && rfp.Activo && p.Activo
                                     && (fe.FechaCreacion >= fechaInicial && fe.FechaCreacion <= fechaFinal)
                                     && proyectos.Contains(rfp.ProyectoID)
                                     && patios.Contains(p.PatioID)
                                     && rfp.ProyectoID == proyectoID
                                     select fe).AsParallel().ToList();
                    }
                    else
                    {
                        registros = (from fe in ctx.Sam3_FolioAvisoEntrada
                                     join rfp in ctx.Sam3_Rel_FolioAvisoLlegada_Proyecto on fe.FolioAvisoLlegadaID equals rfp.FolioAvisoLlegadaID
                                     join p in ctx.Sam3_Proyecto on rfp.ProyectoID equals p.ProyectoID
                                     where fe.Activo && rfp.Activo && p.Activo
                                     && (fe.FechaCreacion >= fechaInicial && fe.FechaCreacion <= fechaFinal)
                                     && proyectos.Contains(rfp.ProyectoID)
                                     && patios.Contains(p.PatioID)
                                     select fe).AsParallel().ToList();
                    }

                    if (clienteID > 0)
                    {
                        int sam3Cliente = (from c in ctx.Sam3_Cliente
                                           where c.Activo && c.Sam2ClienteID == clienteID
                                           select c.ClienteID).AsParallel().SingleOrDefault();
                        registros = registros.Where(x => x.ClienteID == sam3Cliente).ToList();
                    }

                    if (folioAvisoLlegadaID > 0)
                    {
                        registros = registros.Where(x => x.FolioAvisoLlegadaID == folioAvisoLlegadaID).ToList();
                    }

                    registros = registros.GroupBy(x => x.FolioAvisoLlegadaID).Select(x => x.First()).ToList();

                    List<ListadoNUPorRecepcionar> listado = new List<ListadoNUPorRecepcionar>();
                    List<Sam3_OrdenRecepcion> ordenes;
                    ListadoNUPorRecepcionar elemento;

                    foreach (Sam3_FolioAvisoEntrada f in registros)
                    {
                        if (packingListID > 0)
                        {
                            ordenes = (from fc in ctx.Sam3_FolioCuantificacion
                                       join rfci in ctx.Sam3_Rel_FolioCuantificacion_ItemCode on fc.FolioCuantificacionID equals rfci.FolioCuantificacionID
                                       join rid in ctx.Sam3_Rel_ItemCode_Diametro on rfci.Rel_ItemCode_Diametro_ID equals rid.Rel_ItemCode_Diametro_ID
                                       join roi in ctx.Sam3_Rel_OrdenRecepcion_ItemCode on rid.Rel_ItemCode_Diametro_ID equals roi.Rel_ItemCode_Diametro_ID
                                       join it in ctx.Sam3_ItemCode on rid.ItemCodeID equals it.ItemCodeID
                                       join o in ctx.Sam3_OrdenRecepcion on roi.OrdenRecepcionID equals o.OrdenRecepcionID
                                       where fc.Activo && rfci.Activo && roi.Activo
                                       && it.TieneComplementoRecepcion == false
                                       && fc.FolioAvisoEntradaID == f.FolioAvisoEntradaID
                                       && fc.FolioCuantificacionID == packingListID
                                       select o).AsParallel().ToList();

                            ordenes.AddRange((from fc in ctx.Sam3_FolioCuantificacion
                                              join b in ctx.Sam3_Bulto on fc.FolioCuantificacionID equals b.FolioCuantificacionID
                                              join rbi in ctx.Sam3_Rel_Bulto_ItemCode on b.BultoID equals rbi.BultoID
                                              join rid in ctx.Sam3_Rel_ItemCode_Diametro on rbi.Rel_ItemCode_Diametro_ID equals rid.Rel_ItemCode_Diametro_ID
                                              join roi in ctx.Sam3_Rel_OrdenRecepcion_ItemCode on rid.Rel_ItemCode_Diametro_ID equals roi.Rel_ItemCode_Diametro_ID
                                              join o in ctx.Sam3_OrdenRecepcion on roi.OrdenRecepcionID equals o.OrdenRecepcionID
                                              join it in ctx.Sam3_ItemCode on rid.ItemCodeID equals it.ItemCodeID
                                              where fc.Activo && rbi.Activo && roi.Activo
                                              && it.TieneComplementoRecepcion == false
                                              && fc.FolioAvisoEntradaID == f.FolioAvisoEntradaID
                                              && fc.FolioCuantificacionID == packingListID
                                              select o).AsParallel().ToList());
                        }
                        else
                        {
                            ordenes = (from fc in ctx.Sam3_FolioCuantificacion
                                       join rfci in ctx.Sam3_Rel_FolioCuantificacion_ItemCode on fc.FolioCuantificacionID equals rfci.FolioCuantificacionID
                                       join rdi in ctx.Sam3_Rel_ItemCode_Diametro on rfci.Rel_ItemCode_Diametro_ID equals rdi.Rel_ItemCode_Diametro_ID
                                       join roi in ctx.Sam3_Rel_OrdenRecepcion_ItemCode on rdi.Rel_ItemCode_Diametro_ID equals roi.Rel_ItemCode_Diametro_ID
                                       join it in ctx.Sam3_ItemCode on rdi.ItemCodeID equals it.ItemCodeID
                                       join o in ctx.Sam3_OrdenRecepcion on roi.OrdenRecepcionID equals o.OrdenRecepcionID
                                       where fc.Activo && rdi.Activo && roi.Activo
                                       && it.TieneComplementoRecepcion == false
                                       && fc.FolioAvisoEntradaID == f.FolioAvisoEntradaID
                                       select o).AsParallel().ToList();

                            ordenes.AddRange((from fc in ctx.Sam3_FolioCuantificacion
                                              join b in ctx.Sam3_Bulto on fc.FolioCuantificacionID equals b.FolioCuantificacionID
                                              join rbi in ctx.Sam3_Rel_Bulto_ItemCode on b.BultoID equals rbi.BultoID
                                              join rid in ctx.Sam3_Rel_ItemCode_Diametro on rbi.Rel_ItemCode_Diametro_ID equals rid.Rel_ItemCode_Diametro_ID
                                              join roi in ctx.Sam3_Rel_OrdenRecepcion_ItemCode on rid.Rel_ItemCode_Diametro_ID equals roi.Rel_ItemCode_Diametro_ID
                                              join o in ctx.Sam3_OrdenRecepcion on roi.OrdenRecepcionID equals o.OrdenRecepcionID
                                              join it in ctx.Sam3_ItemCode on rid.ItemCodeID equals it.ItemCodeID
                                              where fc.Activo && rbi.Activo && roi.Activo
                                              && it.TieneComplementoRecepcion == false
                                              && fc.FolioAvisoEntradaID == f.FolioAvisoEntradaID
                                              select o).AsParallel().ToList());
                        }

                        ordenes = ordenes.GroupBy(x => x.OrdenRecepcionID).Select(x => x.First()).ToList();

                        //solo las ordenes que no se encuentren en el listado
                        if (listado.Count > 0)
                        {
                            ordenes = (from o in ordenes
                                       where !(from l in listado
                                               select l.OrdenRecepcionID).Contains(o.OrdenRecepcionID)
                                       select o).ToList();
                        }

                        foreach (Sam3_OrdenRecepcion orden in ordenes)
                        {
                            elemento = new ListadoNUPorRecepcionar();
                            elemento.OrdenRecepcionID = orden.OrdenRecepcionID;
                            elemento.FechaOrdenRecepcion = orden.FechaCreacion != null ? orden.FechaCreacion.ToString("dd/MM/yyyy") : "";
                            elemento.OrdenRecepcion = activarFolioConfiguracionOR && orden.Rel_Proyecto_Entidad_Configuracion_ID != null ?
                                (from pc in ctx.Sam3_Rel_Proyecto_Entidad_Configuracion
                                 where pc.Rel_Proyecto_Entidad_Configuracion_ID == orden.Rel_Proyecto_Entidad_Configuracion_ID
                                 select pc.PreFijoFolioOrdenRecepcion + ","
                                + pc.CantidadCerosFolioOrdenRecepcion.ToString() + ","
                                + orden.Consecutivo.ToString() + ","
                                + pc.PostFijoFolioOrdenRecepcion).FirstOrDefault()
                                : orden.Folio.ToString();

                            if (activarFolioConfiguracionOR)
                            {
                                string[] elemntos = elemento.OrdenRecepcion.Split(',').ToArray();
                                int digitos = Convert.ToInt32(elemntos[1]);
                                int consecutivo = Convert.ToInt32(elemntos[2]);
                                string formato = "D" + digitos.ToString();

                                elemento.OrdenRecepcion = elemntos[0].Trim() + consecutivo.ToString(formato).Trim() + elemntos[3].Trim();
                            }

                            if (tipoMaterialID == 3) //todos
                            {
                                elemento.CantidadNUEnOrdenRecepcion = (from roi in ctx.Sam3_Rel_OrdenRecepcion_ItemCode
                                                                       join rid in ctx.Sam3_Rel_ItemCode_Diametro on roi.Rel_ItemCode_Diametro_ID equals rid.Rel_ItemCode_Diametro_ID
                                                                       join it in ctx.Sam3_ItemCode on rid.ItemCodeID equals it.ItemCodeID
                                                                       join nu in ctx.Sam3_NumeroUnico on it.ItemCodeID equals nu.ItemCodeID
                                                                       where roi.Activo && nu.Activo
                                                                       && roi.OrdenRecepcionID == orden.OrdenRecepcionID
                                                                       select nu.NumeroUnicoID).AsParallel().Count().ToString();

                                elemento.CantidadNUSinComplemento = (from roi in ctx.Sam3_Rel_OrdenRecepcion_ItemCode
                                                                     join rid in ctx.Sam3_Rel_ItemCode_Diametro on roi.Rel_ItemCode_Diametro_ID equals rid.Rel_ItemCode_Diametro_ID
                                                                     join it in ctx.Sam3_ItemCode on rid.ItemCodeID equals it.ItemCodeID
                                                                     join nu in ctx.Sam3_NumeroUnico on it.ItemCodeID equals nu.ItemCodeID
                                                                     where roi.Activo && nu.Activo && it.Activo
                                                                     && roi.OrdenRecepcionID == orden.OrdenRecepcionID
                                                                     && (nu.EstatusDocumental == null || nu.EstatusDocumental == "" || nu.EstatusFisico == "" || nu.EstatusFisico == null)
                                                                     select nu.NumeroUnicoID).AsParallel().Count().ToString();
                            }
                            else
                            {
                                elemento.CantidadNUEnOrdenRecepcion = (from roi in ctx.Sam3_Rel_OrdenRecepcion_ItemCode
                                                                       join rid in ctx.Sam3_Rel_ItemCode_Diametro on roi.Rel_ItemCode_Diametro_ID equals rid.Rel_ItemCode_Diametro_ID
                                                                       join it in ctx.Sam3_ItemCode on rid.ItemCodeID equals it.ItemCodeID
                                                                       join nu in ctx.Sam3_NumeroUnico on it.ItemCodeID equals nu.ItemCodeID
                                                                       where roi.Activo && nu.Activo
                                                                       && roi.OrdenRecepcionID == orden.OrdenRecepcionID
                                                                       && it.TipoMaterialID == tipoMaterialID
                                                                       select nu.NumeroUnicoID).AsParallel().Count().ToString();

                                elemento.CantidadNUSinComplemento = (from roi in ctx.Sam3_Rel_OrdenRecepcion_ItemCode
                                                                     join rid in ctx.Sam3_Rel_ItemCode_Diametro on roi.Rel_ItemCode_Diametro_ID equals rid.Rel_ItemCode_Diametro_ID
                                                                     join it in ctx.Sam3_ItemCode on rid.ItemCodeID equals it.ItemCodeID
                                                                     join nu in ctx.Sam3_NumeroUnico on it.ItemCodeID equals nu.ItemCodeID
                                                                     where roi.Activo && nu.Activo && it.Activo
                                                                     && roi.OrdenRecepcionID == orden.OrdenRecepcionID
                                                                     && it.TipoMaterialID == tipoMaterialID
                                                                     && (nu.EstatusDocumental == null || nu.EstatusDocumental == "" || nu.EstatusFisico == "" || nu.EstatusFisico == null)
                                                                     select nu.NumeroUnicoID).AsParallel().Count().ToString();
                            }

                            if (int.Parse(elemento.CantidadNUEnOrdenRecepcion) > 0 && int.Parse(elemento.CantidadNUSinComplemento) > 0)
                            {
                                listado.Add(elemento);
                            }
                        }

                    }

#if DEBUG
                    JavaScriptSerializer serializer = new JavaScriptSerializer();
                    string json = serializer.Serialize(listado);
#endif

                    if (conteo)
                    {
                        int porRecepcionarSuma = 0;
                        foreach (ListadoNUPorRecepcionar item in listado)
                        {
                            porRecepcionarSuma = porRecepcionarSuma + Convert.ToInt32(item.CantidadNUSinComplemento);                        
                        }
                        return porRecepcionarSuma;
                    }
                    else
                    {
                        return listado;
                    }
                }
            }
            catch (Exception ex)
            {
                //-----------------Agregar mensaje al Log -----------------------------------------------
                LoggerBd.Instance.EscribirLog(ex);
                //-----------------Agregar mensaje al Log -----------------------------------------------
                TransactionalInformation result = new TransactionalInformation();
                result.ReturnMessage.Add(ex.Message);
                result.ReturnCode = 500;
                result.ReturnStatus = false;
                result.IsAuthenicated = true;

                return result;
            }
        }

        /// <summary>
        /// Genera el set de datos para el gri de Números únicos sin almacenar 
        /// </summary>
        /// <param name="filtros"></param>
        /// <param name="usuario"></param>
        /// <returns></returns>
        public object ListadoNUSinAlmacenaje(FiltrosJson filtros, Sam3_Usuario usuario, bool conteo = false)
        {
            try
            {
                int totalPorAlmacenar = 0;
                using (SamContext ctx = new SamContext())
                {
                    DateTime fechaInicial = new DateTime();
                    DateTime fechaFinal = new DateTime();
                    DateTime.TryParse(filtros.FechaInicial, out fechaInicial);
                    DateTime.TryParse(filtros.FechaFinal, out fechaFinal);
                    Boolean activarFolioConfiguracionOR = !string.IsNullOrEmpty(ConfigurationManager.AppSettings["ActivarFolioConfiguracionOrdenRecepcion"]) ? (ConfigurationManager.AppSettings["ActivarFolioConfiguracionOrdenRecepcion"].Equals("1") ? true : false) : false;
                   

                    if (fechaFinal.ToShortDateString() == "1/1/0001")
                    {
                        fechaFinal = DateTime.Now;
                    }

                    if (fechaInicial.ToShortDateString() == "1/1/0001")
                    {
                        int mes = DateTime.Now.Month != 1 ? DateTime.Now.Month - 1 : 12;
                        int year = DateTime.Now.Month == 1 ? DateTime.Now.Year - 1 : DateTime.Now.Year;
                        fechaInicial = new DateTime(year, mes, DateTime.Now.Day);
                    }

                    int proyectoID = filtros.ProyectoID != "" ? Convert.ToInt32(filtros.ProyectoID) : 0;
                    int clienteID = filtros.ClienteID != "" ? Convert.ToInt32(filtros.ClienteID) : 0;
                    int folioAvisoLlegadaID = filtros.FolioAvisoLlegadaID != null ? Convert.ToInt32(filtros.FolioAvisoLlegadaID) : 0;
                    int packingListID = filtros.PackingListID != "" ? Convert.ToInt32(filtros.PackingListID) : 0;
                    int tipoMaterialID = filtros.TipoMaterialID != "" ? Convert.ToInt32(filtros.TipoMaterialID) : 0;

                    List<int> proyectos = ctx.Sam3_Rel_Usuario_Proyecto.Where(x => x.UsuarioID == usuario.UsuarioID).Select(x => x.ProyectoID).AsParallel().ToList();

                    List<int> patios = (from r in ctx.Sam3_Proyecto
                                        join p in ctx.Sam3_Patio on r.PatioID equals p.PatioID
                                        where r.Activo && proyectos.Contains(r.ProyectoID)
                                        select p.PatioID).AsParallel().Distinct().ToList();

                    List<Sam3_FolioAvisoEntrada> registros;
                    if (proyectoID > 0)
                    {
                        registros = (from fe in ctx.Sam3_FolioAvisoEntrada
                                     join rfp in ctx.Sam3_Rel_FolioAvisoLlegada_Proyecto on fe.FolioAvisoLlegadaID equals rfp.FolioAvisoLlegadaID
                                     join p in ctx.Sam3_Proyecto on rfp.ProyectoID equals p.ProyectoID
                                     where fe.Activo && rfp.Activo && p.Activo
                                     && (fe.FechaCreacion >= fechaInicial && fe.FechaCreacion <= fechaFinal)
                                     && proyectos.Contains(rfp.ProyectoID)
                                     && patios.Contains(p.PatioID)
                                     && rfp.ProyectoID == proyectoID
                                     select fe).AsParallel().ToList();
                    }
                    else
                    {
                        registros = (from fe in ctx.Sam3_FolioAvisoEntrada
                                     join rfp in ctx.Sam3_Rel_FolioAvisoLlegada_Proyecto on fe.FolioAvisoLlegadaID equals rfp.FolioAvisoLlegadaID
                                     join p in ctx.Sam3_Proyecto on rfp.ProyectoID equals p.ProyectoID
                                     where fe.Activo && rfp.Activo && p.Activo
                                     && (fe.FechaCreacion >= fechaInicial && fe.FechaCreacion <= fechaFinal)
                                     && proyectos.Contains(rfp.ProyectoID)
                                     && patios.Contains(p.PatioID)
                                     select fe).AsParallel().ToList();
                    }

                    if (clienteID > 0)
                    {
                        int sam3Cliente = (from c in ctx.Sam3_Cliente
                                           where c.Activo && c.Sam2ClienteID == clienteID
                                           select c.ClienteID).AsParallel().SingleOrDefault();
                        registros = registros.Where(x => x.ClienteID == sam3Cliente).ToList();
                    }

                    if (folioAvisoLlegadaID > 0)
                    {
                        registros = registros.Where(x => x.FolioAvisoLlegadaID == folioAvisoLlegadaID).ToList();
                    }

                    registros = registros.GroupBy(x => x.FolioAvisoLlegadaID).Select(x => x.First()).ToList();

                    List<ListadoNUSinAlmacenar> listado = new List<ListadoNUSinAlmacenar>();
                    ListadoNUSinAlmacenar elemento;
                    List<Sam3_OrdenRecepcion> ordenes;

                    foreach (Sam3_FolioAvisoEntrada f in registros)
                    {
                        if (packingListID > 0)
                        {
                            ordenes = (from o in ctx.Sam3_OrdenRecepcion
                                       join roi in ctx.Sam3_Rel_OrdenRecepcion_ItemCode on o.OrdenRecepcionID equals roi.OrdenRecepcionID
                                       join rid in ctx.Sam3_Rel_ItemCode_Diametro on roi.Rel_ItemCode_Diametro_ID equals rid.Rel_ItemCode_Diametro_ID
                                       join rfci in ctx.Sam3_Rel_FolioCuantificacion_ItemCode on rid.Rel_ItemCode_Diametro_ID equals rfci.Rel_ItemCode_Diametro_ID
                                       join fc in ctx.Sam3_FolioCuantificacion on rfci.FolioCuantificacionID equals fc.FolioCuantificacionID
                                       where o.Activo && roi.Activo && rfci.Activo && fc.Activo && rid.Activo
                                       && fc.FolioAvisoEntradaID == f.FolioAvisoEntradaID
                                       && fc.FolioCuantificacionID == packingListID
                                       select o).AsParallel().ToList();

                            ordenes.AddRange((from o in ctx.Sam3_OrdenRecepcion
                                              join roi in ctx.Sam3_Rel_OrdenRecepcion_ItemCode on o.OrdenRecepcionID equals roi.OrdenRecepcionID
                                              join rid in ctx.Sam3_Rel_ItemCode_Diametro on roi.Rel_ItemCode_Diametro_ID equals rid.Rel_ItemCode_Diametro_ID
                                              join rbi in ctx.Sam3_Rel_Bulto_ItemCode on rid.Rel_ItemCode_Diametro_ID equals rbi.Rel_ItemCode_Diametro_ID
                                              join b in ctx.Sam3_Bulto on rbi.BultoID equals b.BultoID
                                              join fc in ctx.Sam3_FolioCuantificacion on b.FolioCuantificacionID equals fc.FolioCuantificacionID
                                              where o.Activo && roi.Activo && rbi.Activo && fc.Activo && b.Activo && rid.Activo
                                              && fc.FolioAvisoEntradaID == f.FolioAvisoEntradaID
                                              && fc.FolioCuantificacionID == packingListID
                                              select o).AsParallel().ToList());
                        }
                        else
                        {
                            ordenes = (from o in ctx.Sam3_OrdenRecepcion
                                       join roi in ctx.Sam3_Rel_OrdenRecepcion_ItemCode on o.OrdenRecepcionID equals roi.OrdenRecepcionID
                                       join rid in ctx.Sam3_Rel_ItemCode_Diametro on roi.Rel_ItemCode_Diametro_ID equals rid.Rel_ItemCode_Diametro_ID
                                       join rfci in ctx.Sam3_Rel_FolioCuantificacion_ItemCode on rid.Rel_ItemCode_Diametro_ID equals rfci.Rel_ItemCode_Diametro_ID
                                       join fc in ctx.Sam3_FolioCuantificacion on rfci.FolioCuantificacionID equals fc.FolioCuantificacionID
                                       where o.Activo && roi.Activo && rfci.Activo && fc.Activo && rid.Activo
                                       && fc.FolioAvisoEntradaID == f.FolioAvisoEntradaID
                                       select o).AsParallel().ToList();

                            ordenes.AddRange((from o in ctx.Sam3_OrdenRecepcion
                                              join roi in ctx.Sam3_Rel_OrdenRecepcion_ItemCode on o.OrdenRecepcionID equals roi.OrdenRecepcionID
                                              join rid in ctx.Sam3_Rel_ItemCode_Diametro on roi.Rel_ItemCode_Diametro_ID equals rid.Rel_ItemCode_Diametro_ID
                                              join rbi in ctx.Sam3_Rel_Bulto_ItemCode on rid.Rel_ItemCode_Diametro_ID equals rbi.Rel_ItemCode_Diametro_ID
                                              join b in ctx.Sam3_Bulto on rbi.BultoID equals b.BultoID
                                              join fc in ctx.Sam3_FolioCuantificacion on b.FolioCuantificacionID equals fc.FolioCuantificacionID
                                              where o.Activo && roi.Activo && rbi.Activo && fc.Activo && b.Activo && rid.Activo
                                              && fc.FolioAvisoEntradaID == f.FolioAvisoEntradaID
                                              select o).AsParallel().ToList());
                        }

                        ordenes = ordenes.GroupBy(x => x.OrdenRecepcionID).Select(x => x.First()).ToList();

                        //solo tomar en cuenta las ordenes que aun no se encuentran en el listado
                        if (listado.Count > 0)
                        {
                            ordenes = (from r in ordenes
                                       where !(from l in listado select l.OrdenRecepcionID).Contains(r.OrdenRecepcionID)
                                       select r).ToList(); 
                        }

                        foreach (Sam3_OrdenRecepcion orden in ordenes)
                        {
                            elemento = new ListadoNUSinAlmacenar();
                            elemento.OrdenRecepcionID = orden.OrdenRecepcionID;
                            elemento.FechaOrdenRecepcion = orden.FechaCreacion != null ? orden.FechaCreacion.ToString("dd/MM/yyyy") : "";
                            elemento.OrdenRecepcion = orden.Folio.ToString();


                            elemento.OrdenRecepcion = activarFolioConfiguracionOR ? orden.Rel_Proyecto_Entidad_Configuracion_ID != null ?
                                                            (from pc in ctx.Sam3_Rel_Proyecto_Entidad_Configuracion
                                                             where pc.Rel_Proyecto_Entidad_Configuracion_ID == orden.Rel_Proyecto_Entidad_Configuracion_ID
                                                             select pc.PreFijoFolioOrdenRecepcion + ","
                                                            + pc.CantidadCerosFolioOrdenRecepcion.ToString() + ","
                                                            + orden.Consecutivo.ToString() + ","
                                                            + pc.PostFijoFolioOrdenRecepcion).FirstOrDefault()
                                                            : orden.Folio.ToString() : orden.Folio.ToString();

                            if (activarFolioConfiguracionOR && orden.Rel_Proyecto_Entidad_Configuracion_ID != null)
                            {
                                string[] elemntos = elemento.OrdenRecepcion.Split(',').ToArray();
                                int digitos = Convert.ToInt32(elemntos[1]);
                                int consecutivo = Convert.ToInt32(elemntos[2]);
                                string formato = "D" + digitos.ToString();

                                elemento.OrdenRecepcion = elemntos[0].Trim() + consecutivo.ToString(formato).Trim() + elemntos[3].Trim();
                            }

                            int cantidadNUEnOR = 0;
                            int cantidadNuPorAlmacenar = 0;
                            if (tipoMaterialID == 3)
                            {
                                cantidadNUEnOR = (from roi in ctx.Sam3_Rel_OrdenRecepcion_ItemCode
                                                  where roi.OrdenRecepcionID == orden.OrdenRecepcionID
                                                  select roi.Cantidad.Value).AsParallel().Sum();

                                cantidadNuPorAlmacenar = (from rnuf in ctx.Sam3_Rel_NumeroUnico_RelFC_RelB
                                                          join nu in ctx.Sam3_NumeroUnico on rnuf.NumeroUnicoID equals nu.NumeroUnicoID
                                                          where rnuf.Activo && nu.Activo 
                                                          && rnuf.OrdenRecepcionID == orden.OrdenRecepcionID
                                                          && (nu.Rack != null || nu.Rack != string.Empty || nu.Rack != "")
                                                          select rnuf.NumeroUnicoID).AsParallel().Count();
                            }
                            else 
                            {
                                cantidadNUEnOR = (from roi in ctx.Sam3_Rel_OrdenRecepcion_ItemCode
                                                  join rit in ctx.Sam3_Rel_ItemCode_Diametro on roi.Rel_ItemCode_Diametro_ID equals rit.Rel_ItemCode_Diametro_ID
                                                  join it in ctx.Sam3_ItemCode on rit.ItemCodeID equals it.ItemCodeID
                                                  where roi.Activo && rit.Activo && it.Activo 
                                                  && roi.OrdenRecepcionID == orden.OrdenRecepcionID
                                                  && it.TipoMaterialID == tipoMaterialID
                                                  select roi.Cantidad.Value).AsParallel().Sum();

                                cantidadNuPorAlmacenar = (from rnuf in ctx.Sam3_Rel_NumeroUnico_RelFC_RelB
                                                          join nu in ctx.Sam3_NumeroUnico on rnuf.NumeroUnicoID equals nu.NumeroUnicoID
                                                          join it in ctx.Sam3_ItemCode on nu.ItemCodeID equals it.ItemCodeID
                                                          where rnuf.Activo && nu.Activo && it.Activo
                                                          && rnuf.OrdenRecepcionID == orden.OrdenRecepcionID
                                                          && (nu.Rack != null || nu.Rack != string.Empty || nu.Rack != "")
                                                          && it.TipoMaterialID == tipoMaterialID
                                                          select rnuf.NumeroUnicoID).AsParallel().Count();
                            }

                            elemento.CantidadNUEnOrdenRecepcion = cantidadNUEnOR.ToString();

                            elemento.CantidadNUporAlmacenar = cantidadNuPorAlmacenar.ToString();

                            if (cantidadNUEnOR > 0 && cantidadNuPorAlmacenar > 0 )
                            {
                                listado.Add(elemento);
                            };
                        }
                    }

#if DEBUG
                    JavaScriptSerializer serializer = new JavaScriptSerializer();
                    string json = serializer.Serialize(listado);
#endif
                    if (conteo)
                    {
                        totalPorAlmacenar = listado.Sum(x => int.Parse(x.CantidadNUporAlmacenar));
                        return totalPorAlmacenar;
                    }
                    else
                    {
                        return listado;
                    }
                }
            }
            catch (Exception ex)
            {
                //-----------------Agregar mensaje al Log -----------------------------------------------
                LoggerBd.Instance.EscribirLog(ex);
                //-----------------Agregar mensaje al Log -----------------------------------------------
                TransactionalInformation result = new TransactionalInformation();
                result.ReturnMessage.Add(ex.Message);
                result.ReturnCode = 500;
                result.ReturnStatus = false;
                result.IsAuthenicated = true;

                return result;
            }
        }

        /// <summary>
        /// Genera el set de datos para el gri de incidencias activas por número único
        /// </summary>
        /// <param name="filtros"></param>
        /// <param name="usuario"></param>
        /// <returns></returns>
        public object ListadoIncidenciasActivas(FiltrosJson filtros, Sam3_Usuario usuario, bool conteo = false)
        {
            try
            {
                using (SamContext ctx = new SamContext())
                {
                    DateTime fechaInicial = new DateTime();
                    DateTime fechaFinal = new DateTime();
                    DateTime.TryParse(filtros.FechaInicial, out fechaInicial);
                    DateTime.TryParse(filtros.FechaFinal, out fechaFinal);

                    if (fechaFinal.ToShortDateString() == "1/1/0001")
                    {
                        fechaFinal = DateTime.Now;
                    }

                    if (fechaInicial.ToShortDateString() == "1/1/0001")
                    {
                        int mes = DateTime.Now.Month != 1 ? DateTime.Now.Month - 1 : 12;
                        int year = DateTime.Now.Month == 1 ? DateTime.Now.Year - 1 : DateTime.Now.Year;
                        fechaInicial = new DateTime(year, mes, DateTime.Now.Day);
                    }

                    int proyectoID = filtros.ProyectoID != "" ? Convert.ToInt32(filtros.ProyectoID) : 0;
                    int clienteID = filtros.ClienteID != "" ? Convert.ToInt32(filtros.ClienteID) : 0;
                    int folioAvisoLlegadaID = filtros.FolioAvisoLlegadaID != null ? Convert.ToInt32(filtros.FolioAvisoLlegadaID) : 0;
                    int packingListID = filtros.PackingListID != "" ? Convert.ToInt32(filtros.PackingListID) : 0;
                    int tipoMaterialID = filtros.TipoMaterialID != "" ? Convert.ToInt32(filtros.TipoMaterialID) : 0;

                    List<int> proyectos = ctx.Sam3_Rel_Usuario_Proyecto.Where(x => x.UsuarioID == usuario.UsuarioID).Select(x => x.ProyectoID).AsParallel().ToList();

                    List<int> patios = (from r in ctx.Sam3_Proyecto
                                        join p in ctx.Sam3_Patio on r.PatioID equals p.PatioID
                                        where r.Activo && proyectos.Contains(r.ProyectoID)
                                        select p.PatioID).AsParallel().Distinct().ToList();

                    List<Sam3_Incidencia> registros;
                    if (proyectoID > 0)
                    {
                        registros = (from i in ctx.Sam3_Incidencia
                                     join rin in ctx.Sam3_Rel_Incidencia_NumeroUnico on i.IncidenciaID equals rin.IncidenciaID
                                     join nu in ctx.Sam3_NumeroUnico on rin.NumeroUnicoID equals nu.NumeroUnicoID
                                     join p in ctx.Sam3_Proyecto on nu.ProyectoID equals p.ProyectoID
                                     where i.Activo && rin.Activo && nu.Activo && p.Activo
                                     && (i.FechaCreacion >= fechaInicial && i.FechaCreacion <= fechaFinal)
                                     && proyectos.Contains(nu.ProyectoID)
                                     && patios.Contains(p.PatioID)
                                     && nu.ProyectoID == proyectoID
                                     select i).AsParallel().ToList();
                    }
                    else
                    {
                        registros = (from i in ctx.Sam3_Incidencia
                                     join rin in ctx.Sam3_Rel_Incidencia_NumeroUnico on i.IncidenciaID equals rin.IncidenciaID
                                     join nu in ctx.Sam3_NumeroUnico on rin.NumeroUnicoID equals nu.NumeroUnicoID
                                     join p in ctx.Sam3_Proyecto on nu.ProyectoID equals p.ProyectoID
                                     where i.Activo && rin.Activo && nu.Activo && p.Activo
                                     && (i.FechaCreacion >= fechaInicial && i.FechaCreacion <= fechaFinal)
                                     && proyectos.Contains(nu.ProyectoID)
                                     && patios.Contains(p.PatioID)
                                     select i).AsParallel().ToList();
                    }

                    //if (clienteID > 0)
                    //{
                    //    registros = registros.Where(x => x.ClienteID == clienteID).ToList();
                    //}

                    //if (folioAvisoLlegadaID > 0)
                    //{
                    //    registros = registros.Where(x => x.FolioAvisoLlegadaID == folioAvisoLlegadaID).ToList();
                    //}

                    registros = registros.GroupBy(x => x.IncidenciaID).Select(x => x.First()).ToList();

                    List<ListadoIncidenciasAbiertas> listado = new List<ListadoIncidenciasAbiertas>();
                    ListadoIncidenciasAbiertas elemento;
                    List<Sam3_NumeroUnico> numerosUnicos;

                    foreach (Sam3_Incidencia f in registros)
                    {
                        if (packingListID > 0)
                        {
                            if (tipoMaterialID == 3)
                            {
                                numerosUnicos = (from nu in ctx.Sam3_NumeroUnico
                                                 join rin in ctx.Sam3_Rel_Incidencia_NumeroUnico on nu.NumeroUnicoID equals rin.NumeroUnicoID
                                                 join it in ctx.Sam3_ItemCode on nu.ItemCodeID equals it.ItemCodeID
                                                 join rid in ctx.Sam3_Rel_ItemCode_Diametro on it.ItemCodeID equals rid.ItemCodeID
                                                 join rfi in ctx.Sam3_Rel_FolioCuantificacion_ItemCode on rid.Rel_ItemCode_Diametro_ID equals rfi.Rel_ItemCode_Diametro_ID
                                                 where nu.Activo && rin.Activo && it.Activo && rfi.Activo && rid.Activo
                                                 && rfi.FolioCuantificacionID == packingListID
                                                 && rin.IncidenciaID == f.IncidenciaID
                                                 select nu).AsParallel().ToList();

                                numerosUnicos.AddRange((from nu in ctx.Sam3_NumeroUnico
                                                        join rin in ctx.Sam3_Rel_Incidencia_NumeroUnico on nu.NumeroUnicoID equals rin.NumeroUnicoID
                                                        join it in ctx.Sam3_ItemCode on nu.ItemCodeID equals it.ItemCodeID
                                                        join rid in ctx.Sam3_Rel_ItemCode_Diametro on it.ItemCodeID equals rid.ItemCodeID
                                                        join rbi in ctx.Sam3_Rel_Bulto_ItemCode on rid.Rel_ItemCode_Diametro_ID equals rbi.Rel_ItemCode_Diametro_ID
                                                        join b in ctx.Sam3_Bulto on rbi.BultoID equals b.BultoID
                                                        where nu.Activo && rin.Activo && it.Activo && rbi.Activo && rid.Activo
                                                        && b.FolioCuantificacionID == packingListID
                                                        && rin.IncidenciaID == f.IncidenciaID
                                                        select nu).AsParallel().ToList());
                            }
                            else
                            {
                                numerosUnicos = (from nu in ctx.Sam3_NumeroUnico
                                                 join rin in ctx.Sam3_Rel_Incidencia_NumeroUnico on nu.NumeroUnicoID equals rin.NumeroUnicoID
                                                 join it in ctx.Sam3_ItemCode on nu.ItemCodeID equals it.ItemCodeID
                                                 join rid in ctx.Sam3_Rel_ItemCode_Diametro on it.ItemCodeID equals rid.ItemCodeID
                                                 join rfi in ctx.Sam3_Rel_FolioCuantificacion_ItemCode on rid.Rel_ItemCode_Diametro_ID equals rfi.Rel_ItemCode_Diametro_ID
                                                 where nu.Activo && rin.Activo && it.Activo && rfi.Activo && rid.Activo
                                                 && rfi.FolioCuantificacionID == packingListID
                                                 && it.TipoMaterialID == tipoMaterialID
                                                 && rin.IncidenciaID == f.IncidenciaID
                                                 select nu).AsParallel().ToList();

                                numerosUnicos.AddRange((from nu in ctx.Sam3_NumeroUnico
                                                        join rin in ctx.Sam3_Rel_Incidencia_NumeroUnico on nu.NumeroUnicoID equals rin.NumeroUnicoID
                                                        join it in ctx.Sam3_ItemCode on nu.ItemCodeID equals it.ItemCodeID
                                                        join rid in ctx.Sam3_Rel_ItemCode_Diametro on it.ItemCodeID equals rid.ItemCodeID
                                                        join rbi in ctx.Sam3_Rel_Bulto_ItemCode on rid.Rel_ItemCode_Diametro_ID equals rbi.Rel_ItemCode_Diametro_ID
                                                        join b in ctx.Sam3_Bulto on rbi.BultoID equals b.BultoID
                                                        where nu.Activo && rin.Activo && it.Activo && rbi.Activo && rid.Activo
                                                        && b.FolioCuantificacionID == packingListID
                                                        && it.TipoMaterialID == tipoMaterialID
                                                        && rin.IncidenciaID == f.IncidenciaID
                                                        select nu).AsParallel().ToList());
                            }
                        }
                        else
                        {
                            if (tipoMaterialID == 3)
                            {
                                numerosUnicos = (from nu in ctx.Sam3_NumeroUnico
                                                 join rin in ctx.Sam3_Rel_Incidencia_NumeroUnico on nu.NumeroUnicoID equals rin.NumeroUnicoID
                                                 join it in ctx.Sam3_ItemCode on nu.ItemCodeID equals it.ItemCodeID
                                                 join rid in ctx.Sam3_Rel_ItemCode_Diametro on it.ItemCodeID equals rid.ItemCodeID
                                                 join rfi in ctx.Sam3_Rel_FolioCuantificacion_ItemCode on rid.Rel_ItemCode_Diametro_ID equals rfi.Rel_ItemCode_Diametro_ID
                                                 where nu.Activo && rin.Activo && it.Activo && rfi.Activo && rid.Activo
                                                 && rin.IncidenciaID == f.IncidenciaID
                                                 select nu).AsParallel().ToList();

                                numerosUnicos.AddRange((from nu in ctx.Sam3_NumeroUnico
                                                        join rin in ctx.Sam3_Rel_Incidencia_NumeroUnico on nu.NumeroUnicoID equals rin.NumeroUnicoID
                                                        join it in ctx.Sam3_ItemCode on nu.ItemCodeID equals it.ItemCodeID
                                                        join rid in ctx.Sam3_Rel_ItemCode_Diametro on it.ItemCodeID equals rid.ItemCodeID
                                                        join rbi in ctx.Sam3_Rel_Bulto_ItemCode on rid.Rel_ItemCode_Diametro_ID equals rbi.Rel_ItemCode_Diametro_ID
                                                        join b in ctx.Sam3_Bulto on rbi.BultoID equals b.BultoID
                                                        where nu.Activo && rin.Activo && it.Activo && rbi.Activo
                                                        && rin.IncidenciaID == f.IncidenciaID
                                                        select nu).AsParallel().ToList());
                            }
                            else
                            {
                                numerosUnicos = (from nu in ctx.Sam3_NumeroUnico
                                                 join rin in ctx.Sam3_Rel_Incidencia_NumeroUnico on nu.NumeroUnicoID equals rin.NumeroUnicoID
                                                 join it in ctx.Sam3_ItemCode on nu.ItemCodeID equals it.ItemCodeID
                                                 join rid in ctx.Sam3_Rel_ItemCode_Diametro on it.ItemCodeID equals rid.ItemCodeID
                                                 join rfi in ctx.Sam3_Rel_FolioCuantificacion_ItemCode on rid.Rel_ItemCode_Diametro_ID equals rfi.Rel_ItemCode_Diametro_ID
                                                 where nu.Activo && rin.Activo && it.Activo && rfi.Activo && rid.Activo
                                                 && it.TipoMaterialID == tipoMaterialID
                                                 && rin.IncidenciaID == f.IncidenciaID
                                                 select nu).AsParallel().ToList();

                                numerosUnicos.AddRange((from nu in ctx.Sam3_NumeroUnico
                                                        join rin in ctx.Sam3_Rel_Incidencia_NumeroUnico on nu.NumeroUnicoID equals rin.NumeroUnicoID
                                                        join it in ctx.Sam3_ItemCode on nu.ItemCodeID equals it.ItemCodeID
                                                        join rid in ctx.Sam3_Rel_ItemCode_Diametro on it.ItemCodeID equals rid.ItemCodeID
                                                        join rbi in ctx.Sam3_Rel_Bulto_ItemCode on rid.Rel_ItemCode_Diametro_ID equals rbi.Rel_ItemCode_Diametro_ID
                                                        join b in ctx.Sam3_Bulto on rbi.BultoID equals b.BultoID
                                                        where nu.Activo && rin.Activo && it.Activo && rbi.Activo
                                                        && rin.IncidenciaID==f.IncidenciaID
                                                        && it.TipoMaterialID == tipoMaterialID
                                                        select nu).AsParallel().ToList());
                            }
                        }

                        numerosUnicos = numerosUnicos.GroupBy(x => x.NumeroUnicoID).Select(x => x.First()).ToList();

                        foreach (Sam3_NumeroUnico numUnico in numerosUnicos)
                        {
                            Sam3_ProyectoConfiguracion config = ctx.Sam3_ProyectoConfiguracion.Where(x => x.ProyectoID == numUnico.ProyectoID)
                                .AsParallel().SingleOrDefault();

                            string formato = "D" + config.DigitosNumeroUnico;

                            elemento = new ListadoIncidenciasAbiertas();
                            elemento.NumeroUnico = numUnico.Prefijo + "-" + numUnico.Consecutivo.ToString(formato);
                            elemento.NumeroUnicoID = numUnico.NumeroUnicoID.ToString();
                            elemento.CantidadIncidencias = (from rinu in ctx.Sam3_Rel_Incidencia_NumeroUnico
                                                            join inc in ctx.Sam3_Incidencia on rinu.IncidenciaID equals inc.IncidenciaID
                                                            join nu in ctx.Sam3_NumeroUnico on rinu.NumeroUnicoID equals nu.NumeroUnicoID
                                                            join it in ctx.Sam3_ItemCode on nu.ItemCodeID equals it.ItemCodeID
                                                            where rinu.Activo
                                                            && rinu.NumeroUnicoID == numUnico.NumeroUnicoID
                                                            && inc.Activo
                                                            select rinu.IncidenciaID).AsParallel().Count().ToString();


                            listado.Add(elemento);
                        }
                    }

                    listado = listado.GroupBy(x => x.NumeroUnico).Select(x => x.First()).ToList();

#if DEBUG
                    JavaScriptSerializer serializer = new JavaScriptSerializer();
                    string json = serializer.Serialize(listado);
#endif
                    if (conteo)
                    {
                        return listado.Count();
                    }
                    else
                    {
                        return listado;
                    }
                }
            }
            catch (Exception ex)
            {
                //-----------------Agregar mensaje al Log -----------------------------------------------
                LoggerBd.Instance.EscribirLog(ex);
                //-----------------Agregar mensaje al Log -----------------------------------------------
                TransactionalInformation result = new TransactionalInformation();
                result.ReturnMessage.Add(ex.Message);
                result.ReturnCode = 500;
                result.ReturnStatus = false;
                result.IsAuthenicated = true;

                return result;
            }
        }

        /// <summary>
        /// Genera un listado para combo de los folios de entrada que aun no cuantan con cuantificación
        /// </summary>
        /// <param name="proyectoID"></param>
        /// <param name="usuario"></param>
        /// <returns></returns>
        public object ObtenerFoliosEntradaPorProyecto(int proyectoID, Sam3_Usuario usuario)
        {
            try
            {
                using (SamContext ctx = new SamContext())
                {
                    Boolean activarFolioConfiguracion = !string.IsNullOrEmpty(ConfigurationManager.AppSettings["ActivarFolioConfiguracion"]) ? 
                        (ConfigurationManager.AppSettings["ActivarFolioConfiguracion"].Equals("1") ? true : false) : false;

                    List<ListaCombos> folios = (from fe in ctx.Sam3_FolioAvisoEntrada
                                                join rfp in ctx.Sam3_Rel_FolioAvisoLlegada_Proyecto on fe.FolioAvisoLlegadaID equals rfp.FolioAvisoLlegadaID
                                                join fc in ctx.Sam3_FolioCuantificacion on fe.FolioAvisoEntradaID equals fc.FolioAvisoEntradaID
                                                join rfi in ctx.Sam3_Rel_FolioCuantificacion_ItemCode on fc.FolioCuantificacionID equals rfi.FolioCuantificacionID
                                                where fe.Activo && rfp.Activo && fc.Activo && rfi.Activo
                                                && rfp.ProyectoID == proyectoID
                                                && !(from rel in ctx.Sam3_Rel_NumeroUnico_RelFC_RelB
                                                     where rel.Activo 
                                                     && rel.Rel_FolioCuantificacion_ItemCode_ID == rfi.Rel_FolioCuantificacion_ItemCode_ID
                                                     select rel).Any()
                                                select new ListaCombos
                                                {
                                                    id = fe.FolioAvisoLlegadaID.ToString(),
                                                    value = fe.FolioAvisoLlegadaID.ToString()
                                                }).AsParallel().ToList();

                    //Agregar folios que tienen pendientes en bultos
                    folios.AddRange((from fe in ctx.Sam3_FolioAvisoEntrada
                                     join rfp in ctx.Sam3_Rel_FolioAvisoLlegada_Proyecto on fe.FolioAvisoLlegadaID equals rfp.FolioAvisoLlegadaID
                                     join fc in ctx.Sam3_FolioCuantificacion on fe.FolioAvisoEntradaID equals fc.FolioAvisoEntradaID
                                     join b in ctx.Sam3_Bulto on fc.FolioCuantificacionID equals b.FolioCuantificacionID
                                     join rbi in ctx.Sam3_Rel_Bulto_ItemCode on b.BultoID equals rbi.BultoID
                                     where fe.Activo && rfp.Activo && fc.Activo && rbi.Activo
                                     && rfp.ProyectoID == proyectoID
                                     && !(from rel in ctx.Sam3_Rel_NumeroUnico_RelFC_RelB
                                          where rel.Activo
                                          && rel.Rel_Bulto_ItemCode_ID == rbi.Rel_Bulto_ItemCode_ID
                                          select rel).Any()
                                     select new ListaCombos
                                     {
                                         id = fe.FolioAvisoLlegadaID.ToString(),
                                         value = fe.FolioAvisoLlegadaID.ToString()
                                     }).AsParallel().ToList());

                    if (activarFolioConfiguracion)
                    {
                        foreach (ListaCombos item in folios)
                        {
                            int folioAvisoLlegadaID = Convert.ToInt32(item.id);
                            Sam3_FolioAvisoLlegada FolioAvisoLlegada = ctx.Sam3_FolioAvisoLlegada.Where(x => x.FolioAvisoLlegadaID == folioAvisoLlegadaID).FirstOrDefault();

                            item.value = (from pc in ctx.Sam3_Rel_Proyecto_Entidad_Configuracion
                                          where pc.Entidad == FolioAvisoLlegada.Entidad && pc.Proyecto == FolioAvisoLlegada.ProyectoNombrado
                                          select pc.PreFijoFolioAvisoLlegada + ","
                                           + pc.CantidadCerosFolioAvisoLlegada.ToString() + ","
                                           + FolioAvisoLlegada.Consecutivo.ToString() + ","
                                           + pc.PostFijoFolioAvisoLlegada).FirstOrDefault();

                            string[] elemntos = item.value.Split(',').ToArray();
                            int digitos = Convert.ToInt32(elemntos[1]);
                            int consecutivo = Convert.ToInt32(elemntos[2]);
                            string formato = "D" + digitos.ToString();

                            item.value = elemntos[0].Trim() + consecutivo.ToString(formato).Trim() + elemntos[3].Trim();
                        }
                    }
                    folios = folios.GroupBy(x => x.id).Select(x => x.First()).ToList();

                    return folios;

                }
            }
            catch (Exception ex)
            {
                //-----------------Agregar mensaje al Log -----------------------------------------------
                LoggerBd.Instance.EscribirLog(ex);
                //-----------------Agregar mensaje al Log -----------------------------------------------
                TransactionalInformation result = new TransactionalInformation();
                result.ReturnMessage.Add(ex.Message);
                result.ReturnCode = 500;
                result.ReturnStatus = false;
                result.IsAuthenicated = true;

                return result;
            }
        }

        /// <summary>
        /// Genera un listado para combo con los ItemCodes de un Aviso de entrada
        /// </summary>
        /// <param name="folioAvisoLlegada"></param>
        /// <param name="usuario"></param>
        /// <returns></returns>
        public object ObtenerItemCodesPorFolioLlegada(int folioAvisoLlegada, Sam3_Usuario usuario)
        {
            try
            {
                using (SamContext ctx = new SamContext())
                {
                    List<ListaCombos> listado = (from fe in ctx.Sam3_FolioAvisoEntrada
                                                 join fc in ctx.Sam3_FolioCuantificacion on fe.FolioAvisoEntradaID equals fc.FolioAvisoEntradaID
                                                 join rfi in ctx.Sam3_Rel_FolioCuantificacion_ItemCode on fc.FolioCuantificacionID equals rfi.FolioCuantificacionID
                                                 join rid in ctx.Sam3_Rel_ItemCode_Diametro on rfi.Rel_ItemCode_Diametro_ID equals rid.Rel_ItemCode_Diametro_ID
                                                 join it in ctx.Sam3_ItemCode on rid.ItemCodeID equals it.ItemCodeID
                                                 where fe.Activo && fc.Activo && rfi.Activo && it.Activo
                                                 && fe.FolioAvisoLlegadaID == folioAvisoLlegada
                                                 && !(from rel in ctx.Sam3_Rel_NumeroUnico_RelFC_RelB
                                                      where rel.Rel_FolioCuantificacion_ItemCode_ID == rfi.Rel_FolioCuantificacion_ItemCode_ID
                                                      && rel.Activo
                                                      select rel).Any()
                                                 select new ListaCombos
                                                 {
                                                     id = it.ItemCodeID.ToString(),
                                                     value = it.Codigo
                                                 }).AsParallel().ToList();

                    //incluir los itemcodes que se encuentran en bultos
                    listado.AddRange((from fe in ctx.Sam3_FolioAvisoEntrada
                                      join fc in ctx.Sam3_FolioCuantificacion on fe.FolioAvisoEntradaID equals fc.FolioAvisoEntradaID
                                      join b in ctx.Sam3_Bulto on fc.FolioCuantificacionID equals b.FolioCuantificacionID
                                      join rbi in ctx.Sam3_Rel_Bulto_ItemCode on b.BultoID equals rbi.BultoID
                                      join rid in ctx.Sam3_Rel_ItemCode_Diametro on rbi.Rel_ItemCode_Diametro_ID equals rid.Rel_ItemCode_Diametro_ID
                                      join it in ctx.Sam3_ItemCode on rid.ItemCodeID equals it.ItemCodeID
                                      where fe.Activo && fc.Activo && rbi.Activo && it.Activo
                                      && fe.FolioAvisoLlegadaID == folioAvisoLlegada
                                      && !(from rel in ctx.Sam3_Rel_NumeroUnico_RelFC_RelB
                                           where rel.Rel_Bulto_ItemCode_ID == rbi.Rel_Bulto_ItemCode_ID
                                           && rel.Activo
                                           select rel).Any()
                                      select new ListaCombos
                                      {
                                          id = it.ItemCodeID.ToString(),
                                          value = it.Codigo
                                      }).AsParallel().ToList());

                    listado = listado.GroupBy(x => x.id).Select(x => x.First()).ToList();

#if DEBUG
                    JavaScriptSerializer serializer = new JavaScriptSerializer();
                    string json = serializer.Serialize(listado);
#endif
                    return listado;
                }
            }
            catch (Exception ex)
            {
                //-----------------Agregar mensaje al Log -----------------------------------------------
                LoggerBd.Instance.EscribirLog(ex);
                //-----------------Agregar mensaje al Log -----------------------------------------------
                TransactionalInformation result = new TransactionalInformation();
                result.ReturnMessage.Add(ex.Message);
                result.ReturnCode = 500;
                result.ReturnStatus = false;
                result.IsAuthenicated = true;

                return result;
            }
        }

        /// <summary>
        /// Genera el set de datos para el grid de listado de Packing List
        /// </summary>
        /// <param name="filtros"></param>
        /// <param name="usuario"></param>
        /// <returns></returns>
        public object ListadoPackingList(FiltrosJson filtros, Sam3_Usuario usuario, bool conteo = false)
        {
            try
            {
                using (SamContext ctx = new SamContext())
                {
                    DateTime fechaInicial = new DateTime();
                    DateTime fechaFinal = new DateTime();
                    DateTime.TryParse(filtros.FechaInicial, out fechaInicial);
                    DateTime.TryParse(filtros.FechaFinal, out fechaFinal);
                    Boolean activarFolioConfiguracion = !string.IsNullOrEmpty(ConfigurationManager.AppSettings["ActivarFolioConfiguracion"]) ? (ConfigurationManager.AppSettings["ActivarFolioConfiguracion"].Equals("1") ? true : false) : false;
                    Boolean activarFolioConfiguracionCuantificacion = !string.IsNullOrEmpty(ConfigurationManager.AppSettings["ActivarFolioConfiguracionCuantificacion"]) ? (ConfigurationManager.AppSettings["ActivarFolioConfiguracionCuantificacion"].Equals("1") ? true : false) : false;

                    if (fechaFinal.ToShortDateString() == "1/1/0001")
                    {
                        fechaFinal = DateTime.Now;
                    }

                    if (fechaInicial.ToShortDateString() == "1/1/0001")
                    {
                        int mes = DateTime.Now.Month != 1 ? DateTime.Now.Month - 1 : 12;
                        int year = DateTime.Now.Month == 1 ? DateTime.Now.Year - 1 : DateTime.Now.Year;
                        fechaInicial = new DateTime(year, mes, DateTime.Now.Day);
                    }

                    int proyectoID = filtros.ProyectoID != "" ? Convert.ToInt32(filtros.ProyectoID) : 0;
                    int clienteID = filtros.ClienteID != "" ? Convert.ToInt32(filtros.ClienteID) : 0;
                    int folioAvisoLlegadaID = string.IsNullOrEmpty(filtros.FolioAvisoLlegadaID) ? 0 : Convert.ToInt32(filtros.FolioAvisoLlegadaID);
                    int packingListID = filtros.PackingListID != "" ? Convert.ToInt32(filtros.PackingListID) : 0;
                    int tipoMaterialID = filtros.TipoMaterialID != "" ? Convert.ToInt32(filtros.TipoMaterialID) : 0;

                    List<int> proyectos = ctx.Sam3_Rel_Usuario_Proyecto.Where(x => x.UsuarioID == usuario.UsuarioID).Select(x => x.ProyectoID).AsParallel().ToList();

                    List<int> patios = (from r in ctx.Sam3_Proyecto
                                        join p in ctx.Sam3_Patio on r.PatioID equals p.PatioID
                                        where r.Activo && proyectos.Contains(r.ProyectoID)
                                        select p.PatioID).AsParallel().Distinct().ToList();

                    List<Sam3_FolioAvisoEntrada> registros;
                    if (proyectoID > 0)
                    {
                        registros = (from fe in ctx.Sam3_FolioAvisoEntrada
                                     join rfp in ctx.Sam3_Rel_FolioAvisoLlegada_Proyecto on fe.FolioAvisoLlegadaID equals rfp.FolioAvisoLlegadaID
                                     join p in ctx.Sam3_Proyecto on rfp.ProyectoID equals p.ProyectoID
                                     where fe.Activo && rfp.Activo && p.Activo
                                     && (fe.FechaCreacion >= fechaInicial && fe.FechaCreacion <= fechaFinal)
                                     && proyectos.Contains(rfp.ProyectoID)
                                     && patios.Contains(p.PatioID)
                                     && fe.FolioDescarga > 0
                                     && rfp.ProyectoID == proyectoID
                                     select fe).AsParallel().ToList();
                    }
                    else
                    {
                        registros = (from fe in ctx.Sam3_FolioAvisoEntrada
                                     join rfp in ctx.Sam3_Rel_FolioAvisoLlegada_Proyecto on fe.FolioAvisoLlegadaID equals rfp.FolioAvisoLlegadaID
                                     join p in ctx.Sam3_Proyecto on rfp.ProyectoID equals p.ProyectoID
                                     where fe.Activo && rfp.Activo && p.Activo
                                     && (fe.FechaCreacion >= fechaInicial && fe.FechaCreacion <= fechaFinal)
                                     && proyectos.Contains(rfp.ProyectoID)
                                     && patios.Contains(p.PatioID)
                                     && fe.FolioDescarga > 0
                                     select fe).AsParallel().ToList();
                    }

                    if (clienteID > 0)
                    {
                        int sam3Cliente = (from c in ctx.Sam3_Cliente
                                           where c.Activo && c.Sam2ClienteID == clienteID
                                           select c.ClienteID).AsParallel().SingleOrDefault();
                        registros = registros.Where(x => x.ClienteID == sam3Cliente).ToList();
                    }

                    if (folioAvisoLlegadaID > 0)
                    {
                        registros = registros.Where(x => x.FolioAvisoLlegadaID == folioAvisoLlegadaID).ToList();
                    }

                    registros = registros.GroupBy(x => x.FolioAvisoLlegadaID).Select(x => x.First()).ToList();

                    List<ListadoPkList> listado = new List<ListadoPkList>();
                    ListadoPkList elemento;
                    List<Sam3_FolioCuantificacion> folioc = new List<Sam3_FolioCuantificacion>();

                    foreach (Sam3_FolioAvisoEntrada r in registros)
                    {
                        folioc = (from fc in ctx.Sam3_FolioCuantificacion
                                  join fe in ctx.Sam3_FolioAvisoEntrada on fc.FolioAvisoEntradaID equals fe.FolioAvisoEntradaID
                                  where fc.Activo
                                  && fc.FolioAvisoEntradaID == r.FolioAvisoEntradaID
                                  && fe.FolioDescarga > 0
                                  select fc).AsParallel().ToList();

                        folioc = folioc.GroupBy(x => x.FolioCuantificacionID).Select(x => x.First()).ToList();

                        foreach (Sam3_FolioCuantificacion fc in folioc)
                        {
                            elemento = new ListadoPkList();
                            elemento.FechaFolioAvisoEntrada = r.FechaCreacion != null ? r.FechaCreacion.Value.ToString("dd/MM/yyyy") : "";
                            elemento.FolioCuantificacion = fc.FolioCuantificacionID.ToString();
                            elemento.FolioEntrada = r.FolioAvisoLlegadaID.ToString();
                            elemento.PackingList = fc.PackingList;
                            elemento.Estatus = r.Estatus;

                            elemento.TipoPackingList = (from rfi in ctx.Sam3_Rel_FolioCuantificacion_ItemCode
                                                        join rid in ctx.Sam3_Rel_ItemCode_Diametro on rfi.Rel_ItemCode_Diametro_ID equals rid.Rel_ItemCode_Diametro_ID
                                                        join it in ctx.Sam3_ItemCode on rid.ItemCodeID equals it.ItemCodeID
                                                        join tpm in ctx.Sam3_TipoMaterial on it.TipoMaterialID equals tpm.TipoMaterialID
                                                        where rfi.Activo && it.Activo && tpm.Activo && rid.Activo
                                                        && rfi.FolioCuantificacionID == fc.FolioCuantificacionID
                                                        select tpm.Nombre).AsParallel().FirstOrDefault();

                            elemento.TipoUso = ctx.Sam3_TipoUso.Where(x => x.TipoUsoID == fc.TipoUsoID).Select(x => x.Nombre).AsParallel().SingleOrDefault();


                            Sam3_FolioAvisoLlegada FolioAvisoLlegada = ctx.Sam3_FolioAvisoLlegada.Where(x => x.FolioAvisoLlegadaID == r.FolioAvisoLlegadaID).FirstOrDefault();

                            string NombreFolioAvisoLlegada = (from pc in ctx.Sam3_Rel_Proyecto_Entidad_Configuracion
                                                              where pc.Proyecto == FolioAvisoLlegada.ProyectoNombrado
                                                              && pc.Entidad == FolioAvisoLlegada.Entidad
                                                              select pc.PreFijoFolioAvisoLlegada + ","
                                                              + pc.CantidadCerosFolioAvisoLlegada.ToString() + ","
                                                              + FolioAvisoLlegada.Consecutivo + ","
                                                              + pc.PostFijoFolioAvisoLlegada.Trim()).FirstOrDefault();

                            string NombreFolioCuantificacion = (from pc in ctx.Sam3_Rel_Proyecto_Entidad_Configuracion
                                                                where pc.Rel_Proyecto_Entidad_Configuracion_ID == fc.Rel_Proyecto_Entidad_Configuracion_ID
                                                                select pc.PreFijoFolioPackingList + ","
                                                                + pc.CantidadCerosFolioPackingList.ToString() + ","
                                                                + fc.ConsecutivoConfiguracion.ToString() + ","
                                                                + pc.PostFijoFolioPackingList).FirstOrDefault();

                            int FolioAvisoLlegadaID = FolioAvisoLlegada.FolioAvisoLlegadaID;
                            int ConsecutivoFolioCuanificacion = fc.Consecutivo.Value;
                            int ConsecutivoFolioLlegada = FolioAvisoLlegada.Consecutivo.Value;

                            NombreFolioAvisoLlegada = Conversiones.Instance.FormatearCadenasdeElementos(NombreFolioAvisoLlegada);
                            NombreFolioCuantificacion = Conversiones.Instance.FormatearCadenasdeElementos(NombreFolioCuantificacion);

                            elemento.FolioConfiguracion = activarFolioConfiguracion ? NombreFolioAvisoLlegada : FolioAvisoLlegada.FolioAvisoLlegadaID.ToString();

                            if (activarFolioConfiguracion && activarFolioConfiguracionCuantificacion)
                            {
                                elemento.FolioConfiguracionCuantificacionID = NombreFolioAvisoLlegada + "-" + NombreFolioCuantificacion;
                            }

                            if (activarFolioConfiguracion && !activarFolioConfiguracionCuantificacion)
                            {
                                elemento.FolioConfiguracionCuantificacionID = NombreFolioAvisoLlegada + "-" + ConsecutivoFolioCuanificacion;
                            }

                            if (!activarFolioConfiguracion && activarFolioConfiguracionCuantificacion)
                            {
                                elemento.FolioConfiguracionCuantificacionID = FolioAvisoLlegadaID + "-" + NombreFolioCuantificacion;
                            }

                            if (!activarFolioConfiguracion && !activarFolioConfiguracionCuantificacion)
                            {
                                elemento.FolioConfiguracionCuantificacionID = FolioAvisoLlegadaID + "-" + ConsecutivoFolioCuanificacion;
                            }


                            listado.Add(elemento);
                        }
                    }

#if DEBUG
                    JavaScriptSerializer serializer = new JavaScriptSerializer();
                    string json = serializer.Serialize(listado);
#endif
                    if (conteo)
                    {
                        return listado.Count();
                    }
                    else
                    {
                        return listado;
                    }
                }
            }
            catch (Exception ex)
            {
                //-----------------Agregar mensaje al Log -----------------------------------------------
                LoggerBd.Instance.EscribirLog(ex);
                //-----------------Agregar mensaje al Log -----------------------------------------------
                TransactionalInformation result = new TransactionalInformation();
                result.ReturnMessage.Add(ex.Message);
                result.ReturnCode = 500;
                result.ReturnStatus = false;
                result.IsAuthenicated = true;

                return result;
            }
        }

        /// <summary>
        /// Lista para combo de packing List en filtros de cuantificacion
        /// </summary>
        /// <param name="usuario"></param>
        /// <returns></returns>
        public object PackingListsParaComboFiltros(Sam3_Usuario usuario)
        {
            try
            {
                Boolean activarFolioConfiguracionCuantificacion = !string.IsNullOrEmpty(ConfigurationManager.AppSettings["ActivarFolioConfiguracionCuantificacion"]) ? 
                    (ConfigurationManager.AppSettings["ActivarFolioConfiguracionCuantificacion"].Equals("1") ? true : false) : false;
                bool activaConfigFolioLlegada = ConfigurationManager.AppSettings["ActivarFolioConfiguracion"].Equals("1") ? true : false;
                
                using (SamContext ctx = new SamContext())
                {
                    List<int> proyectos = ctx.Sam3_Rel_Usuario_Proyecto.Where(x => x.UsuarioID == usuario.UsuarioID).Select(x => x.ProyectoID).AsParallel().ToList();

                    List<int> patios = (from r in ctx.Sam3_Proyecto
                                        join p in ctx.Sam3_Patio on r.PatioID equals p.PatioID
                                        where r.Activo && proyectos.Contains(r.ProyectoID)
                                        select p.PatioID).AsParallel().Distinct().ToList();

                    List<ListaCombos> registros = (from fc in ctx.Sam3_FolioCuantificacion
                                                   join fe in ctx.Sam3_FolioAvisoEntrada on fc.FolioAvisoEntradaID equals fe.FolioAvisoEntradaID
                                                   join fa in ctx.Sam3_FolioAvisoLlegada on fe.FolioAvisoLlegadaID equals fa.FolioAvisoLlegadaID
                                                   join p in ctx.Sam3_Proyecto on fc.ProyectoID equals p.ProyectoID
                                                   join pa in ctx.Sam3_Patio on p.PatioID equals pa.PatioID
                                                   where fc.Activo && fe.Activo && fa.Activo
                                                   && fc.Estatus != "Cerrado"
                                                   && proyectos.Contains(fc.ProyectoID)
                                                   && patios.Contains(pa.PatioID)
                                                   select new ListaCombos
                                                   {
                                                       id = fc.FolioCuantificacionID.ToString(),
                                                       value = fc.FolioCuantificacionID.ToString()
                                                   }).AsParallel().ToList();


                    foreach (ListaCombos item in registros)
                    {
                        int foliocuantificacionid = Convert.ToInt32(item.id);
                        Sam3_FolioCuantificacion folioCuantificacion = ctx.Sam3_FolioCuantificacion.Where(x => x.FolioCuantificacionID == foliocuantificacionid).FirstOrDefault();
                        Sam3_FolioAvisoLlegada folioLl = (from fc in ctx.Sam3_FolioCuantificacion
                                                          join fe in ctx.Sam3_FolioAvisoEntrada on fc.FolioAvisoEntradaID equals fe.FolioAvisoEntradaID
                                                          join fa in ctx.Sam3_FolioAvisoLlegada on fe.FolioAvisoLlegadaID equals fa.FolioAvisoLlegadaID
                                                          where fc.Activo && fe.Activo && fa.Activo
                                                          && fc.FolioCuantificacionID == foliocuantificacionid
                                                          select fa).AsParallel().FirstOrDefault();

                        string NombreFolioAvisoLlegada = (from pc in ctx.Sam3_Rel_Proyecto_Entidad_Configuracion
                                                          where pc.Proyecto == folioLl.ProyectoNombrado
                                                          && pc.Entidad == folioLl.Entidad
                                                          select pc.PreFijoFolioAvisoLlegada + ","
                                                          + pc.CantidadCerosFolioAvisoLlegada.ToString() + ","
                                                          + folioLl.Consecutivo + ","
                                                          + pc.PostFijoFolioAvisoLlegada.Trim()).FirstOrDefault();

                        string NombreFolioCuantificacion = (from pc in ctx.Sam3_Rel_Proyecto_Entidad_Configuracion
                                                            where pc.Rel_Proyecto_Entidad_Configuracion_ID == folioCuantificacion.Rel_Proyecto_Entidad_Configuracion_ID
                                                            select pc.PreFijoFolioPackingList + ","
                                                            + pc.CantidadCerosFolioPackingList.ToString() + ","
                                                            + folioCuantificacion.ConsecutivoConfiguracion.ToString() + ","
                                                            + pc.PostFijoFolioPackingList).FirstOrDefault();

                        int FolioAvisoLlegadaID = folioLl.FolioAvisoLlegadaID;
                        int ConsecutivoFolioCuanificacion = folioCuantificacion.Consecutivo.Value;
                        int ConsecutivoFolioLlegada = folioLl.Consecutivo.Value;

                        item.value = "";

                        NombreFolioAvisoLlegada = Conversiones.Instance.FormatearCadenasdeElementos(NombreFolioAvisoLlegada);
                        NombreFolioCuantificacion = Conversiones.Instance.FormatearCadenasdeElementos(NombreFolioCuantificacion);

                        if (activaConfigFolioLlegada && activarFolioConfiguracionCuantificacion)
                        {
                            item.value = NombreFolioAvisoLlegada + "-" + NombreFolioCuantificacion;
                        }

                        if (activaConfigFolioLlegada && !activarFolioConfiguracionCuantificacion)
                        {
                            item.value = NombreFolioAvisoLlegada + "-" + ConsecutivoFolioCuanificacion;
                        }

                        if (!activaConfigFolioLlegada && activarFolioConfiguracionCuantificacion)
                        {
                            item.value = FolioAvisoLlegadaID + "-" + NombreFolioCuantificacion;
                        }

                        if (!activaConfigFolioLlegada && !activarFolioConfiguracionCuantificacion)
                        {
                            item.value = FolioAvisoLlegadaID + "-" + ConsecutivoFolioCuanificacion;
                        }

                    }


                    registros = registros.GroupBy(x => x.id).Select(x => x.First()).ToList();

#if DEBUG
                    JavaScriptSerializer serializer = new JavaScriptSerializer();
                    string json = serializer.Serialize(registros);
#endif

                    return registros;
                }
            }
            catch (Exception ex)
            {
                //-----------------Agregar mensaje al Log -----------------------------------------------
                LoggerBd.Instance.EscribirLog(ex);
                //-----------------Agregar mensaje al Log -----------------------------------------------
                TransactionalInformation result = new TransactionalInformation();
                result.ReturnMessage.Add(ex.Message);
                result.ReturnCode = 500;
                result.ReturnStatus = false;
                result.IsAuthenicated = true;

                return result;
            }
        }

        public object ConteoDashBoardDespachos(FiltrosJson filtro, Sam3_Usuario usuario)
        {
            try
            {
                DashBoardDespacho conteos = new DashBoardDespacho();

                int conteoODT = (int)ListadoOrdenesDeTrabajo(filtro, usuario, true);
                //int conteoPorEntregar = (int)ListadoPorEntregar(filtro, usuario, true);
                int conteoPorDespachar = (int)ListadoPorDespachar(filtro, usuario, true);
                int conteoPreDespacho = (int)ListadoPreDespacho(filtro, usuario, true);
                int conteoEntrega = (int)ListadoPorEntregar(filtro, usuario, true);
                int porDespachar = (int)ListadoDespachos(filtro, usuario);
                int travelerPendiente = (int)ListadoTravelerPendiente(filtro, usuario, true);

                conteos.CantidadODT = conteoODT.ToString();
                conteos.CantidadODTActiva = conteoODT.ToString();
                conteos.PorEntregar = conteoEntrega.ToString();
                conteos.TrevelerPendiente = travelerPendiente.ToString();
                conteos.PorDespachar = conteoPorDespachar.ToString();
                conteos.PreDespacho = conteoPreDespacho.ToString();

                return conteos;
            }
            catch (Exception ex)
            {
                //-----------------Agregar mensaje al Log -----------------------------------------------
                LoggerBd.Instance.EscribirLog(ex);
                //-----------------Agregar mensaje al Log -----------------------------------------------
                TransactionalInformation result = new TransactionalInformation();
                result.ReturnMessage.Add(ex.Message);
                result.ReturnCode = 500;
                result.ReturnStatus = false;
                result.IsAuthenicated = true;

                return result;
            }

        }

        public object ListadoOrdenesDeTrabajo(FiltrosJson filtros, Sam3_Usuario usuario, bool conteo = false)
        {
            try
            {
                List<ListadoODTDespacho> listado = new List<ListadoODTDespacho>();
                using (SamContext ctx = new SamContext())
                {
                    using (Sam2Context ctx2 = new Sam2Context())
                    {
                        #region Filtros
                        //traemos la informacion de los proyectos y patios del usuario
                        List<int> proyectos = ctx.Sam3_Rel_Usuario_Proyecto.Where(x => x.UsuarioID == usuario.UsuarioID && x.Activo)
                            .Select(x => x.ProyectoID).Distinct().AsParallel().ToList();

                        List<int> Patios = (from p in ctx.Sam3_Proyecto
                                            join pa in ctx.Sam3_Patio on p.PatioID equals pa.PatioID
                                            where p.Activo && pa.Activo
                                            && proyectos.Contains(p.ProyectoID)
                                            select pa.PatioID).Distinct().AsParallel().ToList();

                        int folioAvisoLlegadaID = filtros.FolioAvisoLlegadaID != "" ? Convert.ToInt32(filtros.FolioAvisoLlegadaID) : 0;
                        int clienteID = filtros.ClienteID != "" ? Convert.ToInt32(filtros.ClienteID) : 0;
                        int proyectoID = filtros.ProyectoID != "" ? Convert.ToInt32(filtros.ProyectoID) : 0;
                        int folioCuantificacionID = filtros.PackingListID != "" ? Convert.ToInt32(filtros.PackingListID) : 0;
                        int tipoMaterialID = filtros.TipoMaterialID != "" ? Convert.ToInt32(filtros.TipoMaterialID) : 0;


                        DateTime fechaInicial = new DateTime();
                        DateTime fechaFinal = new DateTime();
                        DateTime.TryParse(filtros.FechaInicial, out fechaInicial);
                        DateTime.TryParse(filtros.FechaFinal, out fechaFinal);

                        if (fechaFinal.ToShortDateString() == "1/1/0001")
                        {
                            fechaFinal = DateTime.Now;
                        }

                        if (fechaInicial.ToShortDateString() == "1/1/0001")
                        {
                            int mes = DateTime.Now.Month != 1 ? DateTime.Now.Month - 1 : 12;
                            int year = DateTime.Now.Month == 1 ? DateTime.Now.Year - 1 : DateTime.Now.Year;
                            fechaInicial = new DateTime(year, mes, DateTime.Now.Day);
                        }
                        #endregion

                        //Traemos todos los folios de entrada del periodo de tiempo
                        List<Sam3_FolioAvisoEntrada> registros;

                        if (proyectoID > 0)
                        {
                            registros = (from fe in ctx.Sam3_FolioAvisoEntrada
                                         join rfp in ctx.Sam3_Rel_FolioAvisoLlegada_Proyecto on fe.FolioAvisoLlegadaID equals rfp.FolioAvisoLlegadaID
                                         where fe.Activo && rfp.Activo
                                         && proyectos.Contains(rfp.ProyectoID)
                                         && rfp.ProyectoID == proyectoID
                                         && (fe.FechaCreacion >= fechaInicial && fe.FechaCreacion <= fechaFinal)
                                         select fe).Distinct().AsParallel().ToList();
                        }
                        else
                        {
                            registros = (from fe in ctx.Sam3_FolioAvisoEntrada
                                         join rfp in ctx.Sam3_Rel_FolioAvisoLlegada_Proyecto on fe.FolioAvisoLlegadaID equals rfp.FolioAvisoLlegadaID
                                         where fe.Activo && rfp.Activo
                                         && proyectos.Contains(rfp.ProyectoID)
                                         && (fe.FechaCreacion >= fechaInicial && fe.FechaCreacion <= fechaFinal)
                                         select fe).Distinct().AsParallel().ToList();
                        }

                        if (clienteID > 0)
                        {
                            int sam3Cliente = (from c in ctx.Sam3_Cliente
                                               where c.Activo && c.Sam2ClienteID == clienteID
                                               select c.ClienteID).AsParallel().SingleOrDefault();
                            registros = registros.Where(x => x.ClienteID == sam3Cliente).ToList();
                        }

                        if (folioAvisoLlegadaID > 0)
                        {
                            registros = registros.Where(x => x.FolioAvisoLlegadaID == folioAvisoLlegadaID).ToList();
                        }

                        List<int> NumerosUnicosIDs = new List<int>();
                        List<int> itemCodeIDs = new List<int>();

                        if (folioCuantificacionID > 0)
                        {
                            itemCodeIDs.AddRange((
                                from fe in registros
                                join fc in ctx.Sam3_FolioCuantificacion on fe.FolioAvisoEntradaID equals fc.FolioAvisoEntradaID
                                join rfi in ctx.Sam3_Rel_FolioCuantificacion_ItemCode on fc.FolioCuantificacionID equals rfi.FolioCuantificacionID
                                join rid in ctx.Sam3_Rel_ItemCode_Diametro on rfi.Rel_ItemCode_Diametro_ID equals rid.Rel_ItemCode_Diametro_ID
                                join it in ctx.Sam3_ItemCode on rid.ItemCodeID equals it.ItemCodeID
                                where fc.Activo && rfi.Activo && it.Activo && rid.Activo
                                && fc.FolioCuantificacionID == folioCuantificacionID
                                && it.TipoMaterialID == tipoMaterialID
                                select it.ItemCodeID).Distinct().AsParallel().ToList()
                                );

                            itemCodeIDs.AddRange((
                                    from fe in registros
                                    join fc in ctx.Sam3_FolioCuantificacion on fe.FolioAvisoEntradaID equals fc.FolioAvisoEntradaID
                                    join b in ctx.Sam3_Bulto on fc.FolioCuantificacionID equals b.FolioCuantificacionID
                                    join rbi in ctx.Sam3_Rel_Bulto_ItemCode on b.BultoID equals rbi.BultoID
                                    join rid in ctx.Sam3_Rel_ItemCode_Diametro on rbi.Rel_ItemCode_Diametro_ID equals rid.Rel_ItemCode_Diametro_ID
                                    join it in ctx.Sam3_ItemCode on rid.ItemCodeID equals it.ItemCodeID
                                    where fc.Activo && b.Activo && rbi.Activo && it.Activo && rid.Activo
                                    && fc.FolioCuantificacionID == folioCuantificacionID
                                    && it.TipoMaterialID == tipoMaterialID
                                    select it.ItemCodeID).Distinct().AsParallel().ToList()
                                );

                        }
                        else
                        {
                            itemCodeIDs.AddRange((
                                from fe in registros
                                join fc in ctx.Sam3_FolioCuantificacion on fe.FolioAvisoEntradaID equals fc.FolioAvisoEntradaID
                                join rfi in ctx.Sam3_Rel_FolioCuantificacion_ItemCode on fc.FolioCuantificacionID equals rfi.FolioCuantificacionID
                                join rid in ctx.Sam3_Rel_ItemCode_Diametro on rfi.Rel_ItemCode_Diametro_ID equals rid.Rel_ItemCode_Diametro_ID
                                join it in ctx.Sam3_ItemCode on rid.ItemCodeID equals it.ItemCodeID
                                where fc.Activo && rfi.Activo && it.Activo && rid.Activo
                                && it.TipoMaterialID == tipoMaterialID
                                select it.ItemCodeID).Distinct().AsParallel().ToList()
                                );

                            itemCodeIDs.AddRange((
                                    from fe in registros
                                    join fc in ctx.Sam3_FolioCuantificacion on fe.FolioAvisoEntradaID equals fc.FolioAvisoEntradaID
                                    join b in ctx.Sam3_Bulto on fc.FolioCuantificacionID equals b.FolioCuantificacionID
                                    join rbi in ctx.Sam3_Rel_Bulto_ItemCode on b.BultoID equals rbi.BultoID
                                    join rid in ctx.Sam3_Rel_ItemCode_Diametro on rbi.Rel_ItemCode_Diametro_ID equals rid.Rel_ItemCode_Diametro_ID
                                    join it in ctx.Sam3_ItemCode on rid.ItemCodeID equals it.ItemCodeID
                                    where fc.Activo && b.Activo && rbi.Activo && it.Activo && rid.Activo
                                    && it.TipoMaterialID == tipoMaterialID
                                    select it.ItemCodeID).Distinct().AsParallel().ToList()
                                );
                        }

                        NumerosUnicosIDs = (from nu in ctx.Sam3_NumeroUnico
                                            where nu.Activo
                                            && itemCodeIDs.Contains(nu.ItemCodeID.Value)
                                            select nu.NumeroUnicoID).Distinct().AsParallel().ToList();

                        List<int> sam2_NumerosUnicosIDs = (from nueq in ctx.Sam3_EquivalenciaNumeroUnico
                                                           where nueq.Activo
                                                           && NumerosUnicosIDs.Contains(nueq.Sam3_NumeroUnicoID)
                                                           select nueq.Sam2_NumeroUnicoID).Distinct().AsParallel().ToList();

                        listado = (from odts in ctx2.OrdenTrabajoSpool
                                   join odtm in ctx2.OrdenTrabajoMaterial on odts.OrdenTrabajoSpoolID equals odtm.OrdenTrabajoSpoolID
                                   join sp in ctx2.Spool on odts.SpoolID equals sp.SpoolID
                                   //join ep in ctx.Sam3_EquivalenciaProyecto on sp.ProyectoID equals ep.Sam2_ProyectoID
                                   where sam2_NumerosUnicosIDs.Contains(odtm.NumeroUnicoCongeladoID.Value)
                                   || sam2_NumerosUnicosIDs.Contains(odtm.NumeroUnicoDespachadoID.Value)
                                   select new ListadoODTDespacho
                                   {
                                       ProyectoID = sp.ProyectoID,
                                       Spool = sp.Nombre,
                                       SpoolID = odts.NumeroControl,
                                       NumeroControlID = odts.OrdenTrabajoSpoolID.ToString(),
                                   }).Distinct().AsParallel().ToList();

                        foreach (ListadoODTDespacho item in listado)
                        {
                            Sam3_EquivalenciaProyecto equivalenciaProyecto = ctx.Sam3_EquivalenciaProyecto.Where(x => x.Sam2_ProyectoID == item.ProyectoID && x.Activo).FirstOrDefault();
                            item.ProyectoID = equivalenciaProyecto.Sam3_ProyectoID;
                        }

                    }
                }

#if DEBUG
                JavaScriptSerializer serializer = new JavaScriptSerializer();
                string json = serializer.Serialize(listado);
#endif

                if (conteo)
                {
                    return listado.Count();
                }
                else
                {
                    return listado;
                }
            }
            catch (Exception ex)
            {
                //-----------------Agregar mensaje al Log -----------------------------------------------
                LoggerBd.Instance.EscribirLog(ex);
                //-----------------Agregar mensaje al Log -----------------------------------------------
                TransactionalInformation result = new TransactionalInformation();
                result.ReturnMessage.Add(ex.Message);
                result.ReturnCode = 500;
                result.ReturnStatus = false;
                result.IsAuthenicated = true;

                return result;
            }
        }

        public object ListadoDespachos(FiltrosJson filtros, Sam3_Usuario usuario, bool conteo = false)
        {
            try
            {
                List<int> listado = new List<int>();
                using (SamContext ctx = new SamContext())
                {
                    using (Sam2Context ctx2 = new Sam2Context())
                    {
                        #region Filtros
                        //traemos la informacion de los proyectos y patios del usuario
                        List<int> proyectos = ctx.Sam3_Rel_Usuario_Proyecto.Where(x => x.UsuarioID == usuario.UsuarioID && x.Activo)
                            .Select(x => x.ProyectoID).Distinct().AsParallel().ToList();

                        List<int> Patios = (from p in ctx.Sam3_Proyecto
                                            join pa in ctx.Sam3_Patio on p.PatioID equals pa.PatioID
                                            where p.Activo && pa.Activo
                                            select pa.PatioID).Distinct().AsParallel().ToList();

                        int folioAvisoLlegadaID = filtros.FolioAvisoLlegadaID != "" ? Convert.ToInt32(filtros.FolioAvisoLlegadaID) : 0;
                        int clienteID = filtros.ClienteID != "" ? Convert.ToInt32(filtros.ClienteID) : 0;
                        int proyectoID = filtros.ProyectoID != "" ? Convert.ToInt32(filtros.ProyectoID) : 0;
                        int folioCuantificacionID = filtros.PackingListID != "" ? Convert.ToInt32(filtros.PackingListID) : 0;
                        int tipoMaterialID = filtros.TipoMaterialID != "" ? Convert.ToInt32(filtros.TipoMaterialID) : 0;

                        DateTime fechaInicial = new DateTime();
                        DateTime fechaFinal = new DateTime();
                        DateTime.TryParse(filtros.FechaInicial, out fechaInicial);
                        DateTime.TryParse(filtros.FechaFinal, out fechaFinal);

                        if (fechaFinal.ToShortDateString() == "1/1/0001")
                        {
                            fechaFinal = DateTime.Now;
                        }

                        if (fechaInicial.ToShortDateString() == "1/1/0001")
                        {
                            int mes = DateTime.Now.Month != 1 ? DateTime.Now.Month - 1 : 12;
                            int year = DateTime.Now.Month == 1 ? DateTime.Now.Year - 1 : DateTime.Now.Year;
                            fechaInicial = new DateTime(year, mes, DateTime.Now.Day);
                        }
                        #endregion

                        //Traemos todos los folios de entrada del periodo de tiempo
                        List<Sam3_FolioAvisoEntrada> registros;

                        if (proyectoID > 0)
                        {
                            registros = (from fe in ctx.Sam3_FolioAvisoEntrada
                                         join rfp in ctx.Sam3_Rel_FolioAvisoLlegada_Proyecto on fe.FolioAvisoLlegadaID equals rfp.FolioAvisoLlegadaID
                                         where fe.Activo && rfp.Activo
                                         && proyectos.Contains(rfp.ProyectoID)
                                         && rfp.ProyectoID == proyectoID
                                         && (fe.FechaCreacion >= fechaInicial && fe.FechaCreacion <= fechaFinal)
                                         select fe).Distinct().AsParallel().ToList();
                        }
                        else
                        {
                            registros = (from fe in ctx.Sam3_FolioAvisoEntrada
                                         join rfp in ctx.Sam3_Rel_FolioAvisoLlegada_Proyecto on fe.FolioAvisoLlegadaID equals rfp.FolioAvisoLlegadaID
                                         where fe.Activo && rfp.Activo
                                         && proyectos.Contains(rfp.ProyectoID)
                                         && (fe.FechaCreacion >= fechaInicial && fe.FechaCreacion <= fechaFinal)
                                         select fe).Distinct().AsParallel().ToList();
                        }

                        if (clienteID > 0)
                        {
                            int sam3Cliente = (from c in ctx.Sam3_Cliente
                                               where c.Activo && c.Sam2ClienteID == clienteID
                                               select c.ClienteID).AsParallel().SingleOrDefault();
                            registros = registros.Where(x => x.ClienteID == sam3Cliente).ToList();
                        }

                        if (folioAvisoLlegadaID > 0)
                        {
                            registros = registros.Where(x => x.FolioAvisoLlegadaID == folioAvisoLlegadaID).ToList();
                        }

                        List<int> NumerosUnicosIDs = new List<int>();
                        List<int> itemCodeIDs = new List<int>();

                        if (folioCuantificacionID > 0)
                        {
                            itemCodeIDs.AddRange((
                                from fe in registros
                                join fc in ctx.Sam3_FolioCuantificacion on fe.FolioAvisoEntradaID equals fc.FolioAvisoEntradaID
                                join rfi in ctx.Sam3_Rel_FolioCuantificacion_ItemCode on fc.FolioCuantificacionID equals rfi.FolioCuantificacionID
                                join it in ctx.Sam3_ItemCode on rfi.ItemCodeID equals it.ItemCodeID
                                where fc.Activo && rfi.Activo && it.Activo
                                && fc.FolioCuantificacionID == folioCuantificacionID
                                && it.TipoMaterialID == tipoMaterialID
                                select it.ItemCodeID).Distinct().AsParallel().ToList()
                                );

                            itemCodeIDs.AddRange((
                                    from fe in registros
                                    join fc in ctx.Sam3_FolioCuantificacion on fe.FolioAvisoEntradaID equals fc.FolioAvisoEntradaID
                                    join b in ctx.Sam3_Bulto on fc.FolioCuantificacionID equals b.FolioCuantificacionID
                                    join rbi in ctx.Sam3_Rel_Bulto_ItemCode on b.BultoID equals rbi.BultoID
                                    join it in ctx.Sam3_ItemCode on rbi.ItemCodeID equals it.ItemCodeID
                                    where fc.Activo && b.Activo && rbi.Activo && it.Activo
                                    && fc.FolioCuantificacionID == folioCuantificacionID
                                    && it.TipoMaterialID == tipoMaterialID
                                    select it.ItemCodeID).Distinct().AsParallel().ToList()
                                );

                        }
                        else
                        {
                            itemCodeIDs.AddRange((
                                from fe in registros
                                join fc in ctx.Sam3_FolioCuantificacion on fe.FolioAvisoEntradaID equals fc.FolioAvisoEntradaID
                                join rfi in ctx.Sam3_Rel_FolioCuantificacion_ItemCode on fc.FolioCuantificacionID equals rfi.FolioCuantificacionID
                                join it in ctx.Sam3_ItemCode on rfi.ItemCodeID equals it.ItemCodeID
                                where fc.Activo && rfi.Activo && it.Activo
                                && it.TipoMaterialID == tipoMaterialID
                                select it.ItemCodeID).Distinct().AsParallel().ToList()
                                );

                            itemCodeIDs.AddRange((
                                    from fe in registros
                                    join fc in ctx.Sam3_FolioCuantificacion on fe.FolioAvisoEntradaID equals fc.FolioAvisoEntradaID
                                    join b in ctx.Sam3_Bulto on fc.FolioCuantificacionID equals b.FolioCuantificacionID
                                    join rbi in ctx.Sam3_Rel_Bulto_ItemCode on b.BultoID equals rbi.BultoID
                                    join it in ctx.Sam3_ItemCode on rbi.ItemCodeID equals it.ItemCodeID
                                    where fc.Activo && b.Activo && rbi.Activo && it.Activo
                                    && it.TipoMaterialID == tipoMaterialID
                                    select it.ItemCodeID).Distinct().AsParallel().ToList()
                                );
                        }

                        NumerosUnicosIDs = (from nu in ctx.Sam3_NumeroUnico
                                            where nu.Activo
                                            && itemCodeIDs.Contains(nu.ItemCodeID.Value)
                                            select nu.NumeroUnicoID).Distinct().AsParallel().ToList();

                        List<int> sam2_NumerosUnicosIDs = (from nueq in ctx.Sam3_EquivalenciaNumeroUnico
                                                           where nueq.Activo
                                                           && NumerosUnicosIDs.Contains(nueq.Sam3_NumeroUnicoID)
                                                           select nueq.Sam2_NumeroUnicoID).Distinct().AsParallel().ToList();

                        listado = (from odts in ctx2.OrdenTrabajoSpool
                                   join odtm in ctx2.OrdenTrabajoMaterial on odts.OrdenTrabajoSpoolID equals odtm.OrdenTrabajoSpoolID
                                   join sp in ctx2.Spool on odts.SpoolID equals sp.SpoolID
                                   where (sam2_NumerosUnicosIDs.Contains(odtm.NumeroUnicoCongeladoID.Value)
                                   || sam2_NumerosUnicosIDs.Contains(odtm.NumeroUnicoDespachadoID.Value))
                                   && odtm.DespachoID == null && odtm.NumeroUnicoDespachadoID == null && odtm.TieneDespacho == false
                                   select odtm.OrdenTrabajoMaterialID).Distinct().AsParallel().ToList();



                    }
                }


                return listado.Count();
            }
            catch (Exception ex)
            {
                //-----------------Agregar mensaje al Log -----------------------------------------------
                LoggerBd.Instance.EscribirLog(ex);
                //-----------------Agregar mensaje al Log -----------------------------------------------
                TransactionalInformation result = new TransactionalInformation();
                result.ReturnMessage.Add(ex.Message);
                result.ReturnCode = 500;
                result.ReturnStatus = false;
                result.IsAuthenicated = true;

                return result;
            }
        }

        public object ListadoIncidencias(FiltrosJson filtros, Sam3_Usuario usuario)
        {
            try
            {
                using (SamContext ctx = new SamContext())
                {
                    #region Filtros
                    //traemos la informacion de los proyectos y patios del usuario
                    List<int> proyectos = ctx.Sam3_Rel_Usuario_Proyecto.Where(x => x.UsuarioID == usuario.UsuarioID && x.Activo)
                        .Select(x => x.ProyectoID).Distinct().AsParallel().ToList();

                    List<int> patios = (from p in ctx.Sam3_Proyecto
                                        join pa in ctx.Sam3_Patio on p.PatioID equals pa.PatioID
                                        where p.Activo && pa.Activo
                                        select pa.PatioID).Distinct().AsParallel().ToList();

                    int clienteID = filtros.ClienteID != "" ? Convert.ToInt32(filtros.ClienteID) : 0;
                    int proyectoID = filtros.ProyectoID != "" ? Convert.ToInt32(filtros.ProyectoID) : 0;

                    DateTime fechaInicial = new DateTime();
                    DateTime fechaFinal = new DateTime();
                    DateTime.TryParse(filtros.FechaInicial, out fechaInicial);
                    DateTime.TryParse(filtros.FechaFinal, out fechaFinal);

                    if (fechaFinal.ToShortDateString() == "1/1/0001")
                    {
                        fechaFinal = DateTime.Now;
                    }

                    if (fechaInicial.ToShortDateString() == "1/1/0001")
                    {
                        int mes = DateTime.Now.Month != 1 ? DateTime.Now.Month - 1 : 12;
                        int year = DateTime.Now.Month == 1 ? DateTime.Now.Year - 1 : DateTime.Now.Year;
                        fechaInicial = new DateTime(year, mes, DateTime.Now.Day);
                    }
                    #endregion

                    //Primero obtengo todas las incidencias activas dentro del rango de tiempo
                    List<Sam3_Incidencia> registrosIncidencias = (from incidencia in ctx.Sam3_Incidencia
                                                                  where incidencia.Activo
                                                                  && (incidencia.FechaCreacion >= fechaInicial && incidencia.FechaCreacion <= fechaFinal)
                                                                  select incidencia).Distinct().AsParallel().ToList();

                    List<int> incidenciasIDs = registrosIncidencias.Select(x => x.IncidenciaID).Distinct().ToList();

                    List<ListadoIncidencias> listado = new List<ListadoIncidencias>();
                    List<ListadoIncidencias> listaTemporal = new List<Models.ListadoIncidencias>();
                    List<int> temp = new List<int>();

                    //folios aviso de llegada -- OK
                    temp = (from r in ctx.Sam3_Rel_Incidencia_FolioAvisoLlegada
                            where r.Activo && incidenciasIDs.Contains(r.IncidenciaID)
                            select r.FolioAvisoLlegadaID).AsParallel().Distinct().ToList();

                    listaTemporal = AvisoLlegadaBd.Instance.ListadoInciendias(clienteID, proyectoID, proyectos, patios, temp);

                    if (listaTemporal.Count > 0) { listado.AddRange(listaTemporal); }

                    //Entrada de material
                    temp.Clear();
                    listaTemporal.Clear();
                    temp = (from r in ctx.Sam3_Rel_Incidencia_FolioAvisoEntrada
                            where r.Activo && incidenciasIDs.Contains(r.IncidenciaID)
                            select r.FolioAvisoEntradaID).AsParallel().ToList();

                    listaTemporal = FolioAvisoEntradaBd.Instance.ListadoIncidencias(clienteID, proyectoID, proyectos, patios, temp);

                    if (listaTemporal.Count > 0) { listado.AddRange(listaTemporal); }

                    //Pase salida, no se si existe la incidencia a nivel pase de salida o es de tipo aviso de entrada
                    //listado.AddRange(PaseSalidaBd.Instance.ListadoIncidencias(clienteID, proyectoID, proyectos, patios, incidenciasIDs, fechaInicial, fechaFinal));

                    //Packing list (Folio Cuantificacion)
                    temp.Clear();
                    listaTemporal.Clear();
                    temp = (from r in ctx.Sam3_Rel_Incidencia_FolioCuantificacion
                            where r.Activo && incidenciasIDs.Contains(r.IncidenciaID)
                            select r.FolioCuantificacionID).AsParallel().ToList();

                    listaTemporal = FoliosCuantificacionBd.Instance.ListadoIncidencias(clienteID, proyectoID, proyectos, patios, temp,
                        fechaInicial, fechaFinal);

                    if (listaTemporal.Count > 0) { listado.AddRange(listaTemporal); }

                    //Orden recepcion
                    temp.Clear();
                    listaTemporal.Clear();

                    temp = (from r in ctx.Sam3_Rel_Incidencia_OrdenRecepcion
                            where r.Activo && incidenciasIDs.Contains(r.IncidenciaID)
                            select r.OrdenRecepcionID).AsParallel().ToList();

                    listaTemporal = OrdenRecepcionBd.Instance.ListadoIncidencias(clienteID, proyectoID, proyectos, patios, temp);

                    if (listaTemporal.Count > 0) { listado.AddRange(listaTemporal); }

                    //Complemento recepcion
                    // N/A

                    //ItemCode
                    temp.Clear();
                    listaTemporal.Clear();

                    temp = (from r in ctx.Sam3_Rel_Incidencia_ItemCode
                            where r.Activo && incidenciasIDs.Contains(r.IncidenciaID)
                            select r.ItemCodeID).AsParallel().ToList();

                    listaTemporal = ItemCodeBd.Instance.ListadoIncidencias(clienteID, proyectoID, proyectos, patios, temp);

                    if (listaTemporal.Count > 0) { listado.AddRange(listaTemporal); }

                    //Orden Almacenaje
                    temp.Clear();
                    listaTemporal.Clear();

                    temp = (from r in ctx.Sam3_Rel_Incidencia_OrdenAlmacenaje
                            where r.Activo && incidenciasIDs.Contains(r.IncidenciaID)
                            select r.OrdenalmacenajeID).AsParallel().ToList();

                    listaTemporal = OrdenAlmacenajeBd.Instance.ListadoIncidencias(clienteID, proyectoID, proyectos, patios, temp);

                    if (listaTemporal.Count > 0) { listado.AddRange(listaTemporal); }

                    //Numero Unico
                    temp.Clear();
                    listaTemporal.Clear();

                    temp = (from r in ctx.Sam3_Rel_Incidencia_NumeroUnico
                            where r.Activo && incidenciasIDs.Contains(r.IncidenciaID)
                            select r.NumeroUnicoID).AsParallel().ToList();

                    listaTemporal = NumeroUnicoBd.Instance.ListadoIncidencias(clienteID, proyectoID, proyectos, patios, temp);

                    if (listaTemporal.Count > 0) { listado.AddRange(listaTemporal); }

                    //Despacho
                    temp.Clear();
                    listaTemporal.Clear();

                    temp = (from r in ctx.Sam3_Rel_Incidencia_Despacho
                            where r.Activo && incidenciasIDs.Contains(r.IncidenciaID)
                            select r.DespachoID).AsParallel().ToList();

                    listaTemporal = DespachoBd.Instance.ListadoIncidencias(clienteID, proyectoID, proyectos, patios, temp);

                    if (listaTemporal.Count > 0) { listado.AddRange(listaTemporal); }

                    //Corte
                    temp.Clear();
                    listaTemporal.Clear();

                    temp = (from r in ctx.Sam3_Rel_Incidencia_Corte
                            where r.Activo && incidenciasIDs.Contains(r.IncidenciaID)
                            select r.CorteID).AsParallel().ToList();

                    listaTemporal = CorteBd.Instance.ListadoIncidencias(clienteID, proyectoID, proyectos, patios, temp);

                    if (listaTemporal.Count > 0) { listado.AddRange(listaTemporal); }

                    foreach (ListadoIncidencias l in listado)
                    {
                        DateTime fechaCreacion = new DateTime();
                        DateTime.TryParse(l.FechaRegistro, out fechaCreacion);

                        l.FechaRegistro = fechaCreacion.ToString("yyyy-MM-dd");
                    }

#if DEBUG
                    JavaScriptSerializer serializer = new JavaScriptSerializer();
                    string json = serializer.Serialize(listado);
#endif

                    return listado.OrderBy(x => x.FolioIncidenciaID).ToList();
                }
            }
            catch (Exception ex)
            {
                //-----------------Agregar mensaje al Log -----------------------------------------------
                LoggerBd.Instance.EscribirLog(ex);
                //-----------------Agregar mensaje al Log -----------------------------------------------
                TransactionalInformation result = new TransactionalInformation();
                result.ReturnMessage.Add(ex.Message);
                result.ReturnCode = 500;
                result.ReturnStatus = false;
                result.IsAuthenicated = true;

                return result;
            }
        }

        public object ObtenerEntidadComboIncidencia(int tipoIncidenciaID, string busqueda)
        {
            try
            {
                using (SamContext ctx = new SamContext())
                {
                    List<ListaCombos> listado = new List<ListaCombos>();
                    Boolean activarFolioConfiguracion = !string.IsNullOrEmpty(ConfigurationManager.AppSettings["ActivarFolioConfiguracion"]) ? (ConfigurationManager.AppSettings["ActivarFolioConfiguracion"].Equals("1") ? true : false) : false;
                    Boolean activarFolioConfiguracionCuantificacion = !string.IsNullOrEmpty(ConfigurationManager.AppSettings["ActivarFolioConfiguracionCuantificacion"]) ? (ConfigurationManager.AppSettings["ActivarFolioConfiguracionCuantificacion"].Equals("1") ? true : false) : false;
                    Boolean activarFolioConfiguracionOrdenRecepcion = !string.IsNullOrEmpty(ConfigurationManager.AppSettings["ActivarFolioConfiguracionOrdenRecepcion"]) ? (ConfigurationManager.AppSettings["ActivarFolioConfiguracionOrdenRecepcion"].Equals("1") ? true : false) : false;
                    Boolean activarFolioConfiguracionOrdenAlmacenaje = !string.IsNullOrEmpty(ConfigurationManager.AppSettings["ActivarFolioConfiguracionOrdenAlmacenaje"]) ? (ConfigurationManager.AppSettings["ActivarFolioConfiguracionOrdenAlmacenaje"].Equals("1") ? true : false) : false;

                    switch (tipoIncidenciaID)
                    {
                        case 1: //Folio Aviso Entrada
                            listado = (from fe in ctx.Sam3_FolioAvisoLlegada
                                       where fe.Activo
                                       && fe.FolioAvisoLlegadaID.ToString() == busqueda
                                       select new ListaCombos
                                       {
                                           id = fe.FolioAvisoLlegadaID.ToString(),
                                           value = fe.FolioAvisoLlegadaID.ToString()

                                       }).AsParallel().Distinct().ToList();

                            if (activarFolioConfiguracion)
                            {
                                foreach (ListaCombos lst in listado)
                                {
                                    int folioAvisoLlegadaID = Convert.ToInt32(lst.id);
                                    Sam3_FolioAvisoLlegada folioAvisoLlegada = ctx.Sam3_FolioAvisoLlegada.Where(x => x.FolioAvisoLlegadaID == folioAvisoLlegadaID).FirstOrDefault();

                                    lst.value = (from pc in ctx.Sam3_Rel_Proyecto_Entidad_Configuracion
                                                 where pc.Entidad == folioAvisoLlegada.Entidad && pc.Proyecto == folioAvisoLlegada.ProyectoNombrado
                                                 select pc.PreFijoFolioAvisoLlegada + ","
                                                  + pc.CantidadCerosFolioAvisoLlegada.ToString() + ","
                                                  + folioAvisoLlegada.Consecutivo.ToString() + ","
                                                  + pc.PostFijoFolioAvisoLlegada).FirstOrDefault();

                                    if (lst.value != null)
                                    {
                                        string[] elemntos = lst.value.Split(',').ToArray();
                                        int digitos = Convert.ToInt32(elemntos[1]);
                                        int consecutivo = Convert.ToInt32(elemntos[2]);
                                        string formato = "D" + digitos.ToString();

                                        lst.value = elemntos[0].Trim() + consecutivo.ToString(formato).Trim() + elemntos[3].Trim();
                                    }

                                }
                            }
                            break;
                        case 2: // Entrada de Material
                            listado = (from fem in ctx.Sam3_FolioAvisoEntrada
                                       where fem.Activo
                                       && fem.FolioAvisoEntradaID.ToString() == busqueda
                                       select new ListaCombos
                                       {
                                           id = fem.FolioAvisoEntradaID.ToString(),
                                           value = fem.FolioAvisoEntradaID.ToString()
                                       }).AsParallel().Distinct().ToList();


                            if (activarFolioConfiguracion)
                            {
                                foreach (ListaCombos lst in listado)
                                {
                                    int folioAvisoEntradaID = Convert.ToInt32(lst.id);
                                    Sam3_FolioAvisoEntrada folioAvisoEntrada = ctx.Sam3_FolioAvisoEntrada.Where(x => x.FolioAvisoEntradaID == folioAvisoEntradaID).FirstOrDefault();
                                    Sam3_FolioAvisoLlegada folioAvisoLlegada = ctx.Sam3_FolioAvisoLlegada.Where(x => x.FolioAvisoLlegadaID == folioAvisoEntrada.FolioAvisoLlegadaID).FirstOrDefault();

                                    lst.value = (from pc in ctx.Sam3_Rel_Proyecto_Entidad_Configuracion
                                                 where pc.Entidad == folioAvisoLlegada.Entidad && pc.Proyecto == folioAvisoLlegada.ProyectoNombrado
                                                 select pc.PreFijoFolioAvisoLlegada + ","
                                                  + pc.CantidadCerosFolioAvisoLlegada.ToString() + ","
                                                  + folioAvisoLlegada.Consecutivo.ToString() + ","
                                                  + pc.PostFijoFolioAvisoLlegada).FirstOrDefault();

                                    if (lst.value != null)
                                    {
                                        string[] elemntos = lst.value.Split(',').ToArray();
                                        int digitos = Convert.ToInt32(elemntos[1]);
                                        int consecutivo = Convert.ToInt32(elemntos[2]);
                                        string formato = "D" + digitos.ToString();

                                        lst.value = elemntos[0].Trim() + consecutivo.ToString(formato).Trim() + elemntos[3].Trim();
                                    }

                                }
                            }
                            break;
                        case 3: // Pase Salida. Por el momento sin implementacion
                            break;
                        case 4: // Packing List
                            listado = (from fc in ctx.Sam3_FolioCuantificacion
                                       where fc.Activo
                                       && fc.FolioCuantificacionID.ToString() == busqueda
                                       select new ListaCombos
                                       {
                                           id = fc.FolioCuantificacionID.ToString(),
                                           value = fc.FolioCuantificacionID.ToString()
                                       }).AsParallel().Distinct().ToList();



                            foreach (ListaCombos item in listado)
                            {
                                int foliocuantificacionid = Convert.ToInt32(item.id);
                                Sam3_FolioCuantificacion folioCuantificacion = ctx.Sam3_FolioCuantificacion.Where(x => x.FolioCuantificacionID == foliocuantificacionid).FirstOrDefault();
                                Sam3_FolioAvisoLlegada folioLl = (from fc in ctx.Sam3_FolioCuantificacion
                                                                  join fe in ctx.Sam3_FolioAvisoEntrada on fc.FolioAvisoEntradaID equals fe.FolioAvisoEntradaID
                                                                  join fa in ctx.Sam3_FolioAvisoLlegada on fe.FolioAvisoLlegadaID equals fa.FolioAvisoLlegadaID
                                                                  where fc.Activo && fe.Activo && fa.Activo
                                                                  && fc.FolioCuantificacionID == foliocuantificacionid
                                                                  select fa).AsParallel().FirstOrDefault();

                                string NombreFolioAvisoLlegada = (from pc in ctx.Sam3_Rel_Proyecto_Entidad_Configuracion
                                                                  where pc.Proyecto == folioLl.ProyectoNombrado
                                                                  && pc.Entidad == folioLl.Entidad
                                                                  select pc.PreFijoFolioAvisoLlegada + ","
                                                                  + pc.CantidadCerosFolioAvisoLlegada.ToString() + ","
                                                                  + folioLl.Consecutivo + ","
                                                                  + pc.PostFijoFolioAvisoLlegada.Trim()).FirstOrDefault();

                                string NombreFolioCuantificacion = (from pc in ctx.Sam3_Rel_Proyecto_Entidad_Configuracion
                                                                    where pc.Rel_Proyecto_Entidad_Configuracion_ID == folioCuantificacion.Rel_Proyecto_Entidad_Configuracion_ID
                                                                    select pc.PreFijoFolioPackingList + ","
                                                                    + pc.CantidadCerosFolioPackingList.ToString() + ","
                                                                    + folioCuantificacion.ConsecutivoConfiguracion.ToString() + ","
                                                                    + pc.PostFijoFolioPackingList).FirstOrDefault();

                                int FolioAvisoLlegadaID = folioLl.FolioAvisoLlegadaID;
                                int ConsecutivoFolioCuanificacion = folioCuantificacion.Consecutivo.Value;
                                int ConsecutivoFolioLlegada = folioLl.Consecutivo.Value;

                                NombreFolioAvisoLlegada = Conversiones.Instance.FormatearCadenasdeElementos(NombreFolioAvisoLlegada);
                                NombreFolioCuantificacion = Conversiones.Instance.FormatearCadenasdeElementos(NombreFolioCuantificacion);

                                if (activarFolioConfiguracion && activarFolioConfiguracionCuantificacion)
                                {
                                    item.value = NombreFolioAvisoLlegada + "-" + NombreFolioCuantificacion;
                                }

                                if (activarFolioConfiguracion && !activarFolioConfiguracionCuantificacion)
                                {
                                    item.value = NombreFolioAvisoLlegada + "-" + ConsecutivoFolioCuanificacion;
                                }

                                if (!activarFolioConfiguracion && activarFolioConfiguracionCuantificacion)
                                {
                                    item.value = FolioAvisoLlegadaID + "-" + NombreFolioCuantificacion;
                                }

                                if (!activarFolioConfiguracion && !activarFolioConfiguracionCuantificacion)
                                {
                                    item.value = FolioAvisoLlegadaID + "-" + ConsecutivoFolioCuanificacion;
                                }
                            }
                            break;
                        case 5: // Orden de recepcion
                            listado = (from ordr in ctx.Sam3_OrdenRecepcion
                                       where ordr.Activo
                                       && ordr.Folio.ToString() == busqueda
                                       select new ListaCombos
                                       {
                                           id = ordr.Folio.ToString(),
                                           value = ordr.Folio.ToString()
                                       }).AsParallel().Distinct().ToList();

                            if (activarFolioConfiguracionOrdenRecepcion)
                            {
                                foreach (ListaCombos item in listado)
                                {
                                    int ordenRecepcionid = Convert.ToInt32(item.id);
                                    Sam3_OrdenRecepcion orden = ctx.Sam3_OrdenRecepcion.Where(x => x.Folio == ordenRecepcionid && x.Activo).FirstOrDefault();

                                    item.value = orden.Rel_Proyecto_Entidad_Configuracion_ID != null ? (from pc in ctx.Sam3_Rel_Proyecto_Entidad_Configuracion
                                                                                                        where pc.Rel_Proyecto_Entidad_Configuracion_ID == orden.Rel_Proyecto_Entidad_Configuracion_ID
                                                                                                        select pc.PreFijoFolioOrdenRecepcion + ","
                                                                                                        + pc.CantidadCerosFolioOrdenRecepcion.ToString() + ","
                                                                                                        + orden.Consecutivo.ToString() + ","
                                                                                                        + pc.PostFijoFolioOrdenRecepcion).AsParallel().FirstOrDefault() : orden.Folio.ToString();

                                    if (!string.IsNullOrEmpty(item.value) && orden.Rel_Proyecto_Entidad_Configuracion_ID != null)
                                    {
                                        string[] elemntos = item.value.Split(',').ToArray();
                                        int digitos = Convert.ToInt32(elemntos[1]);
                                        int consecutivo = Convert.ToInt32(elemntos[2]);
                                        string formato = "D" + digitos.ToString();

                                        item.value = elemntos[0].Trim() + consecutivo.ToString(formato).Trim() + elemntos[3].Trim();
                                    }
                                }
                            }


                            break;
                        case 6: // Complemento de recepcion. Por el momento sin implementacion
                            break;
                        case 7: // ItemCode
                            listado = (from it in ctx.Sam3_ItemCode
                                       where it.Activo
                                       && it.Codigo == busqueda
                                       select new ListaCombos
                                       {
                                           id = it.ItemCodeID.ToString(),
                                           value = it.Codigo
                                       }).AsParallel().Distinct().ToList();
                            break;
                        case 8: // Orden de almacenaje
                            listado = (from oa in ctx.Sam3_OrdenAlmacenaje
                                       where oa.Activo
                                       && oa.Folio.ToString() == busqueda
                                       select new ListaCombos
                                       {
                                           id = oa.Folio.ToString(),
                                           value = oa.Folio.ToString()
                                       }).AsParallel().Distinct().ToList();

                            if (activarFolioConfiguracionOrdenAlmacenaje)
                            {
                                foreach (ListaCombos item in listado)
                                {
                                    int ordenAlmacenajeid = Convert.ToInt32(item.id);
                                    Sam3_OrdenAlmacenaje orden = ctx.Sam3_OrdenAlmacenaje.Where(x => x.Folio == ordenAlmacenajeid && x.Activo).FirstOrDefault();

                                    item.value = orden.Rel_Proyecto_Entidad_Configuracion_ID != null ? (from pc in ctx.Sam3_Rel_Proyecto_Entidad_Configuracion
                                                                                                        where pc.Rel_Proyecto_Entidad_Configuracion_ID == orden.Rel_Proyecto_Entidad_Configuracion_ID
                                                                                                        select pc.PreFijoFolioOrdenAlmacenaje + ","
                                                                                                        + pc.CantidadCerosFolioOrdenAlmacenaje.ToString() + ","
                                                                                                        + orden.Consecutivo.ToString() + ","
                                                                                                        + pc.PostFijoFolioOrdenAlmacenaje).AsParallel().FirstOrDefault() : orden.Folio.ToString();

                                    if (!string.IsNullOrEmpty(item.value) && orden.Rel_Proyecto_Entidad_Configuracion_ID != null)
                                    {
                                        string[] elemntos = item.value.Split(',').ToArray();
                                        int digitos = Convert.ToInt32(elemntos[1]);
                                        int consecutivo = Convert.ToInt32(elemntos[2]);
                                        string formato = "D" + digitos.ToString();

                                        item.value = elemntos[0].Trim() + consecutivo.ToString(formato).Trim() + elemntos[3].Trim();
                                    }
                                }
                            }
                            break;
                        case 9: // Numero unico
                            listado = (from nu in ctx.Sam3_NumeroUnico
                                       where nu.Activo
                                       && nu.NumeroUnicoID.ToString() == busqueda
                                       select new ListaCombos
                                       {
                                           id = nu.NumeroUnicoID.ToString(),
                                           value = nu.Prefijo + "-" + nu.Consecutivo
                                       }).AsParallel().Distinct().ToList();

                            foreach (ListaCombos i in listado)
                            {
                                int temp = Convert.ToInt32(i.id);
                                int digitos = (from nu in ctx.Sam3_NumeroUnico
                                               join p in ctx.Sam3_ProyectoConfiguracion on nu.ProyectoID equals p.ProyectoID
                                               where nu.NumeroUnicoID == temp
                                               select p.DigitosNumeroUnico).AsParallel().SingleOrDefault();

                                string formato = "D" + digitos.ToString();
                                string[] partes = i.value.Split('-').ToArray();
                                int consecutivo = Convert.ToInt32(partes[1]);

                                i.value = partes[0] + "-" + consecutivo.ToString(formato);

                            }

                            break;
                        case 10: // Despacho
                            listado = (from d in ctx.Sam3_Despacho
                                       where d.Activo
                                       && d.DespachoID.ToString() == busqueda
                                       select new ListaCombos
                                       {
                                           id = d.DespachoID.ToString(),
                                           value = d.DespachoID.ToString()
                                       }).AsParallel().Distinct().ToList();
                            break;
                        case 11: // Corte
                            listado = (from c in ctx.Sam3_Corte
                                       where c.Activo
                                       && c.CorteID.ToString() == busqueda
                                       select new ListaCombos
                                       {
                                           id = c.CorteID.ToString(),
                                           value = c.CorteID.ToString()
                                       }).AsParallel().Distinct().ToList();
                            break;
                        default:
                            throw new Exception("No se encontro el tipo de incidencia");
                    }

                    return listado;
                }
            }
            catch (Exception ex)
            {
                //-----------------Agregar mensaje al Log -----------------------------------------------
                LoggerBd.Instance.EscribirLog(ex);
                //-----------------Agregar mensaje al Log -----------------------------------------------
                TransactionalInformation result = new TransactionalInformation();
                result.ReturnMessage.Add(ex.Message);
                result.ReturnCode = 500;
                result.ReturnStatus = false;
                result.IsAuthenicated = true;

                return result;
            }
        }

        public object ListaComboIncidencia(int tipoIncidenciaID)
        {
            try
            {
                using (SamContext ctx = new SamContext())
                {
                    List<ListaCombos> listado = new List<ListaCombos>();
                    Boolean activarFolioConfiguracion = !string.IsNullOrEmpty(ConfigurationManager.AppSettings["ActivarFolioConfiguracion"]) ? (ConfigurationManager.AppSettings["ActivarFolioConfiguracion"].Equals("1") ? true : false) : false;
                    Boolean activarFolioConfiguracionCuantificacion = !string.IsNullOrEmpty(ConfigurationManager.AppSettings["ActivarFolioConfiguracionCuantificacion"]) ? (ConfigurationManager.AppSettings["ActivarFolioConfiguracionCuantificacion"].Equals("1") ? true : false) : false;
                    Boolean activarFolioConfiguracionOrdenRecepcion = !string.IsNullOrEmpty(ConfigurationManager.AppSettings["ActivarFolioConfiguracionOrdenRecepcion"]) ? (ConfigurationManager.AppSettings["ActivarFolioConfiguracionOrdenRecepcion"].Equals("1") ? true : false) : false;
                    Boolean activarFolioConfiguracionOrdenAlmacenaje = !string.IsNullOrEmpty(ConfigurationManager.AppSettings["ActivarFolioConfiguracionOrdenAlmacenaje"]) ? (ConfigurationManager.AppSettings["ActivarFolioConfiguracionOrdenAlmacenaje"].Equals("1") ? true : false) : false;

                    switch (tipoIncidenciaID)
                    {
                        case 1: //Folio Aviso Entrada
                            listado = (from fe in ctx.Sam3_FolioAvisoLlegada
                                       where fe.Activo
                                       //&& fe.FolioAvisoLlegadaID.ToString().Contains(busqueda)
                                       select new ListaCombos
                                       {
                                           id = fe.FolioAvisoLlegadaID.ToString(),
                                           value = fe.FolioAvisoLlegadaID.ToString()
                                       }).AsParallel().Distinct().ToList();

                            if (activarFolioConfiguracion)
                            {
                                foreach (ListaCombos lst in listado)
                                {
                                    int folioAvisoLlegadaID = Convert.ToInt32(lst.id);
                                    Sam3_FolioAvisoLlegada folioAvisoLlegada = ctx.Sam3_FolioAvisoLlegada.Where(x => x.FolioAvisoLlegadaID == folioAvisoLlegadaID).FirstOrDefault();

                                    lst.value = (from pc in ctx.Sam3_Rel_Proyecto_Entidad_Configuracion
                                                 where pc.Entidad == folioAvisoLlegada.Entidad && pc.Proyecto == folioAvisoLlegada.ProyectoNombrado
                                                 select pc.PreFijoFolioAvisoLlegada + ","
                                                  + pc.CantidadCerosFolioAvisoLlegada.ToString() + ","
                                                  + folioAvisoLlegada.Consecutivo.ToString() + ","
                                                  + pc.PostFijoFolioAvisoLlegada).FirstOrDefault();

                                    if (lst.value != null)
                                    {
                                        string[] elemntos = lst.value.Split(',').ToArray();
                                        int digitos = Convert.ToInt32(elemntos[1]);
                                        int consecutivo = Convert.ToInt32(elemntos[2]);
                                        string formato = "D" + digitos.ToString();

                                        lst.value = elemntos[0].Trim() + consecutivo.ToString(formato).Trim() + elemntos[3].Trim();
                                    }

                                }
                            }
                            break;
                        case 2: // Entrada de Material
                            listado = (from fem in ctx.Sam3_FolioAvisoEntrada
                                       where fem.Activo
                                       //&& fem.FolioAvisoEntradaID.ToString().Contains(busqueda)
                                       select new ListaCombos
                                       {
                                           id = fem.FolioAvisoEntradaID.ToString(),
                                           value = fem.FolioAvisoEntradaID.ToString()
                                       }).AsParallel().Distinct().ToList();


                            if (activarFolioConfiguracion)
                            {
                                foreach (ListaCombos lst in listado)
                                {
                                    int folioAvisoEntradaID = Convert.ToInt32(lst.id);
                                    Sam3_FolioAvisoEntrada folioAvisoEntrada = ctx.Sam3_FolioAvisoEntrada.Where(x => x.FolioAvisoEntradaID == folioAvisoEntradaID).FirstOrDefault();
                                    Sam3_FolioAvisoLlegada folioAvisoLlegada = ctx.Sam3_FolioAvisoLlegada.Where(x => x.FolioAvisoLlegadaID == folioAvisoEntrada.FolioAvisoLlegadaID).FirstOrDefault();

                                    lst.value = (from pc in ctx.Sam3_Rel_Proyecto_Entidad_Configuracion
                                                 where pc.Entidad == folioAvisoLlegada.Entidad && pc.Proyecto == folioAvisoLlegada.ProyectoNombrado
                                                 select pc.PreFijoFolioAvisoLlegada + ","
                                                  + pc.CantidadCerosFolioAvisoLlegada.ToString() + ","
                                                  + folioAvisoLlegada.Consecutivo.ToString() + ","
                                                  + pc.PostFijoFolioAvisoLlegada).FirstOrDefault();

                                    if (lst.value != null)
                                    {
                                        string[] elemntos = lst.value.Split(',').ToArray();
                                        int digitos = Convert.ToInt32(elemntos[1]);
                                        int consecutivo = Convert.ToInt32(elemntos[2]);
                                        string formato = "D" + digitos.ToString();

                                        lst.value = elemntos[0].Trim() + consecutivo.ToString(formato).Trim() + elemntos[3].Trim();
                                    }

                                }
                            }
                            break;
                        case 3: // Pase Salida. Por el momento sin implementacion
                            break;
                        case 4: // Packing List
                            listado = (from fc in ctx.Sam3_FolioCuantificacion
                                       join fe in ctx.Sam3_FolioAvisoEntrada on fc.FolioAvisoEntradaID equals fe.FolioAvisoEntradaID
                                       join fa in ctx.Sam3_FolioAvisoLlegada on fe.FolioAvisoLlegadaID equals fa.FolioAvisoLlegadaID
                                       where fc.Activo && fe.Activo && fa.Activo
                                       //&& fc.FolioCuantificacionID.ToString().Contains(busqueda)
                                       select new ListaCombos
                                       {
                                           id = fc.FolioCuantificacionID.ToString(),
                                           value = fc.FolioCuantificacionID.ToString()
                                       }).AsParallel().Distinct().ToList();


                            //if (activarFolioConfiguracionCuantificacion)
                            //{
                            foreach (ListaCombos item in listado)
                            {
                                int foliocuantificacionid = Convert.ToInt32(item.id);
                                Sam3_FolioCuantificacion folioCuantificacion = ctx.Sam3_FolioCuantificacion.Where(x => x.FolioCuantificacionID == foliocuantificacionid).FirstOrDefault();
                                Sam3_FolioAvisoLlegada folioLlegada = (from fc in ctx.Sam3_FolioCuantificacion
                                                                       join fe in ctx.Sam3_FolioAvisoEntrada on fc.FolioAvisoEntradaID equals fe.FolioAvisoEntradaID
                                                                       join fa in ctx.Sam3_FolioAvisoLlegada on fe.FolioAvisoLlegadaID equals fa.FolioAvisoLlegadaID
                                                                       where fc.Activo && fe.Activo && fa.Activo
                                                                       && fc.FolioCuantificacionID == foliocuantificacionid
                                                                       select fa).AsParallel().FirstOrDefault();

                                string NombreFolioAvisoLlegada = (from pc in ctx.Sam3_Rel_Proyecto_Entidad_Configuracion
                                                                  where pc.Proyecto == folioLlegada.ProyectoNombrado
                                                                  && pc.Entidad == folioLlegada.Entidad
                                                                  select pc.PreFijoFolioAvisoLlegada + ","
                                                                  + pc.CantidadCerosFolioAvisoLlegada.ToString() + ","
                                                                  + folioLlegada.Consecutivo + ","
                                                                  + pc.PostFijoFolioAvisoLlegada.Trim()).FirstOrDefault();

                                string NombreFolioCuantificacion = (from pc in ctx.Sam3_Rel_Proyecto_Entidad_Configuracion
                                                                    where pc.Rel_Proyecto_Entidad_Configuracion_ID == folioCuantificacion.Rel_Proyecto_Entidad_Configuracion_ID
                                                                    select pc.PreFijoFolioPackingList + ","
                                                                    + pc.CantidadCerosFolioPackingList.ToString() + ","
                                                                    + folioCuantificacion.ConsecutivoConfiguracion.ToString() + ","
                                                                    + pc.PostFijoFolioPackingList).FirstOrDefault();

                                int FolioAvisoLlegadaID = folioLlegada.FolioAvisoLlegadaID;
                                int ConsecutivoFolioCuanificacion = folioCuantificacion.Consecutivo.Value;
                                int ConsecutivoFolioLlegada = folioLlegada.Consecutivo.Value;

                                NombreFolioAvisoLlegada = Conversiones.Instance.FormatearCadenasdeElementos(NombreFolioAvisoLlegada);
                                NombreFolioCuantificacion = Conversiones.Instance.FormatearCadenasdeElementos(NombreFolioCuantificacion);

                                if (activarFolioConfiguracion && activarFolioConfiguracionCuantificacion)
                                {
                                    item.value = NombreFolioAvisoLlegada + "-" + NombreFolioCuantificacion;
                                }

                                if (activarFolioConfiguracion && !activarFolioConfiguracionCuantificacion)
                                {
                                    item.value = NombreFolioAvisoLlegada + "-" + ConsecutivoFolioCuanificacion;
                                }

                                if (!activarFolioConfiguracion && activarFolioConfiguracionCuantificacion)
                                {
                                    item.value = FolioAvisoLlegadaID + "-" + NombreFolioCuantificacion;
                                }

                                if (!activarFolioConfiguracion && !activarFolioConfiguracionCuantificacion)
                                {
                                    item.value = FolioAvisoLlegadaID + "-" + ConsecutivoFolioCuanificacion;
                                }
                            }

                            break;
                        case 5: // Orden de recepcion
                            listado = (from ordr in ctx.Sam3_OrdenRecepcion
                                       where ordr.Activo
                                       //&& ordr.Folio.ToString().Contains(busqueda)
                                       select new ListaCombos
                                       {
                                           id = ordr.Folio.ToString(),
                                           value = ordr.Folio.ToString()
                                       }).AsParallel().Distinct().ToList();

                            if (activarFolioConfiguracionOrdenRecepcion)
                            {
                                foreach (ListaCombos item in listado)
                                {
                                    int ordenRecepcionid = Convert.ToInt32(item.id);
                                    Sam3_OrdenRecepcion orden = ctx.Sam3_OrdenRecepcion.Where(x => x.Folio == ordenRecepcionid && x.Activo).FirstOrDefault();

                                    item.value = orden.Rel_Proyecto_Entidad_Configuracion_ID != null ? (from pc in ctx.Sam3_Rel_Proyecto_Entidad_Configuracion
                                                                                                        where pc.Rel_Proyecto_Entidad_Configuracion_ID == orden.Rel_Proyecto_Entidad_Configuracion_ID
                                                                                                        select pc.PreFijoFolioOrdenRecepcion + ","
                                                                                                        + pc.CantidadCerosFolioOrdenRecepcion.ToString() + ","
                                                                                                        + orden.Consecutivo.ToString() + ","
                                                                                                        + pc.PostFijoFolioOrdenRecepcion).AsParallel().FirstOrDefault() : orden.Folio.ToString();

                                    if (!string.IsNullOrEmpty(item.value) && orden.Rel_Proyecto_Entidad_Configuracion_ID != null)
                                    {
                                        string[] elemntos = item.value.Split(',').ToArray();
                                        int digitos = Convert.ToInt32(elemntos[1]);
                                        int consecutivo = Convert.ToInt32(elemntos[2]);
                                        string formato = "D" + digitos.ToString();

                                        item.value = elemntos[0].Trim() + consecutivo.ToString(formato).Trim() + elemntos[3].Trim();
                                    }
                                }
                            }

                            break;
                        case 6: // Complemento de recepcion. Por el momento sin implementacion
                            break;
                        case 7: // ItemCode
                            listado = (from it in ctx.Sam3_ItemCode
                                       where it.Activo
                                       //&& it.Codigo.Contains(busqueda)
                                       select new ListaCombos
                                       {
                                           id = it.ItemCodeID.ToString(),
                                           value = it.Codigo
                                       }).AsParallel().Distinct().ToList();
                            break;
                        case 8: // Orden de almacenaje
                            listado = (from oa in ctx.Sam3_OrdenAlmacenaje
                                       where oa.Activo
                                       //&& oa.Folio.ToString().Contains(busqueda)
                                       select new ListaCombos
                                       {
                                           id = oa.Folio.ToString(),
                                           value = oa.Folio.ToString()
                                       }).AsParallel().Distinct().ToList();

                            if (activarFolioConfiguracionOrdenAlmacenaje)
                            {
                                foreach (ListaCombos item in listado)
                                {
                                    int ordenAlmacenajeid = Convert.ToInt32(item.id);
                                    Sam3_OrdenAlmacenaje orden = ctx.Sam3_OrdenAlmacenaje.Where(x => x.Folio == ordenAlmacenajeid && x.Activo).FirstOrDefault();

                                    item.value = orden.Rel_Proyecto_Entidad_Configuracion_ID != null ? (from pc in ctx.Sam3_Rel_Proyecto_Entidad_Configuracion
                                                                                                        where pc.Rel_Proyecto_Entidad_Configuracion_ID == orden.Rel_Proyecto_Entidad_Configuracion_ID
                                                                                                        select pc.PreFijoFolioOrdenAlmacenaje + ","
                                                                                                        + pc.CantidadCerosFolioOrdenAlmacenaje.ToString() + ","
                                                                                                        + orden.Consecutivo.ToString() + ","
                                                                                                        + pc.PostFijoFolioOrdenAlmacenaje).AsParallel().FirstOrDefault() : orden.Folio.ToString();

                                    if (!string.IsNullOrEmpty(item.value) && orden.Rel_Proyecto_Entidad_Configuracion_ID != null)
                                    {
                                        string[] elemntos = item.value.Split(',').ToArray();
                                        int digitos = Convert.ToInt32(elemntos[1]);
                                        int consecutivo = Convert.ToInt32(elemntos[2]);
                                        string formato = "D" + digitos.ToString();

                                        item.value = elemntos[0].Trim() + consecutivo.ToString(formato).Trim() + elemntos[3].Trim();
                                    }
                                }
                            }

                            break;
                        case 9: // Numero unico

                            listado = (from nu in ctx.Sam3_NumeroUnico
                                       where nu.Activo
                                       //&& nu.Prefijo.Contains(prefijo)
                                       //&& nu.Consecutivo.ToString().Contains(num)
                                       select new ListaCombos
                                       {
                                           id = nu.NumeroUnicoID.ToString(),
                                           value = nu.Prefijo + "-" + nu.Consecutivo
                                       }).AsParallel().Distinct().ToList();

                            foreach (ListaCombos i in listado)
                            {
                                int temp = Convert.ToInt32(i.id);
                                int digitos = (from nu in ctx.Sam3_NumeroUnico
                                               join p in ctx.Sam3_ProyectoConfiguracion on nu.ProyectoID equals p.ProyectoID
                                               where nu.NumeroUnicoID == temp
                                               select p.DigitosNumeroUnico).AsParallel().SingleOrDefault();

                                string formato = "D" + digitos.ToString();
                                string[] partes = i.value.Split('-').ToArray();
                                int consecutivo = Convert.ToInt32(partes[1]);

                                i.value = partes[0] + "-" + consecutivo.ToString(formato);

                            }
                            break;
                        case 10: // Despacho
                            listado = (from d in ctx.Sam3_Despacho
                                       where d.Activo
                                       //&& elementos.Any(x => d.DespachoID.ToString().Contains(x))
                                       select new ListaCombos
                                       {
                                           id = d.DespachoID.ToString(),
                                           value = d.DespachoID.ToString()
                                       }).AsParallel().Distinct().ToList();
                            break;
                        case 11: // Corte
                            listado = (from c in ctx.Sam3_Corte
                                       where c.Activo
                                       //&& elementos.Any(x => c.CorteID.ToString().Contains(x))
                                       select new ListaCombos
                                       {
                                           id = c.CorteID.ToString(),
                                           value = c.CorteID.ToString()
                                       }).AsParallel().Distinct().ToList();
                            break;
                        default:
                            throw new Exception("No se encontro el tipo de incidencia");
                    }

                    return listado.OrderBy(x => x.value).ToList();
                }
            }
            catch (Exception ex)
            {
                //-----------------Agregar mensaje al Log -----------------------------------------------
                LoggerBd.Instance.EscribirLog(ex);
                //-----------------Agregar mensaje al Log -----------------------------------------------
                TransactionalInformation result = new TransactionalInformation();
                result.ReturnMessage.Add(ex.Message);
                result.ReturnCode = 500;
                result.ReturnStatus = false;
                result.IsAuthenicated = true;

                return result;
            }
        }


        public object ListadoPreDespacho(FiltrosJson filtros, Sam3_Usuario usuario, bool conteo = false)
        {
            try
            {
                using (SamContext ctx = new SamContext())
                {
                    using (Sam2Context ctx2 = new Sam2Context())
                    {
                        #region filtros
                        int clienteID = filtros.ClienteID != null && filtros.ClienteID != "" ? Convert.ToInt32(filtros.ClienteID) : 0;
                        int proyectoID = filtros.ProyectoID != null && filtros.ProyectoID != "" ? Convert.ToInt32(filtros.ProyectoID) : 0;
                        int folioCuantificacionID = filtros.PackingListID != null && filtros.PackingListID != "" ? Convert.ToInt32(filtros.PackingListID) : 0;
                        int folioAvisoEntrada = filtros.FolioAvisoLlegadaID != null && filtros.FolioAvisoLlegadaID != "" ? Convert.ToInt32(filtros.FolioAvisoLlegadaID) : 0;
                        int tipoMaterialID = filtros.TipoMaterialID != "" ? Convert.ToInt32(filtros.TipoMaterialID) : 0;
                        int unidadDeMedida = 0;
                        List<int> patiosUsuario;
                        List<int> proyectosUsuario;
                        List<int> proyectosSam2;
                        List<int> patiosSam2;
                        //int proyectoIDSam2 = 0;

                        DateTime fechaInicial = new DateTime();
                        DateTime fechaFinal = new DateTime();
                        DateTime.TryParse(filtros.FechaInicial, out fechaInicial);
                        DateTime.TryParse(filtros.FechaFinal, out fechaFinal);
                        UsuarioBd.Instance.ObtenerPatiosYProyectosDeUsuario(usuario.UsuarioID, out proyectosUsuario, out patiosUsuario);

                        proyectosSam2 = (from eq in ctx.Sam3_EquivalenciaProyecto
                                         where eq.Activo
                                         && proyectosUsuario.Contains(eq.Sam3_ProyectoID)
                                         select eq.Sam2_ProyectoID).AsParallel().Distinct().ToList();

                        patiosSam2 = (from eq in ctx.Sam3_EquivalenciaPatio
                                      where eq.Activo
                                      && patiosUsuario.Contains(eq.Sam3_PatioID)
                                      select eq.Sam2_PatioID).AsParallel().Distinct().ToList();

                        if (fechaFinal.ToShortDateString() == "1/1/0001")
                        {
                            fechaFinal = DateTime.Now;
                        }

                        if (fechaInicial.ToShortDateString() == "1/1/0001")
                        {
                            int mes = DateTime.Now.Month != 1 ? DateTime.Now.Month - 1 : 12;
                            int year = DateTime.Now.Month == 1 ? DateTime.Now.Year - 1 : DateTime.Now.Year;
                            fechaInicial = new DateTime(year, mes, DateTime.Now.Day);
                        }

                        if (!String.IsNullOrEmpty(filtros.UnidadDeMedida))
                        {
                            unidadDeMedida = Convert.ToInt32(filtros.UnidadDeMedida);
                        }
                        else
                        {
                            throw new Exception("La unidad de Medida es requerida");
                        }

                        #endregion

                        switch (unidadDeMedida)
                        {
                            case 1: // pieza. Numeros Unicos
                                #region Numeros Unicos
                                {
                                    List<int> sam2NumerosUnicos = new List<int>();
                                    List<CantidadMateriales> InfoNumerosUnicos = new List<CantidadMateriales>();
                                    List<ListadoPreDespacho> listadoPorPredespachar = new List<ListadoPreDespacho>();
                                    if (proyectoID > 0)
                                    {
                                        int proyectoIDSam2 = (from eq in ctx.Sam3_EquivalenciaProyecto
                                                              where eq.Activo && eq.Sam3_ProyectoID == proyectoID
                                                              select eq.Sam2_ProyectoID).AsParallel().SingleOrDefault();

                                        sam2NumerosUnicos = (from odt in ctx2.OrdenTrabajo
                                                             join odts in ctx2.OrdenTrabajoSpool on odt.OrdenTrabajoID equals odts.OrdenTrabajoID
                                                             join odtm in ctx2.OrdenTrabajoMaterial on odts.OrdenTrabajoSpoolID equals odtm.OrdenTrabajoSpoolID
                                                             join nu in ctx2.NumeroUnico on odtm.NumeroUnicoCongeladoID equals nu.NumeroUnicoID
                                                             join ic in ctx2.ItemCode on nu.ItemCodeID equals ic.ItemCodeID
                                                             join p in ctx2.Proyecto on odt.ProyectoID equals p.ProyectoID
                                                             where proyectosSam2.Contains(p.ProyectoID)
                                                                 && patiosSam2.Contains(p.PatioID)
                                                             && p.ProyectoID == proyectoIDSam2 && ic.TipoMaterialID == tipoMaterialID
                                                             select nu.NumeroUnicoID).AsParallel().Distinct().ToList();
                                    }
                                    else
                                    {
                                        sam2NumerosUnicos = (from odt in ctx2.OrdenTrabajo
                                                             join odts in ctx2.OrdenTrabajoSpool on odt.OrdenTrabajoID equals odts.OrdenTrabajoID
                                                             join odtm in ctx2.OrdenTrabajoMaterial on odts.OrdenTrabajoSpoolID equals odtm.OrdenTrabajoSpoolID
                                                             join nu in ctx2.NumeroUnico on odtm.NumeroUnicoCongeladoID equals nu.NumeroUnicoID
                                                             join ic in ctx2.ItemCode on nu.ItemCodeID equals ic.ItemCodeID
                                                             join p in ctx2.Proyecto on odt.ProyectoID equals p.ProyectoID
                                                             where ic.TipoMaterialID == tipoMaterialID
                                                             && proyectosSam2.Contains(p.ProyectoID)
                                                             && patiosSam2.Contains(p.PatioID)
                                                             select nu.NumeroUnicoID).AsParallel().Distinct().ToList();
                                    }

                                    InfoNumerosUnicos = (from odt in ctx2.OrdenTrabajo
                                                         join odts in ctx2.OrdenTrabajoSpool on odt.OrdenTrabajoID equals odts.OrdenTrabajoID
                                                         join odtm in ctx2.OrdenTrabajoMaterial on odts.OrdenTrabajoSpoolID equals odtm.OrdenTrabajoSpoolID
                                                         join nu in ctx2.NumeroUnico on odtm.NumeroUnicoCongeladoID equals nu.NumeroUnicoID
                                                         join ic in ctx2.ItemCode on nu.ItemCodeID equals ic.ItemCodeID
                                                         join p in ctx2.Proyecto on odt.ProyectoID equals p.ProyectoID
                                                         where ic.TipoMaterialID == tipoMaterialID
                                                             && proyectosSam2.Contains(p.ProyectoID)
                                                         select new CantidadMateriales
                                                         {
                                                             NumeroUnicoID = nu.NumeroUnicoID,
                                                             MaterialSpoolID = odtm.MaterialSpoolID,
                                                             ItemCodeID = ic.ItemCodeID,
                                                             OrdenTrabajoSpoolID = odts.OrdenTrabajoSpoolID
                                                         }).AsParallel().Distinct().ToList();

                                    List<int> sam3NumerosUnicos = (from eq in ctx.Sam3_EquivalenciaNumeroUnico
                                                                   where eq.Activo
                                                                   && sam2NumerosUnicos.Contains(eq.Sam2_NumeroUnicoID)
                                                                   select eq.Sam3_NumeroUnicoID).AsParallel().Distinct().ToList();



                                    #region Numeros Unicos

                                    List<Sam3_NumeroUnico> lstNumUnicos = (from nu in ctx.Sam3_NumeroUnico
                                                                           join ic in ctx.Sam3_ItemCode on nu.ItemCodeID equals ic.ItemCodeID
                                                                           join icd in ctx.Sam3_Rel_ItemCode_Diametro on ic.ItemCodeID equals icd.ItemCodeID
                                                                           join fcic in ctx.Sam3_Rel_FolioCuantificacion_ItemCode on icd.Rel_ItemCode_Diametro_ID equals fcic.Rel_ItemCode_Diametro_ID
                                                                           join fc in ctx.Sam3_FolioCuantificacion on fcic.FolioCuantificacionID equals fc.FolioCuantificacionID
                                                                           //join pred in ctx.Sam3_PreDespacho on nu.NumeroUnicoID equals pred.NumeroUnicoID
                                                                           join p in ctx.Sam3_Proyecto on nu.ProyectoID equals p.ProyectoID
                                                                           join pa in ctx.Sam3_Patio on p.PatioID equals pa.PatioID
                                                                           where nu.Activo
                                                                           && proyectosUsuario.Contains(p.ProyectoID)
                                                                           && patiosUsuario.Contains(pa.PatioID)
                                                                               //&& (pred.FechaPreDespacho >= fechaInicial && pred.FechaPreDespacho <= fechaFinal)
                                                                           && sam3NumerosUnicos.Contains(nu.NumeroUnicoID)
                                                                           && (fc.FechaCreacion >= fechaInicial && fc.FechaCreacion <= fechaFinal)
                                                                           select nu).AsParallel().Distinct().ToList();

                                    if (folioAvisoEntrada > 0)
                                    {
                                        lstNumUnicos = (from nu in lstNumUnicos
                                                        join ic in ctx.Sam3_ItemCode on nu.ItemCodeID equals ic.ItemCodeID
                                                        join fcic in ctx.Sam3_Rel_FolioCuantificacion_ItemCode on ic.ItemCodeID equals fcic.ItemCodeID
                                                        join fc in ctx.Sam3_FolioCuantificacion on fcic.FolioCuantificacionID equals fc.FolioCuantificacionID
                                                        join fe in ctx.Sam3_FolioAvisoEntrada on fc.FolioAvisoEntradaID equals fe.FolioAvisoEntradaID
                                                        where ic.Activo && fcic.Activo && fc.Activo && fe.Activo && fe.FolioAvisoLlegadaID == folioAvisoEntrada
                                                        select nu).AsParallel().Distinct().ToList();
                                    }

                                    if (clienteID > 0)
                                    {
                                        lstNumUnicos = (from nu in lstNumUnicos
                                                        join p in ctx.Sam3_Proyecto on nu.ProyectoID equals p.ProyectoID
                                                        join c in ctx.Sam3_Cliente on p.ClienteID equals c.ClienteID
                                                        where p.Activo && c.Sam2ClienteID == clienteID
                                                        select nu).AsParallel().Distinct().ToList();
                                    }

                                    if (folioCuantificacionID > 0)
                                    {
                                        lstNumUnicos = (from nu in lstNumUnicos
                                                        join it in ctx.Sam3_ItemCode on nu.ItemCodeID equals it.ItemCodeID
                                                        where it.Activo
                                                        && ((from rfi in ctx.Sam3_Rel_FolioCuantificacion_ItemCode
                                                             where rfi.Activo && rfi.FolioCuantificacionID == folioCuantificacionID
                                                             select rfi.ItemCodeID).Contains(it.ItemCodeID)
                                                        || (from rbi in ctx.Sam3_Rel_Bulto_ItemCode
                                                            join b in ctx.Sam3_Bulto on rbi.BultoID equals b.BultoID
                                                            where rbi.Activo && b.Activo
                                                            && b.FolioCuantificacionID == folioCuantificacionID
                                                            select rbi.ItemCodeID).Contains(it.ItemCodeID))
                                                        select nu).AsParallel().Distinct().ToList();
                                    }

                                    #endregion

                                    foreach (Sam3_NumeroUnico item in lstNumUnicos.Where(x => x.NumeroUnicoID != 0))
                                    {
                                        int itemCode = (from lst in lstNumUnicos
                                                        join ic in ctx.Sam3_ItemCode on lst.ItemCodeID equals ic.ItemCodeID
                                                        where ic.Activo && lst.NumeroUnicoID == item.NumeroUnicoID
                                                        select ic.ItemCodeID).AsParallel().SingleOrDefault();

                                        int itemCodeSam2 = (from eq in ctx.Sam3_EquivalenciaItemCode
                                                            where eq.Activo && eq.Sam3_ItemCodeID == itemCode
                                                            select eq.Sam2_ItemCodeID).AsParallel().SingleOrDefault();

                                        List<int> MaterialesPorIC = InfoNumerosUnicos.Where(x => x.ItemCodeID == itemCodeSam2).Select(x => x.MaterialSpoolID).AsParallel().ToList();

                                        int preDespachados = (from pre in ctx.Sam3_PreDespacho
                                                              where pre.Activo && pre.ItemCodeID == itemCode
                                                              select pre.PreDespachoID).AsParallel().Count();

                                        listadoPorPredespachar.Add(new ListadoPreDespacho
                                                                         {
                                                                             IC = (from ic in ctx.Sam3_ItemCode
                                                                                   where ic.Activo && ic.ItemCodeID == itemCode
                                                                                   select ic.Codigo).AsParallel().SingleOrDefault(),
                                                                             DescripcionIC = (from ic in ctx.Sam3_ItemCode
                                                                                              where ic.Activo && ic.ItemCodeID == itemCode
                                                                                              select ic.DescripcionEspanol).AsParallel().SingleOrDefault(),
                                                                             D1 = (from ic in ctx.Sam3_ItemCode
                                                                                   join di in ctx.Sam3_Rel_ItemCode_Diametro on ic.ItemCodeID equals di.ItemCodeID
                                                                                   join d in ctx.Sam3_Diametro on di.Diametro1ID equals d.DiametroID
                                                                                   where ic.Activo && di.Activo && ic.ItemCodeID == itemCode
                                                                                   select d.Valor.ToString()).AsParallel().SingleOrDefault(),
                                                                             D2 = (from ic in ctx.Sam3_ItemCode
                                                                                   join di in ctx.Sam3_Rel_ItemCode_Diametro on ic.ItemCodeID equals di.ItemCodeID
                                                                                   join d in ctx.Sam3_Diametro on di.Diametro2ID equals d.DiametroID
                                                                                   where ic.Activo && di.Activo && ic.ItemCodeID == itemCode
                                                                                   select d.Valor.ToString()).AsParallel().SingleOrDefault(),
                                                                             Cedula = "",
                                                                             CantidadPredespachada = preDespachados.ToString(),
                                                                             CantidadPorPredespachar = MaterialesPorIC.Count().ToString(),
                                                                             CantidadTotal = (preDespachados + MaterialesPorIC.Count()).ToString()
                                                                         });

                                    }

                                    if (conteo)
                                    {
                                        return listadoPorPredespachar.GroupBy(x => x.IC).Select(x => x.First()).Count();
                                    }
                                    else
                                    {
                                        return listadoPorPredespachar.GroupBy(x => x.IC).Select(x => x.First()).ToList();
                                    }
                                }
                                #endregion
                                break;
                            //break;
                            case 2: // Spool
                                #region Spool
                                List<CantidadSpools> listaCantidadesSpool = (from ot in ctx2.OrdenTrabajo
                                                                             join ots in ctx2.OrdenTrabajoSpool on ot.OrdenTrabajoID equals ots.OrdenTrabajoID
                                                                             join otm in ctx2.OrdenTrabajoMaterial on ots.OrdenTrabajoSpoolID equals otm.OrdenTrabajoSpoolID
                                                                             join nu in ctx2.NumeroUnico on otm.NumeroUnicoCongeladoID equals nu.NumeroUnicoID
                                                                             join ms in ctx2.MaterialSpool on otm.MaterialSpoolID equals ms.MaterialSpoolID
                                                                             join ic in ctx2.ItemCode on ms.ItemCodeID equals ic.ItemCodeID
                                                                             where proyectosSam2.Contains(ot.ProyectoID) && ic.TipoMaterialID == tipoMaterialID
                                                                             && (ot.FechaOrden >= fechaInicial && ot.FechaOrden <= fechaFinal)
                                                                             select new CantidadSpools
                                                                             {
                                                                                 ItemCodeIDSam2 = ic.ItemCodeID,
                                                                                 MaterialSpoolID = otm.MaterialSpoolID,
                                                                                 OrdenTrabajoSpoolID = ots.OrdenTrabajoSpoolID,
                                                                                 SpoolID = ots.SpoolID,
                                                                                 OrdenTrabajoID = ot.OrdenTrabajoID
                                                                             }).AsParallel().Distinct().ToList();

                                if (proyectoID > 0)
                                {
                                    int proyectoIDSam2 = (from eq in ctx.Sam3_EquivalenciaProyecto
                                                          where eq.Activo && eq.Sam3_ProyectoID == proyectoID
                                                          select eq.Sam2_ProyectoID).AsParallel().SingleOrDefault();

                                    listaCantidadesSpool = (from lst in listaCantidadesSpool
                                                            join ot in ctx2.OrdenTrabajo on lst.OrdenTrabajoID equals ot.OrdenTrabajoID
                                                            where ot.ProyectoID == proyectoIDSam2
                                                            select lst).AsParallel().ToList();
                                }

                                if (clienteID > 0)
                                {
                                    listaCantidadesSpool = (from lst in listaCantidadesSpool
                                                            join ot in ctx2.OrdenTrabajo on lst.OrdenTrabajoID equals ot.OrdenTrabajoID
                                                            join p in ctx2.Proyecto on ot.ProyectoID equals p.ProyectoID
                                                            join c in ctx2.Cliente on p.ClienteID equals c.ClienteID
                                                            where c.ClienteID == clienteID
                                                            select lst).AsParallel().ToList();
                                }

                                if (folioAvisoEntrada > 0)
                                {
                                    List<int> itemCodesSam3 = (from lst in listaCantidadesSpool
                                                               join eq in ctx.Sam3_EquivalenciaItemCode on lst.ItemCodeIDSam2 equals eq.Sam2_ItemCodeID
                                                               select eq.Sam3_ItemCodeID).AsParallel().ToList();

                                    List<int> foliosIC = (from fae in ctx.Sam3_FolioAvisoEntrada
                                                          join fc in ctx.Sam3_FolioCuantificacion on fae.FolioAvisoEntradaID equals fc.FolioAvisoEntradaID
                                                          join rfc in ctx.Sam3_Rel_FolioCuantificacion_ItemCode on fc.FolioCuantificacionID equals rfc.FolioCuantificacionID
                                                          join rdic in ctx.Sam3_Rel_ItemCode_Diametro on rfc.Rel_ItemCode_Diametro_ID equals rdic.Rel_ItemCode_Diametro_ID
                                                          join ic in ctx.Sam3_ItemCode on rdic.ItemCodeID equals ic.ItemCodeID
                                                          where fae.Activo && fc.Activo && rfc.Activo && rdic.Activo && ic.Activo
                                                          && itemCodesSam3.Contains(ic.ItemCodeID) && fae.FolioAvisoLlegadaID == folioAvisoEntrada
                                                          select ic.ItemCodeID).AsParallel().Distinct().ToList();

                                    List<int> itemCodesSam2 = (from eq in ctx.Sam3_EquivalenciaItemCode
                                                               where eq.Activo && foliosIC.Contains(eq.Sam3_ItemCodeID)
                                                               select eq.Sam2_ItemCodeID).AsParallel().ToList();

                                    listaCantidadesSpool = (from lst in listaCantidadesSpool
                                                            where itemCodesSam2.Contains(lst.ItemCodeIDSam2)
                                                            select lst).AsParallel().ToList();
                                }
                                if (folioCuantificacionID > 0)
                                {
                                    List<int> itemCodesSam3 = (from lst in listaCantidadesSpool
                                                               join eq in ctx.Sam3_EquivalenciaItemCode on lst.ItemCodeIDSam2 equals eq.Sam2_ItemCodeID
                                                               select eq.Sam3_ItemCodeID).AsParallel().ToList();

                                    List<int> foliosIC = (from fc in ctx.Sam3_FolioCuantificacion
                                                          join rfc in ctx.Sam3_Rel_FolioCuantificacion_ItemCode on fc.FolioCuantificacionID equals rfc.FolioCuantificacionID
                                                          join rdic in ctx.Sam3_Rel_ItemCode_Diametro on rfc.Rel_ItemCode_Diametro_ID equals rdic.Rel_ItemCode_Diametro_ID
                                                          join ic in ctx.Sam3_ItemCode on rdic.ItemCodeID equals ic.ItemCodeID
                                                          where fc.Activo && rfc.Activo && rdic.Activo && ic.Activo
                                                          && fc.FolioCuantificacionID == folioCuantificacionID
                                                          && itemCodesSam3.Contains(ic.ItemCodeID)
                                                          select ic.ItemCodeID).AsParallel().Distinct().ToList();

                                    List<int> itemCodesSam2 = (from eq in ctx.Sam3_EquivalenciaItemCode
                                                               where eq.Activo && foliosIC.Contains(eq.Sam3_ItemCodeID)
                                                               select eq.Sam2_ItemCodeID).AsParallel().ToList();

                                    listaCantidadesSpool = (from lst in listaCantidadesSpool
                                                            where itemCodesSam2.Contains(lst.ItemCodeIDSam2)
                                                            select lst).AsParallel().ToList();
                                }

                                List<ListadoPorSpoolPreDespacho> porSpool = new List<ListadoPorSpoolPreDespacho>();

                                List<int> spoolsRepetidos = listaCantidadesSpool.GroupBy(x => x.SpoolID).Select(x => x.Key).ToList();//.Where(x=> x.Count()>1).Select(x=> x);

                                foreach (int item in spoolsRepetidos)
                                {
                                    //selecciono los materiales por spool
                                    List<int> MaterialesPorSpool = listaCantidadesSpool.Where(x => x.SpoolID == item).Select(x => x.MaterialSpoolID).AsParallel().ToList();

                                    //cuantos de esos materiales estan despachados
                                    int predespachados = (from p in ctx.Sam3_PreDespacho
                                                          where p.Activo && MaterialesPorSpool.Contains(p.MaterialSpoolID)
                                                          select p.MaterialSpoolID).AsParallel().Count();

                                    //si los que me faltan por despachar son > 0 los agrego a la lista, sino no
                                    if (MaterialesPorSpool.Count() - predespachados > 0)
                                    {

                                        porSpool.Add(new ListadoPorSpoolPreDespacho
                                        {
                                            ID = (from s in ctx2.Spool where s.SpoolID == item select s.Nombre).AsParallel().SingleOrDefault(),
                                            CantidadPorPredespachar = (MaterialesPorSpool.Count() - predespachados).ToString(),
                                            CantidadPredespachada = predespachados.ToString(),
                                            CantidadTotal = MaterialesPorSpool.Count().ToString()
                                        });
                                    }
                                }

                                if (conteo)
                                {
                                    return porSpool.Count();
                                }
                                else
                                {
                                    return porSpool;
                                }
                                #endregion
                                break;
                            case 3: // Toneladas
                                #region Toneladas
                                {
                                    List<int> sam2NumerosUnicos = new List<int>();
                                    List<CantidadMateriales> InfoNumerosUnicos = new List<CantidadMateriales>();
                                    List<ListadoPreDespacho> listadoPorPredespachar = new List<ListadoPreDespacho>();
                                    if (proyectoID > 0)
                                    {
                                        int proyectoIDSam2 = (from eq in ctx.Sam3_EquivalenciaProyecto
                                                              where eq.Activo && eq.Sam3_ProyectoID == proyectoID
                                                              select eq.Sam2_ProyectoID).AsParallel().SingleOrDefault();


                                        sam2NumerosUnicos = (from odt in ctx2.OrdenTrabajo
                                                             join odts in ctx2.OrdenTrabajoSpool on odt.OrdenTrabajoID equals odts.OrdenTrabajoID
                                                             join odtm in ctx2.OrdenTrabajoMaterial on odts.OrdenTrabajoSpoolID equals odtm.OrdenTrabajoSpoolID
                                                             join nu in ctx2.NumeroUnico on odtm.NumeroUnicoCongeladoID equals nu.NumeroUnicoID
                                                             join ic in ctx2.ItemCode on nu.ItemCodeID equals ic.ItemCodeID
                                                             join p in ctx2.Proyecto on odt.ProyectoID equals p.ProyectoID
                                                             where proyectosSam2.Contains(p.ProyectoID)
                                                                 && patiosSam2.Contains(p.PatioID)
                                                             && p.ProyectoID == proyectoIDSam2 && ic.TipoMaterialID == tipoMaterialID
                                                             select nu.NumeroUnicoID).AsParallel().Distinct().ToList();
                                    }
                                    else
                                    {
                                        sam2NumerosUnicos = (from odt in ctx2.OrdenTrabajo
                                                             join odts in ctx2.OrdenTrabajoSpool on odt.OrdenTrabajoID equals odts.OrdenTrabajoID
                                                             join odtm in ctx2.OrdenTrabajoMaterial on odts.OrdenTrabajoSpoolID equals odtm.OrdenTrabajoSpoolID
                                                             join nu in ctx2.NumeroUnico on odtm.NumeroUnicoCongeladoID equals nu.NumeroUnicoID
                                                             join ic in ctx2.ItemCode on nu.ItemCodeID equals ic.ItemCodeID
                                                             join p in ctx2.Proyecto on odt.ProyectoID equals p.ProyectoID
                                                             where ic.TipoMaterialID == tipoMaterialID
                                                             && proyectosSam2.Contains(p.ProyectoID)
                                                             && patiosSam2.Contains(p.PatioID)
                                                             select nu.NumeroUnicoID).AsParallel().ToList();
                                    }

                                    InfoNumerosUnicos = (from odt in ctx2.OrdenTrabajo
                                                         join odts in ctx2.OrdenTrabajoSpool on odt.OrdenTrabajoID equals odts.OrdenTrabajoID
                                                         join odtm in ctx2.OrdenTrabajoMaterial on odts.OrdenTrabajoSpoolID equals odtm.OrdenTrabajoSpoolID
                                                         join nu in ctx2.NumeroUnico on odtm.NumeroUnicoCongeladoID equals nu.NumeroUnicoID
                                                         join ic in ctx2.ItemCode on nu.ItemCodeID equals ic.ItemCodeID
                                                         join p in ctx2.Proyecto on odt.ProyectoID equals p.ProyectoID
                                                         where sam2NumerosUnicos.Contains(nu.NumeroUnicoID)
                                                         select new CantidadMateriales
                                                         {
                                                             NumeroUnicoID = nu.NumeroUnicoID,
                                                             MaterialSpoolID = odtm.MaterialSpoolID,
                                                             ItemCodeID = ic.ItemCodeID,
                                                             OrdenTrabajoSpoolID = odts.OrdenTrabajoSpoolID
                                                         }).AsParallel().Distinct().ToList();

                                    List<int> sam3NumerosUnicos = (from eq in ctx.Sam3_EquivalenciaNumeroUnico
                                                                   where eq.Activo
                                                                   && sam2NumerosUnicos.Contains(eq.Sam2_NumeroUnicoID)
                                                                   select eq.Sam3_NumeroUnicoID).AsParallel().Distinct().ToList();



                                    #region Numeros Unicos

                                    List<Sam3_NumeroUnico> lstNumUnicos = (from nu in ctx.Sam3_NumeroUnico
                                                                           join ic in ctx.Sam3_ItemCode on nu.ItemCodeID equals ic.ItemCodeID
                                                                           join icd in ctx.Sam3_Rel_ItemCode_Diametro on ic.ItemCodeID equals icd.ItemCodeID
                                                                           join fcic in ctx.Sam3_Rel_FolioCuantificacion_ItemCode on icd.Rel_ItemCode_Diametro_ID equals fcic.Rel_ItemCode_Diametro_ID
                                                                           join fc in ctx.Sam3_FolioCuantificacion on fcic.FolioCuantificacionID equals fc.FolioCuantificacionID
                                                                           //join pred in ctx.Sam3_PreDespacho on nu.NumeroUnicoID equals pred.NumeroUnicoID
                                                                           join p in ctx.Sam3_Proyecto on nu.ProyectoID equals p.ProyectoID
                                                                           join pa in ctx.Sam3_Patio on p.PatioID equals pa.PatioID
                                                                           where nu.Activo
                                                                           && proyectosUsuario.Contains(p.ProyectoID)
                                                                           && patiosUsuario.Contains(pa.PatioID)
                                                                               //&& (pred.FechaPreDespacho >= fechaInicial && pred.FechaPreDespacho <= fechaFinal)
                                                                           && sam3NumerosUnicos.Contains(nu.NumeroUnicoID)
                                                                           && (fc.FechaCreacion >= fechaInicial && fc.FechaCreacion <= fechaFinal)
                                                                           select nu).AsParallel().Distinct().ToList();

                                    if (folioAvisoEntrada > 0)
                                    {
                                        lstNumUnicos = (from nu in lstNumUnicos
                                                        join ic in ctx.Sam3_ItemCode on nu.ItemCodeID equals ic.ItemCodeID
                                                        join fcic in ctx.Sam3_Rel_FolioCuantificacion_ItemCode on ic.ItemCodeID equals fcic.ItemCodeID
                                                        join fc in ctx.Sam3_FolioCuantificacion on fcic.FolioCuantificacionID equals fc.FolioCuantificacionID
                                                        join fe in ctx.Sam3_FolioAvisoEntrada on fc.FolioAvisoEntradaID equals fe.FolioAvisoEntradaID
                                                        where ic.Activo && fcic.Activo && fc.Activo && fe.Activo && fe.FolioAvisoLlegadaID == folioAvisoEntrada
                                                        select nu).AsParallel().Distinct().ToList();
                                    }

                                    if (clienteID > 0)
                                    {
                                        lstNumUnicos = (from nu in lstNumUnicos
                                                        join p in ctx.Sam3_Proyecto on nu.ProyectoID equals p.ProyectoID
                                                        join c in ctx.Sam3_Cliente on p.ClienteID equals c.ClienteID
                                                        where p.Activo && c.Sam2ClienteID == clienteID
                                                        select nu).AsParallel().Distinct().ToList();
                                    }

                                    if (folioCuantificacionID > 0)
                                    {
                                        lstNumUnicos = (from nu in lstNumUnicos
                                                        join it in ctx.Sam3_ItemCode on nu.ItemCodeID equals it.ItemCodeID
                                                        where it.Activo
                                                        && ((from rfi in ctx.Sam3_Rel_FolioCuantificacion_ItemCode
                                                             where rfi.Activo && rfi.FolioCuantificacionID == folioCuantificacionID
                                                             select rfi.ItemCodeID).Contains(it.ItemCodeID)
                                                        || (from rbi in ctx.Sam3_Rel_Bulto_ItemCode
                                                            join b in ctx.Sam3_Bulto on rbi.BultoID equals b.BultoID
                                                            where rbi.Activo && b.Activo
                                                            && b.FolioCuantificacionID == folioCuantificacionID
                                                            select rbi.ItemCodeID).Contains(it.ItemCodeID))
                                                        select nu).AsParallel().Distinct().ToList();
                                    }

                                    #endregion

                                    foreach (Sam3_NumeroUnico item in lstNumUnicos)
                                    {
                                        int itemCode = (from lst in lstNumUnicos
                                                        join ic in ctx.Sam3_ItemCode on lst.ItemCodeID equals ic.ItemCodeID
                                                        where ic.Activo && lst.NumeroUnicoID == item.NumeroUnicoID
                                                        select ic.ItemCodeID).AsParallel().SingleOrDefault();

                                        int itemCodeSam2 = (from eq in ctx.Sam3_EquivalenciaItemCode
                                                            where eq.Activo && eq.Sam3_ItemCodeID == itemCode
                                                            select eq.Sam2_ItemCodeID).AsParallel().SingleOrDefault();

                                        decimal pesoICS = (from ic in ctx.Sam3_ItemCode
                                                           join rics in ctx.Sam3_Rel_ItemCode_ItemCodeSteelgo on ic.ItemCodeID equals rics.ItemCodeID
                                                           join ics in ctx.Sam3_ItemCodeSteelgo on rics.ItemCodeSteelgoID equals ics.ItemCodeSteelgoID
                                                           where ic.Activo && rics.Activo && ic.ItemCodeID == itemCode
                                                           select ics.Peso).AsParallel().SingleOrDefault();

                                        decimal MaterialesPorIC = InfoNumerosUnicos.Where(x => x.ItemCodeID == itemCodeSam2).Select(x => x.MaterialSpoolID).AsParallel().Count() * pesoICS;

                                        decimal preDespachados = (from pre in ctx.Sam3_PreDespacho
                                                                  where pre.Activo && pre.ItemCodeID == itemCode
                                                                  select pre.PreDespachoID).AsParallel().Count() * pesoICS;

                                        listadoPorPredespachar.Add(new ListadoPreDespacho
                                        {
                                            IC = (from ic in ctx.Sam3_ItemCode
                                                  where ic.Activo && ic.ItemCodeID == itemCode
                                                  select ic.Codigo).AsParallel().SingleOrDefault(),
                                            DescripcionIC = (from ic in ctx.Sam3_ItemCode
                                                             where ic.Activo && ic.ItemCodeID == itemCode
                                                             select ic.DescripcionEspanol).AsParallel().SingleOrDefault(),
                                            D1 = (from ic in ctx.Sam3_ItemCode
                                                  join di in ctx.Sam3_Rel_ItemCode_Diametro on ic.ItemCodeID equals di.ItemCodeID
                                                  join d in ctx.Sam3_Diametro on di.Diametro1ID equals d.DiametroID
                                                  where ic.Activo && di.Activo && ic.ItemCodeID == itemCode
                                                  select d.Valor.ToString()).AsParallel().SingleOrDefault(),
                                            D2 = (from ic in ctx.Sam3_ItemCode
                                                  join di in ctx.Sam3_Rel_ItemCode_Diametro on ic.ItemCodeID equals di.ItemCodeID
                                                  join d in ctx.Sam3_Diametro on di.Diametro2ID equals d.DiametroID
                                                  where ic.Activo && di.Activo && ic.ItemCodeID == itemCode
                                                  select d.Valor.ToString()).AsParallel().SingleOrDefault(),
                                            Cedula = "",
                                            CantidadPredespachada = preDespachados.ToString(),
                                            CantidadPorPredespachar = MaterialesPorIC.ToString(),
                                            CantidadTotal = (preDespachados + MaterialesPorIC).ToString()
                                        });

                                    }

                                    if (conteo)
                                    {
                                        return listadoPorPredespachar.GroupBy(x => x.IC).Select(x => x.First()).Count();
                                    }
                                    else
                                    {
                                        return listadoPorPredespachar.GroupBy(x => x.IC).Select(x => x.First()).ToList();
                                    }
                                }
                                break;
                                #endregion
                            case 4: // MM
                                #region MM
                                {
                                    List<int> sam2NumerosUnicos = new List<int>();
                                    List<CantidadMateriales> InfoNumerosUnicos = new List<CantidadMateriales>();
                                    List<ListadoPreDespacho> listadoPorPredespachar = new List<ListadoPreDespacho>();
                                    if (proyectoID > 0)
                                    {
                                        int proyectoIDSam2 = (from eq in ctx.Sam3_EquivalenciaProyecto
                                                              where eq.Activo && eq.Sam3_ProyectoID == proyectoID
                                                              select eq.Sam2_ProyectoID).AsParallel().SingleOrDefault();

                                        sam2NumerosUnicos = (from odt in ctx2.OrdenTrabajo
                                                             join odts in ctx2.OrdenTrabajoSpool on odt.OrdenTrabajoID equals odts.OrdenTrabajoID
                                                             join odtm in ctx2.OrdenTrabajoMaterial on odts.OrdenTrabajoSpoolID equals odtm.OrdenTrabajoSpoolID
                                                             join nu in ctx2.NumeroUnico on odtm.NumeroUnicoCongeladoID equals nu.NumeroUnicoID
                                                             join ic in ctx2.ItemCode on nu.ItemCodeID equals ic.ItemCodeID
                                                             join p in ctx2.Proyecto on odt.ProyectoID equals p.ProyectoID
                                                             where proyectosSam2.Contains(p.ProyectoID)
                                                                 && patiosSam2.Contains(p.PatioID)
                                                             && p.ProyectoID == proyectoIDSam2 && ic.TipoMaterialID == tipoMaterialID
                                                             select nu.NumeroUnicoID).AsParallel().Distinct().ToList();
                                    }
                                    else
                                    {
                                        sam2NumerosUnicos = (from odt in ctx2.OrdenTrabajo
                                                             join odts in ctx2.OrdenTrabajoSpool on odt.OrdenTrabajoID equals odts.OrdenTrabajoID
                                                             join odtm in ctx2.OrdenTrabajoMaterial on odts.OrdenTrabajoSpoolID equals odtm.OrdenTrabajoSpoolID
                                                             join nu in ctx2.NumeroUnico on odtm.NumeroUnicoCongeladoID equals nu.NumeroUnicoID
                                                             join ic in ctx2.ItemCode on nu.ItemCodeID equals ic.ItemCodeID
                                                             join p in ctx2.Proyecto on odt.ProyectoID equals p.ProyectoID
                                                             where ic.TipoMaterialID == tipoMaterialID
                                                             && proyectosSam2.Contains(p.ProyectoID)
                                                             && patiosSam2.Contains(p.PatioID)
                                                             select nu.NumeroUnicoID).AsParallel().ToList();
                                    }

                                    InfoNumerosUnicos = (from odt in ctx2.OrdenTrabajo
                                                         join odts in ctx2.OrdenTrabajoSpool on odt.OrdenTrabajoID equals odts.OrdenTrabajoID
                                                         join odtm in ctx2.OrdenTrabajoMaterial on odts.OrdenTrabajoSpoolID equals odtm.OrdenTrabajoSpoolID
                                                         join nu in ctx2.NumeroUnico on odtm.NumeroUnicoCongeladoID equals nu.NumeroUnicoID
                                                         join ic in ctx2.ItemCode on nu.ItemCodeID equals ic.ItemCodeID
                                                         join p in ctx2.Proyecto on odt.ProyectoID equals p.ProyectoID
                                                         where sam2NumerosUnicos.Contains(nu.NumeroUnicoID)
                                                         select new CantidadMateriales
                                                         {
                                                             NumeroUnicoID = nu.NumeroUnicoID,
                                                             MaterialSpoolID = odtm.MaterialSpoolID,
                                                             ItemCodeID = ic.ItemCodeID,
                                                             OrdenTrabajoSpoolID = odts.OrdenTrabajoSpoolID
                                                         }).AsParallel().Distinct().ToList();

                                    List<int> sam3NumerosUnicos = (from eq in ctx.Sam3_EquivalenciaNumeroUnico
                                                                   where eq.Activo
                                                                   && sam2NumerosUnicos.Contains(eq.Sam2_NumeroUnicoID)
                                                                   select eq.Sam3_NumeroUnicoID).AsParallel().Distinct().ToList();

                                    #region Numeros Unicos

                                    List<Sam3_NumeroUnico> lstNumUnicos = (from nu in ctx.Sam3_NumeroUnico
                                                                           join ic in ctx.Sam3_ItemCode on nu.ItemCodeID equals ic.ItemCodeID
                                                                           join icd in ctx.Sam3_Rel_ItemCode_Diametro on ic.ItemCodeID equals icd.ItemCodeID
                                                                           join fcic in ctx.Sam3_Rel_FolioCuantificacion_ItemCode on icd.Rel_ItemCode_Diametro_ID equals fcic.Rel_ItemCode_Diametro_ID
                                                                           join fc in ctx.Sam3_FolioCuantificacion on fcic.FolioCuantificacionID equals fc.FolioCuantificacionID
                                                                           //join pred in ctx.Sam3_PreDespacho on nu.NumeroUnicoID equals pred.NumeroUnicoID
                                                                           join p in ctx.Sam3_Proyecto on nu.ProyectoID equals p.ProyectoID
                                                                           join pa in ctx.Sam3_Patio on p.PatioID equals pa.PatioID
                                                                           where nu.Activo
                                                                           && proyectosUsuario.Contains(p.ProyectoID)
                                                                           && patiosUsuario.Contains(pa.PatioID)
                                                                               //&& (pred.FechaPreDespacho >= fechaInicial && pred.FechaPreDespacho <= fechaFinal)
                                                                           && sam3NumerosUnicos.Contains(nu.NumeroUnicoID)
                                                                           && (fc.FechaCreacion >= fechaInicial && fc.FechaCreacion <= fechaFinal)
                                                                           select nu).AsParallel().Distinct().ToList();

                                    if (folioAvisoEntrada > 0)
                                    {
                                        lstNumUnicos = (from nu in lstNumUnicos
                                                        join ic in ctx.Sam3_ItemCode on nu.ItemCodeID equals ic.ItemCodeID
                                                        join fcic in ctx.Sam3_Rel_FolioCuantificacion_ItemCode on ic.ItemCodeID equals fcic.ItemCodeID
                                                        join fc in ctx.Sam3_FolioCuantificacion on fcic.FolioCuantificacionID equals fc.FolioCuantificacionID
                                                        join fe in ctx.Sam3_FolioAvisoEntrada on fc.FolioAvisoEntradaID equals fe.FolioAvisoEntradaID
                                                        where ic.Activo && fcic.Activo && fc.Activo && fe.Activo && fe.FolioAvisoLlegadaID == folioAvisoEntrada
                                                        select nu).AsParallel().Distinct().ToList();
                                    }

                                    if (clienteID > 0)
                                    {
                                        lstNumUnicos = (from nu in lstNumUnicos
                                                        join p in ctx.Sam3_Proyecto on nu.ProyectoID equals p.ProyectoID
                                                        join c in ctx.Sam3_Cliente on p.ClienteID equals c.ClienteID
                                                        where p.Activo && c.Sam2ClienteID == clienteID
                                                        select nu).AsParallel().Distinct().ToList();
                                    }

                                    if (folioCuantificacionID > 0)
                                    {
                                        lstNumUnicos = (from nu in lstNumUnicos
                                                        join it in ctx.Sam3_ItemCode on nu.ItemCodeID equals it.ItemCodeID
                                                        where it.Activo
                                                        && ((from rfi in ctx.Sam3_Rel_FolioCuantificacion_ItemCode
                                                             where rfi.Activo && rfi.FolioCuantificacionID == folioCuantificacionID
                                                             select rfi.ItemCodeID).Contains(it.ItemCodeID)
                                                        || (from rbi in ctx.Sam3_Rel_Bulto_ItemCode
                                                            join b in ctx.Sam3_Bulto on rbi.BultoID equals b.BultoID
                                                            where rbi.Activo && b.Activo
                                                            && b.FolioCuantificacionID == folioCuantificacionID
                                                            select rbi.ItemCodeID).Contains(it.ItemCodeID))
                                                        select nu).AsParallel().Distinct().ToList();
                                    }

                                    #endregion

                                    foreach (Sam3_NumeroUnico item in lstNumUnicos)
                                    {
                                        int itemCode = (from lst in lstNumUnicos
                                                        join ic in ctx.Sam3_ItemCode on lst.ItemCodeID equals ic.ItemCodeID
                                                        where ic.Activo && lst.NumeroUnicoID == item.NumeroUnicoID
                                                        select ic.ItemCodeID).AsParallel().SingleOrDefault();

                                        int itemCodeSam2 = (from eq in ctx.Sam3_EquivalenciaItemCode
                                                            where eq.Activo && eq.Sam3_ItemCodeID == itemCode
                                                            select eq.Sam2_ItemCodeID).AsParallel().SingleOrDefault();

                                        List<int> MaterialesPorIC = InfoNumerosUnicos.Where(x => x.ItemCodeID == itemCodeSam2).Select(x => x.MaterialSpoolID).AsParallel().ToList();

                                        decimal mmPorPredespachar = 0;
                                        decimal mmPredespachados = 0;

                                        foreach (int material in MaterialesPorIC)
                                        {
                                            mmPorPredespachar = mmPorPredespachar + (from ms in ctx2.MaterialSpool
                                                                                     where ms.MaterialSpoolID == material
                                                                                     select ms.Cantidad).AsParallel().SingleOrDefault();
                                        }

                                        List<int> preDespachados = (from pre in ctx.Sam3_PreDespacho
                                                                    where pre.Activo && pre.ItemCodeID == itemCode
                                                                    select pre.MaterialSpoolID).AsParallel().ToList();

                                        foreach (int material in preDespachados)
                                        {
                                            mmPredespachados = mmPredespachados + (from ms in ctx2.MaterialSpool
                                                                                   where ms.MaterialSpoolID == material
                                                                                   select ms.Cantidad).AsParallel().SingleOrDefault();
                                        }

                                        listadoPorPredespachar.Add(new ListadoPreDespacho
                                        {
                                            IC = (from ic in ctx.Sam3_ItemCode
                                                  where ic.Activo && ic.ItemCodeID == itemCode
                                                  select ic.Codigo).AsParallel().SingleOrDefault(),
                                            DescripcionIC = (from ic in ctx.Sam3_ItemCode
                                                             where ic.Activo && ic.ItemCodeID == itemCode
                                                             select ic.DescripcionEspanol).AsParallel().SingleOrDefault(),
                                            D1 = (from ic in ctx.Sam3_ItemCode
                                                  join di in ctx.Sam3_Rel_ItemCode_Diametro on ic.ItemCodeID equals di.ItemCodeID
                                                  join d in ctx.Sam3_Diametro on di.Diametro1ID equals d.DiametroID
                                                  where ic.Activo && di.Activo && ic.ItemCodeID == itemCode
                                                  select d.Valor.ToString()).AsParallel().SingleOrDefault(),
                                            D2 = (from ic in ctx.Sam3_ItemCode
                                                  join di in ctx.Sam3_Rel_ItemCode_Diametro on ic.ItemCodeID equals di.ItemCodeID
                                                  join d in ctx.Sam3_Diametro on di.Diametro2ID equals d.DiametroID
                                                  where ic.Activo && di.Activo && ic.ItemCodeID == itemCode
                                                  select d.Valor.ToString()).AsParallel().SingleOrDefault(),
                                            Cedula = "",
                                            CantidadPredespachada = (mmPredespachados / 1000).ToString(),
                                            CantidadPorPredespachar = (mmPorPredespachar / 1000).ToString(),
                                            CantidadTotal = ((mmPredespachados / 1000) + (mmPorPredespachar / 1000)).ToString()
                                        });
                                    }

                                    if (conteo && filtros.TipoMaterialID == "1")
                                    {
                                        return listadoPorPredespachar.GroupBy(x => x.IC).Select(x => x.First()).Count();
                                    }
                                    else if (!conteo && filtros.TipoMaterialID == "1")
                                    {
                                        return listadoPorPredespachar.GroupBy(x => x.IC).Select(x => x.First()).ToList();
                                    }
                                }
                                break;
                                #endregion
                            default:
                                throw new Exception("Unidad de medida invalida");
                        }
                    }
                }
                return null;
            }
            catch (Exception ex)
            {
                //-----------------Agregar mensaje al Log -----------------------------------------------
                LoggerBd.Instance.EscribirLog(ex);
                //-----------------Agregar mensaje al Log -----------------------------------------------
                TransactionalInformation result = new TransactionalInformation();
                result.ReturnMessage.Add(ex.Message);
                result.ReturnCode = 500;
                result.ReturnStatus = false;
                result.IsAuthenicated = true;

                return result;
            }
        }

        public object ListadoPorDespachar(FiltrosJson filtros, Sam3_Usuario usuario, bool conteo = false)
        {
            try
            {
                using (SamContext ctx = new SamContext())
                {
                    using (Sam2Context ctx2 = new Sam2Context())
                    {
                        #region filtros
                        int clienteID = filtros.ClienteID != null && filtros.ClienteID != "" ? Convert.ToInt32(filtros.ClienteID) : 0;
                        int proyectoID = filtros.ProyectoID != null && filtros.ProyectoID != "" ? Convert.ToInt32(filtros.ProyectoID) : 0;
                        int folioCuantificacionID = filtros.PackingListID != null && filtros.PackingListID != "" ? Convert.ToInt32(filtros.PackingListID) : 0;
                        int folioAvisoEntrada = filtros.FolioAvisoLlegadaID != null && filtros.FolioAvisoLlegadaID != "" ? Convert.ToInt32(filtros.FolioAvisoLlegadaID) : 0;
                        int tipoMaterialID = filtros.TipoMaterialID != "" ? Convert.ToInt32(filtros.TipoMaterialID) : 0;
                        int unidadDeMedida = 0;
                        List<int> patiosUsuario;
                        List<int> proyectosUsuario;
                        List<int> proyectosSam2;
                        List<int> patiosSam2;
                        //int proyectoIDSam2 = 0;

                        DateTime fechaInicial = new DateTime();
                        DateTime fechaFinal = new DateTime();
                        DateTime.TryParse(filtros.FechaInicial, out fechaInicial);
                        DateTime.TryParse(filtros.FechaFinal, out fechaFinal);
                        UsuarioBd.Instance.ObtenerPatiosYProyectosDeUsuario(usuario.UsuarioID, out proyectosUsuario, out patiosUsuario);

                        proyectosSam2 = (from eq in ctx.Sam3_EquivalenciaProyecto
                                         where eq.Activo
                                         && proyectosUsuario.Contains(eq.Sam3_ProyectoID)
                                         select eq.Sam2_ProyectoID).AsParallel().Distinct().ToList();

                        patiosSam2 = (from eq in ctx.Sam3_EquivalenciaPatio
                                      where eq.Activo
                                      && patiosUsuario.Contains(eq.Sam3_PatioID)
                                      select eq.Sam2_PatioID).AsParallel().Distinct().ToList();

                        if (fechaFinal.ToShortDateString() == "1/1/0001")
                        {
                            fechaFinal = DateTime.Now;
                        }

                        if (fechaInicial.ToShortDateString() == "1/1/0001")
                        {
                            int mes = DateTime.Now.Month != 1 ? DateTime.Now.Month - 1 : 12;
                            int year = DateTime.Now.Month == 1 ? DateTime.Now.Year - 1 : DateTime.Now.Year;
                            fechaInicial = new DateTime(year, mes, DateTime.Now.Day);
                        }

                        if (!String.IsNullOrEmpty(filtros.UnidadDeMedida))
                        {
                            unidadDeMedida = Convert.ToInt32(filtros.UnidadDeMedida);
                        }
                        else
                        {
                            throw new Exception("La unidad de Medida es requerida");
                        }

                        #endregion

                        switch (unidadDeMedida)
                        {
                            case 1: // pieza. Numeros Unicos
                                #region Numeros Unicos
                                {
                                    //Obtenemos la lista de los numeros unicos que estan congelados y que aun no tienen despacho ni corte
                                    List<int> sam2NumerosUnicos = new List<int>();
                                    List<CantidadMateriales> InfoNumerosUnicos = new List<CantidadMateriales>();
                                    List<ListadoDespacho> listadoPorDespachar = new List<ListadoDespacho>();
                                    if (proyectoID > 0)
                                    {
                                        int proyectoIDSam2 = (from eq in ctx.Sam3_EquivalenciaProyecto
                                                              where eq.Activo && eq.Sam3_ProyectoID == proyectoID
                                                              select eq.Sam2_ProyectoID).AsParallel().SingleOrDefault();

                                        sam2NumerosUnicos = (from odt in ctx2.OrdenTrabajo
                                                             join odts in ctx2.OrdenTrabajoSpool on odt.OrdenTrabajoID equals odts.OrdenTrabajoID
                                                             join odtm in ctx2.OrdenTrabajoMaterial on odts.OrdenTrabajoSpoolID equals odtm.OrdenTrabajoSpoolID
                                                             join nu in ctx2.NumeroUnico on odtm.NumeroUnicoCongeladoID equals nu.NumeroUnicoID
                                                             join ic in ctx2.ItemCode on nu.ItemCodeID equals ic.ItemCodeID
                                                             join p in ctx2.Proyecto on odt.ProyectoID equals p.ProyectoID
                                                             where proyectosSam2.Contains(p.ProyectoID)
                                                                 && patiosSam2.Contains(p.PatioID)
                                                             && p.ProyectoID == proyectoIDSam2 && ic.TipoMaterialID == tipoMaterialID
                                                             select nu.NumeroUnicoID).AsParallel().Distinct().ToList();
                                    }
                                    else
                                    {
                                        sam2NumerosUnicos = (from odt in ctx2.OrdenTrabajo
                                                             join odts in ctx2.OrdenTrabajoSpool on odt.OrdenTrabajoID equals odts.OrdenTrabajoID
                                                             join odtm in ctx2.OrdenTrabajoMaterial on odts.OrdenTrabajoSpoolID equals odtm.OrdenTrabajoSpoolID
                                                             join nu in ctx2.NumeroUnico on odtm.NumeroUnicoCongeladoID equals nu.NumeroUnicoID
                                                             join ic in ctx2.ItemCode on nu.ItemCodeID equals ic.ItemCodeID
                                                             join p in ctx2.Proyecto on odt.ProyectoID equals p.ProyectoID
                                                             where ic.TipoMaterialID == tipoMaterialID
                                                             && proyectosSam2.Contains(p.ProyectoID)
                                                             && patiosSam2.Contains(p.PatioID)
                                                             select nu.NumeroUnicoID).AsParallel().ToList();
                                    }

                                    InfoNumerosUnicos = (from odt in ctx2.OrdenTrabajo
                                                         join odts in ctx2.OrdenTrabajoSpool on odt.OrdenTrabajoID equals odts.OrdenTrabajoID
                                                         join odtm in ctx2.OrdenTrabajoMaterial on odts.OrdenTrabajoSpoolID equals odtm.OrdenTrabajoSpoolID
                                                         //join nu in ctx2.NumeroUnico on odtm.NumeroUnicoCongeladoID equals nu.NumeroUnicoID
                                                         join ms in ctx2.MaterialSpool on odtm.MaterialSpoolID equals ms.MaterialSpoolID
                                                         join ic in ctx2.ItemCode on ms.ItemCodeID equals ic.ItemCodeID
                                                         join p in ctx2.Proyecto on odt.ProyectoID equals p.ProyectoID
                                                         where ic.TipoMaterialID == tipoMaterialID
                                                              && proyectosSam2.Contains(p.ProyectoID)
                                                              && patiosSam2.Contains(p.PatioID)
                                                         select new CantidadMateriales
                                                         {
                                                             NumeroUnicoID = odtm.NumeroUnicoCongeladoID,
                                                             MaterialSpoolID = odtm.MaterialSpoolID,
                                                             ItemCodeID = ic.ItemCodeID,
                                                             OrdenTrabajoSpoolID = odts.OrdenTrabajoSpoolID
                                                         }).AsParallel().Distinct().ToList();

                                    List<int> sam3NumerosUnicos = (from eq in ctx.Sam3_EquivalenciaNumeroUnico
                                                                   where eq.Activo
                                                                   && sam2NumerosUnicos.Contains(eq.Sam2_NumeroUnicoID)
                                                                   select eq.Sam3_NumeroUnicoID).AsParallel().Distinct().ToList();

                                    #region Numeros Unicos

                                    List<Sam3_NumeroUnico> lstNumUnicos = (from nu in ctx.Sam3_NumeroUnico
                                                                           join ic in ctx.Sam3_ItemCode on nu.ItemCodeID equals ic.ItemCodeID
                                                                           join icd in ctx.Sam3_Rel_ItemCode_Diametro on ic.ItemCodeID equals icd.ItemCodeID
                                                                           join fcic in ctx.Sam3_Rel_FolioCuantificacion_ItemCode on icd.Rel_ItemCode_Diametro_ID equals fcic.Rel_ItemCode_Diametro_ID
                                                                           join fc in ctx.Sam3_FolioCuantificacion on fcic.FolioCuantificacionID equals fc.FolioCuantificacionID
                                                                           //join pred in ctx.Sam3_PreDespacho on nu.NumeroUnicoID equals pred.NumeroUnicoID
                                                                           join p in ctx.Sam3_Proyecto on nu.ProyectoID equals p.ProyectoID
                                                                           join pa in ctx.Sam3_Patio on p.PatioID equals pa.PatioID
                                                                           where nu.Activo
                                                                           && proyectosUsuario.Contains(p.ProyectoID)
                                                                           && patiosUsuario.Contains(pa.PatioID)
                                                                               //&& (pred.FechaPreDespacho >= fechaInicial && pred.FechaPreDespacho <= fechaFinal)
                                                                           && sam3NumerosUnicos.Contains(nu.NumeroUnicoID)
                                                                           && (fc.FechaCreacion >= fechaInicial && fc.FechaCreacion <= fechaFinal)
                                                                           select nu).AsParallel().Distinct().ToList();

                                    if (folioAvisoEntrada > 0)
                                    {
                                        lstNumUnicos = (from nu in lstNumUnicos
                                                        join ic in ctx.Sam3_ItemCode on nu.ItemCodeID equals ic.ItemCodeID
                                                        join fcic in ctx.Sam3_Rel_FolioCuantificacion_ItemCode on ic.ItemCodeID equals fcic.ItemCodeID
                                                        join fc in ctx.Sam3_FolioCuantificacion on fcic.FolioCuantificacionID equals fc.FolioCuantificacionID
                                                        join fe in ctx.Sam3_FolioAvisoEntrada on fc.FolioAvisoEntradaID equals fe.FolioAvisoEntradaID
                                                        where ic.Activo && fcic.Activo && fc.Activo && fe.Activo && fe.FolioAvisoLlegadaID == folioAvisoEntrada
                                                        select nu).AsParallel().Distinct().ToList();
                                    }

                                    if (clienteID > 0)
                                    {
                                        lstNumUnicos = (from nu in lstNumUnicos
                                                        join p in ctx.Sam3_Proyecto on nu.ProyectoID equals p.ProyectoID
                                                        join c in ctx.Sam3_Cliente on p.ClienteID equals c.ClienteID
                                                        where p.Activo && c.Sam2ClienteID == clienteID
                                                        select nu).AsParallel().Distinct().ToList();
                                    }

                                    if (folioCuantificacionID > 0)
                                    {
                                        lstNumUnicos = (from nu in lstNumUnicos
                                                        join it in ctx.Sam3_ItemCode on nu.ItemCodeID equals it.ItemCodeID
                                                        where it.Activo
                                                        && ((from rfi in ctx.Sam3_Rel_FolioCuantificacion_ItemCode
                                                             where rfi.Activo && rfi.FolioCuantificacionID == folioCuantificacionID
                                                             select rfi.ItemCodeID).Contains(it.ItemCodeID)
                                                        || (from rbi in ctx.Sam3_Rel_Bulto_ItemCode
                                                            join b in ctx.Sam3_Bulto on rbi.BultoID equals b.BultoID
                                                            where rbi.Activo && b.Activo
                                                            && b.FolioCuantificacionID == folioCuantificacionID
                                                            select rbi.ItemCodeID).Contains(it.ItemCodeID))
                                                        select nu).AsParallel().Distinct().ToList();
                                    }

                                    #endregion

                                    foreach (Sam3_NumeroUnico item in lstNumUnicos)
                                    {
                                        int itemCode = (from lst in lstNumUnicos
                                                        join ic in ctx.Sam3_ItemCode on lst.ItemCodeID equals ic.ItemCodeID
                                                        where ic.Activo && lst.NumeroUnicoID == item.NumeroUnicoID
                                                        select ic.ItemCodeID).AsParallel().SingleOrDefault();

                                        int itemCodeSam2 = (from eq in ctx.Sam3_EquivalenciaItemCode
                                                            where eq.Activo && eq.Sam3_ItemCodeID == itemCode
                                                            select eq.Sam2_ItemCodeID).AsParallel().SingleOrDefault();

                                        List<int> MaterialesDespachados = InfoNumerosUnicos.Where(x => x.NumeroUnicoID == null && x.ItemCodeID == itemCodeSam2).Select(x => x.MaterialSpoolID).AsParallel().ToList();

                                        List<int> MaterialesPorIC = InfoNumerosUnicos.Where(x => x.ItemCodeID == itemCodeSam2 && x.NumeroUnicoID != null).Select(x => x.MaterialSpoolID).AsParallel().ToList();

                                        int Despachados = (from des in ctx.Sam3_Despacho
                                                           where des.Activo && MaterialesDespachados.Contains(des.MaterialSpoolID)
                                                           select des.DespachoID).AsParallel().Count();

                                        listadoPorDespachar.Add(new ListadoDespacho
                                                                  {
                                                                      IC = (from ic in ctx.Sam3_ItemCode
                                                                            where ic.Activo && ic.ItemCodeID == itemCode
                                                                            select ic.Codigo).AsParallel().SingleOrDefault(),
                                                                      DescripcionIC = (from ic in ctx.Sam3_ItemCode
                                                                                       where ic.Activo && ic.ItemCodeID == itemCode
                                                                                       select ic.DescripcionEspanol).AsParallel().SingleOrDefault(),
                                                                      D1 = (from ic in ctx.Sam3_ItemCode
                                                                            join di in ctx.Sam3_Rel_ItemCode_Diametro on ic.ItemCodeID equals di.ItemCodeID
                                                                            join d in ctx.Sam3_Diametro on di.Diametro1ID equals d.DiametroID
                                                                            where ic.Activo && di.Activo && ic.ItemCodeID == itemCode
                                                                            select d.Valor.ToString()).AsParallel().SingleOrDefault(),
                                                                      D2 = (from ic in ctx.Sam3_ItemCode
                                                                            join di in ctx.Sam3_Rel_ItemCode_Diametro on ic.ItemCodeID equals di.ItemCodeID
                                                                            join d in ctx.Sam3_Diametro on di.Diametro2ID equals d.DiametroID
                                                                            where ic.Activo && di.Activo && ic.ItemCodeID == itemCode
                                                                            select d.Valor.ToString()).AsParallel().SingleOrDefault(),
                                                                      Cedula = "",
                                                                      CantidadDespachada = Despachados.ToString(),
                                                                      CantidadPorDespachar = MaterialesPorIC.Count().ToString(),
                                                                      CantidadTotal = (Despachados + MaterialesPorIC.Count()).ToString()
                                                                  });
                                    }

                                    if (conteo)
                                    {
                                        return listadoPorDespachar.GroupBy(x => x.IC).Select(x => x.First()).Count();
                                    }
                                    else
                                    {
                                        return listadoPorDespachar.GroupBy(x => x.IC).Select(x => x.First()).AsParallel().ToList();
                                    }
                                }
                                break;
                                #endregion
                            case 2: // Spool
                                #region Spool
                                List<CantidadSpools> listaCantidadesSpool = (from ot in ctx2.OrdenTrabajo
                                                                             join ots in ctx2.OrdenTrabajoSpool on ot.OrdenTrabajoID equals ots.OrdenTrabajoID
                                                                             join otm in ctx2.OrdenTrabajoMaterial on ots.OrdenTrabajoSpoolID equals otm.OrdenTrabajoSpoolID
                                                                             join nu in ctx2.NumeroUnico on otm.NumeroUnicoCongeladoID equals nu.NumeroUnicoID
                                                                             join ms in ctx2.MaterialSpool on otm.MaterialSpoolID equals ms.MaterialSpoolID
                                                                             join ic in ctx2.ItemCode on ms.ItemCodeID equals ic.ItemCodeID
                                                                             where proyectosSam2.Contains(ot.ProyectoID) && ic.TipoMaterialID == tipoMaterialID
                                                                             && (ot.FechaOrden >= fechaInicial && ot.FechaOrden <= fechaFinal)
                                                                             select new CantidadSpools
                                                                             {
                                                                                 ItemCodeIDSam2 = ic.ItemCodeID,
                                                                                 MaterialSpoolID = otm.MaterialSpoolID,
                                                                                 OrdenTrabajoSpoolID = ots.OrdenTrabajoSpoolID,
                                                                                 SpoolID = ots.SpoolID,
                                                                                 OrdenTrabajoID = ot.OrdenTrabajoID
                                                                             }).AsParallel().Distinct().ToList();

                                if (proyectoID > 0)
                                {
                                    int proyectoIDSam2 = (from eq in ctx.Sam3_EquivalenciaProyecto
                                                          where eq.Activo && eq.Sam3_ProyectoID == proyectoID
                                                          select eq.Sam2_ProyectoID).AsParallel().SingleOrDefault();

                                    listaCantidadesSpool = (from lst in listaCantidadesSpool
                                                            join ot in ctx2.OrdenTrabajo on lst.OrdenTrabajoID equals ot.OrdenTrabajoID
                                                            where ot.ProyectoID == proyectoIDSam2
                                                            select lst).AsParallel().ToList();
                                }

                                if (clienteID > 0)
                                {
                                    listaCantidadesSpool = (from lst in listaCantidadesSpool
                                                            join ot in ctx2.OrdenTrabajo on lst.OrdenTrabajoID equals ot.OrdenTrabajoID
                                                            join p in ctx2.Proyecto on ot.ProyectoID equals p.ProyectoID
                                                            join c in ctx2.Cliente on p.ClienteID equals c.ClienteID
                                                            where c.ClienteID == clienteID
                                                            select lst).AsParallel().ToList();
                                }

                                if (folioAvisoEntrada > 0)
                                {
                                    List<int> itemCodesSam3 = (from lst in listaCantidadesSpool
                                                               join eq in ctx.Sam3_EquivalenciaItemCode on lst.ItemCodeIDSam2 equals eq.Sam2_ItemCodeID
                                                               select eq.Sam3_ItemCodeID).AsParallel().ToList();

                                    List<int> foliosIC = (from fae in ctx.Sam3_FolioAvisoEntrada
                                                          join fc in ctx.Sam3_FolioCuantificacion on fae.FolioAvisoEntradaID equals fc.FolioAvisoEntradaID
                                                          join rfc in ctx.Sam3_Rel_FolioCuantificacion_ItemCode on fc.FolioCuantificacionID equals rfc.FolioCuantificacionID
                                                          join rdic in ctx.Sam3_Rel_ItemCode_Diametro on rfc.Rel_ItemCode_Diametro_ID equals rdic.Rel_ItemCode_Diametro_ID
                                                          join ic in ctx.Sam3_ItemCode on rdic.ItemCodeID equals ic.ItemCodeID
                                                          where fae.Activo && fc.Activo && rfc.Activo && rdic.Activo && ic.Activo
                                                          && itemCodesSam3.Contains(ic.ItemCodeID) && fae.FolioAvisoLlegadaID == folioAvisoEntrada
                                                          select ic.ItemCodeID).AsParallel().Distinct().ToList();

                                    List<int> itemCodesSam2 = (from eq in ctx.Sam3_EquivalenciaItemCode
                                                               where eq.Activo && foliosIC.Contains(eq.Sam3_ItemCodeID)
                                                               select eq.Sam2_ItemCodeID).AsParallel().ToList();

                                    listaCantidadesSpool = (from lst in listaCantidadesSpool
                                                            where itemCodesSam2.Contains(lst.ItemCodeIDSam2)
                                                            select lst).AsParallel().ToList();
                                }
                                if (folioCuantificacionID > 0)
                                {
                                    List<int> itemCodesSam3 = (from lst in listaCantidadesSpool
                                                               join eq in ctx.Sam3_EquivalenciaItemCode on lst.ItemCodeIDSam2 equals eq.Sam2_ItemCodeID
                                                               select eq.Sam3_ItemCodeID).AsParallel().ToList();

                                    List<int> foliosIC = (from fc in ctx.Sam3_FolioCuantificacion
                                                          join rfc in ctx.Sam3_Rel_FolioCuantificacion_ItemCode on fc.FolioCuantificacionID equals rfc.FolioCuantificacionID
                                                          join rdic in ctx.Sam3_Rel_ItemCode_Diametro on rfc.Rel_ItemCode_Diametro_ID equals rdic.Rel_ItemCode_Diametro_ID
                                                          join ic in ctx.Sam3_ItemCode on rdic.ItemCodeID equals ic.ItemCodeID
                                                          where fc.Activo && rfc.Activo && rdic.Activo && ic.Activo
                                                          && fc.FolioCuantificacionID == folioCuantificacionID
                                                          && itemCodesSam3.Contains(ic.ItemCodeID)
                                                          select ic.ItemCodeID).AsParallel().Distinct().ToList();

                                    List<int> itemCodesSam2 = (from eq in ctx.Sam3_EquivalenciaItemCode
                                                               where eq.Activo && foliosIC.Contains(eq.Sam3_ItemCodeID)
                                                               select eq.Sam2_ItemCodeID).AsParallel().ToList();

                                    listaCantidadesSpool = (from lst in listaCantidadesSpool
                                                            where itemCodesSam2.Contains(lst.ItemCodeIDSam2)
                                                            select lst).AsParallel().ToList();
                                }

                                List<ListadoPorSpoolDespacho> porSpool = new List<ListadoPorSpoolDespacho>();

                                List<int> spoolsRepetidos = listaCantidadesSpool.GroupBy(x => x.SpoolID).Select(x => x.Key).ToList();

                                foreach (int item in spoolsRepetidos)
                                {
                                    //selecciono los materiales por spool
                                    List<int> MaterialesPorSpool = listaCantidadesSpool.Where(x => x.SpoolID == item).Select(x => x.MaterialSpoolID).AsParallel().ToList();

                                    //cuantos de esos materiales estan despachados
                                    int despachados = (from d in ctx.Sam3_Despacho
                                                       where d.Activo && MaterialesPorSpool.Contains(d.MaterialSpoolID)
                                                       select d.MaterialSpoolID).AsParallel().Count();

                                    //si los que me faltan por despachar son > 0 los agrego a la lista, sino no
                                    if (MaterialesPorSpool.Count() - despachados > 0)
                                    {

                                        porSpool.Add(new ListadoPorSpoolDespacho
                                        {
                                            ID = (from s in ctx2.Spool where s.SpoolID == item select s.Nombre).AsParallel().SingleOrDefault(),
                                            CantidadPorDespachar = (MaterialesPorSpool.Count() - despachados).ToString(),
                                            CantidadDespachada = despachados.ToString(),
                                            CantidadTotal = MaterialesPorSpool.Count().ToString()
                                        });
                                    }
                                }
                                if (conteo)
                                {
                                    return porSpool.Count();
                                }
                                else
                                {
                                    return porSpool;
                                }
                                #endregion
                            case 3: // Toneladas
                                #region Toneladas
                                {
                                    //Obtenemos la lista de los numeros unicos que estan congelados y que aun no tienen despacho ni corte
                                    List<int> sam2NumerosUnicos = new List<int>();
                                    List<CantidadMateriales> InfoNumerosUnicos = new List<CantidadMateriales>();
                                    List<ListadoDespacho> listadoPorDespachar = new List<ListadoDespacho>();
                                    if (proyectoID > 0)
                                    {
                                        int proyectoIDSam2 = (from eq in ctx.Sam3_EquivalenciaProyecto
                                                              where eq.Activo && eq.Sam3_ProyectoID == proyectoID
                                                              select eq.Sam2_ProyectoID).AsParallel().SingleOrDefault();

                                        sam2NumerosUnicos = (from odt in ctx2.OrdenTrabajo
                                                             join odts in ctx2.OrdenTrabajoSpool on odt.OrdenTrabajoID equals odts.OrdenTrabajoID
                                                             join odtm in ctx2.OrdenTrabajoMaterial on odts.OrdenTrabajoSpoolID equals odtm.OrdenTrabajoSpoolID
                                                             join nu in ctx2.NumeroUnico on odtm.NumeroUnicoCongeladoID equals nu.NumeroUnicoID
                                                             join ic in ctx2.ItemCode on nu.ItemCodeID equals ic.ItemCodeID
                                                             join p in ctx2.Proyecto on odt.ProyectoID equals p.ProyectoID
                                                             where proyectosSam2.Contains(p.ProyectoID)
                                                                 && patiosSam2.Contains(p.PatioID)
                                                             && p.ProyectoID == proyectoIDSam2 && ic.TipoMaterialID == tipoMaterialID
                                                             select nu.NumeroUnicoID).AsParallel().Distinct().ToList();
                                    }
                                    else
                                    {
                                        sam2NumerosUnicos = (from odt in ctx2.OrdenTrabajo
                                                             join odts in ctx2.OrdenTrabajoSpool on odt.OrdenTrabajoID equals odts.OrdenTrabajoID
                                                             join odtm in ctx2.OrdenTrabajoMaterial on odts.OrdenTrabajoSpoolID equals odtm.OrdenTrabajoSpoolID
                                                             join nu in ctx2.NumeroUnico on odtm.NumeroUnicoCongeladoID equals nu.NumeroUnicoID
                                                             join ic in ctx2.ItemCode on nu.ItemCodeID equals ic.ItemCodeID
                                                             join p in ctx2.Proyecto on odt.ProyectoID equals p.ProyectoID
                                                             where ic.TipoMaterialID == tipoMaterialID
                                                             && proyectosSam2.Contains(p.ProyectoID)
                                                             && patiosSam2.Contains(p.PatioID)
                                                             select nu.NumeroUnicoID).AsParallel().ToList();
                                    }

                                    InfoNumerosUnicos = (from odt in ctx2.OrdenTrabajo
                                                         join odts in ctx2.OrdenTrabajoSpool on odt.OrdenTrabajoID equals odts.OrdenTrabajoID
                                                         join odtm in ctx2.OrdenTrabajoMaterial on odts.OrdenTrabajoSpoolID equals odtm.OrdenTrabajoSpoolID
                                                         //join nu in ctx2.NumeroUnico on odtm.NumeroUnicoCongeladoID equals nu.NumeroUnicoID
                                                         join ms in ctx2.MaterialSpool on odtm.MaterialSpoolID equals ms.MaterialSpoolID
                                                         join ic in ctx2.ItemCode on ms.ItemCodeID equals ic.ItemCodeID
                                                         join p in ctx2.Proyecto on odt.ProyectoID equals p.ProyectoID
                                                         where ic.TipoMaterialID == tipoMaterialID
                                                              && proyectosSam2.Contains(p.ProyectoID)
                                                              && patiosSam2.Contains(p.PatioID)
                                                         select new CantidadMateriales
                                                         {
                                                             NumeroUnicoID = odtm.NumeroUnicoCongeladoID,
                                                             MaterialSpoolID = odtm.MaterialSpoolID,
                                                             ItemCodeID = ic.ItemCodeID,
                                                             OrdenTrabajoSpoolID = odts.OrdenTrabajoSpoolID
                                                         }).AsParallel().Distinct().ToList();

                                    List<int> sam3NumerosUnicos = (from eq in ctx.Sam3_EquivalenciaNumeroUnico
                                                                   where eq.Activo
                                                                   && sam2NumerosUnicos.Contains(eq.Sam2_NumeroUnicoID)
                                                                   select eq.Sam3_NumeroUnicoID).AsParallel().Distinct().ToList();

                                    #region Numeros Unicos

                                    List<Sam3_NumeroUnico> lstNumUnicos = (from nu in ctx.Sam3_NumeroUnico
                                                                           join ic in ctx.Sam3_ItemCode on nu.ItemCodeID equals ic.ItemCodeID
                                                                           join icd in ctx.Sam3_Rel_ItemCode_Diametro on ic.ItemCodeID equals icd.ItemCodeID
                                                                           join fcic in ctx.Sam3_Rel_FolioCuantificacion_ItemCode on icd.Rel_ItemCode_Diametro_ID equals fcic.Rel_ItemCode_Diametro_ID
                                                                           join fc in ctx.Sam3_FolioCuantificacion on fcic.FolioCuantificacionID equals fc.FolioCuantificacionID
                                                                           //join pred in ctx.Sam3_PreDespacho on nu.NumeroUnicoID equals pred.NumeroUnicoID
                                                                           join p in ctx.Sam3_Proyecto on nu.ProyectoID equals p.ProyectoID
                                                                           join pa in ctx.Sam3_Patio on p.PatioID equals pa.PatioID
                                                                           where nu.Activo
                                                                           && proyectosUsuario.Contains(p.ProyectoID)
                                                                           && patiosUsuario.Contains(pa.PatioID)
                                                                               //&& (pred.FechaPreDespacho >= fechaInicial && pred.FechaPreDespacho <= fechaFinal)
                                                                           && sam3NumerosUnicos.Contains(nu.NumeroUnicoID)
                                                                           && (fc.FechaCreacion >= fechaInicial && fc.FechaCreacion <= fechaFinal)
                                                                           select nu).AsParallel().Distinct().ToList();

                                    if (folioAvisoEntrada > 0)
                                    {
                                        lstNumUnicos = (from nu in lstNumUnicos
                                                        join ic in ctx.Sam3_ItemCode on nu.ItemCodeID equals ic.ItemCodeID
                                                        join fcic in ctx.Sam3_Rel_FolioCuantificacion_ItemCode on ic.ItemCodeID equals fcic.ItemCodeID
                                                        join fc in ctx.Sam3_FolioCuantificacion on fcic.FolioCuantificacionID equals fc.FolioCuantificacionID
                                                        join fe in ctx.Sam3_FolioAvisoEntrada on fc.FolioAvisoEntradaID equals fe.FolioAvisoEntradaID
                                                        where ic.Activo && fcic.Activo && fc.Activo && fe.Activo && fe.FolioAvisoLlegadaID == folioAvisoEntrada
                                                        select nu).AsParallel().Distinct().ToList();
                                    }

                                    if (clienteID > 0)
                                    {
                                        lstNumUnicos = (from nu in lstNumUnicos
                                                        join p in ctx.Sam3_Proyecto on nu.ProyectoID equals p.ProyectoID
                                                        join c in ctx.Sam3_Cliente on p.ClienteID equals c.ClienteID
                                                        where p.Activo && c.Sam2ClienteID == clienteID
                                                        select nu).AsParallel().Distinct().ToList();
                                    }

                                    if (folioCuantificacionID > 0)
                                    {
                                        lstNumUnicos = (from nu in lstNumUnicos
                                                        join it in ctx.Sam3_ItemCode on nu.ItemCodeID equals it.ItemCodeID
                                                        where it.Activo
                                                        && ((from rfi in ctx.Sam3_Rel_FolioCuantificacion_ItemCode
                                                             where rfi.Activo && rfi.FolioCuantificacionID == folioCuantificacionID
                                                             select rfi.ItemCodeID).Contains(it.ItemCodeID)
                                                        || (from rbi in ctx.Sam3_Rel_Bulto_ItemCode
                                                            join b in ctx.Sam3_Bulto on rbi.BultoID equals b.BultoID
                                                            where rbi.Activo && b.Activo
                                                            && b.FolioCuantificacionID == folioCuantificacionID
                                                            select rbi.ItemCodeID).Contains(it.ItemCodeID))
                                                        select nu).AsParallel().Distinct().ToList();
                                    }

                                    #endregion

                                    foreach (Sam3_NumeroUnico item in lstNumUnicos)
                                    {
                                        int itemCode = (from lst in lstNumUnicos
                                                        join ic in ctx.Sam3_ItemCode on lst.ItemCodeID equals ic.ItemCodeID
                                                        where ic.Activo && lst.NumeroUnicoID == item.NumeroUnicoID
                                                        select ic.ItemCodeID).AsParallel().SingleOrDefault();

                                        int itemCodeSam2 = (from eq in ctx.Sam3_EquivalenciaItemCode
                                                            where eq.Activo && eq.Sam3_ItemCodeID == itemCode
                                                            select eq.Sam2_ItemCodeID).AsParallel().SingleOrDefault();

                                        List<int> MaterialesDespachados = InfoNumerosUnicos.Where(x => x.NumeroUnicoID == null && x.ItemCodeID == itemCodeSam2).Select(x => x.MaterialSpoolID).AsParallel().ToList();

                                        //List<int> OrdenTrabajoSpoolPorIC = InfoNumerosUnicos.Where(x => x.ItemCodeID == itemCodeSam2).Select(x => x.OrdenTrabajoSpoolID).AsParallel().ToList();

                                        decimal pesoICS = (from ic in ctx.Sam3_ItemCode
                                                           join rics in ctx.Sam3_Rel_ItemCode_ItemCodeSteelgo on ic.ItemCodeID equals rics.ItemCodeID
                                                           join ics in ctx.Sam3_ItemCodeSteelgo on rics.ItemCodeSteelgoID equals ics.ItemCodeSteelgoID
                                                           where ic.Activo && rics.Activo && ic.ItemCodeID == itemCode
                                                           select ics.Peso).AsParallel().SingleOrDefault();

                                        decimal MaterialesPorIC = InfoNumerosUnicos.Where(x => x.ItemCodeID == itemCodeSam2 && x.NumeroUnicoID != null).Select(x => x.MaterialSpoolID).AsParallel().Count() * pesoICS;

                                        decimal Despachados = (from des in ctx.Sam3_Despacho
                                                               where des.Activo && MaterialesDespachados.Contains(des.MaterialSpoolID)
                                                               select des.DespachoID).AsParallel().Count() * pesoICS;

                                        listadoPorDespachar.Add(new ListadoDespacho
                                        {
                                            IC = (from ic in ctx.Sam3_ItemCode
                                                  where ic.Activo && ic.ItemCodeID == itemCode
                                                  select ic.Codigo).AsParallel().SingleOrDefault(),
                                            DescripcionIC = (from ic in ctx.Sam3_ItemCode
                                                             where ic.Activo && ic.ItemCodeID == itemCode
                                                             select ic.DescripcionEspanol).AsParallel().SingleOrDefault(),
                                            D1 = (from ic in ctx.Sam3_ItemCode
                                                  join di in ctx.Sam3_Rel_ItemCode_Diametro on ic.ItemCodeID equals di.ItemCodeID
                                                  join d in ctx.Sam3_Diametro on di.Diametro1ID equals d.DiametroID
                                                  where ic.Activo && di.Activo && ic.ItemCodeID == itemCode
                                                  select d.Valor.ToString()).AsParallel().SingleOrDefault(),
                                            D2 = (from ic in ctx.Sam3_ItemCode
                                                  join di in ctx.Sam3_Rel_ItemCode_Diametro on ic.ItemCodeID equals di.ItemCodeID
                                                  join d in ctx.Sam3_Diametro on di.Diametro2ID equals d.DiametroID
                                                  where ic.Activo && di.Activo && ic.ItemCodeID == itemCode
                                                  select d.Valor.ToString()).AsParallel().SingleOrDefault(),
                                            Cedula = "",
                                            CantidadDespachada = Despachados.ToString(),
                                            CantidadPorDespachar = MaterialesPorIC.ToString(),
                                            CantidadTotal = (Despachados + MaterialesPorIC).ToString()
                                        });
                                    }

                                    if (conteo)
                                    {
                                        return listadoPorDespachar.GroupBy(x => x.IC).Select(x => x.First()).Count();
                                    }
                                    else
                                    {
                                        return listadoPorDespachar.GroupBy(x => x.IC).Select(x => x.First()).ToList();
                                    }
                                }
                                break;
                                #endregion
                            case 4: // MM
                                #region MM
                                {
                                    //Obtenemos la lista de los numeros unicos que estan congelados y que aun no tienen despacho ni corte
                                    List<int> sam2NumerosUnicos = new List<int>();
                                    List<CantidadMateriales> InfoNumerosUnicos = new List<CantidadMateriales>();
                                    List<ListadoDespacho> listadoPorDespachar = new List<ListadoDespacho>();
                                    if (proyectoID > 0)
                                    {
                                        int proyectoIDSam2 = (from eq in ctx.Sam3_EquivalenciaProyecto
                                                              where eq.Activo && eq.Sam3_ProyectoID == proyectoID
                                                              select eq.Sam2_ProyectoID).AsParallel().SingleOrDefault();

                                        sam2NumerosUnicos = (from odt in ctx2.OrdenTrabajo
                                                             join odts in ctx2.OrdenTrabajoSpool on odt.OrdenTrabajoID equals odts.OrdenTrabajoID
                                                             join odtm in ctx2.OrdenTrabajoMaterial on odts.OrdenTrabajoSpoolID equals odtm.OrdenTrabajoSpoolID
                                                             join nu in ctx2.NumeroUnico on odtm.NumeroUnicoCongeladoID equals nu.NumeroUnicoID
                                                             join ic in ctx2.ItemCode on nu.ItemCodeID equals ic.ItemCodeID
                                                             join p in ctx2.Proyecto on odt.ProyectoID equals p.ProyectoID
                                                             where proyectosSam2.Contains(p.ProyectoID)
                                                                 && patiosSam2.Contains(p.PatioID)
                                                             && p.ProyectoID == proyectoIDSam2 && ic.TipoMaterialID == tipoMaterialID
                                                             select nu.NumeroUnicoID).AsParallel().Distinct().ToList();
                                    }
                                    else
                                    {
                                        sam2NumerosUnicos = (from odt in ctx2.OrdenTrabajo
                                                             join odts in ctx2.OrdenTrabajoSpool on odt.OrdenTrabajoID equals odts.OrdenTrabajoID
                                                             join odtm in ctx2.OrdenTrabajoMaterial on odts.OrdenTrabajoSpoolID equals odtm.OrdenTrabajoSpoolID
                                                             join nu in ctx2.NumeroUnico on odtm.NumeroUnicoCongeladoID equals nu.NumeroUnicoID
                                                             join ic in ctx2.ItemCode on nu.ItemCodeID equals ic.ItemCodeID
                                                             join p in ctx2.Proyecto on odt.ProyectoID equals p.ProyectoID
                                                             where ic.TipoMaterialID == tipoMaterialID
                                                             && proyectosSam2.Contains(p.ProyectoID)
                                                             && patiosSam2.Contains(p.PatioID)
                                                             select nu.NumeroUnicoID).AsParallel().ToList();
                                    }

                                    InfoNumerosUnicos = (from odt in ctx2.OrdenTrabajo
                                                         join odts in ctx2.OrdenTrabajoSpool on odt.OrdenTrabajoID equals odts.OrdenTrabajoID
                                                         join odtm in ctx2.OrdenTrabajoMaterial on odts.OrdenTrabajoSpoolID equals odtm.OrdenTrabajoSpoolID
                                                         //join nu in ctx2.NumeroUnico on odtm.NumeroUnicoCongeladoID equals nu.NumeroUnicoID
                                                         join ms in ctx2.MaterialSpool on odtm.MaterialSpoolID equals ms.MaterialSpoolID
                                                         join ic in ctx2.ItemCode on ms.ItemCodeID equals ic.ItemCodeID
                                                         join p in ctx2.Proyecto on odt.ProyectoID equals p.ProyectoID
                                                         where ic.TipoMaterialID == tipoMaterialID
                                                              && proyectosSam2.Contains(p.ProyectoID)
                                                              && patiosSam2.Contains(p.PatioID)
                                                         select new CantidadMateriales
                                                         {
                                                             NumeroUnicoID = odtm.NumeroUnicoCongeladoID,
                                                             MaterialSpoolID = odtm.MaterialSpoolID,
                                                             ItemCodeID = ic.ItemCodeID,
                                                             OrdenTrabajoSpoolID = odts.OrdenTrabajoSpoolID
                                                         }).AsParallel().Distinct().ToList();

                                    List<int> sam3NumerosUnicos = (from eq in ctx.Sam3_EquivalenciaNumeroUnico
                                                                   where eq.Activo
                                                                   && sam2NumerosUnicos.Contains(eq.Sam2_NumeroUnicoID)
                                                                   select eq.Sam3_NumeroUnicoID).AsParallel().Distinct().ToList();

                                    #region Numeros Unicos

                                    List<Sam3_NumeroUnico> lstNumUnicos = (from nu in ctx.Sam3_NumeroUnico
                                                                           join ic in ctx.Sam3_ItemCode on nu.ItemCodeID equals ic.ItemCodeID
                                                                           join icd in ctx.Sam3_Rel_ItemCode_Diametro on ic.ItemCodeID equals icd.ItemCodeID
                                                                           join fcic in ctx.Sam3_Rel_FolioCuantificacion_ItemCode on icd.Rel_ItemCode_Diametro_ID equals fcic.Rel_ItemCode_Diametro_ID
                                                                           join fc in ctx.Sam3_FolioCuantificacion on fcic.FolioCuantificacionID equals fc.FolioCuantificacionID
                                                                           //join pred in ctx.Sam3_PreDespacho on nu.NumeroUnicoID equals pred.NumeroUnicoID
                                                                           join p in ctx.Sam3_Proyecto on nu.ProyectoID equals p.ProyectoID
                                                                           join pa in ctx.Sam3_Patio on p.PatioID equals pa.PatioID
                                                                           where nu.Activo
                                                                           && proyectosUsuario.Contains(p.ProyectoID)
                                                                           && patiosUsuario.Contains(pa.PatioID)
                                                                               //&& (pred.FechaPreDespacho >= fechaInicial && pred.FechaPreDespacho <= fechaFinal)
                                                                           && sam3NumerosUnicos.Contains(nu.NumeroUnicoID)
                                                                           && (fc.FechaCreacion >= fechaInicial && fc.FechaCreacion <= fechaFinal)
                                                                           select nu).AsParallel().Distinct().ToList();

                                    if (folioAvisoEntrada > 0)
                                    {
                                        lstNumUnicos = (from nu in lstNumUnicos
                                                        join ic in ctx.Sam3_ItemCode on nu.ItemCodeID equals ic.ItemCodeID
                                                        join fcic in ctx.Sam3_Rel_FolioCuantificacion_ItemCode on ic.ItemCodeID equals fcic.ItemCodeID
                                                        join fc in ctx.Sam3_FolioCuantificacion on fcic.FolioCuantificacionID equals fc.FolioCuantificacionID
                                                        join fe in ctx.Sam3_FolioAvisoEntrada on fc.FolioAvisoEntradaID equals fe.FolioAvisoEntradaID
                                                        where ic.Activo && fcic.Activo && fc.Activo && fe.Activo && fe.FolioAvisoLlegadaID == folioAvisoEntrada
                                                        select nu).AsParallel().Distinct().ToList();
                                    }

                                    if (clienteID > 0)
                                    {
                                        lstNumUnicos = (from nu in lstNumUnicos
                                                        join p in ctx.Sam3_Proyecto on nu.ProyectoID equals p.ProyectoID
                                                        join c in ctx.Sam3_Cliente on p.ClienteID equals c.ClienteID
                                                        where p.Activo && c.Sam2ClienteID == clienteID
                                                        select nu).AsParallel().Distinct().ToList();
                                    }

                                    if (folioCuantificacionID > 0)
                                    {
                                        lstNumUnicos = (from nu in lstNumUnicos
                                                        join it in ctx.Sam3_ItemCode on nu.ItemCodeID equals it.ItemCodeID
                                                        where it.Activo
                                                        && ((from rfi in ctx.Sam3_Rel_FolioCuantificacion_ItemCode
                                                             where rfi.Activo && rfi.FolioCuantificacionID == folioCuantificacionID
                                                             select rfi.ItemCodeID).Contains(it.ItemCodeID)
                                                        || (from rbi in ctx.Sam3_Rel_Bulto_ItemCode
                                                            join b in ctx.Sam3_Bulto on rbi.BultoID equals b.BultoID
                                                            where rbi.Activo && b.Activo
                                                            && b.FolioCuantificacionID == folioCuantificacionID
                                                            select rbi.ItemCodeID).Contains(it.ItemCodeID))
                                                        select nu).AsParallel().Distinct().ToList();
                                    }

                                    #endregion

                                    foreach (Sam3_NumeroUnico item in lstNumUnicos)
                                    {
                                        int itemCode = (from lst in lstNumUnicos
                                                        join ic in ctx.Sam3_ItemCode on lst.ItemCodeID equals ic.ItemCodeID
                                                        where ic.Activo && lst.NumeroUnicoID == item.NumeroUnicoID
                                                        select ic.ItemCodeID).AsParallel().SingleOrDefault();

                                        int itemCodeSam2 = (from eq in ctx.Sam3_EquivalenciaItemCode
                                                            where eq.Activo && eq.Sam3_ItemCodeID == itemCode
                                                            select eq.Sam2_ItemCodeID).AsParallel().SingleOrDefault();

                                        List<int> MaterialesDespachados = InfoNumerosUnicos.Where(x => x.NumeroUnicoID == null && x.ItemCodeID == itemCodeSam2).Select(x => x.MaterialSpoolID).AsParallel().ToList();

                                        //List<int> OrdenTrabajoSpoolPorIC = InfoNumerosUnicos.Where(x => x.ItemCodeID == itemCodeSam2).Select(x => x.OrdenTrabajoSpoolID).AsParallel().ToList();

                                        List<int> MaterialesPorIC = InfoNumerosUnicos.Where(x => x.ItemCodeID == itemCodeSam2 && x.NumeroUnicoID != null).Select(x => x.MaterialSpoolID).AsParallel().ToList();

                                        decimal mmPorDespachar = 0;
                                        decimal mmDespachados = 0;

                                        foreach (int material in MaterialesPorIC)
                                        {
                                            mmPorDespachar = mmPorDespachar + (from ms in ctx2.MaterialSpool
                                                                               where ms.MaterialSpoolID == material
                                                                               select ms.Cantidad).AsParallel().SingleOrDefault();
                                        }

                                        List<int> Despachados = (from des in ctx.Sam3_Despacho
                                                                 where des.Activo && MaterialesDespachados.Contains(des.MaterialSpoolID)
                                                                 select des.MaterialSpoolID).AsParallel().ToList();

                                        foreach (int material in Despachados)
                                        {
                                            mmDespachados = mmDespachados + (from ms in ctx2.MaterialSpool
                                                                             where ms.MaterialSpoolID == material
                                                                             select ms.Cantidad).AsParallel().SingleOrDefault();
                                        }

                                        listadoPorDespachar.Add(new ListadoDespacho
                                        {
                                            IC = (from ic in ctx.Sam3_ItemCode
                                                  where ic.Activo && ic.ItemCodeID == itemCode
                                                  select ic.Codigo).AsParallel().SingleOrDefault(),
                                            DescripcionIC = (from ic in ctx.Sam3_ItemCode
                                                             where ic.Activo && ic.ItemCodeID == itemCode
                                                             select ic.DescripcionEspanol).AsParallel().SingleOrDefault(),
                                            D1 = (from ic in ctx.Sam3_ItemCode
                                                  join di in ctx.Sam3_Rel_ItemCode_Diametro on ic.ItemCodeID equals di.ItemCodeID
                                                  join d in ctx.Sam3_Diametro on di.Diametro1ID equals d.DiametroID
                                                  where ic.Activo && di.Activo && ic.ItemCodeID == itemCode
                                                  select d.Valor.ToString()).AsParallel().SingleOrDefault(),
                                            D2 = (from ic in ctx.Sam3_ItemCode
                                                  join di in ctx.Sam3_Rel_ItemCode_Diametro on ic.ItemCodeID equals di.ItemCodeID
                                                  join d in ctx.Sam3_Diametro on di.Diametro2ID equals d.DiametroID
                                                  where ic.Activo && di.Activo && ic.ItemCodeID == itemCode
                                                  select d.Valor.ToString()).AsParallel().SingleOrDefault(),
                                            Cedula = "",
                                            CantidadDespachada = (mmDespachados / 1000).ToString(),
                                            CantidadPorDespachar = (mmPorDespachar / 1000).ToString(),
                                            CantidadTotal = ((mmDespachados / 1000) + (mmPorDespachar / 1000)).ToString()
                                        });
                                    }

                                    if (conteo)
                                    {
                                        return listadoPorDespachar.GroupBy(x => x.IC).Select(x => x.First()).Count();
                                    }
                                    else
                                    {
                                        return listadoPorDespachar.GroupBy(x => x.IC).Select(x => x.First()).ToList();
                                    }
                                }
                                break;
                                #endregion
                            default:
                                throw new Exception("Unidad de medida invalida");
                        }
                    }
                }
                return null;
            }
            catch (Exception ex)
            {
                //-----------------Agregar mensaje al Log -----------------------------------------------
                LoggerBd.Instance.EscribirLog(ex);
                //-----------------Agregar mensaje al Log -----------------------------------------------
                TransactionalInformation result = new TransactionalInformation();
                result.ReturnMessage.Add(ex.Message);
                result.ReturnCode = 500;
                result.ReturnStatus = false;
                result.IsAuthenicated = true;

                return result;
            }
        }


        public object ListadoPorEntregar(FiltrosJson filtros, Sam3_Usuario usuario, bool conteo = false)
        {
            try
            {
                using (SamContext ctx = new SamContext())
                {
                    using (Sam2Context ctx2 = new Sam2Context())
                    {
                        #region filtros
                        int clienteID = filtros.ClienteID != null && filtros.ClienteID != "" ? Convert.ToInt32(filtros.ClienteID) : 0;
                        int proyectoID = filtros.ProyectoID != null && filtros.ProyectoID != "" ? Convert.ToInt32(filtros.ProyectoID) : 0;
                        int folioCuantificacionID = filtros.PackingListID != null && filtros.PackingListID != "" ? Convert.ToInt32(filtros.PackingListID) : 0;
                        int folioAvisoEntrada = filtros.FolioAvisoLlegadaID != null && filtros.FolioAvisoLlegadaID != "" ? Convert.ToInt32(filtros.FolioAvisoLlegadaID) : 0;
                        int tipoMaterialID = filtros.TipoMaterialID != "" ? Convert.ToInt32(filtros.TipoMaterialID) : 0;
                        int unidadDeMedida = 0;
                        List<int> patiosUsuario;
                        List<int> proyectosUsuario;
                        List<int> proyectosSam2;
                        List<int> patiosSam2;
                        //int proyectoIDSam2 = 0;

                        DateTime fechaInicial = new DateTime();
                        DateTime fechaFinal = new DateTime();
                        DateTime.TryParse(filtros.FechaInicial, out fechaInicial);
                        DateTime.TryParse(filtros.FechaFinal, out fechaFinal);
                        UsuarioBd.Instance.ObtenerPatiosYProyectosDeUsuario(usuario.UsuarioID, out proyectosUsuario, out patiosUsuario);

                        proyectosSam2 = (from eq in ctx.Sam3_EquivalenciaProyecto
                                         where eq.Activo
                                         && proyectosUsuario.Contains(eq.Sam3_ProyectoID)
                                         select eq.Sam2_ProyectoID).AsParallel().Distinct().ToList();

                        patiosSam2 = (from eq in ctx.Sam3_EquivalenciaPatio
                                      where eq.Activo
                                      && patiosUsuario.Contains(eq.Sam3_PatioID)
                                      select eq.Sam2_PatioID).AsParallel().Distinct().ToList();

                        if (fechaFinal.ToShortDateString() == "1/1/0001")
                        {
                            fechaFinal = DateTime.Now;
                        }

                        if (fechaInicial.ToShortDateString() == "1/1/0001")
                        {
                            int mes = DateTime.Now.Month != 1 ? DateTime.Now.Month - 1 : 12;
                            int year = DateTime.Now.Month == 1 ? DateTime.Now.Year - 1 : DateTime.Now.Year;
                            fechaInicial = new DateTime(year, mes, DateTime.Now.Day);
                        }

                        if (!String.IsNullOrEmpty(filtros.UnidadDeMedida))
                        {
                            unidadDeMedida = Convert.ToInt32(filtros.UnidadDeMedida);
                        }
                        else
                        {
                            throw new Exception("La unidad de Medida es requerida");
                        }

                        #endregion

                        switch (unidadDeMedida)
                        {
                            case 1: // pieza. Numeros Unicos
                                #region Numeros Unicos
                                {
                                    //Obtenemos la lista de los numeros unicos que estan congelados y que aun no tienen despacho ni corte
                                    List<int> sam2NumerosUnicos = new List<int>();
                                    List<CantidadMateriales> InfoNumerosUnicos = new List<CantidadMateriales>();
                                    List<ListadoEntregaDash> listadoPorEntregar = new List<ListadoEntregaDash>();
                                    if (proyectoID > 0)
                                    {
                                        int proyectoIDSam2 = (from eq in ctx.Sam3_EquivalenciaProyecto
                                                              where eq.Activo && eq.Sam3_ProyectoID == proyectoID
                                                              select eq.Sam2_ProyectoID).AsParallel().SingleOrDefault();

                                        sam2NumerosUnicos = (from odt in ctx2.OrdenTrabajo
                                                             join odts in ctx2.OrdenTrabajoSpool on odt.OrdenTrabajoID equals odts.OrdenTrabajoID
                                                             join odtm in ctx2.OrdenTrabajoMaterial on odts.OrdenTrabajoSpoolID equals odtm.OrdenTrabajoSpoolID
                                                             join nu in ctx2.NumeroUnico on odtm.NumeroUnicoCongeladoID equals nu.NumeroUnicoID
                                                             join ic in ctx2.ItemCode on nu.ItemCodeID equals ic.ItemCodeID
                                                             join p in ctx2.Proyecto on odt.ProyectoID equals p.ProyectoID
                                                             where proyectosSam2.Contains(p.ProyectoID)
                                                                 && patiosSam2.Contains(p.PatioID)
                                                             && p.ProyectoID == proyectoIDSam2 && ic.TipoMaterialID == tipoMaterialID
                                                             select nu.NumeroUnicoID).AsParallel().Distinct().ToList();
                                    }
                                    else
                                    {
                                        sam2NumerosUnicos = (from odt in ctx2.OrdenTrabajo
                                                             join odts in ctx2.OrdenTrabajoSpool on odt.OrdenTrabajoID equals odts.OrdenTrabajoID
                                                             join odtm in ctx2.OrdenTrabajoMaterial on odts.OrdenTrabajoSpoolID equals odtm.OrdenTrabajoSpoolID
                                                             join nu in ctx2.NumeroUnico on odtm.NumeroUnicoCongeladoID equals nu.NumeroUnicoID
                                                             join ic in ctx2.ItemCode on nu.ItemCodeID equals ic.ItemCodeID
                                                             join p in ctx2.Proyecto on odt.ProyectoID equals p.ProyectoID
                                                             where ic.TipoMaterialID == tipoMaterialID
                                                             && proyectosSam2.Contains(p.ProyectoID)
                                                             && patiosSam2.Contains(p.PatioID)
                                                             select nu.NumeroUnicoID).AsParallel().ToList();
                                    }

                                    InfoNumerosUnicos = (from odt in ctx2.OrdenTrabajo
                                                         join odts in ctx2.OrdenTrabajoSpool on odt.OrdenTrabajoID equals odts.OrdenTrabajoID
                                                         join odtm in ctx2.OrdenTrabajoMaterial on odts.OrdenTrabajoSpoolID equals odtm.OrdenTrabajoSpoolID
                                                         //join nu in ctx2.NumeroUnico on odtm.NumeroUnicoCongeladoID equals nu.NumeroUnicoID
                                                         join ms in ctx2.MaterialSpool on odtm.MaterialSpoolID equals ms.MaterialSpoolID
                                                         join ic in ctx2.ItemCode on ms.ItemCodeID equals ic.ItemCodeID
                                                         join p in ctx2.Proyecto on odt.ProyectoID equals p.ProyectoID
                                                         where ic.TipoMaterialID == tipoMaterialID
                                                              && proyectosSam2.Contains(p.ProyectoID)
                                                              && patiosSam2.Contains(p.PatioID)
                                                         select new CantidadMateriales
                                                         {
                                                             NumeroUnicoID = odtm.NumeroUnicoCongeladoID,
                                                             MaterialSpoolID = odtm.MaterialSpoolID,
                                                             ItemCodeID = ic.ItemCodeID,
                                                             OrdenTrabajoSpoolID = odts.OrdenTrabajoSpoolID
                                                         }).AsParallel().Distinct().ToList();

                                    List<int> sam3NumerosUnicos = (from eq in ctx.Sam3_EquivalenciaNumeroUnico
                                                                   where eq.Activo
                                                                   && sam2NumerosUnicos.Contains(eq.Sam2_NumeroUnicoID)
                                                                   select eq.Sam3_NumeroUnicoID).AsParallel().Distinct().ToList();

                                    #region Numeros Unicos

                                    List<Sam3_NumeroUnico> lstNumUnicos = (from nu in ctx.Sam3_NumeroUnico
                                                                           join ic in ctx.Sam3_ItemCode on nu.ItemCodeID equals ic.ItemCodeID
                                                                           join icd in ctx.Sam3_Rel_ItemCode_Diametro on ic.ItemCodeID equals icd.ItemCodeID
                                                                           join fcic in ctx.Sam3_Rel_FolioCuantificacion_ItemCode on icd.Rel_ItemCode_Diametro_ID equals fcic.Rel_ItemCode_Diametro_ID
                                                                           join fc in ctx.Sam3_FolioCuantificacion on fcic.FolioCuantificacionID equals fc.FolioCuantificacionID
                                                                           //join pred in ctx.Sam3_PreDespacho on nu.NumeroUnicoID equals pred.NumeroUnicoID
                                                                           join p in ctx.Sam3_Proyecto on nu.ProyectoID equals p.ProyectoID
                                                                           join pa in ctx.Sam3_Patio on p.PatioID equals pa.PatioID
                                                                           where nu.Activo
                                                                           && proyectosUsuario.Contains(p.ProyectoID)
                                                                           && patiosUsuario.Contains(pa.PatioID)
                                                                               //&& (pred.FechaPreDespacho >= fechaInicial && pred.FechaPreDespacho <= fechaFinal)
                                                                           && sam3NumerosUnicos.Contains(nu.NumeroUnicoID)
                                                                           && (fc.FechaCreacion >= fechaInicial && fc.FechaCreacion <= fechaFinal)
                                                                           select nu).AsParallel().Distinct().ToList();

                                    if (folioAvisoEntrada > 0)
                                    {
                                        lstNumUnicos = (from nu in lstNumUnicos
                                                        join ic in ctx.Sam3_ItemCode on nu.ItemCodeID equals ic.ItemCodeID
                                                        join fcic in ctx.Sam3_Rel_FolioCuantificacion_ItemCode on ic.ItemCodeID equals fcic.ItemCodeID
                                                        join fc in ctx.Sam3_FolioCuantificacion on fcic.FolioCuantificacionID equals fc.FolioCuantificacionID
                                                        join fe in ctx.Sam3_FolioAvisoEntrada on fc.FolioAvisoEntradaID equals fe.FolioAvisoEntradaID
                                                        where ic.Activo && fcic.Activo && fc.Activo && fe.Activo && fe.FolioAvisoLlegadaID == folioAvisoEntrada
                                                        select nu).AsParallel().Distinct().ToList();
                                    }

                                    if (clienteID > 0)
                                    {
                                        lstNumUnicos = (from nu in lstNumUnicos
                                                        join p in ctx.Sam3_Proyecto on nu.ProyectoID equals p.ProyectoID
                                                        join c in ctx.Sam3_Cliente on p.ClienteID equals c.ClienteID
                                                        where p.Activo && c.Sam2ClienteID == clienteID
                                                        select nu).AsParallel().Distinct().ToList();
                                    }

                                    if (folioCuantificacionID > 0)
                                    {
                                        lstNumUnicos = (from nu in lstNumUnicos
                                                        join it in ctx.Sam3_ItemCode on nu.ItemCodeID equals it.ItemCodeID
                                                        where it.Activo
                                                        && ((from rfi in ctx.Sam3_Rel_FolioCuantificacion_ItemCode
                                                             where rfi.Activo && rfi.FolioCuantificacionID == folioCuantificacionID
                                                             select rfi.ItemCodeID).Contains(it.ItemCodeID)
                                                        || (from rbi in ctx.Sam3_Rel_Bulto_ItemCode
                                                            join b in ctx.Sam3_Bulto on rbi.BultoID equals b.BultoID
                                                            where rbi.Activo && b.Activo
                                                            && b.FolioCuantificacionID == folioCuantificacionID
                                                            select rbi.ItemCodeID).Contains(it.ItemCodeID))
                                                        select nu).AsParallel().Distinct().ToList();
                                    }

                                    #endregion

                                    foreach (Sam3_NumeroUnico item in lstNumUnicos)
                                    {
                                        int itemCode = (from lst in lstNumUnicos
                                                        join ic in ctx.Sam3_ItemCode on lst.ItemCodeID equals ic.ItemCodeID
                                                        where ic.Activo && lst.NumeroUnicoID == item.NumeroUnicoID
                                                        select ic.ItemCodeID).AsParallel().SingleOrDefault();

                                        int itemCodeSam2 = (from eq in ctx.Sam3_EquivalenciaItemCode
                                                            where eq.Activo && eq.Sam3_ItemCodeID == itemCode
                                                            select eq.Sam2_ItemCodeID).AsParallel().SingleOrDefault();

                                        //Cuantos despachos tiene
                                        List<int> MaterialesDespachados = InfoNumerosUnicos.Where(x => x.NumeroUnicoID == null && x.ItemCodeID == itemCodeSam2).Select(x => x.MaterialSpoolID).AsParallel().ToList();

                                        List<int> DespachadosPorIC = (from des in ctx.Sam3_Despacho
                                                                      where des.Activo && MaterialesDespachados.Contains(des.MaterialSpoolID)
                                                                      select des.DespachoID).AsParallel().ToList();

                                        //Cuantos ya fueron entregados
                                        List<int> Entregados = (from en in ctx.Sam3_Entrega
                                                                join pt in ctx.Sam3_FolioPickingTicket on en.FolioPickingTicketID equals pt.FolioPickingTicketID
                                                                join d in ctx.Sam3_Despacho on pt.DespachoID equals d.DespachoID
                                                                where en.Activo && pt.Activo && d.Activo && DespachadosPorIC.Contains(d.DespachoID)
                                                                select en.EntregaID).AsParallel().ToList();

                                        if (DespachadosPorIC.Count() - Entregados.Count() > 0)
                                        {
                                            listadoPorEntregar.Add(new ListadoEntregaDash
                                            {
                                                IC = (from ic in ctx.Sam3_ItemCode
                                                      where ic.Activo && ic.ItemCodeID == itemCode
                                                      select ic.Codigo).AsParallel().SingleOrDefault(),
                                                DescripcionIC = (from ic in ctx.Sam3_ItemCode
                                                                 where ic.Activo && ic.ItemCodeID == itemCode
                                                                 select ic.DescripcionEspanol).AsParallel().SingleOrDefault(),
                                                D1 = (from ic in ctx.Sam3_ItemCode
                                                      join di in ctx.Sam3_Rel_ItemCode_Diametro on ic.ItemCodeID equals di.ItemCodeID
                                                      join d in ctx.Sam3_Diametro on di.Diametro1ID equals d.DiametroID
                                                      where ic.Activo && di.Activo && ic.ItemCodeID == itemCode
                                                      select d.Valor.ToString()).AsParallel().SingleOrDefault(),
                                                D2 = (from ic in ctx.Sam3_ItemCode
                                                      join di in ctx.Sam3_Rel_ItemCode_Diametro on ic.ItemCodeID equals di.ItemCodeID
                                                      join d in ctx.Sam3_Diametro on di.Diametro2ID equals d.DiametroID
                                                      where ic.Activo && di.Activo && ic.ItemCodeID == itemCode
                                                      select d.Valor.ToString()).AsParallel().SingleOrDefault(),
                                                Cedula = "",
                                                CantidadEntregada = Entregados.Count().ToString(),
                                                CantidadPorEntregar = (DespachadosPorIC.Count() - Entregados.Count()).ToString(),
                                                CantidadTotal = DespachadosPorIC.Count().ToString()
                                            });
                                        }
                                    }

                                    if (conteo)
                                    {
                                        return listadoPorEntregar.GroupBy(x => x.IC).Select(x => x.First()).Count();
                                    }
                                    else
                                    {
                                        return listadoPorEntregar.GroupBy(x => x.IC).Select(x => x.First()).ToList();
                                    }
                                }
                                #endregion
                                break;
                            case 2: // Spool
                                #region Spool
                                List<CantidadSpools> listaCantidadesSpool = (from ot in ctx2.OrdenTrabajo
                                                                             join ots in ctx2.OrdenTrabajoSpool on ot.OrdenTrabajoID equals ots.OrdenTrabajoID
                                                                             join otm in ctx2.OrdenTrabajoMaterial on ots.OrdenTrabajoSpoolID equals otm.OrdenTrabajoSpoolID
                                                                             //join nu in ctx2.NumeroUnico on otm.NumeroUnicoCongeladoID equals nu.NumeroUnicoID
                                                                             join ms in ctx2.MaterialSpool on otm.MaterialSpoolID equals ms.MaterialSpoolID
                                                                             join ic in ctx2.ItemCode on ms.ItemCodeID equals ic.ItemCodeID
                                                                             where proyectosSam2.Contains(ot.ProyectoID) && ic.TipoMaterialID == tipoMaterialID
                                                                             && (ot.FechaOrden >= fechaInicial && ot.FechaOrden <= fechaFinal)
                                                                             select new CantidadSpools
                                                                             {
                                                                                 ItemCodeIDSam2 = ic.ItemCodeID,
                                                                                 MaterialSpoolID = otm.MaterialSpoolID,
                                                                                 OrdenTrabajoSpoolID = ots.OrdenTrabajoSpoolID,
                                                                                 SpoolID = ots.SpoolID,
                                                                                 OrdenTrabajoID = ot.OrdenTrabajoID
                                                                             }).AsParallel().Distinct().ToList();

                                if (proyectoID > 0)
                                {
                                    int proyectoIDSam2 = (from eq in ctx.Sam3_EquivalenciaProyecto
                                                          where eq.Activo && eq.Sam3_ProyectoID == proyectoID
                                                          select eq.Sam2_ProyectoID).AsParallel().SingleOrDefault();


                                    listaCantidadesSpool = (from lst in listaCantidadesSpool
                                                            join ot in ctx2.OrdenTrabajo on lst.OrdenTrabajoID equals ot.OrdenTrabajoID
                                                            where ot.ProyectoID == proyectoIDSam2
                                                            select lst).AsParallel().ToList();
                                }

                                if (clienteID > 0)
                                {
                                    listaCantidadesSpool = (from lst in listaCantidadesSpool
                                                            join ot in ctx2.OrdenTrabajo on lst.OrdenTrabajoID equals ot.OrdenTrabajoID
                                                            join p in ctx2.Proyecto on ot.ProyectoID equals p.ProyectoID
                                                            join c in ctx2.Cliente on p.ClienteID equals c.ClienteID
                                                            where c.ClienteID == clienteID
                                                            select lst).AsParallel().ToList();
                                }

                                if (folioAvisoEntrada > 0)
                                {
                                    List<int> itemCodesSam3 = (from lst in listaCantidadesSpool
                                                               join eq in ctx.Sam3_EquivalenciaItemCode on lst.ItemCodeIDSam2 equals eq.Sam2_ItemCodeID
                                                               select eq.Sam3_ItemCodeID).AsParallel().ToList();

                                    List<int> foliosIC = (from fae in ctx.Sam3_FolioAvisoEntrada
                                                          join fc in ctx.Sam3_FolioCuantificacion on fae.FolioAvisoEntradaID equals fc.FolioAvisoEntradaID
                                                          join rfc in ctx.Sam3_Rel_FolioCuantificacion_ItemCode on fc.FolioCuantificacionID equals rfc.FolioCuantificacionID
                                                          join rdic in ctx.Sam3_Rel_ItemCode_Diametro on rfc.Rel_ItemCode_Diametro_ID equals rdic.Rel_ItemCode_Diametro_ID
                                                          join ic in ctx.Sam3_ItemCode on rdic.ItemCodeID equals ic.ItemCodeID
                                                          where fae.Activo && fc.Activo && rfc.Activo && rdic.Activo && ic.Activo
                                                          && itemCodesSam3.Contains(ic.ItemCodeID) && fae.FolioAvisoLlegadaID == folioAvisoEntrada
                                                          select ic.ItemCodeID).AsParallel().Distinct().ToList();

                                    List<int> itemCodesSam2 = (from eq in ctx.Sam3_EquivalenciaItemCode
                                                               where eq.Activo && foliosIC.Contains(eq.Sam3_ItemCodeID)
                                                               select eq.Sam2_ItemCodeID).AsParallel().ToList();

                                    listaCantidadesSpool = (from lst in listaCantidadesSpool
                                                            where itemCodesSam2.Contains(lst.ItemCodeIDSam2)
                                                            select lst).AsParallel().ToList();
                                }
                                if (folioCuantificacionID > 0)
                                {
                                    List<int> itemCodesSam3 = (from lst in listaCantidadesSpool
                                                               join eq in ctx.Sam3_EquivalenciaItemCode on lst.ItemCodeIDSam2 equals eq.Sam2_ItemCodeID
                                                               select eq.Sam3_ItemCodeID).AsParallel().ToList();

                                    List<int> foliosIC = (from fc in ctx.Sam3_FolioCuantificacion
                                                          join rfc in ctx.Sam3_Rel_FolioCuantificacion_ItemCode on fc.FolioCuantificacionID equals rfc.FolioCuantificacionID
                                                          join rdic in ctx.Sam3_Rel_ItemCode_Diametro on rfc.Rel_ItemCode_Diametro_ID equals rdic.Rel_ItemCode_Diametro_ID
                                                          join ic in ctx.Sam3_ItemCode on rdic.ItemCodeID equals ic.ItemCodeID
                                                          where fc.Activo && rfc.Activo && rdic.Activo && ic.Activo
                                                          && fc.FolioCuantificacionID == folioCuantificacionID
                                                          && itemCodesSam3.Contains(ic.ItemCodeID)
                                                          select ic.ItemCodeID).AsParallel().Distinct().ToList();

                                    List<int> itemCodesSam2 = (from eq in ctx.Sam3_EquivalenciaItemCode
                                                               where eq.Activo && foliosIC.Contains(eq.Sam3_ItemCodeID)
                                                               select eq.Sam2_ItemCodeID).AsParallel().ToList();

                                    listaCantidadesSpool = (from lst in listaCantidadesSpool
                                                            where itemCodesSam2.Contains(lst.ItemCodeIDSam2)
                                                            select lst).AsParallel().ToList();
                                }

                                List<ListadoPorSpoolEntrega> porSpool = new List<ListadoPorSpoolEntrega>();

                                List<int> spoolsRepetidos = listaCantidadesSpool.GroupBy(x => x.SpoolID).Select(x => x.Key).ToList();

                                foreach (int item in spoolsRepetidos)
                                {
                                    //selecciono los materiales por spool
                                    List<int> MaterialesPorSpool = listaCantidadesSpool.Where(x => x.SpoolID == item).Select(x => x.MaterialSpoolID).AsParallel().ToList();

                                    //cuantos de esos materiales estan despachados
                                    List<int> despachados = (from d in ctx.Sam3_Despacho
                                                             where d.Activo && MaterialesPorSpool.Contains(d.MaterialSpoolID)
                                                             select d.DespachoID).AsParallel().ToList();

                                    //cuantos han sido entregados
                                    List<int> Entregados = (from en in ctx.Sam3_Entrega
                                                            join pt in ctx.Sam3_FolioPickingTicket on en.FolioPickingTicketID equals pt.FolioPickingTicketID
                                                            join d in ctx.Sam3_Despacho on pt.DespachoID equals d.DespachoID
                                                            where en.Activo && pt.Activo && d.Activo && despachados.Contains(d.DespachoID)
                                                            select en.EntregaID).AsParallel().ToList();

                                    if (despachados.Count() - Entregados.Count() > 0)
                                    {

                                        porSpool.Add(new ListadoPorSpoolEntrega
                                        {
                                            ID = (from s in ctx2.Spool where s.SpoolID == item select s.Nombre).AsParallel().SingleOrDefault(),
                                            CantidadPorEntregar = (despachados.Count() - Entregados.Count()).ToString(),
                                            CantidadEntregada = Entregados.Count().ToString(),
                                            CantidadTotal = despachados.Count().ToString()
                                        });
                                    }
                                }

                                if (conteo)
                                {
                                    return porSpool.Count();
                                }
                                else
                                {
                                    return porSpool;
                                }
                                #endregion
                                break;
                            case 3: // Toneladas
                                #region Toneladas
                                {
                                    //Obtenemos la lista de los numeros unicos que estan congelados y que aun no tienen despacho ni corte
                                    List<int> sam2NumerosUnicos = new List<int>();
                                    List<CantidadMateriales> InfoNumerosUnicos = new List<CantidadMateriales>();
                                    List<ListadoEntregaDash> listadoPorEntregar = new List<ListadoEntregaDash>();
                                    if (proyectoID > 0)
                                    {
                                        int proyectoIDSam2 = (from eq in ctx.Sam3_EquivalenciaProyecto
                                                              where eq.Activo && eq.Sam3_ProyectoID == proyectoID
                                                              select eq.Sam2_ProyectoID).AsParallel().SingleOrDefault();

                                        sam2NumerosUnicos = (from odt in ctx2.OrdenTrabajo
                                                             join odts in ctx2.OrdenTrabajoSpool on odt.OrdenTrabajoID equals odts.OrdenTrabajoID
                                                             join odtm in ctx2.OrdenTrabajoMaterial on odts.OrdenTrabajoSpoolID equals odtm.OrdenTrabajoSpoolID
                                                             join nu in ctx2.NumeroUnico on odtm.NumeroUnicoCongeladoID equals nu.NumeroUnicoID
                                                             join ic in ctx2.ItemCode on nu.ItemCodeID equals ic.ItemCodeID
                                                             join p in ctx2.Proyecto on odt.ProyectoID equals p.ProyectoID
                                                             where proyectosSam2.Contains(p.ProyectoID)
                                                                 && patiosSam2.Contains(p.PatioID)
                                                             && p.ProyectoID == proyectoIDSam2 && ic.TipoMaterialID == tipoMaterialID
                                                             select nu.NumeroUnicoID).AsParallel().Distinct().ToList();
                                    }
                                    else
                                    {
                                        sam2NumerosUnicos = (from odt in ctx2.OrdenTrabajo
                                                             join odts in ctx2.OrdenTrabajoSpool on odt.OrdenTrabajoID equals odts.OrdenTrabajoID
                                                             join odtm in ctx2.OrdenTrabajoMaterial on odts.OrdenTrabajoSpoolID equals odtm.OrdenTrabajoSpoolID
                                                             join nu in ctx2.NumeroUnico on odtm.NumeroUnicoCongeladoID equals nu.NumeroUnicoID
                                                             join ic in ctx2.ItemCode on nu.ItemCodeID equals ic.ItemCodeID
                                                             join p in ctx2.Proyecto on odt.ProyectoID equals p.ProyectoID
                                                             where ic.TipoMaterialID == tipoMaterialID
                                                             && proyectosSam2.Contains(p.ProyectoID)
                                                             && patiosSam2.Contains(p.PatioID)
                                                             select nu.NumeroUnicoID).AsParallel().ToList();
                                    }

                                    InfoNumerosUnicos = (from odt in ctx2.OrdenTrabajo
                                                         join odts in ctx2.OrdenTrabajoSpool on odt.OrdenTrabajoID equals odts.OrdenTrabajoID
                                                         join odtm in ctx2.OrdenTrabajoMaterial on odts.OrdenTrabajoSpoolID equals odtm.OrdenTrabajoSpoolID
                                                         //join nu in ctx2.NumeroUnico on odtm.NumeroUnicoCongeladoID equals nu.NumeroUnicoID
                                                         join ms in ctx2.MaterialSpool on odtm.MaterialSpoolID equals ms.MaterialSpoolID
                                                         join ic in ctx2.ItemCode on ms.ItemCodeID equals ic.ItemCodeID
                                                         join p in ctx2.Proyecto on odt.ProyectoID equals p.ProyectoID
                                                         where ic.TipoMaterialID == tipoMaterialID
                                                              && proyectosSam2.Contains(p.ProyectoID)
                                                              && patiosSam2.Contains(p.PatioID)
                                                         select new CantidadMateriales
                                                         {
                                                             NumeroUnicoID = odtm.NumeroUnicoCongeladoID,
                                                             MaterialSpoolID = odtm.MaterialSpoolID,
                                                             ItemCodeID = ic.ItemCodeID,
                                                             OrdenTrabajoSpoolID = odts.OrdenTrabajoSpoolID
                                                         }).AsParallel().Distinct().ToList();

                                    List<int> sam3NumerosUnicos = (from eq in ctx.Sam3_EquivalenciaNumeroUnico
                                                                   where eq.Activo
                                                                   && sam2NumerosUnicos.Contains(eq.Sam2_NumeroUnicoID)
                                                                   select eq.Sam3_NumeroUnicoID).AsParallel().Distinct().ToList();

                                    #region Numeros Unicos

                                    List<Sam3_NumeroUnico> lstNumUnicos = (from nu in ctx.Sam3_NumeroUnico
                                                                           join ic in ctx.Sam3_ItemCode on nu.ItemCodeID equals ic.ItemCodeID
                                                                           join icd in ctx.Sam3_Rel_ItemCode_Diametro on ic.ItemCodeID equals icd.ItemCodeID
                                                                           join fcic in ctx.Sam3_Rel_FolioCuantificacion_ItemCode on icd.Rel_ItemCode_Diametro_ID equals fcic.Rel_ItemCode_Diametro_ID
                                                                           join fc in ctx.Sam3_FolioCuantificacion on fcic.FolioCuantificacionID equals fc.FolioCuantificacionID
                                                                           //join pred in ctx.Sam3_PreDespacho on nu.NumeroUnicoID equals pred.NumeroUnicoID
                                                                           join p in ctx.Sam3_Proyecto on nu.ProyectoID equals p.ProyectoID
                                                                           join pa in ctx.Sam3_Patio on p.PatioID equals pa.PatioID
                                                                           where nu.Activo
                                                                           && proyectosUsuario.Contains(p.ProyectoID)
                                                                           && patiosUsuario.Contains(pa.PatioID)
                                                                               //&& (pred.FechaPreDespacho >= fechaInicial && pred.FechaPreDespacho <= fechaFinal)
                                                                           && sam3NumerosUnicos.Contains(nu.NumeroUnicoID)
                                                                           && (fc.FechaCreacion >= fechaInicial && fc.FechaCreacion <= fechaFinal)
                                                                           select nu).AsParallel().Distinct().ToList();

                                    if (folioAvisoEntrada > 0)
                                    {
                                        lstNumUnicos = (from nu in lstNumUnicos
                                                        join ic in ctx.Sam3_ItemCode on nu.ItemCodeID equals ic.ItemCodeID
                                                        join fcic in ctx.Sam3_Rel_FolioCuantificacion_ItemCode on ic.ItemCodeID equals fcic.ItemCodeID
                                                        join fc in ctx.Sam3_FolioCuantificacion on fcic.FolioCuantificacionID equals fc.FolioCuantificacionID
                                                        join fe in ctx.Sam3_FolioAvisoEntrada on fc.FolioAvisoEntradaID equals fe.FolioAvisoEntradaID
                                                        where ic.Activo && fcic.Activo && fc.Activo && fe.Activo && fe.FolioAvisoLlegadaID == folioAvisoEntrada
                                                        select nu).AsParallel().Distinct().ToList();
                                    }

                                    if (clienteID > 0)
                                    {
                                        lstNumUnicos = (from nu in lstNumUnicos
                                                        join p in ctx.Sam3_Proyecto on nu.ProyectoID equals p.ProyectoID
                                                        join c in ctx.Sam3_Cliente on p.ClienteID equals c.ClienteID
                                                        where p.Activo && c.Sam2ClienteID == clienteID
                                                        select nu).AsParallel().Distinct().ToList();
                                    }

                                    if (folioCuantificacionID > 0)
                                    {
                                        lstNumUnicos = (from nu in lstNumUnicos
                                                        join it in ctx.Sam3_ItemCode on nu.ItemCodeID equals it.ItemCodeID
                                                        where it.Activo
                                                        && ((from rfi in ctx.Sam3_Rel_FolioCuantificacion_ItemCode
                                                             where rfi.Activo && rfi.FolioCuantificacionID == folioCuantificacionID
                                                             select rfi.ItemCodeID).Contains(it.ItemCodeID)
                                                        || (from rbi in ctx.Sam3_Rel_Bulto_ItemCode
                                                            join b in ctx.Sam3_Bulto on rbi.BultoID equals b.BultoID
                                                            where rbi.Activo && b.Activo
                                                            && b.FolioCuantificacionID == folioCuantificacionID
                                                            select rbi.ItemCodeID).Contains(it.ItemCodeID))
                                                        select nu).AsParallel().Distinct().ToList();
                                    }

                                    #endregion

                                    foreach (Sam3_NumeroUnico item in lstNumUnicos)
                                    {
                                        int itemCode = (from lst in lstNumUnicos
                                                        join ic in ctx.Sam3_ItemCode on lst.ItemCodeID equals ic.ItemCodeID
                                                        where ic.Activo && lst.NumeroUnicoID == item.NumeroUnicoID
                                                        select ic.ItemCodeID).AsParallel().SingleOrDefault();

                                        int itemCodeSam2 = (from eq in ctx.Sam3_EquivalenciaItemCode
                                                            where eq.Activo && eq.Sam3_ItemCodeID == itemCode
                                                            select eq.Sam2_ItemCodeID).AsParallel().SingleOrDefault();

                                        List<int> MaterialesDespachados = InfoNumerosUnicos.Where(x => x.NumeroUnicoID == null && x.ItemCodeID == itemCodeSam2).Select(x => x.MaterialSpoolID).AsParallel().ToList();

                                        decimal pesoICS = (from ic in ctx.Sam3_ItemCode
                                                           join rics in ctx.Sam3_Rel_ItemCode_ItemCodeSteelgo on ic.ItemCodeID equals rics.ItemCodeID
                                                           join ics in ctx.Sam3_ItemCodeSteelgo on rics.ItemCodeSteelgoID equals ics.ItemCodeSteelgoID
                                                           where ic.Activo && rics.Activo && ic.ItemCodeID == itemCode
                                                           select ics.Peso).AsParallel().SingleOrDefault();


                                        List<int> Despachados = (from des in ctx.Sam3_Despacho
                                                                 where des.Activo && MaterialesDespachados.Contains(des.MaterialSpoolID)
                                                                 select des.DespachoID).AsParallel().ToList();


                                        //Cuantos ya fueron entregados
                                        List<int> Entregados = (from en in ctx.Sam3_Entrega
                                                                join pt in ctx.Sam3_FolioPickingTicket on en.FolioPickingTicketID equals pt.FolioPickingTicketID
                                                                join d in ctx.Sam3_Despacho on pt.DespachoID equals d.DespachoID
                                                                where en.Activo && pt.Activo && d.Activo && Despachados.Contains(d.DespachoID)
                                                                select en.EntregaID).AsParallel().ToList();

                                        if (Despachados.Count() - Entregados.Count() > 0)
                                        {
                                            listadoPorEntregar.Add(new ListadoEntregaDash
                                             {
                                                 IC = (from ic in ctx.Sam3_ItemCode
                                                       where ic.Activo && ic.ItemCodeID == itemCode
                                                       select ic.Codigo).AsParallel().SingleOrDefault(),
                                                 DescripcionIC = (from ic in ctx.Sam3_ItemCode
                                                                  where ic.Activo && ic.ItemCodeID == itemCode
                                                                  select ic.DescripcionEspanol).AsParallel().SingleOrDefault(),
                                                 D1 = (from ic in ctx.Sam3_ItemCode
                                                       join di in ctx.Sam3_Rel_ItemCode_Diametro on ic.ItemCodeID equals di.ItemCodeID
                                                       join d in ctx.Sam3_Diametro on di.Diametro1ID equals d.DiametroID
                                                       where ic.Activo && di.Activo && ic.ItemCodeID == itemCode
                                                       select d.Valor.ToString()).AsParallel().SingleOrDefault(),
                                                 D2 = (from ic in ctx.Sam3_ItemCode
                                                       join di in ctx.Sam3_Rel_ItemCode_Diametro on ic.ItemCodeID equals di.ItemCodeID
                                                       join d in ctx.Sam3_Diametro on di.Diametro2ID equals d.DiametroID
                                                       where ic.Activo && di.Activo && ic.ItemCodeID == itemCode
                                                       select d.Valor.ToString()).AsParallel().SingleOrDefault(),
                                                 Cedula = "",
                                                 CantidadEntregada = (Entregados.Count() * pesoICS).ToString(),
                                                 CantidadPorEntregar = ((Despachados.Count() * pesoICS) - (Entregados.Count() * pesoICS)).ToString(),
                                                 CantidadTotal = (Despachados.Count() * pesoICS).ToString()
                                             });
                                        }
                                    }

                                    if (conteo)
                                    {
                                        return listadoPorEntregar.GroupBy(x => x.IC).Select(x => x.First()).Count();
                                    }
                                    else
                                    {
                                        return listadoPorEntregar.GroupBy(x => x.IC).Select(x => x.First()).ToList();
                                    }
                                }
                                break;
                                #endregion
                                break;
                            case 4: // MM
                                #region MM
                                {
                                    //Obtenemos la lista de los numeros unicos que estan congelados y que aun no tienen despacho ni corte
                                    List<int> sam2NumerosUnicos = new List<int>();
                                    List<CantidadMateriales> InfoNumerosUnicos = new List<CantidadMateriales>();
                                    List<ListadoEntregaDash> listadoPorEntregar = new List<ListadoEntregaDash>();
                                    if (proyectoID > 0)
                                    {
                                        int proyectoIDSam2 = (from eq in ctx.Sam3_EquivalenciaProyecto
                                                              where eq.Activo && eq.Sam3_ProyectoID == proyectoID
                                                              select eq.Sam2_ProyectoID).AsParallel().SingleOrDefault();

                                        sam2NumerosUnicos = (from odt in ctx2.OrdenTrabajo
                                                             join odts in ctx2.OrdenTrabajoSpool on odt.OrdenTrabajoID equals odts.OrdenTrabajoID
                                                             join odtm in ctx2.OrdenTrabajoMaterial on odts.OrdenTrabajoSpoolID equals odtm.OrdenTrabajoSpoolID
                                                             join nu in ctx2.NumeroUnico on odtm.NumeroUnicoCongeladoID equals nu.NumeroUnicoID
                                                             join ic in ctx2.ItemCode on nu.ItemCodeID equals ic.ItemCodeID
                                                             join p in ctx2.Proyecto on odt.ProyectoID equals p.ProyectoID
                                                             where proyectosSam2.Contains(p.ProyectoID)
                                                                 && patiosSam2.Contains(p.PatioID)
                                                             && p.ProyectoID == proyectoIDSam2 && ic.TipoMaterialID == tipoMaterialID
                                                             select nu.NumeroUnicoID).AsParallel().Distinct().ToList();
                                    }
                                    else
                                    {
                                        sam2NumerosUnicos = (from odt in ctx2.OrdenTrabajo
                                                             join odts in ctx2.OrdenTrabajoSpool on odt.OrdenTrabajoID equals odts.OrdenTrabajoID
                                                             join odtm in ctx2.OrdenTrabajoMaterial on odts.OrdenTrabajoSpoolID equals odtm.OrdenTrabajoSpoolID
                                                             join nu in ctx2.NumeroUnico on odtm.NumeroUnicoCongeladoID equals nu.NumeroUnicoID
                                                             join ic in ctx2.ItemCode on nu.ItemCodeID equals ic.ItemCodeID
                                                             join p in ctx2.Proyecto on odt.ProyectoID equals p.ProyectoID
                                                             where ic.TipoMaterialID == tipoMaterialID
                                                             && proyectosSam2.Contains(p.ProyectoID)
                                                             && patiosSam2.Contains(p.PatioID)
                                                             select nu.NumeroUnicoID).AsParallel().ToList();
                                    }

                                    InfoNumerosUnicos = (from odt in ctx2.OrdenTrabajo
                                                         join odts in ctx2.OrdenTrabajoSpool on odt.OrdenTrabajoID equals odts.OrdenTrabajoID
                                                         join odtm in ctx2.OrdenTrabajoMaterial on odts.OrdenTrabajoSpoolID equals odtm.OrdenTrabajoSpoolID
                                                         //join nu in ctx2.NumeroUnico on odtm.NumeroUnicoCongeladoID equals nu.NumeroUnicoID
                                                         join ms in ctx2.MaterialSpool on odtm.MaterialSpoolID equals ms.MaterialSpoolID
                                                         join ic in ctx2.ItemCode on ms.ItemCodeID equals ic.ItemCodeID
                                                         join p in ctx2.Proyecto on odt.ProyectoID equals p.ProyectoID
                                                         where ic.TipoMaterialID == tipoMaterialID
                                                              && proyectosSam2.Contains(p.ProyectoID)
                                                              && patiosSam2.Contains(p.PatioID)
                                                         select new CantidadMateriales
                                                         {
                                                             NumeroUnicoID = odtm.NumeroUnicoCongeladoID,
                                                             MaterialSpoolID = odtm.MaterialSpoolID,
                                                             ItemCodeID = ic.ItemCodeID,
                                                             OrdenTrabajoSpoolID = odts.OrdenTrabajoSpoolID
                                                         }).AsParallel().Distinct().ToList();

                                    List<int> sam3NumerosUnicos = (from eq in ctx.Sam3_EquivalenciaNumeroUnico
                                                                   where eq.Activo
                                                                   && sam2NumerosUnicos.Contains(eq.Sam2_NumeroUnicoID)
                                                                   select eq.Sam3_NumeroUnicoID).AsParallel().Distinct().ToList();

                                    #region Numeros Unicos

                                    List<Sam3_NumeroUnico> lstNumUnicos = (from nu in ctx.Sam3_NumeroUnico
                                                                           join ic in ctx.Sam3_ItemCode on nu.ItemCodeID equals ic.ItemCodeID
                                                                           join icd in ctx.Sam3_Rel_ItemCode_Diametro on ic.ItemCodeID equals icd.ItemCodeID
                                                                           join fcic in ctx.Sam3_Rel_FolioCuantificacion_ItemCode on icd.Rel_ItemCode_Diametro_ID equals fcic.Rel_ItemCode_Diametro_ID
                                                                           join fc in ctx.Sam3_FolioCuantificacion on fcic.FolioCuantificacionID equals fc.FolioCuantificacionID
                                                                           //join pred in ctx.Sam3_PreDespacho on nu.NumeroUnicoID equals pred.NumeroUnicoID
                                                                           join p in ctx.Sam3_Proyecto on nu.ProyectoID equals p.ProyectoID
                                                                           join pa in ctx.Sam3_Patio on p.PatioID equals pa.PatioID
                                                                           where nu.Activo
                                                                           && proyectosUsuario.Contains(p.ProyectoID)
                                                                           && patiosUsuario.Contains(pa.PatioID)
                                                                               //&& (pred.FechaPreDespacho >= fechaInicial && pred.FechaPreDespacho <= fechaFinal)
                                                                           && sam3NumerosUnicos.Contains(nu.NumeroUnicoID)
                                                                           && (fc.FechaCreacion >= fechaInicial && fc.FechaCreacion <= fechaFinal)
                                                                           select nu).AsParallel().Distinct().ToList();

                                    if (folioAvisoEntrada > 0)
                                    {
                                        lstNumUnicos = (from nu in lstNumUnicos
                                                        join ic in ctx.Sam3_ItemCode on nu.ItemCodeID equals ic.ItemCodeID
                                                        join fcic in ctx.Sam3_Rel_FolioCuantificacion_ItemCode on ic.ItemCodeID equals fcic.ItemCodeID
                                                        join fc in ctx.Sam3_FolioCuantificacion on fcic.FolioCuantificacionID equals fc.FolioCuantificacionID
                                                        join fe in ctx.Sam3_FolioAvisoEntrada on fc.FolioAvisoEntradaID equals fe.FolioAvisoEntradaID
                                                        where ic.Activo && fcic.Activo && fc.Activo && fe.Activo && fe.FolioAvisoLlegadaID == folioAvisoEntrada
                                                        select nu).AsParallel().Distinct().ToList();
                                    }

                                    if (clienteID > 0)
                                    {
                                        lstNumUnicos = (from nu in lstNumUnicos
                                                        join p in ctx.Sam3_Proyecto on nu.ProyectoID equals p.ProyectoID
                                                        join c in ctx.Sam3_Cliente on p.ClienteID equals c.ClienteID
                                                        where p.Activo && c.Sam2ClienteID == clienteID
                                                        select nu).AsParallel().Distinct().ToList();
                                    }

                                    if (folioCuantificacionID > 0)
                                    {
                                        lstNumUnicos = (from nu in lstNumUnicos
                                                        join it in ctx.Sam3_ItemCode on nu.ItemCodeID equals it.ItemCodeID
                                                        where it.Activo
                                                        && ((from rfi in ctx.Sam3_Rel_FolioCuantificacion_ItemCode
                                                             where rfi.Activo && rfi.FolioCuantificacionID == folioCuantificacionID
                                                             select rfi.ItemCodeID).Contains(it.ItemCodeID)
                                                        || (from rbi in ctx.Sam3_Rel_Bulto_ItemCode
                                                            join b in ctx.Sam3_Bulto on rbi.BultoID equals b.BultoID
                                                            where rbi.Activo && b.Activo
                                                            && b.FolioCuantificacionID == folioCuantificacionID
                                                            select rbi.ItemCodeID).Contains(it.ItemCodeID))
                                                        select nu).AsParallel().Distinct().ToList();
                                    }

                                    #endregion

                                    foreach (Sam3_NumeroUnico item in lstNumUnicos)
                                    {
                                        int itemCode = (from lst in lstNumUnicos
                                                        join ic in ctx.Sam3_ItemCode on lst.ItemCodeID equals ic.ItemCodeID
                                                        where ic.Activo && lst.NumeroUnicoID == item.NumeroUnicoID
                                                        select ic.ItemCodeID).AsParallel().SingleOrDefault();

                                        int itemCodeSam2 = (from eq in ctx.Sam3_EquivalenciaItemCode
                                                            where eq.Activo && eq.Sam3_ItemCodeID == itemCode
                                                            select eq.Sam2_ItemCodeID).AsParallel().SingleOrDefault();

                                        List<int> MaterialesDespachados = InfoNumerosUnicos.Where(x => x.NumeroUnicoID == null && x.ItemCodeID == itemCodeSam2).Select(x => x.MaterialSpoolID).AsParallel().ToList();

                                        decimal mmEntregados = 0;
                                        decimal mmDespachados = 0;

                                        List<int> Despachados = (from des in ctx.Sam3_Despacho
                                                                 where des.Activo && MaterialesDespachados.Contains(des.MaterialSpoolID)
                                                                 select des.MaterialSpoolID).AsParallel().ToList();

                                        foreach (int material in Despachados)
                                        {
                                            mmDespachados = mmDespachados + (from ms in ctx2.MaterialSpool
                                                                             where ms.MaterialSpoolID == material
                                                                             select ms.Cantidad).AsParallel().SingleOrDefault();
                                        }

                                        //Cuantos ya fueron entregados
                                        List<int> Entregados = (from en in ctx.Sam3_Entrega
                                                                join pt in ctx.Sam3_FolioPickingTicket on en.FolioPickingTicketID equals pt.FolioPickingTicketID
                                                                join d in ctx.Sam3_Despacho on pt.DespachoID equals d.DespachoID
                                                                where en.Activo && pt.Activo && d.Activo && Despachados.Contains(d.MaterialSpoolID)
                                                                select d.MaterialSpoolID).AsParallel().ToList();

                                        foreach (int material in Entregados)
                                        {
                                            mmEntregados = mmEntregados + (from ms in ctx2.MaterialSpool
                                                                           where ms.MaterialSpoolID == material
                                                                           select ms.Cantidad).AsParallel().SingleOrDefault();
                                        }

                                        if ((mmDespachados / 100) - (mmEntregados / 100) > 0)
                                        {
                                            listadoPorEntregar.Add(new ListadoEntregaDash
                                            {
                                                IC = (from ic in ctx.Sam3_ItemCode
                                                      where ic.Activo && ic.ItemCodeID == itemCode
                                                      select ic.Codigo).AsParallel().SingleOrDefault(),
                                                DescripcionIC = (from ic in ctx.Sam3_ItemCode
                                                                 where ic.Activo && ic.ItemCodeID == itemCode
                                                                 select ic.DescripcionEspanol).AsParallel().SingleOrDefault(),
                                                D1 = (from ic in ctx.Sam3_ItemCode
                                                      join di in ctx.Sam3_Rel_ItemCode_Diametro on ic.ItemCodeID equals di.ItemCodeID
                                                      join d in ctx.Sam3_Diametro on di.Diametro1ID equals d.DiametroID
                                                      where ic.Activo && di.Activo && ic.ItemCodeID == itemCode
                                                      select d.Valor.ToString()).AsParallel().SingleOrDefault(),
                                                D2 = (from ic in ctx.Sam3_ItemCode
                                                      join di in ctx.Sam3_Rel_ItemCode_Diametro on ic.ItemCodeID equals di.ItemCodeID
                                                      join d in ctx.Sam3_Diametro on di.Diametro2ID equals d.DiametroID
                                                      where ic.Activo && di.Activo && ic.ItemCodeID == itemCode
                                                      select d.Valor.ToString()).AsParallel().SingleOrDefault(),
                                                Cedula = "",
                                                CantidadEntregada = (mmEntregados / 100).ToString(),
                                                CantidadPorEntregar = ((mmDespachados / 100) - (mmEntregados / 100)).ToString(),
                                                CantidadTotal = (mmDespachados / 100).ToString()
                                            });
                                        }
                                    }

                                    if (conteo)
                                    {
                                        return listadoPorEntregar.GroupBy(x => x.IC).Select(x => x.First()).Count();
                                    }
                                    else
                                    {
                                        return listadoPorEntregar.GroupBy(x => x.IC).Select(x => x.First()).ToList();
                                    }
                                }
                                break;
                                #endregion
                                break;
                            default:
                                throw new Exception("Unidad de medida invalida");
                        }
                    }
                }
                return null;
            }
            catch (Exception ex)
            {
                //-----------------Agregar mensaje al Log -----------------------------------------------
                LoggerBd.Instance.EscribirLog(ex);
                //-----------------Agregar mensaje al Log -----------------------------------------------
                TransactionalInformation result = new TransactionalInformation();
                result.ReturnMessage.Add(ex.Message);
                result.ReturnCode = 500;
                result.ReturnStatus = false;
                result.IsAuthenicated = true;

                return result;
            }
        }

        public object ListadoTravelerPendiente(FiltrosJson filtros, Sam3_Usuario usuario, bool conteo = false)
        {
            try
            {
                using (SamContext ctx = new SamContext())
                {
                    using (Sam2Context ctx2 = new Sam2Context())
                    {
                        #region filtros
                        int clienteID = filtros.ClienteID != null && filtros.ClienteID != "" ? Convert.ToInt32(filtros.ClienteID) : 0;
                        int proyectoID = filtros.ProyectoID != null && filtros.ProyectoID != "" ? Convert.ToInt32(filtros.ProyectoID) : 0;
                        int folioCuantificacionID = filtros.PackingListID != null && filtros.PackingListID != "" ? Convert.ToInt32(filtros.PackingListID) : 0;
                        int folioAvisoEntrada = filtros.FolioAvisoLlegadaID != null && filtros.FolioAvisoLlegadaID != "" ? Convert.ToInt32(filtros.FolioAvisoLlegadaID) : 0;
                        int tipoMaterialID = filtros.TipoMaterialID != "" ? Convert.ToInt32(filtros.TipoMaterialID) : 0;
                        int unidadDeMedida = 0;
                        List<int> patiosUsuario;
                        List<int> proyectosUsuario;
                        List<int> proyectosSam2;
                        List<int> patiosSam2;
                        //int proyectoIDSam2 = 0;

                        DateTime fechaInicial = new DateTime();
                        DateTime fechaFinal = new DateTime();
                        DateTime.TryParse(filtros.FechaInicial, out fechaInicial);
                        DateTime.TryParse(filtros.FechaFinal, out fechaFinal);
                        UsuarioBd.Instance.ObtenerPatiosYProyectosDeUsuario(usuario.UsuarioID, out proyectosUsuario, out patiosUsuario);

                        proyectosSam2 = (from eq in ctx.Sam3_EquivalenciaProyecto
                                         where eq.Activo
                                         && proyectosUsuario.Contains(eq.Sam3_ProyectoID)
                                         select eq.Sam2_ProyectoID).AsParallel().Distinct().ToList();

                        patiosSam2 = (from eq in ctx.Sam3_EquivalenciaPatio
                                      where eq.Activo
                                      && patiosUsuario.Contains(eq.Sam3_PatioID)
                                      select eq.Sam2_PatioID).AsParallel().Distinct().ToList();

                        if (fechaFinal.ToShortDateString() == "1/1/0001")
                        {
                            fechaFinal = DateTime.Now;
                        }

                        if (fechaInicial.ToShortDateString() == "1/1/0001")
                        {
                            int mes = DateTime.Now.Month != 1 ? DateTime.Now.Month - 1 : 12;
                            int year = DateTime.Now.Month == 1 ? DateTime.Now.Year - 1 : DateTime.Now.Year;
                            fechaInicial = new DateTime(year, mes, DateTime.Now.Day);
                        }

                        if (!String.IsNullOrEmpty(filtros.UnidadDeMedida))
                        {
                            unidadDeMedida = Convert.ToInt32(filtros.UnidadDeMedida);
                        }
                        else
                        {
                            throw new Exception("La unidad de Medida es requerida");
                        }

                        #endregion

                        switch (unidadDeMedida)
                        {
                            case 1: // pieza. Numeros Unicos
                                #region Numeros Unicos
                                {
                                    List<CantidadSpools> porSpool = new List<CantidadSpools>();
                                    if (conteo)
                                    {
                                        return porSpool.Count();
                                    }
                                    else
                                    {
                                        return porSpool;
                                    }
                                }
                                #endregion
                                break;
                            case 2: // Spool
                                #region Spool
                                {
                                    List<CantidadSpools> listaCantidadesSpool = (from ot in ctx2.OrdenTrabajo
                                                                                 join ots in ctx2.OrdenTrabajoSpool on ot.OrdenTrabajoID equals ots.OrdenTrabajoID
                                                                                 join otm in ctx2.OrdenTrabajoMaterial on ots.OrdenTrabajoSpoolID equals otm.OrdenTrabajoSpoolID
                                                                                 //join nu in ctx2.NumeroUnico on otm.NumeroUnicoCongeladoID equals nu.NumeroUnicoID
                                                                                 join ms in ctx2.MaterialSpool on otm.MaterialSpoolID equals ms.MaterialSpoolID
                                                                                 join ic in ctx2.ItemCode on ms.ItemCodeID equals ic.ItemCodeID
                                                                                 where proyectosSam2.Contains(ot.ProyectoID) && ic.TipoMaterialID == tipoMaterialID
                                                                                 && (ot.FechaOrden >= fechaInicial && ot.FechaOrden <= fechaFinal)
                                                                                 select new CantidadSpools
                                                                                 {
                                                                                     ItemCodeIDSam2 = ic.ItemCodeID,
                                                                                     MaterialSpoolID = otm.MaterialSpoolID,
                                                                                     OrdenTrabajoSpoolID = ots.OrdenTrabajoSpoolID,
                                                                                     SpoolID = ots.SpoolID,
                                                                                     OrdenTrabajoID = ot.OrdenTrabajoID
                                                                                 }).AsParallel().Distinct().ToList();

                                    List<int> conImpresion = (from imp in ctx.Sam3_FolioImpresionDocumental
                                                              where imp.Activo //&& listaCantidadesSpool.Any(x => x.OrdenTrabajoSpoolID == imp.SpoolID)
                                                              select imp.SpoolID).AsParallel().ToList();

                                    listaCantidadesSpool = (from lst in listaCantidadesSpool
                                                            where !conImpresion.Contains(lst.OrdenTrabajoSpoolID)
                                                            select lst).AsParallel().ToList();

                                    if (proyectoID > 0)
                                    {
                                        int proyectoIDSam2 = (from eq in ctx.Sam3_EquivalenciaProyecto
                                                              where eq.Activo && eq.Sam3_ProyectoID == proyectoID
                                                              select eq.Sam2_ProyectoID).AsParallel().SingleOrDefault();

                                        listaCantidadesSpool = (from lst in listaCantidadesSpool
                                                                join ot in ctx2.OrdenTrabajo on lst.OrdenTrabajoID equals ot.OrdenTrabajoID
                                                                where ot.ProyectoID == proyectoIDSam2
                                                                select lst).AsParallel().ToList();
                                    }

                                    if (clienteID > 0)
                                    {
                                        listaCantidadesSpool = (from lst in listaCantidadesSpool
                                                                join ot in ctx2.OrdenTrabajo on lst.OrdenTrabajoID equals ot.OrdenTrabajoID
                                                                join p in ctx2.Proyecto on ot.ProyectoID equals p.ProyectoID
                                                                join c in ctx2.Cliente on p.ClienteID equals c.ClienteID
                                                                where c.ClienteID == clienteID
                                                                select lst).AsParallel().ToList();
                                    }

                                    if (folioAvisoEntrada > 0)
                                    {
                                        List<int> itemCodesSam3 = (from lst in listaCantidadesSpool
                                                                   join eq in ctx.Sam3_EquivalenciaItemCode on lst.ItemCodeIDSam2 equals eq.Sam2_ItemCodeID
                                                                   select eq.Sam3_ItemCodeID).AsParallel().ToList();

                                        List<int> foliosIC = (from fae in ctx.Sam3_FolioAvisoEntrada
                                                              join fc in ctx.Sam3_FolioCuantificacion on fae.FolioAvisoEntradaID equals fc.FolioAvisoEntradaID
                                                              join rfc in ctx.Sam3_Rel_FolioCuantificacion_ItemCode on fc.FolioCuantificacionID equals rfc.FolioCuantificacionID
                                                              join rdic in ctx.Sam3_Rel_ItemCode_Diametro on rfc.Rel_ItemCode_Diametro_ID equals rdic.Rel_ItemCode_Diametro_ID
                                                              join ic in ctx.Sam3_ItemCode on rdic.ItemCodeID equals ic.ItemCodeID
                                                              where fae.Activo && fc.Activo && rfc.Activo && rdic.Activo && ic.Activo
                                                              && itemCodesSam3.Contains(ic.ItemCodeID) && fae.FolioAvisoLlegadaID == folioAvisoEntrada
                                                              select ic.ItemCodeID).AsParallel().Distinct().ToList();

                                        List<int> itemCodesSam2 = (from eq in ctx.Sam3_EquivalenciaItemCode
                                                                   where eq.Activo && foliosIC.Contains(eq.Sam3_ItemCodeID)
                                                                   select eq.Sam2_ItemCodeID).AsParallel().ToList();

                                        listaCantidadesSpool = (from lst in listaCantidadesSpool
                                                                where itemCodesSam2.Contains(lst.ItemCodeIDSam2)
                                                                select lst).AsParallel().ToList();
                                    }
                                    if (folioCuantificacionID > 0)
                                    {
                                        List<int> itemCodesSam3 = (from lst in listaCantidadesSpool
                                                                   join eq in ctx.Sam3_EquivalenciaItemCode on lst.ItemCodeIDSam2 equals eq.Sam2_ItemCodeID
                                                                   select eq.Sam3_ItemCodeID).AsParallel().ToList();

                                        List<int> foliosIC = (from fc in ctx.Sam3_FolioCuantificacion
                                                              join rfc in ctx.Sam3_Rel_FolioCuantificacion_ItemCode on fc.FolioCuantificacionID equals rfc.FolioCuantificacionID
                                                              join rdic in ctx.Sam3_Rel_ItemCode_Diametro on rfc.Rel_ItemCode_Diametro_ID equals rdic.Rel_ItemCode_Diametro_ID
                                                              join ic in ctx.Sam3_ItemCode on rdic.ItemCodeID equals ic.ItemCodeID
                                                              where fc.Activo && rfc.Activo && rdic.Activo && ic.Activo
                                                              && fc.FolioCuantificacionID == folioCuantificacionID
                                                              && itemCodesSam3.Contains(ic.ItemCodeID)
                                                              select ic.ItemCodeID).AsParallel().Distinct().ToList();

                                        List<int> itemCodesSam2 = (from eq in ctx.Sam3_EquivalenciaItemCode
                                                                   where eq.Activo && foliosIC.Contains(eq.Sam3_ItemCodeID)
                                                                   select eq.Sam2_ItemCodeID).AsParallel().ToList();

                                        listaCantidadesSpool = (from lst in listaCantidadesSpool
                                                                where itemCodesSam2.Contains(lst.ItemCodeIDSam2)
                                                                select lst).AsParallel().ToList();
                                    }

                                    List<ListadoTravelerPendientePorSpool> porSpool = new List<ListadoTravelerPendientePorSpool>();

                                    List<int> OTspoolsRepetidos = listaCantidadesSpool.GroupBy(x => x.OrdenTrabajoSpoolID).Select(x => x.Key).ToList();

                                    foreach (int item in OTspoolsRepetidos)
                                    {
                                        ListadoTravelerPendientePorSpool traveler = new ListadoTravelerPendientePorSpool();

                                        traveler = (from odts in ctx2.OrdenTrabajoSpool
                                                    join odtm in ctx2.OrdenTrabajoMaterial on odts.OrdenTrabajoSpoolID equals odtm.OrdenTrabajoSpoolID
                                                    join sp in ctx2.Spool on odts.SpoolID equals sp.SpoolID
                                                    where odts.OrdenTrabajoSpoolID == item
                                                    select new ListadoTravelerPendientePorSpool
                                                    {
                                                        ProyectoID = sp.ProyectoID,
                                                        Spool = sp.Nombre,
                                                        SpoolID = odts.NumeroControl,
                                                        NumeroControlID = odts.OrdenTrabajoSpoolID.ToString(),
                                                    }).Distinct().AsParallel().SingleOrDefault();

                                        porSpool.Add(traveler);
                                    }

                                    if (conteo)
                                    {
                                        return porSpool.Count();
                                    }
                                    else
                                    {
                                        return porSpool;
                                    }
                                #endregion
                                }
                                break;
                            case 3: // Toneladas
                                #region Toneladas
                                {
                                    List<CantidadSpools> porSpool = new List<CantidadSpools>();
                                    if (conteo)
                                    {
                                        return porSpool.Count();
                                    }
                                    else
                                    {
                                        return porSpool;
                                    }
                                }
                                #endregion
                                break;
                            case 4: // MM
                                #region MM
                                {
                                    List<CantidadSpools> porSpool = new List<CantidadSpools>();
                                    if (conteo)
                                    {
                                        return porSpool.Count();
                                    }
                                    else
                                    {
                                        return porSpool;
                                    }
                                }
                                #endregion
                                break;
                            default:
                                throw new Exception("Unidad de medida invalida");
                        }
                    }
                }
                return null;
            }
            catch (Exception ex)
            {
                //-----------------Agregar mensaje al Log -----------------------------------------------
                LoggerBd.Instance.EscribirLog(ex);
                //-----------------Agregar mensaje al Log -----------------------------------------------
                TransactionalInformation result = new TransactionalInformation();
                result.ReturnMessage.Add(ex.Message);
                result.ReturnCode = 500;
                result.ReturnStatus = false;
                result.IsAuthenicated = true;

                return result;
            }
        }
    }
}