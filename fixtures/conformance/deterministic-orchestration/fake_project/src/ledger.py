class LedgerWriter:
    """Tiny deterministic source file used by LORQ migration fixtures."""

    def write_cell(self, cell_id: str, evidence: dict) -> dict:
        return {"cell_id": cell_id, "evidence": evidence, "kind": "cell"}


class ShardManifest:
    def __init__(self, shard_id: str) -> None:
        self.shard_id = shard_id

    def to_dict(self) -> dict:
        return {"shard_id": self.shard_id, "kind": "run_shard"}
