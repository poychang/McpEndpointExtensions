import argparse
import json
import os
from pathlib import Path
import re
from urllib.error import HTTPError
from urllib.request import urlopen
import xml.etree.ElementTree as ET


PROJECT = Path("src/McpEndpointExtensions.csproj")
UPSTREAM_PACKAGE = "modelcontextprotocol.aspnetcore"
PACKAGE = "pc.modelcontextprotocol.extensions.aspnetcore.endpoints"
STABLE_VERSION = re.compile(r"(0|[1-9]\d*)\.(0|[1-9]\d*)\.(0|[1-9]\d*)")


def version_key(version):
    if not STABLE_VERSION.fullmatch(version):
        raise ValueError(f"Expected a stable major.minor.patch version, got {version!r}")
    return tuple(int(part) for part in version.split("."))


def package_versions(package, allow_missing=False):
    url = f"https://api.nuget.org/v3-flatcontainer/{package}/index.json"
    try:
        with urlopen(url, timeout=60) as response:
            return json.load(response)["versions"]
    except HTTPError as error:
        if allow_missing and error.code == 404:
            return []
        raise


def current_version(project):
    root = ET.fromstring(project.read_text(encoding="utf-8-sig"))
    version = root.findtext("./PropertyGroup/Version")
    reference = root.find("./ItemGroup/PackageReference[@Include='ModelContextProtocol.AspNetCore']")
    if reference is None or reference.get("Version") != "$(Version)":
        raise ValueError("The MCP dependency must share the project's $(Version)")
    version_key(version or "")
    return version


def choose_version(current, upstream):
    stable = [version for version in upstream if STABLE_VERSION.fullmatch(version)]
    if not stable:
        raise ValueError("NuGet returned no stable ModelContextProtocol.AspNetCore versions")
    latest = max(stable, key=version_key)
    if version_key(latest) < version_key(current):
        raise ValueError(f"Refusing to downgrade from {current} to {latest}")
    return latest


def apply_version(project, version):
    version_key(version)
    current = current_version(project)
    if version_key(version) < version_key(current):
        raise ValueError(f"Refusing to downgrade from {current} to {version}")
    content = project.read_bytes()
    old = f"<Version>{current}</Version>".encode()
    if content.count(old) != 1:
        raise ValueError("Expected exactly one project Version property")
    project.write_bytes(content.replace(old, f"<Version>{version}</Version>".encode(), 1))


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("--apply-version")
    args = parser.parse_args()
    if args.apply_version:
        apply_version(PROJECT, args.apply_version)
        return

    current = current_version(PROJECT)
    requested = os.environ.get("REQUESTED_VERSION", "")
    if requested:
        version_key(requested)
        if requested != current:
            raise ValueError(f"Requested release {requested} must match the MCP dependency {current}")
        version = requested
        should_publish = True
    else:
        version = choose_version(current, package_versions(UPSTREAM_PACKAGE))
        should_publish = version not in package_versions(PACKAGE, allow_missing=True)

    outputs = {
        "version": version,
        "should_publish": str(should_publish).lower(),
        "update_required": str(version != current).lower(),
    }
    print(json.dumps(outputs))
    with open(os.environ["GITHUB_OUTPUT"], "a", encoding="utf-8") as output:
        for key, value in outputs.items():
            output.write(f"{key}={value}\n")


if __name__ == "__main__":
    main()
