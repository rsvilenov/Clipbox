using Clipbox.Infrastructure.UI.Extensibility;
using System;
using System.Windows;

namespace Clipbox.Views.Settings.BuiltInExtensions.UIParts
{
    /// <summary>
    /// Interaction logic for PreviewGenerationTimeoutSettingUIPart.xaml
    /// </summary>
    public partial class MaxPreviewTasksSettingUIPart : SettingUIPart<int>
    {
        /// <summary>
        /// This value, multiplied by the number of logical processors,
        /// gives the max number of threads that the system can run
        /// without much trobble. However, this is entirely hypotetical
        /// and does not take into accout the current system state (load, etc...).
        /// </summary>
        private const int MAX_SYSTEM_THREADS_CORE_SCALE_FACTOR = 3;

        public MaxPreviewTasksSettingUIPart()
        {
            InitializeComponent();

            SystemMaxSymultaneousThreads = Environment.ProcessorCount * MAX_SYSTEM_THREADS_CORE_SCALE_FACTOR;

            this.mainGrid.DataContext = this;
        }

        public int MaxPreviewTasks
        {
            get { return (int)GetValue(MaxPreviewTasksProperty); }
            set { SetValue(MaxPreviewTasksProperty, value); }
        }

        public static readonly DependencyProperty MaxPreviewTasksProperty =
            DependencyProperty.Register(nameof(MaxPreviewTasks), typeof(int), typeof(MaxPreviewTasksSettingUIPart), new PropertyMetadata(5, MaxPreviewTasks_PropertyChanged));

        private static void MaxPreviewTasks_PropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var self = (MaxPreviewTasksSettingUIPart)d;
            self.NotifySettingModified();
        }

        /// <summary>
        /// The maximum number of threads the user can specify.
        /// </summary>
        public int SystemMaxSymultaneousThreads
        {
            get { return (int)GetValue(SystemMaxSymultaneousThreadsProperty); }
            set { SetValue(SystemMaxSymultaneousThreadsProperty, value); }
        }

        public static readonly DependencyProperty SystemMaxSymultaneousThreadsProperty =
            DependencyProperty.Register(nameof(SystemMaxSymultaneousThreads), typeof(int), typeof(MaxPreviewTasksSettingUIPart), new PropertyMetadata(0));


        public override int GetSettingValue()
        {
            return MaxPreviewTasks;
        }

        public override void SetSettingValue(int value)
        {
            MaxPreviewTasks = value;
        }
    }
}
