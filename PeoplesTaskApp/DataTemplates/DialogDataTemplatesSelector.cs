using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Metadata;
using System;
using System.Collections.Generic;

namespace PeoplesTaskApp.DataTemplates
{
    public readonly record struct DialogContentInfo(string TemplateKey, object Content) { }

    public class DialogDataTemplatesSelector : IDataTemplate
    {
        [Content] public Dictionary<string, IDataTemplate> AvailableTemplates { get; } = [];

        public Control? Build(object? param)
        {
            if (param is not DialogContentInfo info)
                throw new ArgumentNullException(nameof(param));
            
            return AvailableTemplates[info.TemplateKey].Build(param);
        }

        public bool Match(object? data) =>
            data is DialogContentInfo info
                && !string.IsNullOrEmpty(info.TemplateKey)
                && AvailableTemplates.ContainsKey(info.TemplateKey);
    }
}
