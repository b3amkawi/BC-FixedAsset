using System;
using System.Web;

namespace BC.FixedAsset.Web
{
    public class Global : HttpApplication
    {
        protected void Application_Start(object sender, EventArgs e) { }
        protected void Application_Error(object sender, EventArgs e)
        {
            var exception = Server.GetLastError();
            System.Diagnostics.Trace.TraceError(exception == null ? "Unhandled application error." : exception.ToString());
        }
    }
}
