﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace BackEndSAM.Models
{
    public class ListaDocumentos
    {
        public string DocumentoID { get; set; }
        public string Nombre { get; set; }
        public string Extencion { get; set; }
        public string Url { get; set; }
    }
}