﻿using BackEndSAM.Models;
using DatabaseManager.Sam3;
using SecurityManager.Api.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace BackEndSAM.DataAcces
{
    public class ListadoMaterialesBd
    {

        private static readonly object _mutex = new object();
        private static ListadoMaterialesBd _instance;

        /// <summary>
        /// constructor privado para implementar el patron Singleton
        /// </summary>
        private ListadoMaterialesBd()
        {
        }

        /// <summary>
        /// crea una instancia de la clase
        /// </summary>
        public static ListadoMaterialesBd Instance
        {
            get
            {
                lock (_mutex)
                {
                    if (_instance == null)
                    {
                        _instance = new ListadoMaterialesBd();
                    }
                }
                return _instance;
            }
        }
        /// <summary>
        /// Funcion para obtener los patios del usuario
        /// Combo para el Listado de Materiales
        /// </summary>
        /// <param name="usuario"></param>
        /// <returns></returns>
        public object obtenerPatioListadoMateriales(Sam3_Usuario usuario)
        {
            try
            {
                using (SamContext ctx = new SamContext())
                {
                    //Patios y proyectos del usuario
                    List<int> proyectos = ctx.Sam3_Rel_Usuario_Proyecto.Where(x => x.UsuarioID == usuario.UsuarioID).Select(x => x.ProyectoID).AsParallel().ToList();

                    List<Patio> patios = (from r in ctx.Sam3_Proyecto
                                          join p in ctx.Sam3_Patio on r.PatioID equals p.PatioID
                                          where r.Activo && p.Activo && proyectos.Contains(r.ProyectoID)
                                          select new Patio
                                          {
                                              PatioID = p.PatioID.ToString(),
                                              Nombre = p.Nombre
                                          }).AsParallel().GroupBy(x => x.PatioID).Select(x => x.First()).ToList();

                    return patios;
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

        /// <summary>
        /// Funcion para obtener los proyectos del usuario
        /// Combo Proyectos Listado de Materiales
        /// </summary>
        /// <param name="patioID"></param>
        /// <param name="usuario"></param>
        /// <returns></returns>
        public object obtenerProyectoListadoMateriales(string patioID, Sam3_Usuario usuario)
        {
            try
            {
                using (SamContext ctx = new SamContext())
                {
                    List<Proyecto> proyectos = (from rup in ctx.Sam3_Rel_Usuario_Proyecto
                                                join pr in ctx.Sam3_Proyecto on rup.ProyectoID equals pr.ProyectoID
                                                where rup.Activo && pr.Activo
                                                && rup.UsuarioID == usuario.UsuarioID
                                                && pr.PatioID.ToString() == patioID
                                                select new Proyecto
                                                {
                                                    ProyectoID = pr.ProyectoID.ToString(),
                                                    Nombre = pr.Nombre
                                                }).AsParallel().ToList();

                    return proyectos;
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

        /// <summary>
        /// Obtener los folios cuantificacion
        /// Combo Folio Packing List en Listado de MAteriales
        /// </summary>
        /// <param name="folioLlegadaID"></param>
        /// <param name="proyectoID"></param>
        /// <returns></returns>
        public object obtenerFolioCuantificacionListadoMateriales(string folioLlegadaID, string proyectoID)
        {
            try
            {
                using (SamContext ctx = new SamContext())
                {
                    List<ListaCombos> folios = (from fc in ctx.Sam3_FolioCuantificacion
                                        join ave in ctx.Sam3_FolioAvisoEntrada on fc.FolioAvisoEntradaID equals ave.FolioAvisoEntradaID
                                        where fc.Activo && ave.Activo
                                        && ave.FolioAvisoLlegadaID.ToString() == folioLlegadaID
                                        && fc.ProyectoID.ToString() == proyectoID
                                        select  new ListaCombos
                                        {
                                            id = fc.FolioCuantificacionID.ToString(),
                                            value = fc.FolioCuantificacionID.ToString()
                                        }).AsParallel().ToList();
                    return folios;
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

        /// <summary>
        /// Obtener la informacion del grid de listado de materiales
        /// </summary>
        /// <param name="folioCuantificacion"></param>
        /// <returns></returns>
        public object cargarGridListado(string folioCuantificacion)
        {
            try
            {
                using (SamContext ctx = new SamContext())
                {
                    List<ListadoMaterialesPorPL> lista = new List<ListadoMaterialesPorPL>();

                    lista = (from rfc in ctx.Sam3_Rel_FolioCuantificacion_ItemCode
                             join ic in ctx.Sam3_ItemCode on rfc.ItemCodeID equals ic.ItemCodeID
                             join rics in ctx.Sam3_Rel_ItemCode_ItemCodeSteelgo on ic.ItemCodeID equals rics.ItemCodeID
                             join ics in ctx.Sam3_ItemCodeSteelgo on rics.ItemCodeSteelgoID equals ics.ItemCodeSteelgoID
                             join nu in ctx.Sam3_NumeroUnico on ic.ItemCodeID equals nu.ItemCodeID
                             join fa in ctx.Sam3_FamiliaAcero on ics.FamiliaAceroID equals fa.FamiliaAceroID
                             join fm in ctx.Sam3_FamiliaMaterial on fa.FamiliaMaterialID equals fm.FamiliaMaterialID
                             where rfc.Activo && ic.Activo && rics.Activo && ics.Activo && nu.Activo && fa.Activo && fm.Activo &&
                             rfc.FolioCuantificacionID.ToString() == folioCuantificacion
                             select new ListadoMaterialesPorPL
                             {
                                 NumeroUnicoID = nu.NumeroUnicoID.ToString(),
                                 NumeroUnico = nu.Prefijo + "-" + nu.Consecutivo,
                                 ItemCodeID = ic.ItemCodeID.ToString(),
                                 ItemCode = ic.Codigo,
                                 ItemCodeSteelgo = ics.Codigo,
                                 Descripcion = ics.DescripcionEspanol,
                                 Cedula = ics.Cedula,
                                 TipoAcero = fm.Nombre,
                                 D1 = ics.Diametro1.ToString(),
                                 D2 = ics.Diametro2.ToString(),
                                 RangoInferior = (from pc in ctx.Sam3_ProyectoConfiguracion
                                                      where pc.Activo &&
                                                      pc.ProyectoID == ic.ProyectoID
                                                      select pc.ToleranciaCortes).FirstOrDefault().ToString(),
                                 RangoSuperior = (from pc in ctx.Sam3_ProyectoConfiguracion
                                                         where pc.Activo &&
                                                         pc.ProyectoID == ic.ProyectoID
                                                         select pc.ToleranciaCortes).FirstOrDefault().ToString(),
                                 Cantidad = ic.Cantidad.ToString(),
                                 Colada = ic.ColadaID.ToString(),
                                 EstatusFisico = ic.EstatusFisico,
                                 EstatusDocumental = ic.EstatusDocumental,
                                 AlmacenVirtual = "X"
                             }).AsParallel().ToList();

                    foreach (var i in lista)
                    {
                        int itemcodeID = Convert.ToInt32(i.ItemCodeID);
                        int numeroDigitos = (from it in ctx.Sam3_ItemCode
                                             join pc in ctx.Sam3_ProyectoConfiguracion on it.ProyectoID equals pc.ProyectoID
                                             where it.ItemCodeID == itemcodeID
                                             select pc.DigitosNumeroUnico).AsParallel().SingleOrDefault();

                        string formato = "D" + numeroDigitos.ToString();

                        string[] codigo = i.NumeroUnico.Split('-').ToArray();
                        int consecutivo = Convert.ToInt32(codigo[1]);
                        i.NumeroUnico = codigo[0] + "-" + consecutivo.ToString(formato);
                    }
                    return lista;
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
    }
}