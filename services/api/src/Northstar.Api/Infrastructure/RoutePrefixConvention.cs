using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ApplicationModels;

namespace Northstar.Api.Infrastructure;

public sealed class RoutePrefixConvention : IApplicationModelConvention
{
    private readonly AttributeRouteModel _routePrefix;

    public RoutePrefixConvention(string routePrefix)
    {
        _routePrefix = new AttributeRouteModel(new RouteAttribute(routePrefix));
    }

    public void Apply(ApplicationModel application)
    {
        foreach (var selector in application.Controllers.SelectMany(controller => controller.Selectors))
        {
            selector.AttributeRouteModel = selector.AttributeRouteModel is null
                ? _routePrefix
                : AttributeRouteModel.CombineAttributeRouteModel(_routePrefix, selector.AttributeRouteModel);
        }
    }
}

