{
  lib,
  buildDotnetModule,
  dotnetCorePackages,
  fontconfig,
  freetype,
}:

buildDotnetModule rec {
  pname = "cookbook";
  version = "0.1.0";

  src = ../.;

  projectFile = "CookBook/CookBook.csproj";

  # Generated reproducible NuGet lockfile. To create/update it:
  #   nix build .#default.fetch-deps
  #   ./result nix/deps.json
  # (re-run whenever you change PackageReference versions)
  nugetDeps = ./deps.json;

  dotnet-sdk = dotnetCorePackages.sdk_8_0;
  dotnet-runtime = dotnetCorePackages.aspnetcore_8_0;

  # The apphost produced by `dotnet publish` is named "CookBook".
  executables = [ "CookBook" ];

  # QuestPDF -> SkiaSharp needs native font rendering libs at runtime.
  runtimeDeps = [
    fontconfig
    freetype
  ];

  meta = {
    description = "ASP.NET Core 8 recipe app";
    mainProgram = "CookBook";
    platforms = lib.platforms.linux;
  };
}
