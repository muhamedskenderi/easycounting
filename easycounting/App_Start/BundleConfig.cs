using System.Web;
using System.Web.Optimization;

namespace easycounting
{
    public class BundleConfig
    {
        // For more information on bundling, visit https://go.microsoft.com/fwlink/?LinkId=301862
        public static void RegisterBundles(BundleCollection bundles)
        {
            bundles.Add(new ScriptBundle("~/bundles/jquery").Include(
                        "~/Scripts/jquery-{version}.js"));

            bundles.Add(new ScriptBundle("~/bundles/jqueryval").Include(
                        "~/Scripts/jquery.validate*"));

            // Use the development version of Modernizr to develop with and learn from. Then, when you're
            // ready for production, use the build tool at https://modernizr.com to pick only the tests you need.
           

            bundles.Add(new StyleBundle("~/assets/css").Include(
                      "~/assets/css/style.css"));



            bundles.Add(new StyleBundle("~/assets/fontawesome").Include(
                     "~/assets/plugins/fontawesome/css/fontawesome.min.css", "~/assets/fontawesome5.15.2-web/css/all.css", "~/assets/plugins/fontawesome/css/all.min.css"));
           
            bundles.Add(new StyleBundle("~/assets/bootstrap").Include("~/assets/css/bootstrap.min.css"));


            bundles.Add(new StyleBundle("~/assets/plugins/apexchart").Include(
                "~/assets/plugins/apexchart/apexcharts.min.js",
                 "~/assets/plugins/apexchart/chart-data.js"
                ));

            bundles.Add(new StyleBundle("~/assets/plugins/datatables").Include(
                 "~/assets/plugins/datatables/datatables.min.css",
                   "~/assets/plugins/datatables/apexcharts.min.js",
                   "~/assets/plugins/datatables/jquery.dataTables.min.js"
                   ));

            bundles.Add(new StyleBundle("~/assets/plugins/select2").Include(
           "~/assets/plugins/select2/css/select2.min.css",
           "~/assets/plugins/select2/js/select2.min.js"
           ));




        }
    }
}
