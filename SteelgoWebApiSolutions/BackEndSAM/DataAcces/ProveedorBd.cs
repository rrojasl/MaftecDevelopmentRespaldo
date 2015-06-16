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

namespace BackEndSAM.DataAcces
{
    public class ProveedorBd
    {
        private static readonly object _mutex = new object();
        private static ProveedorBd _instance;

        /// <summary>
        /// constructor privado para implementar el patron Singleton
        /// </summary>
        private ProveedorBd()
        {
        }

        /// <summary>
        /// crea una instancia de la clase
        /// </summary>
        public static ProveedorBd Instance
        {
            get
            {
                lock (_mutex)
                {
                    if (_instance == null)
                    {
                        _instance = new ProveedorBd();
                    }
                }
                return _instance;
            }
        }

        public object ObtenerListadoProveedores()
        {
            try
            {
                using (SamContext ctx = new SamContext())
                {
                    List<Proveedor> lstProveedores = (from p in ctx.Sam3_Proveedor
                                                      where p.Activo
                                                      select new Proveedor
                                                      {
                                                          Nombre = p.Nombre,
                                                          ProveedorID = p.ProveedorID.ToString()
                                                      }).AsParallel().ToList();
                    return lstProveedores;
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

        public object InsertarProveedor(Sam3_Proveedor cambios, Sam3_Usuario usuario)
        {
            try
            {
                using (SamContext ctx = new SamContext())
                {
                    cambios.UsuarioModificacion = usuario.UsuarioID;
                    cambios.FechaModificacion = DateTime.Now;
                    cambios.Activo = true;
                    ctx.Sam3_Proveedor.Add(cambios);
                    ctx.SaveChanges();

                    return new Proveedor { Nombre = cambios.Nombre, ProveedorID = cambios.ProveedorID.ToString() };
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

        public object ActualizarProveedor(Sam3_Proveedor cambios, Sam3_Usuario usuario)
        {
            try
            {
                using (SamContext ctx = new SamContext())
                {
                    Sam3_Proveedor registroEnBd = ctx.Sam3_Proveedor.Where(x => x.ProveedorID == cambios.ProveedorID).AsParallel().SingleOrDefault();
                    registroEnBd.Activo = cambios.Activo != null && cambios.Activo != registroEnBd.Activo ?
                        cambios.Activo : registroEnBd.Activo;
                    registroEnBd.ContactoID = cambios.ContactoID != null && cambios.ContactoID != registroEnBd.ContactoID ?
                        cambios.ContactoID : registroEnBd.ContactoID;
                    registroEnBd.Descripcion = cambios.Descripcion != null && cambios.Descripcion != registroEnBd.Descripcion ?
                        cambios.Descripcion : registroEnBd.Descripcion;
                    registroEnBd.Direccion = cambios.Direccion != null && cambios.Direccion != registroEnBd.Direccion ?
                        cambios.Direccion : registroEnBd.Direccion;
                    registroEnBd.Nombre = cambios.Nombre != null && cambios.Nombre != registroEnBd.Nombre ?
                        cambios.Nombre : registroEnBd.Nombre;
                    registroEnBd.ProveedorID = cambios.ProveedorID != null && cambios.ProveedorID != registroEnBd.ProveedorID ?
                        cambios.ProveedorID : registroEnBd.ProveedorID;
                    registroEnBd.Telefono = cambios.Telefono != null && cambios.Telefono != registroEnBd.Telefono ?
                        cambios.Telefono : registroEnBd.Telefono;
                    registroEnBd.UsuarioModificacion = usuario.UsuarioID;
                    registroEnBd.FechaModificacion = DateTime.Now;

                    ctx.SaveChanges();

                    TransactionalInformation result = new TransactionalInformation();
                    result.ReturnMessage.Add("OK");
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

        public object EliminarProveedor(int proveedorID, Sam3_Usuario usuario)
        {
            try
            {
                using (SamContext ctx = new SamContext())
                {
                    Sam3_Proveedor proveedor = ctx.Sam3_Proveedor.Where(x => x.ProveedorID == proveedorID)
                        .AsParallel().SingleOrDefault();
                    proveedor.Activo = false;
                    proveedor.UsuarioModificacion = usuario.UsuarioID;
                    proveedor.FechaModificacion = DateTime.Now;

                    ctx.SaveChanges();

                    TransactionalInformation result = new TransactionalInformation();
                    result.ReturnMessage.Add("OK");
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

    }
}