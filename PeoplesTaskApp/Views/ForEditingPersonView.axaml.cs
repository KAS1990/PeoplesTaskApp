using Avalonia.Controls;
using PeoplesTaskApp.Models;
using PeoplesTaskApp.ViewModels;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;

namespace PeoplesTaskApp.Views
{
    public abstract class ForEditingPersonViewBase : CustomView<ForEditingPersonViewModel> { }
    public sealed partial class ForEditingPersonView : ForEditingPersonViewBase
    {
        private readonly static int MinAgeValue
            = int.Parse(typeof(Person).GetProperty(nameof(Person.Age), BindingFlags.Public | BindingFlags.Instance)!
                        .GetCustomAttributes<RangeAttribute>()
                        .OfType<RangeAttribute>()
                        .First()
                        .Minimum
                        .ToString() ?? "");

        public ForEditingPersonView()
        {
            InitializeComponent();
        }

        public void AgeTextBox_TextChanging(object? sender, TextChangingEventArgs e)
        {
            var formattedText = Regex.Replace(AgeTextBox.Text ?? "", "[^0-9]", "");

            AgeTextBox.Text
                = formattedText == "" || !int.TryParse(formattedText, out var _)
                    ? MinAgeValue.ToString() // Заменяем всегда на минимально возможное значение, чтобы не было ошибок
                    : formattedText;
        }
    }
}
