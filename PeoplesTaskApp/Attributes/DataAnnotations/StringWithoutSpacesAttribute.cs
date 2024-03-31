using System.ComponentModel.DataAnnotations;

namespace PeoplesTaskApp.Attributes.DataAnnotations
{
    internal class StringWithoutSpacesAttribute : ValidationAttribute
    {
        public override bool IsValid(object? value) => value is string text && !text.Contains(' ');
    }
}
