// Copyright (c) PNC Financial Services. All rights reserved.


using System.Reflection;

namespace Dse.Core;

public static class Utils
{
    extension(object? obj)
    {
        public Dictionary<string, object> ConvertToDictionary() => obj?.GetType()
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .ToDictionary(
                prop => prop.Name,
                prop => prop.GetValue(obj, index: null) ?? new { }
            ) ?? [];
    }
}
