namespace UTB.Minute.CanteenClient.Services;

public class AuthService
{
    public enum UserRole
    {
        Student,
        CanteenWorker
    }

    private UserRole _currentRole = UserRole.Student;
    private string _currentUserName = string.Empty;

    public UserRole CurrentRole
    {
        get => _currentRole;
        set => _currentRole = value;
    }

    public string CurrentUserName
    {
        get => _currentUserName;
        set => _currentUserName = value;
    }

    public bool IsStudent => _currentRole == UserRole.Student;
    public bool IsCanteenWorker => _currentRole == UserRole.CanteenWorker;

    public void SetRole(UserRole role, string userName = "")
    {
        _currentRole = role;
        _currentUserName = userName;
    }

    public void SwitchRole()
    {
        _currentRole = _currentRole == UserRole.Student ? UserRole.CanteenWorker : UserRole.Student;
    }
}
