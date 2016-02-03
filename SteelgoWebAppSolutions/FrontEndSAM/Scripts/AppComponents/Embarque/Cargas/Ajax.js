﻿function AjaxEmbarqueCargaProveedores() {
    loadingStart();

    $Embarque.Embarque.read({ token: Cookies.get("token"), embarquePlanaID: EmbarquePlanaID }).done(function (data) {
        if (data.length > 0) {
            $("#inputProveedor").data("kendoDropDownList").value("");
            $("#inputProveedor").data("kendoDropDownList").dataSource.data(data);
            $("#inputProveedor").data("kendoDropDownList").trigger("change");
            AjaxCargarPaquetes();
        } else {
            $("#inputProveedor").data("kendoDropDownList").value("");
        };
        loadingStop();
    });
}

function AjaxCargarPlanasPlacas() {
    loadingStart();
    EmbarquePlanaID = 0;
    $CargaEmbarque.CargaEmbarque.read({ token: Cookies.get("token"), transportistaID: $("#inputProveedor").val(), embarquePlanaID: EmbarquePlanaID, lenguaje: $("#language").val() }).done(function (data) {
        if (data.length > 0) {
            $("#inputEmbarqueCargaPLacaPlana").data("kendoDropDownList").value("");
            $("#inputEmbarqueCargaPLacaPlana").data("kendoDropDownList").dataSource.data(data);
            $("#lblEstatus").text(data[0].estatus)
        } else {
            $("#inputEmbarqueCargaPLacaPlana").data("kendoDropDownList").text("");
            $("#inputEmbarqueCargaPLacaPlana").data("kendoDropDownList").value("");
            $("#inputEmbarqueCargaPLacaPlana").data("kendoDropDownList").dataSource.data([]);;
        };
        loadingStop();
    });
}

function AjaxCargarPaquetes() {
    loadingStart();

    $CargaEmbarque.CargaEmbarque.read({ token: Cookies.get("token"), tipo: '1' }).done(function (data) {
        if (data.length > 0) {
            $("#inputPaquete").data("kendoDropDownList").value("");
            $("#inputPaquete").data("kendoDropDownList").dataSource.data(data);

            $("#inputPopupPaquete").data("kendoDropDownList").value("");
            $("#inputPopupPaquete").data("kendoDropDownList").dataSource.data(data);

        } else {
            $("#inputEmbarqueCargaPLacaPlana").data("kendoDropDownList").value("");
        };
        loadingStop();
    });
}

function AjaxCargarCuadrante(area) {
    loadingStart();
    $Cuadrante.Cuadrante.read({ token: Cookies.get("token"), AreaID: area }).done(function (data) {
        $("#inputPopupCuadrante").data("kendoDropDownList").value("");
        $("#inputPopupCuadrante").data("kendoDropDownList").dataSource.data(data);
        loadingStop();
    });
}

function AjaxObtenerSpoolID() {
    $CapturaArmado.Armado.read({ ordenTrabajo: $("#InputOrdenTrabajo").val(), tipo: '1', token: Cookies.get("token"), lenguaje: $("#language").val() }).done(function (data) {
        $("#InputOrdenTrabajo").val(data.OrdenTrabajo);
        $("#InputID").data("kendoComboBox").dataSource.data(data.idStatus);
        Cookies.set("LetraProyecto", data.OrdenTrabajo.substring(0, 1), { path: '/' });
    });
}

function AjaxAgregarCarga() {
    loadingStart();

    Captura = [];
    Captura[0] = { Detalles: "" };
    ListaDetalles = [];

    var index = 0;

    ListaDetalles[index] = { TipoConsulta: "", OrdenTrabajoSpoolID: "", Paquete: "", Codigo: "" };
    ListaDetalles[index].TipoConsulta = ObtenerTipoConsulta();
    switch (ListaDetalles[index].TipoConsulta) {
        case 1: //spool
            ListaDetalles[index].OrdenTrabajoSpoolID = $("#InputID").val();
            ListaDetalles[index].Paquete = 0;
            ListaDetalles[index].Codigo = 0;
            break;//paquete
        case 2:
            ListaDetalles[index].OrdenTrabajoSpoolID = 0;
            ListaDetalles[index].Paquete = $("#inputPaquete").val();
            ListaDetalles[index].Codigo = 0;
            break;
        case 3://codigo
            ListaDetalles[index].OrdenTrabajoSpoolID = 0;
            ListaDetalles[index].Paquete = 0;
            ListaDetalles[index].Codigo = $("#inputCodigo").val();
            break;
        case -1:
            ListaDetalles[index].OrdenTrabajoSpoolID = 0;
            ListaDetalles[index].Paquete = 0;
            ListaDetalles[index].Codigo = 0;
            break;

    }

    $CargaEmbarque.CargaEmbarque.read({ token: Cookies.get("token"), TipoConsulta: ListaDetalles[index].TipoConsulta, OrdenTrabajoSpoolID: ListaDetalles[index].OrdenTrabajoSpoolID, Paquete: ListaDetalles[index].Paquete, Codigo: ListaDetalles[index].Codigo, lenguaje: $("#language").val(), embarquePlanaID: EmbarquePlanaID }).done(function (data) {
        if (data[0].Mensaje == null) {
            var ds = $("#grid").data("kendoGrid").dataSource;
            var CantidadRegistrosOriginales = ds._data.length;
            var array = data;
            var totalToneladasCargadas = 0;
            var totalToneladasCargadasGeneral = 0;


            for (var i = 0; i < array.length; i++) {
                if (!validarInformacion(array[i])) {
                    totalToneladasCargadas += array[i]["Peso"];
                    array[i]["Consecutivo"] += CantidadRegistrosOriginales;
                    ds.add(array[i]);
                }
                else
                    displayMessage("EmbarqueCargaInformacionExistente", "", '2');
            }

            var piezas = String(ds._data.length);
            $("#lblEmbarqueCargaTotalPiezas").text(piezas);

            var textoToneladasCargadas = $("#lblEmbarqueCargaToneladasCargadas").text();
            totalToneladasCargadasGeneral = $("#lblEmbarqueCargaToneladasCargadas").text() == "" ? 0 : parseInt($("#lblEmbarqueCargaToneladasCargadas").text());
            textoToneladasCargadas = String(parseFloat((totalToneladasCargadasGeneral + totalToneladasCargadas) * 0.001).toFixed(4));

            $("#lblEmbarqueCargaToneladasCargadas").text(textoToneladasCargadas);
        }
        else {
            displayMessage("", data[0].Mensaje, '2');
        }
        loadingStop();
    });
}

function AjaxCargarCamposPredeterminados() {
    loadingStart();
    $ListadoCamposPredeterminados.ListadoCamposPredeterminados.read({ token: Cookies.get("token"), lenguaje: $("#language").val(), id: 32 }).done(function (data) {
        if (data.toLowerCase() == "spool") {
            $('input:radio[name=EmbarqueCargaTipoSeleccion]:nth(0)').attr('checked', true).trigger("change");
        } else if (data.toLowerCase() == "paquete") {
            $('input:radio[name=EmbarqueCargaTipoSeleccion]:nth(1)').attr('checked', true).trigger("change");

        } else if (data.toLowerCase() == "codigo") {
            $('input:radio[name=EmbarqueCargaTipoSeleccion]:nth(2)').attr('checked', true).trigger("change");
        }

        loadingStop();
    });

}

function AjaxCrearPaquete(arregloCaptura, accion, paqueteID) {
    try {
        loadingStart();
        Captura = [];
        Captura[0] = { Detalles: "" };
        ListaDetalles = [];
        var indice = 0;

        if (arregloCaptura.Accion == undefined) {
            for (index = 0; index < arregloCaptura.length; index++) {
                if (arregloCaptura[index].Seleccionado) {
                    ListaDetalles[indice] = { Accion: "", SpoolID: "", CuadranteID: "" };
                    ListaDetalles[indice].Accion = accion == undefined ? arregloCaptura[index].Accion : accion;
                    ListaDetalles[indice].SpoolID = arregloCaptura[index].SpoolID;
                    ListaDetalles[indice].CuadranteID = arregloCaptura[index].CuadranteID;
                    indice++;
                }
            }
        }
        else {
            ListaDetalles[indice] = { Accion: "", SpoolID: "", CuadranteID: "" };
            ListaDetalles[indice].Accion = accion == undefined ? arregloCaptura.Accion : accion;
            ListaDetalles[indice].SpoolID = arregloCaptura.SpoolID;
            ListaDetalles[indice].CuadranteID = arregloCaptura.CuadranteID;
        }

        Captura[0].Detalles = ListaDetalles;
        $CargaEmbarque.CargaEmbarque.create(Captura[0], { token: Cookies.get("token"), EmpaquetadoPaqueteID: paqueteID }).done(function (data) {
            if (data.Folio != "error") {

                for (index = 0; index < arregloCaptura.length; index++) {
                    if (arregloCaptura[index].Seleccionado) {
                        arregloCaptura[index].Paquete = data.Folio;
                        arregloCaptura[index].EmbarquePlanaID = data.EmbarquePlanaID;

                    }
                }
                AjaxCargarPaquetes();
                displayMessage("EmbarqueCargaCuadranteActualizado", "", '1');
            }
            else if (data.ReturnMessage.length > 0 && data.ReturnMessage[0] != "Ok") {
                displayMessage("EmbarqueCargaErrorCuadranteActualizado", "", '3');
            }
            $("#grid").data("kendoGrid").dataSource.sync();
            loadingStop();
        });


    } catch (e) {
        loadingStop();
        displayMessage("Mensajes_error", e.message, '0');

    }

}

function ajaxGuardar(arregloCaptura) {
    try {
        loadingStart();
        Captura = [];
        Captura[0] = { Detalles: "" };
        ListaDetalles = [];


        if (arregloCaptura.Accion == undefined) {
            for (index = 0; index < arregloCaptura.length; index++) {
                ListaDetalles[index] = { Accion: "", SpoolID: "", CuadranteID: "" };
                if (arregloCaptura[index].Accion != 3) {
                    ListaDetalles[index].Accion = arregloCaptura[index].EmbarquePlanaSpoolID == 0 ? 1 : 2;
                }
                else {
                    ListaDetalles[index].Accion = 3;
                }
                
                ListaDetalles[index].SpoolID = arregloCaptura[index].SpoolID;
                ListaDetalles[index].CuadranteID = arregloCaptura[index].CuadranteID;
            }
        }

        
        EmbarquePlanaID = $('#inputEmbarqueCargaPLacaPlana').data("kendoDropDownList").dataSource._data[0].EmbarquePlanaID;

        Captura[0].Detalles = ListaDetalles;
        $CargaEmbarque.CargaEmbarque.create(Captura[0], { token: Cookies.get("token"), proveedorID: $("#inputProveedor").val(), vehiculoID: $("#inputEmbarqueCargaPLacaPlana").val(), embarquePlanaID: EmbarquePlanaID }).done(function (data) {
            if (data.ReturnMessage.length > 0 && data.ReturnMessage[0] == "Ok") {
                displayMessage("EmbarqueCargaGuardar", "", '1');
                ajaxCargarSpoolXPlaca();
            }
            else if (data.ReturnMessage.length > 0 && data.ReturnMessage[0] != "Ok") {
                displayMessage("EmbarqueCargaErrorGuardar", "", '2');
            }
            $("#grid").data("kendoGrid").dataSource.sync();
            loadingStop();
        });
    } catch (e) {
        loadingStop();
        displayMessage("Mensajes_error", e.message, '0');

    }
};

function ajaxCerrarPlana() {
    try {
        loadingStart();

        CierraPlana = {
            embarquePlanaID: ""
        };
        EmbarquePlanaID = $('#inputEmbarqueCargaPLacaPlana').data("kendoDropDownList").dataSource._data[0].EmbarquePlanaID;
        CierraPlana.embarquePlanaID = EmbarquePlanaID;

        if (EmbarquePlanaID != 0) {
            $CargaEmbarque.CargaEmbarque.update(CierraPlana, { token: Cookies.get("token") }).done(function (data) {
                if (data.ReturnMessage.length > 0 && data.ReturnMessage[0] == "Ok") {
                    displayMessage("EmbarqueCargaCerrarPlana", "", '1');
                    $('#btnEmbarqueCerrarPlana').attr("disabled", true);
                }
                else if (data.ReturnMessage.length > 0 && data.ReturnMessage[0] != "Ok") {
                    displayMessage("EmbarqueCargaErrorCerrarPlana", "", '2');
                }
                $("#grid").data("kendoGrid").dataSource.sync();
                loadingStop();
            });
        }
        else {
            displayMessage("", "La plana debe estar abierta para poder cerrarla", "1");
            loadingStop();
        }
        
    } catch (e) {
        loadingStop();
        displayMessage("Mensajes_error", e.message, '0');

    }
}



function ajaxCargarSpoolXPlaca() {
    loadingStart();
    $CargaEmbarque.CargaEmbarque.read({ token: Cookies.get("token"), proveedorID: $("#inputEmbarqueCargaPLacaPlana").data("kendoDropDownList").value(), placaID: $("#inputProveedor").data("kendoDropDownList").value(), lenguaje: $("#language").val() }).done(function (data) {
        $("#grid").data('kendoGrid').dataSource.data([]);
        var ds = $("#grid").data("kendoGrid").dataSource;
        var CantidadRegistrosOriginales = ds._data.length;
        var array = data;
        var totalToneladasCargadas = 0;
        var totalToneladasCargadasGeneral = 0;


        for (var i = 0; i < array.length; i++) {
            if (!validarInformacion(array[i])) {
                totalToneladasCargadas += array[i]["Peso"];
                array[i]["Consecutivo"] += CantidadRegistrosOriginales;
                ds.add(array[i]);
            }
            else
                displayMessage("EmbarqueCargaInformacionExistente", "", '2');
        }

        var piezas = String(ds._data.length);
        $("#lblEmbarqueCargaTotalPiezas").text(piezas);

        var textoToneladasCargadas = $("#lblEmbarqueCargaToneladasCargadas").text();
        totalToneladasCargadasGeneral = 0;
        textoToneladasCargadas = String(parseFloat((totalToneladasCargadasGeneral + totalToneladasCargadas) * 0.001).toFixed(4));

        $("#lblEmbarqueCargaToneladasCargadas").text(textoToneladasCargadas);

        loadingStop();
    });
}