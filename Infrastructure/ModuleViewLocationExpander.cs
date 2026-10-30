using Microsoft.AspNetCore.Mvc.Razor;
using System.Collections.Generic;
using System.Linq;

namespace MyCourtBooking.Infrastructure
{
    public class ModuleViewLocationExpander : IViewLocationExpander
    {
        public void PopulateValues(ViewLocationExpanderContext context) { }

        public IEnumerable<string> ExpandViewLocations(ViewLocationExpanderContext context, IEnumerable<string> viewLocations)
        {
            // {1} là Controller Name, {0} là Action/View Name
            var moduleLocations = new[]
            {
                "/Modules/{1}/{0}.cshtml",
                "/Modules/{1}/Views/{0}.cshtml",
                "/Modules/Shared/{0}.cshtml",
                "/Views/Shared/{0}.cshtml"
            };

            return moduleLocations.Concat(viewLocations);
        }
    }
}