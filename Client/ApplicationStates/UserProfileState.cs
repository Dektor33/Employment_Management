using BaseLibrary.DTOs;

namespace Client.ApplicationStates
{
    public class UserProfileState
    {
        public Action? Action { get; set; }
        public UserProfile userProfile { get; set; } = new();
        public void ProfileUpdate() => Action?.Invoke();
    }
}
