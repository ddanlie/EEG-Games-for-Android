using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;
using UXF;

public class APIClient
{
    private const string BaseUrl = "xxx";
    private bool StubMode;
    private int StubTimerSec;// how long stub operations would last [seconds]
    public APIClient(bool StubMode = false, int StubTimerSec=3000) 
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
        return default; // TODO - real server request


    }

    public async Task<GeneralGameListInfo> GetGeneralEEGGamesInfo()
    {
        if (StubMode)
        {
            await Task.Delay(StubTimerSec);
            return new GeneralGameListInfo
            {
                games = new[]
                {
                    new GeneralGameInfo { id="1", name="Game 1", description="Very nice game, helpful for early disabilities signs detection", domain="Complex Attention", subdomain="Sustained Attention"},
                    new GeneralGameInfo { id="2", name="Game 2", description="Very nice game, helpful for early disabilities signs detection", domain="Complex Attention", subdomain="Divided Attention"},
                    new GeneralGameInfo { id="3", name="Game 3", description="Very nice game, helpful for early disabilities signs detection", domain="Complex Attention", subdomain="Selective Attention"},
                }
            };
        }
        return default; // TODO - real server request
    }


    // Expects folder path with following file structure:
    // sessionTrialPath/other/eeg_data_T001.csv
    // sessionTrialPath/other/events_T001.csv
    // sessionTrialPath/session_info/settings.json
    // 001 - is a trial number. Hence only the first trial of the session will be sent
    public async Task<bool> SendRecoredRunData(string sessionTrialPath, UserIdentity identity)
    {
        if (StubMode)
        {
            await Task.Delay(StubTimerSec);
            return true;
        }


        // Form file paths
        ProfileSessionInfo profileSessionInfo = GameManager.UXFSingleTrialFolderPathParse(sessionTrialPath);

        string otherFolder = Path.Combine(sessionTrialPath, "other");
        string sessionInfoFolder = Path.Combine(sessionTrialPath, "session_info");

        RecordedRunData runData = new RecordedRunData
        {
            eventsFilePath = Path.Combine(otherFolder, "events_T001.csv"),
            eegDataFilePath = Path.Combine(otherFolder, "eeg_data_T001.csv"),
            gameSettingsFilePath = Path.Combine(sessionInfoFolder, "settings.json"),
            profileSessionInfo = profileSessionInfo
        };

        // Read and send the files
        byte[] eventsBytes = File.ReadAllBytes(runData.eventsFilePath);
        byte[] eegBytes = File.ReadAllBytes(runData.eegDataFilePath);
        byte[] settingsBytes = File.ReadAllBytes(runData.gameSettingsFilePath);

        List<IMultipartFormSection> form = new List<IMultipartFormSection>
        {
            new MultipartFormFileSection("events", eventsBytes, "events_T001.csv", "text/csv"),
            new MultipartFormFileSection("eegData", eegBytes, "eeg_data_T001.csv","text/csv"),
            new MultipartFormFileSection("gameSettings", settingsBytes, "settings.json", "application/json"),
            new MultipartFormDataSection("patientId", profileSessionInfo.patientId),
            new MultipartFormDataSection("experimentName", profileSessionInfo.experimentName),
            new MultipartFormDataSection("sessionNumber", profileSessionInfo.sessionNumber),
        };

        byte[] boundary = UnityWebRequest.GenerateBoundary();

        using var request = UnityWebRequest.Post(BaseUrl + "/recorded-run-send", form, boundary);
        request.SetRequestHeader("Authorization", $"Bearer {identity.token}");
        await request.SendWebRequest();

        return request.result == UnityWebRequest.Result.Success;
    }
    public async Task<bool> SynchronizeProfileRunsDataForward(string experimentName, UserIdentity identity)
    {
        if (StubMode)
        {
            await Task.Delay(StubTimerSec);
            return true;
        }

        try
        {
            string profileSessionsPath = GameManager.GetInstance().UXFProfileSessionsDataPath(experimentName, identity.userId);
            string[] sessionFolders = Directory.GetDirectories(profileSessionsPath, "S*", SearchOption.AllDirectories);

            ProfileSessionInfo[] sessions = sessionFolders
                .Select(folder => GameManager.UXFSingleTrialFolderPathParse(folder))
                .ToArray();

            if (sessions.Length == 0)
            {
                return true;
            }

            ProfileSessionsSyncInfo syncInfo = new ProfileSessionsSyncInfo
            {
                sessions = sessions
            };

            byte[] bodyBytes = Encoding.UTF8.GetBytes(JsonUtility.ToJson(syncInfo));

            using var checkRequest = new UnityWebRequest(BaseUrl + "/sync-profile-data", "GET");
            checkRequest.uploadHandler = new UploadHandlerRaw(bodyBytes);
            checkRequest.downloadHandler = new DownloadHandlerBuffer();
            checkRequest.SetRequestHeader("Content-Type", "application/json");
            checkRequest.SetRequestHeader("Authorization", $"Bearer {identity.token}");
            await checkRequest.SendWebRequest();

            if (checkRequest.result != UnityWebRequest.Result.Success)
            {
                return false;
            }

            string json = checkRequest.downloadHandler.text;
            string wrapped = $"{{\"items\":{json}}}";
            SyncStatusList statusList = JsonUtility.FromJson<SyncStatusList>(wrapped);

            if (statusList.items == null)
            {
                return false;
            }

            // --- Send missing sessions ---
            foreach (var entry in statusList.items)
            {
                if (entry.synced)
                {
                    continue;
                }
                string[] info = entry.key.Split('-');
                string infoPatientId = info[0];
                string infoExperimentName = info[1];
                string infoSessionNumber = info[2];

                string sessionPath = GameManager.GetInstance()
                    .UXFSingleTrialFolderPath(infoExperimentName, infoPatientId, int.Parse(infoSessionNumber));

                bool sent = await SendRecoredRunData(sessionPath, identity);
                if (!sent)
                {
                    return false;
                }
            }

            return true;
        }
        catch (Exception e)
        {
            Debug.LogError($"[SynchronizeProfileRunsData] {e.Message}");
            return false;
        }
    }
}