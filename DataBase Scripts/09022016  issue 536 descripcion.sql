/*
38 Incidencias --> PaginaID

50	RespuestaIncidencia
51	ResolucionIncidencia --> EntidadID
52	CancelarIncidencia

191	51	descripcionarchivo
197	50	descripcionarchivo --> PropiedadID
201	52	descripcionarchivo
*/

Update [Sam3_Rel_Perfil_Propiedad_Pagina]
SET Requerido = 1
WHERE  PaginaID=38 and PerfilID=1 and PropiedadID= /*191*/

Update [Sam3_Rel_Perfil_Propiedad_Pagina]
SET Requerido = 1
WHERE  PaginaID=38 and PerfilID=1 and PropiedadID=/*197*/

Update [Sam3_Rel_Perfil_Propiedad_Pagina]
SET Requerido = 1
WHERE  PaginaID=38 and PerfilID=1 and PropiedadID=/*201*/

Update [Sam3_Rel_Perfil_Propiedad_Pagina]
SET Requerido = 1
WHERE  PaginaID=38 and PerfilID=1 and PropiedadID=/*157*/

--Llegada de material

SELECT * FROM Sam3_Pagina --7
SELECT * FROM Sam3_Entidad --14
SELECT * FROM Sam3_Propiedad --66
WHERE EntidadID = 14


Update [Sam3_Rel_Perfil_Propiedad_Pagina]
SET Requerido = 1
WHERE  PaginaID=7 and PerfilID=1 and PropiedadID=/*66*/


