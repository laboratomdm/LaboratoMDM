using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;

namespace LaboratoMDM.UI.Operator.ViewModels
{
    public partial class ListBoxElementViewModel : ObservableObject
    {
        public string Label { get; }

        public ObservableCollection<string> Items { get; } = new();

        public ListBoxElementViewModel(string label)
        {
            Label = label;
        }

        [RelayCommand]
        private void AddItem()
        {
            Items.Add(string.Empty);
        }

        [RelayCommand]
        private void RemoveItem(string item)
        {
            if (Items.Contains(item))
                Items.Remove(item);
        }
    }
}
