using RiotGames.Platform.Game;
using RiotGames.Platform.Gameclient.Domain;
using RiotGames.Platform.Matchmaking;
using RiotGames.Platform.Reroll.Pojo;
using RiotGames.Platform.Statistics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using WintermintClient.Riot;

namespace WintermintClient.JsApi
{
    internal static class RiotJsTransformer
    {
        private const int kChampionToSkinIdCoefficient = 1000;

        private static string[] allTurns;

        static RiotJsTransformer()
        {
            string[] strArrays = new string[] { "<<none>>", "CHAMP_SELECT", "FAILED_TO_START", "IN_PROGRESS", "JOINING_CHAMP_SELECT", "POST_CHAMP_SELECT", "PRE_CHAMP_SELECT", "START_REQUESTED", "TEAM_SELECT", "TERMINATED", "TERMINATED_IN_ERROR" };
            RiotJsTransformer.allTurns = strArrays;
        }

        private static int GetTurnHash(GameDTO game)
        {
            int num = Math.Max(Array.IndexOf<string>(RiotJsTransformer.allTurns, game.GameState), 0);
            return game.PickTurn << 4 | num & 15;
        }

        private static RiotJsTransformer.JavascriptyTeam ToTeam(long myAccountId, IEnumerable<BannedChampion> bannedChampions, int teamId, IEnumerable<IParticipant> participants)
        {
            RiotJsTransformer.JavascriptyTeam javascriptyTeam = new RiotJsTransformer.JavascriptyTeam()
            {
                Bans = (
                    from x in bannedChampions
                    select x.ChampionId).ToArray<int>(),
                Members = (
                    from p in participants
                    select RiotJsTransformer.TransformParticipant(p, teamId, myAccountId)).ToArray<RiotJsTransformer.JavascriptyPlayer>()
            };
            return javascriptyTeam;
        }

        public static object TransformFailedJoinPlayer(FailedJoinPlayer player)
        {
            QueueDodger queueDodger = player as QueueDodger;
            return new { Summoner = player.Summoner.Name, Reason = player.ReasonFailed.ToLowerInvariant(), Length = (queueDodger != null ? queueDodger.DodgePenaltyRemainingTime : -1) };
        }

        public static RiotJsTransformer.JavascriptyGame TransformGame(GameDTO game, RiotAccount account)
        {
            return RiotJsTransformer.TransformGame(game, account, account.AccountId);
        }

        public static RiotJsTransformer.JavascriptyGame TransformGame(GameDTO game, RiotAccount account, long accountId)
        {
            RiotJsTransformer.JavascriptyPlayer championId;
            if (game == null)
            {
                return new RiotJsTransformer.JavascriptyGame()
                {
                    State = "none"
                };
            }
            GameTypeConfigDTO gameTypeConfigDTO = account.GameTypeConfigs.FirstOrDefault<GameTypeConfigDTO>((GameTypeConfigDTO x) => x.Id == (double)game.GameTypeConfigId) ?? new GameTypeConfigDTO();
            List<BannedChampion> bannedChampions = game.BannedChampions ?? new List<BannedChampion>(0);
            var variable = new
            {
                TeamOne =
                    from x in bannedChampions
                    where x.TeamId == 100
                    select x,
                TeamTwo =
                    from x in bannedChampions
                    where x.TeamId == 200
                    select x
            };
            RiotJsTransformer.JavascriptyGame javascriptyGame = new RiotJsTransformer.JavascriptyGame()
            {
                RealmId = account.RealmId,
                MatchId = (long)game.Id,
                Name = game.Name.Trim(),
                State = GameJsApiService.GetGameState(game.GameState),
                HeroSelectState = GameJsApiService.GetGameHeroSelectState(game.GameState),
                TeamOne = RiotJsTransformer.ToTeam(accountId, variable.TeamOne, 1, game.TeamOne),
                TeamTwo = RiotJsTransformer.ToTeam(accountId, variable.TeamTwo, 2, game.TeamTwo),
                IsOwner = (game.OwnerSummary == null ? false : (long)game.OwnerSummary.AccountId == accountId),
                ConferenceJid = string.Concat(game.RoomName, ".pvp.net"),
                ConferencePassword = game.RoomPassword,
                TurnHash = RiotJsTransformer.GetTurnHash(game),
                GameTypeConfigName = gameTypeConfigDTO.Name,
                QueueName = game.QueueTypeName,
                Created = game.CreationTime,
                ExpiryTime = DateTime.UtcNow + TimeSpan.FromMilliseconds(game.ExpiryTime),
                MapId = game.MapId
            };
            RiotJsTransformer.JavascriptyGame javascriptyGame1 = javascriptyGame;
            Dictionary<string, RiotJsTransformer.JavascriptyPlayer> dictionary = (
                from x in javascriptyGame1.TeamOne.Members.Concat<RiotJsTransformer.JavascriptyPlayer>(javascriptyGame1.TeamTwo.Members)
                where x.InternalName != null
                select x).ToDictionary<RiotJsTransformer.JavascriptyPlayer, string, RiotJsTransformer.JavascriptyPlayer>((RiotJsTransformer.JavascriptyPlayer x) => x.InternalName, (RiotJsTransformer.JavascriptyPlayer x) => x);
            foreach (PlayerChampionSelectionDTO playerChampionSelection in game.PlayerChampionSelections)
            {
                if (!dictionary.TryGetValue(playerChampionSelection.SummonerInternalName, out championId))
                {
                    continue;
                }
                int[] spell1Id = new int[] { (int)playerChampionSelection.Spell1Id, (int)playerChampionSelection.Spell2Id };
                championId.SpellIds = spell1Id;
                championId.ChampionId = playerChampionSelection.ChampionId;
                championId.SkinId = championId.ChampionId * 1000 + playerChampionSelection.SelectedSkinIndex;
            }
            foreach (RiotJsTransformer.JavascriptyPlayer realmId in
                from x in dictionary
                select x.Value)
            {
                realmId.RealmId = account.RealmId;
                if (!(javascriptyGame1.HeroSelectState == "post") && (!(realmId.PickState == "pending") || realmId.ChampionId <= 0))
                {
                    continue;
                }
                realmId.PickState = "completed";
            }
            return javascriptyGame1;
        }

        private static RiotJsTransformer.JavascriptyPlayer TransformParticipant(IParticipant participant, int teamId, long dudeAccountId)
        {
            GameParticipant gameParticipant = participant as GameParticipant;
            if (gameParticipant == null)
            {
                return new RiotJsTransformer.JavascriptyPlayer()
                {
                    PickState = "completed"
                };
            }
            double accountId = -1;
            double summonerId = -1;
            PlayerParticipant playerParticipant = gameParticipant as PlayerParticipant;
            if (playerParticipant != null)
            {
                accountId = playerParticipant.AccountId;
                summonerId = playerParticipant.SummonerId;
            }
            RiotJsTransformer.JsRerollState jsRerollState = null;
            AramPlayerParticipant aramPlayerParticipant = gameParticipant as AramPlayerParticipant;
            if (aramPlayerParticipant != null && aramPlayerParticipant.PointSummary != null)
            {
                PointSummary pointSummary = aramPlayerParticipant.PointSummary;
                RiotJsTransformer.JsRerollState jsRerollState1 = new RiotJsTransformer.JsRerollState()
                {
                    Points = (int)pointSummary.CurrentPoints,
                    MaximumPoints = pointSummary.MaxRolls * (int)pointSummary.PointsCostToRoll,
                    RerollCost = (int)pointSummary.PointsCostToRoll
                };
                jsRerollState = jsRerollState1;
            }
            RiotJsTransformer.JavascriptyPlayer javascriptyPlayer = new RiotJsTransformer.JavascriptyPlayer()
            {
                Name = gameParticipant.SummonerName,
                InternalName = gameParticipant.SummonerInternalName,
                SummonerId = summonerId,
                AccountId = accountId,
                TeamId = teamId,
                RerollState = jsRerollState,
                SpellIds = new int[0],
                PickState = RiotJsTransformer.TransformPickState(gameParticipant.PickMode),
                IsDude = accountId == (double)dudeAccountId
            };
            return javascriptyPlayer;
        }

        public static object TransformPersonalGame(PlayerGameStats playerStats)
        {
            RiotJsTransformer.TransformedPersonalGame transformedPersonalGame = new RiotJsTransformer.TransformedPersonalGame()
            {
                Id = (int)playerStats.GameId,
                Length = -1,
                MapId = (int)playerStats.GameMapId,
                Mode = playerStats.GameMode,
                Queue = playerStats.QueueType,
                Started = playerStats.CreateDate,
                Type = playerStats.QueueType,
                Name = string.Empty,
                AccountId = (long)playerStats.UserId,
                ChampionId = (int)playerStats.ChampionId,
                Spell1 = (int)playerStats.Spell1,
                Spell2 = (int)playerStats.Spell2,
                Left = playerStats.Leaver,
                Statistics = playerStats.Statistics.ToDictionary<RawStat, string, int>((RawStat x) => x.StatType, (RawStat x) => (int)x.Value),
                IpEarned = (int)playerStats.IpEarned,
                Map = GameJsApiService.GetGameMapFriendlyName((int)playerStats.GameMapId)
            };
            return transformedPersonalGame;
        }

        private static string TransformPickState(int pickState)
        {
            switch (pickState)
            {
                case 0:
                    {
                        return "pending";
                    }
                case 1:
                    {
                        return "picking";
                    }
                case 2:
                    {
                        return "completed";
                    }
            }
            return "unknown";
        }

        [Serializable]
        internal class JavascriptyGame
        {
            public string RealmId;

            public long MatchId;

            public string Name;

            public string State;

            public string HeroSelectState;

            public RiotJsTransformer.JavascriptyTeam TeamOne;

            public RiotJsTransformer.JavascriptyTeam TeamTwo;

            public bool IsOwner;

            public string ConferenceJid;

            public string ConferencePassword;

            public string GameTypeConfigName;

            public string QueueName;

            public DateTime Created;

            public DateTime ExpiryTime;

            public int TurnHash;

            public int MapId;

            public int TurnDuration;

            public DateTime TurnEnds;

            public string Id
            {
                get
                {
                    return string.Format("{0}#{1}", this.RealmId, this.MatchId);
                }
            }

            public JavascriptyGame()
            {
            }
        }

        [Serializable]
        internal class JavascriptyPlayer
        {
            public string Name;

            public string InternalName;

            public string RealmId;

            public double AccountId;

            public double SummonerId;

            public int TeamId;

            public bool IsDude;

            public string PickState;

            public RiotJsTransformer.JsRerollState RerollState;

            public int[] SpellIds;

            public int ChampionId;

            public int SkinId;

            public JavascriptyPlayer()
            {
            }
        }

        [Serializable]
        internal class JavascriptyTeam
        {
            public int[] Bans;

            public RiotJsTransformer.JavascriptyPlayer[] Members;

            public JavascriptyTeam()
            {
            }
        }

        [Serializable]
        internal class JsRerollState
        {
            public int Points;

            public int MaximumPoints;

            public int RerollCost;

            public JsRerollState()
            {
            }
        }

        [Serializable]
        internal class TransformedPersonalGame
        {
            public int Id;

            public int Length;

            public int MapId;

            public string Mode;

            public string Queue;

            public DateTime Started;

            public string Type;

            public string Name;

            public long AccountId;

            public int ChampionId;

            public int Spell1;

            public int Spell2;

            public bool Left;

            public Dictionary<string, int> Statistics;

            public string Map;

            public int IpEarned;

            public TransformedPersonalGame()
            {
            }
        }
    }
}