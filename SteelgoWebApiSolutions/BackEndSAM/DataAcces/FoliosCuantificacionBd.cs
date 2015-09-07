﻿using BackEndSAM.Models;
using DatabaseManager.Sam3;
using SecurityManager.Api.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace BackEndSAM.DataAcces
{
    public class FoliosCuantificacionBd
    {
        private static readonly object _mutex = new object();
        private static FoliosCuantificacionBd _instance;

        /// <summary>
        /// constructor privado para implementar el patron Singleton
        /// </summary>
        private FoliosCuantificacionBd()
        {
        }

        /// <summary>
        /// crea una instancia de la clase
        /// </summary>
        public static FoliosCuantificacionBd Instance
        {
            get
            {
                lock (_mutex)
                {
                    if (_instance == null)
                    {
                        _instance = new FoliosCuantificacionBd();
                    }
                }
                return _instance;
            }
        }

        /// <summary>
        /// Obtener Folio AViso Llegada (Combo Folio Aviso Entrada)
        /// </summary>
        /// <returns>Lista de Folios de Aviso de llegada</returns>
        public object obtenerFolioLlegada()
        {
            List<ListaCombos> listFE = new List<ListaCombos>();
            try
            {
                using (SamContext ctx = new SamContext())
                {
                    listFE = (from t in ctx.Sam3_FolioAvisoEntrada
                              where t.FolioDescarga != 0 && t.Activo == true
                              select new ListaCombos
                                {
                                    id = t.FolioAvisoLlegadaID.ToString(),
                                    value = t.FolioAvisoLlegadaID.ToString()
                                }).AsParallel().ToList();
                }
                return listFE;

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
        ///Obtiene la informacion de un aviso de entrada en cuantificacion
        /// </summary>
        /// <param name="avisoEntrada">aviso de entrada seleccionado</param>
        /// <returns>objeto con la informacion</returns>
        public object obtenerDatosFolioEntrada(int avisoEntrada)
        {
            List<Proyecto> proyectos = new List<Proyecto>();
            List<FolioLlegada1> cuantificacion = new List<FolioLlegada1>();

            try
            {
                using (SamContext ctx = new SamContext())
                {
                    proyectos = (from t in ctx.Sam3_Rel_FolioAvisoLlegada_Proyecto
                                 join p in ctx.Sam3_Proyecto on t.ProyectoID equals p.ProyectoID
                                 join e in ctx.Sam3_FolioAvisoEntrada on avisoEntrada equals e.FolioAvisoLlegadaID
                                 where t.FolioAvisoLlegadaID == e.FolioAvisoLlegadaID
                                 select new Proyecto
                                 {
                                     ProyectoID = p.ProyectoID.ToString(),
                                     Nombre = p.Nombre
                                 }).AsParallel().ToList();

                    foreach (var item in proyectos)
                    {
                        if (String.IsNullOrEmpty(item.ProyectoID) || item.ProyectoID.Contains("1"))
                        {
                            proyectos = (from t in ctx.Sam3_Proyecto
                                         where t.Activo == true
                                         select new Proyecto
                                         {
                                             ProyectoID = t.ProyectoID.ToString(),
                                             Nombre = t.Nombre
                                         }).AsParallel().ToList();
                        }
                    }

                    cuantificacion = (from t in ctx.Sam3_FolioCuantificacion
                                      where t.Activo == true
                                      select new FolioLlegada1
                                           {
                                               FolioCuantificacionID = t.FolioCuantificacionID,
                                               FolioAvisoEntradaID = t.FolioCuantificacionID
                                           }).AsParallel().ToList();



                    InfoFolioAvisoEntrada info = new InfoFolioAvisoEntrada();
                    info.Proyecto = proyectos;
                    info.FolioLlegada = cuantificacion;

                    return info;
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
        /// Actualizar proyecto cuando el folio de av llegada no tiene proyecto seleccionado
        /// </summary>
        /// <param name="avisoLlegada">folio de aviso de llegada</param>
        /// <param name="proyectoID">id del proyecto seleccionado</param>
        /// <param name="usuarioID">usuario </param>
        /// <returns></returns>
        public object actualizarProyectos(int avisoLlegada, int proyectoID, int usuarioID)
        {
            try
            {
                using (SamContext ctx = new SamContext())
                {
                    Sam3_Rel_FolioAvisoLlegada_Proyecto avPR = new Sam3_Rel_FolioAvisoLlegada_Proyecto();
                    avPR.FolioAvisoLlegadaID = avisoLlegada;
                    avPR.ProyectoID = proyectoID;
                    avPR.Activo = true;
                    avPR.UsuarioModificacion = usuarioID;
                    avPR.FechaModificacion = DateTime.Now;

                    ctx.Sam3_Rel_FolioAvisoLlegada_Proyecto.Add(avPR);
                    ctx.SaveChanges();

                    TransactionalInformation result = new TransactionalInformation();
                    result.ReturnMessage.Add("Ok");
                    result.ReturnMessage.Add(avPR.Rel_FolioAviso_ProyectoID.ToString());
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


        /// <summary>
        /// Obtener datos de un folio de aviso de entrada / folio cuantificacion
        /// </summary>
        /// <param name="avisoLlegada"></param>
        /// <param name="folioCuantificacion"></param>
        /// <returns></returns>
        public object obtenerDatosFolioCuantificacion(int avisoEntrada, int folioCuantificacion)
        {
            try
            {
                InfoFolioCuantificacion info = new InfoFolioCuantificacion();

                using (SamContext ctx = new SamContext())
                {
                    info = (from t in ctx.Sam3_FolioCuantificacion
                            where t.FolioAvisoEntradaID == avisoEntrada && t.FolioCuantificacionID == folioCuantificacion
                            join tu in ctx.Sam3_TipoUso on t.TipoUsoID equals tu.TipoUsoID
                            select new InfoFolioCuantificacion
                            {
                                ProyectoID = t.ProyectoID,
                                PackingList = t.PackingList,
                               
                                TipoUso = new TipoUso()
                                {
                                    id = t.TipoUsoID.ToString(),
                                    Nombre = tu.Nombre
                                },
                            
                               TipoPackingList = new TipoPackingList()
                               {
                                   id = (from c in ctx.Sam3_Rel_FolioCuantificacion_ItemCode
                                              where c.FolioCuantificacionID == folioCuantificacion
                                              join ic in ctx.Sam3_ItemCode on c.ItemCodeID equals ic.ItemCodeID
                                              select ic.TipoMaterialID).FirstOrDefault().ToString()
                               },

                                Estatus = t.Estatus,

                                FolioLlegadaHijo = (from b in ctx.Sam3_Bulto
                                             where b.FolioCuantificacionID == folioCuantificacion
                                             select b.BultoID).FirstOrDefault()

                            }).AsParallel().FirstOrDefault();
                }

                return info;
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

        public object obtenerDatosGrid()
        {
            try
            {
                InfoFolioAvisoEntrada info = new InfoFolioAvisoEntrada();




                return info;
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