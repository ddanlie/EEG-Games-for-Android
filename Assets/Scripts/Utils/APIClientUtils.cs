using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public class APIClientUtils
{
    public static Dictionary<string, string> IndividualInfoToDict(IndividualInfo item)
    {
        return new Dictionary<string, string>
        {
            { "Name", item.name },
            { "Sex", item.sex.ToString() },
            { "Age", item.age.ToString() },
            { "Weight [kg]", item.weightKg.ToString() },
            { "Smoker", item.smoker.ToString() },
            { "Alcohol", item.alcohol.ToString() },
            { "User Id", item.userId },
            { "Notes", item.notes },
        };
    }

    public static Dictionary<string, GeneralGameListInfo> GeneralGameListInfoSortBySubdomain(GeneralGameListInfo gameList)
    {
        var dict = new Dictionary<string, List<GeneralGameInfo>>();

        foreach (var game in gameList.games)
        {
            if (!dict.TryGetValue(game.subdomain, out var list))
            {
                list = new List<GeneralGameInfo>();
                dict[game.subdomain] = list;
            }

            list.Add(game);
        }

        var result = new Dictionary<string, GeneralGameListInfo>();

        foreach (var kvp in dict)
        {
            result[kvp.Key] = new GeneralGameListInfo
            {
                games = kvp.Value.ToArray()
            };
        }

        return result;
    }
}
