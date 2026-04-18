registry := "registry.annium.com"

_publish-image context dockerfile tag:
    @docker build -t {{registry}}/tools/{{tag}} -f {{context}}/{{dockerfile}} {{context}}
    @docker push {{registry}}/tools/{{tag}}

_publish-package project packageVersion:
    cd {{project}} && dotnet pack --no-build -o . -c Release -p:SymbolPackageFormat=snupkg -p:PackageVersion={{packageVersion}}
    cd {{project}} && dotnet nuget push "*.nupkg" --source https://api.nuget.org/v3/index.json --api-key $(cat ../.xs.credentials)
    cd {{project}} && find . -type f -name '*.nupkg' | xargs -I% rm %

_nix-install:
    ./scripts/nix_install.sh

_nix-uninstall:
    ./scripts/nix_uninstall.sh
