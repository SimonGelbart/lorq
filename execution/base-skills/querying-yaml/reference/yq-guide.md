# yq Quick Reference

This reference assumes `mikefarah/yq`.

## Output Control

```bash
yq -r '.name' file.yml              # raw scalar
yq -o json -I 0 '.items' file.yml   # compact JSON
yq -P '.' file.yml                  # pretty YAML
yq -i '.replicas = 3' file.yml      # edit in place
```

## Common Queries

```bash
yq -r '.services | keys[]' docker-compose.yml
yq -r '.services.*.image' docker-compose.yml
yq '.jobs.build.steps' .github/workflows/ci.yml
yq -r '.jobs.*.runs-on' .github/workflows/ci.yml
yq -r '.spec.template.spec.containers[].name' deployment.yml
yq -r '.field // "default"' file.yml
yq -r '.services.* | select(.ports) | .image' docker-compose.yml
```

## Updates

Preview without `-i`, then apply:

```bash
yq '.spec.replicas = 3' deployment.yml
yq -i '.spec.replicas = 3' deployment.yml
yq '(.spec.template.spec.containers[] | select(.name == "api").image) = "api:v2"' deployment.yml
IMAGE=api:v2 yq -i '.image = strenv(IMAGE)' service.yml
```

## Multi-Document YAML

```bash
yq 'select(document_index == 0)' multi.yml
yq -r 'select(.kind == "Deployment") | .metadata.name' resources.yml
yq ea '. as $item ireduce ({}; . * $item)' base.yml override.yml
```

## Tool Choice

- Use `yq` when edits must preserve YAML formatting, comments, anchors, or document structure.
- Use `yq -o json -I 0` before piping large structured results into another JSON tool.
- Use `strenv(NAME)` for shell-provided strings instead of interpolating values into an expression.
- Use `yq ea` (`eval-all`) when an expression must combine multiple files or documents.
