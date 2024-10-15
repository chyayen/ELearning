using System.Web;
using System.Web.Optimization;

namespace ELearning
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
            bundles.Add(new ScriptBundle("~/bundles/modernizr").Include(
                        "~/Scripts/modernizr-*"));

            bundles.Add(new Bundle("~/bundles/bootstrap").Include(
                      "~/Scripts/bootstrap.js",
                      "~/Scripts/bootstrap.bundle.js"));

            bundles.Add(new Bundle("~/bundles/web_template_vendor").Include(
                      "~/Content/vendor/aos/aos.js",
                      "~/Content/vendor/glightbox/js/glightbox.min.js",
                      "~/Content/vendor/purecounter/purecounter_vanilla.js",
                      "~/Content/vendor/vendor/swiper/swiper-bundle.min.js"));

            bundles.Add(new Bundle("~/bundles/web_template").Include(
                      "~/Content/web_template/js/main.js"));



            bundles.Add(new StyleBundle("~/Content/css").Include(
                      "~/Content/bootstrap.css",
                      "~/web_template/vendor/bootstrap-icons/bootstrap-icons.css",
                      "~/Content/site.css"));

            bundles.Add(new StyleBundle("~/Content/web_template_vendor").Include(
                      "~/Content/web_template/vendor/aos/aos.css",
                      "~/Content/web_template/vendor/glightbox/css/glightbox.min.css",
                      "~/Content/web_template/vendor/swiper/swiper-bundle.min.css"));

            bundles.Add(new StyleBundle("~/Content/web_template").Include(
                      "~/Content/web_template/css/main.css"));
        }
    }
}
