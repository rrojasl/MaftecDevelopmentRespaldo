﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web;
using System.Web.Http;
using System.Web.Http.Cors;
using System.Web.Script.Serialization;
using CommonTools.Libraries.Strings.Security;
using DatabaseManager.EntidadesPersonalizadas;
using DatabaseManager.Sam3;
using SecurityManager.TokenHandler;
using SecurityManager.Api.Models;
using BackEndSAM.DataAcces;
using BackEndSAM.Models;
using System.Diagnostics;
using System.IO;

namespace BackEndSAM.Controllers
{
    [EnableCors(origins: "*", headers: "*", methods: "*")]
    public class DocumentoPaseSalidaController : ApiController
    {
        public object Get(int folio, string token)
        {
            string payload = "";
            string newToken = "";
            bool tokenValido = ManageTokens.Instance.ValidateToken(token, out payload, out newToken);
            if (tokenValido)
            {
                JavaScriptSerializer serializer = new JavaScriptSerializer();
                Sam3_Usuario usuario = serializer.Deserialize<Sam3_Usuario>(payload);
                return DocumentosBd.Instance.ObtenerDocumentosPaseSalida(folio, usuario);
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