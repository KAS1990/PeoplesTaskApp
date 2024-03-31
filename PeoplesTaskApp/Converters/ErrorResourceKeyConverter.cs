using Avalonia.Data.Converters;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Linq;

namespace PeoplesTaskApp.Converters
{
    public class ErrorResourceKeyConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is not List<object?> list)
                return value;

            var resultList = new List<ValidationResult>();

            // В list находятся ключи ресурсов, заменяем их локализованными строками
            foreach (var item in list.OfType<object?>())
            {
                switch (item)
                {
                    case InvalidCastException:
                        resultList.Add(new ValidationResult(""));
                        break;

                    case Exception ex:
                        resultList.Add(new ValidationResult(ex.Message));
                        break;

                    case ValidationResult result:
                        if (result.ErrorMessage is not null)
                        {
                            // Содержит аргументы, разделённые ;.
                            // Первый - ключ ресурса, остальные - значения, которые нужно подставить в string.Format
                            var parts = result.ErrorMessage.Split(";");
                            var formatString = Langs.Resources.ResourceManager.GetString(parts[0]);
                            if (string.IsNullOrEmpty(formatString))
                                result.ErrorMessage = result.ToString();
                            else if (!result.MemberNames.Any())
                                result.ErrorMessage = formatString;
                            else
                            {
                                // Нужно найти ресурс, который содержит имя поля 
                                var localizedFieldName
                                    = Langs.Resources.ResourceManager.GetString($"Parameter{result.MemberNames.First()}Person");
                                if (!string.IsNullOrEmpty(localizedFieldName))
                                {
                                    result.ErrorMessage
                                        = string.Format(formatString,
                                            [localizedFieldName, .. result.MemberNames.ToArray()[1..], .. parts[1..]]);
                                }
                            }
                        }
                        resultList.Add(result);
                        break;

                    default:
                        resultList.Add(new ValidationResult(null));
                        break;
                }
            }

            return resultList;
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
            throw new NotImplementedException($"{GetType().Name}.{nameof(ConvertBack)} is not implemented");
    }
}
