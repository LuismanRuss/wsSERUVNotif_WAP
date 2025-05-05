using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.Text;

namespace wsSERUVNotif_WAP
{
    // NOTE: You can use the "Rename" command on the "Refactor" menu to change the interface name "InotifService1" in both code and config file together.
    [ServiceContract]
    public interface InotifService
    {
        [OperationContract]
        string pCreaNotif(string strDatos, string opcion, string KeyWsSeruv, string strMailsTo);
    }
}
