import io
import os
from pathlib import Path
import tempfile
import unittest
from unittest.mock import patch
from urllib.error import HTTPError

import sync_mcp_version as sync


class SyncVersionTests(unittest.TestCase):
    def setUp(self):
        self.directory = tempfile.TemporaryDirectory()
        self.addCleanup(self.directory.cleanup)
        self.project = Path(self.directory.name) / "library.csproj"
        self.project.write_bytes(
            b'\xef\xbb\xbf<Project><PropertyGroup><Version>2.2.0</Version></PropertyGroup>\r\n'
            b'<ItemGroup><PackageReference Include="ModelContextProtocol.AspNetCore" '
            b'Version="$(Version)" /></ItemGroup></Project>\r\n'
        )

    def test_selects_latest_stable_numerically(self):
        self.assertEqual(
            sync.choose_version("2.2.0", ["2.9.0", "3.0.0-preview.1", "2.10.0", "2.2.0"]),
            "2.10.0",
        )

    def test_same_version_is_not_an_upgrade(self):
        self.assertEqual(sync.choose_version("2.2.0", ["2.2.0"]), "2.2.0")

    def test_rejects_downgrade_and_empty_stable_feed(self):
        for versions in (["2.1.0"], ["3.0.0-preview.1"], []):
            with self.subTest(versions=versions), self.assertRaises(ValueError):
                sync.choose_version("2.2.0", versions)

    def test_updates_shared_version_preserving_encoding_and_newlines(self):
        before = self.project.read_bytes()
        sync.apply_version(self.project, "2.4.0")
        self.assertEqual(
            self.project.read_bytes(),
            before.replace(b"<Version>2.2.0</Version>", b"<Version>2.4.0</Version>"),
        )
        self.assertEqual(sync.current_version(self.project), "2.4.0")

    def test_rejects_invalid_version_and_downgrade_without_changes(self):
        before = self.project.read_bytes()
        for version in ("2.1.0", "2.4.0-preview.1", "2.4.0\nmalicious=true", ""):
            with self.subTest(version=version), self.assertRaises(ValueError):
                sync.apply_version(self.project, version)
            self.assertEqual(self.project.read_bytes(), before)

    def test_requires_shared_dependency_version(self):
        self.project.write_bytes(self.project.read_bytes().replace(b"$(Version)", b"2.2.0"))
        with self.assertRaises(ValueError):
            sync.current_version(self.project)

    def test_missing_own_package_is_allowed_but_other_http_errors_fail(self):
        for code, allow_missing in ((404, False), (403, True), (500, True)):
            with self.subTest(code=code, allow_missing=allow_missing):
                with patch.object(sync, "urlopen", side_effect=HTTPError("url", code, "error", {}, None)):
                    with self.assertRaises(HTTPError):
                        sync.package_versions(sync.PACKAGE, allow_missing=allow_missing)
        with patch.object(sync, "urlopen", side_effect=HTTPError("url", 404, "missing", {}, None)):
            self.assertEqual(sync.package_versions(sync.PACKAGE, allow_missing=True), [])

    def run_check(self, published, requested=""):
        output = Path(self.directory.name) / "output"
        output.write_text("")
        with (
            patch.object(sync, "PROJECT", self.project),
            patch.object(sync, "package_versions", side_effect=[["2.2.0", "2.4.0"], published]) as fetch,
            patch.dict(os.environ, {"REQUESTED_VERSION": requested, "GITHUB_OUTPUT": str(output)}),
            patch("sys.argv", ["sync_mcp_version.py"]),
            patch("sys.stdout", new_callable=io.StringIO),
        ):
            sync.main()
        return dict(line.split("=", 1) for line in output.read_text().splitlines()), fetch.call_count

    def test_new_release_requires_update_and_publish(self):
        outputs, _ = self.run_check(["2.2.0"])
        self.assertEqual(outputs, {
            "version": "2.4.0", "should_publish": "true", "update_required": "true",
        })

    def test_failed_publish_is_retried_after_commit(self):
        sync.apply_version(self.project, "2.4.0")
        outputs, _ = self.run_check(["2.2.0"])
        self.assertEqual(outputs["should_publish"], "true")
        self.assertEqual(outputs["update_required"], "false")

    def test_already_published_version_still_updates_stale_source(self):
        outputs, _ = self.run_check(["2.4.0"])
        self.assertEqual(outputs["should_publish"], "false")
        self.assertEqual(outputs["update_required"], "true")

    def test_up_to_date_published_release_is_noop(self):
        sync.apply_version(self.project, "2.4.0")
        outputs, _ = self.run_check(["2.4.0"])
        self.assertEqual(outputs["should_publish"], "false")
        self.assertEqual(outputs["update_required"], "false")

    def test_explicit_release_must_match_dependency(self):
        outputs, calls = self.run_check([], requested="2.2.0")
        self.assertEqual(outputs["should_publish"], "true")
        self.assertEqual(calls, 0)
        with self.assertRaises(ValueError):
            self.run_check([], requested="2.4.0")


if __name__ == "__main__":
    unittest.main()
