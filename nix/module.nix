self:
{ config, lib, pkgs, ... }:

let
  cfg = config.services.cookbook;
in
{
  options.services.cookbook = {
    enable = lib.mkEnableOption "CookBook recipe app";

    package = lib.mkOption {
      type = lib.types.package;
      default = self.packages.${pkgs.stdenv.hostPlatform.system}.default;
      description = "The CookBook package to run.";
    };

    address = lib.mkOption {
      type = lib.types.str;
      default = "127.0.0.1";
      description = "Address the Kestrel server binds to";
    };

    port = lib.mkOption {
      type = lib.types.port;
      default = 5000;
      description = "Port the app listens on.";
    };

    environmentFile = lib.mkOption {
      type = lib.types.path;
    };
  };

  config = lib.mkIf cfg.enable {
    systemd.services.cookbook = {
      description = "CookBook recipe app";
      wantedBy = [ "multi-user.target" ];
      after = [ "network-online.target" ];
      wants = [ "network-online.target" ];

      environment = {
        ASPNETCORE_ENVIRONMENT = "Production";
        ASPNETCORE_URLS = "http://${cfg.address}:${toString cfg.port}";
        # Trust X-Forwarded-* from Caddy so Identity sees https + correct host
        # (secure cookies, redirect URLs).
        ASPNETCORE_FORWARDEDHEADERS_ENABLED = "true";
        # Data Protection keys (auth cookie signing) persist under $HOME/.aspnet.
        # Pointing HOME at the StateDirectory keeps logins valid across restarts.
        HOME = "/var/lib/cookbook";
      };

      serviceConfig = {
        ExecStart = lib.getExe cfg.package;
        EnvironmentFile = cfg.environmentFile;
        WorkingDirectory = "/var/lib/cookbook";
        StateDirectory = "cookbook";
        DynamicUser = true;
        Restart = "on-failure";

        # Hardening
        NoNewPrivileges = true;
        ProtectSystem = "strict";
        ProtectHome = true;
        PrivateTmp = true;
        PrivateDevices = true;
        RestrictAddressFamilies = [ "AF_INET" "AF_INET6" ];
        RestrictNamespaces = true;
        LockPersonality = true;
        MemoryDenyWriteExecute = false; # .NET JIT needs W^X relaxed
        SystemCallArchitectures = "native";
      };
    };
  };
}
