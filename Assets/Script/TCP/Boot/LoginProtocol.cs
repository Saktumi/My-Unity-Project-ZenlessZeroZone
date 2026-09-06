using System;

[Serializable]
public class LoginRequest
{
    public string type = "login";
    public string account;
    public string password;
    public string serverId;
}

[Serializable]
public class LoginResponse
{
    public string type = "login_result";
    public bool success;
    public string reason;
    public string playerName;
    public string token;
}
