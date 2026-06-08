from eval_runner.judge import normalize_judge_payload, parse_json_from_text


def test_parse_json_from_plain_text():
    payload, error = parse_json_from_text('{"ok": true, "overall_score": 4, "confidence": "high"}')
    assert error is None
    assert payload["overall_score"] == 4


def test_parse_json_from_fence():
    payload, error = parse_json_from_text('```json\n{"ok": true, "overall_score": 3}\n```')
    assert error is None
    assert payload["ok"] is True


def test_normalize_judge_payload_computes_average_when_overall_missing():
    normalized = normalize_judge_payload({
        "ok": True,
        "dimensions": {
            "correctness": {"score": 4},
            "evidence": {"score": 2},
        },
    })
    assert normalized["ok"] is True
    assert normalized["overall_score"] == 3
