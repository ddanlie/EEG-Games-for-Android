using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

[Serializable]
public struct UserIdentity
{
    public string token;
    public string userId;
}

[Serializable]
public struct IndividualInfo
{
    public enum Smoker
    {
        None,
        Current,
        Ex
    }

    public enum Alcohol
    {
        None,
        Moderate,
        Heavy
    }

    public enum Sex
    {
        Male,
        Female
    }


    public string name;
    public string userId;
    public string notes;
    public int age;
    public int weightKg;
    public Smoker smoker;
    public Sex sex;
    public Alcohol alcohol;

}


[Serializable]
public struct GeneralGameListInfo
{
    public GeneralGameInfo[] games;
}

[Serializable]
public struct GeneralGameInfo
{
    public string id;
    public string name;
    public string description;
    public string domain;
    public string subdomain;
    public string sceneName;
}

[Serializable]
public struct EEGGameInfo
{
    GeneralGameInfo generalInfo;
    public string[] biomarkersMeasured;
}

[Serializable]
public struct ProfileSessionsSyncInfo
{
    public ProfileSessionInfo[] sessions;
}

[Serializable]
public struct ProfileSessionInfo
{
    public string patientId;
    public string experimentName;
    public string sessionNumber;
}

[Serializable]
public struct SyncStatusList
{
    public SyncStatusEntry[] items;
}

[Serializable]
public struct SyncStatusEntry
{
    public string key;// "patientId-experimentName-sessionNumber"
    public bool synced;
}

[Serializable]
public struct RecordedRunData
{
    public string eventsFilePath;
    public string eegDataFilePath;
    public string gameSettingsFilePath;
    public ProfileSessionInfo profileSessionInfo;
}