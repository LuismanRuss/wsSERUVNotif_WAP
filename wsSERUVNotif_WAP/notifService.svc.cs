using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.Text;
using System.Text.RegularExpressions;
using System.Web.Services;

namespace wsSERUVNotif_WAP
{
    // NOTE: You can use the "Rename" command on the "Refactor" menu to change the class name "notifService1" in code, svc and config file together.
    // NOTE: In order to launch WCF Test Client for testing this service, please select notifService1.svc or notifService1.svc.cs at the Solution Explorer and start debugging.
    public class notifService1 : InotifService
    {
        public void DoWork()
        {
        }

        public delegate string GeneraNotif(string datos, string op, string KeyWsSeruv, string strMailsTo);

        public class MyNot
        {
            public object EstadoPrevio;
            public GeneraNotif dlg;
        }


        #region Metodo para Notificaciones
        public string pCreaNotif(string strDatos, string opcion, string KeyWsSeruv, string strMailsTo)
        {
            string value = KeyWsSeruv;
            string[] lines = Regex.Split(value, "@");
            string strAmbiente = lines[0].ToString();
            string sKey = lines[1].ToString();

            if (ConfigurationManager.AppSettings["key" + strAmbiente].ToString() != sKey)
            {
                return "";
            }
            else
            {
                clsNotificacion objNotif = new clsNotificacion();
                //objNotif.strAmbiente = strAmbiente;
                string strCadena = string.Empty;
                try
                {
                    strCadena = "Hecho";
                    strCadena = objNotif.SendNotificacion(strDatos, opcion, strAmbiente, strMailsTo);
                }
                catch
                {
                    strCadena = "Error";
                }
                return strCadena;
            }
            //}
        }

        [WebMethod]
        public IAsyncResult BeginpCreaNotif(string datos, string op, string KeyWsSeruv, string strMailsTo, AsyncCallback cb, object s)
        {
            GeneraNotif dlg = new GeneraNotif(pCreaNotif);
            MyNot ms = new MyNot();
            ms.dlg = dlg;
            ms.EstadoPrevio = s;

            return dlg.BeginInvoke(datos, op, KeyWsSeruv, strMailsTo, cb, ms);
        }


        [WebMethod]
        public string EndpCreaNotif(IAsyncResult call)
        {
            MyNot ms = (MyNot)call.AsyncState;
            return ms.dlg.EndInvoke(call);
        }
        #endregion
    }
}
