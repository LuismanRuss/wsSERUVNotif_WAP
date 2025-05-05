using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Collections.Generic;
using System.Configuration;
using System.Text.RegularExpressions;
using System.Data;
using System.IO;
using System.Xml;

namespace wsSERUVNotif_WAP
{
    public class clsNotificacion
    {
        #region Propiedades privadas
        private string _strOpcion; // Varible que definirá que opcion de mensaje se manejará.
        private string _strAccount; // Variable que obtiene la cuenta de la cual se enviaran las notificaciones
        private DataSet _ds;
        private List<System.Net.Mail.MailMessage> _correos;

        private int _idUsuDest;             //Id del usuario Destinatario
        private int _idProceso;             //ID del proceso al que hace referencia la notificación
        private string _strAsunto;          //Asunto de la notificación
        private string _strMensaje;         //Cuerpo de la notificación
        private int _idUsuRem;              //Id del usuario que envía el mensaje 
        private string _correoTo;           //Indica las direccion de correo a quien se enviara el mensaje
        private string _subject;            //Asunto del mensaje
        private string _message;            // Cuerpo del mensaje
        private string _strAmbiente;        // Ambiente desde el cual se enviarán las notificaciones
        private string _strMailsTo;         //Llave que indica si las notificaciones se enviarán a los destinatarios correctos o a un correo pretederminado

        private clsDALSQL _objDALSQL;
        private libSQL _libSQL;

        System.Collections.ArrayList _lstParametros = new System.Collections.ArrayList(); // lista de parametros
        System.Collections.ArrayList _Mensajes = new System.Collections.ArrayList(); //Lista de mensajes
        private static string ruta = System.Web.Hosting.HostingEnvironment.MapPath("~");//Ruta en la raíz el proyecto
        private static Logger _log = new Logger(ruta, "Logs/");
        #endregion

        #region Contructores
        public clsNotificacion()
        {
            this._correos = new List<System.Net.Mail.MailMessage>();
        }

        #endregion

        #region Procedimientos de la Clase

        #region pNotificacionANEXO
        /// <summary>
        /// Función que gnera los correos que se enviarán a los participantes activos en un proceso, cuando un anexo es modificado o agregado,
        /// siempre y cuando se al anexo se encuentra cargado o integrado en un proceso activo
        /// Autor: Daniel Ramírez Hernández, Edgar Morales González
        /// </summary>
        /// <param "strDatos">Cadena que contiene la información necesaria para generar las notificaciones</param>
        /// <returns>devuelve una lista de objeto correos y una lista de objeto mensaje</returns>
        private void pNotificacionANEXO(string strDatos)
        {
            try
            {
                DataSet ds = ConvertXMLToDataSet(strDatos);
                _ds = ds;

                if (ds != null && ds.Tables != null)
                {
                    DataRow drGeneral = ds.Tables[0].Rows[0];
                    String DescApartado = " ";
                    String DescAnexoNuevo = "";

                    if (ds.Tables[0] != null) // En la tabla 0, regresamos la informacion del Anexo
                    {
                        DescApartado = (drGeneral.Table.Columns.Contains("sDApartado") ? drGeneral["sDApartado"].ToString() : "");
                        DescAnexoNuevo = (drGeneral.Table.Columns.Contains("sDAnexo") ? drGeneral["sDAnexo"].ToString() : "");
                    }

                    if (ds.Tables[1] != null) // Viene la informacion del Usuario Obligado y Enlace Principal
                    {
                        foreach (DataRow row in ds.Tables[1].Rows)
                        {
                            clsNotificacion objMensaje = new clsNotificacion(); // Objeto que almacenara los mensajes
                            System.Net.Mail.MailMessage correo = new System.Net.Mail.MailMessage();
                            correo.From = new System.Net.Mail.MailAddress(ConfigurationManager.AppSettings["AppMail"]); // Correo desde el Cual se enviaran las notificaciones
                            drGeneral = row;

                            if (_strMailsTo != "")
                            {
                                correo.To.Add(_strMailsTo);
                            }
                            else if (_strMailsTo == "")
                            {
                                correo.To.Add(row["SObligadoCorreo"].ToString());
                            }

                            correo.Subject = ConfigurationManager.AppSettings["Subject"] + " - " + "ACTUALIZACIÓN DE ANEXO " + " PROCESO ENTREGA-RECEPCIÓN: " + row["sDProceso"].ToString();
                            correo.Body =

                          "<style>" +
                                "  .namefont { font-size : 14pt; text-align:center;  background-color: #B0C4DE }" +
                                "  .bodyfont { font-size : 12pt;  text-align:justify }" +
                                "  .s4font { font-size : 10pt; text-align:center}" +
                                "  .texto { color:#848484; font-weight: bold }"
                                +
                                "</style>" +
                                    "<div  align =" + "center" + ">" + "<b>" + " Notificación" + "</b>" + "</div>" +
                                    "<div align =" + "center" + ">Sistema de Entrega - Recepción de la Universidad Veracruzana</div>" +
                                    "</br></br>" +
                                   "<div class=" + "namefont" + ">Estimado(a)  " + "<b>" + row["SObligadoNombre"].ToString() + "</b>" + "</div>"
                                    + "</br>"
                                    + "</br>"
                                      + "<div  class=" + "bodyfont" + ">" +
                                    "<font face=" + "arial" + ">"

                                    + "<table>"
                                    + "<tr>"
                                    + "<td class=" + "texto" + ">" + "Se han realizado actualizaciones al Anexo: " + "</td>"
                                    + "<td>" + DescAnexoNuevo + "</td>"
                                    + "</tr>"
                                     + "<tr>"
                                    + "<td class=" + "texto" + ">" + "Correspondiente al Apartado: " + "</td>"
                                    + "<td>" + DescApartado + "</td>"
                                    + "</tr>"
                                    + "<tr>"
                                    + "<td colspan=" + "2" + ">" + "</br> Favor de verificar el avance de su entrega. " + "</td>"
                                    + "</tr>"
                                    + "</table>"
                                     + "</font>"
                                    + "</div>"
                                    + "<div class=" + "s4font" + "> </br></br><b> Favor de no enviar correos a esta cuenta, ya que es utilizada por un proceso automatizado y por lo tanto no se revisa</b></div>"
                                     ;

                            // USO DE HTML
                            correo.BodyEncoding = System.Text.Encoding.UTF8;
                            correo.IsBodyHtml = true;
                            //
                            this._correos.Add(correo);

                            objMensaje.idProceso = Convert.ToInt32(row["idProceso"].ToString());
                            objMensaje.idUsuDest = Convert.ToInt32(row["SObligadoID"].ToString());
                            objMensaje.idUsuRem = idUsuRem;                 //ID DEL USUARIO SERUV
                            objMensaje.strAsunto = correo.Subject;
                            objMensaje.strMensaje = correo.Body;
                            objMensaje.correoTo = correo.To.ToString();
                            objMensaje.subject = correo.Subject.ToString();
                            this._Mensajes.Add(objMensaje);

                            /////////////////////////////////////////////////////////////////////////////////////////////////////
                            int EnlacePrincipal = (drGeneral.Table.Columns.Contains("EnlacePrincipal") ? int.Parse(row["EnlacePrincipal"].ToString()) : 0);

                            if (EnlacePrincipal != 0)
                            {
                                System.Net.Mail.MailMessage correo2 = new System.Net.Mail.MailMessage();
                                correo2.From = new System.Net.Mail.MailAddress(ConfigurationManager.AppSettings["AppMail"]); // Correo desde el Cual se enviaran las notificaciones

                                if (_strMailsTo != "")
                                {
                                    correo2.To.Add(_strMailsTo);
                                }
                                else if (_strMailsTo == "")
                                {
                                    correo2.To.Add(row["CorreoEP"].ToString());
                                }

                                correo2.Subject = ConfigurationManager.AppSettings["Subject"] + " - " + "ACTUALIZACIÓN DE ANEXO" + " PROCESO ENTREGA-RECEPCIÓN: " + row["sDProceso"].ToString();
                                correo2.Body =

                              "<style>" +
                                "  .namefont { font-size : 14pt; text-align:center;  background-color: #B0C4DE }" +
                                "  .bodyfont { font-size : 12pt;  text-align:justify }" +
                                "  .s4font { font-size : 10pt; text-align:center}" +
                                "  .texto { color:#848484; font-weight: bold }"
                                +
                                "</style>" +
                                    "<div  align =" + "center" + ">" + "<b>" + " Notificación" + "</b>" + "</div>" +
                                    "<div align =" + "center" + ">Sistema de Entrega - Recepción de la Universidad Veracruzana</div>" +
                                    "</br></br>" +
                                   "<div class=" + "namefont" + ">Estimado(a)  " + "<b>" + row["NombreEP"].ToString() + "</b>" + "</div>"
                                    + "</br>"
                                    + "</br>"
                                      + "<div  class=" + "bodyfont" + ">" +
                                    "<font face=" + "arial" + ">"

                                    + "<table>"
                                    + "<tr>"
                                    + "<td class=" + "texto" + ">" + "Se han realizado actualizaciones al Anexo: " + "</td>"
                                    + "<td>" + DescAnexoNuevo + "</td>"
                                    + "</tr>"
                                     + "<tr>"
                                    + "<td class=" + "texto" + ">" + "Correspondiente al Apartado: " + "</td>"
                                    + "<td>" + DescApartado + "</td>"
                                    + "</tr>"
                                    + "<tr>"
                                    + "<td colspan=" + "2" + ">" + "</br> Favor de verificar el avance de su entrega. " + "</td>"
                                    + "</tr>"
                                    + "</table>"
                                     + "</font>"
                                    + "</div>"
                                    + "<div class=" + "s4font" + "> </br></br><b> Favor de no enviar correos a esta cuenta, ya que es utilizada por un proceso automatizado y por lo tanto no se revisa</b></div>"
                                     ;

                                // USO DE HTML
                                correo2.BodyEncoding = System.Text.Encoding.UTF8;
                                correo2.IsBodyHtml = true;
                                //
                                this._correos.Add(correo2);

                                objMensaje.idProceso = Convert.ToInt32(row["idProceso"].ToString());
                                objMensaje.idUsuDest = Convert.ToInt32(row["EnlacePrincipal"].ToString());
                                objMensaje.idUsuRem = idUsuRem;  //ID DEL USUARIO SERUV
                                objMensaje.strAsunto = correo2.Subject;
                                objMensaje.strMensaje = correo2.Body;
                                objMensaje.correoTo = correo2.To.ToString();
                                objMensaje.subject = correo2.Subject.ToString();
                                this._Mensajes.Add(objMensaje);
                            }
                        }
                    }
                }
            }
            catch
            {
                _ds.Dispose();
            }
            finally
            {
                _ds.Dispose();
            }
        }
        #endregion

        #region pNotificacionNuevoProceso
        /// <summary>
        /// Función que envia correos electronicos a los participantes (sujetos obligados) cuando se crea un nuevo proceso
        /// Autor: Edgard Morales, Emmanuel Méndez Flores
        /// </summary>
        /// <param "strDatos">Cadena que contiene la información necesaria para generar las notificaciones</param>
        /// <returns>devuelve una lista de objeto correos y una lista de objeto mensaje</returns>
        public void pNotificacionNuevoProceso(string strDatos)
        {
            try
            {
                DataSet ds = ConvertXMLToDataSet(strDatos);
                _ds = ds;

                if (ds != null && ds.Tables != null)
                {
                    DataRow drGeneral = ds.Tables[0].Rows[0];
                    DataTable drParticipantes = ds.Tables[1];

                    string strFechaInicio, strFechaFinal, strFi, strFf, strMotivo = "";
                    strFi = drGeneral.Table.Columns.Contains("dFInicio") ? drGeneral["dFInicio"].ToString() : "";
                    strFechaInicio = DateTime.Parse(strFi).ToString("dd-MM-yyyy");
                    strFf = drGeneral.Table.Columns.Contains("dFFinal") ? drGeneral["dFFinal"].ToString() : "";
                    strFechaFinal = DateTime.Parse(strFf).ToString("dd-MM-yyyy");

                    foreach (DataRow row in ds.Tables[1].Rows)
                    {
                        clsNotificacion objMensaje = new clsNotificacion();         // Objeto que almacenara los mensajes
                        System.Net.Mail.MailMessage correo = new System.Net.Mail.MailMessage();
                        correo.From = new System.Net.Mail.MailAddress(ConfigurationManager.AppSettings["AppMail"]);         // Correo desde el Cual se enviaran las notificaciones
                        if (_strMailsTo != "")
                        {
                            correo.To.Add(_strMailsTo);
                        }
                        else if (_strMailsTo == "")
                        {
                            correo.To.Add(row["sCorreo"].ToString());
                        }

                        if (drGeneral["sDTipoProc"].ToString() != "TRANSITORIA")
                        {
                            strMotivo = "Motivo: ";
                        }
                        else
                        {
                            strMotivo = "";
                        }

                        correo.Subject = ConfigurationManager.AppSettings["Subject"] + " - " + " PROCESO ENTREGA-RECEPCIÓN: " + (drGeneral.Table.Columns.Contains("sDProceso") ? drGeneral["sDProceso"].ToString() : "");
                        // PARA EL USO DE HTML
                        correo.SubjectEncoding = System.Text.Encoding.UTF8;
                        //
                        correo.Body =
                        "<style>" +
                        "  .namefont { font-size : 14pt; text-align:center;  background-color: #B0C4DE }" +
                        "  .bodyfont { font-size : 12pt;  text-align:justify }" +
                        "  .s4font { font-size : 10pt; text-align:center}" +
                        "  .texto { color:#848484; font-weight: bold }"
                        +
                        "</style>" +
                            "<div  align =" + "center" + ">" + "<b>" + " Notificación" + "</b>" + "</div>" +
                            "<div align =" + "center" + ">Sistema de Entrega - Recepción de la Universidad Veracruzana</div>" +
                            "</br></br>" +
                           "<div class=" + "namefont" + ">Estimado(a)  " + "<b>" + row["sNombre"].ToString() + "</b>" + "</div>"
                            + "</br>"
                            + "</br>"

                             + "<div  class=" + "bodyfont" + ">" +
                            "<font face=" + "arial" + ">"
                            + "Usted ha sido asignado(a) para participar en el Proceso Entrega-Recepción: "
                            + "<b>" + (drGeneral.Table.Columns.Contains("sDProceso") ? drGeneral["sDProceso"].ToString() : "") + "</b>"
                            + "</br>"
                            + "</br>"

                            + "<table>"
                            + "<tr>"
                            + "<td class=" + "texto" + " colspan=" + "2" + ">" + "Tipo: " + "</td>"
                            + "<td colspan=" + "4" + ">" + (drGeneral.Table.Columns.Contains("sDTipoProc") ? drGeneral["sDTipoProc"].ToString() : "") + "</td>"
                            + "</tr>"
                            + "<tr>"
                            + "<td class=" + "texto" + " colspan=" + "2" + ">" + strMotivo + "</td>"
                            + "<td colspan=" + "4" + ">" + (drGeneral.Table.Columns.Contains("sDMotiProc") ? drGeneral["sDMotiProc"].ToString() : "") + "</td>"
                            + "</tr>"
                            + "<tr>"
                            + "<td nowrap class=" + "texto" + " colspan=" + "2" + ">" + "Fecha de apertura: " + "</td>" + "<td>" + strFechaInicio + "</td>"
                            + "<td nowrap class=" + "texto" + " align=" + "right" + ">" + "Fecha de cierre: " + "</td>" + "<td>" + strFechaFinal + "</td>"
                            + "<td> " + "                             " + "</td>"
                            + "</tr>"
                            + "</table>"

                            + "</div>"
                            + "</font>"
                            + "<div class=" + "s4font" + "> </br></br><b> Favor de no enviar correos a esta cuenta, ya que es utilizada por un proceso automatizado y por lo tanto no se revisa</b></div>"
                            ;

                        // USO DE HTML
                        correo.BodyEncoding = System.Text.Encoding.UTF8;
                        correo.IsBodyHtml = true;
                        //
                        objMensaje.idProceso = Convert.ToInt32(drGeneral.Table.Columns.Contains("idProceso") ? drGeneral["idProceso"].ToString() : "");
                        objMensaje.idUsuDest = Convert.ToInt32(row["idUsuario"].ToString());
                        objMensaje.idUsuRem = idUsuRem;                 //ID DEL USUARIO SERUV
                        objMensaje.strAsunto = correo.Subject;
                        objMensaje.strMensaje = correo.Body;
                        objMensaje.correoTo = correo.To.ToString();
                        objMensaje.subject = correo.Subject.ToString();
                        this._correos.Add(correo);
                        this._Mensajes.Add(objMensaje);
                    }
                }
            }
            catch
            {
                _ds.Dispose();
            }
            finally
            {
                _ds.Dispose();
            }
        }
        #endregion

        #region pNotificacionModificarFecProceso
        /// <summary>
        /// Función que envia correos electronicos a los participantes cuando se modifica un proceso
        /// Autor: Edgard Morales, Emmanuel Méndez Flores
        /// </summary>
        /// <param "strDatos">Cadena que contiene la información necesaria para generar las notificaciones</param>
        /// <returns>devuelve una lista de objeto correos y una lista de objeto mensaje</returns>
        public void pNotificacionModificarFecProceso(string strDatos)
        {
            try
            {
                DataSet ds = ConvertXMLToDataSet(strDatos);
                _ds = ds;

                if (ds != null && ds.Tables != null)
                {
                    DataRow drGeneral = ds.Tables[0].Rows[0];
                    DataTable drParticipantes = ds.Tables[1];

                    string strFechaInicio, strFechaFinal, strFi, strFf = "";

                    strFi = drGeneral.Table.Columns.Contains("dFInicio") ? drGeneral["dFInicio"].ToString() : "";
                    strFechaInicio = DateTime.Parse(strFi).ToString("dd-MM-yyyy");
                    strFf = drGeneral.Table.Columns.Contains("dFFinal") ? drGeneral["dFFinal"].ToString() : "";
                    strFechaFinal = DateTime.Parse(strFf).ToString("dd-MM-yyyy");

                    foreach (DataRow row in this._ds.Tables[1].Rows)
                    {
                        clsNotificacion objMensaje = new clsNotificacion(); // Objeto que almacenara los mensajes
                        System.Net.Mail.MailMessage correo = new System.Net.Mail.MailMessage();
                        correo.From = new System.Net.Mail.MailAddress(ConfigurationManager.AppSettings["AppMail"]); // Correo desde el Cual se enviaran las notificaciones

                        if (_strMailsTo != "")
                        {
                            correo.To.Add(_strMailsTo);
                        }
                        else if (_strMailsTo == "")
                        {
                            correo.To.Add(row["sCorreo"].ToString());
                        }

                        correo.Subject = ConfigurationManager.AppSettings["Subject"] + " - " + "PROCESO ENTREGA-RECEPCIÓN: " + (drGeneral.Table.Columns.Contains("sDProceso") ? drGeneral["sDProceso"].ToString() : "");

                        // PARA EL USO DE HTML
                        correo.SubjectEncoding = System.Text.Encoding.UTF8;
                        //
                        correo.Body =

                        "<style>" +
                        "  .namefont { font-size : 14pt; text-align:center;  background-color: #B0C4DE }" +
                        "  .bodyfont { font-size : 12pt;  text-align:justify }" +
                        "  .s4font { font-size : 10pt; text-align:center}" +
                        "  .texto { color:#848484; font-weight: bold }"
                        +
                        "</style>" +
                            "<div  align =" + "center" + ">" + "<b>" + " Notificación" + "</b>" + "</div>" +
                            "<div align =" + "center" + ">Sistema de Entrega - Recepción de la Universidad Veracruzana</div>" +
                            "</br></br>" +
                           "<div class=" + "namefont" + ">Estimado(a)  " + "<b>" + row["sNombre"].ToString() + "</b>" + "</div>"
                            + "</br>"
                            + "</br>"

                             + "<div  class=" + "bodyfont" + ">" +
                            "<font face=" + "arial" + ">"
                            + "Le informamos que el periodo para el Proceso Entrega-Recepción: "
                            + "<b>" + (drGeneral.Table.Columns.Contains("sDProceso") ? drGeneral["sDProceso"].ToString() : "") + "</b>" + " ha cambiado."
                            + "</br>"
                            + "</br>"
                            + "<table>"
                            + "<tr>"
                            + "<td class=" + "texto" + ">" + "Fecha  inicial: " + "</td>" + "<td>" + strFechaInicio + "</td>"
                            + "<td>" + "   " + "</td>"
                            + "<td class=" + "texto" + ">" + "Fecha final: " + "</td>" + "<td>" + strFechaFinal + "</td>"
                            + "</tr>"
                            + "</table>"

                            + "</div>"
                            + "</font>"
                            + "<div class=" + "s4font" + "> </br></br><b> Favor de no enviar correos a esta cuenta, ya que es utilizada por un proceso automatizado y por lo tanto no se revisa</b></div>"
                            ;

                        // USO DE HTML
                        correo.BodyEncoding = System.Text.Encoding.UTF8;
                        correo.IsBodyHtml = true;
                        //
                        this._correos.Add(correo);

                        objMensaje.idProceso = Convert.ToInt32(row["idProceso"].ToString());
                        objMensaje.idUsuDest = Convert.ToInt32(row["idUsuario"].ToString());
                        objMensaje.idUsuRem = _idUsuRem;             //ID DEL USUARIO SERUV
                        objMensaje.strAsunto = correo.Subject;
                        objMensaje.strMensaje = correo.Body;
                        objMensaje.correoTo = correo.To.ToString();
                        objMensaje.subject = correo.Subject.ToString();
                        this._Mensajes.Add(objMensaje);
                    }
                }
            }
            catch
            {
                _ds.Dispose();
            }
            finally
            {
                _ds.Dispose();
            }
        }
        #endregion

        #region pNotificacionModificarProcesoExt
        /// <summary>
        /// Función que envia correos electronicos a los participantes cuando se modifican o agregan fechas extemporáneas dentro de un proceso
        /// Autor: Edgard Morales, Emmanuel Méndez Flores
        /// </summary>
        /// <param "strDatos">Cadena que contiene la información necesaria para generar las notificaciones</param>
        /// <returns>devuelve una lista de objeto correos y una lista de objeto mensaje</returns>
        public void pNotificacionModificarProcesoExt(string strDatos)
        {
            try
            {
                DataSet ds = ConvertXMLToDataSet(strDatos);
                _ds = ds;

                if (ds != null && ds.Tables != null)
                {
                    DataRow drGeneral = ds.Tables[0].Rows[0];
                    DataTable drParticipantes = ds.Tables[1];

                    string strFechaInicioExt, strFechaFinalExt, strFiExt, strFfExt, strFechaInicio, strFechaFinal, strFi, strFf = "";

                    strFi = drGeneral.Table.Columns.Contains("dFInicio") ? drGeneral["dFInicio"].ToString() : "";
                    strFechaInicio = DateTime.Parse(strFi).ToString("dd-MM-yyyy");
                    strFf = drGeneral.Table.Columns.Contains("dFFinal") ? drGeneral["dFFinal"].ToString() : "";
                    strFechaFinal = DateTime.Parse(strFf).ToString("dd-MM-yyyy");

                    strFiExt = drGeneral.Table.Columns.Contains("dFFInicioEX") ? drGeneral["dFFInicioEX"].ToString() : "";
                    strFechaInicioExt = DateTime.Parse(strFiExt).ToString("dd-MM-yyyy");
                    strFfExt = drGeneral.Table.Columns.Contains("dFFinalEX") ? drGeneral["dFFinalEX"].ToString() : "";
                    strFechaFinalExt = DateTime.Parse(strFfExt).ToString("dd-MM-yyyy");

                    foreach (DataRow row in ds.Tables[1].Rows)
                    {
                        clsNotificacion objMensaje = new clsNotificacion(); // Objeto que almacenara los mensajes
                        System.Net.Mail.MailMessage correo = new System.Net.Mail.MailMessage();
                        correo.From = new System.Net.Mail.MailAddress(ConfigurationManager.AppSettings["AppMail"]); // Correo desde el Cual se enviaran las notificaciones

                        if (_strMailsTo != "")
                        {
                            correo.To.Add(_strMailsTo);
                        }
                        else if (_strMailsTo == "")
                        {
                            correo.To.Add(row["sCorreo"].ToString());
                        }

                        correo.Subject = ConfigurationManager.AppSettings["Subject"] + " - " + "PROCESO ENTREGA-RECEPCIÓN: " + (drGeneral.Table.Columns.Contains("sDProceso") ? drGeneral["sDProceso"].ToString() : "");

                        // PARA EL USO DE HTML
                        correo.SubjectEncoding = System.Text.Encoding.UTF8;
                        //
                        correo.Body =

                        "<style>" +
                       "  .namefont { font-size : 14pt; text-align:center;  background-color: #B0C4DE }" +
                       "  .bodyfont { font-size : 12pt;  text-align:justify }" +
                       "  .s4font { font-size : 10pt; text-align:center}" +
                       "  .texto { color:#848484; font-weight: bold }"
                       +
                       "</style>" +
                           "<div  align =" + "center" + ">" + "<b>" + " Notificación" + "</b>" + "</div>" +
                           "<div align =" + "center" + ">Sistema de Entrega - Recepción de la Universidad Veracruzana</div>" +
                           "</br></br>" +
                          "<div class=" + "namefont" + ">Estimado(a)  " + "<b>" + row["sNombre"].ToString() + "</b>" + "</div>"
                           + "</br>"
                           + "</br>"

                            + "<div  class=" + "bodyfont" + ">" +
                           "<font face=" + "arial" + ">"
                          + "Le informamos que se ha ampliado el periodo para el proceso  "
                           + "<b>" + (drGeneral.Table.Columns.Contains("sDProceso") ? drGeneral["sDProceso"].ToString() : "") + "</b>"
                           + "</br>"
                           + "</br>"
                           + "<table>"
                           + "<tr>"
                           + "<td class=" + "texto" + ">" + "Fecha de apertura: " + "</td>" + "<td>" + strFechaInicio + "</td>"
                           + "<td class=" + "texto" + ">" + "Fecha de cierre: " + "</td>" + "<td>" + strFechaFinal + "</td>"
                           + "</tr>"
                           + "<tr>"
                           + "<td class=" + "texto" + ">" + "Fecha inicial   extemporánea: " + "</td>" + "<td>" + strFechaInicioExt + "</td>"
                           + "<td class=" + "texto" + ">" + "Fecha final   extemporánea: " + "</td>" + "<td>" + strFechaFinalExt + "</td>"
                           + "</tr>"

                           + "<tr>"
                           + "<td class=" + "texto" + ">" + "Justificación: " + "</td>"
                           + "</tr>"
                           + "<tr>"
                           + "<td colspan=" + "4" + ">" + (drGeneral.Table.Columns.Contains("sJustificacion") ? drGeneral["sJustificacion"].ToString() : "") + "</td>"
                           + "</tr>"

                           + "</table>"

                           + "</div>"
                           + "</font>"
                           + "<div class=" + "s4font" + "> </br></br><b> Favor de no enviar correos a esta cuenta, ya que es utilizada por un proceso automatizado y por lo tanto no se revisa</b></div>"
                           ;

                        // USO DE HTML
                        correo.BodyEncoding = System.Text.Encoding.UTF8;
                        correo.IsBodyHtml = true;
                        //
                        this._correos.Add(correo);

                        objMensaje.idProceso = Convert.ToInt32(row["idProceso"].ToString());
                        objMensaje.idUsuDest = Convert.ToInt32(row["idUsuario"].ToString());
                        objMensaje.idUsuRem = idUsuRem;         //ID DEL USUARIO SERUV
                        objMensaje.strAsunto = correo.Subject;
                        objMensaje.strMensaje = correo.Body;
                        objMensaje.correoTo = correo.To.ToString();
                        objMensaje.subject = correo.Subject.ToString();
                        this._Mensajes.Add(objMensaje);
                    }
                }
            }
            catch
            {
                _ds.Dispose();
            }
            finally
            {
                _ds.Dispose();
            }
        }
        #endregion

        #region pCambSujOb()
        /// <summary>
        /// Función que envia correos electronicos cuando se cambia un sujeto obligado, envia al anterior y al nuevo sujeto obligado
        /// Autor: Edgard Morales González, Emmanuel Méndez Flores
        /// </summary>
        /// <param "strDatos">Cadena que contiene la información necesaria para generar las notificaciones</param>
        /// <returns>devuelve una lista de objeto correos y una lista de objeto mensaje</returns>
        private void pCambSujOb(string strDatos)
        {
            try
            {
                DataSet ds = ConvertXMLToDataSet(strDatos);
                _ds = ds;

                if (ds != null && ds.Tables != null)
                {
                    DataRow drSujObAnt = ds.Tables[0].Rows[0];
                    DataRow drSujObNue = ds.Tables[1].Rows[0];


                    //---------------------------------------------------------Notificacion que se envia al nuevo Sujeto Obligado---------------------//
                    foreach (DataRow row in ds.Tables[1].Rows)
                    {
                        clsNotificacion objMensaje = new clsNotificacion(); // Objeto que almacenara los mensajes
                        System.Net.Mail.MailMessage correo = new System.Net.Mail.MailMessage();
                        correo.From = new System.Net.Mail.MailAddress(ConfigurationManager.AppSettings["AppMail"]); // Correo desde el Cual se enviaran las notificaciones

                        if (_strMailsTo != "")
                        {
                            correo.To.Add(_strMailsTo);
                        }
                        else if (_strMailsTo == "")
                        {
                            correo.To.Add(row["sCorreo"].ToString());
                        }

                        correo.Subject = ConfigurationManager.AppSettings["Subject"] + " - " + "PROCESO ENTREGA-RECEPCIÓN: " + (drSujObAnt.Table.Columns.Contains("sDProceso") ? drSujObAnt["sDProceso"].ToString() : "");
                        // PARA EL USO DE HTML
                        correo.SubjectEncoding = System.Text.Encoding.UTF8;
                        //
                        correo.Body =

                        "<style>" +
                     "  .namefont { font-size : 14pt; text-align:center;  background-color: #B0C4DE }" +
                     "  .bodyfont { font-size : 12pt;  text-align:justify }" +
                     "  .s4font { font-size : 10pt; text-align:center}" +
                     "  .texto { color:#848484; font-weight: bold }"
                     +
                     "</style>" +
                         "<div  align =" + "center" + ">" + "<b>" + " Notificación" + "</b>" + "</div>" +
                         "<div align =" + "center" + ">Sistema de Entrega - Recepción de la Universidad Veracruzana</div>" +
                         "</br></br>" +
                        "<div class=" + "namefont" + ">Estimado(a)  " + "<b>" + row["sNombre"].ToString() + "</b>" + "</div>"
                         + "</br>"
                         + "</br>"
                          + "<div  class=" + "bodyfont" + ">" +
                         "<font face=" + "arial" + ">"

                         + "<table>"
                            + "<tr>"
                                + "<td class=" + "texto" + ">" + "Usted ha sido asignado(a) como sujeto obligado de la dependencia/entidad: " + "</td>"
                                + "<td>" + (drSujObAnt.Table.Columns.Contains("nFKDepcia") ? drSujObAnt["nFKDepcia"].ToString() : "") + " " + (drSujObAnt.Table.Columns.Contains("sDDepcia") ? drSujObAnt["sDDepcia"].ToString() : "") + "</td>"
                            + "</tr>"
                             + "<tr>"
                                + "<td class=" + "texto" + ">" + "En el Proceso Entrega-Recepción: " + "</td>"
                                + "<td>" + (drSujObAnt.Table.Columns.Contains("sDProceso") ? drSujObAnt["sDProceso"].ToString() : "") + "</td>"
                            + "</tr>"
                         + "</table>"

                         + "</br>"
                         + "</div>"
                         + "</font>"
                         + "<div class=" + "s4font" + "> </br></br><b> Favor de no enviar correos a esta cuenta, ya que es utilizada por un proceso automatizado y por lo tanto no se revisa</b></div>"
                         ;

                        // USO DE HTML
                        correo.BodyEncoding = System.Text.Encoding.UTF8;
                        correo.IsBodyHtml = true;
                        //
                        this._correos.Add(correo);

                        objMensaje.idProceso = Convert.ToInt32(row["idProceso"].ToString());
                        objMensaje.idUsuDest = Convert.ToInt32(row["idUsuario"].ToString());
                        objMensaje.idUsuRem = idUsuRem;              //ID DEL USUARIO SERUV
                        objMensaje.strAsunto = correo.Subject;
                        objMensaje.strMensaje = correo.Body;
                        objMensaje.correoTo = correo.To.ToString();
                        objMensaje.subject = correo.Subject.ToString();
                        this._Mensajes.Add(objMensaje);
                    }
                }
            }
            catch
            {
                _ds.Dispose();
            }
            finally
            {
                _ds.Dispose();
            }
        }
        #endregion

        #region pIncPart()
        /// <summary>
        /// Función que envia correos electronicos cuando se incluyen participantes
        /// Autor: Edgard Morales González, Emmanuel Méndez Flores
        /// </summary>
        /// <param "strDatos">Cadena que contiene la información necesaria para generar las notificaciones</param>
        /// <returns>devuelve una lista de objeto correos y una lista de objeto mensaje</returns>
        private void pIncPart(string strDatos)
        {
            try
            {
                DataSet ds = ConvertXMLToDataSet(strDatos);
                _ds = ds;

                if (ds != null && ds.Tables != null)
                {
                    DataTable drSujObAnt = ds.Tables[0];

                    foreach (DataRow row in ds.Tables[0].Rows)
                    {
                        string strFechaInicio, strFechaFinal, strFi, strFf = "";
                        strFi = row["dFInicio"].ToString();
                        strFechaInicio = DateTime.Parse(strFi).ToString("dd-MM-yyyy");
                        strFf = row["dFFinal"].ToString();
                        strFechaFinal = DateTime.Parse(strFf).ToString("dd-MM-yyyy");

                        clsNotificacion objMensaje = new clsNotificacion(); // Objeto que almacenara los mensajes
                        System.Net.Mail.MailMessage correo = new System.Net.Mail.MailMessage();
                        correo.From = new System.Net.Mail.MailAddress(ConfigurationManager.AppSettings["AppMail"]); // Correo desde el Cual se enviaran las notificaciones

                        if (_strMailsTo != "")
                        {
                            correo.To.Add(_strMailsTo);
                        }
                        else if (_strMailsTo == "")
                        {
                            correo.To.Add(row["sCorreo"].ToString());
                        }


                        correo.Subject = ConfigurationManager.AppSettings["Subject"] + " - " + "PROCESO ENTREGA-RECEPCIÓN: " + row["sDProceso"].ToString();
                        // PARA EL USO DE HTML
                        correo.SubjectEncoding = System.Text.Encoding.UTF8;
                        //
                        correo.Body =

                        "<style>" +
                        "  .namefont { font-size : 14pt; text-align:center;  background-color: #B0C4DE }" +
                        "  .bodyfont { font-size : 12pt;  text-align:justify }" +
                        "  .s4font { font-size : 10pt; text-align:center}" +
                        "  .texto { color:#848484; font-weight: bold }"
                        +
                        "</style>" +
                            "<div  align =" + "center" + ">" + "<b>" + " Notificación" + "</b>" + "</div>" +
                            "<div align =" + "center" + ">Sistema de Entrega - Recepción de la Universidad Veracruzana</div>" +
                            "</br></br>" +
                           "<div class=" + "namefont" + ">Estimado(a)  " + "<b>" + row["sNombre"].ToString() + "</b>" + "</div>"
                            + "</br>"
                            + "</br>"

                             + "<div  class=" + "bodyfont" + ">" +
                            "<font face=" + "arial" + ">"
                            + "Usted ha sido asignado(a) para participar en el Proceso Entrega-Recepción: "
                            + "<b>" + row["sDProceso"].ToString() + "</b>"
                            + "</br>"
                            + "</br>"
                            + "<table>"
                           + "<tr>"
                           + "<td colspan=" + "5" + " class=" + "texto" + ">" + "Como sujeto obligado de la dependencia/entidad: " + "</td>"
                           + "</tr>"
                           + "<tr>"
                           + "<td colspan=" + "5" + ">" + row["nDepcia"].ToString() + " " + row["sDDepcia"].ToString() + "</b>" + "</td>"
                           + "</tr>"
                            + "<tr>"
                            + "<td class=" + "texto" + ">" + "Fecha de apertura: " + "</td>" + "<td>" + strFechaInicio + "</td>"
                            + "<td></td>"
                            + "<td class=" + "texto" + ">" + "Fecha de cierre: " + "</td>" + "<td>" + strFechaFinal + "</td>"
                            + "</tr>"
                            + "</table>"

                            + "</div>"
                            + "</font>"
                            + "<div class=" + "s4font" + "> </br></br><b> Favor de no enviar correos a esta cuenta, ya que es utilizada por un proceso automatizado y por lo tanto no se revisa</b></div>"
                            ;

                        // USO DE HTML
                        correo.BodyEncoding = System.Text.Encoding.UTF8;
                        correo.IsBodyHtml = true;
                        //
                        this._correos.Add(correo);

                        objMensaje.idProceso = Convert.ToInt32(row["idProceso"].ToString());
                        objMensaje.idUsuDest = Convert.ToInt32(row["idUsuario"].ToString());
                        objMensaje.idUsuRem = idUsuRem;              //ID DEL USUARIO SERUV
                        objMensaje.strAsunto = correo.Subject;
                        objMensaje.strMensaje = correo.Body;
                        objMensaje.correoTo = correo.To.ToString();
                        objMensaje.subject = correo.Subject.ToString();
                        this._Mensajes.Add(objMensaje);
                    }
                }
            }
            catch
            {
                _ds.Dispose();
            }
            finally
            {
                _ds.Dispose();
            }
        }
        #endregion

        #region pExcPart()
        /// <summary>
        /// Función que envia correos electronicos cuando se excluyen participantes
        /// Autor: Edgard Morales, Emmanuel Méndez Flores
        /// </summary>
        /// <param "strDatos">Cadena que contiene la información necesaria para generar las notificaciones</param>
        /// <returns>devuelve una lista de objeto correos y una lista de objeto mensaje</returns>
        private void pExcPart(string strDatos)
        {
            try
            {
                DataSet ds = ConvertXMLToDataSet(strDatos);
                _ds = ds;

                if (ds != null && ds.Tables != null)
                {
                    DataTable drExcPart = ds.Tables[0];

                    foreach (DataRow row in ds.Tables[0].Rows)
                    {
                        clsNotificacion objMensaje = new clsNotificacion(); // Objeto que almacenara los mensajes
                        System.Net.Mail.MailMessage correo = new System.Net.Mail.MailMessage();
                        correo.From = new System.Net.Mail.MailAddress(ConfigurationManager.AppSettings["AppMail"]); // Correo desde el Cual se enviaran las notificaciones

                        if (_strMailsTo != "")
                        {
                            correo.To.Add(_strMailsTo);
                        }
                        else if (_strMailsTo == "")
                        {
                            correo.To.Add(row["sCorreo"].ToString());
                        }

                        correo.Subject = ConfigurationManager.AppSettings["Subject"] + " - " + "PROCESO ENTREGA-RECEPCIÓN: " + row["sDProceso"].ToString();
                        // PARA EL USO DE HTML
                        correo.SubjectEncoding = System.Text.Encoding.UTF8;
                        //
                        correo.Body =

                        "<style>" +
                       "  .namefont { font-size : 14pt; text-align:center;  background-color: #B0C4DE }" +
                       "  .bodyfont { font-size : 12pt;  text-align:justify }" +
                       "  .s4font { font-size : 10pt; text-align:center}" +
                       "  .texto { color:#848484; font-weight: bold }"
                       +
                       "</style>" +
                           "<div  align =" + "center" + ">" + "<b>" + " Notificación" + "</b>" + "</div>" +
                           "<div align =" + "center" + ">Sistema de Entrega - Recepción de la Universidad Veracruzana</div>" +
                           "</br></br>" +
                          "<div class=" + "namefont" + ">Estimado(a)  " + "<b>" + row["sNombre"].ToString() + "</b>" + "</div>"
                           + "</br>"
                           + "</br>"

                            + "<div  class=" + "bodyfont" + ">" +
                           "<font face=" + "arial" + ">"
                           + "Usted ha sido desasignado(a) para participar en el Proceso Entrega-Recepción: "
                           + "<b>" + row["sDProceso"].ToString() + "</b>"
                           + "</br>"
                           + "</br>"
                           + "<table>"
                          + "<tr>"
                          + "<td colspan=" + "5" + "class=" + "texto" + ">" + "Con la dependencia/entidad: " + "</td>"
                          + "</tr>"
                          + "<tr>"
                          + "<td colspan=" + "5" + ">" + row["nFKDepcia"].ToString() + " " + row["sDDepcia"].ToString() + "</b>" + "</td>"
                          + "</tr>"
                           + "</table>"

                           + "</div>"
                           + "</font>"
                           + "<div class=" + "s4font" + "> </br></br><b> Favor de no enviar correos a esta cuenta, ya que es utilizada por un proceso automatizado y por lo tanto no se revisa</b></div>"
                            ;
                        // USO DE HTML
                        correo.BodyEncoding = System.Text.Encoding.UTF8;
                        correo.IsBodyHtml = true;
                        //
                        this._correos.Add(correo);

                        objMensaje.idProceso = Convert.ToInt32(row["idProceso"].ToString());
                        objMensaje.idUsuDest = Convert.ToInt32(row["idUsuario"].ToString());
                        objMensaje.idUsuRem = idUsuRem;      //ID DEL USUARIO SERUV
                        objMensaje.strAsunto = correo.Subject;
                        objMensaje.strMensaje = correo.Body;
                        objMensaje.correoTo = correo.To.ToString();
                        objMensaje.subject = correo.Subject.ToString();
                        this._Mensajes.Add(objMensaje);
                    }
                }
            }
            catch
            {
                _ds.Dispose();
            }
            finally
            {
                _ds.Dispose();
            }
        }
        #endregion

        #region pSujRecp ()
        /// <summary>
        /// Función que envia correos electronicos cuando se agrega o modifica sujeto receptor
        /// Autor: Edgar Morales González, Emmanuel Méndez Flores
        /// </summary>
        /// <param "strDatos">Cadena que contiene la información necesaria para generar las notificaciones</param>
        /// <returns>devuelve una lista de objeto correos y una lista de objeto mensaje</returns>
        private void pSujRecp(string strDatos)
        {
            try
            {
                DataSet ds = ConvertXMLToDataSet(strDatos);
                _ds = ds;

                if (ds != null && ds.Tables != null)
                {
                    DataTable drSujRecp = ds.Tables[0];

                    foreach (DataRow row in ds.Tables[0].Rows)
                    {
                        string strFechaInicio, strFechaFinal, strFi, strFf = "";    //Variables que almacenaran las fechas del proceso extemporáneo
                        strFi = row["dFInicio"].ToString();
                        strFechaInicio = DateTime.Parse(strFi).ToString("dd-MM-yyyy");
                        strFf = row["dFFinal"].ToString();
                        strFechaFinal = DateTime.Parse(strFf).ToString("dd-MM-yyyy");

                        clsNotificacion objMensaje = new clsNotificacion(); // Objeto que almacenara los mensajes
                        System.Net.Mail.MailMessage correo = new System.Net.Mail.MailMessage();
                        correo.From = new System.Net.Mail.MailAddress(ConfigurationManager.AppSettings["AppMail"]); // Correo desde el Cual se enviaran las notificaciones

                        if (_strMailsTo != "")
                        {
                            correo.To.Add(_strMailsTo);
                        }
                        else if (_strMailsTo == "")
                        {
                            correo.To.Add(row["sCorreo"].ToString());
                        }

                        correo.Subject = ConfigurationManager.AppSettings["Subject"] + " - " + "PROCESO ENTREGA-RECEPCIÓN: " + row["sDProceso"].ToString();
                        // PARA EL USO DE HTML
                        correo.SubjectEncoding = System.Text.Encoding.UTF8;
                        //
                        correo.Body =

                        "<style>" +
                        "  .namefont { font-size : 14pt; text-align:center;  background-color: #B0C4DE }" +
                        "  .bodyfont { font-size : 12pt;  text-align:justify }" +
                        "  .s4font { font-size : 10pt; text-align:center}" +
                        "  .texto { color:#848484; font-weight: bold }"
                        +
                        "</style>" +
                            "<div  align =" + "center" + ">" + "<b>" + " Notificación" + "</b>" + "</div>" +
                            "<div align =" + "center" + ">Sistema de Entrega - Recepción de la Universidad Veracruzana</div>" +
                            "</br></br>" +
                           "<div class=" + "namefont" + ">Estimado(a)  " + "<b>" + row["sNombre"].ToString() + "</b>" + "</div>"
                            + "</br>"
                            + "</br>"

                             + "<div  class=" + "bodyfont" + ">" +
                            "<font face=" + "arial" + ">"
                            + "Usted ha sido asignado(a) para participar en el Proceso Entrega-Recepción: "
                            + "<b>" + row["sDProceso"].ToString() + "</b>"
                            + "</br>"
                            + "</br>"
                            + "<table>"
                           + "<tr>"
                           + "<td colspan=" + "5" + " class=" + "texto" + ">" + "Como sujeto receptor de la dependencia/entidad: " + "</td>"
                           + "</tr>"
                           + "<tr>"
                           + "<td colspan=" + "5" + ">" + row["nDepcia"].ToString() + " " + row["sDDepcia"].ToString() + "</b>" + "</td>"
                           + "</tr>"
                            + "<tr>"
                            + "<td class=" + "texto" + ">" + "Fecha de apertura inicial: " + "</td>" + "<td>" + strFechaInicio + "</td>"
                            + "<td></td>"
                            + "<td class=" + "texto" + ">" + "Fecha de apertura final: " + "</td>" + "<td>" + strFechaFinal + "</td>"
                            + "</tr>"
                            + "</table>"

                            + "</div>"
                            + "</font>"
                            + "<div class=" + "s4font" + "> </br></br><b> Favor de no enviar correos a esta cuenta, ya que es utilizada por un proceso automatizado y por lo tanto no se revisa</b></div>"
                            ;
                        // USO DE HTML
                        correo.BodyEncoding = System.Text.Encoding.UTF8;
                        correo.IsBodyHtml = true;
                        //
                        this._correos.Add(correo);

                        objMensaje.idProceso = Convert.ToInt32(row["idProceso"].ToString());
                        objMensaje.idUsuDest = Convert.ToInt32(row["idUsuario"].ToString());
                        objMensaje.idUsuRem = idUsuRem;     //ID DEL USUARIO SERUV
                        objMensaje.strAsunto = correo.Subject;
                        objMensaje.strMensaje = correo.Body;
                        objMensaje.correoTo = correo.To.ToString();
                        objMensaje.subject = correo.Subject.ToString();
                        this._Mensajes.Add(objMensaje);
                    }

                }
            }
            catch
            {
                _ds.Dispose();
            }
            finally
            {
                _ds.Dispose();
            }
        }
        #endregion

        #region pPartExt()
        /// <summary>
        /// Función que envia correos electronicos cuando se agrega un proceso extemporaneo a un participante
        /// Autor: Edgard Morales González, Emmanuel Méndez Flores
        /// </summary>
        /// <param "strDatos">Cadena que contiene la información necesaria para generar las notificaciones</param>
        /// <returns>devuelve una lista de objeto correos y una lista de objeto mensaje</returns>
        private void pPartExt(string strDatos)
        {
            try
            {
                DataSet ds = ConvertXMLToDataSet(strDatos);
                _ds = ds;

                if (ds != null && ds.Tables != null)
                {
                    DataRow drExcPart = ds.Tables[0].Rows[0];
                    foreach (DataRow row in ds.Tables[0].Rows)
                    {
                        string strFechaInicio, strFechaFinal, strFi, strFf = "";
                        strFi = row["dFInicio"].ToString();
                        strFechaInicio = DateTime.Parse(strFi).ToString("dd-MM-yyyy");
                        strFf = row["dFFinal"].ToString();
                        strFechaFinal = DateTime.Parse(strFf).ToString("dd-MM-yyyy");

                        clsNotificacion objMensaje = new clsNotificacion(); // Objeto que almacenara los mensajes
                        System.Net.Mail.MailMessage correo = new System.Net.Mail.MailMessage();
                        correo.From = new System.Net.Mail.MailAddress(ConfigurationManager.AppSettings["AppMail"]); // Correo desde el Cual se enviaran las notificaciones

                        if (_strMailsTo != "")
                        {
                            correo.To.Add(_strMailsTo);
                        }
                        else if (_strMailsTo == "")
                        {
                            correo.To.Add(row["sCorreo"].ToString());
                        }

                        correo.Subject = ConfigurationManager.AppSettings["Subject"] + " - " + "PROCESO ENTREGA-RECEPCIÓN: " + row["sDProceso"].ToString();
                        // PARA EL USO DE HTML
                        correo.SubjectEncoding = System.Text.Encoding.UTF8;
                        //
                        correo.Body =

                        "<style>" +
                    "  .namefont { font-size : 14pt; text-align:center;  background-color: #B0C4DE }" +
                    "  .bodyfont { font-size : 12pt;  text-align:justify }" +
                    "  .s4font { font-size : 10pt; text-align:center}" +
                    "  .texto { color:#848484; font-weight: bold }"
                    +
                    "</style>"
                    +
                        "<div  align =" + "center" + ">" + "<b>" + " Notificación" + "</b>" + "</div>" +
                        "<div align =" + "center" + ">Sistema de Entrega - Recepción de la Universidad Veracruzana</div>" +
                        "</br></br>" +
                       "<div class=" + "namefont" + ">Estimado(a)  " + "<b>" + row["sNombre"].ToString() + "</b>" + "</div>"
                        + "</br>"
                        + "</br>"

                         + "<div  class=" + "bodyfont" + ">" +
                        "<font face=" + "arial" + ">"
                        + "Responsable de la dependencia/entidad: " + "<b>" + row["nDepcia"].ToString() + " " + row["sDDepcia"].ToString() + "</b>"
                       + "Le informamos que se ha agregado un periodo de apertura extemporánea para el Proceso Entrega-Recepción: "
                        + "<b>" + row["sDProceso"].ToString() + "</b>"
                        + "</br>"
                        + "</br>"
                        + "<table>"
                        + "<tr>"
                        + "<td class=" + "texto" + ">" + "Fecha inicial de apertura extemporánea: " + "</td>" + "<td>" + strFechaInicio + "</td>"
                        + "<td class=" + "texto" + ">" + "Fecha final de apertura extemporánea: " + "</td>" + "<td>" + strFechaFinal + "</td>"
                        + "</tr>"
                        + "<tr>"
                        + "<td class=" + "texto" + ">" + "Justificación: " + "</td>"
                        + "</tr>"
                        + "<tr>"
                        + "<td colspan=" + "4" + ">" + row["sJustificacion"].ToString() + "</td>"
                        + "</tr>"
                        + "</table>"

                        + "</div>"
                        + "</font>"
                        + "<div class=" + "s4font" + "> </br></br><b> Favor de no enviar correos a esta cuenta, ya que es utilizada por un proceso automatizado y por lo tanto no se revisa</b></div>"
                        ;
                        // USO DE HTML
                        correo.BodyEncoding = System.Text.Encoding.UTF8;
                        correo.IsBodyHtml = true;
                        //
                        this._correos.Add(correo);

                        objMensaje.idProceso = Convert.ToInt32(row["idProceso"].ToString());
                        objMensaje.idUsuDest = Convert.ToInt32(row["idUsuario"].ToString());
                        objMensaje.idUsuRem = idUsuRem;    //ID DEL USUARIO SERUV
                        objMensaje.strAsunto = correo.Subject;
                        objMensaje.strMensaje = correo.Body;
                        objMensaje.correoTo = correo.To.ToString();
                        objMensaje.subject = correo.Subject.ToString();
                        this._Mensajes.Add(objMensaje);
                    }
                }
            }
            catch
            {
                _ds.Dispose();
            }
            finally
            {
                _ds.Dispose();
            }
        }
        #endregion

        #region pCierreProc()
        /// <summary>
        /// Función que envia correos electronicos cuando se cierra un proceso
        /// Autor: Edgard Morales González
        /// </summary>
        /// <param "strDatos">Cadena que contiene la información necesaria para generar las notificaciones</param>
        /// <returns>devuelve una lista de objeto correos y una lista de objeto mensaje</returns>
        private void pCierreProc(string strDatos)
        {
            try
            {
                DataSet ds = ConvertXMLToDataSet(strDatos);
                _ds = ds;

                if (ds != null && ds.Tables != null)
                {
                    DataRow drEnlaces = ds.Tables[0].Rows[0];

                    foreach (DataRow row in ds.Tables[0].Rows)
                    {
                        clsNotificacion objMensaje = new clsNotificacion(); // Objeto que almacenara los mensajes
                        System.Net.Mail.MailMessage correo = new System.Net.Mail.MailMessage();
                        correo.From = new System.Net.Mail.MailAddress(ConfigurationManager.AppSettings["AppMail"]); // Correo desde el Cual se enviaran las notificaciones

                        if (_strMailsTo != "")
                        {
                            correo.To.Add(_strMailsTo);
                        }
                        else if (_strMailsTo == "")
                        {
                            correo.To.Add(row["sCorreo"].ToString());
                        }

                        correo.Subject = ConfigurationManager.AppSettings["Subject"] + " - " + "PROCESO ENTREGA-RECEPCIÓN: " + (drEnlaces.Table.Columns.Contains("sDProceso") ? drEnlaces["sDProceso"].ToString() : "");
                        // PARA EL USO DE HTML
                        correo.SubjectEncoding = System.Text.Encoding.UTF8;
                        //

                        correo.Body =

                        "<style>" +
                     "  .namefont { font-size : 14pt; text-align:center;  background-color: #B0C4DE }" +
                     "  .bodyfont { font-size : 12pt;  text-align:justify }" +
                     "  .s4font { font-size : 10pt; text-align:center}" +
                     "  .texto { color:#848484; font-weight: bold }"
                     +
                     "</style>" +
                         "<div  align =" + "center" + ">" + "<b>" + " Notificación" + "</b>" + "</div>" +
                         "<div align =" + "center" + ">Sistema de Entrega - Recepción de la Universidad Veracruzana</div>" +
                         "</br></br>" +
                        "<div class=" + "namefont" + ">Estimado(a)  " + "<b>" + row["sNombre"].ToString() + "</b>" + "</div>"
                         + "</br>"
                         + "</br>"
                          + "<div  class=" + "bodyfont" + ">" +
                         "<font face=" + "arial" + ">"
                              + "<div  class=" + "bodyfont" + ">" +
                                    "<font face=" + "arial" + ">"
                                    + "Se le informa que se ha cerrado el Proceso Entrega-Recepción: "
                                    + "<b>" + (drEnlaces.Table.Columns.Contains("sDProceso") ? drEnlaces["sDProceso"].ToString() : "") + "</b>"
                                    + "</br>"
                                    + "</br>"
                         + "</br>"
                         + "</div>"
                         + "</font>"
                         + "<div class=" + "s4font" + "> </br></br><b> Favor de no enviar correos a esta cuenta, ya que es utilizada por un proceso automatizado y por lo tanto no se revisa</b></div>"
                         ;

                        // USO DE HTML
                        correo.BodyEncoding = System.Text.Encoding.UTF8;
                        correo.IsBodyHtml = true;
                        //
                        this._correos.Add(correo);

                        objMensaje.idProceso = Convert.ToInt32(row["idProceso"].ToString());
                        objMensaje.idUsuDest = Convert.ToInt32(row["idFKUsuario"].ToString());
                        objMensaje.idUsuRem = idUsuRem;     //ID DEL USUARIO SERUV
                        objMensaje.strAsunto = correo.Subject;
                        objMensaje.strMensaje = correo.Body;
                        objMensaje.correoTo = correo.To.ToString();
                        objMensaje.subject = correo.Subject.ToString();
                        this._Mensajes.Add(objMensaje);
                    }
                }
            }
            catch
            {
                _ds.Dispose();
            }
            finally
            {
                _ds.Dispose();
            }
        }
        #endregion

        #region pReapertProc()
        /// <summary>
        /// Función que envia correos electronicos a los usuarios que participan en un proceso para informar que este se ha reabierto
        /// Autor: Edgard Morales González
        /// </summary>
        /// <param "strDatos">Cadena que contiene la información necesaria para generar las notificaciones</param>
        /// <returns>devuelve una lista de objeto correos y una lista de objeto mensaje</returns>
        private void pReapertProc(string strDatos)
        {
            try
            {
                DataSet ds = ConvertXMLToDataSet(strDatos);
                _ds = ds;

                if (ds != null && ds.Tables != null)
                {
                    DataTable dtReapProc = ds.Tables[0];

                    string tipoProc = "";
                    string strFechaInicio, strFechaFinal, strFi, strFf = "";

                    foreach (DataRow row in ds.Tables[0].Rows)
                    {
                        clsNotificacion objMensaje = new clsNotificacion(); // Objeto que almacenara los mensajes
                        System.Net.Mail.MailMessage correo = new System.Net.Mail.MailMessage();
                        correo.From = new System.Net.Mail.MailAddress(ConfigurationManager.AppSettings["AppMail"]); // Correo desde el Cual se enviaran las notificaciones

                        if (_strMailsTo != "")
                        {
                            correo.To.Add(_strMailsTo);
                        }
                        else if (_strMailsTo == "")
                        {
                            correo.To.Add(row["sCorreo"].ToString());
                        }

                        correo.Subject = ConfigurationManager.AppSettings["Subject"] + " - " + "REAPERTURA DEL PROCESO ENTREGA - RECEPCIÓN: " + row["sDProceso"].ToString(); // + (drSujObAnt.Table.Columns.Contains("sDProceso") ? drSujObAnt["sDProceso"].ToString() : "");
                        // PARA EL USO DE HTML
                        correo.SubjectEncoding = System.Text.Encoding.UTF8;
                        //

                        if (row["TipoProc"].ToString() == "NRM")
                        {
                            tipoProc = "apertura";
                        }
                        else if (row["TipoProc"].ToString() == "EXT")
                        {
                            tipoProc = "apertura extemporánea";
                        }

                        strFi = row["dFInicio"].ToString();
                        strFechaInicio = DateTime.Parse(strFi).ToString("dd-MM-yyyy");
                        strFf = row["dFFinal"].ToString(); ;
                        strFechaFinal = DateTime.Parse(strFf).ToString("dd-MM-yyyy");


                        correo.Body =

                        "<style>" +
                     "  .namefont { font-size : 14pt; text-align:center;  background-color: #B0C4DE }" +
                     "  .bodyfont { font-size : 12pt;  text-align:justify }" +
                     "  .s4font { font-size : 10pt; text-align:center}" +
                     "  .texto { color:#848484; font-weight: bold }"
                     +
                     "</style>" +
                         "<div  align =" + "center" + ">" + "<b>" + " Notificación" + "</b>" + "</div>" +
                         "<div align =" + "center" + ">Sistema de Entrega - Recepción de la Universidad Veracruzana</div>" +
                         "</br></br>" +
                        "<div class=" + "namefont" + ">Estimado(a)  " + "<b>" + row["sNombre"].ToString() + "</b>" + "</div>"
                         + "</br>"
                         + "</br>"
                          + "<div  class=" + "bodyfont" + ">" +
                         "<font face=" + "arial" + ">"
                              + "<div  class=" + "bodyfont" + ">" +
                                    "<font face=" + "arial" + ">"

                                + "<table>"
                                      + "<tr>"
                                        + "<td class=" + "texto" + " colspan=" + "4" + ">" + "Se ha reabierto la entrega del proceso entrega-recepción: " + "</td>"
                                    + "</tr>"
                                     + "<tr>"
                                        + "<td colspan=" + "4" + ">" + row["sDProceso"].ToString() + "</td>"
                                    + "</tr>"
                                      + "<tr>"
                                        + "<td class=" + "texto" + ">" + "Con fechas de " + tipoProc + " del : " + "</td>"
                                        + "<td>" + strFechaInicio + "</td>"
                                        + "<td class=" + "texto" + ">" + " al " + "</td>"
                                        + "<td>" + strFechaFinal + "</td>"
                                    + "</tr>"
                                     + "<tr>"
                                        + "<td class=" + "texto" + " colspan=" + "4" + ">" + "Justificación: " + "</td>"
                                    + "</tr>"
                                     + "<tr>"
                                        + "<td colspan=" + "4" + ">" + row["sObservaciones"].ToString() + "</td>"
                                    + "</tr>"
                                + "</table>"

                         + "</br>"
                         + "</div>"
                         + "</font>"
                         + "<div class=" + "s4font" + "> </br></br><b> Favor de no enviar correos a esta cuenta, ya que es utilizada por un proceso automatizado y por lo tanto no se revisa</b></div>"
                         ;

                        // USO DE HTML
                        correo.BodyEncoding = System.Text.Encoding.UTF8;
                        correo.IsBodyHtml = true;
                        //
                        this._correos.Add(correo);

                        objMensaje.idProceso = Convert.ToInt32(row["idProceso"].ToString());
                        objMensaje.idUsuDest = Convert.ToInt32(row["idFKUsuario"].ToString());
                        objMensaje.idUsuRem = idUsuRem;      //ID DEL USUARIO SERUV
                        objMensaje.strAsunto = correo.Subject;
                        objMensaje.strMensaje = correo.Body;
                        objMensaje.correoTo = correo.To.ToString();
                        objMensaje.subject = correo.Subject.ToString();
                        this._Mensajes.Add(objMensaje);
                    }
                }
            }
            catch
            {
                _ds.Dispose();
            }
            finally
            {
                _ds.Dispose();
            }
        }

        #endregion

        #region pNotificacion_Inclu_exclu_Anexo
        /// <summary>
        /// Procedimiento que genera la notificacion que se enviara  al excluir e incluir un anexo
        /// Autor: Edgr Morales González, Ma.Guadalupe Dominguez Julian
        /// </summary>
        /// <param "strDatos">Cadena que contiene la información necesaria para generar las notificaciones</param>
        /// <returns>devuelve una lista de objeto correos y una lista de objeto mensaje</returns>
        private void pNotificacion_Inclu_exclu_Anexo(string strDatos)
        {
            try
            {
                DataSet ds = ConvertXMLToDataSet(strDatos);
                _ds = ds;

                if (ds != null && ds.Tables != null)
                {
                    DataRow drGeneral = ds.Tables[0].Rows[0];
                    String strProceso = " ";
                    String strDepcia = "";
                    String strCAnexo = "";
                    String strAnexo = "";
                    String strCApartado = "";
                    String strApartado = "";
                    String strEx_In = "";
                    String strJustif = "";
                    String strNombreExc = "";
                    int idProc = 0;
                    string sEI = "";

                    if (ds.Tables[0] != null) // En la tabla 0, regresamos la informacion del Anexo
                    {
                        strProceso = (drGeneral.Table.Columns.Contains("sDProceso") ? drGeneral["sDProceso"].ToString() : "");
                        strDepcia = (drGeneral.Table.Columns.Contains("sDDepcia") ? drGeneral["sDDepcia"].ToString() : "");
                        strCAnexo = (drGeneral.Table.Columns.Contains("sAnexo") ? drGeneral["sAnexo"].ToString() : "");
                        strAnexo = (drGeneral.Table.Columns.Contains("sDAnexo") ? drGeneral["sDAnexo"].ToString() : "");
                        strCApartado = (drGeneral.Table.Columns.Contains("sApartado") ? drGeneral["sApartado"].ToString() : "");
                        strApartado = (drGeneral.Table.Columns.Contains("sDApartado") ? drGeneral["sDApartado"].ToString() : "");
                        strEx_In = (drGeneral.Table.Columns.Contains("cIndAplica") ? drGeneral["cIndAplica"].ToString() : "");
                        strJustif = (drGeneral.Table.Columns.Contains("sJustificacion") ? drGeneral["sJustificacion"].ToString() : "");
                        idProc = Int32.Parse((drGeneral.Table.Columns.Contains("idProceso") ? drGeneral["idProceso"].ToString() : ""));
                        strNombreExc = (drGeneral.Table.Columns.Contains("sNombreExc") ? drGeneral["sNombreExc"].ToString() : "");
                    }
                    if (strEx_In == "S")
                    {
                        sEI = "incluido";
                    }
                    else
                    {
                        sEI = "excluido";
                    }

                    if (ds.Tables[1] != null) // Viene la informacion del Usuario Obligado y Enlace Principal
                    {
                        //::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
                        int idUsuRem = 0;
                        foreach (DataRow row in ds.Tables[1].Rows)
                        {
                            if (row["cTipo"].ToString() == "SOB")
                            {
                                idUsuRem = int.Parse(row["idFKUsuario"].ToString());
                            }
                        }

                        foreach (DataRow row in ds.Tables[1].Rows)
                        {
                            clsNotificacion objMensaje = new clsNotificacion(); // Objeto que almacenara los mensajes
                            System.Net.Mail.MailMessage correo = new System.Net.Mail.MailMessage();
                            correo.From = new System.Net.Mail.MailAddress(ConfigurationManager.AppSettings["AppMail"]); // Correo desde el Cual se enviaran las notificaciones

                            if (_strMailsTo != "")
                            {
                                correo.To.Add(_strMailsTo);
                            }
                            else if (_strMailsTo == "")
                            {
                                correo.To.Add(row["sCorreo"].ToString());
                            }

                            if (strEx_In == "S")
                            {
                                correo.Subject = "INCLUSIÓN DE ANEXO: " + strCAnexo + " " + strAnexo;
                            }
                            else
                            {
                                correo.Subject = "EXCLUSIÓN DE ANEXO: " + strCAnexo + " " + strAnexo;
                            }

                            // PARA EL USO DE HTML
                            correo.SubjectEncoding = System.Text.Encoding.UTF8;
                            //
                            correo.Body =

                        "<style>" +
                            "  .namefont { font-size : 14pt; text-align:center;  background-color: #B0C4DE }" +
                            "  .bodyfont { font-size : 12pt;  text-align:justify }" +
                            "  .s4font { font-size : 10pt; text-align:center}" +
                            "  .texto { color:#848484; font-weight: bold }"
                            +
                            "</style>" +
                                "<div  align =" + "center" + ">" + "<b>" + " Notificación" + "</b>" + "</div>" +
                                "<div align =" + "center" + ">Sistema de Entrega - Recepción de la Universidad Veracruzana</div>" +
                                "</br></br>" +
                               "<div class=" + "namefont" + ">Estimado(a)  " + "<b>" + row["sNombre"].ToString() + "</b>" + "</div>"
                                + "</br>"
                                + "</br>"
                                + "<div  class=" + "bodyfont" + ">" +
                                 "<font face=" + "arial" + ">"

                               + "<table>"
                               + "<tr>"
                                + "<td class=" + "texto" + ">" + "Se ha " + sEI + " el Anexo: " + "</td>"
                                + "<td>" + strCAnexo + " " + strAnexo + "</td>" + "<td></td>" + "<td></td>"
                               + "</tr>"
                               + "<tr>"
                                + "<td class=" + "texto" + ">" + "Apartado: " + "</td>" + "<td>" + strCApartado + " " + strApartado + "</td>"
                               + "</tr>"
                               + "<tr>"
                               + "<td class=" + "texto" + ">" + "Correspondiente al Proceso Entrega-Recepción: " + "</td>" + "<td colspan=" + "4" + ">" + strProceso + "</td>"
                                + "<td></td>"
                                + "</tr>"
                                + "<tr>"
                                + "<td class=" + "texto" + ">" + "Dependencia/Entidad: " + "</td>" + "<td colspan=" + "4" + ">" + strDepcia + "</td>"
                               + "</tr>"
                                + "<tr>"
                                + "<td colspan=" + "1" + " class=" + "texto" + ">" + "Excluido por: " + "</td>"
                                + "<td colspan=" + "4" + ">" + strNombreExc + "</td>"
                               + "</tr>"
                               + "<tr>"
                                + "<td class=" + "texto" + ">" + "Justificación: " + "</td>"
                               + "</tr>"
                               + "<tr>"
                                + "<td colspan=" + "5" + ">" + strJustif + "</td>"
                               + "</tr>"
                                + "</tr>"
                                + "</table>"

                               + "</div>"
                               + "</font>"
                               ;
                            // USO DE HTML
                            correo.BodyEncoding = System.Text.Encoding.UTF8;
                            correo.IsBodyHtml = true;
                            //
                            this._correos.Add(correo);

                            objMensaje.idProceso = idProc;
                            objMensaje.idUsuDest = Convert.ToInt32(row["idFKUsuario"].ToString());
                            objMensaje.idUsuRem = idUsuRem;         //ID DEL USUARIO SERUV
                            objMensaje.strAsunto = correo.Subject;
                            objMensaje.strMensaje = correo.Body;
                            objMensaje.correoTo = correo.To.ToString();
                            objMensaje.subject = correo.Subject.ToString();
                            this._Mensajes.Add(objMensaje);
                        }
                    }
                }
            }
            catch
            {
                _ds.Dispose();
            }
            finally
            {
                _ds.Dispose();
            }
        }
        #endregion

        #region pEnviaRespuesta()
        /// <summary>
        /// Procedimiento que genera notificaciones cuando se responde a un  mensaje recibido.
        /// Autor: Edgar Morales González
        /// </summary>
        /// <param "strDatos">Cadena que contiene la información necesaria para generar las notificaciones</param>
        /// <returns>devuelve una lista de objeto correos y una lista de objeto mensaje</returns>
        private void pEnviaRespuesta(string strDatos)
        {
            try
            {
                DataSet ds = ConvertXMLToDataSet(strDatos);
                _ds = ds;

                string UsuarioEnvia = "";

                if (ds != null && ds.Tables != null)
                {

                    if (ds.Tables[1].Rows.Count > 0)
                    {
                        foreach (DataRow row in ds.Tables[1].Rows)
                        {
                            if (row["envia"].ToString() == "S")         //Nombre de quien envia el mensaje
                            {
                                UsuarioEnvia = row["sNombre"].ToString();
                            }
                        }

                        foreach (DataRow row in ds.Tables[0].Rows)
                        {
                            foreach (DataRow row2 in ds.Tables[1].Rows)
                            {
                                System.Net.Mail.MailMessage correo = new System.Net.Mail.MailMessage();
                                correo.From = new System.Net.Mail.MailAddress(ConfigurationManager.AppSettings["AppMail"]); // Correo desde el Cual se enviaran las notificaciones

                                if (_strMailsTo != "")
                                {
                                    correo.To.Add(_strMailsTo);
                                }
                                else if (_strMailsTo == "")
                                {
                                    correo.To.Add(row2["sCorreo"].ToString());
                                }

                                correo.Subject = ConfigurationManager.AppSettings["Subject"] + " - " + row["sAsunto"].ToString() + " - " + "PROCESO ER: " + row["sDProceso"].ToString();
                                //
                                correo.SubjectEncoding = System.Text.Encoding.UTF8;
                                ///
                                correo.Body =
                                    "<style>" +
                                    "  .namefont { font-size : 14pt; text-align:center;  background-color: #B0C4DE }" +
                                     "  .bodyfont { font-size : 12pt;  text-align:justify }" +
                                     "  .s4font { font-size : 10pt; text-align:center}" +
                                      "  .texto { color:#848484; font-weight: bold }"
                                     +
                                    "</style>"
                                    + "<font face=" + "arial" + ">" +
                                   "<div  align =" + "center" + ">" + "<b>" + " Notificación" + "</b>" + "</div>" +
                                   "<div align =" + "center" + ">Sistema de Entrega - Recepción de la Universidad Veracruzana</div>" +
                                   "</br></br>" +
                                   "<div class=" + "namefont" + ">Estimado(a)  " + "<b>" + row2["sNombre"].ToString() + "</b>" + "</div>"
                                    + "</br>"
                                    + "</br>"

                                   + "<div  class=" + "bodyfont" + ">"

                                   + "</br>"
                                    + row["sMensaje"].ToString()
                                    + "</br>"
                                    + "</div>"
                                     + "<div class=" + "s4font" + "> </br></br><b> Attentamente. " + UsuarioEnvia + "</b></div>"
                                    ;
                                correo.BodyEncoding = System.Text.Encoding.UTF8;
                                correo.IsBodyHtml = true;

                                this._correos.Add(correo);
                            }
                        }
                    }
                }
            }
            catch
            {
                _ds.Dispose();
            }
            finally
            {
                _ds.Dispose();
            }
        }
        #endregion

        #region void pNotificacionER()
        /// <summary>
        /// Procedimiento que genera la notificación que se enviara cuando se envia la entrega de un proceso entrega-recepción
        /// Autor: Edgar Morales González
        /// </summary>
        /// <param "strDatos">Cadena que contiene la información necesaria para generar las notificaciones</param>
        /// <returns>devuelve una lista de objeto correos y una lista de objeto mensaje</returns>
        private void pNotificacionER(string strDatos)
        {
            try
            {
                DataSet ds = ConvertXMLToDataSet(strDatos);
                _ds = ds;
                string strSujOb = "";

                if (ds != null && ds.Tables != null)
                {
                    foreach (DataRow row in ds.Tables[0].Rows)
                    {
                        if (row["cTipo"].ToString() == "SOB")       // Si el Usuario es Sujeto obligado se almacenara en la variable strSujOb
                        {
                            strSujOb = row["sNombre"].ToString();
                        }
                    }

                    foreach (DataRow row in ds.Tables[0].Rows)
                    {
                        clsNotificacion objMensaje = new clsNotificacion(); // Objeto que almacenara los mensajes
                        System.Net.Mail.MailMessage correo = new System.Net.Mail.MailMessage();
                        correo.From = new System.Net.Mail.MailAddress(ConfigurationManager.AppSettings["AppMail"]); // Correo desde el Cual se enviaran las notificaciones

                        if (_strMailsTo != "")
                        {
                            correo.To.Add(_strMailsTo);
                        }
                        else if (_strMailsTo == "")
                        {
                            correo.To.Add(row["sCorreo"].ToString());
                        }

                        correo.Subject = ConfigurationManager.AppSettings["Subject"] + " - " + "CONCLUSIÓN DE INTEGRACIÓN DE INFORMACIÓN: " + row["nFKDepcia"].ToString();

                        //**********************************//**************************************//**************************************//**********************************//
                        // PARA EL USO DE HTML
                        correo.SubjectEncoding = System.Text.Encoding.UTF8;
                        //
                        correo.Body =

                        "<style>" +
                     "  .namefont { font-size : 14pt; text-align:center;  background-color: #B0C4DE }" +
                     "  .bodyfont { font-size : 12pt;  text-align:justify }" +
                     "  .s4font { font-size : 10pt; text-align:center}" +
                     "  .texto { color:#848484; font-weight: bold }" +
                     "</style>" +
                         "<div  align =" + "center" + ">" + "<b>" + " Notificación" + "</b>" + "</div>" +
                         "<div align =" + "center" + ">Sistema de Entrega - Recepción de la Universidad Veracruzana</div>" +
                         "</br></br>"
                         + "</br>"
                        + "<div class=" + "namefont" + ">Estimado(a)  " + "<b>" + row["sNombre"].ToString() + "</b>" + "</div>"
                             + "</br>"
                             + "</br>" +
                         "<div  class=" + "bodyfont" + ">" +
                             "<font face=" + "arial" + ">"
                              + "<div>CONCLUSIÓN DE INTEGRACIÓN DE INFORMACIÓN: " + " <b>" + row["nFKDepcia"].ToString() + "</b></div></br></br>"

                             + "<table>"
                             + "<tr>"
                             + "<td class=" + "texto" + ">" + "Proceso entrega-recepción: " + "</td>"
                             + "<td>" + row["sDProceso"].ToString() + "</td>"
                             + "</tr>"
                             + "<tr>"
                             + "<td class=" + "texto" + ">" + "Asunto: " + "</td>"
                             + "<td>" + "Integración de la información del proceso entrega-recepción concluida" + "</td>"
                             + "</tr>"
                             + "<tr>"
                             + "<td class=" + "texto" + ">" + "Sujeto Obligado: " + "</td>"
                                     + "<td>" + strSujOb + "</td>"
                             + "</tr>"
                             + "<tr>"
                             + "<td class=" + "texto" + ">" + "Dependencia/Entidad: " + "</td>"
                             + "<td>" + row["nFKDepcia"].ToString() + "</td>"
                             + "</tr>"
                             + "</table>"
                             + "</br>"
                             + "</font>"
                         + "</div>"
                         + "<div class=" + "s4font" + "> </br></br><b> Favor de no enviar correos a esta cuenta, ya que es utilizada por un proceso automatizado y por lo tanto no se revisa</b></div>"
                         ;

                        // USO DE HTML
                        correo.BodyEncoding = System.Text.Encoding.UTF8;
                        correo.IsBodyHtml = true;
                        //
                        this._correos.Add(correo);

                        objMensaje.idProceso = Convert.ToInt32(row["idProceso"].ToString());
                        objMensaje.idUsuDest = Convert.ToInt32(row["idFKUsuario"].ToString());
                        objMensaje.idUsuRem = idUsuRem;         //ID DEL USUARIO SERUV
                        objMensaje.strAsunto = correo.Subject;
                        objMensaje.strMensaje = correo.Body;
                        objMensaje.correoTo = correo.To.ToString();
                        objMensaje.subject = correo.Subject.ToString();
                        this._Mensajes.Add(objMensaje);
                    }
                }
            }
            catch
            {
                _ds.Dispose();
            }
            finally
            {
                _ds.Dispose();
            }
        }
        #endregion

        #region pSolReap()
        /// <summary>
        /// Procedimiento que genera la notificación de solicitud de reapertura de un proceso,  se enviara a todos los participantes de esa dependencia
        /// Autor: Edgar Morales González
        /// </summary>
        /// <param "strDatos">Cadena que contiene la información necesaria para generar las notificaciones</param>
        /// <returns>devuelve una lista de objeto correos y una lista de objeto mensaje</returns>
        public void pSolReap(string strDatos)
        {
            try
            {
                DataSet ds = ConvertXMLToDataSet(strDatos);
                _ds = ds;

                if (ds != null && ds.Tables != null)
                {
                    if (ds.Tables[1].Rows.Count > 0)
                    {
                        foreach (DataRow row in ds.Tables[0].Rows)
                        {
                            foreach (DataRow row2 in ds.Tables[1].Rows)
                            {
                                System.Net.Mail.MailMessage correo = new System.Net.Mail.MailMessage();
                                correo.From = new System.Net.Mail.MailAddress(ConfigurationManager.AppSettings["AppMail"]); // Correo desde el Cual se enviaran las notificaciones

                                if (_strMailsTo != "")
                                {
                                    correo.To.Add(_strMailsTo);
                                }
                                else if (_strMailsTo == "")
                                {
                                    correo.To.Add(row2["sCorreo"].ToString());
                                }

                                correo.Subject = ConfigurationManager.AppSettings["Subject"] + " - " + row["sAsunto"].ToString(); //+ "  " + row["sDProceso"].ToString();
                                //
                                correo.SubjectEncoding = System.Text.Encoding.UTF8;
                                ///
                                correo.Body =
                                    "<style>" +
                                    "  .namefont { font-size : 14pt; text-align:center;  background-color: #B0C4DE }" +
                                     "  .bodyfont { font-size : 12pt;  text-align:justify }" +
                                     "  .s4font { font-size : 10pt; text-align:center}"
                                     +
                                    "</style>"
                                    + "<font face=" + "arial" + ">" +
                                   "<div  align =" + "center" + ">" + "<b>" + " Notificación" + "</b>" + "</div>" +
                                   "<div align =" + "center" + ">Sistema de Entrega - Recepción de la Universidad Veracruzana</div>" +
                                   "</br></br>" +
                                   "<div class=" + "namefont" + ">Estimado(a)  " + "<b>" + row2["sNombre"].ToString() + "</b>" + "</div>"
                                    + "</br>"
                                    + "</br>"

                                    + "<div  class=" + "bodyfont" + ">"
                                    + "Se le informa que el(la) C. " + "<b>" + row["sNombre"].ToString() + "</b>"
                                    + " solicita la reapertura de la Dependencia/Entidad : " + "<b>" + row["sDDepcia"].ToString() + "</b>"
                                    + "</br>" + "</br>"
                                    + "Para el Proceso entrega-recepción: <b>" + row["sDProceso"].ToString() + "</b>"
                                    + "</br>" + "</br>"
                                    + "Con la siguiente justificación: "
                                    + "</br>"
                                    + row["sMensaje"].ToString()
                                    + "</div>"

                                    ;
                                //
                                correo.BodyEncoding = System.Text.Encoding.UTF8;
                                correo.IsBodyHtml = true;

                                this._correos.Add(correo);
                            }
                        }
                    }
                }
            }
            catch
            {
                _ds.Dispose();
            }
            finally
            {
                _ds.Dispose();
            }
        }
        #endregion

        #region pObsBitacora()  
        /// <summary>
        /// Procedimiento que genera las notificación con las observaciones d la bitacora, misma que se enviara al sujeto obligado
        /// Autor: Edgar Morales González
        /// </summary>
        /// <param "strDatos">Cadena que contiene la información necesaria para generar las notificaciones</param>
        /// <returns>devuelve una lista de objeto correos y una lista de objeto mensaje</returns>
        public void pObsBitacora(string strDatos)
        {
            try
            {
                DataSet ds = ConvertXMLToDataSet(strDatos);
                _ds = ds;

                DataTable ds2 = _ds.Tables[1];

                string strCorreo = "";

                if (_ds != null && _ds.Tables != null)
                {
                    if (_ds.Tables[0].Rows.Count > 0 && ds2.Rows.Count > 0)
                    {
                        foreach (DataRow row in _ds.Tables[0].Rows)         // Recorre la tabla y concatena en una variable las observaciones hechas a los anexos
                        {

                            strCorreo += "<table>";
                            strCorreo += "<tr>";
                            strCorreo += "<td class='texto'> Apartado: ";
                            strCorreo += "</td>";
                            strCorreo += "<td>" + row["sApartado"].ToString() + " " + row["sDApartado"].ToString();
                            strCorreo += "</td>";
                            strCorreo += "</tr>";
                            strCorreo += "<tr>";
                            strCorreo += "<td class='texto'> Anexo: ";
                            strCorreo += "</td>";
                            strCorreo += "<td>" + row["sAnexo"].ToString() + " " + row["sDAnexo"].ToString();
                            strCorreo += "</td>";
                            strCorreo += "</tr>";
                            strCorreo += "<tr>";
                            strCorreo += "<td class='texto'> Observaciones: ";
                            strCorreo += "</td>";
                            strCorreo += "<td>";
                            strCorreo += "</td>";
                            strCorreo += "</tr>";
                            strCorreo += "<tr>";
                            strCorreo += "<td colspan='2'>" + row["sObservaciones"].ToString();
                            strCorreo += "</td>";
                            strCorreo += "</tr>";
                            strCorreo += "</table>";

                            strCorreo += "<br>";
                            strCorreo += "<hr align='center' noshade='noshade' size='2' width='92%' />";
                            strCorreo += "<br>";
                        }

                        foreach (DataRow row2 in ds2.Rows)
                        {

                            clsNotificacion objMensaje = new clsNotificacion(); // Objeto que almacenara los mensajes
                            System.Net.Mail.MailMessage correo = new System.Net.Mail.MailMessage();
                            correo.From = new System.Net.Mail.MailAddress(ConfigurationManager.AppSettings["AppMail"].ToString()); // Correo desde el Cual se enviaran las notificaciones

                            if (_strMailsTo != "")
                            {
                                correo.To.Add(_strMailsTo);
                            }
                            else if (_strMailsTo == "")
                            {
                                correo.To.Add(row2["sCorreo"].ToString());
                            }

                            correo.Subject = ConfigurationManager.AppSettings["Subject"] + " - " + "BITÁCORA DE LA DEPENDENCIA: " + row2["sDDepcia"].ToString();

                            /////////////////////////////        ////////////////////////////////////
                            // PARA EL USO DE HTML
                            correo.SubjectEncoding = System.Text.Encoding.UTF8;
                            //
                            correo.Body =

                            "<style>" +
                         "  .namefont { font-size : 14pt; text-align:center;  background-color: #B0C4DE }" +
                         "  .bodyfont { font-size : 12pt;  text-align:justify }" +
                         "  .s4font { font-size : 10pt; text-align:center}" +
                         "  .texto { color:#848484; font-weight: bold }" +
                         "</style>" +
                             "<div  align =" + "center" + ">" + "<b>" + " Notificación" + "</b>" + "</div>" +
                             "<div align =" + "center" + ">Sistema de Entrega - Recepción de la Universidad Veracruzana</div>" +
                             "</br></br>"
                             + "</br>"
                            + "<div class=" + "namefont" + ">Estimado(a)  " + "<b>" + row2["sNombre"].ToString() + "</b>" + "</div>"
                                 + "</br>"
                                 + "</br>" +
                             "<div  class=" + "bodyfont" + ">" +
                                 "<font face=" + "arial" + ">"
                                  + "<div>"
                                    + "Bitácora " + "</br>"
                                    + "Proceso: " + " <b>" + row2["sDProceso"].ToString() + "</b>"
                                    + "<br>"
                                     + "Dependencia: " + " <b>" + row2["sDDepcia"].ToString() + "</b>"
                                  + "</div></br></br>"
                                            + strCorreo
                                        + "</div>"
                                        + "<div class=" + "s4font" + "> </br></br>Atte:<b> " + row2["sNombreSup"].ToString() + "</b></br>" + "Correo: " + row2["sCorreoSup"].ToString() + "</div>"
                                     ;
                            // USO DE HTML
                            correo.BodyEncoding = System.Text.Encoding.UTF8;
                            correo.IsBodyHtml = true;
                            //
                            this._correos.Add(correo);

                            objMensaje.idProceso = Convert.ToInt32(row2["idProceso"].ToString());
                            objMensaje.idUsuDest = Convert.ToInt32(row2["idUsuario"].ToString());
                            objMensaje.idUsuRem = Convert.ToInt32(row2["idSupervisor"].ToString());
                            objMensaje.strAsunto = correo.Subject;
                            objMensaje.strMensaje = correo.Body;
                            objMensaje.correoTo = correo.To.ToString();
                            objMensaje.subject = correo.Subject.ToString();
                            this._Mensajes.Add(objMensaje);

                        }
                    }
                }
            }
            catch
            {
                _ds.Dispose();
            }
            finally
            {
                _ds.Dispose();
            }
        }
        #endregion

        #region pSolProceso()
        /// <summary>
        /// Procedimiento que genera la notificación, cuando se solicita la creación de un nuevo proceso de entrega-recepción
        /// Autor: Edgar Morales González
        /// </summary>
        /// <param "strDatos">Cadena que contiene la información necesaria para generar las notificaciones</param>
        /// <returns>devuelve una lista de objeto correos y una lista de objeto mensaje</returns>
        public void pSolProceso(string strDatos)
        {
            try
            {
                DataSet ds = ConvertXMLToDataSet(strDatos);
                _ds = ds;

                string sFf, sfN; // Variable que almacenara la fechad e separacion del cargo
                string sEnlace = "";
                string sujRep = "";

                if (ds != null && ds.Tables != null)
                {
                    foreach (DataRow row in ds.Tables[0].Rows)
                    {
                        foreach (DataRow row2 in ds.Tables[1].Rows)
                        {
                            clsNotificacion objMensaje = new clsNotificacion(); // Objeto que almacenara los mensajes
                            System.Net.Mail.MailMessage correo = new System.Net.Mail.MailMessage();
                            correo.From = new System.Net.Mail.MailAddress(ConfigurationManager.AppSettings["AppMail"]); // Correo desde el Cual se enviaran las notificaciones

                            if (_strMailsTo != "")
                            {
                                correo.To.Add(_strMailsTo);
                            }
                            else if (_strMailsTo == "")
                            {
                                correo.To.Add(row2["sCorreo"].ToString());
                            }

                            sFf = row["dFSeparacion"].ToString();
                            sfN = DateTime.Parse(sFf).ToString("dd-MM-yyyy");

                            if (ds.Tables[0].Columns.Contains("idUsuEOP"))
                            {
                                if (row["idUsuEOP"].ToString() != null && sEnlace == "")
                                {
                                    sEnlace += "ENLACE PRINCIPAL :" + "<b>" + row["sNombreUsuEOP"].ToString() + "</b>";
                                    sEnlace += "<br>";
                                    sEnlace += "CORREO ELECTRÓNICO: " + "<b>" + row["sCorreoEOP"].ToString() + "</b>";
                                    sEnlace += "<br>";
                                }
                            }
                            if (ds.Tables[0].Columns.Contains("idUsuUSR"))
                            {
                                if (row["idUsuUSR"].ToString() != null && sujRep == "")
                                {
                                    sujRep += "<b>" + row["sNombreUSR"].ToString() + "</b>";
                                    sujRep += "<br>";
                                    sujRep += "CORREO ELECTRÓNICO: ";
                                    sujRep += "<b>" + row["sCorreoUSR"].ToString() + "</b>";
                                }
                            }
                            else if (ds.Tables[0].Columns.Contains("sNombreSR"))
                            {
                                if (sujRep == "")
                                {
                                    sujRep += "<b>" + row["sNombreSR"].ToString() + "</b>";
                                    sujRep += "<br>";

                                    if (ds.Tables[0].Columns.Contains("sCorreoSR"))
                                    {
                                        if (row["sCorreoSR"].ToString() != null)
                                        {
                                            sujRep += "CORREO ELECTRÓNICO: ";
                                            sujRep += "<b>" + row["sCorreoSR"].ToString() + "</b>";
                                        }
                                    }
                                }
                            }

                            correo.Subject = "SOLICITUD DE INTERVENCIÓN EN EL PROCESO ENTREGA-RECEPCIÓN:" + row["sDDepcia"].ToString() + " DEL CARGO O PUESTO " + row["sDPuesto"].ToString();

                            // PARA EL USO DE HTML
                            correo.SubjectEncoding = System.Text.Encoding.UTF8;
                            //
                            correo.Body =

                            "<style>" +
                         "  .namefont { font-size : 14pt; text-align:center;  background-color: #B0C4DE }" +
                         "  .bodyfont { font-size : 12pt;  text-align:justify }" +
                         "  .s4font { font-size : 10pt; text-align:center}" +
                         "  .texto { color:#848484; font-weight: bold }" +
                         "</style>" +
                             "<div  align =" + "center" + ">" + "<b>" + " Notificación" + "</b>" + "</div>" +
                             "<div align =" + "center" + ">Sistema de Entrega - Recepción de la Universidad Veracruzana</div>" +
                             "</br></br>"
                             + "</br>"
                            + "<div class=" + "namefont" + ">" + " Estimado(a): <b>" + row2["sNombre"].ToString() + "</b>" + "</div>"
                                 + "</br>"
                                 + "</br>" +
                             "<div  class=" + "bodyfont" + ">" +
                                 "<font face=" + "arial" + ">"
                                 + "<div>"
                                 + "<p class=" + "texto" + ">"
                                + " SOLICITUD DE INTERVENCIÓN EN EL PROCESO ENTREGA-RECEPCIÓN <b>" + row["sDDepcia"].ToString() + " DEL CARGO O PUESTO " + row["sDPuesto"].ToString() + "</b>"
                                + "</br></br></br> </p>"
                                + "FOLIO DE SOLICITUD: " + "<B>" + row["cveSolProc"].ToString() + "</B><br><br>"
                                  + "EL C. " + " <b>" + row["sNombreUsuSol"].ToString() + "</b>: <b>" + row["sDPuestoSol"].ToString().Trim() + "</b>, DE LA DEPENDENCIA/ENTIDAD: <b>" + row["sDDepciaSol"].ToString()
                                  + "</b>. </BR></BR> SOLICITA LA INTERVENCIÓN EN EL PROCESO DE ENTREGA-RECEPCIÓN DE C. <b>"
                                  + row["sNombreUsuob"].ToString() + "</b>, DEL CARGO O PUESTO: <b>" + row["sDPuesto"].ToString().Trim() + "</b>, DE LA DEPENDENCIA/ENTIDAD: <b>" + row["sDDepcia"].ToString()
                                  + "</b>, CON MOTIVO DE: <B>" + row["sDMotiProc"].ToString()
                                  + "</B> <br><BR>"
                                  + sEnlace
                                  + "<br><br>"
                                  + "SUJETO RECEPTOR: " + sujRep
                                  + "<br><br>"
                                  + "LUGAR: " + "<b>" + row["sLugar"].ToString() + "</b> <br>"
                                  + "<br>"
                                  + " FECHA DE SEPARACIÓN DEL CARGO: " + "<b>" + sfN + "</b><br>"
                                  + "OBSERVACIONES : " + "<b>" + row["sObservaciones"].ToString() + "</b><br>"
                                  + "<br><br>"
                                  + "FUNDAMENTO: "
                                  + "<br>"
                                  + "Con base en el Artículo 21 del Reglamento para el Proceso de Entrega-Recepción y en la Guía relativa, ambos de la Universidad Veracruzana, en mi calidad de Superior "
                                  + "Jerárquico solicito la intervención de la Contraloría General en el proceso de entrega-recepción de todos los recursos, información, documentos y asuntos relacionados "
                                  + "con el empleo, cargo o comisión que se cita."
                                  + "</div></br>"
                                 + "</font>"
                             + "</div>"

                             ;

                            // USO DE HTML
                            correo.BodyEncoding = System.Text.Encoding.UTF8;
                            correo.IsBodyHtml = true;
                            this._correos.Add(correo);

                            objMensaje.idProceso = Convert.ToInt32(0);
                            objMensaje.idUsuDest = Convert.ToInt32(row2["idUsuario"].ToString());
                            objMensaje.idUsuRem = Convert.ToInt32(row["idUsuSol"].ToString());         //ID DEL USUARIO SERUV
                            objMensaje.strAsunto = correo.Subject;
                            objMensaje.strMensaje = correo.Body;
                            objMensaje.correoTo = correo.To.ToString();
                            objMensaje.subject = correo.Subject.ToString();
                            this._Mensajes.Add(objMensaje);
                        }
                    }
                }
            }
            catch
            {
                _ds.Dispose();
            }
            finally
            {
                _ds.Dispose();
            }
        }
        #endregion

        #region pEnvCorreo
        /// <summary>
        /// Procedimiento que genera el correo electronico que se enviara a uno o mas usuarios, sin necesidad de estar asociado a un proceso especifico.
        /// Autor: Edgar Morales González
        /// </summary>
        /// <param "strDatos">Cadena que contiene la información necesaria para generar las notificaciones</param>
        /// <returns>devuelve una lista de objeto correos y una lista de objeto mensaje</returns>
        public void pEnvCorreo(string strDatos)
        {
            try
            {
                DataSet ds = ConvertXMLToDataSet(strDatos);
                _ds = ds;

                DataTable dtMensaje = _ds.Tables[0];
                DataTable dtUsuarios = _ds.Tables[1];

                string sNombreEnv = "";
                string sCorreoEnv = "";

                // Se arma el correo
                if (_ds != null && _ds.Tables != null)
                {
                    if (dtMensaje.Rows.Count > 0 && dtUsuarios.Rows.Count > 0)
                    {
                        foreach (DataRow row in dtMensaje.Rows)
                        {
                            sNombreEnv = row["sNombreE"].ToString();
                            sCorreoEnv = row["sCorreoE"].ToString();
                            strMensaje = row["sRecomendacion"].ToString();
                            strAsunto = row["sAsunto"].ToString();
                            idProceso = 0;    //ID 0 porque el mensaje no esta ligado a un proceso
                            idUsuRem = int.Parse(row["idUsuarioE"].ToString());
                        }

                        foreach (DataRow row2 in dtUsuarios.Rows)
                        {
                            clsNotificacion objMensaje = new clsNotificacion(); // Objeto que almacenara los mensajes
                            System.Net.Mail.MailMessage correo = new System.Net.Mail.MailMessage();
                            correo.From = new System.Net.Mail.MailAddress(ConfigurationManager.AppSettings["AppMail"].ToString()); // Correo desde el Cual se enviaran las notificaciones

                            if (_strMailsTo != "")
                            {
                                correo.To.Add(_strMailsTo);
                            }
                            else if (_strMailsTo == "")
                            {
                                correo.To.Add(row2["sCorreo"].ToString());
                            }

                            correo.Subject = strAsunto;

                            /////////////////////////////        ////////////////////////////////////
                            // PARA EL USO DE HTML
                            correo.SubjectEncoding = System.Text.Encoding.UTF8;
                            //
                            correo.Body =

                            "<style>" +
                         "  .namefont { font-size : 14pt; text-align:center;  background-color: #B0C4DE }" +
                         "  .bodyfont { font-size : 12pt;  text-align:justify }" +
                         "  .s4font { font-size : 10pt; text-align:center}" +
                         "  .texto { color:#848484; font-weight: bold }" +
                         "</style>" +
                             "<div  align =" + "center" + ">" + "<b>" + " Notificación" + "</b>" + "</div>" +
                             "<div align =" + "center" + ">Sistema de Entrega - Recepción de la Universidad Veracruzana</div>" +
                             "</br></br>"
                             + "</br>"
                            + "<div class=" + "namefont" + ">Estimado(a)  " + "<b>" + row2["sNombre"].ToString() + "</b>" + "</div>"
                                 + "</br>"
                                 + "</br>" +
                             "<div  class=" + "bodyfont" + ">" +
                                 "<font face=" + "arial" + ">"
                                  + "<div>"
                                    + strMensaje
                                        + "</div>"
                                        + "<div class=" + "s4font" + "> </br></br>Atte:<b> " + sNombreEnv + "</b></br>" + "Correo: " + sCorreoEnv + "</div>"
                                     ;
                            // USO DE HTML
                            correo.BodyEncoding = System.Text.Encoding.UTF8;
                            correo.IsBodyHtml = true;
                            //
                            this._correos.Add(correo);

                            objMensaje.idProceso = idProceso;
                            objMensaje.idUsuDest = Convert.ToInt32(row2["idUsuario"].ToString());
                            objMensaje.idUsuRem = idUsuRem;
                            objMensaje.strAsunto = correo.Subject;
                            objMensaje.strMensaje = correo.Body;
                            objMensaje.correoTo = correo.To.ToString();
                            objMensaje.subject = correo.Subject.ToString();
                            this._Mensajes.Add(objMensaje);
                        }
                    }
                }
            }
            catch
            {
            }
        }

        #endregion

        private void pComentarios(int idComentario)
        {
            DataSet dsDatos = new DataSet();
            using (this._objDALSQL = new clsDALSQL(false, _strAmbiente))
            {
                using (this._libSQL = new libSQL())
                {
                    _lstParametros.Add(this._libSQL.CrearParametro("@strACCION", "COMENTARIOS"));
                    _lstParametros.Add(this._libSQL.CrearParametro("@idCOMENTARIO", idComentario));
                    this._objDALSQL.ExecQuery_SET("PA_SEL_WNOTIFICACIONES", _lstParametros);
                    dsDatos = this._objDALSQL.Get_dtSet();
                    if (dsDatos.Tables.Count > 0)
                    {
                        DataTable dtComentario = dsDatos.Tables[0];
                        //dtCorreosEnviar = dsDatos.Tables[0];
                        if (dtComentario.Rows.Count > 0)
                        {
                            foreach (DataRow row in dtComentario.Rows)
                            {
                                strAsunto = "SIA - Buzón de comentarios(" + row["Asunto"].ToString() + ")";

                                strMensaje = "<style>" +
                                             "  .namefont { font-size : 14pt; text-align:center;  background-color: #B0C4DE }" +
                                             "  .bodyfont { font-size : 12pt;  text-align:justify }" +
                                             "  .s4font { font-size : 10pt; text-align:center}" +
                                             "  .texto { color:#848484; font-weight: bold }" +
                                             "</style>" +
                                                 "<div  align =" + "center" + ">" + "<b>" + " Notificación" + "</b>" + "</div>" +
                                                 "<div align =" + "center" + ">Sistema de Información Administrativa</div>" +
                                                 "</br></br>" +
                                                "<div  class=" + "bodyfont" + ">" +
                                                 "<font face=" + "arial" + ">" +
                                                    "<div  class=" + "bodyfont" + ">" +
                                                            "<font face=" + "arial" + ">" +
                                                             row["sDComentario"].ToString() +
                                                             "</br>" +
                                                             "</br>" +
                                                  "</br>" +
                                                  "</div>" +
                                                  "</font>" +
                                                  "<div class=" + "s4font" + "> </br></br><b> Favor de no enviar correos a esta cuenta, ya que es utilizada por un proceso automatizado y por lo tanto no se revisa</b></div>";

                                System.Net.Mail.MailMessage correo = new System.Net.Mail.MailMessage();
                                //correo.From = new System.Net.Mail.MailAddress(ConfigurationManager.AppSettings["AppMail"].ToString()); // Correo desde el Cual se enviaran las notificaciones
                                correo.From = new System.Net.Mail.MailAddress("saisuv@uv.mx"); // Correo desde el Cual se enviaran las notificaciones
                                correo.BodyEncoding = System.Text.Encoding.UTF8;
                                correo.IsBodyHtml = true;
                                correo.Subject = strAsunto;
                                correo.Body = strMensaje;
                                //objMensaje.strAsunto = correo.Subject;
                                //objMensaje.strMensaje = correo.Body;
                                //objMensaje.correoTo = correo.To.ToString();
                                //objMensaje.subject = correo.Subject.ToString();
                                //this._Mensajes.Add(objMensaje);

                                if (_strMailsTo != "")
                                {
                                    correo.To.Add(_strMailsTo);
                                }
                                else if (_strMailsTo == "")
                                {
                                    correo.To.Add("emmendez@uv.mx");
                                    correo.To.Add("josmendez@uv.mx");
                                }

                                correo.Subject = strAsunto;
                                this._correos.Add(correo);
                            }
                        }
                    }
                }
            }
        }


        #region public void SendNotificacion()
        /// <summary>
        /// Procedimiento que enviará notificaciones vía correo electrónico según la opción que se paso en el constructor.
        /// Autor: Edgar Morales González
        /// </summary>
        /// <param "strDatos">Cadena que contiene la información necesaria para generar las notificaciones</param>
        /// <returns>devuelve una lista de objeto correos y una lista de objeto mensaje</returns>
        public string SendNotificacion(string strDatos, string opcion, string strAmbiente, string strMailsTo)
        {
            _strAmbiente = strAmbiente;
            _strOpcion = opcion;
            _strMailsTo = strMailsTo;

            GetIdSeruv();
            switch (opcion)     // Manda a llamar a la función que genera el correo de notificación
            {
                case "ANEXO":
                    pNotificacionANEXO(strDatos);
                    break;
                case "ENVIAR_ER":
                    pNotificacionER(strDatos);
                    break;
                case "NUEVO_PROCESO":
                    pNotificacionNuevoProceso(strDatos);
                    break;
                case "MODIFICAR_FEC_PROCESO":
                    pNotificacionModificarFecProceso(strDatos);
                    break;
                case "MODIFICAR_PROCESO_EXT":
                    pNotificacionModificarProcesoExt(strDatos);
                    break;
                case "EXCLU_INCLU_ANEXO":
                    pNotificacion_Inclu_exclu_Anexo(strDatos);
                    break;
                case "CAMB_SUJ_OB":
                    pCambSujOb(strDatos);
                    break;
                case "INC_PART":
                    pIncPart(strDatos);
                    break;
                case "SUJ_RECP":
                    pSujRecp(strDatos);
                    break;
                case "EXC_PART":
                    pExcPart(strDatos);
                    break;
                case "PART_EXT":
                    pPartExt(strDatos);
                    break;
                case "CIERRE_PROC":
                    pCierreProc(strDatos);
                    break;
                case "REAP_PROC":
                    pReapertProc(strDatos);
                    break;
                case "SOL_REAP":
                    pSolReap(strDatos);
                    break;
                case "ENVIARESPUESTA":
                    pEnviaRespuesta(strDatos);
                    break;
                case "OBS_BITACORA":
                    pObsBitacora(strDatos);
                    break;
                case "SOL_PROCESO":
                    pSolProceso(strDatos);
                    break;
                case "ENV_CORREO":
                    pEnvCorreo(strDatos);
                    break;
                case "COMENTARIO":
                    pComentarios(72);
                    break;
            }
            return SendMails();          // Envia los Correos a los destinatarios
                                         // dsDatos.Dispose();
        }
        #endregion

        #region void SendMails()
        /// <summary>
        /// Función que se encarga de enviar los correos a los destinatarios correspondientes
        /// Autor: Edgar Morales González
        /// </summary>
        /// <param "strDatos">Cadena que contiene la información necesaria para generar las notificaciones</param>
        /// <returns>devuelve una lista de objeto correos y una lista de objeto mensaje</returns>
        public string SendMails()
        {
            string strRespuesta = "Hecho";
            clsNotificacion objNotifica = new clsNotificacion();
            objNotifica.MailSplit();            // Obtiene la cuenta desde la cual se enviaran las notificaciones
            if (this._correos != null)
            {
                string strCuerpoMensaje = "";

                foreach (System.Net.Mail.MailMessage correo in this._correos)
                {
                    //System.Net.Mail.SmtpClient smtp = new System.Net.Mail.SmtpClient(ConfigurationManager.AppSettings["SmtpClient"]);// "exuv01.intra.uv.mx"
                    System.Net.Mail.SmtpClient smtp = new System.Net.Mail.SmtpClient();
                    smtp.Timeout = 200000;
                    smtp.UseDefaultCredentials = false;
                    smtp.Credentials = new System.Net.NetworkCredential(objNotifica._strAccount, DES.funDES_FromBase64(ConfigurationManager.AppSettings["AppMailPwd"]));
                    //smtp.Credentials = new System.Net.NetworkCredential("saisuv@uv.mx", "ssv258vmx");
                    smtp.Port = 587;
                    smtp.Host = ConfigurationManager.AppSettings["SmtpClient"];
                    smtp.EnableSsl = true;
                    smtp.DeliveryMethod = System.Net.Mail.SmtpDeliveryMethod.Network;


                    //smtp.Port = 25;

                    //SmtpClient oSmtp = new SmtpClient();
                    //oSmtp.UseDefaultCredentials = false;
                    //oSmtp.Credentials = new NetworkCredential(strUser, strPwd);
                    //oSmtp.Port = 587;
                    //oSmtp.Host = strServ;
                    //oSmtp.EnableSsl = true;
                    //oSmtp.DeliveryMethod = SmtpDeliveryMethod.Network;
                    //oSmtp.Send(oEmail);

                    correoTo = correo.To.ToString();
                    subject = correo.Subject.ToString();
                    message = correo.Body.ToString();

                    if (message != strCuerpoMensaje)
                    {
                        strCuerpoMensaje = message;
                        try
                        {
                            smtp.Send(correo);

                            switch (_strOpcion) // Si la opcion se encuentra en los siguientes casos, manda a función Inserta Mensaje
                            {
                                case "ANEXO":
                                case "ENVIAR_ER":
                                case "NUEVO_PROCESO":
                                case "MODIFICAR_FEC_PROCESO":
                                case "MODIFICAR_PROCESO_EXT":
                                case "EXCLU_INCLU_ANEXO":
                                case "CAMB_SUJ_OB":
                                case "INC_PART":
                                case "SUJ_RECP":
                                case "EXC_PART":
                                case "PART_EXT":
                                case "CIERRE_PROC":
                                case "REAP_PROC":
                                case "OBS_BITACORA":
                                case "SOL_PROCESO":
                                case "ENV_CORREO":
                                    InsertaMensaje(correoTo, subject, message);
                                    break;
                            }
                        }
                        catch (Exception ex)
                        {
                            _log.Registrar(ex.ToString(), "Debug", ex.TargetSite.DeclaringType.Name, ex.TargetSite.ToString());
                            strRespuesta = ex.Message;
                            correo.Dispose();
                        }
                        finally
                        {
                            correo.Dispose();
                        }
                    }
                }
            }
            return strRespuesta;
        }
        #endregion

        #region public void MailSplit()
        /// <summary>
        /// Procedimiento que obtiene el nombre de la cuenta que se usara para el envío de los correos de notificaciones,mediante la funcion split.
        /// Autor: Edgard Morales González
        /// </summary>
        /// <param "strDatos">Cadena que contiene la información necesaria para generar las notificaciones</param>
        /// <returns>devuelve una lista de objeto correos y una lista de objeto mensaje</returns>
        public void MailSplit()
        {
            _strAccount = ConfigurationManager.AppSettings["AppMail"];
            //string value = ConfigurationManager.AppSettings["AppMail"];
            //string[] lines = Regex.Split(value, "@");
            //_strAccount = lines[0];
        }
        #endregion

        #region IDisposable Members

        public void Dispose()
        {
            GC.SuppressFinalize(this);
        }
        #endregion

        #region // Función que convierte los datos  XML a un DataSet
        /// <summary>
        /// Función que convierte los datos  XML a un DataSet
        /// Autor: Edgar Morales González
        /// </summary>
        /// <param "strDatos">Cadena que contiene la información necesaria para generar las notificaciones</param>
        /// <returns>devuelve una lista de objeto correos y una lista de objeto mensaje</returns>
        public DataSet ConvertXMLToDataSet(string xmlData)
        {
            StringReader stream = null;
            XmlTextReader reader = null;
            try
            {
                DataSet xmlDS = new DataSet();
                stream = new StringReader(xmlData);
                // Load the XmlTextReader from the stream
                reader = new XmlTextReader(stream);

                xmlDS.ReadXml(reader);
                return xmlDS;
            }
            catch
            {
                return null;
            }
            finally
            {
                if (reader != null) reader.Close();
            }
        }
        #endregion

        #region InsertaMensaje
        /// <summary>
        /// Función que inserta los mensajes enviados por correo, dentro de la tabla APVMENSAJE
        /// Autor: Edgar Morales González
        /// </summary>
        /// <param "strDatos">Cadena que contiene la información necesaria para generar las notificaciones</param>
        /// <returns>devuelve una lista de objeto correos y una lista de objeto mensaje</returns>
        public void InsertaMensaje(string correoTo, string subject, string message)
        {
            string strCuentaCorreo = "";
            string strRecibe = "";
            string strCuerpoMensaje = "";

            foreach (clsNotificacion objMensaje in this._Mensajes)
            {
                string asunto = objMensaje.strAsunto;
                string mensaje = objMensaje.strMensaje;
                int idusuario = objMensaje.idUsuDest;
                int idusuarior = objMensaje.idUsuRem;
                int idProc = objMensaje.idProceso;
                string correoToSend = objMensaje.correoTo;
                string Sub = objMensaje.subject;

                if (correoTo == objMensaje.correoTo && subject == Sub && mensaje == message)
                {
                    if (strCuentaCorreo != correoTo && strRecibe != subject && strCuerpoMensaje != message)
                    {
                        strCuentaCorreo = correoTo;
                        strRecibe = subject;
                        strCuerpoMensaje = message;

                        using (this._objDALSQL = new clsDALSQL(false, _strAmbiente))
                        {
                            using (this._libSQL = new libSQL())
                            {
                                _lstParametros.Add(this._libSQL.CrearParametro("@strASUNTO", asunto));
                                _lstParametros.Add(this._libSQL.CrearParametro("@strMENSAJE", mensaje));
                                _lstParametros.Add(this._libSQL.CrearParametro("@intIDDESTINATARIO", idusuario));
                                _lstParametros.Add(this._libSQL.CrearParametro("@intIDPROCESO", idProc));
                                _lstParametros.Add(this._libSQL.CrearParametro("@intIDREMITENTE", idusuarior));
                                _lstParametros.Add(this._libSQL.CrearParametro("@strACCION", "NOT_AUTO"));

                                this._objDALSQL.ExecQuery_SET("[PA_IDUH_NOTIFICACIONES]", _lstParametros);
                            }
                        }
                    }
                }
            }
        }
        #endregion

        #region GetIdSeruv()
        /// <summary>
        /// Función que obtiene el ID del usuario seruv
        /// Autor: Edgar Morales González
        /// </summary>
        /// <param "strDatos">Cadena que contiene la información necesaria para generar las notificaciones</param>
        /// <returns>devuelve una lista de objeto correos y una lista de objeto mensaje</returns>
        public bool GetIdSeruv()
        {
            bool blnRespuesta = false;
            using (clsDALSQL objDALSQL = new clsDALSQL(false, _strAmbiente))
            {
                libSQL lSQL = new libSQL();
                _lstParametros.Add(lSQL.CrearParametro("@strACCION", "USUARIO_SERUV"));
                try
                {
                    if (blnRespuesta = objDALSQL.ExecQuery_SET("PA_IDUH_NOTIFICACIONES", _lstParametros))
                    {
                        DataSet dsUsu = new DataSet();
                        dsUsu = objDALSQL.Get_dtSet();

                        if (dsUsu != null && dsUsu.Tables != null)
                        {
                            DataTable drSeruv = dsUsu.Tables[0];

                            foreach (DataRow row in dsUsu.Tables[0].Rows)
                            {
                                _idUsuRem = Convert.ToInt32(row["idUsuario"].ToString());
                            }
                        }
                    }
                }
                catch
                {
                }
            }
            return blnRespuesta;
        }
        #endregion


        #endregion

        #region Getters y Setters
        public int idUsuDest { get { return _idUsuDest; } set { _idUsuDest = value; } }                   //Id del usuario Destinatario
        public int idProceso { get { return _idProceso; } set { _idProceso = value; } }                      //ID del proceso al que hace referencia la notificación
        public string strAsunto { get { return _strAsunto; } set { _strAsunto = value; } }                  //Asunto de la notificación
        public string strMensaje { get { return _strMensaje; } set { _strMensaje = value; } }               //Cuerpo de la notificación
        public int idUsuRem { get { return _idUsuRem; } set { _idUsuRem = value; } }                        //Id del usuario que envía el mensaje 
        public string correoTo { get { return _correoTo; } set { _correoTo = value; } }                      //Indica las direccion de correo a quien se enviara el mensaje
        public string subject { get { return _subject; } set { _subject = value; } }                        //Asunto del mensaje
        public string message { get { return _message; } set { _message = value; } }                         // Cuerpo del mensaje
        public string strAmbiente { get { return _strAmbiente; } set { _strAmbiente = value; } }             // Ambiente desde el cual se enviarán las notificaciones
        public string strMailsTo { get { return _strMailsTo; } set { _strMailsTo = value; } }                 //Llave que indica si las notificaciones se enviarán a los destinatarios correctos o a un correo pretederminado
        #endregion

    }
}