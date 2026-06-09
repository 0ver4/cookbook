{
  description = "CookBook — ASP.NET Core 8 recipe app";

  inputs.nixpkgs.url = "github:NixOS/nixpkgs/nixos-unstable";

  outputs =
    { self, nixpkgs }:
    let
      systems = [ "x86_64-linux" "aarch64-linux" ];
      forAllSystems = f: nixpkgs.lib.genAttrs systems (system: f nixpkgs.legacyPackages.${system});
    in
    {
      packages = forAllSystems (pkgs: {
        default = pkgs.callPackage ./nix/cookbook.nix { };
      });

      # `nixos-rebuild` consumers import this and set services.cookbook.enable = true;
      nixosModules.default = import ./nix/module.nix self;

      devShells = forAllSystems (pkgs: {
        default = pkgs.mkShell {
          packages = [ pkgs.dotnet-sdk_8 ];
        };
      });
    };
}
