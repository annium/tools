import 'lib.just'

set shell := ["bash", "-cu"]
set positional-arguments

[private]
default:
    @just --list

# base

setup:
    @echo "=== $0 ==="
    dotnet tool restore

format:
    @echo "=== $0 ==="
    dotnet tool run csharpier format . --config-path $(pwd)/.editorconfig
    dotnet tool run xs format -sc -ic

format-full: format
    @echo "=== $0 ==="
    dotnet format style
    dotnet format analyzers

ensure-no-changes:
    #!/usr/bin/env bash
    set -e
    echo "=== ensure-no-changes ==="
    if [[ -n "$(git status --porcelain)" ]]; then
        echo "Changes detected:"
        git status
        git --no-pager diff --no-color --exit-code
    fi

update:
    @echo "=== $0 ==="
    dotnet tool list --format json | jq -r '.data[] | "\(.packageId)"' | xargs -I% dotnet tool install %
    dotnet tool run xs update all -sc -ic

clean:
    @echo "=== $0 ==="
    dotnet tool run xs clean -sc -ic
    find . -type f -name '*.nupkg' | xargs -I% rm %

build:
    #!/usr/bin/env bash
    set -e
    echo "=== build ==="
    packageVersion=$(dotnet tool run versioning get-version -v $(cat version))
    dotnet build -c Release --nologo -v q -p:PackageVersion=$packageVersion

test:
    @echo "=== $0 ==="
    dotnet test -c Release --no-build --nologo --logger "trx;LogFilePrefix=test-results.trx"

# publish / install / uninstall subprojects
# publish packs with --no-build, so it expects a preceding `just build`

publish: publish-doclint publish-versioning publish-xrest

publish-doclint:
    @echo "=== $0 ==="
    @just _publish-packages doclint/src/Annium.DocLint

publish-versioning:
    @echo "=== $0 ==="
    @just _publish-packages versioning/src/Annium.Versioning

publish-xrest:
    @echo "=== $0 ==="
    @just _publish-packages \
        xrest/src/Annium.XRest.Core \
        xrest/src/Annium.XRest \
        xrest/src/sources/Annium.XRest.Sources.Shared \
        xrest/src/sources/Annium.XRest.Sources.AspNetCore

install: install-doclint install-versioning install-xrest

install-doclint:
    @echo "=== $0 ==="
    @just _tool-install doclint/src/Annium.DocLint

install-versioning:
    @echo "=== $0 ==="
    @just _tool-install versioning/src/Annium.Versioning

install-xrest:
    @echo "=== $0 ==="
    @just _tool-install xrest/src/Annium.XRest

uninstall: uninstall-doclint uninstall-versioning uninstall-xrest

uninstall-doclint:
    @echo "=== $0 ==="
    @just _tool-uninstall doclint/src/Annium.DocLint

uninstall-versioning:
    @echo "=== $0 ==="
    @just _tool-uninstall versioning/src/Annium.Versioning

uninstall-xrest:
    @echo "=== $0 ==="
    @just _tool-uninstall xrest/src/Annium.XRest

# xrest development

xrest-server:
    @echo "=== $0 ==="
    dotnet run --project xrest/demo/Annium.XRest.Demo.Server

xrest-gen:
    @echo "=== $0 ==="
    dotnet run --project xrest/src/Annium.XRest -- \
        cs gen \
        -s http://localhost:5000 \
        -ns Annium.XRest.Demo.Client.Api \
        -o xrest/demo/Annium.XRest.Demo.Client/Api \
        -trace

# publish docker images

publish-backuper:
    @echo "=== $0 ==="
    @just _publish-image xbackup/src Backuper.Api/app.dockerfile backuper

# ci

ci-merge-request-short:
    #!/usr/bin/env bash
    set -e
    echo "=== ci-merge-request-short ==="
    just setup
    just format
    just ensure-no-changes
    just clean
    just build

ci-merge-request-full:
    #!/usr/bin/env bash
    set -e
    echo "=== ci-merge-request-full ==="
    just setup
    just format
    just ensure-no-changes
    just clean
    just build
    just test

ci-release:
    #!/usr/bin/env bash
    set -e
    echo "=== ci-release ==="
    just setup
    just format
    just ensure-no-changes
    just ci-set-package-version
    just clean
    just build
    just publish
    just ci-push-tag
    echo "Release complete"

ci-set-package-version:
    @echo "=== $0 ==="
    dotnet tool run versioning set-version -v $(cat version)

ci-push-tag:
    #!/usr/bin/env bash
    set -e
    echo "=== ci-push-tag ==="
    packageVersion=$(dotnet tool run versioning get-version -v $(cat version))
    git push origin v$packageVersion
