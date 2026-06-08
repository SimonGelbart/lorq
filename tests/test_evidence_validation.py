from __future__ import annotations

from pathlib import Path

import pytest

from eval_runner.schema import SchemaError, validate_case
from eval_runner.validators.core import run_validators
from eval_runner.validators.evidence import extract_file_references, symbol_near_file_reference
from eval_runner.utils import write_json


def test_extract_file_references_from_markdown_answer():
    answer = "See `src/Foo/Bar.cs` and src/App/appsettings.json. Ignore https://example.com/a/b.cs."
    assert extract_file_references(answer) == ["src/Foo/Bar.cs", "src/App/appsettings.json"]


def test_symbol_near_file_reference():
    answer = "`src/Auth/PermissionService.cs` implements PermissionService for admin authorization."
    assert symbol_near_file_reference(answer, "PermissionService", ["src/Auth/PermissionService.cs"])


def test_validation_checks_existing_file_references_and_required_evidence(tmp_path: Path):
    worktree = tmp_path / "repo"
    (worktree / "src/Auth").mkdir(parents=True)
    (worktree / "src/Auth/PermissionService.cs").write_text("class PermissionService {}", encoding="utf-8")
    out = tmp_path / "run"
    out.mkdir()
    write_json(out / "run.manifest.json", {"worktree": str(worktree)})
    (out / "answer.md").write_text(
        "PermissionService is implemented in `src/Auth/PermissionService.cs`.",
        encoding="utf-8",
    )
    (out / "stdout.raw.jsonl").write_text("", encoding="utf-8")

    case = {
        "id": "permissions",
        "validation": {
            "required_symbols": [
                {"symbol": "PermissionService", "must_be_near_file_reference": True}
            ],
            "required_files": ["src/Auth/PermissionService.cs"],
            "required_evidence": {
                "min_existing_file_references": 1,
                "min_source_files": 1,
                "max_missing_cited_files": 0,
            },
        },
    }
    result = run_validators(out, case, {"id": "mode"})
    assert result["ok"] is True
    evidence = result["answer"]["evidence"]
    assert evidence["existing_file_reference_count"] == 1
    assert evidence["missing_file_reference_count"] == 0


def test_validation_flags_missing_cited_files_when_configured(tmp_path: Path):
    worktree = tmp_path / "repo"
    worktree.mkdir()
    out = tmp_path / "run"
    out.mkdir()
    write_json(out / "run.manifest.json", {"worktree": str(worktree)})
    (out / "answer.md").write_text("See `src/Missing/File.cs`.", encoding="utf-8")
    (out / "stdout.raw.jsonl").write_text("", encoding="utf-8")

    case = {
        "id": "missing",
        "validation": {
            "required_evidence": {"max_missing_cited_files": 0},
        },
    }
    result = run_validators(out, case, {"id": "mode"})
    assert result["ok"] is False
    checks = {check["type"]: check for check in result["answer"]["checks"]}
    assert checks["max_missing_cited_files"]["ok"] is False


def test_schema_accepts_symbol_objects_and_required_evidence():
    validate_case(
        {
            "id": "case",
            "title": "Case",
            "task": "Do it",
            "validation": {
                "required_symbols": [
                    "Foo",
                    {"symbol": "Bar", "must_be_near_file_reference": True},
                ],
                "required_evidence": {
                    "min_existing_file_references": 1,
                    "require_symbol_near_file_reference": True,
                },
            },
        },
        "case.yaml",
    )


def test_schema_rejects_bad_symbol_object():
    with pytest.raises(SchemaError):
        validate_case(
            {
                "id": "case",
                "title": "Case",
                "task": "Do it",
                "validation": {"required_symbols": [{"name": ""}]},
            },
            "case.yaml",
        )
