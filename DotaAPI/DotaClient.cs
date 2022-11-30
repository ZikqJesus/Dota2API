using SteamKit2;
using SteamKit2.Internal;
using SteamKit2.GC; 
using SteamKit2.GC.Dota.Internal; 
using DotaAPI.enums;
using DotaAPI.utils;
using ProtoBuf;
using DotaAPI.DotaEventArgs;
using System.Net;
using Newtonsoft.Json;

namespace DotaAPI
{
    public class DotaClient
    {
        SteamClient client;
        SteamUser user;
        SteamGameCoordinator coordinator;
        CallbackManager callbackManager;

        string login;
        string password;
        const int APPID = 570;
        bool flag;
        bool isConnected = false;
        bool isDebug;
        public ProfileCard profile;
        public CMsgDOTAMatch Match { get; private set; }
        CSODOTALobby Lobby;
        Dota2HeroesRequestResult herodata;

        public DotaClient(string username, string password, string apikey, bool isDebug=false)
        {
            this.login = username;
            this.password = password;
            this.isDebug = isDebug;

            herodata = GetHeroData(apikey);

            client = new SteamClient();

            user = client.GetHandler<SteamUser>();
            coordinator = client.GetHandler<SteamGameCoordinator>();

            callbackManager = new CallbackManager(client);

            callbackManager.Subscribe<SteamClient.ConnectedCallback>(OnConnected);
            callbackManager.Subscribe<SteamUser.LoggedOnCallback>(OnLoggedOn);
            callbackManager.Subscribe<SteamGameCoordinator.MessageCallback>(OnGCMessage);

            var thread = new Thread(new ThreadStart(Wait));
            thread.Start();
        }

        public event Action OnConnectedDota = delegate { };
        public event EventHandler<UpdatedLobbyEventArgs> OnLobbyChanged;
        public event EventHandler<MatchEndEventArgs> OnMatchEnd;
        public void Connect()
        {
            client.Connect();
            while (!isConnected)
            {

            }
            OnConnectedDota();
        }

        public void OnConnected(SteamClient.ConnectedCallback callback)
        {
            user.LogOn(new SteamUser.LogOnDetails { Username = login, Password = password });
            
        }
        public void Wait()
        {
            while (!flag)
            {
                callbackManager.RunWaitCallbacks(TimeSpan.FromSeconds(1));
            }
        }
        void OnLoggedOn(SteamUser.LoggedOnCallback callback)
        {
            if (callback.Result != EResult.OK)
            {


                Console.WriteLine("Unable to logon to Steam: {0}", callback.Result);

                flag = true; 
                return;
            }

            if (isDebug)
                Console.WriteLine("Logged in! Launching DOTA...");

            var playGame = new ClientMsgProtobuf<CMsgClientGamesPlayed>(EMsg.ClientGamesPlayed);

            playGame.Body.games_played.Add(new CMsgClientGamesPlayed.GamePlayed
            {
                game_id = new GameID(APPID), 
            });

            client.Send(playGame);

            Thread.Sleep(5000);

            var clientHello = new ClientGCMsgProtobuf<CMsgClientHello>((uint)EGCBaseClientMsg.k_EMsgGCClientHello);
            clientHello.Body.engine = ESourceEngine.k_ESE_Source2;
            coordinator.Send(clientHello, APPID);
            if (isDebug)
                Console.WriteLine("Launched!");
        }
        private void Waiting()
        {
            Thread.Sleep(500);
        }

        void OnGCMessage(SteamGameCoordinator.MessageCallback callback)
        {
            var messageMap = new Dictionary<uint, Action<IPacketGCMsg>>
            {
                { (uint)EGCBaseClientMsg.k_EMsgGCClientWelcome, OnClientWelcome },
                { (uint)ESOMsg.k_ESOMsg_CacheSubscribed, CacheSubscribedHandle },
                { (uint)ESOMsg.k_ESOMsg_CacheUnsubscribed, CacheUnSubscribedHandle },
                { (uint)EDOTAGCMsg.k_EMsgClientToGCGetProfileCardResponse, OnProfileRequest },
                { (uint)EDOTAGCMsg.k_EMsgGCMatchDetailsResponse, OnMatchDetails },


            };

            Action<IPacketGCMsg> func;
            if (!messageMap.TryGetValue(callback.EMsg, out func))
            {
                return;
            }

            func(callback.Message);
        }

        private void CacheUnSubscribedHandle(IPacketGCMsg packetMsg)
        {
            var unSub = new ClientGCMsgProtobuf<CMsgSOCacheUnsubscribed>(packetMsg);
            if (Lobby != null && unSub.Body.owner_soid.id == Lobby.lobby_id)
            {
                UpdatedLobbyEventArgs e = new()
                {
                    OldLobby = Lobby,
                    NewLobby = null
                };
                Lobby = null;
               
                EventHandler<UpdatedLobbyEventArgs> handler = OnLobbyChanged;
                if (handler != null)
                {
                    handler(this, e);
                }
            }
        }
        public void LaunchLobby()
        {
            var request = new ClientGCMsgProtobuf<CMsgPracticeLobbyLaunch>((uint)EDOTAGCMsg.k_EMsgGCPracticeLobbyLaunch);
            coordinator.Send(request, APPID);
            
            if (isDebug)
            {
                Console.WriteLine("Lobby started!");
            }

            Waiting();

            var thread = new Thread(new ThreadStart(LobbyStatusCheck));
            thread.Start();

        }
        private Dota2HeroesRequestResult GetHeroData(string APIKey) 
        {
            string data = "";
            using (WebClient client = new WebClient()) 
            {
                data = client.DownloadString("https://api.steampowered.com/IEconDOTA2_570/GetHeroes/v0001/?key=" + APIKey);
            }
            try 
            {
                return JsonConvert.DeserializeObject<Dota2HeroesRequestResult>(data);
            } 
            catch 
            {
                return null;
            }
        }
        private string GetHeroName(uint id)
        {
            if (herodata == null || id < 1)
            {
                return "no_hero";
            }
            Hero hero = herodata.result.heroes.ToList().Find(h => { return h.id == id; });
            if (hero == null)
            {
                return "no_hero";
            }
            return hero.name;
        }
        private void MatchEnd(MatchEndEventArgs e)
        {
            var outcome = e.Lobby.match_outcome;
            if (outcome == EMatchOutcome.k_EMatchOutcome_RadVictory)
            {
                e.Winner = DotaTeam.Radiant;
            }
            else if (outcome == EMatchOutcome.k_EMatchOutcome_DireVictory)
            {
                e.Winner = DotaTeam.Dire;
            }
            else
            {
                return;
            }
            EventHandler<MatchEndEventArgs> handler = OnMatchEnd;
            if (handler != null)
            {
                handler(this, e);
            }

        }
        private void LobbyStatusCheck()
        {
            while (true)
            {
                if ((DotaGameState)Lobby.game_state == DotaGameState.DOTA_GAMERULES_STATE_GAME_IN_PROGRESS)
                    break;
            }
            if (Lobby != null)
            {
                Thread.Sleep(5000);
                List<CSODOTALobbyMember> players = Lobby.all_members;

                List<string> RadiantHeroes = new List<string>();
                List<string> DireHeroes = new List<string>();
                List<string> RadiantPlayers = new();
                List<string> DirePlayers = new();

                foreach (CSODOTALobbyMember player in players)
                {
                    if (player.team == DOTA_GC_TEAM.DOTA_GC_TEAM_GOOD_GUYS)
                    {
                        string heroName = GetHeroName(player.hero_id);
                        RadiantHeroes.Add(heroName);
                        RadiantPlayers.Add(player.name);
                    }
                    else if (player.team == DOTA_GC_TEAM.DOTA_GC_TEAM_BAD_GUYS)
                    {
                        string heroName = GetHeroName(player.hero_id);
                        DireHeroes.Add(heroName);
                        DirePlayers.Add(player.name);
                    }
                }
                MatchEndEventArgs e = new()
                {
                    DireHeroes = DireHeroes,
                    RadiantHeroes = RadiantHeroes,
                    Lobby = Lobby,
                    DirePlayers = DirePlayers,
                    RadiantPlayers = RadiantPlayers
                };
                while (true)
                {
                    if (Lobby == null)
                    {
                        break;
                    }
                    if (Lobby.match_outcome != EMatchOutcome.k_EMatchOutcome_Unknown)
                    {
                        e.Lobby.match_outcome = Lobby.match_outcome;
                    }
                    
                }
                e.Lobby.match_outcome = EMatchOutcome.k_EMatchOutcome_Unknown;

                MatchEnd(e);
            }

        }
        public void GetMatchDetails(uint match_id)
        {
            var requestMatch = new ClientGCMsgProtobuf<CMsgGCMatchDetailsRequest>((uint)EDOTAGCMsg.k_EMsgGCMatchDetailsRequest);
            requestMatch.Body.match_id = match_id;

            coordinator.Send(requestMatch, APPID);
            
            Waiting();
        }
        private void OnMatchDetails(IPacketGCMsg obj)
        {
            var msg = new ClientGCMsgProtobuf<CMsgGCMatchDetailsResponse>(obj);

            EResult result = (EResult)msg.Body.result;
            if (result != EResult.OK)
            {
                Console.WriteLine("Unable to request match details: {0}", result);
            }

            Match = msg.Body.match;
        }

        void CacheSubscribedHandle(IPacketGCMsg packetMsg)
        {
            var msg = new ClientGCMsgProtobuf<CMsgSOCacheSubscribed>(packetMsg);
            foreach (var cache in msg.Body.objects)
            {
                SubscribedType(cache);
            }
        }

        void SubscribedType(CMsgSOCacheSubscribed.SubscribedType type)
        {
            switch ((UpdateTypes)type.type_id)
            {
                case UpdateTypes.LOBBY:
                    OnLobbyUpdated(type.object_data[0]);
                    break;
            }
        }
        void OnLobbyUpdated(byte[] data)
        {
            CSODOTALobby lob;
            using (var stream = new MemoryStream(data))
            {
                lob = Serializer.Deserialize<CSODOTALobby>(stream);
                Lobby = lob;
            }
            
            UpdatedLobbyEventArgs e = new()
            {
                OldLobby = Lobby,
                NewLobby = lob
            };

            EventHandler<UpdatedLobbyEventArgs> handler = OnLobbyChanged;
            if (handler != null)
            {
                handler(this, e);
            }
        }

        void OnClientWelcome(IPacketGCMsg packetMsg)
        {
            var msg = new ClientGCMsgProtobuf<CMsgClientWelcome>(packetMsg);
            if (isDebug)
                Console.WriteLine("GC is welcoming us. Version: {0}", msg.Body.version);
            isConnected = true;
        }
        public void CreateLobby(string name, string password="", Gamemodes gamemode=Gamemodes.AP, Regions region=Regions.STOCKHOLM)
        {
            if (Lobby != null)
            {
                Console.WriteLine("Lobby already created!");
                return;
            }
            var request = new ClientGCMsgProtobuf<CMsgPracticeLobbyCreate>((uint)EDOTAGCMsg.k_EMsgGCPracticeLobbyCreate);
            request.Body.lobby_details = new CMsgPracticeLobbySetDetails();
            request.Body.lobby_details.game_mode = (uint)gamemode;
            request.Body.lobby_details.game_name = name;
            request.Body.lobby_details.pass_key = password;
            request.Body.lobby_details.server_region = (uint)region;

            coordinator.Send(request, APPID);

            if (isDebug)
                Console.WriteLine("Lobby is created!");

            Waiting();
        }

        public void LeaveLobby()
        {
            var requestMatch = new ClientGCMsgProtobuf<CMsgPracticeLobbyLeave>((uint)EDOTAGCMsg.k_EMsgGCPracticeLobbyLeave);
            coordinator.Send(requestMatch, APPID);

            if (isDebug)
                Console.WriteLine("getting out lobby!");

            Waiting();
        }

        public void JoinTeam(DotaTeam team, uint slot = 1)
        {
            var joinSlot =
                new ClientGCMsgProtobuf<CMsgPracticeLobbySetTeamSlot>((uint)EDOTAGCMsg.k_EMsgGCPracticeLobbySetTeamSlot);
            joinSlot.Body.team = (DOTA_GC_TEAM)team;
            joinSlot.Body.slot = slot;

            coordinator.Send(joinSlot, APPID);
            Waiting();
        }
        public void InviteToLobby(ulong steam_id)
        {
            var invite = new ClientGCMsgProtobuf<CMsgInviteToLobby>((uint)EGCBaseMsg.k_EMsgGCInviteToLobby);
            invite.Body.steam_id = steam_id;

            coordinator.Send(invite, APPID);
            if (isDebug)
            {
                Console.WriteLine("Invited!");
            }
            Waiting();
        }

        public ProfileCard GetDotaProfile(uint steam_id)
        {
            var request =
                new ClientGCMsgProtobuf<CMsgClientToGCGetProfileCard>((uint)EDOTAGCMsg.k_EMsgClientToGCGetProfileCard);
            request.Body.account_id = steam_id;

            coordinator.Send(request, APPID);
            Waiting();
            var p = profile;
            profile = null;
            return p;

        }

        private void OnProfileRequest(IPacketGCMsg packetMsg)
        {
            profile = new(packetMsg);

        }

    }   
}
