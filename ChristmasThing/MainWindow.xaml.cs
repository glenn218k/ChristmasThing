using System.Windows;

namespace ChristmasThing
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private void ListBox_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (DataContext is MainWindowViewModel vm)
            {
                foreach (var item in e.AddedItems)
                {
                    if (item is ParticipantViewModel pvm)
                    {
                        vm.SelectedParticipants.Add(pvm);
                    }
                }

                foreach (var item in e.RemovedItems)
                {
                    if (item is ParticipantViewModel pvm)
                    {
                        vm.SelectedParticipants.Remove(pvm);
                    }
                }
            }
        }
    }
}