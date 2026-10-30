using Microsoft.AspNetCore.Mvc.Razor;
using System.Collections.Generic;
using System.Linq;

namespace MyCourtBooking.Modules.Shared
{
    public class ModuleViewLocationExpander : IViewLocationExpander
    {
        public void PopulateValues(ViewLocationExpanderContext context) { }

        public IEnumerable<string> ExpandViewLocations(ViewLocationExpanderContext context, IEnumerable<string> viewLocations)
        {
            var moduleLocations = new[]
            {
                // Quét trực tiếp file trong folder Module (VD: /Modules/Courts/Detail.cshtml)
                "/Modules/{1}/{0}.cshtml",
                "/Modules/{1}/Views/{0}.cshtml",
                "/Modules/{1}/Views/Shared/{0}.cshtml",
                
                // Layout & View dùng chung
                "/Modules/Shared/{0}.cshtml",
                "/Modules/Shared/Views/{0}.cshtml",
                "/Views/{1}/{0}.cshtml",
                "/Views/Shared/{0}.cshtml"
            };

            return moduleLocations.Union(viewLocations);
        }
    }
}