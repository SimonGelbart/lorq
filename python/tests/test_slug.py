from eval_runner.utils import slug


def test_slug_basic():
    assert slug("Default Graphify+ / Admin Permissions r1") == "default-graphify-admin-permissions-r1"
