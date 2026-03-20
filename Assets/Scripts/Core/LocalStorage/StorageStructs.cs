using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

[Serializable]
struct IndividualInfo
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


    string name;
    string userId;
    string notes;
    int age;
    int weightKg;
    Smoker smoker;
    Sex sex;
    Alcohol alcohol;
 
}
