using System.Web;
using System.Web.Mvc;

namespace Demo.AwsS3Minio.Web
{
    public class FilterConfig
    {
        public static void RegisterGlobalFilters(GlobalFilterCollection filters)
        {
            filters.Add(new HandleErrorAttribute());
        }
    }
}
