using DotaAPI;

DotaClient cl = new("Login", "password@", true);

cl.Connect();
cl.CreateLobby("huesosi", gamemode: DotaAPI.enums.Gamemodes.CM);
cl.JoinTeam(DotaAPI.enums.DotaTeam.Pool, slot: 1);
