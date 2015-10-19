﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Web;
using System.Web.Http;
using System.Web.Http.Cors;
using System.Web.Script.Serialization;
using BackEndSAM.DataAcces;
using BackEndSAM.Models;
using CommonTools.Libraries.Strings.Security;
using DatabaseManager.Sam3;
using SecurityManager.Api.Models;
using SecurityManager.TokenHandler;

namespace BackEndSAM.Controllers
{
     [EnableCors(origins: "*", headers: "*", methods: "*")]
    public class DummyNumerosUnicosController : ApiController
    {
        // GET api/dummynumerosunicos
        public object Get(string ProyectoID,string texto, string token)
        {
             //Li
            string payload = "";
            string newToken = "";
            bool tokenValido = ManageTokens.Instance.ValidateToken(token, out payload, out newToken);
            JavaScriptSerializer serializer = new JavaScriptSerializer();
            Sam3_Usuario usuario = serializer.Deserialize<Sam3_Usuario>(payload);
            int Proyecto = ProyectoID != "" ? Convert.ToInt32(ProyectoID) : 0;
            return NumeroUnicoBd.Instance.ListadoNumerosUnicosCorte(Proyecto, texto, usuario);
        }


        public object Get(string NumeroUnicoID, string token)
        {
            string payload = "";
            string newToken = "";
            bool tokenValido = ManageTokens.Instance.ValidateToken(token, out payload, out newToken);
            JavaScriptSerializer serializer = new JavaScriptSerializer();
            Sam3_Usuario usuario = serializer.Deserialize<Sam3_Usuario>(payload);

            return NumeroUnicoBd.Instance.DetalleNumeroUnicoCorte(Convert.ToInt32(NumeroUnicoID), usuario);

        }

        public object Put(ParametrosBusquedaODT DatosODT, string token)
        {
            string payload = "";
            string newToken = "";
            bool tokenValido = ManageTokens.Instance.ValidateToken(token, out payload, out newToken);
            JavaScriptSerializer serializer = new JavaScriptSerializer();
            Sam3_Usuario usuario = serializer.Deserialize<Sam3_Usuario>(payload);
            return CorteBd.Instance.ListadoGenerarCorte(DatosODT, usuario);
        }

        public void Post(DummyDatosODTCorteGuardar Cortes, string token)
        {

        }
    }
}
