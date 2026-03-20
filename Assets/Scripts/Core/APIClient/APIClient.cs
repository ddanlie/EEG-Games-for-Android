using System;
using System.Text;
using System.Threading.Tasks;
using Unity.VisualScripting.Antlr3.Runtime;
using UnityEngine;
using UnityEngine.Networking;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.ListView;

public class APIClient
{
    private const string BaseUrl = "xxx";


    // Registration/Login
    [Serializable] private class EmailRequest { public string email; }
    [Serializable] private class EmailCodeRequest { public string email; public string code; }
    [Serializable] private class UserIdentityResponse { public string token; public string userId; }
    public async Task<UserIdentity> Login(string token)
    {
        var body = Encoding.UTF8.GetBytes(JsonUtility.ToJson(new EmailCodeRequest { email = null, code = null }));

        using var request = new UnityWebRequest(BaseUrl + "/auth/login", "POST");
        request.uploadHandler = new UploadHandlerRaw(body);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Authorization", "Bearer " + token);
        request.SetRequestHeader("Accept", "application/json");

        await request.SendWebRequest();
        if (request.result != UnityWebRequest.Result.Success)
            return new UserIdentity { token = null, userId = null };

        var response = JsonUtility.FromJson<UserIdentityResponse>(request.downloadHandler.text);
        return new UserIdentity { token = response.token, userId = response.userId };
    }

    public async Task<UserIdentity> Login(string email, string code)
    {
        var body = Encoding.UTF8.GetBytes(JsonUtility.ToJson(new EmailCodeRequest { email = email, code = code }));

        using var request = new UnityWebRequest(BaseUrl + "/auth/login", "POST");
        request.uploadHandler = new UploadHandlerRaw(body);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Authorization", "Bearer " + "");
        request.SetRequestHeader("Content-Type", "application/json");

        await request.SendWebRequest();
        if (request.result != UnityWebRequest.Result.Success)
            return new UserIdentity { token = null, userId = null };

        var response = JsonUtility.FromJson<UserIdentityResponse>(request.downloadHandler.text);
        return new UserIdentity { token = response.token, userId = response.userId };
    }

    public async Task<bool> RequestLogin(string email)
    {
        var body = Encoding.UTF8.GetBytes(JsonUtility.ToJson(new EmailRequest { email = email }));

        using var request = new UnityWebRequest(BaseUrl + "/auth/login/request", "POST");
        request.uploadHandler = new UploadHandlerRaw(body);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");

        await request.SendWebRequest();
        return request.result == UnityWebRequest.Result.Success;
    }


    public async Task<bool> RequestRegister(string email)
    {
        var body = Encoding.UTF8.GetBytes(JsonUtility.ToJson(new EmailRequest { email = email }));

        using var request = new UnityWebRequest(BaseUrl + "/auth/register/request", "POST");
        request.uploadHandler = new UploadHandlerRaw(body);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");

        await request.SendWebRequest();
        return request.result == UnityWebRequest.Result.Success;
    }

    public async Task<UserIdentity> Register(string email, string code)
    {
        var body = Encoding.UTF8.GetBytes(JsonUtility.ToJson(new EmailCodeRequest { email = email, code = code }));

        using var request = new UnityWebRequest(BaseUrl + "/auth/register", "POST");
        request.uploadHandler = new UploadHandlerRaw(body);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");

        await request.SendWebRequest();
        if (request.result != UnityWebRequest.Result.Success)
            return new UserIdentity { token = null, userId = null };

        var response = JsonUtility.FromJson<UserIdentityResponse>(request.downloadHandler.text);
        return new UserIdentity { token = response.token, userId = response.userId };
    }

    public async Task<IndividualInfo> GetIndividualInfo(string userId)
    {

    }
}