using Microsoft.AspNetCore.Mvc.Razor;

namespace FunPayBot.src.Infrastructure
{
    public class CustomViewLocationExpander : IViewLocationExpander
    {
        public IEnumerable<string> ExpandViewLocations(ViewLocationExpanderContext context,
            IEnumerable<string> viewLocations)
        {
            // Добавляем поиск в Pages
            return new[] {
            "~/Pages/Features/{0}.cshtml",
            "~/Pages/{1}/{0}.cshtml",
            "~/Pages/Shared/{0}.cshtml"
        }.Concat(viewLocations);
        }

        public void PopulateValues(ViewLocationExpanderContext context) { }
    }
}
