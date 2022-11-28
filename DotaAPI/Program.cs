using DotaAPI;

DotaClient cl = new("Login", "password@", true);
/*DotaClient cl = new("angree1bee1", "snake1337322");*/

cl.Connect();
cl.CreateLobby("huesosi", gamemode: DotaAPI.enums.Gamemodes.CM);
cl.JoinTeam(DotaAPI.enums.DotaTeam.Pool, slot: 1);