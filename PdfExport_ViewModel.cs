using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using System.Windows.Forms;

namespace MyRevitAddin.ViewModels
{
    // Represents a selectable sheet or element
    public class SelectableElement : INotifyPropertyChanged
    {
        private bool _isSelected;
        public string Name { get; set; }

        public bool IsSelected
        {
            get => _isSelected;
            set
            {
                if (_isSelected != value)
                {
                    _isSelected = value;
                    OnPropertyChanged();
                }
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    // Simple RelayCommand implementation
    public class RelayCommand : ICommand
    {
        private readonly Action _execute;
        private readonly Func<bool> _canExecute;

        public RelayCommand(Action execute, Func<bool> canExecute = null)
        {
            _execute = execute;
            _canExecute = canExecute;
        }

        public event EventHandler CanExecuteChanged;

        public bool CanExecute(object parameter)
        {
            return _canExecute == null || _canExecute();
        }

        public void Execute(object parameter)
        {
            _execute();
        }

        public void RaiseCanExecuteChanged()
        {
            CanExecuteChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    public class DirectorySelectionViewModel : INotifyPropertyChanged
    {
        private string _selectedDirectoryDisplay = "No directory selected";
        private int _selectedItemsCount = 0;

        public string SelectedDirectoryDisplay
        {
            get => _selectedDirectoryDisplay;
            set
            {
                if (_selectedDirectoryDisplay != value)
                {
                    _selectedDirectoryDisplay = value;
                    OnPropertyChanged();
                    OkCommand?.RaiseCanExecuteChanged();
                }
            }
        }

        public int SelectedItemsCount
        {
            get => _selectedItemsCount;
            private set
            {
                if (_selectedItemsCount != value)
                {
                    _selectedItemsCount = value;
                    OnPropertyChanged();
                }
            }
        }

        private ObservableCollection<SelectableElement> _elements = new ObservableCollection<SelectableElement>();
        public ObservableCollection<SelectableElement> Elements
        {
            get => _elements;
            set
            {
                if (_elements != value)
                {
                    if (_elements != null)
                    {
                        _elements.CollectionChanged -= OnElementsCollectionChanged;
                        foreach (var element in _elements)
                        {
                            element.PropertyChanged -= OnElementPropertyChanged;
                        }
                    }

                    _elements = value;

                    if (_elements != null)
                    {
                        _elements.CollectionChanged += OnElementsCollectionChanged;
                        foreach (var element in _elements)
                        {
                            element.PropertyChanged += OnElementPropertyChanged;
                        }
                    }

                    UpdateSelectedItemsCount();
                    OnPropertyChanged();
                }
            }
        }

        public RelayCommand BrowseCommand { get; }
        public RelayCommand OkCommand { get; }
        public RelayCommand CancelCommand { get; }

        public event EventHandler<bool> RequestClose;
        public event PropertyChangedEventHandler PropertyChanged;

        public DirectorySelectionViewModel()
        {
            BrowseCommand = new RelayCommand(BrowseDirectory);
            OkCommand = new RelayCommand(OnOk, CanExecuteOk);
            CancelCommand = new RelayCommand(OnCancel);

            Elements.CollectionChanged += OnElementsCollectionChanged;
        }

        private bool CanExecuteOk()
        {
            return !string.IsNullOrEmpty(_selectedDirectoryDisplay)
                   && _selectedDirectoryDisplay != "No directory selected"
                   && SelectedItemsCount > 0;
        }

        private void OnElementPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(SelectableElement.IsSelected))
            {
                UpdateSelectedItemsCount();
                OkCommand.RaiseCanExecuteChanged();
            }
        }

        private void OnElementsCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.NewItems != null)
            {
                foreach (SelectableElement element in e.NewItems)
                {
                    element.PropertyChanged += OnElementPropertyChanged;
                }
            }

            if (e.OldItems != null)
            {
                foreach (SelectableElement element in e.OldItems)
                {
                    element.PropertyChanged -= OnElementPropertyChanged;
                }
            }

            UpdateSelectedItemsCount();
            OkCommand.RaiseCanExecuteChanged();
        }

        private void UpdateSelectedItemsCount()
        {
            SelectedItemsCount = Elements.Count(e => e.IsSelected);
        }

        private void BrowseDirectory()
        {
            using (var dialog = new FolderBrowserDialog
            {
                Description = "Select Export Directory"
            })
            {
                var result = dialog.ShowDialog();
                if (result == DialogResult.OK)
                {
                    SelectedDirectoryDisplay = dialog.SelectedPath;
                }
            }
        }

        private void OnOk()
        {
            RequestClose?.Invoke(this, true);
        }

        private void OnCancel()
        {
            RequestClose?.Invoke(this, false);
        }

        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}