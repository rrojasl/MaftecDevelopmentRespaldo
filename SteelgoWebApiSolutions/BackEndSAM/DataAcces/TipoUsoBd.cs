﻿using BackEndSAM.Models;
using DatabaseManager.Sam3;
using SecurityManager.Api.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace BackEndSAM.DataAcces
{
    public class TipoUsoBd
    {

         private static readonly object _mutex = new object();
         private static TipoUsoBd _instance;

        /// <summary>
        /// constructor privado para implementar el patron Singleton
        /// </summary>
        private TipoUsoBd()
        {
        }

        /// <summary>
        /// crea una instancia de la clase
        /// </summary>
        public static TipoUsoBd Instance
        {
            get
            {
                lock (_mutex)
                {
                    if (_instance == null)
                    {
                        _instance = new TipoUsoBd();
                    }
                }
                return _instance;
            }
        }

        /// <summary>
        /// Obtener los tipos de uso para Cuantificacion
        /// </summary>
        /// <returns>lista de tipos de uso</returns>
        public object ObtenerTipoUso()
        {
            try
            {
                List<TipoUso> listTU = new List<TipoUso>();

                using (SamContext ctx = new SamContext())
                {
                    listTU.Add(new TipoUso { Nombre = "Agregar Nuevo", id = "0" });

                    List<TipoUso> tipoUso = (from t in ctx.Sam3_TipoUso
                              where t.Activo
                              select new TipoUso
                                {
                                    id = t.TipoUsoID.ToString(),
                                    Nombre = t.Nombre
                                }).AsParallel().ToList();

                    listTU.AddRange(tipoUso);
                }
                return listTU;
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