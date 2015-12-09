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
    public class ReporteController : ApiController
    {
        public object Get(ConsultaReportes consulta, string token)
        {
            string payload = "";
            string newToken = "";
            bool tokenValido = ManageTokens.Instance.ValidateToken(token, out payload, out newToken);
            if (tokenValido)
            {
                JavaScriptSerializer serializer = new JavaScriptSerializer();
                Sam3_Usuario usuario = serializer.Deserialize<Sam3_Usuario>(payload);
                switch (consulta.TipoReporte)
                {
                    case 1: //Formato Etiquetas Orden de recepcion
                        return ReportesBd.Instance.ReporteFormatoEtiquetasOrdenRecepcion(consulta.FolioOrdenRecepcion, usuario);
                    case 2: //Formato Incidencia
                        return null;
                        break;
                    case 3: //Pase Salida transportista
                        return null;
                        break;
                    default:
                        TransactionalInformation result = new TransactionalInformation();
                        result.ReturnMessage.Add("Tipo de reporte no encontrado");
                        result.ReturnCode = 500;
                        result.ReturnStatus = false;
                        result.IsAuthenicated = true;
                        return result;
                }
                
            }
            else
            {
                TransactionalInformation result = new TransactionalInformation();
                result.ReturnMessage.Add(payload);
                result.ReturnCode = 401;
                result.ReturnStatus = false;
                result.IsAuthenicated = false;
                return result;
            }
        }

    }
}