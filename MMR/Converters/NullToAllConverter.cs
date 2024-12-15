using System;
using System.Diagnostics;
using System.Globalization;
using Avalonia.Data;
using Avalonia.Data.Converters;
using MMR.Models.Enums;

namespace MMR.Converters;

public class NullToAllConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        Debug.WriteLine($"NullToAllConverter.Convert called with value: {value}, type: {value?.GetType()}");
        
        // 处理 BindingNotification
        if (value is BindingNotification notification)
        {
            value = notification.Value;
        }
        
        // 特别处理 WorkStatus 枚举
        if (value is WorkStatus workStatus)
        {
            return workStatus.ToString();
        }
        
        // 明确返回字符串 "All"，而不是 null
        if (value == null)
        {
            return "All";
        }
        
        return value.ToString();
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        Debug.WriteLine($"NullToAllConverter.ConvertBack called with value: {value}, targetType: {targetType}");
        
        if (value == null || value.ToString() == "All")
        {
            if (targetType == typeof(WorkStatus?))
            {
                return null;
            }
            return null;
        }
        
        if (targetType.IsEnum)
        {
            return Enum.Parse(targetType, value.ToString());
        }
        
        return value;
    }
}