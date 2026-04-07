namespace ServerEye.Core.Services.Billing;

using System.Collections.Generic;

/// <summary>
/// Helper class for extracting data from Stripe webhook objects.
/// </summary>
public static class StripeWebhookHelper
{
    public static string GetStringProperty(dynamic obj, string propertyName)
    {
        try
        {
            var value = GetProperty(obj, propertyName);
            return value?.ToString() ?? string.Empty;
        }
        catch
        {
            return string.Empty;
        }
    }

    public static long GetLongProperty(dynamic obj, string propertyName)
    {
        try
        {
            var value = GetProperty(obj, propertyName);
            if (value == null)
            {
                return 0;
            }

            return Convert.ToInt64(value);
        }
        catch
        {
            return 0;
        }
    }

    public static Dictionary<string, string>? GetMetadata(dynamic obj)
    {
        try
        {
            var metadata = GetProperty(obj, "Metadata");
            if (metadata is IDictionary<string, string> dict)
            {
                return new Dictionary<string, string>(dict);
            }

            return null;
        }
        catch
        {
            return null;
        }
    }

    public static dynamic? GetProperty(dynamic obj, string propertyName)
    {
        try
        {
            var type = obj.GetType();
            var property = type.GetProperty(propertyName);
            return property?.GetValue(obj);
        }
        catch
        {
            return null;
        }
    }

    public static string? GetFirstPriceId(dynamic subscription)
    {
        try
        {
            var items = GetProperty(subscription, "Items");
            if (items == null)
            {
                return null;
            }

            var data = GetProperty(items, "Data");
            if (data == null)
            {
                return null;
            }

            foreach (var item in data)
            {
                var price = GetProperty(item, "Price");
                if (price != null)
                {
                    var priceId = GetProperty(price, "Id");
                    return priceId?.ToString();
                }
            }

            return null;
        }
        catch
        {
            return null;
        }
    }
}
