﻿
Cookies.set("home", true, { path: '/' });
Cookies.set("navegacion", "42", { path: '/' });

var $CargaCarroModel = {
    listContainer: {
        create: "",
        list: "",
        detail: "",
        destroy: ""
    },
    properties: {

        InputCarro: {
            visible: "InputCarroDiv",
            editable: "inputCarro",
        },
        InputID: {
            visible: "InputIDDiv",
            editable: "InputID",
            required: "InputID",
        }
    }
};
