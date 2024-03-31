using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Reflection;

namespace PeoplesTaskApp.Helpers
{
    /// <summary>
    /// Реализует интерфейс INotifyDataErrorInfo и выполняет проверку значений. Поддерживает многопоточность.
    /// Этот функционал вынесен в отдельный класс, чтобы можно было его применять в разных классах, если это будет нужно
    /// Можно было вынести проверку во ViewModel, но я решил оставить её в моделях,
    /// чтобы проверки можно было теоретически тестировать вместе с моделями, а ViewModel не тестировать,
    /// т.к. там будет максимально простой функционал
    /// </summary>
    internal class ErrorChecker<TParent>(TParent parent) : INotifyDataErrorInfo
        where TParent : notnull
    {
        private readonly TParent _parent = parent;

        private readonly IReadOnlyDictionary<string, PropertyInfo> _propertyInfosToCheck
            = parent.GetType()
                .GetProperties(BindingFlags.Instance | BindingFlags.Public)
                .Where(pi => pi.GetCustomAttributes(false).OfType<ValidationAttribute>().Any())
                .ToDictionary(pi => pi.Name);

        private readonly ConcurrentDictionary<string, (object locker, List<ValidationResult> errorResourceKeys)> _errors = new();

        #region INotifyDataErrorInfo implementation

        public bool HasErrors => !_errors.IsEmpty;

        public event EventHandler<DataErrorsChangedEventArgs>? ErrorsChanged;

        public IEnumerable GetErrors(string? propertyName)
        {
            // ToImmutableList Создаёт копию, чтобы не падать в foreach, если данные пишутся/читаются из разных потоков
            if (string.IsNullOrEmpty(propertyName))
            {
                return _errors.Values
                    .SelectMany(info =>
                    {
                        lock (info.locker)
                            return info.errorResourceKeys.ToImmutableList();
                    })
                    .ToList();
            }
            else if (_errors.TryGetValue(propertyName!, out var validationResultInfos))
            {
                lock (validationResultInfos.locker)
                    return validationResultInfos.errorResourceKeys.ToImmutableList();
            }
            else
                return Array.Empty<ValidationResult>();
        }

        void RaiseErrorChangedEvent(string? propertyName) => ErrorsChanged?.Invoke(_parent, new DataErrorsChangedEventArgs(propertyName));

        #endregion

        public void ValidateParent()
        {
            var results = new List<ValidationResult>(1);
            var ok = Validator.TryValidateObject(_parent, new ValidationContext(_parent, null, null), results);

            _errors.Clear();
            if (!ok)
            {
                var grouppedResults
                    = results.SelectMany(validationResult =>
                            validationResult.MemberNames
                                .Select(propertyName => new ValidationResult(validationResult.ErrorMessage, [propertyName])))
                        .GroupBy(validationResult => validationResult.MemberNames.First())
                        .ToDictionary(group => group.Key, group => group.ToList());
                foreach (var pair in grouppedResults)
                {
                    if (_propertyInfosToCheck.ContainsKey(pair.Key))
                    {
                        _errors.AddOrUpdate(pair.Key,
                            _ => (new object(), pair.Value),  // Элемента нет => создаём его. Эта функция может быть вызвана несколько раз
                            (propertyName, current) => // Элемент есть => проверяем изменения. Эта функция может быть вызвана несколько раз
                            {
                                UpdateErrorsForProperty(current, pair.Value, out _);
                                return current;
                            });
                    }
                }

                foreach (var propertyName in _errors.Keys)
                    RaiseErrorChangedEvent(propertyName);
            }
        }

        public void ValidateParentProperty(string propertyName)
        {
            if (_propertyInfosToCheck.TryGetValue(propertyName, out var pi))
            {
                var propertyValue = pi.GetValue(_parent, null);

                var results = new List<ValidationResult>();
                var ok
                    = Validator.TryValidateProperty(propertyValue,
                        new ValidationContext(_parent, null, null)
                        {
                            MemberName = propertyName
                        },
                        results);

                if (ok)
                {   // Ошибок нет => удаляем элемент из словаря
                    if (_errors.TryRemove(propertyName, out _))
                        RaiseErrorChangedEvent(propertyName);
                }
                else
                {
                    var areErrorsChanged = true;
                    _errors.AddOrUpdate(propertyName,
                        _ => // Элемента нет => создаём его. Эта функция может быть вызвана несколько раз
                        {
                            areErrorsChanged = true;
                            return (new object(), results!);
                        },
                        (_, current) => // Элемент есть => проверяем изменения. Эта функция может быть вызвана несколько раз
                        {
                            UpdateErrorsForProperty(current, results!, out areErrorsChanged);
                            return current;
                        });

                    if (areErrorsChanged)
                        RaiseErrorChangedEvent(propertyName);
                }
            }
        }

        private static void UpdateErrorsForProperty((object locker, List<ValidationResult> errorResourceKeys) currentValue,
            List<ValidationResult> validationResults,
            out bool areErrorsChanged)
        {
            areErrorsChanged = false;
            lock (currentValue.locker)
            {
                areErrorsChanged = currentValue.errorResourceKeys.Count != validationResults.Count;
                if (!areErrorsChanged)
                {
                    for (int i = 0; i < validationResults.Count; i++)
                    {
                        if (currentValue.errorResourceKeys[i].ErrorMessage != validationResults[i].ErrorMessage)
                        {
                            areErrorsChanged = true;
                            break;
                        }
                    }
                }

                if (areErrorsChanged)
                {   // validationResultInfos - структура, поэтому присваивание работать не будет
                    currentValue.errorResourceKeys.Clear();
                    currentValue.errorResourceKeys.AddRange(validationResults);
                }
            }
        }
    }
}
