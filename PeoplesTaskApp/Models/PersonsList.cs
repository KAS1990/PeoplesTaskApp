using DynamicData;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Disposables;

namespace PeoplesTaskApp.Models
{
    public class PersonsList : ReactiveObjectEx
    {
        private readonly SourceCache<Person, Guid> _personsSource = new(p => p.Id);
        public IObservableCache<Person, Guid> Persons => _personsSource.AsObservableCache();

        public PersonsList()
        {
            Disposable.Create(_personsSource.Clear).DisposeWith(DisposableOnDestroy);
        }

        public void Load(IEnumerable<Person> persons)
        {
            if (persons.GroupBy(p => p.Id).Any(group => group.Count() > 1))
                throw new ArgumentException("All pearson ids must be unique", nameof(persons));

            _personsSource.Edit(updator => updator.Load(persons));
        }

        public void AddPerson(Person person)
        {
            if (person.Id == Guid.Empty)
            {   // Генерируем guid
                person = new Person(Guid.NewGuid(), person);
            }

            _personsSource.Edit(updator =>
            {
                if (updator.Lookup(person.Id).HasValue)
                    throw new ArgumentException($"This Id {person.Id} is existed", nameof(person));

                updator.AddOrUpdate(person);
            });
        }

        public void RemovePersons(IEnumerable<Guid> personIds) => _personsSource.RemoveKeys(personIds);

        public void UpdatePerson(Person person)
        {
            _personsSource.Edit(updator =>
            {
                if (!updator.Lookup(person.Id).HasValue)
                    throw new ArgumentException($"This Id {person.Id} is not found", nameof(person));

                updator.AddOrUpdate(person);
            });
        }
    }
}
