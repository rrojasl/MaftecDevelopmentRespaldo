﻿using BackEndSAM.Models;
using DatabaseManager.Sam2;
using DatabaseManager.Sam3;
using SecurityManager.Api.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace BackEndSAM.DataAcces
{
    public class PreDespachoBd
    {
        private static readonly object _mutex = new object();
        private static PreDespachoBd _instance;

        /// <summary>
        /// constructor privado para implementar el patron Singleton
        /// </summary>
        private PreDespachoBd()
        {
        }

        /// <summary>
        /// crea una instancia de la clase
        /// </summary>
        public static PreDespachoBd Instance
        {
            get
            {
                lock (_mutex)
                {
                    if (_instance == null)
                    {
                        _instance = new PreDespachoBd();
                    }
                }
                return _instance;
            }
        }

        /// <summary>
        /// Funcion para obtener los datos del grid de pre despacho
        /// </summary>
        /// <param name="spoolID"></param>
        /// <param name="usuario"></param>
        /// <returns></returns>
        public object ObtenerGridPreDespacho(int spoolID, Sam3_Usuario usuario)
        {
            try
            {
                List<int> proyectos = new List<int>();
                List<int> patios = new List<int>();
                using (SamContext ctx = new SamContext())
                {
                    proyectos = (from p in ctx.Sam3_Rel_Usuario_Proyecto
                                 join eqp in ctx.Sam3_EquivalenciaProyecto on p.ProyectoID equals eqp.Sam3_ProyectoID
                                 where p.Activo && eqp.Activo
                                 && p.UsuarioID == usuario.UsuarioID
                                 select eqp.Sam2_ProyectoID).Distinct().AsParallel().ToList();

                    proyectos = proyectos.Where(x => x > 0).ToList();

                    patios = (from p in ctx.Sam3_Proyecto
                              join pa in ctx.Sam3_Patio on p.PatioID equals pa.PatioID
                              join eq in ctx.Sam3_EquivalenciaPatio on pa.PatioID equals eq.Sam2_PatioID
                              where p.Activo && pa.Activo && eq.Activo
                              && proyectos.Contains(p.ProyectoID)
                              select eq.Sam2_PatioID).Distinct().AsParallel().ToList();

                    patios = patios.Where(x => x > 0).ToList();

                    using (Sam2Context ctx2 = new Sam2Context())
                    {
                        List<PreDespacho> listado = (from ot in ctx2.OrdenTrabajo
                                                     join ots in ctx2.OrdenTrabajoSpool on ot.OrdenTrabajoID equals ots.OrdenTrabajoID
                                                     join ms in ctx2.MaterialSpool on ots.SpoolID equals ms.SpoolID
                                                     join ic in ctx2.ItemCode on ms.ItemCodeID equals ic.ItemCodeID
                                                     where ots.OrdenTrabajoSpoolID == spoolID
                                                     && proyectos.Contains(ot.ProyectoID)
                                                     && ic.TipoMaterialID == 2
                                                     select new PreDespacho
                                                     {
                                                         ItemCodeID = ic.ItemCodeID.ToString(),
                                                         ItemCode = ic.Codigo,
                                                         NumeroControlID = ots.OrdenTrabajoSpoolID.ToString(),
                                                         NumeroControl = ots.NumeroControl,
                                                         Descripcion = ic.DescripcionEspanol
                                                     }).AsParallel().GroupBy(x => x.NumeroControlID).Select(x => x.First()).ToList();

                        foreach (PreDespacho item in listado)
                        {
                            if (ctx.Sam3_PreDespacho.Where(x => x.OrdenTrabajoSpoolID.ToString() == item.NumeroControlID && x.ItemCodeID.ToString() == item.ItemCodeID).Any())
                            {
                                item.NumeroUnicoID = (from pd in ctx.Sam3_PreDespacho
                                                      where pd.OrdenTrabajoSpoolID.ToString() == item.NumeroControlID
                                                      && pd.ItemCodeID.ToString() == item.ItemCodeID && pd.Activo
                                                      select pd.NumeroUnicoID.ToString()).AsParallel().SingleOrDefault();
                            }
                        }

                        return listado.OrderBy(x => x.NumeroControlID).ToList();
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
        /// Funcion para el boton Predespachar 
        /// Pantalla pre despacho
        /// </summary>
        /// <param name="lista"></param>
        /// <param name="usuario"></param>
        /// <returns></returns>
        public object Predespachar(List<PreDespachoItems> lista, Sam3_Usuario usuario)
        {
            try
            {
                using (SamContext ctx = new SamContext())
                {
                    using (Sam2Context ctx2 = new Sam2Context())
                    {
                        foreach (PreDespachoItems item in lista)
                        {
                            bool tieneHold = (from ots in ctx2.OrdenTrabajoSpool
                                              where ots.OrdenTrabajoSpoolID.ToString() == item.NumeroControl
                                              && !(from sh in ctx2.SpoolHold
                                                   where sh.SpoolID == ots.SpoolID
                                                   && (sh.Confinado || sh.TieneHoldCalidad || sh.TieneHoldIngenieria)
                                                   select sh).Any()
                                              select ots.OrdenTrabajoSpoolID).AsParallel().Any();

                            if (!tieneHold)
                            {
                                int numeroUnicoSam3 = (from eq in ctx.Sam3_EquivalenciaNumeroUnico
                                                       where eq.Activo && eq.Sam2_NumeroUnicoID.ToString() == item.NumeroUnico
                                                       select eq.Sam3_NumeroUnicoID).AsParallel().SingleOrDefault();

                                if (ctx.Sam3_PreDespacho.Where(x => x.OrdenTrabajoSpoolID.ToString() == item.NumeroControl && x.ItemCodeID.ToString() == item.ItemCode).Any())
                                {
                                    Sam3_PreDespacho preDespacho = ctx.Sam3_PreDespacho.Where(x => x.OrdenTrabajoSpoolID.ToString() == item.NumeroControl && x.ItemCodeID.ToString() == item.ItemCode).AsParallel().SingleOrDefault();
                                    preDespacho.Activo = true;
                                    preDespacho.FechaModificacion = DateTime.Now;
                                    preDespacho.FechaPreDespacho = DateTime.Now;
                                    preDespacho.UsuarioModificacion = usuario.UsuarioID;
                                    preDespacho.ProyectoID = Convert.ToInt32(item.ProyectoID);
                                    preDespacho.NumeroUnicoID = numeroUnicoSam3;

                                    ctx.SaveChanges();
                                }
                                else
                                {
                                    Sam3_PreDespacho preDespacho = new Sam3_PreDespacho();
                                    preDespacho.Activo = true;
                                    preDespacho.FechaModificacion = DateTime.Now;
                                    preDespacho.FechaPreDespacho = DateTime.Now;
                                    preDespacho.UsuarioModificacion = usuario.UsuarioID;
                                    preDespacho.ProyectoID = Convert.ToInt32(item.ProyectoID);
                                    preDespacho.OrdenTrabajoSpoolID = Convert.ToInt32(item.NumeroControl);
                                    preDespacho.NumeroUnicoID = numeroUnicoSam3;
                                    preDespacho.MaterialSpoolID = (from ots in ctx2.OrdenTrabajoSpool
                                                                   join ms in ctx2.MaterialSpool on ots.SpoolID equals ms.SpoolID
                                                                   where ots.OrdenTrabajoSpoolID.ToString() == item.NumeroControl
                                                                   select ms.MaterialSpoolID).AsParallel().SingleOrDefault();

                                    ctx.Sam3_PreDespacho.Add(preDespacho);
                                    ctx.SaveChanges();
                                }
                            }
                            else
                            {
                                throw new Exception("El Spool cuenta con Hold, no se puede Pre-Despachar");
                            }
                        }

                        TransactionalInformation result = new TransactionalInformation();
                        //result.ReturnMessage.Add(preDespacho.PreDespachoID.ToString());
                        result.ReturnCode = 200;
                        result.ReturnStatus = true;
                        result.IsAuthenicated = true;

                        return result;
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

        public object ListadoNumerosUnicos(string numeroControl, string itemcode, int proyectoID, Sam3_Usuario usuario)
        {
            try
            {
                List<ListaCombos> listado = new List<ListaCombos>();
                using (SamContext ctx = new SamContext())
                {
                    using (Sam2Context ctx2 = new Sam2Context())
                    {
                        int proyectoSam2 = (from eq in ctx.Sam3_EquivalenciaProyecto
                                            where eq.Sam3_ProyectoID == proyectoID
                                            select eq.Sam2_ProyectoID).AsParallel().SingleOrDefault();

                        List<int> sam2_NumerosUnicosIDs = (from odts in ctx2.OrdenTrabajoSpool
                                                           join odt in ctx2.OrdenTrabajo on odts.OrdenTrabajoID equals odt.OrdenTrabajoID
                                                           join ms in ctx2.MaterialSpool on odts.SpoolID equals ms.SpoolID
                                                           join it in ctx2.ItemCode on ms.ItemCodeID equals it.ItemCodeID
                                                           join nu in ctx2.NumeroUnico on it.ItemCodeID equals nu.ItemCodeID
                                                           join nui in ctx2.NumeroUnicoInventario on nu.NumeroUnicoID equals nui.NumeroUnicoID
                                                           where odts.OrdenTrabajoSpoolID.ToString() == numeroControl
                                                           && it.TipoMaterialID == 2
                                                           && it.ItemCodeID.ToString() == itemcode
                                                           && odt.ProyectoID == proyectoSam2
                                                           && ms.Diametro1 == nu.Diametro1
                                                           && ms.Diametro2 == nu.Diametro2
                                                           select nu.NumeroUnicoID).Distinct().AsParallel().ToList();

                        listado = (from nu in ctx.Sam3_NumeroUnico
                                   join nueq in ctx.Sam3_EquivalenciaNumeroUnico on nu.NumeroUnicoID equals nueq.Sam3_NumeroUnicoID
                                   where nu.Activo && nueq.Activo
                                   && sam2_NumerosUnicosIDs.Contains(nueq.Sam2_NumeroUnicoID)
                                   select new ListaCombos
                                   {
                                       id = nu.NumeroUnicoID.ToString(),
                                       value = nu.Prefijo + "-" + nu.Consecutivo
                                   }).Distinct().AsParallel().ToList();

                        foreach (ListaCombos lst in listado)
                        {
                            string[] elementos = lst.value.Split('-');

                            int temp = Convert.ToInt32(lst.id);

                            int digitos = (from nu in ctx.Sam3_NumeroUnico
                                           join p in ctx.Sam3_ProyectoConfiguracion on nu.ProyectoID equals p.ProyectoID
                                           where nu.Activo && p.Activo
                                           && nu.NumeroUnicoID == temp
                                           select p.DigitosNumeroUnico).AsParallel().SingleOrDefault();

                            string formato = "D" + digitos.ToString();
                            int consecutivo = Convert.ToInt32(elementos[1]);

                            lst.value = elementos[0] + "-" + consecutivo.ToString(formato);

                        }
                    }
                }
                return listado;
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