using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Threading;

namespace RevoltUltimate.API.Notification
{
    public class NotificationViewModel : INotifyPropertyChanged
    {
        private ObservableCollection<NotificationItem> _notifications = new ObservableCollection<NotificationItem>();
        public ObservableCollection<NotificationItem> Notifications
        {
            get => _notifications;
            set { _notifications = value; OnPropertyChanged(); OnPropertyChanged(nameof(IsBusy)); }
        }

        public bool IsBusy => Notifications.Any(n => n.Status == NotificationStatus.InProgress);

        public NotificationViewModel()
        {
            Notifications.CollectionChanged += (s, e) => OnPropertyChanged(nameof(IsBusy));
        }

        public void AddTask(string taskName)
        {
            var existingTask = Notifications.FirstOrDefault(n => n.TaskName == taskName && n.Status == NotificationStatus.InProgress);
            if (existingTask == null)
            {
                Dispatcher.CurrentDispatcher.Invoke(() =>
                {
                    Notifications.Insert(0, new NotificationItem { TaskName = taskName, Status = NotificationStatus.InProgress, Timestamp = DateTime.Now });
                });
                OnPropertyChanged(nameof(IsBusy));
            }
        }

        public void UpdateTaskStatus(string taskName, NotificationStatus status, string errorMessage = null)
        {
            Dispatcher.CurrentDispatcher.Invoke(() =>
            {
                var task = Notifications.FirstOrDefault(n =>
                    n.TaskName == taskName && n.Status == NotificationStatus.InProgress);
                if (task != null)
                {
                    task.Status = status;
                    task.ErrorMessage = errorMessage;
                    task.Timestamp = DateTime.Now;
                }
                else
                {
                    Notifications.Insert(0,
                        new NotificationItem
                        {
                            TaskName = taskName,
                            Status = status,
                            ErrorMessage = errorMessage,
                            Timestamp = DateTime.Now
                        });
                }
            });
            OnPropertyChanged(nameof(IsBusy));
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
