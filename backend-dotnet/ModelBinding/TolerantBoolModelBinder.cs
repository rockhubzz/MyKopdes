using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace MyKopdes.ModelBinding;

/// <summary>
/// Query/form model binder for booleans that accepts "1"/"0" in addition to
/// "true"/"false". The Laravel API accepted 1/0 and the frontend sends
/// <c>?low_stock=1</c>-style flags; ASP.NET Core's default binder only parses
/// true/false, which made those checkboxes return 400 "not valid".
/// </summary>
public class TolerantBoolModelBinder : IModelBinder
{
    public Task BindModelAsync(ModelBindingContext context)
    {
        var raw = context.ValueProvider.GetValue(context.ModelName);
        var value = raw.FirstValue?.Trim().ToLowerInvariant();
        if (string.IsNullOrEmpty(value))
            return Task.CompletedTask;

        bool result;
        switch (value)
        {
            case "1":
            case "true":
            case "yes":
            case "on":
                result = true;
                break;
            case "0":
            case "false":
            case "no":
                result = false;
                break;
            default:
                context.ModelState.TryAddModelError(
                    context.ModelName, $"The value '{value}' is not valid.");
                return Task.CompletedTask;
        }

        context.Result = context.ModelType == typeof(bool?)
            ? ModelBindingResult.Success((bool?)result)
            : ModelBindingResult.Success(result);
        return Task.CompletedTask;
    }
}

public class TolerantBoolModelBinderProvider : IModelBinderProvider
{
    public IModelBinder? GetBinder(ModelBinderProviderContext context)
    {
        var source = context.Metadata.BindingSource;
        // Query/form/route values only — JSON body booleans keep the default
        // System.Text.Json binder.
        if (source == null ||
            (source != BindingSource.Query && source != BindingSource.Form && source != BindingSource.Path))
            return null;

        var modelType = context.Metadata.ModelType;
        if (modelType == typeof(bool) || modelType == typeof(bool?))
            return new TolerantBoolModelBinder();
        return null;
    }
}
