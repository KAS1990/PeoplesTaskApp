using Newtonsoft.Json;
using PeoplesTaskApp.Attributes.DataAnnotations;
using PeoplesTaskApp.Helpers;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using System;
using System.Collections;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Reactive.Disposables;
using System.Reactive.Linq;

namespace PeoplesTaskApp.Models
{
    [JsonObject(MemberSerialization.OptIn)]
    public class Person : ReactiveObjectEx, INotifyDataErrorInfo, ICloneable
    {
        private readonly ErrorChecker<Person> _errorChecker;

        public Guid Id { get; }

        [JsonProperty]
        [Required(ErrorMessage = "ErrorIsRequiredFormatString")]
        [StringWithoutSpaces(ErrorMessage = "ErrorThereIsSpacesInStringFormatString")]
        [Reactive]
        public string Name { get; set; } = "";

        [JsonProperty]
        [Required(ErrorMessage = "ErrorIsRequiredFormatString")]
        [StringWithoutSpaces(ErrorMessage = "ErrorThereIsSpacesInStringFormatString")]
        [Reactive]
        public string Surname { get; set; } = "";

        [JsonProperty]
        [Range(1, 99, ErrorMessage = "ErrorOutOfRangeFormatString;1;100")]
        [Reactive]
        public int Age { get; set; } = 1;

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        [Reactive]
        public string? City { get; set; }

        #region INotifyDataErrorInfo implementation

        public bool HasErrors => _errorChecker.HasErrors;

        public event EventHandler<DataErrorsChangedEventArgs>? ErrorsChanged
        {
            add { _errorChecker.ErrorsChanged += value; }
            remove { _errorChecker.ErrorsChanged -= value; }
        }

        public IEnumerable GetErrors(string? propertyName) => _errorChecker.GetErrors(propertyName);

        #endregion

        public Person() : this(Guid.Empty) { }

        public Person(Guid id)
        {
            Id = id;

            _errorChecker = new ErrorChecker<Person>(this);
            _errorChecker.ValidateParent();

            Changed.ObserveOn(RxApp.MainThreadScheduler)
                .Subscribe(args =>
                {
                    if (args.PropertyName is not null)
                        _errorChecker.ValidateParentProperty(args.PropertyName);
                })
                .DisposeWith(DisposableOnDestroy);
        }

        public Person(Guid id, Person data) : this(id)
        {
            Name = data.Name;
            Surname = data.Surname;
            Age = data.Age;
            City = data.City;
        }

        #region ICloneable implementation

        object ICloneable.Clone() => new Person(Id, this);

        public Person Clone() => ((this as ICloneable).Clone() as Person)!;

        #endregion

        public bool FieldsValuesEqual(Person other) =>
            Name == other.Name && Surname == other.Surname && Age == other.Age && City == other.City;

    }
}
