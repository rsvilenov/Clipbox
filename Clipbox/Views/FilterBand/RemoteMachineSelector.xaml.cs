using Clipbox.ExtendedClipboard.Interfaces;
using Clipbox.Infrastructure.UI;
using Clipbox.Views.FilterBand.EventArgsClasses;
using Clipbox.Win32;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace Clipbox.Views.FilterBand
{
    /// <summary>
    /// Interaction logic for RemoteMachineSelector.xaml
    /// </summary>
    public partial class RemoteMachineSelector : FilterDialogBase
    {
        public event EventHandler<SourceMachineEventArgs> SourceMachinesSelected;

        public ICommand AddMachine { get; set; }
        public ICommand RemoveMachine { get; set; }

        public RemoteMachineSelector(IClipboardItemsDataProvider clipboardItemsDataProvider)
            : base(clipboardItemsDataProvider)
        {
            InitializeComponent();
            this.mainBorder.DataContext = this;
            
            MachineInfoCollection = new AsyncObservableCollection<SourceMachineInfo>();
            SelectedMachines = new AsyncObservableCollection<SourceMachineInfo>();

            AddMachine = new DelegateCommand<SourceMachineInfo>(AddMachineHandler);
            RemoveMachine = new DelegateCommand<SourceMachineInfo>(RemoveMachineHandler);
        }

        private List<SourceMachineInfo> _allMachines;
        

        private AsyncObservableCollection<SourceMachineInfo> MachineInfoCollection
        {
            get { return (AsyncObservableCollection<SourceMachineInfo>)GetValue(MachineInfoCollectionProperty); }
            set { SetValue(MachineInfoCollectionProperty, value); }
        }

        private static readonly DependencyProperty MachineInfoCollectionProperty =
            DependencyProperty.Register(nameof(MachineInfoCollection), typeof(AsyncObservableCollection<SourceMachineInfo>), typeof(RemoteMachineSelector), new PropertyMetadata(null));
        

        public AsyncObservableCollection<SourceMachineInfo> SelectedMachines
        {
            get { return (AsyncObservableCollection<SourceMachineInfo>)GetValue(SelectedMachinesProperty); }
            set { SetValue(SelectedMachinesProperty, value); }
        }
        
        public static readonly DependencyProperty SelectedMachinesProperty =
            DependencyProperty.Register(nameof(SelectedMachines), typeof(AsyncObservableCollection<SourceMachineInfo>), typeof(RemoteMachineSelector), new PropertyMetadata(null));

        
        private void RemoveMachineHandler(SourceMachineInfo obj)
        {
            MachineInfoCollection.Add(obj);
            SelectedMachines.Remove(obj);
        }

        private void AddMachineHandler(SourceMachineInfo obj)
        {
            SelectedMachines.Add(obj);
            MachineInfoCollection.Remove(obj);
        }

        protected override void LoadData()
        {
            string localMachineUniqueId = MachineUtils.GetMachineGuid();
            _allMachines = ClipboardItemsDataProvider
                .GetAllSourceMachinesAndUsernames()
#if !DEBUG
                // filter out the local username@machineUniqueId
                .Where(dic => !(dic.Key.MachineUniqueId == localMachineUniqueId && dic.Value == Environment.UserName))
#endif
                .Select(dic => new SourceMachineInfo
            {
                    MachineName = dic.Key.MachineName,
                    MachineType = dic.Key.MachineType,
                    MachineUniqueId = dic.Key.MachineUniqueId,
                    UserName = dic.Value
            })
            .ToList();
        }
        
        protected override void OnLoadDataComplete()
        {
            MachineInfoCollection = new AsyncObservableCollection<SourceMachineInfo>(_allMachines);
        }

        protected override void OnOkButton()
        {
            SourceMachinesSelected.Invoke(this, new SourceMachineEventArgs(SelectedMachines));
        }

        protected override void OnCancelButton()
        {
            SourceMachinesSelected.Invoke(this, new SourceMachineEventArgs(Enumerable.Empty<SourceMachineInfo>()));
        }

        protected override bool ShouldOkButtonBeEnabled()
        {
            return SelectedMachines.Any();
        }

        private void SearchTextBox_SearchRequested(object sender, RoutedEventArgs e)
        {
            MachineInfoCollection.Clear();
            IsLoading = true;
            string searchText = searchTextBox.Text;
            List<SourceMachineInfo> selectedApps = SelectedMachines.ToList();

            Task.Run(() =>
            {
                IList<SourceMachineInfo> filteredMachineList = _allMachines
                .Where(m => m.MachineName.IndexOf(searchText, StringComparison.OrdinalIgnoreCase) > -1
                    || m.MachineUniqueId.IndexOf(searchText, StringComparison.OrdinalIgnoreCase) > -1)
                .Where(m => !selectedApps.Contains(m))
                .ToList();

                Dispatcher
                .BeginInvoke(DispatcherPriority.Background, new Action(() => MachineInfoCollection.AddRange(filteredMachineList)))
                .Task.ContinueWith((t) => Dispatcher.Invoke(() => IsLoading = false));
            });
        }
    }
}
