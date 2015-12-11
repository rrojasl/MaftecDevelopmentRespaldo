﻿var folioGenerado;
function AjaxCargarDatos() {
    $ImpresionPruebas.ImpresionPruebas.read({ token: Cookies.get("token"), lenguaje: $("#language").val() }).done(function (data) {
        $("#grid").data('kendoGrid').dataSource.data([]);
        var ds = $("#grid").data("kendoGrid").dataSource;
        var array = data;

        for (var i = 0; i < array.length; i++) {
            ds.add(array[i]);
        }
    });
};

function AjaxGenerarReporte(arregloJuntas)
{
    loadingStart();
    Captura = [];
    Captura[0] = { Detalles: "" };
    ListaDetalles = [];
    contador = 0;
    ReporteIDConsecutivo;

    if (JuntasSeleccionadasConIDUnico(arregloJuntas) && AsignarIDUnicoXJuntaSeleccionada(arregloJuntas)) {

        for (var i = 0; i < arregloJuntas.length; i++) {
            if (arregloJuntas[i].Seleccionado && (arregloJuntas[i].Status == 'N' || arregloJuntas[i].Status == 'NR' || arregloJuntas[i].Status != 'A')) {
                ListaDetalles[contador] = { RequisicionPruebaElementoID: "", ReporteID: "" };
                ListaDetalles[contador].RequisicionPruebaElementoID = arregloJuntas[i].RequisicionPruebaElementoID;
                ListaDetalles[contador].ReporteID = arregloJuntas[i].ReporteID;
                ListaDetalles[contador].Status = arregloJuntas[i].Status;
                contador++;
            }
        }



        Captura[0].Detalles = ListaDetalles;
        if (Captura[0].Detalles.length > 0) {
            $ImpresionPruebas.ImpresionPruebas.create(Captura[0], { token: Cookies.get("token"), lenguaje: $("#language").val() }).done(function (data) {
                if (data.ReturnMessage.length > 0 && data.ReturnMessage[0] == "Ok") {
                    AjaxCargarDatos();
                    mensaje = "Se guardo correctamente la informacion" + "-0";
                    displayMessage("CapturaMensajeGuardadoExitoso", "", '1');
                }
                else if (data.ReturnMessage.length > 0 && data.ReturnMessage[0] != "Ok") {
                    mensaje = "No se guardo la informacion el error es: " + data.RetsurnMessage[0] + "-2"
                    displayMessage("CapturaMensajeGuardadoErroneo", "", '1');
                }
                loadingStop();
            });
        }
        else {
            loadingStop();
        }
    }
    else {
        displayMessage("CapturaMensajeJuntasErroneas", "", '1');
        loadingStop();
    }
    
}