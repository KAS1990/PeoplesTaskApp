using Avalonia.Controls;
using PeoplesTaskApp.ViewModels;
using System;

namespace PeoplesTaskApp.Views
{
    public partial class ModalDialogWindow : Window
    {
        public ModalDialogWindow()
        {
            InitializeComponent();
        }

        protected override void OnDataContextChanged(EventArgs e)
        {
            if (DataContext is ViewModelBase viewModel)
                MainViewModelViewHost.ViewModel = viewModel;
        }
    }
}
