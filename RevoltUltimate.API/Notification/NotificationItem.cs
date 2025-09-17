using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace RevoltUltimate.API.Notification
{
    public enum NotificationStatus
    {
        InProgress,
        Success,
        Failed,
    }
    public class NotificationItem : INotifyPropertyChanged
    {
        private string _taskName;
        public string TaskName
        {
            get => _taskName;
            set { _taskName = value; OnPropertyChanged(); }
        }

        private NotificationStatus _status;
        public NotificationStatus Status
        {
            get => _status;
            set { _status = value; OnPropertyChanged(); }
        }

        private string _errorMessage;
        public string ErrorMessage
        {
            get => _errorMessage;
            set { _errorMessage = value; OnPropertyChanged(); }
        }

        private DateTime _timestamp;
        public DateTime Timestamp
        {
            get => _timestamp;
            set { _timestamp = value; OnPropertyChanged(); }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
