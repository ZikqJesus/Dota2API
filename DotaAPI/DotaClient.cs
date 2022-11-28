using System;
using System.Collections.Generic;
using System.Threading;

using SteamKit2;
using SteamKit2.Internal; // brings in our protobuf client messages
using SteamKit2.GC; // brings in the GC related classes
using SteamKit2.GC.Dota.Internal; // brings in dota specific protobuf messages
using DotaAPI.enums;
using DotaAPI.utils;
using ProtoBuf;

namespace DotaAPI
{
    internal class DotaClient
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
        public DotaClient(string username, string password, bool isDebug=false)
        {
            this.login = username;
            this.password = password;
            this.isDebug = isDebug;

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
        public void Connect()
        {
            client.Connect();
            while (!isConnected)
            {

            }
        }

        public void OnConnected(SteamClient.ConnectedCallback callback)
        {
            user.LogOn(new SteamUser.LogOnDetails { Username = login, Password = password });
            if (isDebug)
            {
                Console.WriteLine("Connected!");
            }
            
        }
        public void Wait()
        {
            while (!flag)
            {
                // continue running callbacks until we get match details
                callbackManager.RunWaitCallbacks(TimeSpan.FromSeconds(1));
            }
        }
        void OnLoggedOn(SteamUser.LoggedOnCallback callback)
        {
            if (callback.Result != EResult.OK)
            {
                // logon failed (password incorrect, steamguard enabled, etc)
                // an EResult of AccountLogonDenied means the account has SteamGuard enabled and an email containing the authcode was sent
                // in that case, you would get the auth code from the email and provide it in the LogOnDetails

                Console.WriteLine("Unable to logon to Steam: {0}", callback.Result);

                flag = true; // we didn't actually get the match details, but we need to jump out of the callback loop
                return;
            }

            if (isDebug)
                Console.WriteLine("Logged in! Launching DOTA...");

            // we've logged into the account
            // now we need to inform the steam server that we're playing dota (in order to receive GC messages)

            // steamkit doesn't expose the "play game" message through any handler, so we'll just send the message manually
            var playGame = new ClientMsgProtobuf<CMsgClientGamesPlayed>(EMsg.ClientGamesPlayed);

            playGame.Body.games_played.Add(new CMsgClientGamesPlayed.GamePlayed
            {
                game_id = new GameID(APPID), // or game_id = APPID,
            });

            // send it off
            // notice here we're sending this message directly using the SteamClient
            client.Send(playGame);

            // delay a little to give steam some time to establish a GC connection to us
            Thread.Sleep(5000);

            // inform the dota GC that we want a session
            var clientHello = new ClientGCMsgProtobuf<SteamKit2.GC.Dota.Internal.CMsgClientHello>((uint)EGCBaseClientMsg.k_EMsgGCClientHello);
            clientHello.Body.engine = ESourceEngine.k_ESE_Source2;
            coordinator.Send(clientHello, APPID);
            if (isDebug)
                Console.WriteLine("Launched!");
        }
        private void Waiting()
        {
            Thread.Sleep(2000);
        }

        void OnGCMessage(SteamGameCoordinator.MessageCallback callback)
        {
            // setup our dispatch table for messages
            // this makes the code cleaner and easier to maintain
            var messageMap = new Dictionary<uint, Action<IPacketGCMsg>>
            {
                { ( uint )EGCBaseClientMsg.k_EMsgGCClientWelcome, OnClientWelcome },
                { (uint) EDOTAGCMsg.k_EMsgGCPracticeLobbyJoinResponse, OnLobbyCreated},
                {(uint) EDOTAGCMsg.k_EMsgClientToGCGetProfileCardResponse, OnProfileRequest},
                { ( uint )EDOTAGCMsg.k_EMsgGCMatchDetailsResponse, OnMatchDetails },

            };

            Action<IPacketGCMsg> func;
            if (!messageMap.TryGetValue(callback.EMsg, out func))
            {
                // this will happen when we recieve some GC messages that we're not handling
                // this is okay because we're handling every essential message, and the rest can be ignored
                return;
            }

            func(callback.Message);
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

        // this message arrives when the GC welcomes a client
        // this happens after telling steam that we launched dota (with the ClientGamesPlayed message)
        // this can also happen after the GC has restarted (due to a crash or new version)
        void OnLobbyCreated(IPacketGCMsg packetMsg)
        {
            
        }
        void OnClientWelcome(IPacketGCMsg packetMsg)
        {
            // in order to get at the contents of the message, we need to create a ClientGCMsgProtobuf from the packet message we recieve
            // note here the difference between ClientGCMsgProtobuf and the ClientMsgProtobuf used when sending ClientGamesPlayed
            // this message is used for the GC, while the other is used for general steam messages
            var msg = new ClientGCMsgProtobuf<CMsgClientWelcome>(packetMsg);
            if (isDebug)
                Console.WriteLine("GC is welcoming us. Version: {0}", msg.Body.version);
            isConnected = true;
            // at this point, the GC is now ready to accept messages from us
            // so now we'll request the details of the match we're looking for
        }
        public void CreateLobby(string name, string password="", Gamemodes gamemode=Gamemodes.AP, Regions region=Regions.STOCKHOLM)
        {
            var requestMatch = new ClientGCMsgProtobuf<CMsgPracticeLobbyCreate>((uint)EDOTAGCMsg.k_EMsgGCPracticeLobbyCreate);
            requestMatch.Body.lobby_details = new CMsgPracticeLobbySetDetails();
            requestMatch.Body.lobby_details.game_mode = (uint)gamemode;
            requestMatch.Body.lobby_details.game_name = name;
            requestMatch.Body.lobby_details.pass_key = password;
            requestMatch.Body.lobby_details.server_region = (uint)region;

            coordinator.Send(requestMatch, APPID);

            if (isDebug)
                Console.WriteLine("Lobby is created!");

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
            Waiting();
        }

        public void GetPlayerData(uint steam_id)
        {
            var request =
                new ClientGCMsgProtobuf<CMsgClientToGCGetProfileCard>((uint)EDOTAGCMsg.k_EMsgClientToGCGetProfileCard);
            request.Body.account_id = steam_id;

            coordinator.Send(request, APPID);
            Waiting();
        }

        private void OnProfileRequest(IPacketGCMsg packetMsg)
        {
            var msg = new ClientGCMsgProtobuf<CMsgDOTAProfileCard>(packetMsg);
            ProfileCard p;
            try
            {
                p = new()
                {
                    AccountId = msg.Body.account_id,
                    BandgePoints = msg.Body.badge_points,
                    IsPlusSubscriber = msg.Body.is_plus_subscriber,
                    RankTier = msg.Body.rank_tier,
                    RankTierMmrType = msg.Body.rank_tier_mmr_type,
                    EventId = msg.Body.event_id,
                    EventPoints = msg.Body.event_points,
                    FavoriteTeamPacked = msg.Body.favorite_team_packed,
                    LeaderboardRank = msg.Body.leaderboard_rank,
                    LeaderboardRankCore = msg.Body.leaderboard_rank_core,
                    PlusOriginalStartDate = msg.Body.plus_original_start_date,
                    PreviousRankTier = msg.Body.previous_rank_tier,
                    RankTierPeak = msg.Body.rank_tier_peak,
                    RankTierScore = msg.Body.rank_tier_score
                };
            }
                
            catch
            {
                p = new()
                {
                    AccountId = 0
                };
            }
            profile = p;
        }
        
/*        public void JoinTeam(DOTA_GC_TEAM team, uint slot = 1, DOTABotDifficulty botDifficulty = DOTABotDifficulty.BOT_DIFFICULTY_EXTRA3)
        {
            var joinSlot =
                new ClientGCMsgProtobuf<CMsgPracticeLobbySetTeamSlot>((uint)EDOTAGCMsg.k_EMsgGCPracticeLobbySetTeamSlot);
            joinSlot.Body.team = team;
            joinSlot.Body.slot = slot;

            coordinator.Send(joinSlot, APPID);
            if (isDebug)
                Console.WriteLine("changed team");
        }*/

    }   
}
