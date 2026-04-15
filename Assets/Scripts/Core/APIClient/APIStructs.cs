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
    string patientId;
    ProfileSessionInfo[] sessions;
}


[Serializable]
public struct ProfileSessionInfo
{
    string experimentName;
    string sessionNumber;
}