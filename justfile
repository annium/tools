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

# publish / install / uninstall subprojects via their own makefiles

publish: publish-doclint publish-versioning publish-xrest

publish-doclint:
    @echo "=== $0 ==="
    @just _make-publish doclint

publish-versioning:
    @echo "=== $0 ==="
    @just _make-publish versioning

publish-xrest:
    @echo "=== $0 ==="
    @just _make-publish xrest

install: install-doclint install-versioning install-xrest

install-doclint:
    @echo "=== $0 ==="
    make -C doclint install

install-versioning:
    @echo "=== $0 ==="
    make -C versioning install

install-xrest:
    @echo "=== $0 ==="
    make -C xrest install

uninstall: uninstall-doclint uninstall-versioning uninstall-xrest

uninstall-doclint:
    @echo "=== $0 ==="
    make -C doclint uninstall

uninstall-versioning:
    @echo "=== $0 ==="
    make -C versioning uninstall

uninstall-xrest:
    @echo "=== $0 ==="
    make -C xrest uninstall

# publish docker images

publish-backuper:
    @echo "=== $0 ==="
    @just _publish-image Backuper/src Backuper.Api/app.dockerfile backuper

publish-mbus-proxy:
    @echo "=== $0 ==="
    @just _publish-image MessageBus/src/MessageBus.Proxy app.dockerfile mbus.proxy

publish-mbus-sink:
    @echo "=== $0 ==="
    @just _publish-image MessageBus/src/MessageBus.Sink app.dockerfile mbus.sink

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

ci-release apiKey repository githubToken:
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
    just ci-push-tag "$2" "$3"
    echo "Release complete"

ci-set-package-version:
    @echo "=== $0 ==="
    dotnet tool run versioning set-version -v $(cat version)

ci-push-tag repository githubToken:
    #!/usr/bin/env bash
    set -e
    echo "=== ci-push-tag ==="
    packageVersion=$(dotnet tool run versioning get-version -v $(cat version))
    git push origin v$packageVersion

# private helpers

_make-publish name:
    #!/usr/bin/env bash
    set -e
    packageVersion=$(dotnet tool run versioning get-version -v $(cat version))
    make -C {{name}} publish packageVersion=$packageVersion
