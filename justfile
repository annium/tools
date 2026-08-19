set shell := ["bash", "-cu"]
set positional-arguments
# lib.just is copied in by the umbrella repo's `just copy-ci`; recipes redefined below
# override the shared ones. local.just holds this repo's own private helpers.
set allow-duplicate-recipes := true

import 'lib.just'
import 'local.just'

# overrides

# this repo publishes each subproject's packages separately, reading the nuget key from
# .xs.credentials rather than taking it as an argument, so the shared pack/publish pair is replaced

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

# no docs pipeline in this repo, so the shared ci-* recipes' `just docs-lint` step is dropped
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
    # the publish recipes read the nuget key from .xs.credentials, which `just copy-keys` provisions
    # locally from the umbrella repo; in CI it arrives as a secret instead. The file is gitignored,
    # so writing it here does not disturb ensure-no-changes.
    printf '%s' "$1" > .xs.credentials
    just setup
    just format
    just ensure-no-changes
    just ci-set-package-version
    just clean
    just build
    just test
    just publish
    just ci-push-tag "$2" "$3"
    echo "Release complete"
