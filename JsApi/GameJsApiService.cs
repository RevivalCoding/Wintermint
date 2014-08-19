using System;
using WintermintClient.Data;

namespace WintermintClient.JsApi
{
    public abstract class GameJsApiService : JsApiService
    {
        protected GameJsApiService()
        {
        }

        internal static string GetAllowSpectators(string jsValue)
        {
            string lowerInvariant = jsValue.ToLowerInvariant();
            string str = lowerInvariant;
            if (lowerInvariant != null)
            {
                if (str == "any")
                {
                    return "ALL";
                }
                if (str == "dropin")
                {
                    return "DROPINONLY";
                }
                if (str == "lobby")
                {
                    return "LOBBYONLY";
                }
                if (str != "none")
                {
                }
            }
            return "NONE";
        }

        internal static string GetGameHeroSelectState(string gameState)
        {
            string str = gameState;
            string str1 = str;
            if (str != null)
            {
                if (str1 == "TEAM_SELECT")
                {
                    return "team";
                }
                if (str1 == "PRE_CHAMP_SELECT")
                {
                    return "pre";
                }
                if (str1 == "CHAMP_SELECT")
                {
                    return "pick";
                }
                if (str1 == "POST_CHAMP_SELECT")
                {
                    return "post";
                }
                if (str1 == "JOINING_CHAMP_SELECT")
                {
                    return "found";
                }
            }
            return "none";
        }

        internal static string GetGameMapFriendlyName(int value)
        {
            string mapClassification = GameData.GetMapClassification(value);
            string str = mapClassification;
            if (mapClassification != null)
            {
                if (str == "crystal-scar")
                {
                    return "Crystal Scar";
                }
                if (str == "howling-abyss")
                {
                    return "Howling Abyss";
                }
                if (str == "proving-grounds")
                {
                    return "Proving Grounds";
                }
                if (str == "summoners-rift")
                {
                    return "Summoner's Rift";
                }
                if (str == "twisted-treeline")
                {
                    return "Twisted Treeline";
                }
            }
            return "Unknown";
        }

        internal static string GetGameMode(string jsValue)
        {
            string lowerInvariant = jsValue.ToLowerInvariant();
            string str = lowerInvariant;
            if (lowerInvariant != null)
            {
                if (str == "aram")
                {
                    return "ARAM";
                }
                if (str == "classic")
                {
                    return "CLASSIC";
                }
                if (str == "dominion")
                {
                    return "ODIN";
                }
                if (str == "tutorial")
                {
                    return "TUTORIAL";
                }
            }
            throw new ArgumentException("jsValue");
        }

        internal static string GetGameMode(int mapId)
        {
            int num = mapId;
            if (num != 3)
            {
                switch (num)
                {
                    case 7:
                        {
                            return "ARAM";
                        }
                    case 8:
                        {
                            return "ODIN";
                        }
                }
                return "CLASSIC";
            }
            return "ARAM";
        }

        internal static string GetGameState(string gameState)
        {
            string str = gameState;
            string str1 = str;
            if (str != null)
            {
                switch (str1)
                {
                    case "TEAM_SELECT":
                        {
                            return "team-select";
                        }
                    case "PRE_CHAMP_SELECT":
                    case "CHAMP_SELECT":
                    case "POST_CHAMP_SELECT":
                        {
                            return "hero-select";
                        }
                    case "IN_PROGRESS":
                        {
                            return "in-progress";
                        }
                    case "START_REQUESTED":
                        {
                            return "game-init";
                        }
                    case "JOINING_CHAMP_SELECT":
                        {
                            return "match-found";
                        }
                    case "TERMINATED":
                    case "TERMINATED_IN_ERROR":
                    case "FAILED_TO_START":
                        {
                            return "error";
                        }
                }
            }
            return "unknown";
        }
    }
}