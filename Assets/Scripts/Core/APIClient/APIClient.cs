using System;
using System.Text;
using System.Threading.Tasks;
using Unity.VisualScripting.Antlr3.Runtime;
using UnityEngine;
using UnityEngine.Networking;

public class APIClient
{
    private const string BaseUrl = "xxx";
    private bool StubMode;
    private int StubTimerSec;// how long stub operations would last [seconds]
    public APIClient(bool StubMode = false, int StubTimerSec=4000) 
    {
        this.StubMode = StubMode;
        this.StubTimerSec = StubTimerSec;
    }

    // Registration/Login
    [Serializable] private class EmailRequest { public string email; }
    [Serializable] private class EmailCodeRequest { public string email; public string code; }
    [Serializable] private class UserIdentityResponse { public string token; public string userId; }
    public async Task<UserIdentity> Login(string token)
    {
        if (StubMode) { await Task.Delay(StubTimerSec); return new UserIdentity { token = "xxx_stub_token_xxx", userId = "1" }; }
        var body = Encoding.UTF8.GetBytes(JsonUtility.ToJson(new EmailCodeRequest { email = null, code = null }));

        using var request = new UnityWebRequest(BaseUrl + "/auth/login", "POST");
        request.uploadHandler = new UploadHandlerRaw(body);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Authorization", "Bearer " + token);
        request.SetRequestHeader("Accept", "application/json");

        await request.SendWebRequest();
        if (request.result != UnityWebRequest.Result.Success)
            return default;

        var response = JsonUtility.FromJson<UserIdentityResponse>(request.downloadHandler.text);
        return new UserIdentity { token = response.token, userId = response.userId };
    }

    public async Task<UserIdentity> Login(string email, string code)
    {
        if (StubMode) { await Task.Delay(StubTimerSec); return new UserIdentity { token = "xxx_stub_token_xxx", userId = "1" }; }
        var body = Encoding.UTF8.GetBytes(JsonUtility.ToJson(new EmailCodeRequest { email = email, code = code }));

        using var request = new UnityWebRequest(BaseUrl + "/auth/login", "POST");
        request.uploadHandler = new UploadHandlerRaw(body);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Authorization", "Bearer " + "");
        request.SetRequestHeader("Content-Type", "application/json");

        await request.SendWebRequest();
        if (request.result != UnityWebRequest.Result.Success)
            return default;

        var response = JsonUtility.FromJson<UserIdentityResponse>(request.downloadHandler.text);
        return new UserIdentity { token = response.token, userId = response.userId };
    }

    public async Task<bool> RequestLogin(string email)
    {
        if (StubMode) { await Task.Delay(StubTimerSec); return true; }
        var body = Encoding.UTF8.GetBytes(JsonUtility.ToJson(new EmailRequest { email = email }));

        using var request = new UnityWebRequest(BaseUrl + "/auth/login/request", "POST");
        request.uploadHandler = new UploadHandlerRaw(body);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");

        await request.SendWebRequest();
        return request.result == UnityWebRequest.Result.Success;
    }


    //public async Task<bool> RequestRegister(string email)
    //{
    //    if (StubMode) { await Task.Delay(StubTimerSec); return true; }
    //    var body = Encoding.UTF8.GetBytes(JsonUtility.ToJson(new EmailRequest { email = email }));

    //    using var request = new UnityWebRequest(BaseUrl + "/auth/register/request", "POST");
    //    request.uploadHandler = new UploadHandlerRaw(body);
    //    request.downloadHandler = new DownloadHandlerBuffer();
    //    request.SetRequestHeader("Content-Type", "application/json");

    //    await request.SendWebRequest();
    //    return request.result == UnityWebRequest.Result.Success;
    //}

    //public async Task<UserIdentity> Register(string email, string code)
    //{
    //    if (StubMode) { await Task.Delay(StubTimerSec); return new UserIdentity { token = "xxx_stub_token_xxx", userId = "1" }; }
    //    var body = Encoding.UTF8.GetBytes(JsonUtility.ToJson(new EmailCodeRequest { email = email, code = code }));

    //    using var request = new UnityWebRequest(BaseUrl + "/auth/register", "POST");
    //    request.uploadHandler = new UploadHandlerRaw(body);
    //    request.downloadHandler = new DownloadHandlerBuffer();
    //    request.SetRequestHeader("Content-Type", "application/json");

    //    await request.SendWebRequest();
    //    if (request.result != UnityWebRequest.Result.Success)
    //        return default;

    //    var response = JsonUtility.FromJson<UserIdentityResponse>(request.downloadHandler.text);
    //    return new UserIdentity { token = response.token, userId = response.userId };
    //}

    public async Task<IndividualInfo> GetIndividualInfo(string userId)
    {
        if (StubMode)
        {
            await Task.Delay(StubTimerSec);
            return new IndividualInfo
            {
                name = "John Doe",
                userId = userId,
                notes = "Test stub",
                age = 30,
                weightKg = 75,
                smoker = IndividualInfo.Smoker.None,
                sex = IndividualInfo.Sex.Male,
                alcohol = IndividualInfo.Alcohol.Moderate
            };
        }
        return default;

        // TODO - real server request
    }
}