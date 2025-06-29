using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Razor.TagHelpers;

namespace ChatbotApi.Web.Filters;

[HtmlTargetElement(Attributes = ControllersAttributeName)]
[HtmlTargetElement(Attributes = ActionsAttributeName)]
[HtmlTargetElement(Attributes = RouteAttributeName)]
[HtmlTargetElement(Attributes = ClassAttributeName)]
public class ActiveRouteTagHelper : TagHelper
{
    private const string ActionsAttributeName = "asp-active-actions";
    private const string ControllersAttributeName = "asp-active-controllers";
    private const string ClassAttributeName = "asp-active-class";
    private const string RouteAttributeName = "asp-active-route";

    [HtmlAttributeName(ControllersAttributeName)]
    public string Controllers { get; set; } = default!;

    [HtmlAttributeName(ActionsAttributeName)]
    public string Actions { get; set; } = default!;

    [HtmlAttributeName(RouteAttributeName)]
    public string Route { get; set; } = default!;

    [HtmlAttributeName(ClassAttributeName)]
    public string Class { get; set; } = "active";

    [HtmlAttributeNotBound] [ViewContext] public ViewContext ViewContext { get; set; } = default!;


    public override Task ProcessAsync(TagHelperContext context, TagHelperOutput output)
    {
        RouteValueDictionary routeValues = ViewContext.RouteData.Values;
        string? currentAction = routeValues["action"]?.ToString();
        string? currentController = routeValues["controller"]?.ToString();

        if (string.IsNullOrEmpty(Actions) && !string.IsNullOrEmpty(currentAction))
        {
            Actions = currentAction;
        }

        if (string.IsNullOrEmpty(Controllers) && !string.IsNullOrEmpty(currentController))
        {
            Controllers = currentController;
        }

        string[] acceptedActions = Actions.Trim().Split(',').Distinct().ToArray();
        string[] acceptedControllers = Controllers.Trim().Split(',').Distinct().ToArray();


        LowerCaseComparer lcComparer = new LowerCaseComparer();

        if (acceptedActions.Contains(currentAction, lcComparer) &&
            acceptedControllers.Contains(currentController, lcComparer))
        {
            SetAttribute(output, "class", Class);
        }

        return base.ProcessAsync(context, output);
    }

    private void SetAttribute(TagHelperOutput output, string attributeName, string value, bool merge = true)
    {
        string v = value;
        if (output.Attributes.TryGetAttribute(attributeName, out TagHelperAttribute attribute))
        {
            if (merge)
            {
                v = $"{attribute.Value} {value}";
            }
        }

        output.Attributes.SetAttribute(attributeName, v);
    }
}

public class LowerCaseComparer : IEqualityComparer<string?>
{
    public bool Equals(string? x, string? y)
    {
        if (x == null || y == null)
        {
            return false;
        }

        return x.ToLowerInvariant().Equals(y.ToLowerInvariant());
    }

    public int GetHashCode(string obj)
    {
        return obj.GetHashCode();
    }
}
